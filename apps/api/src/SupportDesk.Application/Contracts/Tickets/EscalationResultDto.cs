namespace SupportDesk.Application.Contracts.Tickets;

public sealed record EscalationResultDto(TicketDetailDto Ticket, TicketEscalationDto Escalation, TriageDto Triage);
