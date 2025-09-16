using System.Net;
using System.Net.Sockets;

namespace Layer4Balancer.Wrappers;

public class TcpClientWrapper : ITcpClientWrapper
{
    private readonly TcpClient _client;

    public Socket Client => _client.Client;
    
    public TcpClientWrapper()
    {
        _client = new TcpClient();
    }

    public TcpClientWrapper(TcpClient client)
    {
        _client = client;
    }

    public TcpClientWrapper(string hostname, int port)
    {
        _client = new TcpClient(hostname, port);
    }
    
    public Task ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken = default)
    {
        return _client.ConnectAsync(address, port, cancellationToken).AsTask();
    }

    public void Close()
    {
        _client.Close();
    }

    public Stream GetStream()
    {
        return _client.GetStream();
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}