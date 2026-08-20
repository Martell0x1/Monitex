using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartHome.Controllers;
using SmartHome.DTO;
using SmartHome.Model;
using SmartHome.Services;
using smart_home.UnitTests.TestSupport;

namespace smart_home.UnitTests.Controllers;

public class DeviceControllerUnitTest
{
    private readonly Mock<IDeviceService> _deviceService = new();
    private readonly DeviceController _sut;

    public DeviceControllerUnitTest()
    {
        _sut = new DeviceController(_deviceService.Object);
    }

    [Fact]
    public void SayHello_ReturnsHello()
    {
        Assert.Equal("Hello", _sut.sayHello());
    }

    [Fact]
    public async Task Create_Device_ReturnsUnauthorized_WhenUserIdClaimIsMissing()
    {
        _sut.WithUser();

        var result = await _sut.Create_Device(new CreateDeviceDTO { Device_name = "hub-1" });

        Assert.IsType<UnauthorizedObjectResult>(result);
        _deviceService.Verify(s => s.CreateDeviceAsync(It.IsAny<CreateDeviceDTO>(), It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData("userId")]
    [InlineData(ClaimTypes.NameIdentifier)]
    [InlineData("nameid")]
    [InlineData("sub")]
    public async Task Create_Device_AcceptsKnownUserIdClaimTypes(string claimType)
    {
        _sut.WithUser(new Claim(claimType, "9"));
        _deviceService.Setup(s => s.CreateDeviceAsync(It.IsAny<CreateDeviceDTO>(), 9)).ReturnsAsync(41);

        var result = await _sut.Create_Device(new CreateDeviceDTO { Device_name = "hub-1" });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("41", ok.Value!.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_Device_ReturnsConflict_WhenServiceRejectsCreate()
    {
        _sut.WithUser(new Claim("userId", "9"));
        _deviceService.Setup(s => s.CreateDeviceAsync(It.IsAny<CreateDeviceDTO>(), 9)).ReturnsAsync(0);

        var result = await _sut.Create_Device(new CreateDeviceDTO { Device_name = "hub-1" });

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task GetDevicesByUserId_ReturnsDevices()
    {
        _deviceService.Setup(s => s.GetDevicesByUserIdAsync(9)).ReturnsAsync(
        [
            new Device { Device_id = 1, User_id = 9, Device_name = "hub-1", Device_status = "offline" }
        ]);

        var result = await _sut.GetDevicesByUserId(9);

        var ok = Assert.IsType<OkObjectResult>(result);
        var devices = Assert.IsType<List<Device>>(ok.Value);
        Assert.Equal("hub-1", Assert.Single(devices).Device_name);
    }
}
