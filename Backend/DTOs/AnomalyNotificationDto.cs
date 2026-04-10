using System.Text.Json.Serialization;

namespace SmartHome.DTO;

public class AnomalyNotificationDto
{
    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = default!;

    [JsonPropertyName("sensorId")]
    public string? SensorId { get; set; }

    [JsonPropertyName("sensorName")]
    public string? SensorName { get; set; }

    [JsonPropertyName("sensorType")]
    public string? SensorType { get; set; }

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "warning";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "Anomaly detected";

    [JsonPropertyName("message")]
    public string Message { get; set; } = default!;

    [JsonPropertyName("value")]
    public double? Value { get; set; }

    [JsonPropertyName("threshold")]
    public double? Threshold { get; set; }

    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
