using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SmartHome.Data.Repositories;
using SmartHome.DTO;

namespace SmartHome.Services;

public class AuthService : IAuthService
{
    private readonly IUserService _UserService;
    private readonly IDeviceRepository _deviceRepository;

    private readonly ISensorRepository _sensorRepository;
    private readonly IConfiguration _config;

    private readonly ILogger<AuthService> _logger;

    public AuthService(
      IUserService service,
      IDeviceRepository deviceRepository,
      ISensorRepository sensorRepository,
      IConfiguration config,
      ILogger<AuthService> logger)
    {
        _UserService = service;
        _deviceRepository = deviceRepository;
        _sensorRepository = sensorRepository;
        _logger = logger;
        _config = config;
    }
    public async Task<AuthResponseDto?> RegisterAsync(RegisterDTO dto)
    {
        var user = await _UserService.CreateUserAsync(dto);
        if(user == null)
            return null;
        var userId = user.Id;
        return GenerateJWTtoken(userId, user.Username, user.Email, false,false);
    }
    public AuthResponseDto GenerateJWTtoken(int userId, string username, string email, bool hasDevices , bool hasSensors)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["key"]));
        var creds = new SigningCredentials(key,SecurityAlgorithms.HmacSha256);
        var ExpirationMintues = Convert.ToDouble(jwtSettings["DurationInMinutes"]);


        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("userId", userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, username),
            new Claim("hasDevices", hasDevices.ToString().ToLower()),
            new Claim("hasSensors", hasSensors.ToString().ToLower()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(ExpirationMintues),
            signingCredentials: creds
        );
        var tokenHandler = new JwtSecurityTokenHandler();
        var AccessToken = tokenHandler.WriteToken(token);

        return new AuthResponseDto
        {
            AccessToken = AccessToken,
            ExpiresAt = token.ValidTo,
            HasDevices = hasDevices,
            HasSensors = hasSensors
        };
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDTO dto)
    {
        var user = await _UserService.GetUserByEmailAsync(dto.Email);

        if(user == null || !BCrypt.Net.BCrypt.Verify(dto.Password,user.Password))
            return null;

        var deviceCount = await _deviceRepository.GetDevicesCountByUserIdAsync(user.Id) ?? 0;
        var hasDevices = deviceCount > 0;

        var sensorsCount = await _sensorRepository.GetSensorsCountByUserId(user.Id) ?? 0;
        var hasSensors = sensorsCount > 0;

        _logger.Log(LogLevel.Information,$"{deviceCount}/{sensorsCount}");


        return GenerateJWTtoken(
          user.Id, user.Username,
          user.Email, hasDevices , hasSensors);

    }

    public Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        throw new NotImplementedException();
    }
}
