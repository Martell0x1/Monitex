namespace SmartHome.Config;

public class ISmartHomeConfig
{
  protected IConfigurationRoot ConfigObject()
  {
    var Config = new ConfigurationBuilder()
      .SetBasePath(Directory.GetCurrentDirectory())
      .AddJsonFile("appsettings.json",optional:false,reloadOnChange:true)
      .AddEnvironmentVariables()
      .Build();

    return Config;
  }
  public Task<T> Config<T>()
  {
    throw new NotImplementedException();
  }
}
