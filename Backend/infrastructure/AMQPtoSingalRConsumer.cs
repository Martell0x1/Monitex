using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmartHome.Config;
using SmartHome.Data.Repositories;
using SmartHome.DTO;
using SmartHome.Infrastructure.Hubs;
using SmartHome.Services;

namespace SmartHome.Infrastructure;

public class AMQPtoSignalRConsumer : BackgroundService
{
    private readonly RabbitmqConfig _config;
    private readonly IHubContext<SensorHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AMQPtoSignalRConsumer> _logger;
    private readonly DeviceHealthService _deviceHealthService;

    private IChannel? _channel;

    public AMQPtoSignalRConsumer(
        RabbitmqConfig config,
        IServiceScopeFactory scopeFactory,
        ILogger<AMQPtoSignalRConsumer> logger,
        IHubContext<SensorHub> hubContext,
        DeviceHealthService deviceHealthService)
    {
        _config = config;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _deviceHealthService = deviceHealthService;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _channel = await _config.Config();

        var queueName = _config.GetDashboardQueue();

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, ea) =>
        {
            using var scope =  _scopeFactory.CreateScope();
            var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
            var sensorRepository = scope.ServiceProvider.GetRequiredService<ISensorRepository>();
            var body = ea.Body.ToArray();

            var message = Encoding.UTF8.GetString(body);

            _logger.Log(LogLevel.Information,$"SingalR Incoming Message {message}");
            using var jsonDocument = JsonDocument.Parse(message);
            var messageType = GetMessageType(jsonDocument.RootElement);

            if (messageType == "health")
            {
                await PublishHealthPayload(message, deviceRepository);
                return;
            }

            var sensorData =
                JsonSerializer.Deserialize<InfluxSensorReadingMessage>(
                    message,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            if (sensorData == null)
            {
                _logger.LogWarning("SignalR consumer received an invalid sensor payload.");
                return;
            }

            _logger.Log(
                LogLevel.Information,
                "Serialized Data {DeviceName}/{Value} ip {IpAddress}",
                sensorData.DeviceName,
                sensorData.Value,
                sensorData.IpAddress
            );
            var device = await deviceRepository.GetDeviceByNameAsync(sensorData.DeviceName);
            if(device == null) throw new Exception("Null device");

            var userId = device.User_id;
            var sensors = await sensorRepository.GetSensorsByDeviceAsync(device.Device_id);
            var matchedSensor = MatchSensor(sensors, sensorData.SensorType);

            var hubPayload = new SingalRDTO
            {
                DeviceId = device.Device_id,
                DeviceName = device.Device_name,
                SensorId = matchedSensor?.Sensor_id,
                SensorName = matchedSensor?.Name,
                SensorType = sensorData.SensorType,
                Value = sensorData.Value,
                Timestamp = sensorData.Timestamp,
                IpAddress = sensorData.IpAddress
            };

            _logger.Log(
                LogLevel.Information,
                "SignalR mapped device {DeviceId}/{DeviceName} sensor {SensorId}/{SensorName} type {SensorType} value {Value}",
                hubPayload.DeviceId,
                hubPayload.DeviceName,
                hubPayload.SensorId,
                hubPayload.SensorName,
                hubPayload.SensorType,
                hubPayload.Value
            );

            await _hubContext
                .Clients
                .User(userId.ToString())
                .SendAsync(
                    "ReceiveSensorData",
                    hubPayload
                );
        };

        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: true,
            consumer: consumer
        );
    }

    private async Task PublishHealthPayload(
        string message,
        IDeviceRepository deviceRepository)
    {
        var heartbeat =
            JsonSerializer.Deserialize<DeviceHealthHeartbeatMessage>(
                message,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

        if (heartbeat == null)
        {
            _logger.LogWarning("SignalR consumer received an invalid health heartbeat.");
            return;
        }

        var device = await deviceRepository.GetDeviceByNameAsync(heartbeat.DeviceName);
        if (device == null)
            throw new Exception("Null device");

        var healthPayload = _deviceHealthService.Evaluate(
            device.Device_id,
            device.Device_name,
            heartbeat
        );

        _logger.Log(
            LogLevel.Information,
            "Device health evaluated for {DeviceName}: {Score} {State}",
            healthPayload.DeviceName,
            healthPayload.Score,
            healthPayload.State
        );

        await _hubContext
            .Clients
            .User(device.User_id.ToString())
            .SendAsync(
                "ReceiveDeviceHealth",
                healthPayload
            );
    }

    private static SmartHome.Model.Sensor? MatchSensor(
        List<SmartHome.Model.Sensor> sensors,
        string incomingSensorType)
    {
        if (sensors.Count == 0)
            return null;

        var normalizedType = Normalize(incomingSensorType);

        var exactTypeMatch = sensors.FirstOrDefault(sensor =>
            Normalize(sensor.Type) == normalizedType);

        if (exactTypeMatch != null)
            return exactTypeMatch;

        var nameMatch = sensors.FirstOrDefault(sensor =>
            Normalize(sensor.Name).Contains(normalizedType) ||
            normalizedType.Contains(Normalize(sensor.Name)));

        if (nameMatch != null)
            return nameMatch;

        return sensors.FirstOrDefault();
    }

    private static string Normalize(string value)
    {
        return string.Concat(
            value
                .Trim()
                .ToLowerInvariant()
                .Where(char.IsLetterOrDigit)
        );
    }

    private static string GetMessageType(JsonElement rootElement)
    {
        foreach (var property in rootElement.EnumerateObject())
        {
            if (string.Equals(property.Name, "messageType", StringComparison.OrdinalIgnoreCase))
            {
                return property.Value.GetString()?.Trim().ToLowerInvariant() ?? "sensor";
            }
        }

        return "sensor";
    }
}
