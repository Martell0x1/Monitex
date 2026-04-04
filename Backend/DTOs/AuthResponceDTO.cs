namespace SmartHome.DTO;
public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? RefreshToken { get; set; }
    public bool HasDevices { get; set; }

    public bool HasSensors {get ; set;}
}
