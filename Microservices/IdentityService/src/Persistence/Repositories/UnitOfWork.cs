using IdentityService.Domain.Interfaces;
using IdentityService.Persistence.Context;

namespace IdentityService.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly IdentityDbContext _context;
    private IUserProfileRepository? _userProfiles;
    private IAuditLogRepository? _auditLogs;
    private bool _disposed;

    public UnitOfWork(IdentityDbContext context)
    {
        _context = context;
    }

    public IUserProfileRepository UserProfiles =>
        _userProfiles ??= new UserProfileRepository(_context);

    public IAuditLogRepository AuditLogs =>
        _auditLogs ??= new AuditLogRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }
}
