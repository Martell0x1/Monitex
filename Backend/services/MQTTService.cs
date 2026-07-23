using System.Text;
using MQTTnet;
using SmartHome.Config;

namespace SmartHome.Services;

public class MQTTService
{
  private readonly MosquittoConfig _config;
  private readonly ILogger<MQTTService> _logger;
  private IMqttClient? _client;

  public event Func<string, Task>? OnMessageRecieved;

  public MQTTService(MosquittoConfig config, ILogger<MQTTService> logger)
  {
    _config = config;
    _logger = logger;
  }

  public async Task Listen()
  {
    _client = await _config.Config();

    _client.ApplicationMessageReceivedAsync += async e =>
    {
      var topic = e.ApplicationMessage.Topic;
      var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
      _logger.LogInformation("Message Recieved From Topic [{Topic}]:{Payload}", topic, payload);

      if (OnMessageRecieved != null)
      {
        await OnMessageRecieved(payload);
      }
    };
  }
}
