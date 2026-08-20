using Moq;
using SmartHome.Services;
using SmartHome.Model;
using SmartHome.DTO;

using SmartHome.Data.Repositories;

namespace smart_home.UnitTests.Services;

public class DeviceServiceUnitTest {

    private readonly Mock<IDeviceRepository> _deviceRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;


    public DeviceServiceUnitTest() {
        _deviceRepositoryMock = new Mock<IDeviceRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
    }

    [Fact]
    public async Task CreateDeviceAsync_ReturnsZero_WhenDeviceAlreadyExists() {
        // Arrange
        var _service = new DeviceService(_deviceRepositoryMock.Object,_userRepositoryMock.Object);
        var _name = "test device";
        var _user_id = 1;

        var _device = new Device {
            Device_id = 1,
            Device_name = _name,
            User_id = _user_id,
            Device_status = "offline",
            LastSeen = DateTime.Now
        };
        
        var _createDeviceDTO = new CreateDeviceDTO {
            Device_name = _name
        };

        _deviceRepositoryMock
            .Setup(repo => repo.GetDeviceByNameAsync(_name))
            .ReturnsAsync(_device);
 

        // Act

        var result = await _service.CreateDeviceAsync(_createDeviceDTO,_user_id);

        // Assert
        Assert.Equal(1,result);
    }

    [Fact]

    public async Task CreateDeviceAsync_ReturnsZero_WhenUserNotFound() {
        // Arrange 
        var _service = new DeviceService(_deviceRepositoryMock.Object,_userRepositoryMock.Object);
        var _name = "test device";
        var _user_id = 1;

        _userRepositoryMock
            .Setup(repo => repo.GetUserById(_user_id))
            .ReturnsAsync(null as User);
        
        var _createDeviceDTO = new CreateDeviceDTO {
            Device_name = _name
        };

        // Act

        var result = await _service.CreateDeviceAsync(_createDeviceDTO,_user_id);


        // Assert
        Assert.Equal(0,result);
    }

    [Fact]
    public async Task CreateDeviceAsync_ReturnsDeviceId_WhenDeviceCreated() {
        // Arrange
        var _service = new DeviceService(_deviceRepositoryMock.Object,_userRepositoryMock.Object);

        var new_device = new Device {
            Device_name = "test device",
            User_id = 1,
            Device_status = "offline",
        };

        var user = new User {
            Id = 1,
            Username = "test user",
            Email = "test@test.com",
            Password = "testpassword"
        };

        _userRepositoryMock
        .Setup(r => r.GetUserById(1))
        .ReturnsAsync(user);


        _deviceRepositoryMock
        .Setup(r => r.CreateDeviceAsync(It.Is<Device>(d =>
            d.Device_name == new_device.Device_name &&
            d.User_id == new_device.User_id &&
            d.Device_status == new_device.Device_status
        )))
        .ReturnsAsync(1);


        // Act

        var result = await _service.CreateDeviceAsync(new CreateDeviceDTO {
            Device_name = "test device"
        },1);


        // Assert

        Assert.Equal(1,result);
    }

    [Fact]
    public async Task GetDevicesByUserIdAsync_ReturnsListOfDevices() {
        // Arrange
        var _service = new DeviceService(_deviceRepositoryMock.Object,_userRepositoryMock.Object);

        var devices = new List<Device> {
            new Device {
                Device_id = 1,
                Device_name = "test device",
                User_id = 1,
                Device_status = "offline",
                LastSeen = DateTime.Now
            },
            new Device {
                Device_id = 2,
                Device_name = "test device 2",
                User_id = 1,
                Device_status = "offline",
                LastSeen = DateTime.Now
            }
        };

        var _user_id = 1;

        _deviceRepositoryMock
        .Setup(r => r.GetDevicesByUserIdAsync(_user_id))
        .ReturnsAsync(devices);

        // Act

        var result = await _service.GetDevicesByUserIdAsync(_user_id);


        // Assert
        Assert.Equal(devices,result);
        Assert.Equal(devices.Count,result.Count);
    }
}