namespace Layer4Balancer.Interfaces;

public interface ILoadBalancerService
{
    Task StartAsync(int maxConnections, CancellationToken token);
}