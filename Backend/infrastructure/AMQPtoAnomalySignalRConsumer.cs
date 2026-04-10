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

namespace SmartHome.Infrastructure;

public class AMQPtoAnomalySignalRConsumer : BackgroundService
{
    private readonly RabbitmqConfig _config;
    private readonly IHubContext<SensorHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AMQPtoAnomalySignalRConsumer> _logger;

    private IChannel? _channel;

    public AMQPtoAnomalySignalRConsumer(
        RabbitmqConfig config,
        IServiceScopeFactory scopeFactory,
        ILogger<AMQPtoAnomalySignalRConsumer> logger,
        IHubContext<SensorHub> hubContext)
    {
        _config = config;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = await _config.Config();

        var queueName = _config.GetPythonResultsQueue();
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            using var scope = _scopeFactory.CreateScope();
            var deviceRepository = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());

            _logger.LogInformation("Anomaly queue incoming message {Message}", message);

            var payload = JsonSerializer.Deserialize<AnomalyNotificationDto>(
                message,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (payload == null || string.IsNullOrWhiteSpace(payload.DeviceName))
            {
                _logger.LogWarning("Anomaly consumer received an invalid anomaly payload.");
                return;
            }

            if (string.IsNullOrWhiteSpace(payload.Message))
            {
                _logger.LogWarning("Anomaly consumer received an anomaly payload without a message.");
                return;
            }

            var device = await deviceRepository.GetDeviceByNameAsync(payload.DeviceName);
            if (device == null)
            {
                _logger.LogWarning(
                    "Anomaly consumer could not find device {DeviceName} for payload.",
                    payload.DeviceName
                );
                return;
            }

            payload.DeviceId ??= device.Device_id.ToString();
            payload.Severity = NormalizeSeverity(payload.Severity);

            await _hubContext
                .Clients
                .User(device.User_id.ToString())
                .SendAsync("ReceiveAnomalyNotification", payload, stoppingToken);

            _logger.LogInformation(
                "Anomaly notification forwarded for device {DeviceId}/{DeviceName} to user {UserId}",
                device.Device_id,
                device.Device_name,
                device.User_id
            );
        };

        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: true,
            consumer: consumer
        );
    }

    private static string NormalizeSeverity(string? severity)
    {
        var normalized = severity?.Trim().ToLowerInvariant();

        if (normalized == "critical" || normalized == "error" || normalized == "high")
            return "critical";

        if (normalized == "info" || normalized == "low")
            return "info";

        return "warning";
    }
}
