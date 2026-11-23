// ================================================================
// All child entities owned by the AuditReport aggregate root.
// Private setters — mutations only via methods.
// ================================================================
using Domain.ValueObjects;

namespace Domain.Entities.Audit;

// ── AuditDocumentResponse ────────────────────────────────────

public sealed class AuditDocumentResponse
{
    public int ResponseId { get; private set; }
    public int AuditId { get; private set; }
    public int ItemId { get; private set; }
    public DocumentAvailabilityValue IsAvailable { get; private set; } = DocumentAvailabilityValue.NotSet;
    public DateTime? ValidityDate { get; private set; }
    public string? UnitInfo { get; private set; }
    public string? Attachments { get; private set; }
    public string? Remarks { get; private set; }
    public string CreatedBy { get; private set; } = default!;
    public DateTime CreatedOn { get; private set; }
    public string UpdatedBy { get; private set; } = default!;
    public DateTime UpdatedOn { get; private set; }

    // Navigation — loaded after reconstitution
    public DocumentChecklistItem? Item { get; private set; }

    private AuditDocumentResponse() { }

    public static AuditDocumentResponse Create(int auditId, int itemId, DocumentAvailabilityValue availability, DateTime? validityDate, string? unitInfo, string? attachments, string? remarks, string createdBy) => new()
    {
        AuditId = auditId,
        ItemId = itemId,
        IsAvailable = availability,
        ValidityDate = validityDate,
        UnitInfo = unitInfo?.Trim(),
        Attachments = attachments?.Trim(),
        Remarks = remarks?.Trim(),
        CreatedBy = createdBy,
        CreatedOn = DateTime.UtcNow,
        UpdatedBy = createdBy,
        UpdatedOn = DateTime.UtcNow
    };

    public static AuditDocumentResponse Reconstitute(int responseId, int auditId, int itemId, string? isAvailable, DateTime? validityDate, string? unitInfo, string? attachments, string? remarks, string createdBy, DateTime createdOn, string updatedBy, DateTime updatedOn) => new()
    {
        ResponseId = responseId,
        AuditId = auditId,
        ItemId = itemId,
        IsAvailable = DocumentAvailabilityValue.From(isAvailable),
        ValidityDate = validityDate,
        UnitInfo = unitInfo,
        Attachments = attachments,
        Remarks = remarks,
        CreatedBy = createdBy,
        CreatedOn = createdOn,
        UpdatedBy = updatedBy,
        UpdatedOn = updatedOn
    };

    internal void Update(DocumentAvailabilityValue availability, DateTime? validityDate, string? unitInfo, string? attachments, string? remarks, string updatedBy)
    {
        IsAvailable = availability;
        ValidityDate = validityDate;
        UnitInfo = unitInfo?.Trim();
        Attachments = attachments?.Trim();
        Remarks = remarks?.Trim();
        UpdatedBy = updatedBy;
        UpdatedOn = DateTime.UtcNow;
    }

    public void SetItem(DocumentChecklistItem item) => Item = item;
    public bool IsSet => IsAvailable.IsSet;
}

// ── AuditCheckPointResponse ──────────────────────────────────

public sealed class AuditCheckPointResponse
{
    public int ResponseId { get; private set; }
    public int AuditId { get; private set; }
    public int CheckPointId { get; private set; }
    public CheckPointScoreValue Score { get; private set; } = CheckPointScoreValue.NotApplicable;
    public string? Remarks { get; private set; }
    public string CreatedBy { get; private set; } = default!;
    public DateTime CreatedOn { get; private set; }
    public string UpdatedBy { get; private set; } = default!;
    public DateTime UpdatedOn { get; private set; }

    // Navigation
    public CheckPoint? CheckPointDef { get; private set; }

    private AuditCheckPointResponse() { }

    public static AuditCheckPointResponse Create(int auditId, int checkPointId, CheckPointScoreValue score, string? remarks, string createdBy) => new()
    {
        AuditId = auditId,
        CheckPointId = checkPointId,
        Score = score,
        Remarks = remarks?.Trim(),
        CreatedBy = createdBy,
        CreatedOn = DateTime.UtcNow,
        UpdatedBy = createdBy,
        UpdatedOn = DateTime.UtcNow
    };

    public static AuditCheckPointResponse Reconstitute(int responseId, int auditId, int checkPointId, int? score, string? remarks, string createdBy, DateTime createdOn, string updatedBy, DateTime updatedOn) => new()
    {
        ResponseId = responseId,
        AuditId = auditId,
        CheckPointId = checkPointId,
        Score = score.HasValue ? CheckPointScoreValue.From(score.Value) : CheckPointScoreValue.NotApplicable,
        Remarks = remarks,
        CreatedBy = createdBy,
        CreatedOn = createdOn,
        UpdatedBy = updatedBy,
        UpdatedOn = updatedOn
    };

    internal void UpdateScore(CheckPointScoreValue score, string? remarks, string updatedBy)
    {
        Score = score;
        Remarks = remarks?.Trim();
        UpdatedBy = updatedBy;
        UpdatedOn = DateTime.UtcNow;
    }

    public void SetCheckPointDef(CheckPoint cp) => CheckPointDef = cp;
}

// ── AuditElementScore ────────────────────────────────────────

public sealed class AuditElementScore
{
    public int ScoreId { get; private set; }
    public int AuditId { get; private set; }
    public int ElementId { get; private set; }
    public int TotalCheckPoints { get; private set; }
    public decimal TotalScore { get; private set; }
    public decimal ElementAvgScore { get; private set; }
    public int FinalRating { get; private set; }
    public string CreatedBy { get; private set; } = default!;
    public DateTime CreatedOn { get; private set; }
    public string UpdatedBy { get; private set; } = default!;
    public DateTime UpdatedOn { get; private set; }

    // Navigation
    public KeyElement? Element { get; private set; }

    private AuditElementScore() { }

    public static AuditElementScore Create(int auditId, int elementId, int totalCps, decimal totalScore, decimal avgScore, int finalRating, string createdBy) => new()
    {
        AuditId = auditId,
        ElementId = elementId,
        TotalCheckPoints = totalCps,
        TotalScore = totalScore,
        ElementAvgScore = Math.Round(avgScore, 2),
        FinalRating = finalRating,
        CreatedBy = createdBy,
        CreatedOn = DateTime.UtcNow,
        UpdatedBy = createdBy,
        UpdatedOn = DateTime.UtcNow
    };

    public static AuditElementScore Reconstitute(int scoreId, int auditId, int elementId, int totalCps, decimal totalScore, decimal avgScore, int finalRating, string createdBy, DateTime createdOn, string updatedBy, DateTime updatedOn) => new()
    {
        ScoreId = scoreId,
        AuditId = auditId,
        ElementId = elementId,
        TotalCheckPoints = totalCps,
        TotalScore = totalScore,
        ElementAvgScore = avgScore,
        FinalRating = finalRating,
        CreatedBy = createdBy,
        CreatedOn = createdOn,
        UpdatedBy = updatedBy,
        UpdatedOn = updatedOn
    };

    public void SetElement(KeyElement element) => Element = element;

    public string RatingLabel => CheckPointScoreValue.GetLabel(FinalRating);
    
}

// ── AuditAttachmentResponse ──────────────────────────────────

public sealed class AuditAttachmentResponse
{
    public int AttachmentId { get; private set; }
    public int AuditId { get; private set; }
    public int ItemId { get; private set; }
    public string? IsAvailable { get; private set; }
    public string? Attachment { get; private set; }  // file path / URL
    public string? Remarks { get; private set; }
    public string CreatedBy { get; private set; } = default!;
    public DateTime CreatedOn { get; private set; }
    public string UpdatedBy { get; private set; } = default!;
    public DateTime UpdatedOn { get; private set; }

    public AttachmentItem? Item { get; private set; }

    private AuditAttachmentResponse() { }

    public static AuditAttachmentResponse Create(int auditId, int itemId, string attachmentPath, string? remarks, string createdBy) => new()
    {
        AuditId = auditId,
        ItemId = itemId,
        Attachment = attachmentPath?.Trim(),
        Remarks = remarks?.Trim(),
        CreatedBy = createdBy,
        CreatedOn = DateTime.UtcNow,
        UpdatedBy = createdBy,
        UpdatedOn = DateTime.UtcNow
    };

    public static AuditAttachmentResponse Reconstitute(int id, int auditId, int itemId, string? isAvailable, string? attachment, string? remarks, string createdBy, DateTime createdOn, string updatedBy, DateTime updatedOn) => new()
    {
        AttachmentId = id,
        AuditId = auditId,
        ItemId = itemId,
        IsAvailable = isAvailable,
        Attachment = attachment,
        Remarks = remarks,
        CreatedBy = createdBy,
        CreatedOn = createdOn,
        UpdatedBy = updatedBy,
        UpdatedOn = updatedOn
    };

    internal void Update(string attachmentPath, string? remarks, string updatedBy)
    {
        Attachment = attachmentPath?.Trim();
        Remarks = remarks?.Trim();
        UpdatedBy = updatedBy;
        UpdatedOn = DateTime.UtcNow;
    }

    public void SetItem(AttachmentItem item) => Item = item;
}

// ── AuditActivityLog ─────────────────────────────────────────

public sealed class AuditActivityLog
{
    public int LogId { get; private set; }
    public int AuditId { get; private set; }
    public string Action { get; private set; } = default!;
    public int? StepNo { get; private set; }
    public string PerformedBy { get; private set; } = default!;
    public DateTime PerformedAt { get; private set; }
    public string? Notes { get; private set; }

    private AuditActivityLog() { }

    public static AuditActivityLog Create(int auditId, string action, int? stepNo, string performedBy, string? notes = null) => new()
    {
        AuditId = auditId,
        Action = action,
        StepNo = stepNo,
        PerformedBy = performedBy,
        PerformedAt = DateTime.UtcNow,
        Notes = notes
    };

    public static AuditActivityLog Reconstitute(int logId, int auditId, string action, int? stepNo, string performedBy, DateTime performedAt, string? notes) => new()
    {
        LogId = logId,
        AuditId = auditId,
        Action = action,
        StepNo = stepNo,
        PerformedBy = performedBy,
        PerformedAt = performedAt,
        Notes = notes
    };
}