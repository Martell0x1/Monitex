namespace SmartHome.DTO;

public class DeviceHealthSignalRDto
{
    public int DeviceId { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public int Score { get; set; }

    public string State { get; set; } = string.Empty;

    public List<string> Reasons { get; set; } = [];

    public int WifiRssi { get; set; }

    public uint FreeHeapBytes { get; set; }

    public uint MinFreeHeapBytes { get; set; }

    public ulong UptimeSeconds { get; set; }

    public string RestartReason { get; set; } = string.Empty;

    public bool MqttConnected { get; set; }

    public bool LastSensorReadOk { get; set; }

    public DateTime Timestamp { get; set; }
}
