using System.Net;
using Layer4Balancer.Interfaces;
using Layer4Balancer.Services;
using NSubstitute;
using Shouldly;

namespace Layer4BalancerTests;

public class BalancerStrategyLeastConnectionTests
{
    private IBackendRepository _backendRespositoryMock = Substitute.For<IBackendRepository>();
    
    private readonly BalancerStrategyLeastConnection _sut;

    public BalancerStrategyLeastConnectionTests()
    {
        _sut = new BalancerStrategyLeastConnection(_backendRespositoryMock);
    }
    

    [Fact]
    public void GetNextAvailable_NoBackend_ReturnsNull()
    {
        // Arrange
        
        // Act
        var backend = _sut.GetNextAvailable();
        
        // Assert
        backend.ShouldBeNull();
    }

    [Fact]
    public void GetNextAvailable_OneBackendAvailable_ReturnsIt()
    {
        // Arrange
        const int port = 6666;
        _backendRespositoryMock
            .GetAll()
            .Returns([new Backend { IpAddress = IPAddress.Loopback, Port = port }]);
        
        // Act
        var backend = _sut.GetNextAvailable();
        
        // Assert
        backend.ShouldNotBeNull();
        backend.IpAddress.ShouldBe(IPAddress.Loopback);
        backend.Port.ShouldBe(port);
    }

    [Fact]
    public void GetNextAvailable_TwoBackendAvailable_SameConnectionCount_ReturnsFirst()
    {
        // Arrange
        const string ip1 = "8.8.8.8";
        const int port1 = 6666;
        const string ip2 = "4.4.4.4";
        const int port2 = 7777;
        
        var backend1 = new Backend { IpAddress = IPAddress.Parse(ip1), Port = port1 };
        var backend2 = new Backend { IpAddress = IPAddress.Parse(ip2), Port = port2 };
        _backendRespositoryMock
            .GetAll()
            .Returns([ backend1, backend2 ]);

        // Act
        var backend = _sut.GetNextAvailable();
        
        // Assert
        backend.ShouldNotBeNull();
        backend.IpAddress.ShouldBe(IPAddress.Parse(ip1));
        backend.Port.ShouldBe(port1);
    }

    [Fact]
    public void GetNextAvailable_TwoBackendAvailable_FirstHas1Connection_ReturnsSecond()
    {
        // Arrange
        const string ip1 = "8.8.8.8";
        const int port1 = 6666;
        const string ip2 = "4.4.4.4";
        const int port2 = 7777;

        var backend1 = new Backend { IpAddress = IPAddress.Parse(ip1), Port = port1 };
        var backend2 = new Backend { IpAddress = IPAddress.Parse(ip2), Port = port2 };
        _backendRespositoryMock
            .GetAll()
            .Returns([ backend1, backend2 ]);
        
        backend1.IncrementActiveConnectionCount();
        
        // Act
        var backend = _sut.GetNextAvailable();
        
        // Assert
        backend.ShouldNotBeNull();
        backend.IpAddress.ShouldBe(IPAddress.Parse(ip2));
        backend.Port.ShouldBe(port2);
    }

    [Fact]
    public void GetNextAvailable_TwoBackend_FirstIsNotAvailable_ReturnsSecond()
    {
        // Arrange
        const string ip1 = "8.8.8.8";
        const int port1 = 6666;
        const string ip2 = "4.4.4.4";
        const int port2 = 7777;
        
        var backend1 = new Backend { IpAddress = IPAddress.Parse(ip1), Port = port1 };
        var backend2 = new Backend { IpAddress = IPAddress.Parse(ip2), Port = port2 };
        _backendRespositoryMock
            .GetAll()
            .Returns([ backend1, backend2 ]);
        
        backend1.SetAvailable(false);
        
        // Act
        var backend = _sut.GetNextAvailable();
        
        // Assert
        backend.ShouldNotBeNull();
        backend.IpAddress.ShouldBe(IPAddress.Parse(ip2));
        backend.Port.ShouldBe(port2);
    }

    [Fact]
    public void GetNextAvailable_TwoBackend_NoneOfThemAvailable_ReturnsNull()
    {
        // Arrange
        const string ip1 = "8.8.8.8";
        const int port1 = 6666;
        const string ip2 = "4.4.4.4";
        const int port2 = 7777;
        
        var backend1 = new Backend { IpAddress = IPAddress.Parse(ip1), Port = port1 };
        var backend2 = new Backend { IpAddress = IPAddress.Parse(ip2), Port = port2 };
        _backendRespositoryMock
            .GetAll()
            .Returns([ backend1, backend2 ]);

        backend1.SetAvailable(false);
        backend2.SetAvailable(false);
        
        // Act
        var backend = _sut.GetNextAvailable();
        
        // Assert
        backend.ShouldBeNull();
    }
}