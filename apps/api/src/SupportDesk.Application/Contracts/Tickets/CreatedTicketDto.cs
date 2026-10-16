namespace SupportDesk.Application.Contracts.Tickets;

public sealed record CreatedTicketDto(TicketDetailDto Ticket, TriageDto Triage);
