using System.Security.Cryptography.Xml;
using SmartHome.Services;

namespace SmartHome.Infrastructure;

public class MQTTtoAMQP
{
  private readonly MQTTService _mqttService;
  private readonly AMQPService _ampqService;

  public MQTTtoAMQP(MQTTService mQTTService , AMQPService aMQPService)
  {
    _mqttService = mQTTService;
    _ampqService = aMQPService;
  }

  public void start()
  {
    _mqttService.OnMessageRecieved += async payload =>
    {
      await Transform(payload);
    };
  }

  private async Task Transform(string payload)
  {
    await _ampqService.PublishMessage(payload);
  }
}
