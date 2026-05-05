using Rethink.Services.Common.Entities.Billing.Claim;
using System.Threading;
using System.Threading.Tasks;

namespace Rethink.Services.Common.Interfaces
{
    public interface IEligibility271Repository
    {
        Task SaveAsync(Eligibility271ResponseEntity entity, CancellationToken ct);
    }
}
