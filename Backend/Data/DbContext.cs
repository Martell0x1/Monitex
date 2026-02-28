namespace SmartHome.Data;
using Microsoft.Data.SqlClient;
public class DbContext
{
    private readonly string _connectionString;
    public DbContext(IConfiguration configuration)
    {
        
        _connectionString =  configuration.GetConnectionString("Default") ?? 
            throw new InvalidOperationException("Cannot Find Connection String");
    }

    public  SqlConnection GetConnection()
    {
        return new SqlConnection(_connectionString);
    }
}