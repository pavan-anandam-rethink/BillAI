using IdentityService.Domain.Entities;
using System.Linq.Expressions;

namespace IdentityService.Domain.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<UserProfile?> FindOneAsync(Expression<Func<UserProfile, bool>> predicate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserProfile>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(UserProfile entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserProfile entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, bool softDelete = true, CancellationToken cancellationToken = default);
}
