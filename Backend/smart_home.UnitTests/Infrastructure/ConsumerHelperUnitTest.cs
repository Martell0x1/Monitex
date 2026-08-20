using System.Reflection;
using System.Text.Json;
using SmartHome.Infrastructure;
using SmartHome.Model;

namespace smart_home.UnitTests.Infrastructure;

public class ConsumerHelperUnitTest
{
    [Theory]
    [InlineData("""{"messageType":"health"}""", "health")]
    [InlineData("""{"MessageType":"HEALTH"}""", "health")]
    [InlineData("""{"value":1}""", "sensor")]
    public void GetMessageType_ReadsCaseInsensitiveProperty(string json, string expected)
    {
        using var document = JsonDocument.Parse(json);

        Assert.Equal(expected, Invoke<string>(typeof(AMQPtoInfluxConsumer), "GetMessageType", document.RootElement));
        Assert.Equal(expected, Invoke<string>(typeof(AMQPtoSignalRConsumer), "GetMessageType", document.RootElement));
    }

    [Theory]
    [InlineData("critical", "critical")]
    [InlineData("ERROR", "critical")]
    [InlineData("high", "critical")]
    [InlineData("info", "info")]
    [InlineData("low", "info")]
    [InlineData("warn", "warning")]
    [InlineData(null, "warning")]
    public void NormalizeSeverity_MapsKnownLevels(string? input, string expected)
    {
        Assert.Equal(expected, Invoke<string>(typeof(AMQPtoAnomalySignalRConsumer), "NormalizeSeverity", input!));
    }

    [Fact]
    public void MatchSensor_PrefersExactTypeThenNameThenFirst()
    {
        var sensors = new List<Sensor>
        {
            new() { Sensor_id = 1, Name = "kitchen-hum", Type = "humidity", Location = "kitchen" },
            new() { Sensor_id = 2, Name = "oven-temp", Type = "temperature", Location = "kitchen" }
        };

        var exact = Invoke<Sensor?>(typeof(AMQPtoSignalRConsumer), "MatchSensor", sensors, "Temperature");
        var byName = Invoke<Sensor?>(typeof(AMQPtoSignalRConsumer), "MatchSensor", sensors, "oven");
        var fallback = Invoke<Sensor?>(typeof(AMQPtoSignalRConsumer), "MatchSensor", sensors, "unknown");
        var empty = Invoke<Sensor?>(typeof(AMQPtoSignalRConsumer), "MatchSensor", new List<Sensor>(), "temperature");

        Assert.Equal(2, exact?.Sensor_id);
        Assert.Equal(2, byName?.Sensor_id);
        Assert.Equal(1, fallback?.Sensor_id);
        Assert.Null(empty);
    }

    private static T Invoke<T>(Type type, string name, params object?[] args)
    {
        var method = type.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"Missing {type.Name}.{name}");
        return (T)method.Invoke(null, args)!;
    }
}
