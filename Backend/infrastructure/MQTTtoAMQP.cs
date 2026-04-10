using SmartHome.Services;

namespace SmartHome.Infrastructure;

public class MQTTtoAMQP
{
  private readonly MQTTService _mqttService;
  private readonly SensorMessageDispatcher _messageDispatcher;

  public MQTTtoAMQP(MQTTService mQTTService, SensorMessageDispatcher messageDispatcher)
  {
    _mqttService = mQTTService;
    _messageDispatcher = messageDispatcher;
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
    await _messageDispatcher.DispatchAsync(payload);
  }
}
