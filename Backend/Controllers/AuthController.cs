using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartHome.DTO;
using SmartHome.Services;

namespace SmartHome.Controllers;

[ApiController]
[Route("/api/auth/")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _AuthService;
    public AuthController(IAuthService UserService) => _AuthService = UserService;

    [Route("register")]
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterDTO body)
    {
        var AuthResponce = await _AuthService.RegisterAsync(body);
        if(AuthResponce == null)
            return Conflict(new{Message="Email Already In Use"});
        return Ok(new{message="User Created Succefully",token=AuthResponce.AccessToken});
    }
    [Route("login")]
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginDTO body)
    {
        var AuthResponce = await _AuthService.LoginAsync(body);
        if(AuthResponce == null)
            return Unauthorized(new{Message="Invalid Email Or Password"});
        return Ok(new{Message="User Loged in Successfully",token=AuthResponce.AccessToken});
    }
}