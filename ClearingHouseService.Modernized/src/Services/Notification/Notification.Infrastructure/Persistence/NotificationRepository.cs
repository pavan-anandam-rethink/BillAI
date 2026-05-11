using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;
using Notification.Domain.Interfaces;

namespace Notification.Infrastructure.Persistence;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _context;

    public NotificationRepository(NotificationDbContext context) => _context = context;

    public async Task<NotificationRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Notifications.FindAsync(new object[] { id }, cancellationToken);

    public async Task<IReadOnlyList<NotificationRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Notifications.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NotificationRecord>> FindAsync(Expression<Func<NotificationRecord, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.Notifications.Where(predicate).ToListAsync(cancellationToken);

    public async Task<NotificationRecord> AddAsync(NotificationRecord entity, CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task UpdateAsync(NotificationRecord entity, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(NotificationRecord entity, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Expression<Func<NotificationRecord, bool>>? predicate = null, CancellationToken cancellationToken = default) =>
        predicate is null ? await _context.Notifications.CountAsync(cancellationToken) : await _context.Notifications.CountAsync(predicate, cancellationToken);

    public async Task<bool> ExistsAsync(Expression<Func<NotificationRecord, bool>> predicate, CancellationToken cancellationToken = default) =>
        await _context.Notifications.AnyAsync(predicate, cancellationToken);

    public async Task<IReadOnlyList<NotificationRecord>> GetUnsentAsync(CancellationToken cancellationToken = default) =>
        await _context.Notifications.Where(n => !n.IsSent).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NotificationRecord>> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default) =>
        await _context.Notifications.Where(n => n.CorrelationId == correlationId).ToListAsync(cancellationToken);
}
