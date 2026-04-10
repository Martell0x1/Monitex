using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SmartHome.Settings;

namespace SmartHome.Config;

public class RabbitmqConfig : ISmartHomeConfig
{
    private readonly ILogger<RabbitmqConfig> _logger;
    private readonly ConnectionFactory _connectionFactory;

    private IConnection? _connection;
    private IChannel? _channel;
    private RabbitMqSettings _settings;

    public RabbitmqConfig(ILogger<RabbitmqConfig> logger ,IOptions<RabbitMqSettings> settings )
    {
        _logger = logger;
        _settings = settings.Value;
        _connectionFactory = new ConnectionFactory();
    }

    public async Task<IChannel> Config()
    {
        if (_channel != null)
            return _channel;

        _connectionFactory.Uri = new Uri(_settings.Url);

        _connection = await _connectionFactory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        var exchange = _settings.Exchange;
        await _channel.QueueDeclareAsync(
            _settings.DashboardQueue,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        await _channel.QueueDeclareAsync(
            _settings.InfluxQueue,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        await _channel.QueueDeclareAsync(
            _settings.PythonModelQueue,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        await _channel.QueueDeclareAsync(
            _settings.PythonResultsQueue,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        await _channel.ExchangeDeclareAsync(
            exchange,
            ExchangeType.Topic,
            durable: true
        );

        await _channel.QueueBindAsync(
            _settings.DashboardQueue,
            exchange,
            _settings.DashboardRoutingKey
        );

        await _channel.QueueBindAsync(
            _settings.InfluxQueue,
            exchange,
            _settings.InfluxRoutingKey
        );

        await _channel.QueueBindAsync(
            _settings.PythonModelQueue,
            exchange,
            _settings.PythonModelRoutingKey
        );

        await _channel.QueueBindAsync(
            _settings.PythonResultsQueue,
            exchange,
            _settings.PythonResultsRoutingKey
        );
        _logger.LogInformation("RabbitMQ connected successfully");

        return _channel;
    }

    public string GetExchange()
        => _settings.Exchange;

    public string GetDashboardQueue()
        => _settings.DashboardQueue;
    public string GetInfluxQueue()
        => _settings.InfluxQueue;
    public string GetPythonModelQueue()
        => _settings.PythonModelQueue;
    public string GetPythonResultsQueue()
        => _settings.PythonResultsQueue;
    public string GetDashboardRoutingKey()
        => _settings.DashboardRoutingKey;
    public string GetInfluxRoutingKey()
        => _settings.InfluxRoutingKey;
    public string GetPythonModelRoutingKey()
        => _settings.PythonModelRoutingKey;
    public string GetPythonResultsRoutingKey()
        => _settings.PythonResultsRoutingKey;
}
