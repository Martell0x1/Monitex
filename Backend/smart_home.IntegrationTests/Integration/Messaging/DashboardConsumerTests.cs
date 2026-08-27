using System.Text.Json;
using SmartHome.DTO;

namespace smart_home.IntegrationTests.Integration.Messaging;

public class DashboardConsumerTests
{
	[Fact]
	public void HealthPayload_PreservesMessageTypeAndDeviceName()
	{
		var payload = JsonSerializer.Deserialize<DeviceHealthHeartbeatMessage>(
			"{\"messageType\":\"health\",\"deviceName\":\"gateway\"}",
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		Assert.NotNull(payload);
		Assert.Equal("health", payload.MessageType);
		Assert.Equal("gateway", payload.DeviceName);
	}
}
