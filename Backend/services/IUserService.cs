using SmartHome.DTO;
using SmartHome.Model;

namespace SmartHome.Services;

public interface IUserService
{
    public User GetUserByIdSync(int id);
    public Task<User?> GetUserByIdAsync(int id);
    public Task<User?> GetUserByEmailAsync(string email);
    public IEnumerable<User> GetAllUsers();
    public Task<User> CreateUserAsync(RegisterDTO dto);
    public void EditUser(int id , User user);
    public void DeleteUser(int id);
}