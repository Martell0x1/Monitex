using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using SmartHome.Data.Repositories;
using SmartHome.Model;
using SmartHome.Services;

namespace SmartHome.Controllers;


[ApiController]
[Route("/api/async/users")]
public class UserController
{
    private readonly IUserService _userService;

    public UserController(IUserService userService) => _userService = userService; 

    [HttpGet("hello")]
    public string Hello() => "Hello";

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return new NotFoundResult();
        return new OkObjectResult(user);
    }
    
}