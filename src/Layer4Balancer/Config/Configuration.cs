namespace Layer4Balancer.Config;

public class Configuration
{
    private static Configuration _instance = new();
    
    public static Configuration Instance => _instance;
    
    public int ListeningPort { get; set; } = 7000;

    public BackendSettings[] Backends { get; set; } = [];

    public TimeSpan AvailabilityCheckInterval { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan AvailabilityCheckConnectionTimeout { get; set; } = TimeSpan.FromSeconds(5);
}