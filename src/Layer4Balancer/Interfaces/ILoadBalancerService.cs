using Layer4Balancer.Wrappers;

namespace Layer4Balancer.Interfaces;

public interface ILoadBalancerService
{
    Task StartAsync(Func<ITcpClientWrapper> tcpClientFactory, CancellationToken token);
}