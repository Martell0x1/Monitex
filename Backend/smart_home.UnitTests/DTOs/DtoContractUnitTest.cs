using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using SmartHome.DTO;

namespace smart_home.UnitTests.DTOs;

public class DtoContractUnitTest
{
    [Fact]
    public void RegisterDTO_AcceptsPasswordThatMatchesPolicy()
    {
        var dto = new RegisterDTO
        {
            Username = "martell",
            Email = "martell@example.com",
            Password = "Secret1!"
        };

        Assert.Empty(Validate(dto));
    }

    [Theory]
    [InlineData("secret1!")]
    [InlineData("Secret!!")]
    [InlineData("Ab1!")]
    [InlineData("not-an-email")]
    public void RegisterDTO_RejectsInvalidValues(string invalid)
    {
        var dto = new RegisterDTO
        {
            Username = "martell",
            Email = invalid.Contains('@') || invalid == "not-an-email" ? invalid : "martell@example.com",
            Password = invalid.Contains('@') || invalid == "not-an-email" ? "Secret1!" : invalid
        };

        Assert.NotEmpty(Validate(dto));
    }

    [Fact]
    public void InfluxSensorReadingMessage_RoundTripsCamelCaseJson()
    {
        const string json = """
            {"deviceName":"hub-1","sensorType":"temperature","value":21.5,"timestamp":"2026-08-20T12:00:00Z","ipAddress":"10.0.0.8"}
            """;

        var message = JsonSerializer.Deserialize<InfluxSensorReadingMessage>(json);

        Assert.NotNull(message);
        Assert.Equal("hub-1", message.DeviceName);
        Assert.Equal("temperature", message.SensorType);
        Assert.Equal(21.5, message.Value);
        Assert.Equal("10.0.0.8", message.IpAddress);
    }

    [Fact]
    public void DeviceHealthHeartbeatMessage_ReadsMessageType()
    {
        const string json = """
            {"messageType":"health","deviceName":"hub-1","wifiRssi":-50,"freeHeapBytes":80000,"minFreeHeapBytes":40000,"uptimeSeconds":3600,"mqttConnected":true,"lastSensorReadOk":true}
            """;

        var heartbeat = JsonSerializer.Deserialize<DeviceHealthHeartbeatMessage>(json);

        Assert.NotNull(heartbeat);
        Assert.Equal("health", heartbeat.MessageType);
        Assert.True(heartbeat.MqttConnected);
    }

    [Fact]
    public void AnomalyNotificationDto_UsesDefaultSeverityAndTitle()
    {
        var dto = JsonSerializer.Deserialize<AnomalyNotificationDto>("""{"deviceName":"hub-1","message":"spike"}""")!;

        Assert.Equal("warning", dto.Severity);
        Assert.Equal("Anomaly detected", dto.Title);
        Assert.Equal("spike", dto.Message);
    }

    private static List<ValidationResult> Validate(object instance)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, true);
        return results;
    }
}
