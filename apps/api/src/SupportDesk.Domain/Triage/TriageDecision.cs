namespace SupportDesk.Domain.Triage;

public sealed record TriageDecision(PriorityDecision Priority, SlaWindow Window, AssignmentDecision Assignment);
