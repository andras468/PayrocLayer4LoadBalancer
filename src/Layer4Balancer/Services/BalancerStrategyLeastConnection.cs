using Layer4Balancer.Interfaces;

namespace Layer4Balancer.Services;

public class BalancerStrategyLeastConnection : IBalancerStrategy
{
    private readonly object _lock = new();
    private readonly IBackendRepository _backendRepository;

    public BalancerStrategyLeastConnection(IBackendRepository backendRepository)
    {
        _backendRepository = backendRepository;
    }
    
    public Backend? GetNextAvailable()
    {
        lock (_lock)
        {
            var backendsWithMinimumConnections = _backendRepository.GetAll()
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
}