using Microsoft.AspNetCore.Mvc;
using Moq;
using SmartHome.Controllers;
using SmartHome.Model;
using SmartHome.Services;

namespace smart_home.UnitTests.Controllers;

public class UserControllerUnitTest
{
    private readonly Mock<IUserService> _userService = new();
    private readonly UserController _sut;

    public UserControllerUnitTest()
    {
        _sut = new UserController(_userService.Object);
    }

    [Fact]
    public void Hello_ReturnsHello()
    {
        Assert.Equal("Hello", _sut.Hello());
    }

    [Fact]
    public async Task GetUserById_ReturnsNotFound_WhenUserIsMissing()
    {
        _userService.Setup(s => s.GetUserByIdAsync(4)).ReturnsAsync((User?)null);

        var result = await _sut.GetUserById(4);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetUserById_ReturnsOk_WhenUserExists()
    {
        var user = new User { Id = 4, Username = "martell", Email = "a@b.com", Password = "x" };
        _userService.Setup(s => s.GetUserByIdAsync(4)).ReturnsAsync(user);

        var result = await _sut.GetUserById(4);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(user, ok.Value);
    }
}
