using RabbitMQ.Client;
using SmartHome.Config;
using System.Text;

namespace SmartHome.Services;

public class AMQPService
{
    private readonly RabbitmqConfig _config;
    private IChannel? _channel;

    public AMQPService(RabbitmqConfig config)
    {
        _config = config;
    }

    public async Task PublishMessage(string message)
    {
        _channel ??= await _config.Config();

        var body = Encoding.UTF8.GetBytes(message);

        await _channel.BasicPublishAsync(
            exchange: _config.GetExchange(),
            routingKey: _config.GetRoutingKey(),
            body: body
        );
    }
}
