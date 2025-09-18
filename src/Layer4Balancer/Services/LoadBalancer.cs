using Layer4Balancer.Interfaces;
using Layer4Balancer.Wrappers;
using Serilog;

namespace Layer4Balancer.Services;

public class LoadBalancer : ILoadBalancerService
{
    private readonly ITcpListenerWrapper _listener;
    private readonly IBalancerStrategy _balancerStrategy;
    private readonly ISocketHandler _handler;
    private readonly ICheckBackendAvailability _checkBackendAvailability;
    private readonly ILogger _logger;
    
    public LoadBalancer(
        ITcpListenerWrapper listener,
        IBalancerStrategy balancerStrategy,
        ISocketHandler handler,
        ICheckBackendAvailability checkBackendAvailability)
    {
        _logger = Log.ForContext<LoadBalancer>();
        
        _balancerStrategy = balancerStrategy;
        _handler = handler;
        _checkBackendAvailability = checkBackendAvailability;

        _listener = listener;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _checkBackendAvailability.StartCheckAsync(cancellationToken);
            
        _logger.Debug("Starting load balancer");
        
        _listener.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.Debug("Waiting for new connection");
            var clientTask = _listener.AcceptTcpClientAsync(cancellationToken);
            
            var client = await clientTask;

            if (!clientTask.IsCompletedSuccessfully)
            {
                _logger.Information("Listener returned with an error");
                continue;
            }
            
            var backend = _balancerStrategy.GetNextAvailable();

            if (backend is null)
            {
                _logger.Warning("No available backend, closing client connection");
                client.Close();
                
                continue;
            }
            
            _logger.Debug("Selected backend is {IpAddress}:{Port}", backend.IpAddress, backend.Port);
            _ = Task.Run(() => _handler.HandleConnection(client, backend, cancellationToken), cancellationToken);
        }
        
        _logger.Debug("Load balancer finished");
    }
}