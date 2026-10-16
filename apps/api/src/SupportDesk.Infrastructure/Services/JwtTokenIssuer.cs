using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Configuration;

namespace SupportDesk.Infrastructure.Services;

public sealed class JwtTokenIssuer(JwtOptions options, IClock clock) : ITokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    private readonly SigningCredentials _credentials = new(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
        SecurityAlgorithms.HmacSha256);

    public IssuedToken Issue(int agentId, string fullName, string email)
    {
        var now = clock.UtcNow;
        var expiresAtUtc = now.AddMinutes(options.LifetimeMinutes);

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expiresAtUtc,
            SigningCredentials = _credentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, agentId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim(JwtRegisteredClaimNames.Name, fullName),
                new Claim(JwtRegisteredClaimNames.Email, email)
            ])
        });

        return new IssuedToken(token, expiresAtUtc);
    }
}
