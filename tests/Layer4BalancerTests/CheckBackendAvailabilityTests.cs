using System.Net;
using Layer4Balancer.Interfaces;
using Layer4Balancer.Services;
using Layer4Balancer.Wrappers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;

namespace Layer4BalancerTests;

public class CheckBackendAvailabilityTests
{
    private readonly IBackendRepository _backendRepository = Substitute.For<IBackendRepository>();
    private readonly ITcpClientWrapper _tcpClientWrapperMock = Substitute.For<ITcpClientWrapper>();
    
    private readonly CheckBackendAvailability _sut;
    
    public CheckBackendAvailabilityTests()
    {
        _sut = new CheckBackendAvailability(() => _tcpClientWrapperMock, _backendRepository);
    }

    [Fact]
    public async Task CheckBackendAvailability_StartsCheck_OneBackend_AbleToConnect_AvailableSetToTrue()
    {
        // Arrange
        var mre = new ManualResetEventSlim(false);
        
        const int port = 5000;
        
        using var cts = new CancellationTokenSource();

        var backend = new Backend { IpAddress = IPAddress.Loopback, Port = port };
        _backendRepository
            .GetAll()
            .Returns(_ =>
            {
                mre.Wait(1000);
                return [backend];
            });

        _tcpClientWrapperMock
            .ConnectAsync(Arg.Any<IPAddress>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        
        // Act
        _ = _sut.StartCheckAsync(cts.Token);
        await cts.CancelAsync();
        mre.Set();

        // Assert
        backend.Available.ShouldBe(true);
    }

    [Fact]
    public async Task CheckBackendAvailability_StartsCheck_OneBackend_AbleToConnect_DoesNotResetActiveConnectionsCount()
    {
        // Arrange
        var mre = new ManualResetEventSlim(false);
        
        const int port = 5000;
        
        using var cts = new CancellationTokenSource();

        var backend = new Backend { IpAddress = IPAddress.Loopback, Port = port };
        backend.IncrementActiveConnectionCount();
        backend.IncrementActiveConnectionCount();

        _backendRepository
            .GetAll()
            .Returns(_ =>
            {
                mre.Wait(1000);
                return [backend];
            });

        _tcpClientWrapperMock
            .ConnectAsync(Arg.Any<IPAddress>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.CompletedTask);
        
        // Act
        _ = _sut.StartCheckAsync(cts.Token);
        await cts.CancelAsync();
        mre.Set();
        
        // Assert
        backend.ActiveConnectionCount.ShouldBe(2);
    }

    [Fact]
    public async Task CheckBackendAvailability_StartsCheck_OneBackend_NotAbleToConnect_AvailableSetToFalse()
    {
        // Arrange
        const int port = 5000;
        
        using var cts = new CancellationTokenSource();

        var backend = new Backend { IpAddress = IPAddress.Loopback, Port = port };
        _backendRepository
            .GetAll()
            .Returns([backend]);

        _tcpClientWrapperMock
            .ConnectAsync(Arg.Any<IPAddress>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsForAnyArgs(_ => throw new Exception());
        
        // Act
        _ = _sut.StartCheckAsync(cts.Token);
        await Task.Delay(100, cts.Token);
        await cts.CancelAsync();
        
        // Assert
        backend.Available.ShouldBe(false);
    }

    [Fact]
    public async Task CheckBackendAvailability_StartsCheck_OneBackend_NotAbleToConnect_ResetsActiveConnectionCount()
    {
        // Arrange
        const int port = 5000;
        
        using var cts = new CancellationTokenSource();

        var backend = new Backend { IpAddress = IPAddress.Loopback, Port = port };
        backend.IncrementActiveConnectionCount();
        backend.IncrementActiveConnectionCount();
        
        _backendRepository
            .GetAll()
            .Returns([backend]);

        _tcpClientWrapperMock
            .ConnectAsync(Arg.Any<IPAddress>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsForAnyArgs(_ => throw new Exception());
        
        // Act
        _ = _sut.StartCheckAsync(cts.Token);
        await Task.Delay(100, cts.Token);
        await cts.CancelAsync();
        
        // Assert
        backend.ActiveConnectionCount.ShouldBe(0);
    }
}