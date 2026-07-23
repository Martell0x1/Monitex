using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace SmartHome.Infrastructure.Hubs;

[Authorize]
public class SensorHub : Hub
{
    private readonly ILogger<SensorHub> _logger;

    public SensorHub(ILogger<SensorHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = ResolveUserId();
        var claimSummary = string.Join(
            ", ",
            Context.User?.Claims.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>()
        );

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
            _logger.LogInformation(
                "SignalR connected. ConnectionId={ConnectionId} UserId={UserId} UserIdentifier={UserIdentifier} Claims=[{Claims}]",
                Context.ConnectionId,
                userId,
                Context.UserIdentifier,
                claimSummary
            );
        }
        else
        {
            _logger.LogWarning(
                "SignalR connected without resolvable user id. ConnectionId={ConnectionId} Authenticated={Authenticated} Claims=[{Claims}]",
                Context.ConnectionId,
                Context.User?.Identity?.IsAuthenticated,
                claimSummary
            );
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = ResolveUserId();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(userId));
        }

        await base.OnDisconnectedAsync(exception);
    }

    public static string UserGroup(string userId) => $"user-{userId}";

    private string? ResolveUserId()
    {
        if (!string.IsNullOrWhiteSpace(Context.UserIdentifier) && Context.UserIdentifier != "0")
        {
            return Context.UserIdentifier;
        }

        var user = Context.User;
        return user?.FindFirst("userId")?.Value
            ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("nameid")?.Value
            ?? user?.FindFirst("sub")?.Value;
    }
}
