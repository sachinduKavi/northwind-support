namespace SupportDesk.Application.Abstractions;

public interface ITokenIssuer
{
    IssuedToken Issue(int agentId, string fullName, string email);
}

public sealed record IssuedToken(string Token, DateTime ExpiresAtUtc);
