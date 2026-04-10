using SmartHome.Data.Repositories;
using SmartHome.DTO;
using SmartHome.Model;

namespace SmartHome.Services;

public class SensorService : ISensorService
{
    private readonly ISensorRepository _sensorRepository;
    private readonly IDeviceRepository _deviceRepository;

    public SensorService(ISensorRepository sensorRepository, IDeviceRepository deviceRepository)
    {
        _sensorRepository = sensorRepository;
        _deviceRepository = deviceRepository;
    }

    public async Task<int> Create(CreateSensorDto sensorDto, int userId)
    {
        var device = await _deviceRepository.GetLatestDeviceByUserAsync(userId);
        if (device == null)
            throw new InvalidOperationException("No device found for this user.");

        var sensor = new Sensor
        {
            Device_id = device.Device_id,
            Name = sensorDto.Name,
            Type = sensorDto.Type,
            Location = sensorDto.Location,
            IpAddress = string.IsNullOrWhiteSpace(sensorDto.IpAddress) ? null : sensorDto.IpAddress,
            Description = string.IsNullOrWhiteSpace(sensorDto.Description) ? null : sensorDto.Description
        };

        return await _sensorRepository.CreateSensorAsync(sensor);
    }

    public async Task<List<SensorSummaryDto>> GetSensorsByDeviceAsync(int deviceId, int userId)
    {
        var device = await _deviceRepository.GetDeviceByIdForUserAsync(deviceId, userId);
        if (device == null)
            throw new InvalidOperationException("Device was not found for this user.");

        var sensors = await _sensorRepository.GetSensorsByDeviceAsync(deviceId);

        return sensors.Select(sensor => new SensorSummaryDto
        {
            SensorId = sensor.Sensor_id,
            DeviceId = sensor.Device_id,
            Name = sensor.Name,
            Type = sensor.Type,
            Location = sensor.Location,
            IpAddress = sensor.IpAddress,
            Description = sensor.Description,
            CreatedAt = sensor.Created_at
        }).ToList();
    }
}
