using BillingService.Domain.Interfaces.History;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Enums.Billing.History;
using Rethink.Services.Common.Helpers;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.History;

public class AuditService: IAuditService
{
    private readonly IRepository<BillingDbContext, AuditLogEntity> _auditRepository;

    public AuditService(
        IRepository<BillingDbContext, AuditLogEntity> auditRepository
        )
    {
        _auditRepository = auditRepository;
    }

    public async Task TrackAsync<T>(
        ActionType action,
        int changedBy,
        int AccountInfoId,
        int FunderId,
        string EntityName,
        T? oldEntity = default,
        T? newEntity = default,
        List<string>? ignoreFields = null)
    {
        var entityName = typeof(T).Name;
        Dictionary<string, object>? oldValues = null;
        Dictionary<string, object>? newValues = null;

        switch (action)
        {
            case ActionType.I:
                newValues = newEntity.GetChanges(ignoreFields);
                break;

            case ActionType.U:
                var changes = oldEntity.GetChanges(newEntity!, ignoreFields);

                if (!changes.OldValues.Any())
                    return; // No changes → no audit

                oldValues = changes.OldValues;//oldValues;
                newValues = changes.NewValues;
                break;

            case ActionType.D:
                oldValues = oldEntity.GetChanges(ignoreFields);
                break;
        }

        var audit = new AuditLogEntity
        {
            EntityName = EntityName,
            ActionType = action,
            OldValue = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValue = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            ChangedBy = changedBy,
            ChangedOn = DateTime.UtcNow,
            AccountInfoId = AccountInfoId,
            EntityId = FunderId
        };

        await _auditRepository.AddAsync(audit);
        await _auditRepository.SaveChangesAsync();
    }
}