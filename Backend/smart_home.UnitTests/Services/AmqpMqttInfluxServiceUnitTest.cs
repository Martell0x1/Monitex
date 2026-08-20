using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartHome.Config;
using SmartHome.Services;
using SmartHome.Settings;

namespace smart_home.UnitTests.Services;

public class AmqpMqttInfluxServiceUnitTest
{
    [Fact]
    public async Task AMQPService_ConvenienceMethods_PublishToConfiguredRoutingKeys()
    {
        var config = new RabbitmqConfig(
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
            }));
        var sut = new RecordingAmqpService(config);

        await sut.PublishDashboardMessage("dash");
        await sut.PublishInfluxMessage("influx");
        await sut.PublishPythonModelMessage("python");

        Assert.Equal(
        [
            ("dash", "sensors.dashboard"),
            ("influx", "sensors.influx"),
            ("python", "sensors.python")
        ], sut.Published);
    }

    [Fact]
    public void MQTTService_CanBeConstructedWithoutConnecting()
    {
        var options = Options.Create(new MosquittoSettings
        {
            Ip = "127.0.0.1",
            Port = 1883,
            Topic = "topic/test"
        });
        var sut = new MQTTService(
            new MosquittoConfig(NullLogger<MosquittoConfig>.Instance, options),
            NullLogger<MQTTService>.Instance);

        Assert.NotNull(sut);
    }

    [Fact]
    public void InfluxService_CanBeConstructedFromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InfluxDB:Token"] = "token",
                ["InfluxDB:Url"] = "http://localhost:8086",
                ["InfluxDB:Bucket"] = "sensors",
                ["InfluxDB:Org"] = "monitex"
            })
            .Build();

        var sut = new InfluxService(new InfluxDBConfiguration(configuration));

        Assert.NotNull(sut);
    }

    [Fact]
    public void InfluxDBConfiguration_Throws_WhenTokenIsMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InfluxDB:Url"] = "http://localhost:8086",
                ["InfluxDB:Bucket"] = "sensors"
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new InfluxDBConfiguration(configuration));
    }

    private sealed class RecordingAmqpService(RabbitmqConfig config) : AMQPService(config)
    {
        public List<(string Message, string RoutingKey)> Published { get; } = [];

        public override Task PublishMessage(string message, string routingKey)
        {
            Published.Add((message, routingKey));
            return Task.CompletedTask;
        }
    }
}
