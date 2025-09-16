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
}