using System.ComponentModel.DataAnnotations;

namespace SmartHome.Model;

public class Sensor
{
    [Required]
    public int Sensor_id { get; set; }

    [Required]
    public int Device_id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    [Required]
    public string Location { get; set; } = string.Empty;

    public string? IpAddress { get; set; }

    public string? Description { get; set; }

    public DateTime Created_at { get; set; }
}
