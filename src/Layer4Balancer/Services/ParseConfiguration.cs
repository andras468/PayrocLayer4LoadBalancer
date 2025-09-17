using System.Net;
using Layer4Balancer.Config;
using Layer4Balancer.Wrappers;
using Serilog;

namespace Layer4Balancer.Services;

public class ParseConfiguration
{
    private const string EnvBackendList = "LB_BACKEND_LIST";
    private const string EnvListeningPort = "LB_LISTENING_PORT";
    private const string EnvCheckIntervalMs = "LB_AVAILABILITY_CHECK_INTERVAL_MS";
    private const string EnvConnectionTimeoutMs = "LB_AVAILABILITY_CHECK_CONNECTION_TIMEOUT_MS";

    private readonly IEnvironmentWrapper _environmentWrapper;
    private readonly ILogger _logger;

    public ParseConfiguration(IEnvironmentWrapper environmentWrapper)
    {
        _environmentWrapper = environmentWrapper;
        _logger = Log.ForContext<ParseConfiguration>();
    }
    
    public Configuration Parse(Configuration configurationInstance)
    {
        var backendList = _environmentWrapper.GetEnvironmentVariable(EnvBackendList);
        configurationInstance.Backends = ParseBackendList(backendList).ToArray();
        if (configurationInstance.Backends.Length == 0)
        {
            _logger.Information("No valid backends found");
        }
        
        var listeningPortString = _environmentWrapper.GetEnvironmentVariable(EnvListeningPort);
        if (int.TryParse(listeningPortString, out var listeningPort) && listeningPort is > 0 and <= 65535)
        {
            configurationInstance.ListeningPort = listeningPort;
        }
        else
        {
            configurationInstance.ListeningPort = Configuration.DefaultListeningPort;
            _logger.Information("Listening port is invalid using default {DefaultListeningPort}", Configuration.DefaultListeningPort);
        }
        
        var checkIntervalMsString = _environmentWrapper.GetEnvironmentVariable(EnvCheckIntervalMs);
        if (int.TryParse(checkIntervalMsString, out var checkIntervalMs) && checkIntervalMs > 0)
        {
            configurationInstance.AvailabilityCheckInterval = TimeSpan.FromMilliseconds(checkIntervalMs);
        }
        else
        {
            configurationInstance.AvailabilityCheckInterval = TimeSpan.FromMilliseconds(Configuration.DefaultCheckIntervalMs);
            _logger.Information("Availability check interval is invalid using default {DefaultAvailabilityCheckInterval} ms", Configuration.DefaultCheckIntervalMs);
        }

        var connectionTimeoutMsString = _environmentWrapper.GetEnvironmentVariable(EnvConnectionTimeoutMs);
        if (int.TryParse(connectionTimeoutMsString, out var connectionTimeoutMs) && connectionTimeoutMs > 0)
        {
            configurationInstance.AvailabilityCheckConnectionTimeout = TimeSpan.FromMilliseconds(connectionTimeoutMs);
        }
        else
        {
            configurationInstance.AvailabilityCheckConnectionTimeout = TimeSpan.FromMilliseconds(Configuration.DefaultConnectionTimeoutMs);
            _logger.Information("Availability check timeout interval is invalid using default {DefaultAvailabilityCheckConnectionInterval} ms", Configuration.DefaultConnectionTimeoutMs);
        }

        return configurationInstance;
    }

    private BackendSettings[] ParseBackendList(string? backendList)
    {
        var backendAddressStrings = backendList?.Trim().Split(",") ?? [];
        var backendAddressList = new List<BackendSettings>();
        
        foreach (var backendAddressString in backendAddressStrings)
        {
            var addressParts = backendAddressString.Trim().Split(":");
            if (addressParts.Length == 2 &&
                IPAddress.TryParse(addressParts[0], out var ipAddress) &&
                int.TryParse(addressParts[1], out var port) &&
                port is > 0 and < 65535)
            {
                backendAddressList.Add(new BackendSettings { Address = ipAddress, Port = port });
                continue;
            }
            
            _logger.Information("Backend address is not valid: {InputBackendAddress}", backendAddressString);
        }
        
        return backendAddressList.ToArray();
    }
}