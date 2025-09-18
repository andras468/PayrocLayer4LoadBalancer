using Layer4Balancer.Services;

namespace Layer4Balancer.Interfaces;

public interface IBalancerStrategy
{
    Backend? GetNextAvailable();
}