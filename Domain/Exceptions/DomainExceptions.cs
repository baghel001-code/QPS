
// ================================================================
// Strongly-typed exceptions for every business rule violation
// ================================================================
namespace Domain.Exceptions;

public abstract class DomainException : Exception
{
    public string Code { get; }
    protected DomainException(string code, string message) : base(message) => Code = code;
}

// ── Audit lifecycle ──────────────────────────────────────────

public sealed class AuditNotFoundException : DomainException
{
    public AuditNotFoundException(int auditId)
        : base("AUDIT_NOT_FOUND", $"Audit with ID {auditId} was not found or has been deleted.") { }
}

public sealed class AuditAlreadySubmittedException : DomainException
{
    public AuditAlreadySubmittedException(string auditCode)
        : base("AUDIT_ALREADY_SUBMITTED", $"Audit '{auditCode}' has already been submitted and cannot be modified.") { }
}

public sealed class AuditStepNotUnlockedException : DomainException
{
    public AuditStepNotUnlockedException(int requestedStep, int maxUnlocked)
        : base("STEP_NOT_UNLOCKED", $"Step {requestedStep} is locked. Complete up to step {maxUnlocked} first.") { }
}

public sealed class AuditIncompleteForSubmissionException : DomainException
{
    public AuditIncompleteForSubmissionException(IEnumerable<string> missingSteps)
        : base("AUDIT_INCOMPLETE", $"Cannot submit audit. The following steps are incomplete: {string.Join(", ", missingSteps)}.")
    { }
}

// ── Scoring & grading ────────────────────────────────────────

public sealed class InvalidCheckPointScoreException : DomainException
{
    public InvalidCheckPointScoreException(int score)
        : base("INVALID_SCORE", $"Score '{score}' is invalid. Allowed values: 0, 2, 4, 6, 8, 10.") { }
}

public sealed class GradeNotFoundException : DomainException
{
    public GradeNotFoundException(decimal scorePercent)
        : base("GRADE_NOT_FOUND", $"No grade found for audit score {scorePercent:F1}%. Check grade table boundaries.") { }
}

// ── Document checklist ───────────────────────────────────────

public sealed class InvalidDocumentAvailabilityException : DomainException
{
    public InvalidDocumentAvailabilityException(string value)
        : base("INVALID_DOC_AVAILABILITY", $"Document availability '{value}' is invalid. Use 'Y' or 'N'.") { }
}

// ── Audit code ─────────────────────────────────────────────── AuditCodeGenerationException

public sealed class AuditCodeGenerationException : DomainException
{
    public AuditCodeGenerationException(string reason)
        : base("AUDIT_CODE_FAILED", $"Failed to generate audit code: {reason}.") { }
    // Overload with inner exception for chaining
    public AuditCodeGenerationException(string reason, Exception inner)
        : base("AUDIT_CODE_FAILED", $"Failed to generate audit code: {reason}")
    { }
}


// ── Critical compliance ──────────────────────────────────────

public sealed class CriticalCheckPointFailedException : DomainException
{
    public CriticalCheckPointFailedException(string question)
        : base("CRITICAL_FAIL", $"Critical compliance check failed: '{question}'. Audit cannot be approved.") { }
}

// Infrastructure/Exceptions/DataAccessException.cs
public sealed class DataAccessException : Exception
{
    public DataAccessException(string message, Exception inner)
        : base(message, inner) { }
}
