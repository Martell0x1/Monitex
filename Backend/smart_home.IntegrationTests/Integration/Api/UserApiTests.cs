using System.Net;
using smart_home.IntegrationTests.TestSupport.Fixtures;

namespace smart_home.IntegrationTests.Integration.Api;

public class UserApiTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly HttpClient _client;

	public UserApiTests(CustomWebApplicationFactory factory)
	{
		_client = factory.CreateClient();
	}

	[Fact]
	public async Task Hello_ReturnsGreeting()
	{
		var response = await _client.GetAsync("/api/async/users/hello");

		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		Assert.Equal("Hello", await response.Content.ReadAsStringAsync());
	}
}
