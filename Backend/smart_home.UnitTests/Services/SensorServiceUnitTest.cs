using Moq;
using SmartHome.Data.Repositories;
using SmartHome.DTO;
using SmartHome.Model;
using SmartHome.Services;

namespace smart_home.UnitTests.Services;

public class SensorServiceUnitTest
{
    private readonly Mock<ISensorRepository> _sensorRepository = new();
    private readonly Mock<IDeviceRepository> _deviceRepository = new();
    private readonly SensorService _sut;

    public SensorServiceUnitTest()
    {
        _sut = new SensorService(_sensorRepository.Object, _deviceRepository.Object);
    }

    [Fact]
    public async Task Create_Throws_WhenUserHasNoDevice()
    {
        _deviceRepository.Setup(r => r.GetLatestDeviceByUserAsync(9)).ReturnsAsync((Device?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.Create(new CreateSensorDto { Name = "temp", Type = "temperature", Location = "kitchen" }, 9));

        _sensorRepository.Verify(r => r.CreateSensorAsync(It.IsAny<Sensor>()), Times.Never);
    }

    [Fact]
    public async Task Create_PersistsSensorOnLatestDevice_AndNullsBlankOptionalFields()
    {
        _deviceRepository.Setup(r => r.GetLatestDeviceByUserAsync(9)).ReturnsAsync(new Device
        {
            Device_id = 15,
            User_id = 9,
            Device_name = "hub-1",
            Device_status = "online"
        });
        _sensorRepository.Setup(r => r.CreateSensorAsync(It.IsAny<Sensor>())).ReturnsAsync(88);

        var result = await _sut.Create(new CreateSensorDto
        {
            Name = "temp",
            Type = "temperature",
            Location = "kitchen",
            IpAddress = "   ",
            Description = ""
        }, 9);

        Assert.Equal(88, result);
        _sensorRepository.Verify(r => r.CreateSensorAsync(It.Is<Sensor>(s =>
            s.Device_id == 15 &&
            s.Name == "temp" &&
            s.Type == "temperature" &&
            s.Location == "kitchen" &&
            s.IpAddress == null &&
            s.Description == null)), Times.Once);
    }

    [Fact]
    public async Task GetSensorsByDeviceAsync_Throws_WhenDeviceDoesNotBelongToUser()
    {
        _deviceRepository.Setup(r => r.GetDeviceByIdForUserAsync(15, 9)).ReturnsAsync((Device?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.GetSensorsByDeviceAsync(15, 9));
        _sensorRepository.Verify(r => r.GetSensorsByDeviceAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetSensorsByDeviceAsync_MapsSensorsToSummaries()
    {
        var createdAt = new DateTime(2026, 8, 20, 12, 0, 0, DateTimeKind.Utc);
        _deviceRepository.Setup(r => r.GetDeviceByIdForUserAsync(15, 9)).ReturnsAsync(new Device
        {
            Device_id = 15,
            User_id = 9,
            Device_name = "hub-1",
            Device_status = "online"
        });
        _sensorRepository.Setup(r => r.GetSensorsByDeviceAsync(15)).ReturnsAsync(
        [
            new Sensor
            {
                Sensor_id = 3,
                Device_id = 15,
                Name = "temp",
                Type = "temperature",
                Location = "kitchen",
                IpAddress = "10.0.0.8",
                Description = "oven",
                Created_at = createdAt
            }
        ]);

        var result = await _sut.GetSensorsByDeviceAsync(15, 9);

        var summary = Assert.Single(result);
        Assert.Equal(3, summary.SensorId);
        Assert.Equal(15, summary.DeviceId);
        Assert.Equal("temp", summary.Name);
        Assert.Equal("temperature", summary.Type);
        Assert.Equal("kitchen", summary.Location);
        Assert.Equal("10.0.0.8", summary.IpAddress);
        Assert.Equal("oven", summary.Description);
        Assert.Equal(createdAt, summary.CreatedAt);
    }
}
