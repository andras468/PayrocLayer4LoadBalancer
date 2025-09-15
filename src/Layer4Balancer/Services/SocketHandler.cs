using Layer4Balancer.Interfaces;
using Layer4Balancer.Wrappers;
using Serilog;

namespace Layer4Balancer.Services;

public class SocketHandler : ISocketHandler
{
    private readonly ILogger _logger;

    public SocketHandler()
    {
        _logger = Log.ForContext<LoadBalancerService>();
    }

    public async  Task HandleConnection(ITcpClientWrapper client, Func<ITcpClientWrapper> tcpClientFactory, Backend backend, CancellationToken cancellationToken)
    {
        _logger.Information("Handling new connection from {RemoteIpAddressAndPort}", client.Client.RemoteEndPoint?.ToString());
        using var serverSocket = tcpClientFactory.Invoke();

        try
        {
            _logger.Debug("Connecting to backend endpoint {BackendIpAddress}:{BackendPort}", backend.IpAddress, backend.Port);
            await serverSocket.ConnectAsync(backend.IpAddress, backend.Port, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Exception while opening connection to backend, closing client connection");
            client.Close();
        }

        backend.IncrementActiveConnectionCount();
        _logger.Debug("Current active connection to backend is {ActiveConnectionCount}", backend.ActiveConnectionCount);

        try
        {
            var clientStream = client.GetStream();
            var backendStream = serverSocket.GetStream();

            var clientCopyTask = clientStream.CopyToAsync(backendStream, cancellationToken);
            var backendCopyTask = backendStream.CopyToAsync(clientStream, cancellationToken);

            _logger.Debug("Two-way streams are open, waiting for any of the tasks to finish");
            var finishedTask = await Task.WhenAny([clientCopyTask, backendCopyTask]);

            _logger.Debug(finishedTask == clientCopyTask ? "Client stream closed" : "Backend stream closed");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Exception raised while copying streams");
        }
        finally
        {
            client.Close();
            serverSocket.Close();
            backend.DecrementActiveConnectionCount();
        }
    }
}