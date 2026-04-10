namespace SmartHome.Services;

public class SensorMessageDispatcher
{
    private readonly AMQPService _amqpService;
    private readonly ILogger<SensorMessageDispatcher> _logger;

    public SensorMessageDispatcher(
        AMQPService amqpService,
        ILogger<SensorMessageDispatcher> logger)
    {
        _amqpService = amqpService;
        _logger = logger;
    }

    public async Task DispatchAsync(string payload)
    {
        await _amqpService.PublishDashboardMessage(payload);
        await _amqpService.PublishInfluxMessage(payload);
        await _amqpService.PublishPythonModelMessage(payload);

        _logger.LogInformation(
            "Sensor payload published to dashboard, influx, and python model routes."
        );
    }
}
