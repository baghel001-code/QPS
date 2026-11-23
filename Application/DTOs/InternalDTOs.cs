// ================================================================
// Application / DTOs / InternalDTOs.cs
//
// Used ONLY inside Application layer and Infrastructure layer.
// Never exposed to Web or API controllers directly.
// ================================================================
namespace Application.DTOs;

// ── Audit Core ────────────────────────────────────────────────
public sealed record AuditReportHeaderDto(
    int AUDIT_ID,
    string AUDIT_CODE,
    int STATUS_ID,
    int CURRENT_STEP,
    int MAX_UNLOCKED_STEP,
    string AUDITOR_EMP_CODE,
    DateTime AUDIT_DATE,
    long AUDIT_REQ_ID,
    string FACTORY_OWNER_EMAIL,
    decimal? AUDIT_SCORE_PERCENT,
    int? GRADE_ID,
    string? GRADE_CODE,
    string? GRADECOLOR_HEX,
    string? VALIDATION_PERIOD,
    int VALIDITY_IN_MONTHS,
    DateTime? VALIDITY_TILL,
    string? OVERALLCOMMENTS,
    bool IS_DELETED,
    string CREATED_BY,
    DateTime CREATED_ON,
    string UPDATED_BY,
    DateTime UPDATED_ON,
    DateTime? SUBMITTED_AT);
//		
public sealed record CheckPointElementMapDto(
    int CHECK_POINT_ID,
    int ELEMENT_ID);

public sealed record AuditCheckPointDto
{
    public int RESPONSE_ID { get; init; }
    public int AUDIT_ID { get; init; }
    public int CHECK_POINT_ID { get; init; }
    public int SCORE { get; init; }
    public string? REMARKS { get; init; }
    public string CREATED_BY { get; init; }
    public DateTime CREATED_ON { get; init; }
    public string UPDATED_BY { get; init; }
    public DateTime UPDATED_ON { get; init; }



}


public sealed record KeyElementWithCheckPointDto(
    int ELEMENT_ID,
    int ELEMENT_NO,
    string ELEMENT_NAME,
    int DISPLAY_ORDER,
    bool IS_ACTIVE,
    string CREATED_BY,
    DateTime CREATED_ON,
    string UPDATED_BY,
    DateTime UPDATED_ON,
    int? CHECK_POINT_ID,
    int? CP_ELEMENT_ID,
    int? CHECK_POINT_NO,
    string? QUESTION,
    bool? HAVE_DROPDOWN,
    bool? IS_CRITICAL,
    int? CP_ORDER,
    bool? CP_IS_ACTIVE,
    string? CP_CB,
    DateTime? CP_CO,
    string? CP_UB,
    DateTime? CP_UO);

public sealed record DocumentChecklistItemDto(
    int ITEM_ID,
    int SR_NO,
    string DOCUMENT_NAME,
    bool IS_MANDATORY,
    bool IS_ACTIVE,
    int DISPLAY_ORDER,
    string CREATED_BY,
    DateTime CREATED_ON,
    string UPDATED_BY,
    DateTime UPDATED_ON);

public sealed record AuditStatusDto(
    int STATUS_ID,
    string STATUS_NAME,
    int DISPLAY_ORDER,
    bool IS_ACTIVE,
    string CREATED_BY,
    DateTime CREATED_ON,
    string UPDATED_BY,
    DateTime UPDATED_ON);

public sealed record AuditGradeDto(
    int GRADE_ID,
    string GRADE_CODE,
    decimal MIN_SCORE,
    decimal MAX_SCORE,
    int VALIDITY_IN_MONTHS,
    string VALIDATION_PERIOD,
    string COLOR_HEX,
    string DESCRIPTION,
    bool IS_ACTIVE,
    string CREATED_BY,
    DateTime CREATED_ON,
    string UPDATED_BY,
    DateTime UPDATED_ON);

public sealed record AttachmentItemDto(
    int ITEM_ID,
    int SR_NO,
    string ATTACHMENT_NAME,
    bool IS_MANDATORY,
    bool IS_ACTIVE,
    int DISPLAY_ORDER,
    string CREATED_BY,
    DateTime CREATED_ON,
    string UPDATED_BY,
    DateTime UPDATED_ON);

/// <summary>Flat read model from VW_AUDITSUMMARY or AUDIT_REPORT join.</summary>
public sealed record AuditHeaderRecord
{
    public int AuditId { get; init; }
    public string AuditCode { get; init; } = default!;
    public int StatusId { get; init; }
    public string StatusName { get; init; } = default!;
    public int CurrentStep { get; init; }
    public int MaxUnlockedStep { get; init; }
    public string AuditorEmpCode { get; init; } = default!;
    public DateTime AuditDate { get; init; }
    public long AuditReqId { get; init; }
    public string FactoryOwnerEmail { get; init; } = default!;
    public decimal? AuditScorePercent { get; init; }
    public int? GradeId { get; init; }
    public char? GradeCode { get; init; }
    public string? GradeColorHex { get; init; }
    public string? ValidationPeriod { get; init; }
    public int? ValidityInMonths { get; init; }
    public DateTime? ValidityTill { get; init; }
    public string? OverallComments { get; init; }
    public bool IsDeleted { get; init; }
    public string CreatedBy { get; init; } = default!;
    public DateTime CreatedOn { get; init; }
    public string UpdatedBy { get; init; } = default!;
    public DateTime UpdatedOn { get; init; }
    public DateTime? SubmittedAt { get; init; }
}

/// <summary>Flat row from MASTER_AUDIT_KEY_ELEMENT + MASTER_AUDIT_CHECK_POINT join.</summary>
public sealed record KeyElementWithCheckPointsRecord
{
    // Element columns
    public int ElementId { get; init; }
    public int ElementNo { get; init; }
    public string ElementName { get; init; } = default!;
    public int ElemOrder { get; init; }
    public bool ElemIsActive { get; init; }
    // CheckPoint columns
    public int CheckPointId { get; init; }
    public int CheckPointNo { get; init; }
    public string Question { get; init; } = default!;
    public bool HaveDropdown { get; init; }
    public bool IsCritical { get; init; }
    public int CpOrder { get; init; }
    public bool CpIsActive { get; init; }
}

public sealed record DocumentChecklistItemRecord
{
    public int ItemId { get; init; }
    public int SrNo { get; init; }
    public string DocumentName { get; init; } = default!;
    public bool IsMandatory { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed record AttachmentItemRecord
{
    public int ItemId { get; init; }
    public int SrNo { get; init; }
    public string AttachmentName { get; init; } = default!;
    public bool IsMandatory { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
}

public sealed record AuditGradeRecord
{
    public int GradeId { get; init; }
    public char GradeCode { get; init; }
    public decimal MinScore { get; init; }
    public decimal MaxScore { get; init; }
    public int ValidityInMonths { get; init; }
    public string ValidationPeriod { get; init; } = default!;
    public string ColorHex { get; init; } = default!;
    public string Description { get; init; } = default!;
}

// ── Step-save commands (Application layer input) ──────────────

public sealed record SaveStep1Command(int AuditId, string AuditorEmpCode, DateTime AuditDate, string UpdatedBy);

public sealed record SaveStep2Command(int AuditId, long AuditReqId, string FactoryOwnerEmail, string UpdatedBy);

public sealed record DocumentResponseInput(int ItemId, string? IsAvailable, DateTime? ValidityDate, string? UnitInfo, string? Attachments, string? Remarks);

public sealed record SaveStep3Command(int AuditId, IEnumerable<DocumentResponseInput> Responses, string UpdatedBy);

public sealed record CheckPointScoreInput(int CheckPointId, int? Score, string? Remarks);

public sealed record SaveElementStepCommand(int AuditId, int ElementId, IEnumerable<CheckPointScoreInput> Scores, string UpdatedBy);

public sealed record AttachmentResponseInput(int ItemId, string AttachmentPath, string? Remarks);

public sealed record SaveAttachmentsCommand(int AuditId, IEnumerable<AttachmentResponseInput> Attachments, string UpdatedBy);

public sealed record SaveCommentsCommand(int AuditId, string? Comments, string UpdatedBy);

public sealed record SubmitAuditCommand(int AuditId, string SubmittedBy);

// ── Response read models (Application layer output) ───────────

public sealed record AuditDocumentResponseRecord
{
    public int ResponseId { get; init; }
    public int ItemId { get; init; }
    public int SrNo { get; init; }
    public string DocumentName { get; init; } = default!;
    public bool IsMandatory { get; init; }
    public string? IsAvailable { get; init; }
    public DateTime? ValidityDate { get; init; }
    public string? UnitInfo { get; init; }
    public string? Attachments { get; init; }
    public string? Remarks { get; init; }
}

public sealed record AuditCheckPointResponseRecord
{
    public int ResponseId { get; init; }
    public int CheckPointId { get; init; }
    public int ElementId { get; init; }
    public int CheckPointNo { get; init; }
    public string Question { get; init; } = default!;
    public bool IsCritical { get; init; }
    public int? Score { get; init; }
    public string? Remarks { get; init; }
}

public sealed record AuditElementScoreRecord
{
    public int ScoreId { get; init; }
    public int ElementId { get; init; }
    public int ElementNo { get; init; }
    public string ElementName { get; init; } = default!;
    public int TotalCheckPoints { get; init; }
    public decimal TotalScore { get; init; }
    public decimal ElementAvgScore { get; init; }
    public int FinalRating { get; init; }
}

public sealed record ScoreCalculationResult(
    decimal ScorePercent,
    int GradeId,
    char GradeCode,
    string ColorHex,
    string ValidationPeriod,
    DateTime ValidityTill,
    bool IsBlacklistRisk);

// ── Pagination ────────────────────────────────────────────────

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}

public sealed record AuditListFilter(string? SearchTerm = null, int? StatusId = null, DateTime? FromDate = null, DateTime? ToDate = null, string? AuditorEmpCode = null, int Page = 1, int PageSize = 20);