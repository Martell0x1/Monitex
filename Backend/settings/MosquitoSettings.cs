namespace SmartHome.Settings;

public class MosquittoSettings
{
    public required string Ip { get; set; }
    public int Port { get; set; }
    public required string Topic { get; set; }
}
