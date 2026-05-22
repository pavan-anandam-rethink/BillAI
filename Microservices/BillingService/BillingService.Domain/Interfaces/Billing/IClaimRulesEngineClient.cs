using BillingService.Domain.Models.RulesEngine;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IClaimRulesEngineClient
    {
        Task<IReadOnlyList<ClaimRulesEngineViolation>> EvaluateClaimAsync(
            ClaimRulesEngineRequest request,
            CancellationToken cancellationToken = default);
    }
}
