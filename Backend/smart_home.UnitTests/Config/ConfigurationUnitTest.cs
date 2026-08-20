using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartHome.Config;
using SmartHome.Data;
using SmartHome.Settings;

namespace smart_home.UnitTests.Config;

public class ConfigurationUnitTest
{
    [Fact]
    public void PostgresDbContext_Throws_WhenConnectionStringIsMissing()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        Assert.Throws<InvalidOperationException>(() => new PostgresDbContext(configuration));
    }

    [Fact]
    public void PostgresDbContext_BuildsConnection_FromConfiguredString()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=monitex;Username=test;Password=test"
            })
            .Build();

        using var connection = new PostgresDbContext(configuration).GetConnection();

        Assert.Contains("Host=localhost", connection.ConnectionString, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Database=monitex", connection.ConnectionString, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InfluxDBConfiguration_Throws_WhenUrlOrBucketIsMissing()
    {
        var missingUrl = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InfluxDB:Token"] = "token",
                ["InfluxDB:Bucket"] = "bucket"
            })
            .Build();
        var missingBucket = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InfluxDB:Token"] = "token",
                ["InfluxDB:Url"] = "http://localhost:8086"
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() => new InfluxDBConfiguration(missingUrl));
        Assert.Throws<InvalidOperationException>(() => new InfluxDBConfiguration(missingBucket));
    }

    [Fact]
    public void RabbitmqConfig_ExposesBoundSettings()
    {
        var config = new RabbitmqConfig(
            NullLogger<RabbitmqConfig>.Instance,
            Options.Create(SampleRabbitSettings()));

        Assert.Equal("iot.sensors.exchange", config.GetExchange());
        Assert.Equal("iot.sensors.dashboard.queue", config.GetDashboardQueue());
        Assert.Equal("iot.sensors.influx.queue", config.GetInfluxQueue());
        Assert.Equal("iot.sensors.anomaly.queue", config.GetPythonModelQueue());
        Assert.Equal("iot.sensors.anomaly.results.queue", config.GetPythonResultsQueue());
        Assert.Equal("sensor.dashboard", config.GetDashboardRoutingKey());
        Assert.Equal("sensor.influx", config.GetInfluxRoutingKey());
        Assert.Equal("sensor.anomaly", config.GetPythonModelRoutingKey());
        Assert.Equal("sensor.anomaly.detected", config.GetPythonResultsRoutingKey());
    }

    [Fact]
    public void InfluxAmqpConfig_ExposesPipelineQueue()
    {
        var config = new InfluxAmqpConfig(
            NullLogger<InfluxAmqpConfig>.Instance,
            Options.Create(SampleRabbitSettings()),
            Options.Create(new InfluxPipelineSettings { Queue = "iot.sensors.influx.queue" }));

        Assert.Equal("iot.sensors.influx.queue", config.GetQueue());
    }

    [Fact]
    public async Task ISmartHomeConfig_GenericConfig_ThrowsNotImplemented()
    {
        var config = new ISmartHomeConfig();

        await Assert.ThrowsAsync<NotImplementedException>(() => config.Config<string>());
    }

    private static RabbitMqSettings SampleRabbitSettings() => new()
    {
        Url = "amqp://guest:guest@localhost:5672/",
        Exchange = "iot.sensors.exchange",
        DashboardQueue = "iot.sensors.dashboard.queue",
        InfluxQueue = "iot.sensors.influx.queue",
        PythonModelQueue = "iot.sensors.anomaly.queue",
        PythonResultsQueue = "iot.sensors.anomaly.results.queue",
        DashboardRoutingKey = "sensor.dashboard",
        InfluxRoutingKey = "sensor.influx",
        PythonModelRoutingKey = "sensor.anomaly",
        PythonResultsRoutingKey = "sensor.anomaly.detected"
    };
}
