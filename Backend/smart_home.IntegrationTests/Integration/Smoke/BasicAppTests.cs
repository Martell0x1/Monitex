using Microsoft.AspNetCore.Mvc.Testing;
using smart_home.IntegrationTests.TestSupport.Fixtures;

namespace smart_home.IntegrationTests.Integration.Smoke;

public class BasicAppTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public BasicAppTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task App_Should_Start()
    {
        var response = await _client.GetAsync("/");

        Assert.NotEqual(
            System.Net.HttpStatusCode.InternalServerError,
            response.StatusCode);
    }
}