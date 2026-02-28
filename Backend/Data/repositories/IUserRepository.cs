using SmartHome.Model;

namespace SmartHome.Data.Repositories;

public interface IUserRepository
{
    public  Task<User?> GetUserById(int id);
    public Task<User?> GetUserByEmailAsync(string email);
    public User GetUserByIdMock(int id);
    public Task<IEnumerable<User>> GetAllUsers();
    public Task<int> CreateUser(User user);
    public Task EditUser(int id , User user);
    public Task DeleteUser(int id);
}