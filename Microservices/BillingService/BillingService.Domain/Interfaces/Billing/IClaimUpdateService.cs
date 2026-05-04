using BillingService.Domain.Models;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IClaimUpdateService
    {
        Task<ClaimUpdateResult> UpdateClaimSecondaryFunderOnRefresh(int accountInfoId, int memberId, int claimId);
        Task<ClaimNextFundersAndControlNumberModel> CheckAndGetSecondaryFunderDetails(int accountInfoId, ClaimEntity claim);
    }
}
