using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHome.DTO;
using SmartHome.Services;

namespace SmartHome.Controllers;

[ApiController]
[Authorize]
[Route("/api/device")]
public class DeviceController : ControllerBase
{
  private readonly IDeviceService _Service;
  public DeviceController(IDeviceService deviceService) => _Service = deviceService;
  [HttpGet]
  public string sayHello()=> "Hello";

  [HttpPost("create")]
  public async Task<IActionResult> Create_Device([FromBody] CreateDeviceDTO body)
  {
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

    if (!int.TryParse(userIdClaim, out var userId))
        return Unauthorized(new { message = "Invalid user token" });

    var result = await _Service.CreateDeviceAsync(body,userId);
    if(result == null)
      return Conflict(new{message="Failed To Register a new device"});
    return Ok(new{message="Device Created Succefully",name=body.Device_name});
  }

  [HttpGet("user/{id}")]
  public async Task<IActionResult> GetDevicesByUserId(int id)
  {
    var devices = await _Service.GetDevicesByUserIdAsync(id);
    return Ok(devices);
  }

}
