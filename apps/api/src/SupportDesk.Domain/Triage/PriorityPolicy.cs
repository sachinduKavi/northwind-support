using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Tickets;

namespace SupportDesk.Domain.Triage;

public static class PriorityPolicy
{
    public const TicketPriority DefaultPriority = TicketPriority.Medium;

    public static PriorityDecision Decide(TicketPriority? requested, Category category)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (category.ForcesCriticalPriority)
        {
            return new PriorityDecision(
                TicketPriority.Critical,
                $"{category.Name} tickets are always {TicketPriority.Critical}.");
        }

        return requested is { } priority
            ? new PriorityDecision(priority, $"{priority} priority as requested.")
            : new PriorityDecision(DefaultPriority, $"No priority requested, so {DefaultPriority} applies.");
    }
}
