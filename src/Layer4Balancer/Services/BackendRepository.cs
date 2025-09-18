using System.Net;
using Layer4Balancer.Config;
using Layer4Balancer.Interfaces;

namespace Layer4Balancer.Services;

public class BackendRepository : IBackendRepository
{
    private readonly object _lock = new();
    private readonly List<Backend> _backends = new();
    
    public void Add(IPAddress ipAddress, int port)
    {
        lock (_lock)
        {
            _backends.Add(new Backend { IpAddress = ipAddress, Port = port });
        }
    }

    public void Add(string ipAddress, int port)
    {
        Add(IPAddress.Parse(ipAddress), port);
    }

    public void AddRange(IEnumerable<BackendSettings> backendSettings)
    {
        lock (_lock)
        {
            _backends.AddRange(
                backendSettings
                    .Select(x => new Backend { IpAddress = x.Address, Port = x.Port }));
        }
    }

    public IEnumerable<Backend> GetAll()
    {
        lock (_lock)
        {
            return _backends.AsEnumerable();
        }
    }
}