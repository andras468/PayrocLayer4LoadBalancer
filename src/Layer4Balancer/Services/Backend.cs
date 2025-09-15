using System.Net;

namespace Layer4Balancer.Services;

public class Backend
{
    private readonly object _lock = new();

    private int _activeConnectionCount;
    private bool _available = true;

    public IPAddress IpAddress { get; set; } = IPAddress.Loopback;
    
    public int Port { get; set; }

    public int ActiveConnectionCount => _activeConnectionCount;

    public void IncrementActiveConnectionCount()
    {
        Interlocked.Increment(ref _activeConnectionCount);
    }

    public void DecrementActiveConnectionCount()
    {
        Interlocked.Decrement(ref _activeConnectionCount);
    }

    public bool Available => _available;

    public void SetAvailable(bool available)
    {
        lock (_lock)
        {
            _available = available;

            if (!available)
            {
                Interlocked.Exchange(ref _activeConnectionCount, 0);
            }
        }
    }
}