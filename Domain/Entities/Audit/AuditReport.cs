// ================================================================
//Domain / Entities / AuditReport.cs
//
// AGGREGATE ROOT — AuditReport
//
// Business rules enforced here:
//  1. Draft created only via factory method Create()
//  2. Steps can only be saved in order — cannot skip ahead
//  3. Once Submitted, no edits allowed
//  4. Score calculation is a domain concern
//  5. Critical checkpoints (No Child Labour) cannot score 0
//  6. Submission requires all 16 steps completed
//  7. Audit code is immutable after creation
//  8. Grade and validity are computed — never manually set
//  9. IsDeleted is soft-delete only — physical delete forbidden
// ================================================================
using Domain.Enums;
using Domain.Events;
using Domain.Exceptions;
using Domain.ValueObjects;

namespace Domain.Entities.Audit;

public sealed class AuditReport
{
    // ── Identity ─────────────────────────────────────────────
    public int AuditId { get; private set; }
    public AuditCode AuditCode { get; private set; } = default!;

    // ── State ────────────────────────────────────────────────
    public AuditStatusEnum Status { get; private set; } = AuditStatusEnum.Draft;
    public int CurrentStep { get; private set; } = (int)WizardStep.AuditorInfo;
    public int MaxUnlockedStep { get; private set; } = (int)WizardStep.AuditorInfo;

    // ── Step 1: Auditor ──────────────────────────────────────
    public string AuditorEmpCode { get; private set; } = default!;
    public DateTime AuditDate { get; private set; }

    // ── Step 2: Vendor / Factory ─────────────────────────────
    public long AuditReqId { get; private set; }
    public string FactoryOwnerEmail { get; private set; } = default!;

    // ── Computed Score ───────────────────────────────────────
    public ScorePercent? AuditScorePercent { get; private set; }
    public int? GradeId { get; private set; }
    public char? GradeCode { get; private set; }
    public string? Description { get; private set; }
    public DateTime? ValidityTill { get; private set; }

    // ── Comments ─────────────────────────────────────────────
    public string? OverallComments { get; private set; }

    // ── Audit Trail ──────────────────────────────────────────
    public bool IsDeleted { get; private set; }
    public string CreatedBy { get; private set; } = default!;
    public DateTime CreatedOn { get; private set; }
    public string UpdatedBy { get; private set; } = default!;
    public DateTime UpdatedOn { get; private set; }
    public DateTime? SubmittedAt { get; private set; }

    // ── Child Collections ────────────────────────────────────
    private readonly List<AuditDocumentResponse> _documentResponses = new();
    private readonly List<AuditCheckPointResponse> _checkPointResponses = new();
    private readonly List<AuditElementScore> _elementScores = new();
    private readonly List<AuditAttachmentResponse> _attachmentResponses = new();
    private readonly List<AuditActivityLog> _activityLogs = new();
    private readonly List<DomainEvent> _domainEvents = new();

    public IReadOnlyList<AuditDocumentResponse> DocumentResponses => _documentResponses.AsReadOnly();
    public IReadOnlyList<AuditCheckPointResponse> CheckPointResponses => _checkPointResponses.AsReadOnly();
    public IReadOnlyList<AuditElementScore> ElementScores => _elementScores.AsReadOnly();
    public IReadOnlyList<AuditAttachmentResponse> AttachmentResponses => _attachmentResponses.AsReadOnly();
    public IReadOnlyList<AuditActivityLog> ActivityLogs => _activityLogs.AsReadOnly();
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    private AuditReport() { }
    // ================================================================
    // FACTORY METHOD — only way to create a new draft
    // ================================================================
    public static AuditReport Create(long auditReqId, AuditCode auditCode, string auditorEmpCode, DateTime auditDate, string createdBy)
    {
        // ── Step 1 validations only ───────────────────────────
        if (string.IsNullOrWhiteSpace(auditorEmpCode))
            throw new ArgumentException("Auditor employee code is required.", nameof(auditorEmpCode));
        if (auditDate == default || auditDate.Date > DateTime.UtcNow.Date.AddDays(1))
            throw new ArgumentException("A valid audit date is required.", nameof(auditDate));
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created-by user is required.", nameof(createdBy));

        var audit = new AuditReport
        {
            AuditReqId = auditReqId,
            AuditCode = auditCode,
            Status = AuditStatusEnum.Draft,
            CurrentStep = (int)WizardStep.AuditorInfo,
            MaxUnlockedStep = (int)WizardStep.AuditorInfo,
            AuditorEmpCode = auditorEmpCode.Trim().ToUpperInvariant(),
            AuditDate = auditDate.Date,
            FactoryOwnerEmail = string.Empty,
            IsDeleted = false,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow,
            UpdatedBy = createdBy,
            UpdatedOn = DateTime.UtcNow
        };

        audit._domainEvents.Add(new AuditDraftCreatedEvent(0, auditCode.Value, auditorEmpCode, auditDate));

        return audit;
    }

    // ================================================================
    // RECONSTITUTE — rebuild from persistence (Dapper)
    // ================================================================
    public static AuditReport Reconstitute(
        int auditId, string auditCode, int statusId, int currentStep, int maxUnlocked,
        string auditorEmpCode, DateTime auditDate, long auditReqId, string factoryOwnerEmail,
        decimal? scorePct, int? gradeId, char? gradeCode, DateTime? validityTill,
        string? overallComments, bool isDeleted,
        string createdBy, DateTime createdOn, string updatedBy, DateTime updatedOn,
        DateTime? submittedAt)
        => new()
        {
            AuditId = auditId,
            AuditCode = AuditCode.From(auditCode),
            Status = (AuditStatusEnum)statusId,
            CurrentStep = currentStep,
            MaxUnlockedStep = maxUnlocked,
            AuditorEmpCode = auditorEmpCode,
            AuditDate = auditDate,
            AuditReqId = auditReqId,
            FactoryOwnerEmail = factoryOwnerEmail,
            AuditScorePercent = scorePct.HasValue ? ScorePercent.From(scorePct.Value) : null,
            GradeId = gradeId,
            GradeCode = gradeCode,
            ValidityTill = validityTill,
            OverallComments = overallComments,
            IsDeleted = isDeleted,
            CreatedBy = createdBy,
            CreatedOn = createdOn,
            UpdatedBy = updatedBy,
            UpdatedOn = updatedOn,
            SubmittedAt = submittedAt
        };

    // ================================================================
    // STEP 1 — UPDATE AUDITOR INFO
    // ================================================================
    public void UpdateAuditorInfo(long auditReqId, string auditorEmpCode, DateTime auditDate, string updatedBy)
    {
        GuardNotSubmitted();
        if (auditReqId <= 0)
            throw new ArgumentException("Audit request ID is required.");
        if (string.IsNullOrWhiteSpace(auditorEmpCode))
            throw new ArgumentException("Auditor employee code is required.");

        AuditReqId = auditReqId;
        AuditorEmpCode = auditorEmpCode.Trim().ToUpperInvariant();
        AuditDate = auditDate.Date;
        Touch(updatedBy);
        UnlockNextStep((int)WizardStep.AuditorInfo);
        AddLog("Step 1 Saved - Auditor Info", (int)WizardStep.AuditorInfo, updatedBy);
        _domainEvents.Add(new AuditStepCompletedEvent(AuditId, (int)WizardStep.AuditorInfo, "Auditor Info"));
    }

    // ================================================================
    // STEP 2 — UPDATE VENDOR / REQUEST INFO
    // ================================================================
    public void UpdateVendorInfo(string factoryOwnerEmail, string updatedBy)
    {
        GuardNotSubmitted();
        GuardStepUnlocked((int)WizardStep.VendorInfo);

        if (string.IsNullOrWhiteSpace(factoryOwnerEmail))
            throw new ArgumentException("Factory owner email is required.");
        FactoryOwnerEmail = factoryOwnerEmail.Trim().ToLowerInvariant();
        Touch(updatedBy);
        UnlockNextStep((int)WizardStep.VendorInfo);
        AddLog("Step 2 Saved - Vendor Info", (int)WizardStep.VendorInfo, updatedBy);
        _domainEvents.Add(new AuditStepCompletedEvent(AuditId, (int)WizardStep.VendorInfo, "Vendor Info"));
    }

    // ================================================================
    // STEP 3 — DOCUMENT CHECKLIST
    // ================================================================
    public void UpsertDocumentResponse(int itemId, DocumentAvailabilityValue availability, DateTime? validityDate, string? unitInfo, string? attachments, string? remarks, string updatedBy)
    {
        GuardNotSubmitted();
        GuardStepUnlocked((int)WizardStep.DocumentChecklist);

        var existing = _documentResponses.FirstOrDefault(r => r.ItemId == itemId);
        if (existing is null)
        {
            _documentResponses.Add(AuditDocumentResponse.Create(AuditId, itemId, availability, validityDate, unitInfo, attachments, remarks, updatedBy));
        }
        else
        {
            existing.Update(availability, validityDate, unitInfo, attachments, remarks, updatedBy);
        }

        Touch(updatedBy);
    }

    public void CompleteDocumentStep(string updatedBy)
    {
        GuardNotSubmitted();
        Touch(updatedBy);
        UnlockNextStep((int)WizardStep.DocumentChecklist);
        AddLog("Step 3 Saved - Document Checklist", (int)WizardStep.DocumentChecklist, updatedBy);
        _domainEvents.Add(new AuditStepCompletedEvent(AuditId, (int)WizardStep.DocumentChecklist, "Document Checklist"));
    }

    // ================================================================
    // STEPS 4–13 — CHECKPOINT SCORES (one element per step)
    // ================================================================
    public void UpsertCheckPointResponse(int checkPointId, int elementId, CheckPointScoreValue score, CheckPoint checkPointDef, string? remarks, string updatedBy)
    {
        GuardNotSubmitted();
        var wizardStep = elementId + 3; // Element 1 = Step 4
        GuardStepUnlocked(wizardStep);

        // Domain business rule: critical checkpoints cannot be N/A
        checkPointDef.ValidateScore(score);

        var existing = _checkPointResponses.FirstOrDefault(r => r.CheckPointId == checkPointId);
        if (existing is null)
        {
            _checkPointResponses.Add(AuditCheckPointResponse.Create(AuditId, checkPointId, score, remarks, updatedBy));
        }
        else
        {
            existing.UpdateScore(score, remarks, updatedBy);
        }

        Touch(updatedBy);

        // Raise critical failure event
        if (checkPointDef.IsCritical && score.Value <= 2)
            _domainEvents.Add(new CriticalComplianceFailedEvent(AuditId, checkPointId, checkPointDef.Question));
    }

    public void CompleteElementStep(int elementId, string updatedBy)
    {
        GuardNotSubmitted();
        var wizardStep = elementId + 3;
        Touch(updatedBy);
        UnlockNextStep(wizardStep);
        AddLog($"Element {elementId} Scores Saved", wizardStep, updatedBy);
        _domainEvents.Add(new AuditStepCompletedEvent(AuditId, wizardStep, $"Element {elementId}"));
    }

    public void ReconstituteCheckPointResponseList(List<AuditCheckPointResponse> auditCheckPointResponses)
    {
        if (!_checkPointResponses.Any())
        {
            foreach (var item in auditCheckPointResponses)
            {
                _checkPointResponses.Add(item);
            }

        }
    }


    // ================================================================
    // STEP 14 — PHOTO ATTACHMENTS
    // ================================================================
    public void UpsertAttachmentResponse(int itemId, string attachmentPath, string? remarks, string updatedBy)
    {
        GuardNotSubmitted();
        GuardStepUnlocked((int)WizardStep.PhotoAttachments);

        var existing = _attachmentResponses.FirstOrDefault(r => r.ItemId == itemId);
        if (existing is null)
            _attachmentResponses.Add(AuditAttachmentResponse.Create(AuditId, itemId, attachmentPath, remarks, updatedBy));
        else
            existing.Update(attachmentPath, remarks, updatedBy);

        Touch(updatedBy);
    }

    public void CompleteAttachmentStep(string updatedBy)
    {
        GuardNotSubmitted();
        Touch(updatedBy);
        UnlockNextStep((int)WizardStep.PhotoAttachments);
        AddLog("Step 14 Saved - Attachments", (int)WizardStep.PhotoAttachments, updatedBy);
    }

    // ================================================================
    // STEP 15 — OVERALL COMMENTS
    // ================================================================
    public void SaveOverallComments(string? comments, string updatedBy)
    {
        GuardNotSubmitted();
        GuardStepUnlocked((int)WizardStep.OverallComments);
        OverallComments = comments?.Trim();
        Touch(updatedBy);
        UnlockNextStep((int)WizardStep.OverallComments);
        AddLog("Step 15 Saved - Comments", (int)WizardStep.OverallComments, updatedBy);
    }

    // ================================================================
    // SUBMIT — computes scores, locks the audit
    // ================================================================
    public AuditScoreResult Submit(IEnumerable<AuditGrade> availableGrades, string submittedBy)
    {
        GuardNotSubmitted();
        ValidateForSubmission();

        // Compute element scores
        //var cpByElement = _checkPointResponses.Where(r => !r.Score.IsNA).GroupBy(r => GetElementIdForCheckPoint(r.CheckPointId)).ToList();
        var cpByElement = _checkPointResponses.GroupBy(r => GetElementIdForCheckPoint(r.CheckPointId)).ToList();
        _elementScores.Clear();

        foreach (var grp in cpByElement)
        {
            var avg = grp.Average(r => (decimal)r.Score.Value);
            var rating = ScoringCalculator.ComputeElementRating(avg);
            _elementScores.Add(AuditElementScore.Create(
                AuditId, grp.Key, grp.Count(),
                grp.Sum(r => (decimal)r.Score.Value),
                avg, rating, submittedBy));
        }

        var pct = _elementScores.Any()
             ? ScorePercent.From(ScoringCalculator.ComputeScorePercent(_elementScores.Select(e => e.FinalRating)))
             : ScorePercent.Zero;

        var grade = availableGrades.FirstOrDefault(g => g.Matches(pct.Value)) ?? throw new GradeNotFoundException(pct.Value);

        AuditScorePercent = pct;
        GradeId = grade.GradeId;
        GradeCode = grade.GradeCode;
        Description = grade.Description;
        ValidityTill = grade.ComputeValidityTill();
        Status = AuditStatusEnum.Submitted;
        SubmittedAt = DateTime.UtcNow;

        Touch(submittedBy);
        UnlockNextStep((int)WizardStep.ReviewAndSubmit);
        AddLog($"Audit Submitted — Score: {pct} Grade: {grade.GradeCode}", 16, submittedBy);
        _domainEvents.Add(new AuditSubmittedEvent(AuditId, AuditCode.Value,
            pct.Value, grade.GradeCode, ValidityTill.Value));

        return new AuditScoreResult(pct.Value, grade.GradeCode, grade.ColorHex, grade.Description, grade.ValidationPeriod, ValidityTill.Value, grade.IsBlacklistRisk);
    }

    // ================================================================
    // APPROVE / REJECT
    // ================================================================
    public void Approve(string approvedBy)
    {
        if (Status != AuditStatusEnum.Submitted && Status != AuditStatusEnum.UnderReview)
            throw new InvalidOperationException("Only Submitted or Under Review audits can be approved.");
        Status = AuditStatusEnum.Approved;
        Touch(approvedBy);
        AddLog("Audit Approved", null, approvedBy);
        _domainEvents.Add(new AuditApprovedEvent(AuditId, approvedBy));
    }

    public void Reject(string rejectedBy, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.");
        Status = AuditStatusEnum.Rejected;
        Touch(rejectedBy);
        AddLog($"Audit Rejected — {reason}", null, rejectedBy);
        _domainEvents.Add(new AuditRejectedEvent(AuditId, rejectedBy, reason));
    }

    // ================================================================
    // SOFT DELETE
    // ================================================================
    public void SoftDelete(string deletedBy)
    {
        if (Status == AuditStatusEnum.Approved)
            throw new InvalidOperationException("Approved audits cannot be deleted.");
        IsDeleted = true;
        Touch(deletedBy);
        AddLog("Audit Soft-Deleted", null, deletedBy);
    }

    // ================================================================
    // QUERY HELPERS
    // ================================================================
    public bool IsSubmitted => Status == AuditStatusEnum.Submitted || Status == AuditStatusEnum.Approved || Status == AuditStatusEnum.Rejected;
    public bool IsEditable => !IsSubmitted && !IsDeleted;
    public bool IsStepComplete(int step) => step < CurrentStep;
    public bool IsStepUnlocked(int step) => step <= MaxUnlockedStep;

    public int CompletedStepsCount => MaxUnlockedStep - 1;
    public double ProgressPercent => (CompletedStepsCount / (double)(int)WizardStep.ReviewAndSubmit) * 100;

    // Loaded by repo after reconstitution — element ID lookup
    private Dictionary<int, int> _checkPointElementMap = new();
    public void SetCheckPointElementMap(Dictionary<int, int> map) => _checkPointElementMap = map;
    private int GetElementIdForCheckPoint(int cpId) => _checkPointElementMap.TryGetValue(cpId, out var eid) ? eid : 0;

    // ================================================================
    // GUARDS
    // ================================================================
    private void GuardNotSubmitted()
    {
        if (IsSubmitted)
            throw new AuditAlreadySubmittedException(AuditCode.Value);
    }

    private void GuardStepUnlocked(int step)
    {
        if (step > MaxUnlockedStep)
            throw new AuditStepNotUnlockedException(step, MaxUnlockedStep);
    }

    private void ValidateForSubmission()
    {
        var missing = new List<string>();
        const int lastDataStep = (int)WizardStep.OverallComments;
        for (int s = 1; s <= lastDataStep; s++)
        {
            if (MaxUnlockedStep < s + 1)
                missing.Add(((WizardStep)s).ToString());
        }
        if (missing.Any())
            throw new AuditIncompleteForSubmissionException(missing);
    }

    // ================================================================
    // PRIVATE HELPERS
    // ================================================================
    private void Touch(string by)
    {
        UpdatedBy = by;
        UpdatedOn = DateTime.UtcNow;
        if (Status == AuditStatusEnum.Draft)
            Status = AuditStatusEnum.InProgress;
    }

    private void UnlockNextStep(int completedStep)
    {
        CurrentStep = completedStep + 1;
        MaxUnlockedStep = Math.Max(MaxUnlockedStep, completedStep + 1);
    }

    private void AddLog(string action, int? step, string by, string? notes = null)
    {
        _activityLogs.Add(AuditActivityLog.Create(AuditId, action, step, by, notes));
    }
}

// ── AuditScoreResult (returned by Submit) ────────────────────
public sealed record AuditScoreResult(decimal ScorePercent, char GradeCode, string ColorHex, string Description, string ValidationPeriod, DateTime ValidityTill, bool IsBlacklistRisk);


public static class ScoringCalculator
{

    public static int ComputeElementRating(decimal averageScore) => averageScore switch
    {
        0 => 0,
        <= 2.4m => 2,
        <= 4.4m => 4,
        <= 6.4m => 6,
        <= 8.4m => 8,
        _ => 10
    };

    public static decimal ComputeElementWeight(int rating) => rating switch
    {
        0 => 0m,
        <= 4 => 0.25m,
        6 => 0.50m,
        _ => 1.0m // 8 or 10
    };

    public static decimal ComputeElementWeightFromAverage(decimal averageScore)
        => ComputeElementWeight(ComputeElementRating(averageScore));

    public static decimal ComputeScorePercent(IEnumerable<int> elementRatings, int decimals = 0)
    {
        // % Audit Score = [(0.25 (score≤4) + 0.5 (score=6) + 1 (score>6)]× 100 / No of key elements
        var ratings = elementRatings.ToList();
        if (!ratings.Any()) return 0m;

        var weightedSum = ratings.Sum(ComputeElementWeight);
        var percent = (weightedSum / ratings.Count) * 100m;

        return Math.Round(percent, decimals, MidpointRounding.AwayFromZero);
    }
}