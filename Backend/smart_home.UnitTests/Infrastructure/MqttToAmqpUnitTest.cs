using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartHome.Config;
using SmartHome.Infrastructure;
using SmartHome.Services;
using SmartHome.Settings;

namespace smart_home.UnitTests.Infrastructure;

public class MqttToAmqpUnitTest
{
    [Fact]
    public async Task Start_DispatchesMqttPayloadsToAmqpRoutes()
    {
        var amqp = new RecordingAmqpService();
        var dispatcher = new SensorMessageDispatcher(amqp, NullLogger<SensorMessageDispatcher>.Instance);
        var mqtt = new MQTTService(
            new MosquittoConfig(
                NullLogger<MosquittoConfig>.Instance,
                Options.Create(new MosquittoSettings { Ip = "127.0.0.1", Port = 1883, Topic = "topic/test" })),
            NullLogger<MQTTService>.Instance);
        var bridge = new MQTTtoAMQP(mqtt, dispatcher);

        bridge.start();
        await RaiseMqttMessage(mqtt, "{\"temp\":21}");

        Assert.Equal(3, amqp.Published.Count);
        Assert.All(amqp.Published, item => Assert.Equal("{\"temp\":21}", item.Message));
        Assert.Equal(
        [
            "sensors.dashboard",
            "sensors.influx",
            "sensors.python"
        ], amqp.Published.Select(item => item.RoutingKey));
    }

    private static Task RaiseMqttMessage(MQTTService mqtt, string payload)
    {
        var handler = typeof(MQTTService)
            .GetField("OnMessageRecieved", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?.GetValue(mqtt) as Func<string, Task>;

        Assert.NotNull(handler);
        return handler(payload);
    }

    private sealed class RecordingAmqpService() : AMQPService(new RabbitmqConfig(
        NullLogger<RabbitmqConfig>.Instance,
        Options.Create(new RabbitMqSettings
        {
            Url = "amqp://guest:guest@localhost:5672/",
            Exchange = "sensors",
            DashboardQueue = "dashboard",
            InfluxQueue = "influx",
            PythonModelQueue = "python-model",
            PythonResultsQueue = "python-results",
            DashboardRoutingKey = "sensors.dashboard",
            InfluxRoutingKey = "sensors.influx",
            PythonModelRoutingKey = "sensors.python",
            PythonResultsRoutingKey = "sensors.results"
        })))
    {
        public List<(string Message, string RoutingKey)> Published { get; } = [];

        public override Task PublishMessage(string message, string routingKey)
        {
            Published.Add((message, routingKey));
            return Task.CompletedTask;
        }
    }
}
