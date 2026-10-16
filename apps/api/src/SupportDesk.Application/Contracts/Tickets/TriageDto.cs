using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Triage;

namespace SupportDesk.Application.Contracts.Tickets;

public sealed record TriageDto(
    TicketPriority Priority,
    string PriorityReason,
    double SlaWindowHours,
    string SlaWindowReason,
    AgentSummaryDto? AssignedAgent,
    string AssignmentReason)
{
    public static TriageDto From(TriageDecision decision) =>
        new(
            decision.Priority.Priority,
            decision.Priority.Reason,
            decision.Window.Length.TotalHours,
            decision.Window.Reason,
            decision.Assignment is { AgentId: { } id, AgentName: { } name } ? new AgentSummaryDto(id, name) : null,
            decision.Assignment.Reason);
}
