using System.Net.Sockets;
using Layer4Balancer.Services;

namespace Layer4Balancer.Interfaces;

public interface ISocketHandler
{
    Task HandleConnection(TcpClient client, Backend backend, CancellationToken cancellationToken);
}