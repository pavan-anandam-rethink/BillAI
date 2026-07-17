using BillAI.RulesEngine.Shared.Abstractions;

namespace BillAI.RulesEngine.Domain.Common;

/// <summary>
/// Base class for all domain entities with GUID identity, audit fields, and multi-tenant support.
/// </summary>
public abstract class AuditableEntity : IEntity<Guid>, IAuditable, IMultiTenant
{
    /// <inheritdoc/>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <inheritdoc/>
    public string TenantId { get; protected set; } = string.Empty;

    /// <inheritdoc/>
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    /// <inheritdoc/>
    public string? CreatedBy { get; protected set; }

    /// <inheritdoc/>
    public DateTimeOffset? UpdatedAt { get; protected set; }

    /// <inheritdoc/>
    public string? UpdatedBy { get; protected set; }

    /// <summary>Marks the entity as modified by the specified user.</summary>
    public void Touch(string updatedBy)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}
