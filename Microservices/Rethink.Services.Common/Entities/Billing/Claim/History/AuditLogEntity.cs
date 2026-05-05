using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Enums.Billing.History;
using System;

namespace Rethink.Services.Common.Entities.Billing.Claim.History;

public class AuditLogEntity: BasePersistEntity
{
    public int EntityId { get; set; }
    public string EntityName { get; set; }

    public ActionType ActionType { get; set; }

    public string OldValue { get; set; }

    public string NewValue { get; set; }

    public int AccountInfoId { get; set; }

    public int ChangedBy { get; set; }

    public DateTime ChangedOn { get; set; }
}