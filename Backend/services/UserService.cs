using SmartHome.Data.Repositories;
using SmartHome.DTO;
using SmartHome.Model;

using BCrypt.Net;

namespace SmartHome.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _IUserRepository;
    public UserService(IUserRepository repo) => _IUserRepository = repo;
    public async Task<User> CreateUserAsync(RegisterDTO dto)
    {
        var existinguser = await _IUserRepository.GetUserByEmailAsync(dto.Email);
        if(existinguser != null)
            return null;

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        };
        await _IUserRepository.CreateUser(user);
        return user;
    }

    public void DeleteUser(int id)
    {
        throw new NotImplementedException();
    }

    public void EditUser(int id, User user)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<User> GetAllUsers()
    {
        throw new NotImplementedException();
    }

    public async Task<User> GetUserByIdSync(int id)
    {
        return await _IUserRepository.GetUserByDeviceIdAsync(id);
    }
    public async Task<User?> GetUserByIdAsync(int id)
    {
        return await _IUserRepository.GetUserById(id);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await _IUserRepository.GetUserByEmailAsync(email);
    }
}
