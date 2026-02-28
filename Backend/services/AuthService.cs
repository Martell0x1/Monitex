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
    private readonly IConfiguration _config;

    public AuthService(IUserService service , IConfiguration config)
    {
        _UserService = service;
        _config = config;
    }
    public async Task<AuthResponseDto?> RegisterAsync(RegisterDTO dto)
    {
        var user = await _UserService.CreateUserAsync(dto);
        if(user == null)
            return null;
        var userId = user.Id;
        return GenerateJWTtoken(userId,user.Username,user.Email);
    }
    public AuthResponseDto GenerateJWTtoken(int userId , string username ,string email)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["key"]));
        var creds = new SigningCredentials(key,SecurityAlgorithms.HmacSha256);
        var ExpirationMintues = Convert.ToDouble(jwtSettings["DurationInMinutes"]);


        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, username),
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
            ExpiresAt = token.ValidTo
        };
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDTO dto)
    {
        var user = await _UserService.GetUserByEmailAsync(dto.Email);
        
        if(user == null || !BCrypt.Net.BCrypt.Verify(dto.Password,user.Password)) 
            return null;

        return GenerateJWTtoken(user.Id,user.Username,user.Email);

    }

    public Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
    {
        throw new NotImplementedException();
    }
}