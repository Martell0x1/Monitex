using System.Text;
using Microsoft.Extensions.Options;
using MQTTnet;
using SmartHome.Settings;
namespace SmartHome.Config;

public class MosquittoConfig : ISmartHomeConfig
{
  private MqttClientFactory mqttFactory;
  private ILogger<MosquittoConfig> _logger;
  private MosquittoSettings _settings;

  public MosquittoConfig(ILogger<MosquittoConfig> logger , IOptions<MosquittoSettings> settings)
  {
    mqttFactory = new MqttClientFactory();
    _logger = logger;
    _settings = settings.Value;
  }
  public async Task<IMqttClient> Config()
  {
    var client = mqttFactory.CreateMqttClient();


    var options = new MqttClientOptionsBuilder()
      .WithTcpServer(_settings.Ip,_settings.Port)
      .WithWillQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
      .Build();

    await client.ConnectAsync(options);
    _logger.LogInformation("Connected To Mqtt !");

    var topic = string.IsNullOrWhiteSpace(_settings.Topic)
      ? "topic/test"
      : _settings.Topic;

    await client.SubscribeAsync(topic);
    _logger.LogInformation("Subscribed to topic {Topic}", topic);

    return client;
  }
}
