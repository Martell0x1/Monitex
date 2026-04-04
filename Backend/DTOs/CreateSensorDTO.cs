using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SmartHome.DTO;

public class CreateSensorDto
{
    [Required]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("ipAddress")]
    public string? IpAddress { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
