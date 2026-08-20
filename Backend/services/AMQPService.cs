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

    public virtual async Task PublishMessage(string message, string routingKey)
    {
        _channel ??= await _config.Config();

        var body = Encoding.UTF8.GetBytes(message);

        await _channel.BasicPublishAsync(
            exchange: _config.GetExchange(),
            routingKey: routingKey,
            body: body
        );
    }

    public virtual Task PublishDashboardMessage(string message)
        => PublishMessage(message, _config.GetDashboardRoutingKey());

    public virtual Task PublishInfluxMessage(string message)
        => PublishMessage(message, _config.GetInfluxRoutingKey());

    public virtual Task PublishPythonModelMessage(string message)
        => PublishMessage(message, _config.GetPythonModelRoutingKey());
}
