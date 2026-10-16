using Microsoft.Extensions.Logging;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Exceptions;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler(
    IAgentRepository agents,
    IPasswordHasher passwordHasher,
    ITokenIssuer tokenIssuer,
    ILogger<LoginCommandHandler> logger)
{
    public const string InvalidCredentialsMessage = "Email or password is incorrect.";

    public async Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken ct)
    {
        var agent = await agents.GetByEmailAsync(request.Email.Trim(), ct);

        var passwordMatches = passwordHasher.Verify(agent?.PasswordHash, request.Password);

        if (agent is null || !passwordMatches || !agent.IsActive)
        {
            logger.LogInformation("Rejected a sign-in attempt.");
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var token = tokenIssuer.Issue(agent.Id, agent.FullName, agent.Email);

        logger.LogInformation("Agent {AgentId} signed in.", agent.Id);

        return new LoginResponse(token.Token, token.ExpiresAtUtc, new CurrentAgentDto(agent.Id, agent.FullName, agent.Email));
    }
}
