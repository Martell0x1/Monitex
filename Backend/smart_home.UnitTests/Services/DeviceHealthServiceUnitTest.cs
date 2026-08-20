using SmartHome.DTO;
using SmartHome.Services;

namespace smart_home.UnitTests.Services;

public class DeviceHealthServiceUnitTest
{
    private readonly DeviceHealthService _sut = new();

    [Fact]
    public void Evaluate_ReturnsHealthy_WhenAllSignalsAreGood()
    {
        var heartbeat = HealthyHeartbeat();

        var result = _sut.Evaluate(12, "hub-1", heartbeat);

        Assert.Equal(12, result.DeviceId);
        Assert.Equal("hub-1", result.DeviceName);
        Assert.Equal(100, result.Score);
        Assert.Equal("Healthy", result.State);
        Assert.Equal(heartbeat.IpAddress, result.IpAddress);
        Assert.Equal(3, result.Reasons.Count);
        Assert.Contains("MQTT link is connected.", result.Reasons);
        Assert.DoesNotContain(result.Reasons, r => r.Contains("disconnected", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Evaluate_ReturnsWarning_WhenMqttIsDown()
    {
        var heartbeat = HealthyHeartbeat();
        heartbeat.MqttConnected = false;

        var result = _sut.Evaluate(1, "hub-1", heartbeat);

        Assert.Equal(75, result.Score);
        Assert.Equal("Warning", result.State);
        Assert.Equal("MQTT link is disconnected.", result.Reasons[0]);
    }

    [Fact]
    public void Evaluate_AppliesWifiAndHeapPenalties()
    {
        var heartbeat = HealthyHeartbeat();
        heartbeat.WifiRssi = -75;
        heartbeat.FreeHeapBytes = 40_000;
        heartbeat.MinFreeHeapBytes = 15_000;

        var result = _sut.Evaluate(1, "hub-1", heartbeat);

        Assert.Equal(70, result.Score);
        Assert.Equal("Warning", result.State);
        Assert.Contains("Wi-Fi signal is weaker than ideal.", result.Reasons);
        Assert.Contains("Free heap headroom is shrinking.", result.Reasons);
        Assert.Contains("Minimum heap dipped recently.", result.Reasons);
    }

    [Fact]
    public void Evaluate_ClampsScoreAndReturnsCritical_WhenManySignalsFail()
    {
        var heartbeat = new DeviceHealthHeartbeatMessage
        {
            IpAddress = "10.0.0.4",
            MqttConnected = false,
            LastSensorReadOk = false,
            WifiRssi = -90,
            FreeHeapBytes = 10_000,
            MinFreeHeapBytes = 5_000,
            UptimeSeconds = 10,
            RestartReason = "brownout",
            Timestamp = DateTime.UtcNow
        };

        var result = _sut.Evaluate(1, "hub-1", heartbeat);

        Assert.Equal(0, result.Score);
        Assert.Equal("Critical", result.State);
        Assert.Equal(
        [
            "MQTT link is disconnected.",
            "Recent sensor reads are failing.",
            "Wi-Fi signal is very weak."
        ], result.Reasons);
    }

    [Fact]
    public void Evaluate_TreatsVeryWeakWifiAsTwentyPointPenalty()
    {
        var heartbeat = HealthyHeartbeat();
        heartbeat.WifiRssi = -85;

        var result = _sut.Evaluate(1, "hub-1", heartbeat);

        Assert.Equal(80, result.Score);
        Assert.Equal("Warning", result.State);
        Assert.Contains("Wi-Fi signal is very weak.", result.Reasons);
    }

    private static DeviceHealthHeartbeatMessage HealthyHeartbeat() => new()
    {
        IpAddress = "10.0.0.8",
        MqttConnected = true,
        LastSensorReadOk = true,
        WifiRssi = -50,
        FreeHeapBytes = 80_000,
        MinFreeHeapBytes = 40_000,
        UptimeSeconds = 3_600,
        RestartReason = "poweron",
        Timestamp = DateTime.UtcNow
    };
}
