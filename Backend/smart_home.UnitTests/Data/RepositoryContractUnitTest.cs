using Microsoft.Extensions.Configuration;
using SmartHome.Data;
using SmartHome.Data.Repositories;
using SmartHome.Model;

namespace smart_home.UnitTests.Data;

public class RepositoryContractUnitTest
{
    private readonly UserRepository _users;
    private readonly DeviceRepository _devices;
    private readonly SensorRepository _sensors;

    public RepositoryContractUnitTest()
    {
        var context = new PostgresDbContext(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=monitex;Username=test;Password=test"
            })
            .Build());
        _users = new UserRepository(context);
        _devices = new DeviceRepository(context);
        _sensors = new SensorRepository(context);
    }

    [Fact]
    public void UserRepository_UnimplementedMembers_Throw()
    {
        Assert.Throws<NotImplementedException>(() => _users.DeleteUser(1).GetAwaiter().GetResult());
        Assert.Throws<NotImplementedException>(() => _users.EditUser(1, new User { Username = "a", Email = "a@b.com", Password = "x" }).GetAwaiter().GetResult());
        Assert.Throws<NotImplementedException>(() => _users.GetAllUsers().GetAwaiter().GetResult());
    }
}
