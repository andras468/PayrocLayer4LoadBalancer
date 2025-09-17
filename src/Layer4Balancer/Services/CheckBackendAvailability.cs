using Layer4Balancer.Config;
using Layer4Balancer.Interfaces;
using Layer4Balancer.Wrappers;
using Serilog;

namespace Layer4Balancer.Services;

public class CheckBackendAvailability : ICheckBackendAvailability
{
    private readonly IBackendRepository _backendRepository;
    private readonly ILogger _logger;
    private readonly TimeSpan _delay = Configuration.Instance.AvailabilityCheckInterval;
    private readonly TimeSpan _connectionTimeout = Configuration.Instance.AvailabilityCheckConnectionTimeout;
    private readonly Func<ITcpClientWrapper> _tcpClientFactory;

    public CheckBackendAvailability(Func<ITcpClientWrapper> tcpClientFactory, IBackendRepository backendRepository)
    {
        _tcpClientFactory = tcpClientFactory;
        _backendRepository = backendRepository;
        _logger = Log.ForContext<CheckBackendAvailability>();
    }

    public Task StartCheckAsync(CancellationToken cancellationToken)
    {
        _logger.Debug("Starting check backend availability");
        _ = Task.Run(async () => await ExecuteAsync(cancellationToken), cancellationToken);
        
        return Task.CompletedTask;
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var backend in _backendRepository.GetAll())
            {
                try
                {
                    using var client = _tcpClientFactory.Invoke();
                    var connectTask = client.ConnectAsync(backend.IpAddress, backend.Port, cancellationToken);
                    var timeoutTask = Task.Delay(_connectionTimeout, cancellationToken);
                    
                    await Task.WhenAny(connectTask, timeoutTask);
                    if (!connectTask.IsCompletedSuccessfully)
                    {
                        _logger.Information("Backend is offline {IpAddress}:{Port}", backend.IpAddress, backend.Port);
                        backend.SetAvailable(false);
                        continue;
                    }

                    if (!backend.Available)
                    {
                        _logger.Debug("Backend is back online {IpAddress}:{Port}", backend.IpAddress, backend.Port);
                    }
                    backend.SetAvailable(true);
                }
                catch (Exception e)
                {
                    backend.SetAvailable(false);
                    _logger.Information(e, "Exception raised while trying to connect to a backend {IpAddress}:{Port}", backend.IpAddress, backend.Port);
                }
            }
            
            await Task.Delay(_delay, cancellationToken);
        }
    }
}