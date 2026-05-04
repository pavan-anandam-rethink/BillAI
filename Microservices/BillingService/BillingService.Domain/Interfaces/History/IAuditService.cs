using Rethink.Services.Common.Enums.Billing.History;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.History;

public interface IAuditService
{
    Task TrackAsync<T>(ActionType action, 
                       int changedBy,
                       int AccountInfoId,
                       int FunderId,
                       string EntityName,
                       T? oldEntity = default,
                       T? newEntity = default,
                       List<string>? ignoreFields = null);
}
