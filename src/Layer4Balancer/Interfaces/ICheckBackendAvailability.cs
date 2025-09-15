namespace Layer4Balancer.Interfaces;

public interface ICheckBackendAvailability
{
    Task StartCheckAsync(CancellationToken cancellationToken);
}