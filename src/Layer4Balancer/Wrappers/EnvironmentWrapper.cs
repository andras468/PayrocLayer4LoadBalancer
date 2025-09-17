using Layer4Balancer.Interfaces;

namespace Layer4Balancer.Wrappers;

public class EnvironmentWrapper : IEnvironmentWrapper
{
    public string? GetEnvironmentVariable(string variable)
    {
        return Environment.GetEnvironmentVariable(variable);
    }
}