using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartHome.Config;
using SmartHome.Services;
using SmartHome.Settings;

namespace smart_home.UnitTests.Services;

public class SensorMessageDispatcherUnitTest
{
    [Fact]
    public async Task DispatchAsync_PublishesPayloadToAllThreeRoutes()
    {
        var amqp = new RecordingAmqpService();
        var sut = new SensorMessageDispatcher(amqp, NullLogger<SensorMessageDispatcher>.Instance);

        await sut.DispatchAsync("{\"temp\":21}");

        Assert.Equal(
        [
            "{\"temp\":21}",
            "{\"temp\":21}",
            "{\"temp\":21}"
        ], amqp.PublishedBodies);
        Assert.Equal(["dashboard", "influx", "python"], amqp.PublishedRoutes);
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
        public List<string> PublishedBodies { get; } = [];
        public List<string> PublishedRoutes { get; } = [];

        public override Task PublishDashboardMessage(string message)
        {
            PublishedBodies.Add(message);
            PublishedRoutes.Add("dashboard");
            return Task.CompletedTask;
        }

        public override Task PublishInfluxMessage(string message)
        {
            PublishedBodies.Add(message);
            PublishedRoutes.Add("influx");
            return Task.CompletedTask;
        }

        public override Task PublishPythonModelMessage(string message)
        {
            PublishedBodies.Add(message);
            PublishedRoutes.Add("python");
            return Task.CompletedTask;
        }
    }
}
