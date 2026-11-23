// ================================================================
// Infrastructure / Services / V1 / AuditRepositoryService.cs
// ================================================================
using Application.DTOs;
using Application.Interfaces.V1;
using Dapper;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using DocumentFormat.OpenXml.Spreadsheet;
using Domain.Entities.Audit;
using Domain.Entities.Common;
using Domain.Entities.Modals;
using Domain.Entities.Response;
using Domain.Exceptions;
using Domain.ValueObjects;
using Infrastructure.Utilitys;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.Extensions.Configuration;
using MyNewEncDec;
using System.Data;
using System.Data.SqlClient;

namespace Infrastructure.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly DapperHelper _dapperHelper;

        public AuditRepository(DapperHelper dapperHelper)
        {
            _dapperHelper = dapperHelper;
        }

        #region Command
        // ── Generate audit code ───────────────────────────────

        public async Task<string> GenerateAuditCodeAsync()
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@AUDIT_CODE", dbType: DbType.String, direction: ParameterDirection.Output, size: 30);

                await _dapperHelper.ExecuteStoredProcedureAsync("SP_FACTORY_AUDIT_CODE_GENERATE", parameters);

                var auditCode = parameters.Get<string>("@AUDIT_CODE");

                if (string.IsNullOrWhiteSpace(auditCode))
                    throw new InvalidOperationException("SP_FACTORY_AUDIT_CODE_GENERATE returned null or empty audit code.");

                return auditCode;
            }
            catch (SqlException ex)
            {
                // SQL Server specific errors — connection, timeout, constraint violations
                throw new DataAccessException($"Database error while generating audit code. SQL Error {ex.Number}: {ex.Message}", ex);
            }
            catch (InvalidOperationException ex)
            {
                // SP returned empty/null — business logic failure
                throw new AuditCodeGenerationException(ex.Message);
            }
            catch (Exception ex)
            {
                // Unexpected errors — log and rethrow as domain exception
                throw new AuditCodeGenerationException($"Unexpected error while generating audit code: {ex.Message}");
            }
        }
        // ── Insert new audit ─────────────────────────────────
        public async Task<int> InsertAsync(AuditReport audit)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@AUDIT_CODE", audit.AuditCode.Value, DbType.String, size: 30);
                parameters.Add("@STATUS_ID", (int)audit.Status, DbType.Int32);
                parameters.Add("@CURRENT_STEP", audit.CurrentStep, DbType.Int32);
                parameters.Add("@MAX_UNLOCKED_STEP", audit.MaxUnlockedStep, DbType.Int32);
                parameters.Add("@AUDITOR_EMP_CODE", audit.AuditorEmpCode, DbType.String, size: 30);
                parameters.Add("@AUDIT_DATE", audit.AuditDate, DbType.DateTime);
                parameters.Add("@AUDIT_REQ_ID", audit.AuditReqId, DbType.Int64);
                parameters.Add("@FACTORY_OWNER_EMAIL", audit.FactoryOwnerEmail, DbType.String, size: 150);
                parameters.Add("@CREATED_BY", audit.CreatedBy, DbType.String, size: 100);

                // Output parameter — must specify size for int
                parameters.Add("@NEW_AUDIT_ID", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await _dapperHelper.ExecuteStoredProcedureAsync("SP_FACTORY_AUDIT_REPORT_INSERT", parameters);

                var newAuditId = parameters.Get<int>("@NEW_AUDIT_ID");

                if (newAuditId <= 0)
                    throw new InvalidOperationException("SP_FACTORY_AUDIT_REPORT_INSERT returned invalid Audit ID.");
                return newAuditId;
            }
            catch (SqlException ex)
            {
                throw new DataAccessException(
                    $"Database error while inserting audit report. SQL Error {ex.Number}: {ex.Message}", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new DataAccessException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                throw new DataAccessException(
                    $"Unexpected error while inserting audit report: {ex.Message}", ex);
            }

        }

        // ── Full update of audit header ───────────────────────
        public async Task UpdateAsync(AuditReport audit)
        {
            try
            {
                // ── Build TVP DataTables ──────────────────────────

                var documentTable = BuildDocumentTable(audit);
                var checkpointTable = BuildCheckpointTable(audit);
                var elementTable = BuildElementTable(audit);
                var attachmentTable = BuildAttachmentTable(audit);
                var activityTable = BuildActivityLogTable(audit);

                // ── Build scalar parameters ───────────────────────

                var p = new DynamicParameters();
                p.Add("@AUDIT_ID", audit.AuditId, DbType.Int32);
                p.Add("@STATUS_ID", (int)audit.Status, DbType.Int32);
                p.Add("@CURRENT_STEP", audit.CurrentStep, DbType.Int32);
                p.Add("@MAX_UNLOCKED_STEP", audit.MaxUnlockedStep, DbType.Int32);
                p.Add("@AUDITOR_EMP_CODE", audit.AuditorEmpCode, DbType.String, size: 30);
                p.Add("@AUDIT_DATE", audit.AuditDate, DbType.DateTime);
                p.Add("@AUDIT_REQ_ID", audit.AuditReqId, DbType.Int64);
                p.Add("@FACTORY_OWNER_EMAIL", audit.FactoryOwnerEmail, DbType.String, size: 150);
                p.Add("@AUDIT_SCORE_PERCENT", audit.AuditScorePercent?.Value, DbType.Decimal);
                p.Add("@GRADE_ID", audit.GradeId, DbType.Int32);
                p.Add("@VALIDITY_TILL", audit.ValidityTill, DbType.Date);
                p.Add("@OVER_ALL_COMMENTS", audit.OverallComments, DbType.String, size: 2000);
                p.Add("@IS_DELETED", audit.IsDeleted, DbType.Boolean);
                p.Add("@UPDATED_BY", audit.UpdatedBy, DbType.String, size: 100);
                p.Add("@SUBMITTED_AT", audit.SubmittedAt, DbType.DateTime2);

                // ── Add TVP parameters ────────────────────────────
                // TVPs must use SqlMapper.ICustomQueryParameter via AsTableValuedParameter()

                p.Add("@DOCUMENT_RESPONSES", documentTable.AsTableValuedParameter("TVP_FACTORY_AUDIT_DOCUMENT_RESPONSE"));

                p.Add("@CHECKPOINT_RESPONSES", checkpointTable.AsTableValuedParameter("TVP_FACTORY_AUDIT_CHECKPOINT_RESPONSE"));

                p.Add("@ELEMENT_SCORES", elementTable.AsTableValuedParameter("TVP_FACTORY_AUDIT_ELEMENT_SCORE"));

                p.Add("@ATTACHMENT_RESPONSES", attachmentTable.AsTableValuedParameter("TVP_FACTORY_AUDIT_ATTACHMENT_RESPONSE"));

                p.Add("@ACTIVITY_LOGS", activityTable.AsTableValuedParameter("TVP_FACTORY_AUDIT_ACTIVITY_LOG"));

                // ── Single SP call — transaction handled inside SP ─
                await _dapperHelper.ExecuteStoredProcedureAsync("SP_FACTORY_AUDIT_REPORT_SAVE_ALL", p);
            }
            catch (SqlException ex)
            {
                // Map SP THROW error numbers to domain exceptions
                throw ex.Number switch
                {
                    50010 => new AuditNotFoundException(audit.AuditId),
                    50032 => new InvalidCheckPointScoreException(0),
                    _ => new DataAccessException($"Database error in UpdateAsync. SQL Error {ex.Number}: {ex.Message}", ex)
                };
            }
            catch (Exception ex)
            {
                throw new DataAccessException($"Unexpected error in UpdateAsync: {ex.Message}", ex);
            }
        }

        // ── TVP_AUDIT_DOCUMENT_RESPONSE ───────────────────────────
        private static DataTable BuildDocumentTable(AuditReport audit)
        {
            var dt = new DataTable();
            dt.Columns.Add("ITEM_ID", typeof(int));
            dt.Columns.Add("IS_AVAILABLE", typeof(string));
            dt.Columns.Add("VALIDITY_DATE", typeof(DateTime));
            dt.Columns.Add("UNIT_INFO", typeof(string));
            dt.Columns.Add("ATTACHMENTS", typeof(string));
            dt.Columns.Add("REMARKS", typeof(string));

            foreach (var dr in audit.DocumentResponses)
            {
                dt.Rows.Add(
                    dr.ItemId,
                    dr.IsAvailable.Raw?.ToString() ?? (object)DBNull.Value,
                    dr.ValidityDate.HasValue
                        ? dr.ValidityDate.Value : DBNull.Value,
                    dr.UnitInfo ?? (object)DBNull.Value,
                    dr.Attachments ?? (object)DBNull.Value,
                    dr.Remarks ?? (object)DBNull.Value
                );
            }

            return dt;
        }

        // ── TVP_AUDIT_CHECKPOINT_RESPONSE ────────────────────────
        private static DataTable BuildCheckpointTable(AuditReport audit)
        {
            var dt = new DataTable();
            dt.Columns.Add("CHECK_POINT_ID", typeof(int));
            dt.Columns.Add("SCORE", typeof(int));
            dt.Columns.Add("REMARKS", typeof(string));

            foreach (var cp in audit.CheckPointResponses)
            {
                dt.Rows.Add(cp.CheckPointId, cp.Score.Value, cp.Remarks ?? (object)DBNull.Value);
            }

            return dt;
        }

        // ── TVP_AUDIT_ELEMENT_SCORE ──────────────────────────────
        private static DataTable BuildElementTable(AuditReport audit)
        {
            var dt = new DataTable();
            dt.Columns.Add("ELEMENT_ID", typeof(int));
            dt.Columns.Add("TOTAL_CHECK_POINTS", typeof(int));
            dt.Columns.Add("TOTAL_SCORE", typeof(decimal));
            dt.Columns.Add("ELEMENT_AVG_SCORE", typeof(decimal));
            dt.Columns.Add("FINAL_RATING", typeof(int));

            foreach (var es in audit.ElementScores)
            {
                dt.Rows.Add(
                    es.ElementId,
                    es.TotalCheckPoints,
                    es.TotalScore,
                    es.ElementAvgScore,
                    es.FinalRating
                );
            }

            return dt;
        }

        // ── TVP_AUDIT_ATTACHMENT_RESPONSE ────────────────────────
        private static DataTable BuildAttachmentTable(AuditReport audit)
        {
            var dt = new DataTable();
            dt.Columns.Add("ITEM_ID", typeof(int));
            dt.Columns.Add("ATTACHMENT", typeof(string));
            dt.Columns.Add("REMARKS", typeof(string));

            foreach (var ar in audit.AttachmentResponses)
            {
                dt.Rows.Add(
                    ar.ItemId,
                    ar.Attachment ?? (object)DBNull.Value,
                    ar.Remarks ?? (object)DBNull.Value
                );
            }

            return dt;
        }

        // ── TVP_AUDIT_ACTIVITY_LOG ───────────────────────────────
        private static DataTable BuildActivityLogTable(AuditReport audit)
        {
            var dt = new DataTable();
            dt.Columns.Add("ACTION", typeof(string));
            dt.Columns.Add("STEP_NO", typeof(int));
            dt.Columns.Add("PERFORMED_BY", typeof(string));
            dt.Columns.Add("NOTES", typeof(string));

            // Only NEW logs (LogId == 0 means not yet persisted)
            foreach (var log in audit.ActivityLogs.Where(l => l.LogId == 0))
            {
                dt.Rows.Add(
                    log.Action,
                    log.StepNo.HasValue ? log.StepNo.Value : DBNull.Value,
                    log.PerformedBy,
                    log.Notes ?? (object)DBNull.Value
                );
            }

            return dt;
        }

        // ── Get by ID (full aggregate) ────────────────────────

        #endregion

        #region Query

        // ── Get By ID ─────────────────────────────────────────────
        public async Task<AuditReport?> GetByIdAsync(int auditId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@AUDIT_ID", auditId, DbType.Int32);

                var (auditRows, cpMapRows, cps) = await _dapperHelper.ExecuteStoredProcedureMultiple3ListAsync<AuditReportHeaderDto, CheckPointElementMapDto, AuditCheckPointDto>("SP_FACTORY_AUDIT_REPORT_GET_BY_ID", parameters);

                var row = auditRows.FirstOrDefault();

                if (row is null)
                    return null;

                var audit = AuditReport.Reconstitute(
                    row.AUDIT_ID,
                    row.AUDIT_CODE,
                    row.STATUS_ID,
                    row.CURRENT_STEP,
                    row.MAX_UNLOCKED_STEP,
                    row.AUDITOR_EMP_CODE,
                    row.AUDIT_DATE,
                    row.AUDIT_REQ_ID,
                    row.FACTORY_OWNER_EMAIL,
                    row.AUDIT_SCORE_PERCENT,
                    row.GRADE_ID,
                    row.GRADE_CODE?.FirstOrDefault(),
                    row.VALIDITY_TILL,
                    row.OVERALLCOMMENTS,
                    row.IS_DELETED,
                    row.CREATED_BY,
                    row.CREATED_ON,
                    row.UPDATED_BY,
                    row.UPDATED_ON,
                    row.SUBMITTED_AT);

                audit.SetCheckPointElementMap(cpMapRows.ToDictionary(x => x.CHECK_POINT_ID, x => x.ELEMENT_ID));

                if (cps is not null)
                {
                    List<AuditCheckPointResponse> auditCheckPointResponses = new List<AuditCheckPointResponse>();
                    foreach (var item in cps)
                    {
                        auditCheckPointResponses.Add(AuditCheckPointResponse.Reconstitute(item.RESPONSE_ID, item.AUDIT_ID, item.CHECK_POINT_ID, item.SCORE,
                            item.REMARKS, item.CREATED_BY, item.CREATED_ON, item.UPDATED_BY, item.UPDATED_ON));
                    }
                    audit.ReconstituteCheckPointResponseList(auditCheckPointResponses);
                }
                return audit;
            }
            catch (SqlException ex)
            {
                throw new DataAccessException(
                    $"Database error in GetByIdAsync. SQL Error {ex.Number}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new DataAccessException(
                    $"Unexpected error in GetByIdAsync: {ex.Message}", ex);
            }
        }

        // ── Get By Code ───────────────────────────────────────────
        public async Task<AuditReport?> GetByCodeAsync(string auditCode)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@AUDIT_CODE", auditCode, DbType.String, size: 30);

                var auditId = await _dapperHelper.ExecuteScalerStoredProcedureAsync<int?>("SP_FACTORY_AUDIT_REPORT_GET_BY_CODE", parameters);

                if (auditId is null or 0) return null;

                return await GetByIdAsync(auditId.Value);
            }
            catch (SqlException ex)
            {
                throw new DataAccessException(
                    $"Database error in GetByCodeAsync. SQL Error {ex.Number}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new DataAccessException(
                    $"Unexpected error in GetByCodeAsync: {ex.Message}", ex);
            }
        }

        // ── Get Paged ─────────────────────────────────────────────
        public async Task<PagedResult<AuditHeaderRecord>> GetPagedAsync(AuditListFilter filter)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@SEARCH_TERM", filter.SearchTerm, DbType.String, size: 100);
                parameters.Add("@STATUS_ID", filter.StatusId, DbType.Int32);
                parameters.Add("@FROM_DATE", filter.FromDate, DbType.DateTime);
                parameters.Add("@TO_DATE", filter.ToDate, DbType.DateTime);
                parameters.Add("@EMP_CODE", filter.AuditorEmpCode, DbType.String, size: 30);
                parameters.Add("@PAGE", filter.Page, DbType.Int32);
                parameters.Add("@PAGE_SIZE", filter.PageSize, DbType.Int32);


                var (totalRow, auditRows) = await _dapperHelper.ExecuteStoredProcedureMultipleListAsync<int, AuditHeaderRecord>(
                    "SP_FACTORY_AUDIT_REPORT_GET_PAGED", parameters);

                var totalCount = totalRow.FirstOrDefault();
                var items = auditRows?.ToList() ?? new List<AuditHeaderRecord>();

                return new PagedResult<AuditHeaderRecord>(items, totalCount, filter.Page, filter.PageSize);
            }
            catch (SqlException ex)
            {
                throw new DataAccessException(
                    $"Database error in GetPagedAsync. SQL Error {ex.Number}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new DataAccessException(
                    $"Unexpected error in GetPagedAsync: {ex.Message}", ex);
            }
        }

        // ── Get Detail ────────────────────────────────────────────
        public async Task<AuditDetailResponse?> GetDetailAsync(int auditId)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@AUDIT_ID", auditId, DbType.Int32);

                var resultSets = await _dapperHelper.ExecuteStoredProcedureWithNListAsync("SP_FACTORY_AUDIT_REPORT_GET_DETAIL", parameters);

                // SP returns 5 result sets:
                //   #1 — header
                //   #2 — document responses
                //   #3 — checkpoint responses
                //   #4 — element scores
                //   #5 — attachment responses
                //   #6 — Rating Grid Grade Master responses

                // #1 Header
                var hdr = resultSets[0].Cast<dynamic>().FirstOrDefault();

                if (hdr == null)
                    return null;


                // #2 Document responses

                var docsRows = resultSets.Length > 1 ? resultSets[1] : Enumerable.Empty<dynamic>();
                var docs = docsRows
                    .Select(r => new DocumentResponseDto(
                        (int)r.RESPONSE_ID,
                        (int)r.ITEM_ID,
                        (int)r.SR_NO,
                        (string)r.DOCUMENT_NAME,
                        (bool)r.IS_MANDATORY,
                        (string)r.IS_AVAILABLE,
                        (DateTime?)r.VALIDITY_DATE,
                        (string)r.UNIT_INFO,
                        (string?)r.ATTACHMENTS,
                        (string?)r.REMARKS))
                    .ToList();

                // #3 Checkpoint responses
                var cpsRows = resultSets.Length > 2 ? resultSets[2] : Enumerable.Empty<dynamic>();
                var cps = cpsRows
                    .Select(r => new CheckPointResponseDto(
                        (int)r.RESPONSE_ID,
                        (int)r.CHECK_POINT_ID,
                        (int)r.ELEMENT_ID,
                        (int)r.CHECK_POINT_NO,
                        (string)r.QUESTION,
                        (bool)r.IS_CRITICAL,
                        (int?)r.SCORE,
                        CheckPointScoreValue.GetLabel((int?)r.SCORE),
                        (string?)r.REMARKS))
                    .ToList();

                // #4 Element scores
                var elemsRows = resultSets.Length > 3 ? resultSets[3] : Enumerable.Empty<dynamic>();
                var elems = elemsRows
                    .Select(r => new ElementScoreDto(
                        (int)r.SCORE_ID,
                        (int)r.ELEMENT_ID,
                        (int)r.ELEMENT_NO,
                        (string)r.ELEMENT_NAME,
                        (int)r.TOTAL_CHECK_POINTS,
                        (decimal)r.TOTAL_SCORE,
                        (decimal)r.ELEMENT_AVG_SCORE,
                        (int)r.FINAL_RATING,
                        CheckPointScoreValue.GetLabel((int)r.FINAL_RATING)))
                    .ToList();

                // #5 Attachment responses
                var attachesRows = resultSets.Length > 4 ? resultSets[4] : Enumerable.Empty<dynamic>();
                var attaches = attachesRows
                    .Select(r => new AttachmentResponseDto(
                        (int)r.ATTACHMENT_ID,
                        (int)r.ITEM_ID,
                        (int)r.SR_NO,
                        (string)r.ATTACHMENT_NAME,
                        (bool)r.IS_MANDATORY,
                        (string)r.IS_AVAILABLE,
                        (string?)r.ATTACHMENT,
                        (string?)r.REMARKS))
                    .ToList();

                // #6 Grade Master responses
                var gradeRows = resultSets.Length > 5 ? resultSets[5] : Enumerable.Empty<dynamic>();
                var ratingGrid = gradeRows
                    .Select(x => new AuditGradeDto(
                    x.GRADE_ID,
                    x.GRADE_CODE,
                    x.MIN_SCORE,
                    x.MAX_SCORE,
                    x.VALIDITY_IN_MONTHS,
                    x.VALIDATION_PERIOD,
                    x.COLOR_HEX,
                    x.DESCRIPTION,
                    x.IS_ACTIVE,
                    x.CREATED_BY,
                    x.CREATED_ON,
                    x.UPDATED_BY,
                    x.UPDATED_ON))
                .ToList();

                var gradeCode = hdr.GRADE_CODE != null ? (char?)((string)hdr.GRADE_CODE)[0] : null;

                return new AuditDetailResponse
                {
                    AuditId = (int)hdr.AUDIT_ID,
                    AuditCode = (string)hdr.AUDIT_CODE,
                    StatusId = (int)hdr.STATUS_ID,
                    StatusName = (string)hdr.STATUS_NAME,
                    CurrentStep = (int)hdr.CURRENT_STEP,
                    MaxUnlockedStep = (int)hdr.CURRENT_STEP,
                    AuditorEmpCode = (string)hdr.AUDITOR_EMP_CODE,
                    AuditorName = (string)hdr.AUDITOR_NAME,
                    AuditDate = (DateTime)hdr.AUDIT_DATE,
                    AuditReqId = (long)hdr.AUDIT_REQ_ID,
                    FactoryOwnerEmail = (string)hdr.FACTORY_OWNER_EMAIL,
                    OverallComments = (string)hdr.OVER_ALL_COMMENTS,
                    AuditScorePercent = (decimal?)hdr.AUDIT_SCORE_PERCENT,
                    GradeCode = gradeCode,
                    GradeColorHex = (string?)hdr.GRADECOLOR_HEX,
                    Description = (string)hdr.DESCRIPTION,
                    ValidationPeriod = (string?)hdr.VALIDATION_PERIOD,
                    ValidityInMonths = (int?)hdr.VALIDITY_IN_MONTHS,
                    ValidityTill = (DateTime?)hdr.VALIDITY_TILL,
                    IsBlacklistRisk = gradeCode == 'D',
                    CreatedBy = (string)hdr.CREATED_BY,
                    CreatedOn = (DateTime)hdr.CREATED_ON,
                    UpdatedBy = (string)hdr.UPDATED_BY,
                    UpdatedOn = (DateTime)hdr.UPDATED_ON,
                    SubmittedAt = (DateTime?)hdr.SUBMITTED_AT,
                    ProgressPercent = 0,
                    DocumentResponses = docs,
                    CheckPointResponses = cps,
                    ElementScores = elems,
                    AttachmentResponses = attaches,
                    RatingGridResponse  =ratingGrid
                };
            }
            catch (SqlException ex) when (ex.Number == 50075)
            {
                // Audit not found — return null gracefully
                throw new DataAccessException(
                    $"Unexpected error in GetDetailAsync: {ex.Message}", ex);
            }
            catch (SqlException ex)
            {
                throw new DataAccessException(
                    $"Database error in GetDetailAsync. SQL Error {ex.Number}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new DataAccessException(
                    $"Unexpected error in GetDetailAsync: {ex.Message}", ex);
            }
        }

        #endregion

    }

    public class AuditMasterRepository : IMasterRepository
    {
        private readonly DapperHelper _dapperHelper;
        public AuditMasterRepository(DapperHelper dapperHelper)
        {
            _dapperHelper = dapperHelper;
        }

        #region Query
        public async Task<IReadOnlyList<KeyElement>> GetKeyElementsAsync()
        {
            var rows = await _dapperHelper.ExecuteStoredProcedureListAsync<KeyElementWithCheckPointDto>("SP_FACTORY_AUDIT_MASTER_KEY_ELEMENT_GET_ALL");

            var dict = new Dictionary<int, KeyElement>();

            foreach (var row in rows)
            {
                if (!dict.TryGetValue(row.ELEMENT_ID, out var element))
                {
                    element = KeyElement.Reconstitute(
                        row.ELEMENT_ID,
                        row.ELEMENT_NO,
                        row.ELEMENT_NAME,
                        row.DISPLAY_ORDER,
                        row.IS_ACTIVE,
                        row.CREATED_BY,
                        row.CREATED_ON,
                        row.UPDATED_BY,
                        row.UPDATED_ON);

                    dict[row.ELEMENT_ID] = element;
                }

                if (row.CHECK_POINT_ID.HasValue)
                {
                    var cp = CheckPoint.Reconstitute(
                        row.CHECK_POINT_ID.Value,
                        row.ELEMENT_ID,
                        row.CHECK_POINT_NO!.Value,
                        row.QUESTION!,
                        row.HAVE_DROPDOWN!.Value,
                        row.IS_CRITICAL!.Value,
                        row.CP_ORDER!.Value,
                        row.CP_IS_ACTIVE!.Value,
                        row.CP_CB!,
                        row.CP_CO!.Value,
                        row.CP_UB!,
                        row.CP_UO!.Value);

                    element.AddCheckPoint(cp);
                }
            }

            return dict.Values.ToList();
        }

        public async Task<IReadOnlyList<DocumentChecklistItem>> GetDocumentChecklistAsync()
        {
            var rows = await _dapperHelper.ExecuteStoredProcedureListAsync<DocumentChecklistItemDto>("SP_FACTORY_AUDIT_MASTER_DOCUMENT_CHECKLIST_GET_ALL");

            return rows
                .Select(x => DocumentChecklistItem.Reconstitute(
                    x.ITEM_ID,
                    x.SR_NO,
                    x.DOCUMENT_NAME,
                    x.IS_MANDATORY,
                    x.IS_ACTIVE,
                    x.DISPLAY_ORDER,
                    x.CREATED_BY,
                    x.CREATED_ON,
                    x.UPDATED_BY,
                    x.UPDATED_ON))
                .ToList()
                .AsReadOnly();
        }

        public async Task<IReadOnlyList<AttachmentItem>> GetAttachmentItemsAsync()
        {
            var rows = await _dapperHelper.ExecuteStoredProcedureListAsync<AttachmentItemDto>("SP_FACTORY_AUDIT_MASTER_ATTACHMENT_ITEM_GET_ALL");

            return rows
                .Select(x => AttachmentItem.Reconstitute(
                    x.ITEM_ID,
                    x.SR_NO,
                    x.ATTACHMENT_NAME,
                    x.IS_MANDATORY,
                    x.IS_ACTIVE,
                    x.DISPLAY_ORDER,
                    x.CREATED_BY,
                    x.CREATED_ON,
                    x.UPDATED_BY,
                    x.UPDATED_ON))
                .ToList()
                .AsReadOnly();
        }


        public async Task<IReadOnlyList<AuditGrade>> GetGradesAsync()
        {
            var rows = await _dapperHelper.ExecuteStoredProcedureListAsync<AuditGradeDto>("SP_FACTORY_AUDIT_MASTER_GRADE_GET_ALL");

            return rows
                .Select(x => AuditGrade.Reconstitute(
                    x.GRADE_ID,
                    x.GRADE_CODE.FirstOrDefault(),
                    x.MIN_SCORE,
                    x.MAX_SCORE,
                    x.VALIDITY_IN_MONTHS,
                    x.VALIDATION_PERIOD,
                    x.COLOR_HEX,
                    x.DESCRIPTION,
                    x.IS_ACTIVE,
                    x.CREATED_BY,
                    x.CREATED_ON,
                    x.UPDATED_BY,
                    x.UPDATED_ON))
                .ToList()
                .AsReadOnly();
        }

        public async Task<IReadOnlyList<AuditStatus>> GetStatusesAsync()
        {
            var rows =
                await _dapperHelper.ExecuteStoredProcedureListAsync<AuditStatusDto>("SP_FACTORY_AUDIT_MASTER_STATUS_GET_ALL");

            return rows
                .Select(x => AuditStatus.Reconstitute(
                    x.STATUS_ID,
                    x.STATUS_NAME,
                    x.DISPLAY_ORDER,
                    x.IS_ACTIVE,
                    x.CREATED_BY,
                    x.CREATED_ON,
                    x.UPDATED_BY,
                    x.UPDATED_ON))
                .ToList()
                .AsReadOnly();
        }
        #endregion

    }
}


