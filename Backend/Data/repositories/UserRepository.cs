using System.Data;
using Microsoft.Data.SqlClient;
using SmartHome.Model;

namespace SmartHome.Data.Repositories;

public class UserRepository : BaseNpRepository,IUserRepository
{
    public UserRepository (PostgresDbContext dbContext) : base(dbContext){}
    public async Task<int> CreateUser(User user)
    {
        return await ExecuteQueryAsync<int>(async cmd =>
        {
           cmd.Parameters.Add("@Username",NpgsqlTypes.NpgsqlDbType.Varchar).Value = user.Username;
           cmd.Parameters.Add("@Email",NpgsqlTypes.NpgsqlDbType.Varchar).Value = user.Email;
           cmd.Parameters.Add("@Password",NpgsqlTypes.NpgsqlDbType.Varchar).Value = user.Password;

           var result = await cmd.ExecuteScalarAsync();

           return result != null ?
              Convert.ToInt32(result) : throw new InvalidOperationException("User Creation Failed") ;
        },@"INSERT INTO Users (user_name , email , password)
            VALUES (@Username , @Email , @Password)
            RETURNING user_id");
    }

    public Task DeleteUser(int id)
    {
        throw new NotImplementedException();
    }

    public Task EditUser(int id, User user)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<User>> GetAllUsers()
    {
        throw new NotImplementedException();
    }

  public async Task<User?> GetUserByDeviceIdAsync(int device_id)
  {
    return await ExecuteQueryAsync<User?>(async cmd =>
    {
      cmd.Parameters.Add("@device_id",NpgsqlTypes.NpgsqlDbType.Integer).Value=device_id;

      using var reader = await cmd.ExecuteReaderAsync();

      if(!await reader.ReadAsync())
        return null;

      var user = new User
      {
        Id = reader.GetInt32(reader.GetOrdinal("Id")),
        Username = reader.GetString(reader.GetOrdinal("Username")),
        Email = reader.GetString(reader.GetOrdinal("Email")),
        Password = reader.GetString(reader.GetOrdinal("Password"))
      };

      return user;

      },@"SELECT u.user_id, u.user_name, u.email, u.password
              FROM Users u
              INNER JOIN devices d ON u.user_id = d.user_id
              WHERE d.device_id = @device_id;");
  }

  public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await ExecuteQueryAsync<User?>(async cmd =>
        {
            cmd.Parameters.Add("@Email", NpgsqlTypes.NpgsqlDbType.Varchar).Value = email;

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetInt32(reader.GetOrdinal("user_id")),
                    Username = reader.GetString(reader.GetOrdinal("user_name")),
                    Email = reader.GetString(reader.GetOrdinal("email")),
                    Password = reader.GetString(reader.GetOrdinal("password")),
                };
            }

            return null;
        },
        @"SELECT user_id, user_name, email, password
        FROM Users
        WHERE email = @Email;");
    }

    public async Task<User?> GetUserById(int id)
    {
        return await ExecuteQueryAsync<User>(async cmd => {
            cmd.Parameters.Add("@Id",NpgsqlTypes.NpgsqlDbType.Integer).Value = id;

            using var reader = await cmd.ExecuteReaderAsync();

            if(await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                };
            }
            return null;
        },"SELECT * FROM Users where user_id = @Id");
    }
}
