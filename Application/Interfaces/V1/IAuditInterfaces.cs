// ================================================================
// Application / Interfaces / IServices.cs
// Contracts for application services and repositories
// ================================================================
using Application.DTOs;
using Domain.Entities.Audit;


namespace Application.Interfaces.V1;

// ── Implement in Infrastructure ───────────────────────────
public interface IAuditRepository
{
    Task<AuditReport?> GetByIdAsync(int auditId);
    Task<AuditReport?> GetByCodeAsync(string auditCode);
    Task<int> InsertAsync(AuditReport audit);
    Task UpdateAsync(AuditReport audit);
    Task<string> GenerateAuditCodeAsync();
    Task<PagedResult<AuditHeaderRecord>> GetPagedAsync(AuditListFilter filter);
    Task<AuditDetailResponse?> GetDetailAsync(int auditId);
}

public interface IMasterRepository
{
    Task<IReadOnlyList<KeyElement>> GetKeyElementsAsync();
    Task<IReadOnlyList<DocumentChecklistItem>> GetDocumentChecklistAsync();
    Task<IReadOnlyList<AttachmentItem>> GetAttachmentItemsAsync();
    Task<IReadOnlyList<AuditGrade>> GetGradesAsync();
    Task<IReadOnlyList<AuditStatus>> GetStatusesAsync();
}

// ── Application service interfaces ───────────────────────────

public interface IAuditWizardService
{
    // Wizard commands
    Task<ApiResult<AuditSummaryResponse>> StartAuditAsync(StartAuditRequest req);
    Task<ApiResult<AuditSummaryResponse>> SaveStep1Async(StartAuditRequest req);
    Task<ApiResult<AuditSummaryResponse>> SaveStep2Async(VendorInfoRequest req);
    Task<ApiResult<AuditSummaryResponse>> SaveStep3Async(ListOfDocumentResponseRequest req);
    Task<ApiResult<AuditSummaryResponse>> SaveElementStepAsync(ElementScoresRequest req);
    Task<ApiResult<AuditSummaryResponse>> SaveAttachmentsAsync( ListOfAttachmentRequest req);
    Task<ApiResult<AuditSummaryResponse>> SaveCommentsAsync( OverallCommentsRequest req);
    Task<ApiResult<ScoreResultResponse>> SubmitAuditAsync(AuditBaseReq obj);

    // Queries
    Task<ApiResult<AuditDetailResponse>> GetDetailAsync(int auditId);
    Task<ApiResult<PagedResponse<AuditSummaryResponse>>> GetListAsync(AuditListFilter filter);
}

public interface IMasterDataService
{
    Task<IReadOnlyList<KeyElementResponse>> GetKeyElementsAsync();
    Task<IReadOnlyList<DocumentItemResponse>> GetDocumentChecklistAsync();
    Task<IReadOnlyList<AttachmentItemResponse>> GetAttachmentItemsAsync();
    Task<IReadOnlyList<AuditGradeResponse>> GetGradesAsync();
    Task<IReadOnlyList<AuditStatusResponse>> GetStatusesAsync();
    Task<IReadOnlyList<RatingResponse>> GetRatingsAsync();
}



