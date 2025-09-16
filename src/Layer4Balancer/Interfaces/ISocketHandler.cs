using Layer4Balancer.Services;
using Layer4Balancer.Wrappers;

namespace Layer4Balancer.Interfaces;

public interface ISocketHandler
{
    Task HandleConnection(ITcpClientWrapper client, Backend backend, CancellationToken cancellationToken);
}