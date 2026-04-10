namespace SmartHome.DTO;
public class SingalRDTO
{
    public int DeviceId { get; set; }

    public string DeviceName { get; set; } = string.Empty;

    public int? SensorId { get; set; }

    public string? SensorName { get; set; }

    public string SensorType { get; set; } = string.Empty;

    public double Value { get; set; }

    public DateTime Timestamp { get; set; }

    public string? IpAddress { get; set; }
}
