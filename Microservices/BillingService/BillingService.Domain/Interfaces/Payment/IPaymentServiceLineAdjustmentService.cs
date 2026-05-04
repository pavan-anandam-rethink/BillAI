using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentClaimServiceLine;
using BillingService.Domain.Models.PaymentClaimServiceLineAdjustment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Payment
{
    public interface IPaymentServiceLineAdjustmentService
    {
        Task<List<PaymentClaimServiceLineAdjustmentModel>> GetPaymentServiceLineAdjustments(int serviceLineId);

        Task<List<PaymentClaimServiceLineAdjustmentModel>> GetPaymentServiceLineAdjustmentsByCharge(GetChargeDetailsModel model);
        Task<List<PaymentClaimServiceLineAdjustmentModel>> AddPaymentServiceLineAdjustmentsAsync(AddOrEditAdjustmentModel model);
        Task DeleteServiceLineAdjustmentsAsync(IdsWithUserInfo model);

        Task<List<PaymentClaimServiceLineAdjustmentModel>> UpdateServiceLineAdjustmentsAsync(AddOrEditAdjustmentModel model);
        Task<List<AdjustmentReasonCodes>> GetAdjustmentReasonDescriptionsAsync(string codes);
        Task ReapplyPRAdjustmentsAfterSecondaryBillingAsync(int claimId);
        Task DeleteServiceLineAdjustmentsAsync(AddOrEditAdjustmentModelForBulkPosting model);
    }
}