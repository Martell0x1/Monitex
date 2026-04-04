using System.Data;
using Npgsql;
using SmartHome.Model;

namespace SmartHome.Data.Repositories;

public class DeviceRepository : BaseNpRepository, IDeviceRepository
{
    public DeviceRepository(PostgresDbContext context) : base(context) { }

    public async Task<int> CreateDeviceAsync(Device device)
    {
        return await ExecuteQueryAsync<int>(async cmd =>
        {
            cmd.Parameters.Add("@User_id", NpgsqlTypes.NpgsqlDbType.Integer).Value = device.User_id;
            cmd.Parameters.Add("@Device_name", NpgsqlTypes.NpgsqlDbType.Varchar).Value = device.Device_name;
            cmd.Parameters.Add("@Device_status", NpgsqlTypes.NpgsqlDbType.Varchar).Value = device.Device_status;

            var result = await cmd.ExecuteScalarAsync();

            return result != null
                ? Convert.ToInt32(result)
                : throw new InvalidOperationException("Insertion failed");

        },
        @"INSERT INTO devices(user_id, device_name, device_status)
          VALUES(@User_id, @Device_name, @Device_status)
          RETURNING device_id");
    }

    public async Task<Device?> GetDeviceAsync(int id)
    {
        return await ExecuteQueryAsync<Device?>(async cmd =>
        {
            cmd.Parameters.Add("@Device_id", NpgsqlTypes.NpgsqlDbType.Integer).Value = id;

            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.Read())
                return null;

            return new Device
            {
                Device_id = reader.GetInt32(0),
                User_id = reader.GetInt32(1),
                Device_name = reader.GetString(2),
                Device_status = reader.GetString(3),
                LastSeen = reader.GetDateTime(4)
            };

        },
        @"SELECT device_id, user_id, device_name, device_status, last_seen
          FROM devices
          WHERE device_id = @Device_id");
    }

  public async Task<Device?> GetDeviceByIdForUserAsync(int deviceId, int userId)
  {
    return await ExecuteQueryAsync<Device?>(async cmd =>
    {
      cmd.Parameters.Add("@device_id",NpgsqlTypes.NpgsqlDbType.Integer).Value=deviceId;
      cmd.Parameters.Add("@user_id",NpgsqlTypes.NpgsqlDbType.Integer).Value=userId;

      using var reader = await cmd.ExecuteReaderAsync();

      if(!reader.Read())
        return null;
      return new Device
      {
          Device_id = reader.GetInt32(0),
          User_id = reader.GetInt32(1),
          Device_name = reader.GetString(2),
          Device_status = reader.GetString(3),
          LastSeen = reader.GetDateTime(4)
      };

    },@"SELECT device_id, user_id, device_name, device_status, last_seen
          FROM devices
          WHERE device_id = @device_id AND user_id = @user_id");
  }

  public async Task<Device?> GetLatestDeviceByUserAsync(int userId)
  {
    return await ExecuteQueryAsync<Device?>(async cmd =>
    {
      cmd.Parameters.Add("@User_id", NpgsqlTypes.NpgsqlDbType.Integer).Value = userId;

      using var reader = await cmd.ExecuteReaderAsync();

      if (!reader.Read())
        return null;

      return new Device
      {
        Device_id = reader.GetInt32(0),
        User_id = reader.GetInt32(1),
        Device_name = reader.GetString(2),
        Device_status = reader.GetString(3),
        LastSeen = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4)
      };
    },
    @"SELECT device_id, user_id, device_name, device_status, last_seen
      FROM devices
      WHERE user_id = @User_id
      ORDER BY device_id DESC
      LIMIT 1");
  }

  public async Task<Device?> GetDeviceByNameAsync(string name)
    {
        return await ExecuteQueryAsync<Device?>(async cmd =>
        {
            cmd.Parameters.Add("@Name", NpgsqlTypes.NpgsqlDbType.Varchar).Value = name;

            using var reader = await cmd.ExecuteReaderAsync();

            if (!reader.Read())
                return null;

            return new Device
            {
                Device_id = reader.GetInt32(0),
                User_id = reader.GetInt32(1),
                Device_name = reader.GetString(2),
                Device_status = reader.GetString(3),
                LastSeen = reader.GetDateTime(4)
            };

        },
        @"SELECT device_id, user_id, device_name, device_status, last_seen
          FROM devices
          WHERE device_name = @Name");
    }

    public async Task<bool> RemoveDeviceAsync(int id)
    {
        return await ExecuteQueryAsync<bool>(async cmd =>
        {
            cmd.Parameters.Add("@Device_id", NpgsqlTypes.NpgsqlDbType.Integer).Value = id;

            var rows = await cmd.ExecuteNonQueryAsync();

            return rows > 0;

        },
        @"DELETE FROM devices
          WHERE device_id = @Device_id");
    }

    public async Task<int?> GetDevicesCountByUserIdAsync(int user_id)
    {
        return await ExecuteQueryAsync<int?>(async cmd =>
        {
            cmd.Parameters.Add("@User_Id", NpgsqlTypes.NpgsqlDbType.Integer).Value = user_id;

            var result = await cmd.ExecuteScalarAsync();
            return result != null ? Convert.ToInt32(result) : 0;
        },
        @"SELECT COUNT(*)
          FROM devices
          WHERE user_id = @User_Id");
    }
}
