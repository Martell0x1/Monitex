using SmartHome.DTO;

namespace SmartHome.Services;

public class DeviceHealthService
{
    public DeviceHealthSignalRDto Evaluate(
        int deviceId,
        string deviceName,
        DeviceHealthHeartbeatMessage heartbeat)
    {
        var score = 100;
        var negativeReasons = new List<string>();
        var positiveReasons = new List<string>();

        if (!heartbeat.MqttConnected)
        {
            score -= 25;
            negativeReasons.Add("MQTT link is disconnected.");
        }
        else
        {
            positiveReasons.Add("MQTT link is connected.");
        }

        if (!heartbeat.LastSensorReadOk)
        {
            score -= 15;
            negativeReasons.Add("Recent sensor reads are failing.");
        }
        else
        {
            positiveReasons.Add("Sensor reads are succeeding.");
        }

        if (heartbeat.WifiRssi <= -85)
        {
            score -= 20;
            negativeReasons.Add("Wi-Fi signal is very weak.");
        }
        else if (heartbeat.WifiRssi <= -75)
        {
            score -= 10;
            negativeReasons.Add("Wi-Fi signal is weaker than ideal.");
        }
        else
        {
            positiveReasons.Add("Wi-Fi signal is stable.");
        }

        if (heartbeat.FreeHeapBytes < 25_000)
        {
            score -= 20;
            negativeReasons.Add("Free heap is critically low.");
        }
        else if (heartbeat.FreeHeapBytes < 50_000)
        {
            score -= 10;
            negativeReasons.Add("Free heap headroom is shrinking.");
        }
        else
        {
            positiveReasons.Add("Heap headroom is healthy.");
        }

        if (heartbeat.MinFreeHeapBytes < 20_000)
        {
            score -= 10;
            negativeReasons.Add("Minimum heap dipped recently.");
        }

        if (heartbeat.UptimeSeconds < 120)
        {
            score -= 15;
            negativeReasons.Add($"Device restarted recently ({heartbeat.RestartReason}).");
        }
        else
        {
            positiveReasons.Add("Uptime looks stable.");
        }

        score = Math.Clamp(score, 0, 100);

        var state = score >= 85
            ? "Healthy"
            : score >= 60
                ? "Warning"
                : "Critical";

        var reasons = negativeReasons
            .Concat(positiveReasons)
            .Take(3)
            .ToList();

        return new DeviceHealthSignalRDto
        {
            DeviceId = deviceId,
            DeviceName = deviceName,
            IpAddress = heartbeat.IpAddress,
            Score = score,
            State = state,
            Reasons = reasons,
            WifiRssi = heartbeat.WifiRssi,
            FreeHeapBytes = heartbeat.FreeHeapBytes,
            MinFreeHeapBytes = heartbeat.MinFreeHeapBytes,
            UptimeSeconds = heartbeat.UptimeSeconds,
            RestartReason = heartbeat.RestartReason,
            MqttConnected = heartbeat.MqttConnected,
            LastSensorReadOk = heartbeat.LastSensorReadOk,
            Timestamp = heartbeat.Timestamp
        };
    }
}
