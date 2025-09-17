using System.Net;

namespace Layer4Balancer.Config;

public class BackendSettings
{
    public IPAddress Address { get; set; } = IPAddress.Loopback;
    
    public int Port { get; set; }
}