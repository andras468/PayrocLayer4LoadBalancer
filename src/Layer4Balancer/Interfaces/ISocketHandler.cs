using Layer4Balancer.Services;
using Layer4Balancer.Wrappers;

namespace Layer4Balancer.Interfaces;

public interface ISocketHandler
{
    Task HandleConnection(ITcpClientWrapper client, Func<ITcpClientWrapper> tcpClientFactory, Backend backend, CancellationToken cancellationToken);
}