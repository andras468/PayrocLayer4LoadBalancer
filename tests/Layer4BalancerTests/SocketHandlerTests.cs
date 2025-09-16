using System.Net;
using System.Text;
using Layer4Balancer.Services;
using Layer4Balancer.Wrappers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Layer4BalancerTests;

public class SocketHandlerTests
{
    private readonly SocketHandler _sut;
    private readonly ITcpClientWrapper _backendTcpClientMock = Substitute.For<ITcpClientWrapper>();
    private readonly ITcpClientWrapper _clientTcpClientMock = Substitute.For<ITcpClientWrapper>();

    public SocketHandlerTests()
    {
        _sut = new SocketHandler(() => _backendTcpClientMock);
    }

    [Fact]
    public async Task HandleConnection_ConnectToBackendFails_ClosesClient()
    {
        // Arrange
        var backend = new Backend { IpAddress = IPAddress.Loopback, Port = 5001 };
        _backendTcpClientMock
            .ConnectAsync(Arg.Any<IPAddress>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsForAnyArgs(new Exception());
        
        // Act
        await _sut.HandleConnection(_clientTcpClientMock, backend, CancellationToken.None);
        
        // Assert
        _clientTcpClientMock
            .Received(1)
            .Close();
    }

    [Fact]
    public async Task HandleConnection_ConnectToBackendFails_ActiveConnectionsStaysTheSame()
    {
        // Arrange
        var backend = new Backend { IpAddress = IPAddress.Loopback, Port = 5001 };
        backend.IncrementActiveConnectionCount();
        
        _backendTcpClientMock
            .ConnectAsync(Arg.Any<IPAddress>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsForAnyArgs(new Exception());
        
        // Act
        await _sut.HandleConnection(_clientTcpClientMock, backend, CancellationToken.None);
        
        // Assert
        backend.ActiveConnectionCount.ShouldBe(1);
    }
    
    [Fact]
    public Task HandleConnection_ConnectedToBackend_ActiveConnectionsGoesUp()
    {
        // Arrange
        var mre = new ManualResetEventSlim(false);
        
        var backend = new Backend { IpAddress = IPAddress.Loopback, Port = 5001 };
        backend.IncrementActiveConnectionCount();
        
        _backendTcpClientMock
            .ConnectAsync(Arg.Any<IPAddress>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        _clientTcpClientMock
            .GetStream()
            .ReturnsForAnyArgs(_ =>
            {
                mre.Wait(1000);
                return new MemoryStream();
            });
        
        // Act
        _ = _sut.HandleConnection(_clientTcpClientMock, backend, CancellationToken.None);
        
        // Assert
        backend.ActiveConnectionCount.ShouldBe(1);
        mre.Set();

        return Task.CompletedTask;
    }
    
    [Fact]
    public async Task HandleConnection_ConnectedToBackend_ErrorDuringStream_ClosesAllClientsAndNoIncreaseInActiveConnections()
    {
        // Arrange
        var backend = new Backend { IpAddress = IPAddress.Loopback, Port = 5001 };
        
        _backendTcpClientMock
            .ConnectAsync(Arg.Any<IPAddress>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        // Act
        await _sut.HandleConnection(_clientTcpClientMock, backend, CancellationToken.None);
        
        // Assert
        _backendTcpClientMock
            .Received(1)
            .Close();
        
        _clientTcpClientMock
            .Received(1)
            .Close();
    }
    
    [Fact]
    public async Task HandleConnection_ConnectedToBackend_ErrorDuringStream_NoIncreaseInActiveConnections()
    {
        // Arrange
        var backend = new Backend { IpAddress = IPAddress.Loopback, Port = 5001 };
        
        _backendTcpClientMock
            .ConnectAsync(Arg.Any<IPAddress>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        // Act
        await _sut.HandleConnection(_clientTcpClientMock, backend, CancellationToken.None);
        
        // Assert
        backend.ActiveConnectionCount.ShouldBe(0);
    }
    
    [Fact]
    public async Task HandleConnection_ConnectedToBackend_ClientStreamCopiedToBackend()
    {
        // Arrange
        var backend = new Backend { IpAddress = IPAddress.Loopback, Port = 5001 };
        
        _backendTcpClientMock
            .ConnectAsync(Arg.Any<IPAddress>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        
        const string clientTestData = "ClientTestData";
        var clientStream = new MemoryStream(Encoding.UTF8.GetBytes(clientTestData));
        _clientTcpClientMock
            .GetStream()
            .ReturnsForAnyArgs(clientStream);
        
        var backendStream = new MemoryStream();
        _backendTcpClientMock
            .GetStream()
            .ReturnsForAnyArgs(backendStream);
        
        // Act
        await _sut.HandleConnection(_clientTcpClientMock, backend, CancellationToken.None);
        
        // Assert
        Encoding.UTF8.GetString(backendStream.ToArray()).ShouldBe(clientTestData);
    }
    
    [Fact]
    public async Task HandleConnection_ConnectedToBackend_BackendStreamCopiedToClient()
    {
        // Arrange
        var backend = new Backend { IpAddress = IPAddress.Loopback, Port = 5001 };
        
        _backendTcpClientMock
            .ConnectAsync(Arg.Any<IPAddress>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        var clientStream = new MemoryStream();
        _clientTcpClientMock
            .GetStream()
            .ReturnsForAnyArgs(clientStream);
        
        
        const string backendTestData = "BackendTestData";
        var backendStream = new MemoryStream(Encoding.UTF8.GetBytes(backendTestData));
        _backendTcpClientMock
            .GetStream()
            .ReturnsForAnyArgs(backendStream);
        
        // Act
        await _sut.HandleConnection(_clientTcpClientMock, backend, CancellationToken.None);
        
        // Assert
        Encoding.UTF8.GetString(clientStream.ToArray()).ShouldBe(backendTestData);
    }
}