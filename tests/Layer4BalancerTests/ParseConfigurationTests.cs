using System.Net;
using Layer4Balancer.Config;
using Layer4Balancer.Services;
using Layer4Balancer.Wrappers;
using NSubstitute;
using Shouldly;

namespace Layer4BalancerTests;

public class ParseConfigurationTests
{
    private const string EnvBackendList = "LB_BACKEND_LIST";
    private const string EnvListeningPort = "LB_LISTENING_PORT";
    private const string EnvCheckIntervalMs = "LB_AVAILABILITY_CHECK_INTERVAL_MS";
    private const string EnvConnectionTimeoutMs = "LB_AVAILABILITY_CHECK_CONNECTION_TIMEOUT_MS";
    
    private readonly IEnvironmentWrapper _environmentWrapperMock = Substitute.For<IEnvironmentWrapper>();

    private readonly Configuration _configuration;
    
    private readonly ParseConfiguration _sut;

    public ParseConfigurationTests()
    {
        _configuration = new Configuration();
        
        _sut = new ParseConfiguration(_environmentWrapperMock);
    }

    [Theory]
    [InlineData(EnvBackendList)]
    [InlineData(EnvListeningPort)]
    [InlineData(EnvCheckIntervalMs)]
    [InlineData(EnvConnectionTimeoutMs)]
    public void Parse_Gets_AppropriateVariable(string envVariableName)
    {
        // Arrange
        
        // Act
        _sut.Parse(_configuration);
        
        // Assert
        _environmentWrapperMock
            .Received(1)
            .GetEnvironmentVariable(Arg.Is(envVariableName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("     ")]
    [InlineData("invalidIpAddress")]
    [InlineData("invalidIpAddress1;invalidIpAddress2")]
    [InlineData("127.0.0.1:notAPort1;127.0.0.1:notAPort2")]
    public void Parse_BackendList_Invalid_SetsBackendListInConfigurationObjectEmpty(string? backendListString)
    {
        // Arrange
        _configuration.Backends = [new BackendSettings()];
        
        _environmentWrapperMock
            .GetEnvironmentVariable(Arg.Is<string>(s => s == EnvBackendList))
            .Returns(backendListString);
        
        // Act
        _sut.Parse(_configuration);
        
        // Assert
        _configuration.Backends.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("127.0.0.1:-1")]
    [InlineData("127.0.0.1:0")]
    [InlineData("127.0.0.1:65536")]
    [InlineData("127.0.0.1:1000000")]
    public void Parse_BackendList_PortIsOutsideRange(string? backendListString)
    {
        // Arrange
        _configuration.Backends = [new BackendSettings()];
        
        _environmentWrapperMock
            .GetEnvironmentVariable(Arg.Is<string>(s => s == EnvBackendList))
            .Returns(backendListString);
        
        // Act
        _sut.Parse(_configuration);
        
        // Assert
        _configuration.Backends.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("127.0.0.1:-1;127.0.0.1:10000")]
    [InlineData("invalidIpAddress:10000;127.0.0.1:10000")]
    public void Parse_OneBackendConfigOutOfTwoIsValid_OnlyOneAddedToTheConfiguration(string? backendListString)
    {
        // Arrange
        _configuration.Backends = [new BackendSettings()];
        
        _environmentWrapperMock
            .GetEnvironmentVariable(Arg.Is<string>(s => s == EnvBackendList))
            .Returns(backendListString);
        
        // Act
        _sut.Parse(_configuration);
        
        // Assert
        _configuration.Backends.ShouldHaveSingleItem();
        _configuration.Backends[0].Address.ShouldBe(IPAddress.Loopback);
        _configuration.Backends[0].Port.ShouldBe(10000);
    }

    [Fact]
    public void Parse_TwoValidBackendConfiguration_ConfigurationHaveTwoBackendSettings()
    {
        // Arrange
        _configuration.Backends = [new BackendSettings()];
        
        _environmentWrapperMock
            .GetEnvironmentVariable(Arg.Is<string>(s => s == EnvBackendList))
            .Returns("127.0.0.1:7510;8.8.8.8:10000");
        
        // Act
        _sut.Parse(_configuration);
        
        // Assert
        _configuration.Backends.Length.ShouldBe(2);
        _configuration.Backends[0].Address.ShouldBe(IPAddress.Loopback);
        _configuration.Backends[0].Port.ShouldBe(7510);
        _configuration.Backends[1].Address.ShouldBe(IPAddress.Parse("8.8.8.8"));
        _configuration.Backends[1].Port.ShouldBe(10000);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("     ")]
    [InlineData("testNotANumber")]
    [InlineData("3.1415926")]
    public void Parse_ListeningPort_Invalid_SetsListeningPortToDefault(string? listeningPortString)
    {
        // Arrange
        _environmentWrapperMock
            .GetEnvironmentVariable(Arg.Is<string>(s => s == EnvListeningPort))
            .Returns(listeningPortString);
        
        // Act
        _sut.Parse(_configuration);
        
        // Assert
        _configuration.ListeningPort.ShouldBe(Configuration.DefaultListeningPort);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("65536")]
    [InlineData("1000000")]
    public void Parse_ListeningPort_NumberButOutOfBounds_SetsListeningPortToDefault(string? listeningPortString)
    {
        // Arrange
        _environmentWrapperMock
            .GetEnvironmentVariable(Arg.Is<string>(s => s == EnvListeningPort))
            .Returns(listeningPortString);
        
        // Act
        _sut.Parse(_configuration);
        
        // Assert
        _configuration.ListeningPort.ShouldBe(Configuration.DefaultListeningPort);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("65535", 65535)]
    [InlineData("10000", 10000)]
    [InlineData("8888", 8888)]
    public void Parse_ListeningPort_Valid_SetsListeningPortToConfigured(string? listeningPortString, int expectedListeningPort)
    {
        // Arrange
        _environmentWrapperMock
            .GetEnvironmentVariable(Arg.Is<string>(s => s == EnvListeningPort))
            .Returns(listeningPortString);
        
        // Act
        _sut.Parse(_configuration);
        
        // Assert
        _configuration.ListeningPort.ShouldBe(expectedListeningPort);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("     ")]
    [InlineData("testNotANumber")]
    [InlineData("3.1415926")]
    [InlineData("4000000000")]
    public void Parse_CheckInterval_Invalid_SetsCheckIntervalToToDefault(string? checkIntervalString)
    {
        // Arrange
        _environmentWrapperMock
            .GetEnvironmentVariable(Arg.Is<string>(s => s == EnvCheckIntervalMs))
            .Returns(checkIntervalString);
        
        // Act
        _sut.Parse(_configuration);
        
        // Assert
        _configuration.AvailabilityCheckInterval.ShouldBe(TimeSpan.FromMilliseconds(Configuration.DefaultCheckIntervalMs));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Parse_CheckInterval_NumberButOutOfBounds_SetsCheckIntervalToDefault(string? checkIntervalString)
    {
        // Arrange
        _environmentWrapperMock
            .GetEnvironmentVariable(Arg.Is<string>(s => s == EnvCheckIntervalMs))
            .Returns(checkIntervalString);
        
        // Act
        _sut.Parse(_configuration);
        
        // Assert
        _configuration.AvailabilityCheckInterval.ShouldBe(TimeSpan.FromMilliseconds(Configuration.DefaultCheckIntervalMs));
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("10", 10)]
    [InlineData("200", 200)]
    [InlineData("2000000", 2000000)]
    public void Parse_CheckInterval_Valid_SetsCheckIntervalToConfigured(string? checkIntervalString, int expectedCheckInterval)
    {
        // Arrange
        _configuration.Backends = [new BackendSettings()];
        
        _environmentWrapperMock
            .GetEnvironmentVariable(Arg.Is<string>(s => s == EnvCheckIntervalMs))
            .Returns(checkIntervalString);
        
        // Act
        _sut.Parse(_configuration);
        
        // Assert
        _configuration.AvailabilityCheckInterval.ShouldBe(TimeSpan.FromMilliseconds(expectedCheckInterval));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("     ")]
    [InlineData("testNotANumber")]
    [InlineData("3.1415926")]
    [InlineData("4000000000")]
    public void Parse_TimeoutInterval_Invalid_SetsTimeoutIntervalToToDefault(string? timeoutIntervalString)
    {
        // Arrange
        _environmentWrapperMock
            .GetEnvironmentVariable(Arg.Is<string>(s => s == EnvConnectionTimeoutMs))
            .Returns(timeoutIntervalString);
        
        // Act
        _sut.Parse(_configuration);
        
        // Assert
        _configuration.AvailabilityCheckConnectionTimeout.ShouldBe(TimeSpan.FromMilliseconds(Configuration.DefaultConnectionTimeoutMs));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void Parse_TimeoutInterval_NumberButOutOfBounds_SetsTimeoutIntervalToDefault(string? timeoutIntervalString)
    {
        // Arrange
        _environmentWrapperMock
            .GetEnvironmentVariable(Arg.Is<string>(s => s == EnvConnectionTimeoutMs))
            .Returns(timeoutIntervalString);
        
        // Act
        _sut.Parse(_configuration);
        
        // Assert
        _configuration.AvailabilityCheckConnectionTimeout.ShouldBe(TimeSpan.FromMilliseconds(Configuration.DefaultConnectionTimeoutMs));
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("10", 10)]
    [InlineData("200", 200)]
    [InlineData("2000000", 2000000)]
    public void Parse_TimeoutInterval_Valid_SetsTimeoutIntervalToConfigured(string? timeoutIntervalString, int expectedTimeoutInterval)
    {
        // Arrange
        _configuration.Backends = [new BackendSettings()];
        
        _environmentWrapperMock
            .GetEnvironmentVariable(Arg.Is<string>(s => s == EnvConnectionTimeoutMs))
            .Returns(timeoutIntervalString);
        
        // Act
        _sut.Parse(_configuration);
        
        // Assert
        _configuration.AvailabilityCheckConnectionTimeout.ShouldBe(TimeSpan.FromMilliseconds(expectedTimeoutInterval));
    }
}