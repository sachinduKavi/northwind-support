using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Domain.Repositories;
using SupportDesk.Domain.Triage;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Infrastructure.Repositories;

public sealed class AgentRepository(SupportDbContext db) : IAgentRepository
{
    /// <summary>Loads the whole aggregate, specializations included.</summary>
    public Task<Agent?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Set<Agent>()
            .Include(a => a.Specializations)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Agent?> GetByEmailAsync(string email, CancellationToken ct) =>
        db.Set<Agent>().AsNoTracking().FirstOrDefaultAsync(a => a.Email == email, ct);

    public async Task<IReadOnlyList<AgentCandidate>> GetCandidatesAsync(int? excludingTicketId, CancellationToken ct) =>
        await db.Set<Agent>()
            .AsNoTracking()
            .Select(a => new AgentCandidate(
                a.Id,
                a.FullName,
                a.IsActive,
                a.MaxOpenTickets,
                db.Set<Ticket>().Count(t =>
                    t.AssignedAgentId == a.Id
                    && t.Status != TicketStatus.Resolved
                    && t.Status != TicketStatus.Closed
                    && (excludingTicketId == null || t.Id != excludingTicketId)),
                a.Specializations.Select(s => s.CategoryId).ToList()))
            .ToListAsync(ct);
}
