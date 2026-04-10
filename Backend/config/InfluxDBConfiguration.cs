using InfluxDB.Client;

namespace SmartHome.Config;
public class InfluxDBConfiguration
{
  private readonly string _token;
  private readonly string _url;
  public readonly string _bucket;
  public readonly string? _org;

  public InfluxDBConfiguration(IConfiguration configuration)
  {
    _token = configuration["InfluxDB:Token"]
      ?? throw new InvalidOperationException("Missing InfluxDB:Token configuration.");
    _url = configuration["InfluxDB:Url"]
      ?? throw new InvalidOperationException("Missing InfluxDB:Url configuration.");
    _bucket = configuration["InfluxDB:Bucket"]
      ?? throw new InvalidOperationException("Missing InfluxDB:Bucket configuration.");
    _org = configuration["InfluxDB:Org"];
  }

  public InfluxDBClient GetClient()
  {
      var client = InfluxDBClientFactory.Create(_url, _token.ToCharArray());
      return client;
  }
}
