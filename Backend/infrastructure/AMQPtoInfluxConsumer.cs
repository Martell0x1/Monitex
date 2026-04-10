using System.Text;
using System.Text.Json;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SmartHome.Config;
using SmartHome.Data.Repositories;
using SmartHome.DTO;
using SmartHome.Services;

namespace SmartHome.Infrastructure;

public class AMQPtoInfluxConsumer : BackgroundService
{
    private readonly ILogger<AMQPtoInfluxConsumer> _logger;
    private readonly InfluxAmqpConfig _config;
    private readonly InfluxService _influxService;

    private IChannel? _channel;
    private string? _queue;

    private readonly IServiceScopeFactory _scopeFactory;

    public AMQPtoInfluxConsumer(
        ILogger<AMQPtoInfluxConsumer> logger,
        InfluxAmqpConfig config,
        InfluxService influxService,
        IServiceScopeFactory scopeFactory
    )
    {
        _logger = logger;
        _config = config;
        _influxService = influxService;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _config.Config();
        _queue = _config.GetQueue();

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            using var scope =  _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
            try
            {

                var body = eventArgs.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                using var jsonDocument = JsonDocument.Parse(message);
                var messageType = GetMessageType(jsonDocument.RootElement);

                if (messageType == "health")
                {
                    await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
                    return;
                }

                // _logger.LogInformation("📩 Incoming sensor message:");
                // _logger.LogInformation(message);

                var sensorData = JsonSerializer.Deserialize<InfluxSensorReadingMessage>(
                    message,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (sensorData == null)
                {
                    _logger.LogWarning("Invalid sensor payload received");
                    return;
                }

                var device = await repo.GetDeviceByNameAsync(sensorData.DeviceName);
                if(device == null) throw new Exception("NULL");
                int userId = device.User_id;

                await _influxService.WriteSensorData(
                    userId,
                    sensorData.DeviceName,
                    sensorData.SensorType,
                    sensorData.Value,
                    sensorData.Timestamp
                );

                // _logger.LogInformation(
                //     "📊 Written to InfluxDB → Device: {Device}, Sensor: {Sensor}, Value: {Value}",
                //     sensorData.DeviceName,
                //     sensorData.SensorType,
                //     sensorData.Value
                // );

                await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RabbitMQ message");

                if (_channel != null)
                    await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, requeue: false);
            }
        };

        await _channel.BasicConsumeAsync(_queue, autoAck: false, consumer: consumer);

        _logger.LogInformation("🚀 AMQPtoInfluxConsumer started listening on queue: {Queue}", _queue);

        await Task.Delay(Timeout.Infinite, stoppingToken);
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
