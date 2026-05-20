namespace IdentityService.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserProfileRepository UserProfiles { get; }
    IAuditLogRepository AuditLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
