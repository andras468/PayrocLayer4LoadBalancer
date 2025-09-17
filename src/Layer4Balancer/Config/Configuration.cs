namespace Layer4Balancer.Config;

public class Configuration
{
    public const int DefaultListeningPort = 7000;
    public const int DefaultCheckIntervalMs = 5000;
    public const int DefaultConnectionTimeoutMs = 1000;
    
    private static Configuration _instance = new();
    
    public static Configuration Instance => _instance;
    
    public int ListeningPort { get; set; } = DefaultListeningPort;

    public BackendSettings[] Backends { get; set; } = [];

    public TimeSpan AvailabilityCheckInterval { get; set; } = TimeSpan.FromMilliseconds(DefaultCheckIntervalMs);

    public TimeSpan AvailabilityCheckConnectionTimeout { get; set; } = TimeSpan.FromMilliseconds(DefaultConnectionTimeoutMs);
}