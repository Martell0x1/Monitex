using System.Data;
using SmartHome.Model;

namespace SmartHome.Data.Repositories;

public class SensorRepository : BaseNpRepository, ISensorRepository
{
    public SensorRepository(PostgresDbContext context) : base(context) { }


    public async Task<int> CreateSensorAsync(Sensor sensor)
    {
        return await ExecuteQueryAsync<int>(async cmd =>
        {
            cmd.Parameters.Add("@Device_id", NpgsqlTypes.NpgsqlDbType.Integer).Value = sensor.Device_id;
            cmd.Parameters.Add("@Name", NpgsqlTypes.NpgsqlDbType.Varchar).Value = sensor.Name;
            cmd.Parameters.Add("@Sensor_type", NpgsqlTypes.NpgsqlDbType.Varchar).Value = sensor.Type;
            cmd.Parameters.Add("@Location", NpgsqlTypes.NpgsqlDbType.Varchar).Value = sensor.Location;
            cmd.Parameters.Add("@Ip_address", NpgsqlTypes.NpgsqlDbType.Varchar).Value = (object?)sensor.IpAddress ?? DBNull.Value;
            cmd.Parameters.Add("@Description", NpgsqlTypes.NpgsqlDbType.Varchar).Value = (object?)sensor.Description ?? DBNull.Value;

            var result = await cmd.ExecuteScalarAsync();

            return result != null
                ? Convert.ToInt32(result)
                : throw new InvalidOperationException("Insertion failed");

        },
        @"INSERT INTO sensors(device_id, name, sensor_type, location, ip_address, description)
          VALUES(@Device_id, @Name, @Sensor_type, @Location, @Ip_address, @Description)
          RETURNING sensor_id");
    }


    public async Task<Sensor?> GetSensorAsync(int sensorId)
    {
        return await ExecuteQueryAsync<Sensor?>(async cmd =>
        {
            cmd.Parameters.Add("@Sensor_id", NpgsqlTypes.NpgsqlDbType.Integer).Value = sensorId;

            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.Read())
                return null;

            return new Sensor
            {
                Sensor_id = reader.GetInt32(0),
                Device_id = reader.GetInt32(1),
                Name = reader.GetString(2),
                Type = reader.GetString(3),
                Location = reader.GetString(4),
                IpAddress = reader.IsDBNull(5) ? null : reader.GetString(5),
                Description = reader.IsDBNull(6) ? null : reader.GetString(6),
                Created_at = reader.GetDateTime(7)
            };

        },
        @"SELECT sensor_id, device_id, name, sensor_type, location, ip_address, description, created_at
          FROM sensors
          WHERE sensor_id = @Sensor_id");
    }


    public async Task<List<Sensor>> GetSensorsByDeviceAsync(int deviceId)
    {
        return await ExecuteQueryAsync<List<Sensor>>(async cmd =>
        {
            cmd.Parameters.Add("@Device_id", NpgsqlTypes.NpgsqlDbType.Integer).Value = deviceId;

            var sensors = new List<Sensor>();

            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                sensors.Add(new Sensor
                {
                    Sensor_id = reader.GetInt32(0),
                    Device_id = reader.GetInt32(1),
                    Name = reader.GetString(2),
                    Type = reader.GetString(3),
                    Location = reader.GetString(4),
                    IpAddress = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Description = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Created_at = reader.GetDateTime(7)
                });
            }

            return sensors;

        },
        @"SELECT sensor_id, device_id, name, sensor_type, location, ip_address, description, created_at
          FROM sensors
          WHERE device_id = @Device_id");
    }

  public async Task<int?> GetSensorsCountByUserId(int user_id)
  {
    return await ExecuteQueryAsync<int>(async cmd =>
    {
      cmd.Parameters.Add("@User_id",NpgsqlTypes.NpgsqlDbType.Integer).Value=user_id;
      var rows = await cmd.ExecuteScalarAsync();
      return rows != null ? Convert.ToInt32(rows):0;
    },
    @"SELECT COUNT(*) FROM sensors
    WHERE device_id in (
      SELECT device_id FROM devices
      WHERE user_id = @User_id
    );");
  }

  public async Task<bool> RemoveSensorAsync(int sensorId)
    {
        return await ExecuteQueryAsync<bool>(async cmd =>
        {
            cmd.Parameters.Add("@Sensor_id", NpgsqlTypes.NpgsqlDbType.Integer).Value = sensorId;

            var rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0;

        },
        @"DELETE FROM sensors
          WHERE sensor_id = @Sensor_id");
    }
}
