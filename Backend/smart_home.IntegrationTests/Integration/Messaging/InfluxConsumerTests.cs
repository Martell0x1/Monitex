using System.Text.Json;
using SmartHome.DTO;

namespace smart_home.IntegrationTests.Integration.Messaging;

public class InfluxConsumerTests
{
	[Fact]
	public void SensorPayload_DeserializesCaseInsensitively()
	{
		var payload = JsonSerializer.Deserialize<InfluxSensorReadingMessage>(
			"{\"DeviceName\":\"living-room\",\"SensorType\":\"temperature\",\"Value\":21.5}",
			new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

		Assert.NotNull(payload);
		Assert.Equal("living-room", payload.DeviceName);
		Assert.Equal("temperature", payload.SensorType);
		Assert.Equal(21.5, payload.Value);
	}
}
