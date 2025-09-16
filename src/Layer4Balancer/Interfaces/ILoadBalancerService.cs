namespace Layer4Balancer.Interfaces;

public interface ILoadBalancerService
{
    Task StartAsync(CancellationToken token);
}