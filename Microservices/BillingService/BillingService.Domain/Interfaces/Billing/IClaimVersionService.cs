using BillingService.Domain.Models;
using Rethink.Services.Common.Entities.Billing.Claim;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IClaimVersionService
    {
        Task<int> CreateAsync(ClaimDetailsModel claim, int accountInfoId, int memberId);
        Task<ClaimVersionEntity> GetByIdAsync(int id);
    }
}
