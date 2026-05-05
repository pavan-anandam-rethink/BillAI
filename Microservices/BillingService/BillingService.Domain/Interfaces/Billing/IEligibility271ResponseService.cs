using Rethink.Services.Common.Models.Claim;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IEligibility271ResponseService
    {
        Task<EligibilityResponse> GetEligibilityResponse(EligibilityRequest request);
    }
}