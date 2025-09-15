using System.Collections;
using System.Net;
using Layer4Balancer.Services;

namespace Layer4Balancer.Interfaces;

public interface IBackendRepository
{
    void Add(IPAddress ipAddress, int port);
    
    void Add(string ipAddress, int port);
    
    Backend? GetNextAvailable();
    
    IEnumerable<Backend> GetAll();
}