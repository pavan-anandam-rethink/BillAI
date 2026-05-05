using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentEntity> CreatePayment(int accountInfoId,
                                          int memberId,
                                          int? paymentEraUploadId,
                                          PaymentTypes paymentType,
                                          string xml = null,
                                          bool commitImmediately = false);

        PaymentErrorEntity CreatePaymentError(PaymentEntity payment,
                                              string errorMsg,
                                              PaymentErrorSeverity severity,
                                              EraErrorType errorType,
                                              PaymentStatus? paymentStatus = null);

        PaymentClaimErrorEntity CreatePaymentClaimError(PaymentClaimEntity paymentClaim,
                                                        string errorMsg,
                                                        PaymentErrorSeverity severity,
                                                        EraErrorType errorType,
                                                        PaymentStatus? paymentStatus = null);

        PaymentClaimServiceLineErrorEntity CreateClaimServiceLineError(int memberId,
                                                                       PaymentClaimServiceLineEntity serviceLine,
                                                                       string errorMsg,
                                                                       PaymentErrorSeverity severity,
                                                                       EraErrorType errorType,
                                                                       PaymentStatus? paymentStatus = null);
    }
}
