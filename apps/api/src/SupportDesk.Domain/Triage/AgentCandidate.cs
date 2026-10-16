namespace SupportDesk.Domain.Triage;

public sealed record AgentCandidate(
    int Id,
    string FullName,
    bool IsActive,
    int MaxOpenTickets,
    int OpenTicketCount,
    IReadOnlyCollection<int> SpecializationCategoryIds);
