using System.Net;
using System.Net.Sockets;

namespace Layer4Balancer.Wrappers;

public class TcpListenerWrapper : ITcpListenerWrapper
{
    private readonly TcpListener _listener;

    public TcpListenerWrapper(IPAddress listeningAddress, int listeningPort)
    {
        _listener = new TcpListener(listeningAddress, listeningPort);
    }
    
    public async Task<ITcpClientWrapper> AcceptTcpClientAsync(CancellationToken cancellationToken)
    {
        return new TcpClientWrapper(await _listener.AcceptTcpClientAsync(cancellationToken));
    }

    public void Start()
    {
        _listener.Start();
    }
}