namespace SmartHome.Settings;
public class RabbitMqSettings
{
    public required string Url { get; set; }
    public required string Exchange { get; set; }
    public required string Queue { get; set; }
    public required string RoutingKey { get; set; }
}
