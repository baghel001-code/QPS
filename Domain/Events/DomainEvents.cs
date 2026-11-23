
// ================================================================
// Domain events raised by aggregate roots
// ================================================================
namespace Domain.Events;

public abstract record DomainEvent(DateTime OccurredAt)
{
    protected DomainEvent() : this(DateTime.UtcNow) { }
}

public sealed record AuditDraftCreatedEvent(int AuditId, string AuditCode, string AuditorEmpCode, DateTime AuditDate) : DomainEvent;

public sealed record AuditStepCompletedEvent(int AuditId, int StepNo, string StepName) : DomainEvent;

public sealed record AuditSubmittedEvent(int AuditId, string AuditCode, decimal ScorePercent, char GradeCode, DateTime ValidityTill) : DomainEvent;

public sealed record AuditApprovedEvent(int AuditId, string ApprovedBy) : DomainEvent;

public sealed record AuditRejectedEvent(int AuditId, string RejectedBy, string Reason) : DomainEvent;

public sealed record CriticalComplianceFailedEvent(int AuditId, int CheckPointId, string Question) : DomainEvent;