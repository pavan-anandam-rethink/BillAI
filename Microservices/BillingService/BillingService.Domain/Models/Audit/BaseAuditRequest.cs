using Rethink.Services.Common.Enums.Billing.History;
using System;

namespace BillingService.Domain.Models.Audit;

public abstract class BaseAuditRequest<T>
{
    public int ChangedBy { get; set; }
    public T Data { get; set; }
}