
using SmartHome.Model;
namespace SmartHome.Data.Repositories;

public interface IDeviceRepository
{
  public Task<Device?> GetDeviceAsync(int id);
  public Task<Device?> GetDeviceByNameAsync(string name);
  public Task<Device?> GetDeviceByIdForUserAsync(int deviceId, int userId);
  public Task<Device?> GetLatestDeviceByUserAsync(int userId);

  public Task<int?> GetDevicesCountByUserIdAsync(int user_id);
  public Task<int> CreateDeviceAsync(Device device);

  public Task<bool> RemoveDeviceAsync(int id);
}
