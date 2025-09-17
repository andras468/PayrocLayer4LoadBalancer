using System.Net;
using Layer4Balancer.Interfaces;
using Layer4Balancer.Services;
using Layer4Balancer.Wrappers;
using NSubstitute;

namespace Layer4BalancerTests;

public class LoadBalancerTests
{
    private readonly ITcpListenerWrapper _tcpListenerMock = Substitute.For<ITcpListenerWrapper>();
    private readonly ITcpClientWrapper _tcpClientMock = Substitute.For<ITcpClientWrapper>();
    private readonly IBackendRepository _backendRepositoryMock = Substitute.For<IBackendRepository>();
    private readonly ISocketHandler _socketHandlerMock = Substitute.For<ISocketHandler>();
    private readonly ICheckBackendAvailability _checkBackendAvailabilityMock = Substitute.For<ICheckBackendAvailability>();
    
    private readonly LoadBalancer _sut;

    public LoadBalancerTests()
    {
        
        _sut = new LoadBalancer(
            _tcpListenerMock,
            _backendRepositoryMock,
            _socketHandlerMock,
            _checkBackendAvailabilityMock);
    }

    [Fact]
    public async Task StartAsync_Starts_CheckBackendAvailability()
    {
        // Arrange
        using var cts =  new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        var mre = new  ManualResetEventSlim(false);
        
        _tcpListenerMock
            .AcceptTcpClientAsync(Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(_ =>
            {
                mre.Set();
                return _tcpClientMock;
            });
        
        // Act
        _ = Task.Run(async () => await _sut.StartAsync(cancellationToken), cancellationToken);
        mre.Wait(1000);

        // Assert
        await _checkBackendAvailabilityMock
            .Received(1)
            .StartCheckAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_Starts_Listening()
    {
        // Arrange
        using var cts =  new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        var mre = new  ManualResetEventSlim(false);
        
        _tcpListenerMock
            .AcceptTcpClientAsync(Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(_ =>
            {
                mre.Set();
                return _tcpClientMock;
            });
        
        // Act
        _ = Task.Run(async () => await _sut.StartAsync(cancellationToken), cancellationToken);

        // Assert
        mre.Wait(1000);
        
        _tcpListenerMock
            .ReceivedWithAnyArgs(1)
            .Start();

        await cts.CancelAsync();
    }

    [Fact]
    public async Task StartAsync_ReturnsWithAcceptedConnection_NoBackend_ClosesConnection()
    {
        // Arrange
        using var cts =  new CancellationTokenSource();
        var cancellationToken = cts.Token;
        
        var mre = new  ManualResetEventSlim(false);
        
        _tcpClientMock
            .When(x => x.Close())
            .Do(_ =>
            {
                cts.Cancel();
                mre.Set();
            });

        _tcpListenerMock
            .AcceptTcpClientAsync(Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(_ => _tcpClientMock);
        
        // Act
        _ = Task.Run(async () => await _sut.StartAsync(cancellationToken), cancellationToken);

        // Assert
        mre.Wait(1000);
        
        _tcpClientMock
            .Received(1)
            .Close();
        
        await cts.CancelAsync();
    }
    
    [Fact]
    public async Task StartAsync_ReturnsWithAcceptedConnection_BackendAvailable_CallsHandleConnection()
    {
        // Arrange
        using var cts =  new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var backend = new Backend { IpAddress = IPAddress.Loopback, Port = 5002 };
        var mre = new  ManualResetEventSlim(false);

        _socketHandlerMock
            .When(x => x.HandleConnection(
                Arg.Any<ITcpClientWrapper>(),
                Arg.Any<Backend>(),
                Arg.Any<CancellationToken>()))
            .Do(_ =>
            {
                mre.Set();
                cts.Cancel();
            });
                
        _backendRepositoryMock
            .GetNextAvailable()
            .Returns(backend);
        
        _tcpListenerMock
            .AcceptTcpClientAsync(Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(_ => _tcpClientMock);
        
        // Act
        _ = Task.Run(async () => await _sut.StartAsync(cancellationToken), cancellationToken);
        mre.Wait(1000);

        // Assert
        await _socketHandlerMock
            .Received()
            .HandleConnection(
                Arg.Is<ITcpClientWrapper>(x => x == _tcpClientMock),
                Arg.Is<Backend>(x => x.Port == 5002),
                Arg.Any<CancellationToken>());
        
        await cts.CancelAsync();
    }
}