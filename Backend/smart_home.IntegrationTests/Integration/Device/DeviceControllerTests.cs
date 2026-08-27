using System.Net;
using smart_home.IntegrationTests.TestSupport.Fixtures;

namespace smart_home.IntegrationTests.Integration.Device;

public class DeviceControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DeviceControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDevice_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/device");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}