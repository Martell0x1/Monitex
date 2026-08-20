using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using SmartHome.Controllers;
using SmartHome.DTO;
using SmartHome.Services;

namespace smart_home.UnitTests.Controllers;

public class AuthControllerUnitTest
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly AuthController _sut;

    public AuthControllerUnitTest()
    {
        _sut = new AuthController(_authService.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task Register_ReturnsConflict_WhenEmailIsTaken()
    {
        var dto = ValidRegister();
        _authService.Setup(s => s.RegisterAsync(dto)).Returns(Task.FromResult<AuthResponseDto>(null!));

        var result = await _sut.Register(dto);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(409, conflict.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsOkWithToken_WhenUserIsCreated()
    {
        var dto = ValidRegister();
        _authService.Setup(s => s.RegisterAsync(dto)).ReturnsAsync(new AuthResponseDto { AccessToken = "jwt" });

        var result = await _sut.Register(dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("jwt", ok.Value!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenCredentialsAreInvalid()
    {
        var dto = new LoginDTO { Email = "a@b.com", Password = "Secret1!" };
        _authService.Setup(s => s.LoginAsync(dto)).Returns(Task.FromResult<AuthResponseDto>(null!));

        var result = await _sut.Login(dto);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsOkWithToken_WhenCredentialsMatch()
    {
        var dto = new LoginDTO { Email = "a@b.com", Password = "Secret1!" };
        _authService.Setup(s => s.LoginAsync(dto)).ReturnsAsync(new AuthResponseDto { AccessToken = "jwt" });

        var result = await _sut.Login(dto);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("jwt", ok.Value!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void GoogleLogin_ChallengesGoogleScheme()
    {
        var url = new Mock<IUrlHelper>();
        url.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("/api/auth/google-callback");
        _sut.Url = url.Object;

        var result = _sut.GoogleLogin();

        var challenge = Assert.IsType<ChallengeResult>(result);
        Assert.Contains(GoogleDefaults.AuthenticationScheme, challenge.AuthenticationSchemes);
        Assert.Equal("/api/auth/google-callback", challenge.Properties?.RedirectUri);
    }

    [Fact]
    public async Task GoogleCallback_ReturnsUnauthorized_WhenGoogleLoginFails()
    {
        _authService.Setup(s => s.LoginWithGoogleAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync((AuthResponseDto?)null);

        var result = await _sut.GoogleCallback();

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GoogleCallback_ReturnsOkWithToken_WhenGoogleLoginSucceeds()
    {
        _authService.Setup(s => s.LoginWithGoogleAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(new AuthResponseDto { AccessToken = "google-jwt" });

        var result = await _sut.GoogleCallback();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("google-jwt", ok.Value!.ToString(), StringComparison.Ordinal);
    }

    private static RegisterDTO ValidRegister() => new()
    {
        Username = "martell",
        Email = "martell@example.com",
        Password = "Secret1!"
    };
}
