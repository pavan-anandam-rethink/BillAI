using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IFeatureFlagService
    {
        Task<bool> IsProviderEnrollmentValidationEnabledAsync();
        Task<bool> IsAiClaimRulesEngineEnabledAsync();
    }
}
