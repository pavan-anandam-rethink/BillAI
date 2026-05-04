using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Domain.Interfaces;
using Rethink.Services.Domain.Services;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Payment
{
    public class PaymentServiceTest
    {
        private readonly Mock<ILoggerFactory> _loggerFactory;
        private readonly Mock<IRepository<BillingDbContext, PaymentEntity>> _paymentRepository;
        private readonly IPaymentService _paymentService;

        public PaymentServiceTest()
        {
            _loggerFactory = new Mock<ILoggerFactory>();
            _paymentRepository = new Mock<IRepository<BillingDbContext, PaymentEntity>>();
            _paymentService = new PaymentService(_loggerFactory.Object, _paymentRepository.Object);
        }

        #region CreatePayment Tests

        [Fact]
        public async Task CreatePayment_ShouldReturnPaymentEntity_WithCorrectProperties()
        {
            // Arrange
            var accountInfoId = 1;
            var memberId = 100;
            var paymentEraUploadId = 50;
            var paymentType = PaymentTypes.InsurancePayment;
            var xml = "<test>xml</test>";

            // Act
            var result = await _paymentService.CreatePayment(accountInfoId, memberId, paymentEraUploadId, paymentType, xml);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(accountInfoId, result.AccountInfoId);
            Assert.Equal(paymentEraUploadId, result.PaymentEraUploadId);
            Assert.Equal((int)paymentType, result.PaymentTypeId);
            Assert.Equal(PaymentStatus.SubmittedForParsing, result.Status);
            Assert.Equal(xml, result.TransactionXml);
        }

        [Fact]
        public async Task CreatePayment_WithNullXml_ShouldReturnPaymentEntityWithNullXml()
        {
            // Arrange
            var accountInfoId = 1;
            var memberId = 100;
            var paymentEraUploadId = 50;
            var paymentType = PaymentTypes.ERAReceived;

            // Act
            var result = await _paymentService.CreatePayment(accountInfoId, memberId, paymentEraUploadId, paymentType);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.TransactionXml);
        }

        [Fact]
        public async Task CreatePayment_WithNullPaymentEraUploadId_ShouldReturnPaymentEntityWithNullUploadId()
        {
            // Arrange
            var accountInfoId = 1;
            var memberId = 100;
            var paymentType = PaymentTypes.ClientPayment;

            // Act
            var result = await _paymentService.CreatePayment(accountInfoId, memberId, null, paymentType);

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.PaymentEraUploadId);
        }

        [Theory]
        [InlineData(PaymentTypes.InsurancePayment)]
        [InlineData(PaymentTypes.ERAReceived)]
        [InlineData(PaymentTypes.ClientPayment)]
        [InlineData(PaymentTypes.OtherPayment)]
        public async Task CreatePayment_WithDifferentPaymentTypes_ShouldSetCorrectPaymentTypeId(PaymentTypes paymentType)
        {
            // Arrange
            var accountInfoId = 1;
            var memberId = 100;

            // Act
            var result = await _paymentService.CreatePayment(accountInfoId, memberId, null, paymentType);

            // Assert
            Assert.Equal((int)paymentType, result.PaymentTypeId);
        }

        #endregion

        #region CreatePaymentError Tests

        [Fact]
        public void CreatePaymentError_ShouldReturnPaymentErrorEntity_WithCorrectProperties()
        {
            // Arrange
            var payment = new PaymentEntity { Id = 1, CreatedBy = 100 };
            var errorMsg = "Test error message";
            var severity = PaymentErrorSeverity.Error;
            var errorType = EraErrorType.Parsing;

            // Act
            var result = _paymentService.CreatePaymentError(payment, errorMsg, severity, errorType);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(payment, result.Payment);
            Assert.Equal(payment.Id, result.PaymentId);
            Assert.Equal(errorMsg, result.ErrorMessage);
            Assert.Equal(severity, result.Severity);
            Assert.Equal((int)errorType, result.ErrorType);
        }

        [Fact]
        public void CreatePaymentError_WithPaymentStatus_ShouldUpdatePaymentStatus()
        {
            // Arrange
            var payment = new PaymentEntity { Id = 1, CreatedBy = 100, Status = PaymentStatus.SubmittedForParsing };
            var errorMsg = "Test error";
            var severity = PaymentErrorSeverity.Warning;
            var errorType = EraErrorType.Claim;
            var newStatus = PaymentStatus.ParsingError;

            // Act
            var result = _paymentService.CreatePaymentError(payment, errorMsg, severity, errorType, newStatus);

            // Assert
            Assert.Equal(newStatus, payment.Status);
        }

        [Fact]
        public void CreatePaymentError_WithNullPaymentStatus_ShouldNotChangePaymentStatus()
        {
            // Arrange
            var originalStatus = PaymentStatus.SubmittedForParsing;
            var payment = new PaymentEntity { Id = 1, CreatedBy = 100, Status = originalStatus };
            var errorMsg = "Test error";
            var severity = PaymentErrorSeverity.Info;
            var errorType = EraErrorType.Parsing;

            // Act
            _paymentService.CreatePaymentError(payment, errorMsg, severity, errorType, null);

            // Assert
            Assert.Equal(originalStatus, payment.Status);
        }

        #endregion

        #region CreatePaymentClaimError Tests

        [Fact]
        public void CreatePaymentClaimError_ShouldReturnPaymentClaimErrorEntity_WithCorrectProperties()
        {
            // Arrange
            var payment = new PaymentEntity { Id = 1, Status = PaymentStatus.SubmittedForParsing };
            var paymentClaim = new PaymentClaimEntity { Id = 10, CreatedBy = 100, Payment = payment };
            var errorMsg = "Claim error message";
            var severity = PaymentErrorSeverity.Error;
            var errorType = EraErrorType.Claim;

            // Act
            var result = _paymentService.CreatePaymentClaimError(paymentClaim, errorMsg, severity, errorType);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(paymentClaim.Id, result.PaymentClaimId);
            Assert.Equal(paymentClaim, result.PaymentClaim);
            Assert.Equal(errorMsg, result.ErrorMessage);
            Assert.Equal(severity, result.Severity);
            Assert.Equal((int)errorType, result.ErrorType);
        }

        [Fact]
        public void CreatePaymentClaimError_WithPaymentStatus_ShouldUpdatePaymentStatus()
        {
            // Arrange
            var payment = new PaymentEntity { Id = 1, Status = PaymentStatus.SubmittedForParsing };
            var paymentClaim = new PaymentClaimEntity { Id = 10, CreatedBy = 100, Payment = payment };
            var errorMsg = "Error";
            var severity = PaymentErrorSeverity.Warning;
            var errorType = EraErrorType.Claim;
            var newStatus = PaymentStatus.ParsingError;

            // Act
            _paymentService.CreatePaymentClaimError(paymentClaim, errorMsg, severity, errorType, newStatus);

            // Assert
            Assert.Equal(newStatus, paymentClaim.Payment.Status);
        }

        #endregion

        #region CreateClaimServiceLineError Tests

        [Fact]
        public void CreateClaimServiceLineError_ShouldReturnServiceLineErrorEntity_WithCorrectProperties()
        {
            // Arrange
            var memberId = 100;
            var payment = new PaymentEntity { Id = 1, Status = PaymentStatus.SubmittedForParsing };
            var paymentClaim = new PaymentClaimEntity { Id = 10, Payment = payment };
            var serviceLine = new PaymentClaimServiceLineEntity { Id = 20, PaymentClaim = paymentClaim };
            var errorMsg = "Service line error";
            var severity = PaymentErrorSeverity.Error;
            var errorType = EraErrorType.ChargeEntry;

            // Act
            var result = _paymentService.CreateClaimServiceLineError(memberId, serviceLine, errorMsg, severity, errorType);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(serviceLine.Id, result.PaymentClaimServiceLineId);
            Assert.Equal(serviceLine, result.PaymentClaimServiceLine);
            Assert.Equal(errorMsg, result.ErrorMessage);
            Assert.Equal(severity, result.Severity);
            Assert.Equal((int)errorType, result.ErrorType);
        }

        [Fact]
        public void CreateClaimServiceLineError_WithPaymentStatus_ShouldUpdatePaymentStatus()
        {
            // Arrange
            var memberId = 100;
            var payment = new PaymentEntity { Id = 1, Status = PaymentStatus.SubmittedForParsing };
            var paymentClaim = new PaymentClaimEntity { Id = 10, Payment = payment };
            var serviceLine = new PaymentClaimServiceLineEntity { Id = 20, PaymentClaim = paymentClaim };
            var errorMsg = "Error";
            var severity = PaymentErrorSeverity.Warning;
            var errorType = EraErrorType.Claim;
            var newStatus = PaymentStatus.ParsingError;

            // Act
            _paymentService.CreateClaimServiceLineError(memberId, serviceLine, errorMsg, severity, errorType, newStatus);

            // Assert
            Assert.Equal(newStatus, serviceLine.PaymentClaim.Payment.Status);
        }

        [Fact]
        public void CreateClaimServiceLineError_WithNullPaymentStatus_ShouldNotChangePaymentStatus()
        {
            // Arrange
            var memberId = 100;
            var originalStatus = PaymentStatus.SubmittedForParsing;
            var payment = new PaymentEntity { Id = 1, Status = originalStatus };
            var paymentClaim = new PaymentClaimEntity { Id = 10, Payment = payment };
            var serviceLine = new PaymentClaimServiceLineEntity { Id = 20, PaymentClaim = paymentClaim };
            var errorMsg = "Error";
            var severity = PaymentErrorSeverity.Info;
            var errorType = EraErrorType.Parsing;

            // Act
            _paymentService.CreateClaimServiceLineError(memberId, serviceLine, errorMsg, severity, errorType, null);

            // Assert
            Assert.Equal(originalStatus, serviceLine.PaymentClaim.Payment.Status);
        }

        #endregion
    }
}
