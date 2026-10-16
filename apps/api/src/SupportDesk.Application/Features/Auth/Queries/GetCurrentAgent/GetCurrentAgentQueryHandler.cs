using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Auth.Queries.GetCurrentAgent;

public sealed class GetCurrentAgentQueryHandler(IAgentRepository agents)
{
    public async Task<CurrentAgentDto> HandleAsync(int agentId, CancellationToken ct)
    {
        var agent = await agents.GetByIdAsync(agentId, ct);

        if (agent is null || !agent.IsActive)
        {
            throw new UnauthorizedException("Your session is no longer valid. Please sign in again.");
        }

        return new CurrentAgentDto(agent.Id, agent.FullName, agent.Email);
    }
}
