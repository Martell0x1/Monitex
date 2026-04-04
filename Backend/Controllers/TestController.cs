using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SmartHome.Controllers;

[ApiController]
[Route("/api/test")]
public class TestController : ControllerBase
{
  [HttpGet]
  [Authorize]
  public string Hello() => "Hello";
}
