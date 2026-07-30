using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using SmartHome.DTO;
using SmartHome.Services;

namespace SmartHome.Controllers;

[ApiController]
[Route("/api/auth/")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _AuthService;
    private readonly IDeviceService _IDevice;
    public AuthController(IAuthService UserService) => _AuthService = UserService;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDTO body)
    {
        var AuthResponce = await _AuthService.RegisterAsync(body);
        if(AuthResponce == null)
            return Conflict(new{Message="Email Already In Use"});
        return Ok(new{message="User Created Succefully",token=AuthResponce.AccessToken});
    }
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDTO body)
    {
        var AuthResponce = await _AuthService.LoginAsync(body);
        if(AuthResponce == null)
            return Unauthorized(new{Message="Invalid Email Or Password"});
        return Ok(new{Message="User Loged in Successfully",token=AuthResponce.AccessToken});
    }

    [HttpGet("google-login")]
    [AllowAnonymous]
    public IActionResult GoogleLogin(){
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback))
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(){
        var authResponse = await _AuthService.LoginWithGoogleAsync(HttpContext);

        if (authResponse== null)
           return Unauthorized(new{ Message =" Google Authentication failed"});

        return Ok(new {
            Message ="Google Login successful",
            Token =authResponse.AccessToken
        }); 
    }}

