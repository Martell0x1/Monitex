using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartHome.Infrastructure.Hubs;

namespace smart_home.UnitTests.Hubs;

public class SensorHubUnitTest
{
    [Fact]
    public void UserGroup_PrefixesUserId()
    {
        Assert.Equal("user-42", SensorHub.UserGroup("42"));
    }

    [Fact]
    public async Task OnConnectedAsync_AddsConnectionToUserGroup_WhenUserIdentifierIsPresent()
    {
        var groups = new Mock<IGroupManager>();
        var hub = CreateHub(groups, userIdentifier: "9", connectionId: "conn-1");

        await hub.OnConnectedAsync();

        groups.Verify(g => g.AddToGroupAsync("conn-1", "user-9", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_UsesUserIdClaim_WhenUserIdentifierIsZero()
    {
        var groups = new Mock<IGroupManager>();
        var hub = CreateHub(
            groups,
            userIdentifier: "0",
            connectionId: "conn-2",
            claims: [new Claim("userId", "15")]);

        await hub.OnConnectedAsync();

        groups.Verify(g => g.AddToGroupAsync("conn-2", "user-15", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_DoesNotJoinGroup_WhenUserCannotBeResolved()
    {
        var groups = new Mock<IGroupManager>();
        var hub = CreateHub(groups, userIdentifier: null, connectionId: "conn-3");

        await hub.OnConnectedAsync();

        groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task OnDisconnectedAsync_RemovesConnectionFromUserGroup()
    {
        var groups = new Mock<IGroupManager>();
        var hub = CreateHub(groups, userIdentifier: "9", connectionId: "conn-1");

        await hub.OnDisconnectedAsync(null);

        groups.Verify(g => g.RemoveFromGroupAsync("conn-1", "user-9", It.IsAny<CancellationToken>()), Times.Once);
    }

    private static SensorHub CreateHub(
        Mock<IGroupManager> groups,
        string? userIdentifier,
        string connectionId,
        Claim[]? claims = null)
    {
        var context = new Mock<HubCallerContext>();
        context.SetupGet(c => c.ConnectionId).Returns(connectionId);
        context.SetupGet(c => c.UserIdentifier).Returns(userIdentifier);
        context.SetupGet(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(claims ?? [], "Test")));

        return new SensorHub(NullLogger<SensorHub>.Instance)
        {
            Context = context.Object,
            Groups = groups.Object
        };
    }
}
