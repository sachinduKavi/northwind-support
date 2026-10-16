using Microsoft.AspNetCore.Identity;
using SupportDesk.Domain.Aggregates.Agents;
using IPasswordHasher = SupportDesk.Application.Abstractions.IPasswordHasher;

namespace SupportDesk.Infrastructure.Services;

public sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<Agent> _hasher = new();
    private readonly string _unmatchableHash;

    public IdentityPasswordHasher()
    {
        _unmatchableHash = _hasher.HashPassword(null!, Guid.NewGuid().ToString("N"));
    }

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        return _hasher.HashPassword(null!, password);
    }

    public bool Verify(string? passwordHash, string password)
    {
        var result = _hasher.VerifyHashedPassword(null!, passwordHash ?? _unmatchableHash, password ?? string.Empty);

        return passwordHash is not null && result != PasswordVerificationResult.Failed;
    }
}
