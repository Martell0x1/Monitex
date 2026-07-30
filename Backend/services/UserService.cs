using SmartHome.Data.Repositories;
using SmartHome.DTO;
using SmartHome.Model;

using BCrypt.Net;

namespace SmartHome.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _IUserRepository;

    public UserService(IUserRepository repo)
    {
        _IUserRepository = repo;
    }

    public async Task<User> CreateUserAsync(RegisterDTO dto)
    {
        var existingUser = await _IUserRepository.GetUserByEmailAsync(dto.Email);

        if (existingUser != null)
            return null;

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        var userId = await _IUserRepository.CreateUser(user);
        user.Id = userId;

        return user;
    }

    // Google OAuth
    public async Task<User> CreateGoogleUserAsync(string username, string email)
    {
        var existingUser = await _IUserRepository.GetUserByEmailAsync(email);

        if (existingUser != null)
            return existingUser;

        var user = new User
        {
            Username = username,
            Email = email,
            Password = string.Empty
        };

        var userId = await _IUserRepository.CreateUser(user);
        user.Id = userId;

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