using System.Text.Json.Serialization;

namespace SmartHome.DTO;

public class InfluxSensorReadingMessage
{
    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = default!;

    [JsonPropertyName("sensorType")]
    public string SensorType { get; set; } = default!;

    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; }
}
