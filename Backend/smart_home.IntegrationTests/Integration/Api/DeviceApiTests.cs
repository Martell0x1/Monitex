using System.Net;
using smart_home.IntegrationTests.TestSupport.Fixtures;

namespace smart_home.IntegrationTests.Integration.Api;

public class DeviceApiTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly CustomWebApplicationFactory _factory;

	public DeviceApiTests(CustomWebApplicationFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task GetDevice_WithAuthentication_ReturnsHello()
	{
		using var client = _factory.CreateAuthenticatedClient();

		var response = await client.GetAsync("/api/device");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("Hello", await response.Content.ReadAsStringAsync());
	}
}
