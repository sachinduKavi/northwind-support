using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Domain.Triage;

public sealed record PriorityDecision(TicketPriority Priority, string Reason);
