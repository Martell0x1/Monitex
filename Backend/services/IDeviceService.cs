namespace SmartHome.Services;
using SmartHome.Model;
using SmartHome.DTO;
public interface IDeviceService
{
  public Task<Device> GetDeviceAsync(int id);
  public Task<int> CreateDeviceAsync(CreateDeviceDTO device, int user_id);

  public Task<List<Device>> GetDevicesByUserIdAsync(int user_id);

  public Task<bool> RemoveDeviceAsync(int id);
}
