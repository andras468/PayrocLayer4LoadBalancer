using System.Net;
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

    public Backend? GetNextAvailable()
    {
        lock (_lock)
        {
            var backendsWithMinimumConnections = _backends
                .Where(backend => backend.Available)
                .ToArray();

            if (backendsWithMinimumConnections.Length == 0)
            {
                return null;
            }

            var minimumConnections = backendsWithMinimumConnections.Min(backend => backend.ActiveConnectionCount);
            return backendsWithMinimumConnections.FirstOrDefault(backend => backend.ActiveConnectionCount == minimumConnections);
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