using System.ComponentModel.DataAnnotations;

namespace SmartHome.Model;
public class SensorReading
{
    [Required]
    public int Id { get; set; }

    public bool MotionDetected { get; set; }
    public int LightLevel { get; set; }

    [Required]
    public DateTime Timestamp { get; set; }

    [Required]
    public int DeviceId { get; set; }

    [Required]
    public Device Device { get; set; }
}
