// ================================================================
// FactoryAudit.Application / Services / AuditWizardService.cs
// Orchestrates domain logic and repository calls.
// All domain exceptions mapped to ApiResult.
// ================================================================
using Application.DTOs;
using Application.Interfaces.V1;
using Domain.Entities.Audit;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Infrastructure.Services.V1;

public sealed class AuditWizardService : IAuditWizardService
{
    private readonly IAuditRepository _auditRepo;
    private readonly IMasterRepository _masterRepo;

    public AuditWizardService(IAuditRepository auditRepo, IMasterRepository masterRepo)
    {
        _auditRepo = auditRepo;
        _masterRepo = masterRepo;
    }

    // ── Start Audit (Step 1 — creates draft) ─────────────
    public async Task<ApiResult<AuditSummaryResponse>> StartAuditAsync(StartAuditRequest req)
    {
        try
        {
            var code = await _auditRepo.GenerateAuditCodeAsync();
            var audit = AuditReport.Create(req.AuditReqId, AuditCode.From(code), req.AuditorEmpCode, req.AuditDate, req.UpdatedBy);

            var id = await _auditRepo.InsertAsync(audit);
            // Reload to get DB-assigned ID
            var saved = await _auditRepo.GetByIdAsync(id);
            return ApiResult<AuditSummaryResponse>.Ok(ToSummary(saved!));
        }
        catch (DomainException ex) { return ApiResult<AuditSummaryResponse>.Fail(ex.Message, ex.Code); }
        catch (Exception ex) { return ApiResult<AuditSummaryResponse>.Fail(ex.Message); }
    }

    // ── Save Step 1 (update auditor info) ─────────────────

    public async Task<ApiResult<AuditSummaryResponse>> SaveStep1Async(StartAuditRequest req)
    {
        try
        {
            var audit = await GetOrThrowAsync(req.AuditId);
            audit.UpdateAuditorInfo(req.AuditReqId, req.AuditorEmpCode, req.AuditDate, req.UpdatedBy);
            await _auditRepo.UpdateAsync(audit);
            return ApiResult<AuditSummaryResponse>.Ok(ToSummary(audit));
        }
        catch (DomainException ex)
        {
            return ApiResult<AuditSummaryResponse>.Fail(ex.Message, ex.Code);
        }
    }

    // ── Save Step 2 (vendor info) ─────────────────────────

    public async Task<ApiResult<AuditSummaryResponse>> SaveStep2Async(VendorInfoRequest req)
    {
        try
        {
            var audit = await GetOrThrowAsync(req.AuditId);
            audit.UpdateVendorInfo(req.FactoryOwnerEmail, req.UpdatedBy);
            await _auditRepo.UpdateAsync(audit);
            return ApiResult<AuditSummaryResponse>.Ok(ToSummary(audit));
        }
        catch (DomainException ex) { return ApiResult<AuditSummaryResponse>.Fail(ex.Message, ex.Code); }
    }

    // ── Save Step 3 (document checklist) ──────────────────

    public async Task<ApiResult<AuditSummaryResponse>> SaveStep3Async(ListOfDocumentResponseRequest req)
    {
        try
        {
            var audit = await GetOrThrowAsync(req.AuditId);

            foreach (var r in req.documentResponseRequests)
            {
                var avail = DocumentAvailabilityValue.From(r.IsAvailable);
                audit.UpsertDocumentResponse(r.ItemId, avail, r.ValidityDate,
                    r.UnitInfo, r.Attachments, r.Remarks, req.UpdatedBy);
            }

            audit.CompleteDocumentStep(req.UpdatedBy);
            await _auditRepo.UpdateAsync(audit);
            return ApiResult<AuditSummaryResponse>.Ok(ToSummary(audit));
        }
        catch (DomainException ex) { return ApiResult<AuditSummaryResponse>.Fail(ex.Message, ex.Code); }
    }

    // ── Save Element Step (Steps 4–13) ────────────────────

    public async Task<ApiResult<AuditSummaryResponse>> SaveElementStepAsync(ElementScoresRequest req)
    {
        try
        {
            var audit = await GetOrThrowAsync(req.AuditId);
            var keyElems = await _masterRepo.GetKeyElementsAsync();
            var element = keyElems.FirstOrDefault(e => e.ElementId == req.ElementId);
            if (element is null)
                return ApiResult<AuditSummaryResponse>.Fail("Element not found.");

            foreach (var score in req.CheckPoints)
            {
                var cpDef = element.CheckPoints.FirstOrDefault(c => c.CheckPointId == score.CheckPointId);
                if (cpDef is null) continue;

                var scoreVal = CheckPointScoreValue.From(score.Score ?? 0);
                audit.UpsertCheckPointResponse(score.CheckPointId, req.ElementId, scoreVal, cpDef, score.Remarks, req.UpdatedBy);
            }

            audit.CompleteElementStep(req.ElementId, req.UpdatedBy);
            await _auditRepo.UpdateAsync(audit);
            return ApiResult<AuditSummaryResponse>.Ok(ToSummary(audit));
        }
        catch (DomainException ex) { return ApiResult<AuditSummaryResponse>.Fail(ex.Message, ex.Code); }
    }

    // ── Save Attachments (Step 14) ────────────────────────

    public async Task<ApiResult<AuditSummaryResponse>> SaveAttachmentsAsync(ListOfAttachmentRequest req)
    {
        try
        {
            var audit = await GetOrThrowAsync(req.AuditId);
            foreach (var a in req.attachmentRequests)
                audit.UpsertAttachmentResponse(a.ItemId, a.AttachmentPath, a.Remarks, req.UpdatedBy);
            audit.CompleteAttachmentStep(req.UpdatedBy);
            await _auditRepo.UpdateAsync(audit);
            return ApiResult<AuditSummaryResponse>.Ok(ToSummary(audit));
        }
        catch (DomainException ex) { return ApiResult<AuditSummaryResponse>.Fail(ex.Message, ex.Code); }
    }

    // ── Save Comments (Step 15) ───────────────────────────

    public async Task<ApiResult<AuditSummaryResponse>> SaveCommentsAsync(OverallCommentsRequest req)
    {
        try
        {
            var audit = await GetOrThrowAsync(req.AuditId);
            audit.SaveOverallComments(req.OverallComments, req.UpdatedBy);
            await _auditRepo.UpdateAsync(audit);
            return ApiResult<AuditSummaryResponse>.Ok(ToSummary(audit));
        }
        catch (DomainException ex) { return ApiResult<AuditSummaryResponse>.Fail(ex.Message, ex.Code); }
    }

    // ── Submit (Step 16) ──────────────────────────────────

    public async Task<ApiResult<ScoreResultResponse>> SubmitAuditAsync(AuditBaseReq obj)
    {
        try
        {

            var audit = await GetOrThrowAsync(obj.AuditId);

            var grades = await _masterRepo.GetGradesAsync();

            var result = audit.Submit(grades, obj.UpdatedBy);
            await _auditRepo.UpdateAsync(audit);

            return ApiResult<ScoreResultResponse>.Ok(new ScoreResultResponse(
                obj.AuditId, audit.AuditCode.Value, result.ScorePercent, result.GradeCode, result.ColorHex, result.Description, result.ValidationPeriod, 
                result.ValidityTill, result.IsBlacklistRisk),
                $"Audit submitted successfully. Grade: {result.GradeCode}");
        }
        catch (AuditIncompleteForSubmissionException ex) { return ApiResult<ScoreResultResponse>.Fail(ex.Message, ex.Code); }
        catch (DomainException ex) { return ApiResult<ScoreResultResponse>.Fail(ex.Message, ex.Code); }
    }

    // ── Queries ───────────────────────────────────────────

    public async Task<ApiResult<AuditDetailResponse>> GetDetailAsync(int auditId)
    {
        var detail = await _auditRepo.GetDetailAsync(auditId);
        return detail is null
            ? ApiResult<AuditDetailResponse>.Fail("Audit not found.", "AUDIT_NOT_FOUND")
            : ApiResult<AuditDetailResponse>.Ok(detail);
    }

    public async Task<ApiResult<PagedResponse<AuditSummaryResponse>>> GetListAsync(AuditListFilter filter)
    {
        var paged = await _auditRepo.GetPagedAsync(filter);
        var items = paged.Items.Select(ToSummaryFromRecord).ToList();
        var resp = new PagedResponse<AuditSummaryResponse>(
            items, paged.TotalCount, paged.Page, paged.PageSize,
            paged.TotalPages, paged.HasNext, paged.HasPrev);
        return ApiResult<PagedResponse<AuditSummaryResponse>>.Ok(resp);
    }

    // ── Helpers ───────────────────────────────────────────

    private async Task<AuditReport> GetOrThrowAsync(int auditId)
    {
        var audit = await _auditRepo.GetByIdAsync(auditId);
        if (audit is null) throw new AuditNotFoundException(auditId);
        return audit;
    }

    private static AuditSummaryResponse ToSummary(AuditReport a) => new()
    {
        AuditId = a.AuditId,
        AuditCode = a.AuditCode.Value,
        StatusId = (int)a.Status,
        StatusName = a.Status.ToString(),
        CurrentStep = a.CurrentStep,
        AuditorEmpCode = a.AuditorEmpCode,
        AuditDate = a.AuditDate,
        AuditReqId = a.AuditReqId,
        AuditScorePercent = a.AuditScorePercent?.Value,
        GradeCode = a.GradeCode,
        ValidityTill = a.ValidityTill,
        CreatedOn = a.CreatedOn,
        SubmittedAt = a.SubmittedAt,
        ProgressPercent = a.ProgressPercent
    };

    private static AuditSummaryResponse ToSummaryFromRecord(AuditHeaderRecord r) => new()
    {
        AuditId = r.AuditId,
        AuditCode = r.AuditCode,
        StatusId = r.StatusId,
        StatusName = r.StatusName,
        CurrentStep = r.CurrentStep,
        AuditorEmpCode = r.AuditorEmpCode,
        AuditDate = r.AuditDate,
        AuditReqId = r.AuditReqId,
        AuditScorePercent = r.AuditScorePercent,
        GradeCode = r.GradeCode,
        GradeColorHex = r.GradeColorHex,
        ValidationPeriod = r.ValidationPeriod,
        ValidityTill = r.ValidityTill,
        CreatedOn = r.CreatedOn,
        SubmittedAt = r.SubmittedAt,
        ProgressPercent = (r.CurrentStep / (double)WizardStateDto.TotalSteps) * 100
    };
}

// ── Master data service ───────────────────────────────────────

public sealed class MasterDataService : IMasterDataService
{
    private readonly IMasterRepository _repo;
    public MasterDataService(IMasterRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<KeyElementResponse>> GetKeyElementsAsync() =>
        (await _repo.GetKeyElementsAsync())
            .Select(e => new KeyElementResponse(e.ElementId, e.ElementNo, e.ElementName,
                e.DisplayOrder,
                e.ActiveCheckPoints.Select(c => new CheckPointResponse(c.CheckPointId,
                    c.ElementId, c.CheckPointNo, c.Question, c.HaveDropdown,
                    c.IsCritical, c.DisplayOrder)).ToList()))
            .ToList().AsReadOnly();

    public async Task<IReadOnlyList<DocumentItemResponse>> GetDocumentChecklistAsync() =>
        (await _repo.GetDocumentChecklistAsync())
            .Select(d => new DocumentItemResponse(d.ItemId, d.SrNo, d.DocumentName,
                d.IsMandatory, d.DisplayOrder))
            .ToList().AsReadOnly();

    public async Task<IReadOnlyList<AttachmentItemResponse>> GetAttachmentItemsAsync() =>
        (await _repo.GetAttachmentItemsAsync())
            .Select(a => new AttachmentItemResponse(a.ItemId, a.SrNo, a.AttachmentName,
                a.IsMandatory, a.DisplayOrder))
            .ToList().AsReadOnly();

    public async Task<IReadOnlyList<AuditGradeResponse>> GetGradesAsync() =>
        (await _repo.GetGradesAsync())
            .Select(g => new AuditGradeResponse(g.GradeId, g.GradeCode, g.MinScore,
                g.MaxScore, g.ValidityInMonths, g.ValidationPeriod, g.ColorHex, g.Description))
            .ToList().AsReadOnly();

    public async Task<IReadOnlyList<AuditStatusResponse>> GetStatusesAsync() =>
        (await _repo.GetStatusesAsync())
            .Select(s => new AuditStatusResponse(s.StatusId, s.StatusName, s.DisplayOrder))
            .ToList().AsReadOnly();

    public Task<IReadOnlyList<RatingResponse>> GetRatingsAsync() =>
        Task.FromResult<IReadOnlyList<RatingResponse>>(new List<RatingResponse>
        {
            new(1, 0,  "N/A"),
            new(2, 2,  "Poor"),
            new(3, 4,  "Below Average"),
            new(4, 6,  "Average"),
            new(5, 8,  "Good"),
            new(6, 10, "Excellent")
        }.AsReadOnly());
}
