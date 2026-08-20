using System.Security.Claims;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using SmartHome.Infrastructure.Hubs;

namespace smart_home.UnitTests.Hubs;

public class MonitexUserIdProviderUnitTest
{
    private readonly MonitexUserIdProvider _sut = new();

    [Theory]
    [InlineData("userId", "12")]
    [InlineData("nameid", "8")]
    [InlineData("sub", "3")]
    public void GetUserId_ReadsPreferredClaimTypes(string claimType, string value)
    {
        var connection = CreateConnection(new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(claimType, value)
        ], "Test")));

        Assert.Equal(value, _sut.GetUserId(connection));
    }

    [Fact]
    public void GetUserId_IgnoresNonPositiveIds()
    {
        var connection = CreateConnection(new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("userId", "0"),
            new Claim("customUser", "21")
        ], "Test")));

        Assert.Equal("21", _sut.GetUserId(connection));
    }

    [Fact]
    public void GetUserId_ReturnsNull_WhenNoNumericUserClaimExists()
    {
        var connection = CreateConnection(new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, "a@b.com")
        ], "Test")));

        Assert.Null(_sut.GetUserId(connection));
    }

    [Fact]
    public void GetUserId_ReturnsNull_WhenUserHasNoClaims()
    {
        var connection = CreateConnection(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.Null(_sut.GetUserId(connection));
    }

    private static HubConnectionContext CreateConnection(ClaimsPrincipal user)
    {
        var connection = new DefaultConnectionContext("test-connection")
        {
            User = user
        };

        return new HubConnectionContext(
            connection,
            new HubConnectionContextOptions { KeepAliveInterval = TimeSpan.FromSeconds(15) },
            NullLoggerFactory.Instance);
    }
}
