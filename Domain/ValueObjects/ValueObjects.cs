// ================================================================
// Domain / ValueObjects / ValueObjects.cs
// Immutable value types that encapsulate business rules
// ================================================================
using Domain.Exceptions;

namespace Domain.ValueObjects;

// ── AuditCode ─────────────────────────────────────────────────
/// <summary>Format: VMM-YYYY-NNNNN. Immutable once assigned.</summary>
public sealed record AuditCode
{
    public string Value { get; }

    private AuditCode(string value) => Value = value;

    public static AuditCode Create(int year, int sequence)
    {
        if (sequence <= 0)
            throw new AuditCodeGenerationException("Sequence must be positive.");
        return new AuditCode($"VMM-{year}-{sequence:D5}");
    }

    public static AuditCode From(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !raw.StartsWith("VMM-"))
            throw new AuditCodeGenerationException($"'{raw}' is not a valid audit code.");
        return new AuditCode(raw);
    }

    public override string ToString() => Value;
}

// ── ScorePercent ─────────────────────────────────────────────
/// <summary>0–100 % audit score. Cannot be negative or >100.</summary>
public sealed record ScorePercent
{
    public decimal Value { get; }

    private ScorePercent(decimal value) => Value = value;

    public static ScorePercent Zero => new(0);

    public static ScorePercent From(decimal value)
    {
        if (value < 0 || value > 100)
            throw new ArgumentOutOfRangeException(nameof(value), "Score must be between 0 and 100.");
        return new ScorePercent(Math.Round(value, 2));
    }

    public override string ToString() => $"{Value:F1}%";
}

// ── CheckPointScoreValue ──────────────────────────────────────
/// <summary>Validates that a raw int maps to an allowed score: 0,2,4,6,8,10.</summary>
public sealed record CheckPointScoreValue
{
    private static readonly HashSet<int> AllowedScores = new() { 0, 2, 4, 6, 8, 10 };
    public int Value { get; }

    private CheckPointScoreValue(int value) => Value = value;

    public static CheckPointScoreValue From(int raw)
    {
        if (!AllowedScores.Contains(raw))
            throw new InvalidCheckPointScoreException(raw);
        return new CheckPointScoreValue(raw);
    }

    public static CheckPointScoreValue NotApplicable => new(0);
    public bool IsNA => Value == 0;

    // ── SINGLE SOURCE OF TRUTH ─────────────────────────────
    // Static — callable without an instance, handles null safely
    public static string GetLabel(int? score) => score switch
    {
        0 => "N/A",
        2 => "Poor",
        4 => "Below Average",
        6 => "Average",
        8 => "Good",
        10 => "Excellent",
        _ => "Unknown"
    };

    // Instance property — delegates to static (no duplication)
    public string Label => GetLabel(Value);
    public override string ToString() => $"{Value} ({Label})";
}

// ── DocumentAvailability ─────────────────────────────────────
/// <summary>Strongly-typed Y/N with null = not-set business meaning.</summary>
public sealed record DocumentAvailabilityValue
{
    public char? Raw { get; }     // 'Y', 'N', or null
    public bool? IsAvailable => Raw switch { 'Y' => true, 'N' => false, _ => null };
    public bool IsSet => Raw.HasValue;

    private DocumentAvailabilityValue(char? raw) => Raw = raw;

    public static DocumentAvailabilityValue NotSet => new((char?)null);
    public static DocumentAvailabilityValue Available => new('Y');
    public static DocumentAvailabilityValue NotAvailable => new('N');

    public static DocumentAvailabilityValue From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return NotSet;
        return value.Trim().ToUpperInvariant() switch
        {
            "Y" => Available,
            "N" => NotAvailable,
            _ => throw new InvalidDocumentAvailabilityException(value)
        };
    }

    public override string ToString() => Raw?.ToString() ?? "-";
}

// ── AuditPeriod ───────────────────────────────────────────────
/// <summary>Validity window computed from grade's months.</summary>
public sealed record AuditPeriod
{
    public DateTime From { get; }
    public DateTime Until { get; }
    public int ValidityMonths { get; }

    private AuditPeriod(DateTime from, int months)
    {
        From = from;
        ValidityMonths = months;
        Until = from.AddMonths(months);
    }

    public static AuditPeriod Create(int validityMonths) => new(DateTime.UtcNow, validityMonths);

    public bool IsExpired => DateTime.UtcNow > Until;

    public override string ToString() => $"{From:dd MMM yyyy} — {Until:dd MMM yyyy} ({ValidityMonths} months)";
}
