using System.Net;
using Layer4Balancer.Config;
using Serilog;
using Layer4Balancer.Services;
using Layer4Balancer.Wrappers;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

var logger = Log.ForContext<Program>();

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    logger.Information("Ctrl-C pressed");
    eventArgs.Cancel = true;
    cts.Cancel();
};

new ParseConfiguration(new EnvironmentWrapper()).Parse(Configuration.Instance);

if (Configuration.Instance.Backends.Length == 0)
{
    logger.Error("No backends configured. Exiting...");
    Environment.Exit(-1);
}

var backendRepository = new BackendRepository();
backendRepository.AddRange(Configuration.Instance.Backends);

var tcpClientWrapperFactory = () => new TcpClientWrapper();

var socketHandler = new SocketHandler(tcpClientWrapperFactory);
var checkAvailability = new CheckBackendAvailability(tcpClientWrapperFactory, backendRepository);

var loadBalancerService = new LoadBalancer(new TcpListenerWrapper(IPAddress.Any, Configuration.Instance.ListeningPort), backendRepository, socketHandler, checkAvailability);

logger.Information("Load balancer service started on port {ListeningPort}", Configuration.Instance.ListeningPort);
await loadBalancerService.StartAsync(cts.Token);
