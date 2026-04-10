namespace SmartHome.DTO;

public class SensorSummaryDto
{
    public int SensorId { get; set; }

    public int DeviceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}
