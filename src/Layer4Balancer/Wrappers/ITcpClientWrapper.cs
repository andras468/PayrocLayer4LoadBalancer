using System.Net;
using System.Net.Sockets;

namespace Layer4Balancer.Wrappers;

public interface ITcpClientWrapper : IDisposable
{
    Socket Client { get; }
    
    ValueTask ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken = default);    
    
    void Close();
    
    NetworkStream GetStream();
}