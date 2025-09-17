namespace Layer4Balancer.Wrappers;

public interface ITcpListenerWrapper
{
    Task<ITcpClientWrapper> AcceptTcpClientAsync(CancellationToken cancellationToken = default);

    void Start();
}