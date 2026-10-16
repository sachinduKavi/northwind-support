namespace SupportDesk.Domain.Triage;

public sealed record AssignmentDecision(int? AgentId, string? AgentName, string Reason)
{
    public bool IsAssigned => AgentId is not null;
}
