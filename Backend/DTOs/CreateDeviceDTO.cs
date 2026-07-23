using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHome.DTO;

public class CreateDeviceDTO
{
  [Required]
  [JsonPropertyName("device_name")]
  public string Device_name { set; get; } = string.Empty;
}
