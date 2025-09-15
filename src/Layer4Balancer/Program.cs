using System.Net;
using Serilog;
using Layer4Balancer.Services;

const int listeningPort = 7000;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

var logger = Log.ForContext<Program>();

var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    logger.Information("Ctrl-C pressed");
    eventArgs.Cancel = true;
    cts.Cancel();
};

var backendRepository = new BackendRepository();
backendRepository.Add(IPAddress.Loopback, 7001);
// backendRepository.Add(IPAddress.Loopback, 7002);

var socketHandler = new SocketHandler();
var checkAvailability = new CheckBackendAvailability(backendRepository);

var loadBalancerService = new LoadBalancerService(IPAddress.Loopback, listeningPort, backendRepository, socketHandler, checkAvailability);

logger.Information("Load balancer service started on port {ListeningPort}", listeningPort);
await loadBalancerService.StartAsync(2, cts.Token);
