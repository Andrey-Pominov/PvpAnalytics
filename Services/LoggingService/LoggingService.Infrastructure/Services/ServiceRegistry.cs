using LoggingService.Core.Entities;
using LoggingService.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace LoggingService.Infrastructure.Services;

public interface IServiceRegistry
{
    Task<long> RegisterServiceAsync(string serviceName, string endpoint, string version, CancellationToken ct = default);
    Task<bool> UpdateHeartbeatAsync(string serviceName, CancellationToken ct = default);
    Task<List<RegisteredService>> GetRegisteredServicesAsync(CancellationToken ct = default);
    Task<RegisteredService?> GetServiceByNameAsync(string serviceName, CancellationToken ct = default);
    Task MarkServiceOfflineAsync(string serviceName, CancellationToken ct = default);
}

public class ServiceRegistry(LoggingDbContext dbContext) : IServiceRegistry
{
    public async Task<long> RegisterServiceAsync(string serviceName, string endpoint, string version, CancellationToken ct = default)
    {
        var existing = await dbContext.RegisteredServices
            .FirstOrDefaultAsync(s => s.ServiceName == serviceName, ct);

        if (existing != null)
        {
            existing.Endpoint = endpoint;
            existing.Version = version;
            existing.LastHeartbeat = DateTime.UtcNow;
            existing.Status = "Online";
            await dbContext.SaveChangesAsync(ct);
            return existing.Id;
        }

        var service = new RegisteredService
        {
            ServiceName = serviceName,
            Endpoint = endpoint,
            Version = version,
            RegisteredAt = DateTime.UtcNow,
            LastHeartbeat = DateTime.UtcNow,
            Status = "Online"
        };

        dbContext.RegisteredServices.Add(service);
        await dbContext.SaveChangesAsync(ct);
        return service.Id;
    }

    public async Task<bool> UpdateHeartbeatAsync(string serviceName, CancellationToken ct = default)
    {
        var service = await dbContext.RegisteredServices
            .FirstOrDefaultAsync(s => s.ServiceName == serviceName, ct);

        if (service == null)
            return false;

        service.LastHeartbeat = DateTime.UtcNow;
        service.Status = "Online";
        await dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<RegisteredService>> GetRegisteredServicesAsync(CancellationToken ct = default)
    {
        return await dbContext.RegisteredServices
            .OrderBy(s => s.ServiceName)
            .ToListAsync(ct);
    }

    public async Task<RegisteredService?> GetServiceByNameAsync(string serviceName, CancellationToken ct = default)
    {
        return await dbContext.RegisteredServices
            .FirstOrDefaultAsync(s => s.ServiceName == serviceName, ct);
    }

    public async Task MarkServiceOfflineAsync(string serviceName, CancellationToken ct = default)
    {
        var service = await dbContext.RegisteredServices
            .FirstOrDefaultAsync(s => s.ServiceName == serviceName, ct);

        if (service != null)
        {
            service.Status = "Offline";
            await dbContext.SaveChangesAsync(ct);
        }
    }
}

