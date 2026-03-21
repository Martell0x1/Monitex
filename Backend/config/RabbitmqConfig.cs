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
        var queue = _settings.Queue;
        var routingKey = _settings.RoutingKey;

        await _channel.ExchangeDeclareAsync(
            exchange,
            ExchangeType.Direct,
            durable: true
        );

        await _channel.QueueDeclareAsync(
            queue,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        await _channel.QueueBindAsync(queue, exchange, routingKey);

        _logger.LogInformation("RabbitMQ connected successfully");

        return _channel;
    }

    public string GetExchange()
        => _settings.Exchange;

    public string GetRoutingKey()
        => _settings.RoutingKey;
    public string GetQueue()
        => _settings.Queue;
}
