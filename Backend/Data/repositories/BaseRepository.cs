using Microsoft.Data.SqlClient;
// using Npgsql;

namespace SmartHome.Data.Repositories;

public abstract class BaseRepository
{
    private readonly DbContext _context;
    protected BaseRepository(DbContext context) => _context = context;

    protected async Task<T> ExecuteQueryAsync<T>(Func<SqlCommand,Task<T?>> action, string sql)
    {
        using var connection = _context.GetConnection();
        await connection.OpenAsync();
        using var command = new SqlCommand(sql,connection);
        return await action(command);
    }

    protected T ExecuteQuery<T>(Func<SqlCommand,T?> action , string sql)
    {
        using var connection = _context.GetConnection();
        connection.Open();
        using var command = new SqlCommand(sql,connection);
        return action(command) ;
    }
}