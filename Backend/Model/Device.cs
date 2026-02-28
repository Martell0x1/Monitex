using System.ComponentModel.DataAnnotations;

namespace SmartHome.Model;

public class Device
{
    [Required]
    public int Id { get; set; }

    [Required]
    public string DeviceIdentifier { get; set; }
    public string Name { get; set; }

    [Required]
    public int HomeId { get; set; }
    [Required]
    public Home Home { get; set; }

    [Required]
    public List<SensorReading> SensorReadings { get; set; }
}
