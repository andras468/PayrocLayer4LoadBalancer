using System.Net;
using Layer4Balancer.Services;
using Shouldly;

namespace Layer4BalancerTests;

public class BackendRepositoryTests
{
    private readonly BackendRepository _sut;

    public BackendRepositoryTests()
    {
        _sut = new BackendRepository();
    }
    
    [Fact]
    public void Add_GetAll_ReturnsRecord()
    {
        // Arrange
        const int port = 6666;
        
        // Act
        _sut.Add(IPAddress.IPv6Loopback, port);
        
        // Assert
        var result = _sut.GetAll().ToArray();
        result.ShouldHaveSingleItem();
        result[0].IpAddress.ShouldBe(IPAddress.IPv6Loopback);
        result[0].Port.ShouldBe(port);
    }
    
    [Fact]
    public void AddIpAsString_GetAll_ReturnsRecord()
    {
        // Arrange
        const string ip = "8.8.8.8";
        const int port = 6666;
        
        // Act
        _sut.Add(IPAddress.Parse(ip), 6666);
        
        // Assert
        var result = _sut.GetAll().ToArray();
        result.ShouldHaveSingleItem();
        result[0].IpAddress.ShouldBe(IPAddress.Parse(ip));
        result[0].Port.ShouldBe(port);
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
        _sut.Add(IPAddress.Loopback, port);
        
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
        
        _sut.Add(IPAddress.Parse(ip1), port1);
        _sut.Add(IPAddress.Parse(ip2), port2);

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
        
        _sut.Add(IPAddress.Parse(ip1), port1);
        _sut.Add(IPAddress.Parse(ip2), port2);

        _sut.GetAll().First(x => x.Port == port1).IncrementActiveConnectionCount();
        
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
        
        _sut.Add(IPAddress.Parse(ip1), port1);
        _sut.Add(IPAddress.Parse(ip2), port2);

        _sut.GetAll().First(x => x.Port == port1).SetAvailable(false);
        
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
        
        _sut.Add(IPAddress.Parse(ip1), port1);
        _sut.Add(IPAddress.Parse(ip2), port2);

        _sut.GetAll().First(x => x.Port == port1).SetAvailable(false);
        _sut.GetAll().First(x => x.Port == port2).SetAvailable(false);
        
        // Act
        var backend = _sut.GetNextAvailable();
        
        // Assert
        backend.ShouldBeNull();
    }
}