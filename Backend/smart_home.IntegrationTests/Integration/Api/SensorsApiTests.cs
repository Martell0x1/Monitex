using System.Net;
using System.Net.Http.Json;
using smart_home.IntegrationTests.TestSupport.Fixtures;

namespace smart_home.IntegrationTests.Integration.Api;

public class SensorsApiTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly CustomWebApplicationFactory _factory;

	public SensorsApiTests(CustomWebApplicationFactory factory)
	{
		_factory = factory;
	}

	[Fact]
	public async Task RegisterSensors_WithEmptyList_ReturnsBadRequest()
	{
		using var client = _factory.CreateAuthenticatedClient();

		var response = await client.PostAsJsonAsync("/api/sensors/bulk", Array.Empty<object>());

		Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
	}

	[Fact]
	public async Task GetSensors_WithoutAuthentication_ReturnsUnauthorized()
	{
		using var client = _factory.CreateClient();

		var response = await client.GetAsync("/api/sensors/device/1");

		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}
}
