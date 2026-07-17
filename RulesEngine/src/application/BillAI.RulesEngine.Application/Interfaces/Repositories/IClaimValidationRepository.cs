using BillAI.RulesEngine.Domain.Entities.Claims;
using BillAI.RulesEngine.Shared.Models;

namespace BillAI.RulesEngine.Application.Interfaces.Repositories;

/// <summary>
/// Persistence contract for <see cref="ClaimValidationRecord"/> aggregates.
/// </summary>
public interface IClaimValidationRepository
{
    Task<ClaimValidationRecord?> GetByIdAsync(Guid id, string tenantId, CancellationToken ct = default);
    Task<ClaimValidationRecord?> GetByAuditIdAsync(Guid auditId, string tenantId, CancellationToken ct = default);
    Task<PagedResult<ClaimValidationRecord>> GetAllAsync(
        string tenantId,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task AddAsync(ClaimValidationRecord record, CancellationToken ct = default);
}
