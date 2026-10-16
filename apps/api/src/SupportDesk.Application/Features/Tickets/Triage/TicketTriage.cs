using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;
using SupportDesk.Domain.Triage;

namespace SupportDesk.Application.Features.Tickets.Triage;

public sealed class TicketTriage(
    ICustomerRepository customers,
    ICategoryRepository categories,
    IAgentRepository agents,
    SlaPolicy slaPolicy)
{
    public async Task<TriageDecision> ForNewTicketAsync(
        int customerId,
        int categoryId,
        TicketPriority? requestedPriority,
        CancellationToken ct)
    {
        var (customer, category) = await LoadAsync(customerId, categoryId, ct);

        var priority = PriorityPolicy.Decide(requestedPriority, category);
        var window = slaPolicy.WindowFor(priority.Priority, customer.Tier);
        var assignment = AgentSelector.Choose(await agents.GetCandidatesAsync(null, ct), category);

        return new TriageDecision(priority, window, assignment);
    }

    public async Task<TriageDecision> ForEscalationAsync(Ticket ticket, CancellationToken ct)
    {
        var toPriority = ticket.PriorityAfterEscalation();
        var (customer, category) = await LoadAsync(ticket.CustomerId, ticket.CategoryId, ct);

        var priority = new PriorityDecision(toPriority, $"Escalated from {ticket.Priority} to {toPriority}.");
        var window = slaPolicy.WindowFor(toPriority, customer.Tier);
        var assignment = AgentSelector.Reevaluate(
            ticket.AssignedAgentId, await agents.GetCandidatesAsync(ticket.Id, ct), category);

        return new TriageDecision(priority, window, assignment);
    }

    private async Task<(Customer Customer, Category Category)> LoadAsync(int customerId, int categoryId, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(customerId, ct)
            ?? throw new NotFoundException("Customer", customerId);

        var category = await categories.GetByIdAsync(categoryId, ct)
            ?? throw new NotFoundException("Category", categoryId);

        return (customer, category);
    }
}
