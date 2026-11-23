// ================================================================
// FactoryAudit.Domain / Entities / MasterEntities.cs
// Read-only master data entities loaded from seed tables.
// Private setters enforce that changes must go through methods.
// ================================================================
using Domain.Enums;
using Domain.Exceptions;
using Domain.ValueObjects;
namespace Domain.Entities.Audit;

// ── AuditStatus ──────────────────────────────────────────────

public sealed class AuditStatus
{
    public int StatusId { get; private set; }
    public string StatusName { get; private set; } = default!;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public string CreatedBy { get; private set; } = default!;
    public DateTime CreatedOn { get; private set; }
    public string UpdatedBy { get; private set; } = default!;
    public DateTime UpdatedOn { get; private set; }

    private AuditStatus() { } // Dapper

    public static AuditStatus Reconstitute(int id, string name, int order, bool isActive, string createdBy, DateTime createdOn, string updatedBy, DateTime updatedOn) => new()
    {
        StatusId = id,
        StatusName = name,
        DisplayOrder = order,
        IsActive = isActive,
        CreatedBy = createdBy,
        CreatedOn = createdOn,
        UpdatedBy = updatedBy,
        UpdatedOn = updatedOn
    };

    public AuditStatusEnum ToEnum() => (AuditStatusEnum)StatusId;
    public override string ToString() => StatusName;
}

// ── AuditGrade ───────────────────────────────────────────────

public sealed class AuditGrade
{
    public int GradeId { get; private set; }
    public char GradeCode { get; private set; }
    public decimal MinScore { get; private set; }
    public decimal MaxScore { get; private set; }
    public int ValidityInMonths { get; private set; }
    public string ValidationPeriod { get; private set; } = default!;
    public string ColorHex { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public string CreatedBy { get; private set; } = default!;
    public DateTime CreatedOn { get; private set; }
    public string UpdatedBy { get; private set; } = default!;
    public DateTime UpdatedOn { get; private set; }

    private AuditGrade() { }

    public static AuditGrade Reconstitute(int id, char code, decimal min, decimal max,
        int months, string period, string color, string desc, bool isActive,
        string createdBy, DateTime createdOn, string updatedBy, DateTime updatedOn)
        => new()
        {
            GradeId = id,
            GradeCode = code,
            MinScore = min,
            MaxScore = max,
            ValidityInMonths = months,
            ValidationPeriod = period,
            ColorHex = color,
            Description = desc,
            IsActive = isActive,
            CreatedBy = createdBy,
            CreatedOn = createdOn,
            UpdatedBy = updatedBy,
            UpdatedOn = updatedOn
        };

    /// <summary>Business rule: score falls within this grade's band.</summary>
    public bool Matches(decimal scorePercent) => scorePercent >= MinScore && scorePercent <= MaxScore;

    /// <summary>Computes the validity end date from today.</summary>
    public DateTime ComputeValidityTill() => DateTime.UtcNow.Date.AddMonths(ValidityInMonths);

    public bool IsBlacklistRisk => GradeCode == 'D';

    public override string ToString() => $"Grade {GradeCode} ({MinScore}–{MaxScore}%)";
}

// ── KeyElement ───────────────────────────────────────────────

public sealed class KeyElement
{
    public int ElementId { get; private set; }
    public int ElementNo { get; private set; }
    public string ElementName { get; private set; } = default!;
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public string CreatedBy { get; private set; } = default!;
    public DateTime CreatedOn { get; private set; }
    public string UpdatedBy { get; private set; } = default!;
    public DateTime UpdatedOn { get; private set; }

    private readonly List<CheckPoint> _checkPoints = new();
    public IReadOnlyList<CheckPoint> CheckPoints => _checkPoints.AsReadOnly();

    private KeyElement() { }

    public static KeyElement Reconstitute(int id, int no, string name, int order, bool isActive, string createdBy, DateTime createdOn, string updatedBy, DateTime updatedOn) => new()
    {
        ElementId = id,
        ElementNo = no,
        ElementName = name,
        DisplayOrder = order,
        IsActive = isActive,
        CreatedBy = createdBy,
        CreatedOn = createdOn,
        UpdatedBy = updatedBy,
        UpdatedOn = updatedOn
    };

    public void AddCheckPoint(CheckPoint cp) => _checkPoints.Add(cp);

    /// <summary>Returns only active checkpoints in display order.</summary>
    public IReadOnlyList<CheckPoint> ActiveCheckPoints => _checkPoints.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ToList().AsReadOnly();

    /// <summary>Business: element has at least one critical checkpoint.</summary>
    public bool HasCriticalCheckPoints => _checkPoints.Any(c => c.IsCritical && c.IsActive);

    /// <summary>Wizard step number for this element (Element 1 = Step 4, etc.)</summary>
    public int WizardStep => ElementNo + 3;

    public override string ToString() => $"{ElementNo}. {ElementName}";
}

// ── CheckPoint ───────────────────────────────────────────────

public sealed class CheckPoint
{
    public int CheckPointId { get; private set; }
    public int ElementId { get; private set; }
    public int CheckPointNo { get; private set; }
    public string Question { get; private set; } = default!;
    public bool HaveDropdown { get; private set; }
    public bool IsCritical { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsActive { get; private set; }
    public string CreatedBy { get; private set; } = default!;
    public DateTime CreatedOn { get; private set; }
    public string UpdatedBy { get; private set; } = default!;
    public DateTime UpdatedOn { get; private set; }

    private CheckPoint() { }

    public static CheckPoint Reconstitute(int id, int elementId, int no, string question, bool hasDropdown, bool isCritical, int order, bool isActive, string createdBy,
        DateTime createdOn, string updatedBy, DateTime updatedOn) => new()
        {
            CheckPointId = id,
            ElementId = elementId,
            CheckPointNo = no,
            Question = question,
            HaveDropdown = hasDropdown,
            IsCritical = isCritical,
            DisplayOrder = order,
            IsActive = isActive,
            CreatedBy = createdBy,
            CreatedOn = createdOn,
            UpdatedBy = updatedBy,
            UpdatedOn = updatedOn
        };

    /// <summary>Business: validate that a given score is acceptable for this checkpoint.</summary>
    public void ValidateScore(CheckPointScoreValue score)
    {
        // Critical checkpoints cannot have N/A score (0)
        if (IsCritical && score.IsNA)
            throw new CriticalCheckPointFailedException(Question);
    }

    public override string ToString() => $"CP{CheckPointNo}: {Question}";
}

// ── DocumentChecklistItem ────────────────────────────────────

public sealed class DocumentChecklistItem
{
    public int ItemId { get; private set; }
    public int SrNo { get; private set; }
    public string DocumentName { get; private set; } = default!;
    public bool IsMandatory { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }
    public string CreatedBy { get; private set; } = default!;
    public DateTime CreatedOn { get; private set; }
    public string UpdatedBy { get; private set; } = default!;
    public DateTime UpdatedOn { get; private set; }

    private DocumentChecklistItem() { }

    public static DocumentChecklistItem Reconstitute(int id, int srNo, string name,
        bool isMandatory, bool isActive, int order,
        string createdBy, DateTime createdOn, string updatedBy, DateTime updatedOn)
        => new()
        {
            ItemId = id,
            SrNo = srNo,
            DocumentName = name,
            IsMandatory = isMandatory,
            IsActive = isActive,
            DisplayOrder = order,
            CreatedBy = createdBy,
            CreatedOn = createdOn,
            UpdatedBy = updatedBy,
            UpdatedOn = updatedOn
        };

    public override string ToString() => $"{SrNo}. {DocumentName}";
}

// ── AttachmentItem ───────────────────────────────────────────

public sealed class AttachmentItem
{
    public int ItemId { get; private set; }
    public int SrNo { get; private set; }
    public string AttachmentName { get; private set; } = default!;
    public bool IsMandatory { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }
    public string CreatedBy { get; private set; } = default!;
    public DateTime CreatedOn { get; private set; }
    public string UpdatedBy { get; private set; } = default!;
    public DateTime UpdatedOn { get; private set; }

    private AttachmentItem() { }

    public static AttachmentItem Reconstitute(int id, int srNo, string name,
        bool isMandatory, bool isActive, int order,
        string createdBy, DateTime createdOn, string updatedBy, DateTime updatedOn)
        => new()
        {
            ItemId = id,
            SrNo = srNo,
            AttachmentName = name,
            IsMandatory = isMandatory,
            IsActive = isActive,
            DisplayOrder = order,
            CreatedBy = createdBy,
            CreatedOn = createdOn,
            UpdatedBy = updatedBy,
            UpdatedOn = updatedOn
        };

    public override string ToString() => $"{SrNo}. {AttachmentName}";
}
