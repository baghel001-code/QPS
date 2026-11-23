// ================================================================
// PUBLIC contract DTOs — consumed by:
//   • Blazor Server (Web project)
//   • ASP.NET Core API (Controllers / minimal API endpoints)
//
// Rules:
//   • All request DTOs carry [Required] / [MaxLength] attributes
//   • All response DTOs are sealed records (immutable)
//   • No domain types leak out (no ValueObjects / Entities here)
// ================================================================
using Domain.Entities.Audit;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

// ================================================================
//  ── REFERENCE DATA RESPONSES ──
// ================================================================

public sealed record KeyElementResponse(int ElementId, int ElementNo, string ElementName, int DisplayOrder, IReadOnlyList<CheckPointResponse> CheckPoints);

public sealed record CheckPointResponse(int CheckPointId, int ElementId, int CheckPointNo, string Question, bool HaveDropdown, bool IsCritical, int DisplayOrder);

public sealed record DocumentItemResponse(int ItemId, int SrNo, string DocumentName, bool IsMandatory, int DisplayOrder);

public sealed record AttachmentItemResponse(int ItemId, int SrNo, string AttachmentName, bool IsMandatory, int DisplayOrder);

public sealed record AuditGradeResponse(int GradeId, char GradeCode, decimal MinScore, decimal MaxScore, int ValidityInMonths, string ValidationPeriod, string ColorHex, string Description);

public sealed record AuditStatusResponse(int StatusId, string StatusName, int DisplayOrder);

public sealed record RatingResponse(int RatingId, int Score, string Label);

// ================================================================
//  ── AUDIT SUMMARY (LIST VIEW) ──
// ================================================================

public sealed record AuditSummaryResponse
{
    public int AuditId { get; init; }
    public string AuditCode { get; init; } = default!;
    public int StatusId { get; init; }
    public string StatusName { get; init; } = default!;
    public int CurrentStep { get; init; }
    public string AuditorEmpCode { get; init; } = default!;
    public DateTime AuditDate { get; init; }
    public long AuditReqId { get; init; }
    public decimal? AuditScorePercent { get; init; }
    public char? GradeCode { get; init; }
    public string? GradeColorHex { get; init; }
    public string? ValidationPeriod { get; init; }
    public DateTime? ValidityTill { get; init; }
    public DateTime CreatedOn { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public double ProgressPercent { get; init; }
}

// ================================================================
//  ── AUDIT DETAIL (FULL RESPONSE) ──
// ================================================================

public sealed record AuditDetailResponse
{
    public int AuditId { get; init; }
    public string AuditCode { get; init; } = default!;
    public int StatusId { get; init; }
    public string StatusName { get; init; } = default!;
    public int CurrentStep { get; init; }
    public int MaxUnlockedStep { get; init; }
    public double ProgressPercent { get; init; }

    // Step 1
    public string AuditorEmpCode { get; init; } = default!;
    public string AuditorName { get; init; } = default!;
    public DateTime AuditDate { get; init; }

    // Step 2
    public long AuditReqId { get; init; }
    public string FactoryOwnerEmail { get; init; } = default!;

    // Score
    public decimal? AuditScorePercent { get; init; }
    public int? GradeId { get; init; }
    public char? GradeCode { get; init; }
    public string? GradeColorHex { get; init; }
    public string? Description { get; init; }
    public string? ValidationPeriod { get; init; }
    public int? ValidityInMonths { get; init; }
    public DateTime? ValidityTill { get; init; }
    public bool IsBlacklistRisk { get; init; }

    // Comments
    public string? OverallComments { get; init; }

    // Audit trail
    public string CreatedBy { get; init; } = default!;
    public DateTime CreatedOn { get; init; }
    public string UpdatedBy { get; init; } = default!;
    public DateTime UpdatedOn { get; init; }
    public DateTime? SubmittedAt { get; init; }

    // Collections
    public IReadOnlyList<DocumentResponseDto> DocumentResponses { get; init; } = default!;
    public IReadOnlyList<CheckPointResponseDto> CheckPointResponses { get; init; } = default!;
    public IReadOnlyList<ElementScoreDto> ElementScores { get; init; } = default!;
    public IReadOnlyList<AttachmentResponseDto> AttachmentResponses { get; init; } = default!;
    public IReadOnlyList<AuditGradeDto> RatingGridResponse { get; init; } = default!;
}

public sealed record DocumentResponseDto(int ResponseId, int ItemId, int SrNo, string DocumentName, bool IsMandatory, string? IsAvailable, DateTime? ValidityDate, string? UnitInfo, string? Attachments, string? Remarks);

public sealed record CheckPointResponseDto(int ResponseId, int CheckPointId, int ElementId, int CheckPointNo, string Question, bool IsCritical, int? Score, string? ScoreLabel, string? Remarks);

public sealed record ElementScoreDto(int ScoreId, int ElementId, int ElementNo, string ElementName, int TotalCheckPoints, decimal TotalScore, decimal ElementAvgScore, int FinalRating, string RatingLabel);

public sealed record AttachmentResponseDto(int AttachmentId, int ItemId, int SrNo, string AttachmentName, bool IsMandatory, string? IsAvailable, string? Attachment, string? Remarks);

// ================================================================
//  ── STEP REQUEST DTOs (WEB + API INPUT) ──
// ================================================================

/// <summary>Step 1 — Start or update auditor info</summary>

public sealed record AuditBaseReq(int AuditId, string UpdatedBy);
public abstract class AuditRequestBase
{
    public int AuditId { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;
}
public sealed class StartAuditRequest : AuditRequestBase
{
    public long AuditReqId { get; set; }

    [Required(ErrorMessage = "Auditor employee code is required.")]
    [MaxLength(30, ErrorMessage = "Employee code max 30 characters.")]
    public string AuditorEmpCode { get; set; } = default!;

    [Required(ErrorMessage = "Audit date is required.")]
    public DateTime AuditDate { get; set; }

    public string? AuditorName { get; set; } = default!;
    public string AuditorNameWithEmpCode { get { return $"{AuditorName} [{AuditorEmpCode}]"; } }
}

/// <summary>Step 2 — Vendor / factory request info</summary>
public sealed class VendorInfoRequest : AuditRequestBase
{
    [Required(ErrorMessage = "Factory owner email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [MaxLength(150)]
    public string FactoryOwnerEmail { get; set; } = default!;

}
public sealed class VendorFactoryInfo
{
    public string VendorName { get; set; } = default!;
    public string FactoryAddress { get; set; } = default!;
    public string AdditionalUnit { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string BuyerName { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public string OwnerName { get; set; } = default!;
    public string OwnerContactNo { get; set; } = default!;
}

/// <summary>Step 3 — Document checklist row</summary>
public sealed class DocumentResponseRequest
{
    [Required]
    public int ItemId { get; set; }

    [MaxLength(1)]
    public string? IsAvailable { get; set; }   // "Y" or "N"

    public DateTime? ValidityDate { get; set; }

    [MaxLength(100)]
    public string? UnitInfo { get; set; }

    [MaxLength(500)]
    public string? Attachments { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }
}

public sealed class ListOfDocumentResponseRequest : AuditRequestBase
{
    public List<DocumentResponseRequest> documentResponseRequests { get; set; } = new();
}

/// <summary>Steps 4–13 — Checkpoint score row</summary>
public sealed class CheckPointScoreRequest
{
    [Required]
    public int CheckPointId { get; set; }

    /// <summary>Allowed: 0, 2, 4, 6, 8, 10</summary>
    public int? Score { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }
}

/// <summary>Batch of checkpoint scores for one element step</summary>
public sealed class ElementScoresRequest : AuditRequestBase
{
    [Required]
    public int ElementId { get; set; }

    [Required, MinLength(1, ErrorMessage = "At least one checkpoint score is required.")]
    public List<CheckPointScoreRequest> CheckPoints { get; set; } = new();
}

/// <summary>Step 14 — Photo attachment row</summary>
public sealed class AttachmentRequest
{
    [Required]
    public int ItemId { get; set; }

    //[Required(ErrorMessage = "Attachment file path/URL is required.")]
    [MaxLength(500)]
    public string AttachmentPath { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Remarks { get; set; } = string.Empty;
}
public sealed class ListOfAttachmentRequest : AuditRequestBase
{
    public List<AttachmentRequest> attachmentRequests { get; set; } = new();
}

/// <summary>Step 15 — Overall comments</summary>
public sealed class OverallCommentsRequest : AuditRequestBase
{
    [MaxLength(2000, ErrorMessage = "Comments cannot exceed 2000 characters.")]
    public string? OverallComments { get; set; }
}

/// <summary>Final submit — no body needed, just auditId in route</summary>
public sealed class SubmitAuditRequest
{
    // Intentionally empty — auditor identity comes from claims / session
}

// ================================================================
//  ── SCORE RESULT RESPONSE ──
// ================================================================

public sealed record ScoreResultResponse(int AuditId, string AuditCode, decimal ScorePercent, char GradeCode, string ColorHex, string Description, string ValidationPeriod, DateTime ValidityTill, bool IsBlacklistRisk);

// ================================================================
//  ── PAGED LIST ──
// ================================================================

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize, int TotalPages, bool HasNext, bool HasPrev);

// ================================================================
//  ── WIZARD STATE (Blazor Server in-memory state) ──
// ================================================================

/// <summary>
/// Held in a scoped Blazor service — tracks wizard progress.
/// Built from AuditDetailResponse after load/resume.
/// </summary>
public sealed class WizardStateDto
{
    public int AuditId { get; set; }
    public string AuditCode { get; set; } = string.Empty;
    public int CurrentStep { get; set; } = 1;
    public int MaxUnlockedStep { get; set; } = 1;
    public bool IsSubmitted { get; set; }
    public string StatusName { get; set; } = "Draft";

    // Step forms
    public VendorFactoryInfo VendorFactoryInfo { get; set; } = new();
    public StartAuditRequest Step1 { get; set; } = new();
    public VendorInfoRequest Step2 { get; set; } = new();
    public List<DocumentResponseRequest> Step3Docs { get; set; } = new();
    public List<ElementScoresRequest> ElementSteps { get; set; } = new();
    public List<AttachmentRequest> Step14Attachments { get; set; } = new();
    public OverallCommentsRequest Step15 { get; set; } = new();

    public IReadOnlyList<AuditGradeResponse>? RatingGridList { get; set; }

    // After submission
    public ScoreResultResponse? ScoreResult { get; set; }

    public const int TotalSteps = 16;

    public bool IsStepComplete(int step) => step < CurrentStep;
    public bool IsStepUnlocked(int step) => step <= MaxUnlockedStep;
    public bool IsCurrentStep(int step) => step == CurrentStep;

    public double ProgressPercent => Math.Round((Math.Max(0, CurrentStep - 1) / (double)(TotalSteps - 1)) * 100, 1);

    public static string GetStepLabel(int step) => step switch
    {
        1 => "Auditor Info",
        2 => "Vendor Info",
        3 => "Document Checklist",
        4 => "Raw Materials",
        5 => "Product Dev & Sampling",
        6 => "Cutting",
        7 => "Sewing",
        8 => "Finishing",
        9 => "Packing",
        10 => "Lab Testing",
        11 => "QA Management",
        12 => "QC System",
        13 => "Min. Compliances",
        14 => "Photo Attachments",
        15 => "Comments",
        16 => "Review & Submit",
        _ => $"Step {step}"
    };

    public static string GetStepIcon(int step) => step switch
    {
        1 => "👤",
        2 => "🏭",
        3 => "📋",
        4 => "🧵",
        5 => "✏️",
        6 => "✂️",
        7 => "🪡",
        8 => "🔥",
        9 => "📦",
        10 => "🔬",
        11 => "📊",
        12 => "✅",
        13 => "⚖️",
        14 => "📸",
        15 => "💬",
        16 => "🚀",
        _ => "📝"
    };

    public static string GetStepGroup(int step) => step switch
    {
        1 or 2 => "Basic Info",
        3 => "Documents",
        >= 4 and <= 13 => "Evaluation",
        14 => "Attachments",
        15 or 16 => "Submit",
        _ => "Other"
    };

    // Maps element number (1–10) to wizard step number (4–13)
    public static int ElementToWizardStep(int elementNo) => elementNo + 3;
    public static int WizardStepToElement(int wizardStep) => wizardStep - 3;

    // ── Preview Score (before submission) ─────────────────────

    public ScoreResultResponse? ComputePreviewScore()
    {
        if (RatingGridList == null || !RatingGridList.Any())
            return null;

        if (ElementSteps == null || !ElementSteps.Any())
            return null;

        var elementScores = ElementSteps
             .Select(e =>
             {
                 var scoredPoints = e.CheckPoints.ToList();// e.CheckPoints.Where(c => c.Score > 0).ToList();
                 if (!scoredPoints.Any()) return 0;
                 var avg = (decimal)scoredPoints.Average(c => c.Score);
                 return ScoringCalculator.ComputeElementRating(avg);
             })
             .ToList();

        if (!elementScores.Any()) return null;

        var scorePercent = ScoringCalculator.ComputeScorePercent(elementScores);

        var grade = RatingGridList.FirstOrDefault(g => scorePercent >= g.MinScore && scorePercent <= g.MaxScore);
        if (grade == null) return null;

        return new ScoreResultResponse(
            AuditId: AuditId,
            AuditCode: AuditCode,
            ScorePercent: scorePercent,
            GradeCode: grade.GradeCode,
            ColorHex: grade.ColorHex,
            Description: grade.Description,
            ValidationPeriod: grade.ValidationPeriod,
            ValidityTill: DateTime.Now.AddMonths(grade.ValidityInMonths),
            IsBlacklistRisk: scorePercent <= 20
        );

        

        
    }
}

// ================================================================
//  ── API ENVELOPE ──
// ================================================================

public sealed class ApiResult<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public T? Data { get; init; }
    public string? ErrorCode { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    public static ApiResult<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResult<T> Fail(string error, string? code = null) =>
        new() { Success = false, Errors = new[] { error }, ErrorCode = code };

    public static ApiResult<T> ValidationFail(IEnumerable<string> errors) =>
        new() { Success = false, Errors = errors.ToArray(), ErrorCode = "VALIDATION_ERROR" };
}