using FluentValidation;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Application.Features.Tickets.Commands.EscalateTicket;

public sealed class EscalateTicketRequestValidator : AbstractValidator<EscalateTicketRequest>
{
    public EscalateTicketRequestValidator()
    {
        RuleFor(x => x.Reason)
            .Must(reason => reason is not null
                && reason.Trim().Length is >= TicketEscalation.ReasonMinLength and <= TicketEscalation.ReasonMaxLength)
            .WithMessage($"Reason must be {TicketEscalation.ReasonMinLength} to {TicketEscalation.ReasonMaxLength} characters.");
    }
}
