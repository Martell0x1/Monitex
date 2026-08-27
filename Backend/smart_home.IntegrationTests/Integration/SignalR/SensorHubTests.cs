using SmartHome.Infrastructure.Hubs;

namespace smart_home.IntegrationTests.Integration.SignalR;

public class SensorHubTests
{
	[Theory]
	[InlineData("1", "user-1")]
	[InlineData("42", "user-42")]
	public void UserGroup_UsesStableUserGroupName(string userId, string expected)
	{
		Assert.Equal(expected, SensorHub.UserGroup(userId));
	}
}
