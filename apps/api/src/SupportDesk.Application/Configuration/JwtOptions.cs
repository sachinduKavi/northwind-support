using System.Text;

namespace SupportDesk.Application.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const int MinimumSigningKeyBytes = 32;

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string SigningKey { get; init; } = string.Empty;

    public int LifetimeMinutes { get; init; }

    public JwtOptions EnsureValid()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
        }

        if (Encoding.UTF8.GetByteCount(SigningKey) < MinimumSigningKeyBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:SigningKey must be at least {MinimumSigningKeyBytes} bytes. Set it with the Jwt__SigningKey environment variable.");
        }

        if (LifetimeMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt:LifetimeMinutes must be positive.");
        }

        return this;
    }
}
