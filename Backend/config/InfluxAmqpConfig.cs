using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SmartHome.Settings;

namespace SmartHome.Config;

public class InfluxAmqpConfig
{
    private readonly ILogger<InfluxAmqpConfig> _logger;
    private readonly RabbitMqSettings _rabbitMqSettings;
    private readonly InfluxPipelineSettings _pipelineSettings;
    private readonly ConnectionFactory _connectionFactory;

    private IConnection? _connection;
    private IChannel? _channel;

    public InfluxAmqpConfig(
        ILogger<InfluxAmqpConfig> logger,
        IOptions<RabbitMqSettings> rabbitMqSettings,
        IOptions<InfluxPipelineSettings> pipelineSettings
    )
    {
        _logger = logger;
        _rabbitMqSettings = rabbitMqSettings.Value;
        _pipelineSettings = pipelineSettings.Value;
        _connectionFactory = new ConnectionFactory();
    }

    public async Task<IChannel> Config()
    {
        if (_channel != null)
            return _channel;

        _connectionFactory.Uri = new Uri(_rabbitMqSettings.Url);

        _connection = await _connectionFactory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            _rabbitMqSettings.Exchange,
            ExchangeType.Topic,
            durable: true
        );

        await _channel.QueueDeclareAsync(
            _pipelineSettings.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        await _channel.QueueBindAsync(
            _pipelineSettings.Queue,
            _rabbitMqSettings.Exchange,
            _rabbitMqSettings.InfluxRoutingKey
        );

        _logger.LogInformation(
            "Influx RabbitMQ consumer connected on queue {Queue}",
            _pipelineSettings.Queue
        );

        return _channel;
    }

    public string GetQueue() => _pipelineSettings.Queue;
}
