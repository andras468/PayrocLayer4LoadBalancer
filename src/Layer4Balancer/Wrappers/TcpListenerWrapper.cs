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
    
    public ITcpClientWrapper AcceptTcpClient()
    {
        return new TcpClientWrapper(_listener.AcceptTcpClient());
    }

    public void Start()
    {
        _listener.Start();
    }
}