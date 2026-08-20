using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SmartHome.Data.Repositories;
using SmartHome.DTO;
using SmartHome.Model;
using SmartHome.Services;

namespace smart_home.UnitTests.Services;

public class AuthServiceUnitTest
{
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IDeviceRepository> _deviceRepository = new();
    private readonly Mock<ISensorRepository> _sensorRepository = new();
    private readonly Mock<ILogger<AuthService>> _logger = new();
    private readonly AuthService _sut;

    public AuthServiceUnitTest()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:key"] = "unit-test-signing-key-that-is-long-enough",
                ["Jwt:DurationInMinutes"] = "30",
                ["Jwt:Issuer"] = "monitex-tests",
                ["Jwt:Audience"] = "monitex-clients"
            })
            .Build();

        _sut = new AuthService(
            _userService.Object,
            _deviceRepository.Object,
            _sensorRepository.Object,
            config,
            _logger.Object);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsNull_WhenUserCannotBeCreated()
    {
        var dto = new RegisterDTO { Username = "martell", Email = "a@b.com", Password = "Secret1!" };
        _userService.Setup(s => s.CreateUserAsync(dto)).Returns(Task.FromResult<User>(null!));

        var result = await _sut.RegisterAsync(dto);

        Assert.Null(result);
    }

    [Fact]
    public async Task RegisterAsync_ReturnsTokenWithoutDeviceOrSensorFlags()
    {
        var dto = new RegisterDTO { Username = "martell", Email = "a@b.com", Password = "Secret1!" };
        _userService.Setup(s => s.CreateUserAsync(dto)).ReturnsAsync(new User
        {
            Id = 11,
            Username = "martell",
            Email = "a@b.com",
            Password = "hash"
        });

        var result = await _sut.RegisterAsync(dto);

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(result.HasDevices);
        Assert.False(result.HasSensors);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenUserIsMissing()
    {
        _userService.Setup(s => s.GetUserByEmailAsync("a@b.com")).ReturnsAsync((User?)null);

        var result = await _sut.LoginAsync(new LoginDTO { Email = "a@b.com", Password = "Secret1!" });

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ReturnsNull_WhenPasswordDoesNotMatch()
    {
        _userService.Setup(s => s.GetUserByEmailAsync("a@b.com")).ReturnsAsync(new User
        {
            Id = 4,
            Username = "martell",
            Email = "a@b.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Other1!")
        });

        var result = await _sut.LoginAsync(new LoginDTO { Email = "a@b.com", Password = "Secret1!" });

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginAsync_ReturnsTokenWithDeviceAndSensorFlags()
    {
        _userService.Setup(s => s.GetUserByEmailAsync("a@b.com")).ReturnsAsync(new User
        {
            Id = 4,
            Username = "martell",
            Email = "a@b.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Secret1!")
        });
        _deviceRepository.Setup(r => r.GetDevicesCountByUserIdAsync(4)).ReturnsAsync(2);
        _sensorRepository.Setup(r => r.GetSensorsCountByUserId(4)).ReturnsAsync(3);

        var result = await _sut.LoginAsync(new LoginDTO { Email = "a@b.com", Password = "Secret1!" });

        Assert.NotNull(result);
        Assert.True(result.HasDevices);
        Assert.True(result.HasSensors);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        Assert.Equal("4", jwt.Claims.First(c => c.Type == "userId").Value);
        Assert.Equal("true", jwt.Claims.First(c => c.Type == "hasDevices").Value);
        Assert.Equal("true", jwt.Claims.First(c => c.Type == "hasSensors").Value);
    }

    [Fact]
    public async Task LoginAsync_TreatsNullCountsAsZero()
    {
        _userService.Setup(s => s.GetUserByEmailAsync("a@b.com")).ReturnsAsync(new User
        {
            Id = 4,
            Username = "martell",
            Email = "a@b.com",
            Password = BCrypt.Net.BCrypt.HashPassword("Secret1!")
        });
        _deviceRepository.Setup(r => r.GetDevicesCountByUserIdAsync(4)).ReturnsAsync((int?)null);
        _sensorRepository.Setup(r => r.GetSensorsCountByUserId(4)).ReturnsAsync((int?)null);

        var result = await _sut.LoginAsync(new LoginDTO { Email = "a@b.com", Password = "Secret1!" });

        Assert.NotNull(result);
        Assert.False(result.HasDevices);
        Assert.False(result.HasSensors);
    }

    [Fact]
    public async Task LoginWithGoogleAsync_ReturnsNull_WhenAuthenticationFails()
    {
        var context = CreateHttpContext(AuthenticateResult.Fail("no ticket"));

        var result = await _sut.LoginWithGoogleAsync(context);

        Assert.Null(result);
        _userService.Verify(s => s.GetUserByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginWithGoogleAsync_ReturnsNull_WhenEmailOrNameIsMissing()
    {
        var identity = new ClaimsIdentity("Google");
        identity.AddClaim(new Claim(ClaimTypes.Email, "user@gmail.com"));
        var context = CreateHttpContext(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), "Google")));

        var result = await _sut.LoginWithGoogleAsync(context);

        Assert.Null(result);
    }

    [Fact]
    public async Task LoginWithGoogleAsync_CreatesUser_WhenEmailIsNew()
    {
        var identity = new ClaimsIdentity("Google");
        identity.AddClaim(new Claim(ClaimTypes.Email, "user@gmail.com"));
        identity.AddClaim(new Claim(ClaimTypes.Name, "Google User"));
        var context = CreateHttpContext(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), "Google")));

        _userService.Setup(s => s.GetUserByEmailAsync("user@gmail.com")).ReturnsAsync((User?)null);
        _userService.Setup(s => s.CreateGoogleUserAsync("Google User", "user@gmail.com")).ReturnsAsync(new User
        {
            Id = 22,
            Username = "Google User",
            Email = "user@gmail.com",
            Password = string.Empty
        });
        _deviceRepository.Setup(r => r.GetDevicesCountByUserIdAsync(22)).ReturnsAsync(0);
        _sensorRepository.Setup(r => r.GetSensorsCountByUserId(22)).ReturnsAsync(1);

        var result = await _sut.LoginWithGoogleAsync(context);

        Assert.NotNull(result);
        Assert.False(result.HasDevices);
        Assert.True(result.HasSensors);
        _userService.Verify(s => s.CreateGoogleUserAsync("Google User", "user@gmail.com"), Times.Once);
    }

    [Fact]
    public void GenerateJWTtoken_IncludesExpectedClaims()
    {
        var response = _sut.GenerateJWTtoken(7, "martell", "a@b.com", true, false);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken);
        Assert.Equal("monitex-tests", jwt.Issuer);
        Assert.Contains("monitex-clients", jwt.Audiences);
        Assert.Equal("7", jwt.Subject);
        Assert.Equal("a@b.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email || c.Type == ClaimTypes.Email).Value);
        Assert.True(response.HasDevices);
        Assert.False(response.HasSensors);
        Assert.Equal(response.ExpiresAt, jwt.ValidTo);
    }

    [Fact]
    public async Task RefreshTokenAsync_ThrowsNotImplemented()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() => _sut.RefreshTokenAsync("token"));
    }

    private static DefaultHttpContext CreateHttpContext(AuthenticateResult result)
    {
        var authentication = new Mock<IAuthenticationService>();
        authentication
            .Setup(s => s.AuthenticateAsync(It.IsAny<HttpContext>(), It.IsAny<string?>()))
            .ReturnsAsync(result);

        var services = new ServiceCollection();
        services.AddSingleton(authentication.Object);

        return new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }
}
