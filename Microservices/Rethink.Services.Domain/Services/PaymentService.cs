using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Rethink.Services.Domain.Services
{
    public class PaymentService : BaseService, IPaymentService
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly IRepository<BillingDbContext, PaymentEntity> _paymentRepository;

        public PaymentService(ILoggerFactory loggerFactory,
                              IRepository<BillingDbContext, PaymentEntity> paymentRepository)
        {
            _loggerFactory = loggerFactory;
            _paymentRepository = paymentRepository;
        }


        public async Task<PaymentEntity> CreatePayment(int accountInfoId,
                                                       int memberId,
                                                       int? paymentEraUploadId,
                                                       PaymentTypes paymentType,
                                                       string xml = null,
                                                       bool commitImmediately = false)
        {
            var payment = new PaymentEntity
            {
                AccountInfoId = accountInfoId,
                PaymentEraUploadId = paymentEraUploadId,
                PaymentTypeId = (int)paymentType,
                Status = PaymentStatus.SubmittedForParsing,
                TransactionXml = xml
            };
            MarkCreated(payment, memberId);
            return payment;
        }

        public PaymentErrorEntity CreatePaymentError(PaymentEntity payment,
                                                     string errorMsg,
                                                     PaymentErrorSeverity severity,
                                                     EraErrorType errorType,
                                                     PaymentStatus? paymentStatus = null)
        {
            var error = new PaymentErrorEntity()
            {
                Payment = payment,
                PaymentId = payment.Id,
                ErrorMessage = errorMsg,
                Severity = severity,
                ErrorType = (int)errorType

            };
            MarkCreated(error, payment.CreatedBy);
            if (paymentStatus.HasValue)
            {
                payment.Status = paymentStatus.Value;
            }
            return error;
        }

        public PaymentClaimErrorEntity CreatePaymentClaimError(PaymentClaimEntity paymentClaim,
                                                               string errorMsg,
                                                               PaymentErrorSeverity severity,
                                                               EraErrorType errorType,
                                                               PaymentStatus? paymentStatus = null)
        {
            var error = new PaymentClaimErrorEntity()
            {
                PaymentClaimId = paymentClaim.Id,
                PaymentClaim = paymentClaim,
                ErrorMessage = errorMsg,
                Severity = severity,
                ErrorType = (int)errorType

            };
            MarkCreated(error, paymentClaim.CreatedBy);
            if (paymentStatus.HasValue)
            {
                paymentClaim.Payment.Status = paymentStatus.Value;
            }
            return error;
        }

        public PaymentClaimServiceLineErrorEntity CreateClaimServiceLineError(int memberId,
                                                                              PaymentClaimServiceLineEntity serviceLine,
                                                                              string errorMsg,
                                                                              PaymentErrorSeverity severity,
                                                                              EraErrorType errorType,
                                                                              PaymentStatus? paymentStatus = null)
        {
            var error = new PaymentClaimServiceLineErrorEntity()
            {
                PaymentClaimServiceLineId = serviceLine.Id,
                PaymentClaimServiceLine = serviceLine,
                ErrorMessage = errorMsg,
                Severity = severity,
                ErrorType = (int)errorType

            };
            MarkCreated(error, memberId);
            if (paymentStatus.HasValue)
            {
                serviceLine.PaymentClaim.Payment.Status = paymentStatus.Value;
            }
            return error;
        }

    }
}
