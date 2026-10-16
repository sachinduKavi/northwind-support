using SupportDesk.Domain.Aggregates.Categories;

namespace SupportDesk.Domain.Triage;

public static class AgentSelector
{
    public static bool IsEligible(AgentCandidate agent, Category category) =>
        agent.IsActive
        && agent.OpenTicketCount < agent.MaxOpenTickets
        && (!category.RequiresSpecialist || agent.SpecializationCategoryIds.Contains(category.Id));

    public static AssignmentDecision Choose(IEnumerable<AgentCandidate> candidates, Category category)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(category);

        var eligible = candidates
            .Where(agent => IsEligible(agent, category))
            .OrderBy(agent => agent.OpenTicketCount)
            .ThenBy(agent => agent.Id)
            .ToList();

        if (eligible.Count == 0)
        {
            return new AssignmentDecision(
                null,
                null,
                category.RequiresSpecialist
                    ? $"Unassigned because no active {category.Name} specialist has capacity."
                    : "Unassigned because no active agent has capacity.");
        }

        var chosen = eligible[0];
        var pool = category.RequiresSpecialist ? $"{category.Name} specialists" : "agents";

        return new AssignmentDecision(
            chosen.Id,
            chosen.FullName,
            $"Assigned to {chosen.FullName}, who has the fewest open tickets ({chosen.OpenTicketCount} of {chosen.MaxOpenTickets}) among the eligible {pool} ({eligible.Count}).");
    }

    public static AssignmentDecision Reevaluate(int? currentAgentId, IReadOnlyCollection<AgentCandidate> candidates, Category category)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(category);

        var current = candidates.FirstOrDefault(agent => agent.Id == currentAgentId);

        if (current is not null && IsEligible(current, category))
        {
            return new AssignmentDecision(current.Id, current.FullName, $"Kept {current.FullName}, who is still eligible.");
        }

        var choice = Choose(candidates, category);

        return current is null
            ? choice
            : choice with { Reason = $"{current.FullName} is no longer eligible. {choice.Reason}" };
    }
}
