using System.Net;
using smart_home.IntegrationTests.TestSupport.Fixtures;

namespace smart_home.IntegrationTests.Integration.Smoke;

public class AppBootAndAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly CustomWebApplicationFactory _factory;

	public AppBootAndAuthorizationTests(CustomWebApplicationFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task ProtectedRoute_WithoutAuthentication_ReturnsUnauthorized()
	{
		using var client = _factory.CreateClient();

		var response = await client.GetAsync("/api/device");

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}

	[Fact]
	public async Task ProtectedRoute_WithAuthentication_ReachesController()
	{
		using var client = _factory.CreateAuthenticatedClient();

		var response = await client.GetAsync("/api/device");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("Hello", await response.Content.ReadAsStringAsync());
	}
}
