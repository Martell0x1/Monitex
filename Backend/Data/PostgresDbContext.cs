namespace SmartHome.Data;

using System.Data;
using Microsoft.Data.SqlClient;
using Npgsql;

public class PostgresDbContext
{
    private readonly string _connectionString;
    public PostgresDbContext(IConfiguration configuration)
    {

        _connectionString =  configuration.GetConnectionString("Postgres") ??
            throw new InvalidOperationException("Cannot Find Connection String");
    }

    public NpgsqlConnection GetConnection()
    {
      return new NpgsqlConnection(_connectionString);
    }
}
