using Moq;
using SmartHome.Data.Repositories;
using SmartHome.DTO;
using SmartHome.Model;
using SmartHome.Services;

namespace smart_home.UnitTests.Services;

public class UserServiceUnitTest
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly UserService _sut;

    public UserServiceUnitTest()
    {
        _sut = new UserService(_userRepository.Object);
    }

    [Fact]
    public async Task CreateUserAsync_ReturnsNull_WhenEmailAlreadyExists()
    {
        var dto = new RegisterDTO
        {
            Username = "martell",
            Email = "martell@example.com",
            Password = "Secret1!"
        };
        _userRepository
            .Setup(r => r.GetUserByEmailAsync(dto.Email))
            .ReturnsAsync(new User { Id = 1, Username = "existing", Email = dto.Email, Password = "hash" });

        var result = await _sut.CreateUserAsync(dto);

        Assert.Null(result);
        _userRepository.Verify(r => r.CreateUser(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateUserAsync_HashesPasswordAndAssignsId()
    {
        var dto = new RegisterDTO
        {
            Username = "martell",
            Email = "martell@example.com",
            Password = "Secret1!"
        };
        _userRepository.Setup(r => r.GetUserByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _userRepository.Setup(r => r.CreateUser(It.IsAny<User>())).ReturnsAsync(17);

        var result = await _sut.CreateUserAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(17, result.Id);
        Assert.Equal(dto.Username, result.Username);
        Assert.Equal(dto.Email, result.Email);
        Assert.NotEqual(dto.Password, result.Password);
        Assert.True(BCrypt.Net.BCrypt.Verify(dto.Password, result.Password));
        _userRepository.Verify(r => r.CreateUser(It.Is<User>(u =>
            u.Username == dto.Username &&
            u.Email == dto.Email &&
            u.Password != dto.Password)), Times.Once);
    }

    [Fact]
    public async Task CreateGoogleUserAsync_ReturnsExistingUser_WhenEmailExists()
    {
        var existing = new User
        {
            Id = 3,
            Username = "google-user",
            Email = "user@gmail.com",
            Password = string.Empty
        };
        _userRepository.Setup(r => r.GetUserByEmailAsync(existing.Email)).ReturnsAsync(existing);

        var result = await _sut.CreateGoogleUserAsync("other-name", existing.Email);

        Assert.Same(existing, result);
        _userRepository.Verify(r => r.CreateUser(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateGoogleUserAsync_CreatesUserWithEmptyPassword_WhenEmailIsNew()
    {
        _userRepository.Setup(r => r.GetUserByEmailAsync("user@gmail.com")).ReturnsAsync((User?)null);
        _userRepository.Setup(r => r.CreateUser(It.IsAny<User>())).ReturnsAsync(21);

        var result = await _sut.CreateGoogleUserAsync("google-user", "user@gmail.com");

        Assert.Equal(21, result.Id);
        Assert.Equal("google-user", result.Username);
        Assert.Equal("user@gmail.com", result.Email);
        Assert.Equal(string.Empty, result.Password);
    }

    [Fact]
    public async Task GetUserByIdAsync_DelegatesToRepository()
    {
        var user = new User { Id = 8, Username = "a", Email = "a@b.com", Password = "x" };
        _userRepository.Setup(r => r.GetUserById(8)).ReturnsAsync(user);

        var result = await _sut.GetUserByIdAsync(8);

        Assert.Same(user, result);
    }

    [Fact]
    public async Task GetUserByEmailAsync_DelegatesToRepository()
    {
        var user = new User { Id = 8, Username = "a", Email = "a@b.com", Password = "x" };
        _userRepository.Setup(r => r.GetUserByEmailAsync("a@b.com")).ReturnsAsync(user);

        var result = await _sut.GetUserByEmailAsync("a@b.com");

        Assert.Same(user, result);
    }

    [Fact]
    public async Task GetUserByIdSync_UsesDeviceLookup()
    {
        var user = new User { Id = 8, Username = "a", Email = "a@b.com", Password = "x" };
        _userRepository.Setup(r => r.GetUserByDeviceIdAsync(55)).ReturnsAsync(user);

        var result = await _sut.GetUserByIdSync(55);

        Assert.Same(user, result);
    }

    [Fact]
    public void UnimplementedMembers_ThrowNotImplemented()
    {
        Assert.Throws<NotImplementedException>(() => _sut.DeleteUser(1));
        Assert.Throws<NotImplementedException>(() => _sut.EditUser(1, new User { Username = "a", Email = "a@b.com", Password = "x" }));
        Assert.Throws<NotImplementedException>(() => _sut.GetAllUsers());
    }
}
