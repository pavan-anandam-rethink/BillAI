using IdentityService.Domain.Entities;
using IdentityService.Domain.Interfaces;
using IdentityService.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IdentityService.Persistence.Repositories;

public class UserProfileRepository(IdentityDbContext context) : IUserProfileRepository
{
    public async Task<UserProfile?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await context.UserProfiles.FindAsync([id], cancellationToken);
    }

    public async Task<UserProfile?> FindOneAsync(Expression<Func<UserProfile, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await context.UserProfiles.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<IReadOnlyList<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.UserProfiles.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(UserProfile entity, CancellationToken cancellationToken = default)
    {
        await context.UserProfiles.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(UserProfile entity, CancellationToken cancellationToken = default)
    {
        context.UserProfiles.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(string id, bool softDelete = true, CancellationToken cancellationToken = default)
    {
        var entity = await GetByIdAsync(id, cancellationToken);
        if (entity == null) return;

        if (softDelete)
        {
            entity.IsDeleted = true;
            entity.DeletedOn = DateTime.UtcNow;
            context.UserProfiles.Update(entity);
        }
        else
        {
            context.UserProfiles.Remove(entity);
        }
    }
}
