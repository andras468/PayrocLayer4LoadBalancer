namespace Layer4Balancer.Wrappers;

public interface IEnvironmentWrapper
{
    string? GetEnvironmentVariable(string variable);
}