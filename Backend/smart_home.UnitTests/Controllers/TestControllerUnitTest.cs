using SmartHome.Controllers;

namespace smart_home.UnitTests.Controllers;

public class TestControllerUnitTest
{
    [Fact]
    public void Hello_ReturnsHello()
    {
        Assert.Equal("Hello", new TestController().Hello());
    }
}
