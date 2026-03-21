using System.Text;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client.Events;
using SmartHome.Config;
using SmartHome.Infrastructure.Hubs;

namespace SmartHome.Infrastructure;

public class RabbitmqConsumer : BackgroundService
{
  private readonly RabbitmqConfig _config;
  private readonly IHubContext<SensorHub> _hub;

  public RabbitmqConsumer(RabbitmqConfig config , IHubContext<SensorHub> hub)
  {
    _config = config;
    _hub = hub;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    var channel = await _config.Config();
    var consumer = new AsyncEventingBasicConsumer(channel);

    consumer.ReceivedAsync += async (sender ,eventArgs) =>
    {
      var body = eventArgs.Body.ToArray();
      var message = Encoding.UTF8.GetString(body);

      await _hub.Clients.All.SendAsync(
        "ReceiveSensorReading",
        message,
        cancellationToken:stoppingToken
      );
    };
    await channel.BasicConsumeAsync(
        queue: _config.GetQueue(),
        autoAck: true,
        noLocal: false,
        exclusive:false,
        consumerTag: "",
        arguments: null,
        consumer: consumer,
        cancellationToken: stoppingToken
    );
  }
}
