using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Application.Exceptions;
using SupportDesk.Application.Features.Tickets.Queries.GetTicket;
using SupportDesk.Application.Features.Tickets.Triage;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

public sealed class EscalateTicketCommandHandler(
    ITicketRepository tickets,
    TicketTriage triage,
    IUnitOfWork unitOfWork,
    IClock clock,
    ITicketQueries queries,
    GetTicketQueryHandler getTicket,
    ILogger<EscalateTicketCommandHandler> logger)
{
    public async Task<EscalationResultDto> HandleAsync(int id, EscalateTicketRequest request, CancellationToken ct)
    {
        var ticket = await tickets.GetByIdAsync(id, ct) ?? throw new NotFoundException("Ticket", id);

        var decision = await triage.ForEscalationAsync(ticket, ct);

        var escalation = ticket.Escalate(
            decision.Window.Length,
            decision.Assignment.AgentId,
            request.Reason,
            request.EscalatedBy,
            clock.UtcNow);

        await tickets.AddEscalationAsync(escalation, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Ticket {Reference} escalated from {From} to {To} by {EscalatedBy}.",
            ticket.Reference, escalation.FromPriority, escalation.ToPriority, escalation.EscalatedBy);

        var recorded = await queries.GetEscalationAsync(escalation.Id, ct)
            ?? throw new NotFoundException("Escalation", escalation.Id);

        return new EscalationResultDto(await getTicket.HandleAsync(id, ct), recorded, TriageDto.From(decision));
    }
}
