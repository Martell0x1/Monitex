using System.Text;
using MQTTnet;
using SmartHome.Config;

namespace SmartHome.Services;

public class MQTTService
{
  private  MosquittoConfig _config;
  private  ILogger<MQTTService> _logger;

  public event Func<string,Task>? OnMessageRecieved;

  public MQTTService(MosquittoConfig config , ILogger<MQTTService> logger)
  {
    _config = config;
    _logger = logger;
  }

  public async Task Listen()
  {
    var client = await _config.Config();

    client.ApplicationMessageReceivedAsync += async e =>
    {
      var topic = e.ApplicationMessage.Topic;
      var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
      _logger.LogInformation($"Message Recieved From Topic [{topic}]:{payload}");

      if(OnMessageRecieved != null)
        await OnMessageRecieved(payload);

    };
  }


}
