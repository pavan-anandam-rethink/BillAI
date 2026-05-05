using BillingService.Domain.Models.BulkPaymentPosting;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Models.PaymentClaimServiceLine;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BillingService.Domain.Interfaces.Payment
{
    public interface IBulkPaymentPostingService
    {
        Task<List<BulkPaymentResponseModel>> GetAllPayments(BulkPaymentPostingRequestModel paymentPostingRequestModel);
    }
}