using Microsoft.Extensions.Configuration;
using SmartHome.Data;
using SmartHome.Data.Repositories;

namespace smart_home.IntegrationTests.Integration.Repositories;

public class UserRepositoryTests
{
	[Fact]
	public void Repository_CanBeConstructedFromTestingConfiguration()
	{
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["ConnectionStrings:Postgres"] = "Host=localhost;Database=monitex"
			})
			.Build();

		var repository = new UserRepository(new PostgresDbContext(configuration));

		Assert.NotNull(repository);
	}
}
