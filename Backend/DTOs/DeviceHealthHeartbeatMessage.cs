using System.Text.Json.Serialization;

namespace SmartHome.DTO;

public class DeviceHealthHeartbeatMessage
{
    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = "health";

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("uptimeSeconds")]
    public ulong UptimeSeconds { get; set; }

    [JsonPropertyName("wifiRssi")]
    public int WifiRssi { get; set; }

    [JsonPropertyName("freeHeapBytes")]
    public uint FreeHeapBytes { get; set; }

    [JsonPropertyName("minFreeHeapBytes")]
    public uint MinFreeHeapBytes { get; set; }

    [JsonPropertyName("restartReason")]
    public string RestartReason { get; set; } = string.Empty;

    [JsonPropertyName("mqttConnected")]
    public bool MqttConnected { get; set; }

    [JsonPropertyName("lastSensorReadOk")]
    public bool LastSensorReadOk { get; set; }
}
