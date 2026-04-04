

using Npgsql;

namespace SmartHome.Data.Repositories;

public abstract class BaseNpRepository
{
    private readonly PostgresDbContext _context;
    protected BaseNpRepository(PostgresDbContext context) => _context = context;

    protected async Task<T> ExecuteQueryAsync<T>(Func<NpgsqlCommand,Task<T?>> action, string sql)
    {
        using var connection = _context.GetConnection();
        await connection.OpenAsync();
        using var command = new NpgsqlCommand(sql,connection);
        return await action(command);
    }
}
