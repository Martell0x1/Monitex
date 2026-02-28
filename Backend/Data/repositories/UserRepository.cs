using System.Data;
using Microsoft.Data.SqlClient;
using SmartHome.Model;

namespace SmartHome.Data.Repositories;

public class UserRepository : BaseRepository,IUserRepository
{
    public UserRepository (DbContext dbContext) : base(dbContext){}
    public async Task<int> CreateUser(User user)
    {
        return await ExecuteQueryAsync<int>(async cmd =>
        {
           cmd.Parameters.Add("@Username",SqlDbType.VarChar).Value = user.Username;
           cmd.Parameters.Add("@Email",SqlDbType.VarChar).Value = user.Email;
           cmd.Parameters.Add("@Password",SqlDbType.VarChar).Value = user.Password;
           
           var result = await cmd.ExecuteScalarAsync();

           return result != null ? 
              Convert.ToInt32(result) : throw new InvalidOperationException("User Creation Failed") ;
        },@"INSERT INTO Users (Username , Email , Password)
            OUTPUT INSERTED.Id
            VALUES (@Username , @Email , @Password)");
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

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await ExecuteQueryAsync<User?>(async cmd =>
        {
            cmd.Parameters.Add("@Email", SqlDbType.VarChar).Value = email;

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Username = reader.GetString(reader.GetOrdinal("Username")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Password = reader.GetString(reader.GetOrdinal("Password")),
                };
            }

            return null;
        },
        @"SELECT Id, Username, Email, Password
        FROM Users
        WHERE Email = @Email;");
    }

    public async Task<User?> GetUserById(int id)
    {
        return await ExecuteQueryAsync<User>(async cmd => {
            cmd.Parameters.Add("@Id",SqlDbType.Int).Value = id;

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
        },"SELECT * FROM Users where Id = @Id");
    }
    public User? GetUserByIdMock(int id)
    {
        return ExecuteQuery(cmd =>
        {
            cmd.Parameters.Add("@Id",SqlDbType.Int).Value = id;
            using var reader =cmd.ExecuteReader();
            if (reader.Read())
            {
                return new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1)
                };
            }
            return null;
        },@"WAITFOR DELAY '00:00:02';
          SELECT * FROM Users where Id = @Id");
    }
}