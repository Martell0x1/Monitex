using SmartHome.DTO;

namespace SmartHome.Services;

public interface ISensorService
{
    Task<int> Create(CreateSensorDto sensorDto, int userId);
}
