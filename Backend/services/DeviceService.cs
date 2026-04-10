using SmartHome.Data.Repositories;
using SmartHome.DTO;
using SmartHome.Model;
using SmartHome.Services;

public class DeviceService : IDeviceService
{
  private readonly IDeviceRepository _Repository;
  private readonly IUserRepository _UserRepository;
  public DeviceService(IDeviceRepository repository,IUserRepository userRepository)
  {
    _Repository = repository;
    _UserRepository = userRepository;
  }
  public async Task<int> CreateDeviceAsync(CreateDeviceDTO device, int user_id)
  {
    var existingDevice = await _Repository.GetDeviceByNameAsync(device.Device_name);
    if(existingDevice != null)
      return 0;

    var user = await _UserRepository.GetUserById(user_id);
    if (user == null)
      return 0;

    var newDevice = new Device
    {
      User_id = user_id,
      Device_name = device.Device_name,
      Device_status = "offline"
    };

    return await _Repository.CreateDeviceAsync(newDevice);
  }

  public Task<Device> GetDeviceAsync(int id)
  {
    throw new NotImplementedException();
  }
  public async Task<List<Device>> GetDevicesByUserIdAsync(int user_id)
  {
        var devices = await _Repository.GetDevicesByUserIdAsync(user_id);
    return devices;
  }

  public Task<bool> RemoveDeviceAsync(int id)
  {
    throw new NotImplementedException();
  }
}
