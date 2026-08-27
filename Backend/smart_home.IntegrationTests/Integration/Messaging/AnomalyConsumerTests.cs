using System.Text.Json;
using SmartHome.DTO;

namespace smart_home.IntegrationTests.Integration.Messaging;

public class AnomalyConsumerTests
{
	[Fact]
	public void AnomalyPayload_DeserializesOptionalDeviceId()
	{
		var payload = JsonSerializer.Deserialize<AnomalyNotificationDto>(
			"{\"deviceName\":\"gateway\",\"message\":\"high temperature\",\"severity\":\"HIGH\"}",
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		Assert.NotNull(payload);
		Assert.Equal("gateway", payload.DeviceName);
		Assert.Equal("high temperature", payload.Message);
		Assert.Null(payload.DeviceId);
		Assert.Equal("HIGH", payload.Severity);
	}
}
