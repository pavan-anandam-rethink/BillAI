using IdentityService.Domain.Entities;
using IdentityService.Domain.Interfaces;
using IdentityService.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Persistence.Repositories;

public class AuditLogRepository(IdentityDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(auditLog.Id))
            auditLog.Id = Guid.NewGuid().ToString("N");

        await context.AuditLogs.AddAsync(auditLog, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await context.AuditLogs
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(string entityName, string entityId, CancellationToken cancellationToken = default)
    {
        return await context.AuditLogs
            .Where(a => a.EntityName == entityName && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
