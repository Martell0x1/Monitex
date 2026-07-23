using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace SmartHome.Infrastructure.Hubs;

/// <summary>
/// Resolves SignalR UserIdentifier from JWT claims used by Monitex auth.
/// </summary>
public class MonitexUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var user = connection.User;
        if (user == null)
        {
            return null;
        }

        var direct = FirstClaimValue(
            user,
            "userId",
            ClaimTypes.NameIdentifier,
            "nameid",
            "sub"
        );

        if (!string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        foreach (var claim in user.Claims)
        {
            var type = claim.Type;
            var looksLikeUserId =
                type.Contains("user", StringComparison.OrdinalIgnoreCase)
                || type.Contains("nameidentifier", StringComparison.OrdinalIgnoreCase)
                || type is "sub" or "nameid";

            if (looksLikeUserId
                && int.TryParse(claim.Value, out var id)
                && id > 0)
            {
                return id.ToString();
            }
        }

        return null;
    }

    private static string? FirstClaimValue(ClaimsPrincipal user, params string[] claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = user.FindFirst(claimType)?.Value;
            if (!string.IsNullOrWhiteSpace(value)
                && int.TryParse(value, out var id)
                && id > 0)
            {
                return id.ToString();
            }
        }

        return null;
    }
}
