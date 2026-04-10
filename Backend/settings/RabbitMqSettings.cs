namespace SmartHome.Settings;
public class RabbitMqSettings
{
    public required string Url { get; set; }
    public required string Exchange { get; set; }
    public required string DashboardQueue { get; set; }
    public required string InfluxQueue { get; set; }
    public required string PythonModelQueue { get; set; }
    public required string PythonResultsQueue { get; set; }
    public required string DashboardRoutingKey { get; set; }
    public required string InfluxRoutingKey { get; set; }
    public required string PythonModelRoutingKey { get; set; }
    public required string PythonResultsRoutingKey { get; set; }
}
