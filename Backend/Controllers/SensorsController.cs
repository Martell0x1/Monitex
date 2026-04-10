using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHome.DTO;
using SmartHome.Services;

namespace SmartHome.Controllers;

[ApiController]
[Authorize]
[Route("/api/sensors")]
public class SensorsController : ControllerBase
{
    private readonly ISensorService _sensorService;

    public SensorsController(ISensorService sensorService)
    {
        _sensorService = sensorService;
    }

    [HttpGet("device/{deviceId:int}")]
    public async Task<IActionResult> GetSensorsByDevice(int deviceId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Invalid user token" });

        try
        {
            var sensors = await _sensorService.GetSensorsByDeviceAsync(deviceId, userId);
            return Ok(sensors);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> RegisterSensors([FromBody] List<CreateSensorDto> sensors)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized(new { message = "Invalid user token" });

        if (sensors == null || sensors.Count == 0)
            return BadRequest(new { message = "Sensors list cannot be empty" });

        try
        {
            foreach (var sensor in sensors)
            {
                await _sensorService.Create(sensor, userId);
            }

            return Ok(new { message = "Sensors registered successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
