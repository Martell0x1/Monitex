using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using SmartHome.Config;

namespace SmartHome.Services;

public class InfluxService
{
    private readonly InfluxDBClient _client;
    private readonly string _bucket;
    private readonly string? _org;

    public InfluxService(InfluxDBConfiguration config)
    {
        _client = config.GetClient();
        _bucket = config._bucket; // make Bucket public or add a getter
        _org = config._org;       // make Org public or add a getter
    }

    public async Task WriteSensorData(
        int userId,
        string deviceName,
        string sensorType,
        double value,
        DateTime? timestamp = null
    )
    {
        var point = PointData
            .Measurement("sensor_readings")
            .Tag("userId", userId.ToString())
            .Tag("device_name", deviceName)
            .Tag("sensorType", sensorType)
            .Field("value", value)
            .Timestamp(timestamp ?? DateTime.UtcNow, WritePrecision.Ns);

        var writeApi = _client.GetWriteApiAsync();

        await writeApi.WritePointAsync(point, _bucket, _org);
    }
}
