using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.Models;
using Rethink.Services.Common.Entities.Billing.Payment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Billing
{
    public interface IChargePaymentService
    {
        Task<List<ChargePaymentItem>> GetForClaim(int claimId, int accountInfoId);

        Task<ChargePaymentItem> Save(ChargePaymentItem item, int memberId);

        Task<ChargePaymentItem> Delete(ChargePaymentItem item, int memberId);

        Task<PaymentOptions> GetPaymentOptions(int claimId, int accountInfoId);

        Task<decimal> GetRemainingAmount(int chargeId, int accountInfoId);
        Task AddChargePaymentEntitesAsync(IEnumerable<ChargePaymentEntity> entites);
    }
}
