using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using smart_home.IntegrationTests.TestSupport.Fixtures;

namespace smart_home.IntegrationTests.Integration.Api;

public class AuthApiTests : IClassFixture<CustomWebApplicationFactory>
{
	private readonly HttpClient _client;

	public AuthApiTests(CustomWebApplicationFactory factory)
	{
		_client = factory.CreateClient(new WebApplicationFactoryClientOptions
		{
			AllowAutoRedirect = false
		});
	}

	[Fact]
	public async Task GoogleLogin_StartsGoogleChallenge()
	{
		var response = await _client.GetAsync("/api/auth/google-login");

		Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
		Assert.NotNull(response.Headers.Location);
	}
}
