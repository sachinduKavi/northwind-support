using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using SupportDesk.Application.Exceptions;

namespace SupportDesk.Presentation.Extensions;

public static class ClaimsPrincipalExtensions
{
    private const string InvalidSessionMessage = "Your session is no longer valid. Please sign in again.";

    public static int GetAgentId(this ClaimsPrincipal user) =>
        int.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : throw new UnauthorizedException(InvalidSessionMessage);

    public static string GetAgentName(this ClaimsPrincipal user) =>
        user.FindFirstValue(JwtRegisteredClaimNames.Name) is { Length: > 0 } name
            ? name
            : throw new UnauthorizedException(InvalidSessionMessage);
}
