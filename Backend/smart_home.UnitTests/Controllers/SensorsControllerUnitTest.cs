using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartHome.Controllers;
using SmartHome.DTO;
using SmartHome.Services;
using smart_home.UnitTests.TestSupport;

namespace smart_home.UnitTests.Controllers;

public class SensorsControllerUnitTest
{
    private readonly Mock<ISensorService> _sensorService = new();
    private readonly SensorsController _sut;

    public SensorsControllerUnitTest()
    {
        _sut = new SensorsController(_sensorService.Object);
    }

    [Fact]
    public async Task GetSensorsByDevice_ReturnsUnauthorized_WhenTokenHasNoUserId()
    {
        _sut.WithUser();

        var result = await _sut.GetSensorsByDevice(3);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetSensorsByDevice_ReturnsNotFound_WhenServiceThrowsInvalidOperation()
    {
        _sut.WithUser(new Claim("userId", "9"));
        _sensorService.Setup(s => s.GetSensorsByDeviceAsync(3, 9))
            .ThrowsAsync(new InvalidOperationException("Device was not found for this user."));

        var result = await _sut.GetSensorsByDevice(3);

        var notFound = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains("not found", notFound.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetSensorsByDevice_ReturnsOk_WhenSensorsExist()
    {
        _sut.WithUser(new Claim("sub", "9"));
        _sensorService.Setup(s => s.GetSensorsByDeviceAsync(3, 9)).ReturnsAsync(
        [
            new SensorSummaryDto { SensorId = 1, Name = "temp", Type = "temperature", Location = "kitchen" }
        ]);

        var result = await _sut.GetSensorsByDevice(3);

        var ok = Assert.IsType<OkObjectResult>(result);
        var sensors = Assert.IsType<List<SensorSummaryDto>>(ok.Value);
        Assert.Equal("temp", Assert.Single(sensors).Name);
    }

    [Fact]
    public async Task RegisterSensors_ReturnsBadRequest_WhenListIsEmpty()
    {
        _sut.WithUser(new Claim("userId", "9"));

        var result = await _sut.RegisterSensors([]);

        Assert.IsType<BadRequestObjectResult>(result);
        _sensorService.Verify(s => s.Create(It.IsAny<CreateSensorDto>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RegisterSensors_ReturnsBadRequest_WhenCreateThrows()
    {
        _sut.WithUser(new Claim("userId", "9"));
        _sensorService.Setup(s => s.Create(It.IsAny<CreateSensorDto>(), 9))
            .ThrowsAsync(new InvalidOperationException("No device found for this user."));

        var result = await _sut.RegisterSensors(
        [
            new CreateSensorDto { Name = "temp", Type = "temperature", Location = "kitchen" }
        ]);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RegisterSensors_CreatesEverySensor_WhenListIsValid()
    {
        _sut.WithUser(new Claim("nameid", "9"));
        _sensorService.Setup(s => s.Create(It.IsAny<CreateSensorDto>(), 9)).ReturnsAsync(1);

        var result = await _sut.RegisterSensors(
        [
            new CreateSensorDto { Name = "temp", Type = "temperature", Location = "kitchen" },
            new CreateSensorDto { Name = "hum", Type = "humidity", Location = "kitchen" }
        ]);

        Assert.IsType<OkObjectResult>(result);
        _sensorService.Verify(s => s.Create(It.IsAny<CreateSensorDto>(), 9), Times.Exactly(2));
    }
}
