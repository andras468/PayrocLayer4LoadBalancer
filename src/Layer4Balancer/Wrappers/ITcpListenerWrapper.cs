namespace Layer4Balancer.Wrappers;

public interface ITcpListenerWrapper
{
    ITcpClientWrapper AcceptTcpClient();

    void Start();
}