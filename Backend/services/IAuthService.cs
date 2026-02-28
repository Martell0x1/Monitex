using SmartHome.DTO;
namespace SmartHome.Services;


public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDTO dto);
    Task<AuthResponseDto> LoginAsync(LoginDTO dto);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);

};