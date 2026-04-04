using SmartHome.Model;

namespace SmartHome.Data.Repositories;

public interface ISensorRepository
{
    Task<int> CreateSensorAsync(Sensor sensor);

    Task<Sensor?> GetSensorAsync(int sensorId);

    Task<int?> GetSensorsCountByUserId(int user_id);

    Task<List<Sensor>> GetSensorsByDeviceAsync(int deviceId);

    Task<bool> RemoveSensorAsync(int sensorId);
}
