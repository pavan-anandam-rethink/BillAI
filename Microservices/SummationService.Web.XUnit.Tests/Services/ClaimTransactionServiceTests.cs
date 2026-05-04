using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using SummationService.Domain.Interfaces;
using SummationService.Domain.Services;

namespace SummationService.Web.XUnit.Tests.Services
{
    public class ClaimTransactionServiceTests
    {
        private readonly Mock<IRepository<BillingDbContext, ClaimTransactionEntity>> _claimTransactionRepositoryMock;
        private readonly Mock<IHelperService> _helperServiceMock;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _paymentClaimRepositoryMock;
        private readonly Mock<ILogger<ClaimTransactionService>> _loggerMock;
        private readonly ClaimTransactionService _service;
        private readonly CancellationToken _cancellationToken;

        public ClaimTransactionServiceTests()
        {
            _claimTransactionRepositoryMock = new Mock<IRepository<BillingDbContext, ClaimTransactionEntity>>();
            _helperServiceMock = new Mock<IHelperService>();
            _paymentClaimRepositoryMock = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
            _loggerMock = new Mock<ILogger<ClaimTransactionService>>();
            _cancellationToken = CancellationToken.None;

            _service = new ClaimTransactionService(
                _claimTransactionRepositoryMock.Object,
                _helperServiceMock.Object,
                _paymentClaimRepositoryMock.Object,
                _loggerMock.Object
            );
        }

        #region AddOrUpdateClaimTransactionAsync Tests


        [Fact]
        public async Task AddOrUpdateClaimTransactionAsync_WhenClaimIdNotFound_ShouldReturnFalse()
        {
            // Arrange
            var transactionType = ClaimTransactionType.billedAmount;
            var transactionTypeId = 123;

            _helperServiceMock
                .Setup(x => x.GetClaimIdFromChargeEntryIdAsync(transactionTypeId, _cancellationToken))
                .ReturnsAsync((int?)null);

            // Act
            var result = await _service.AddOrUpdateClaimTransactionAsync(transactionType, transactionTypeId, _cancellationToken);

            // Assert
            Assert.False(result);
            _claimTransactionRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ClaimTransactionEntity>()), Times.Never);
            _claimTransactionRepositoryMock.Verify(x => x.Update(It.IsAny<ClaimTransactionEntity>()), Times.Never);
            _claimTransactionRepositoryMock.Verify(x => x.CommitAsync(), Times.Never);
        }

        [Fact]
        public async Task AddOrUpdateClaimTransactionAsync_WhenClaimIdIsZero_ShouldReturnFalse()
        {
            // Arrange
            var transactionType = ClaimTransactionType.writeOff;
            var transactionTypeId = 123;

            _helperServiceMock
                .Setup(x => x.GetClaimIdFromWriteOffIdAsync(transactionTypeId, _cancellationToken))
                .ReturnsAsync(0);

            // Act
            var result = await _service.AddOrUpdateClaimTransactionAsync(transactionType, transactionTypeId, _cancellationToken);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region FindClaimIdByTransactionTypeIdAsync Tests

        [Theory]
        [InlineData(ClaimTransactionType.billedAmount)]
        [InlineData(ClaimTransactionType.deleteCharge)]
        public async Task FindClaimIdByTransactionTypeIdAsync_ForChargeTypes_ShouldCallGetClaimIdFromChargeEntryIdAsync(ClaimTransactionType transactionType)
        {
            // Arrange
            var transactionTypeId = 123;
            var expectedClaimId = 456;

            _helperServiceMock
                .Setup(x => x.GetClaimIdFromChargeEntryIdAsync(transactionTypeId, _cancellationToken))
                .ReturnsAsync(expectedClaimId);

            // Act
            var result = await _service.FindClaimIdByTransactionTypeIdAsync(transactionType, transactionTypeId, _cancellationToken);

            // Assert
            Assert.Equal(expectedClaimId, result);
            _helperServiceMock.Verify(x => x.GetClaimIdFromChargeEntryIdAsync(transactionTypeId, _cancellationToken), Times.Once);
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_ForWriteOffType_ShouldCallGetClaimIdFromWriteOffIdAsync()
        {
            // Arrange
            var transactionTypeId = 123;
            var expectedClaimId = 456;

            _helperServiceMock
                .Setup(x => x.GetClaimIdFromWriteOffIdAsync(transactionTypeId, _cancellationToken))
                .ReturnsAsync(expectedClaimId);

            // Act
            var result = await _service.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.writeOff, transactionTypeId, _cancellationToken);

            // Assert
            Assert.Equal(expectedClaimId, result);
            _helperServiceMock.Verify(x => x.GetClaimIdFromWriteOffIdAsync(transactionTypeId, _cancellationToken), Times.Once);
        }

        [Theory]
        [InlineData(ClaimTransactionType.insurancePayment)]
        [InlineData(ClaimTransactionType.eraReceived)]
        [InlineData(ClaimTransactionType.patientPayment)]
        [InlineData(ClaimTransactionType.otherPayment)]
        [InlineData(ClaimTransactionType.deleteChargePayment)]
        public async Task FindClaimIdByTransactionTypeIdAsync_ForPaymentTypes_ShouldCallGetClaimIdFromPaymentIdAsync(ClaimTransactionType transactionType)
        {
            // Arrange
            var transactionTypeId = 123;
            var expectedClaimId = 456;

            _helperServiceMock
                .Setup(x => x.GetClaimIdFromPaymentIdAsync(transactionTypeId, _cancellationToken))
                .ReturnsAsync(expectedClaimId);

            // Act
            var result = await _service.FindClaimIdByTransactionTypeIdAsync(transactionType, transactionTypeId, _cancellationToken);

            // Assert
            Assert.Equal(expectedClaimId, result);
            _helperServiceMock.Verify(x => x.GetClaimIdFromPaymentIdAsync(transactionTypeId, _cancellationToken), Times.Once);
        }

        [Theory]
        [InlineData(ClaimTransactionType.adjustment)]
        [InlineData(ClaimTransactionType.patientResponsibility)]
        public async Task FindClaimIdByTransactionTypeIdAsync_ForAdjustmentTypes_ShouldCallGetClaimIdFromAdjustmentIdAsync(ClaimTransactionType transactionType)
        {
            // Arrange
            var transactionTypeId = 123;
            var expectedClaimId = 456;

            _helperServiceMock
                .Setup(x => x.GetClaimIdFromAdjustmentIdAsync(transactionTypeId, _cancellationToken))
                .ReturnsAsync(expectedClaimId);

            // Act
            var result = await _service.FindClaimIdByTransactionTypeIdAsync(transactionType, transactionTypeId, _cancellationToken);

            // Assert
            Assert.Equal(expectedClaimId, result);
            _helperServiceMock.Verify(x => x.GetClaimIdFromAdjustmentIdAsync(transactionTypeId, _cancellationToken), Times.Once);
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_ForDeleteClaimType_ShouldReturnTransactionTypeId()
        {
            // Arrange
            var transactionTypeId = 123;

            // Act
            var result = await _service.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.deleteClaim, transactionTypeId, _cancellationToken);

            // Assert
            Assert.Equal(transactionTypeId, result);
        }

        [Theory]
        [InlineData(ClaimTransactionType.submitClaim)]
        [InlineData(ClaimTransactionType.newDay)]
        [InlineData(ClaimTransactionType.updatePaymentSummary)]
        public async Task FindClaimIdByTransactionTypeIdAsync_ForNonApplicableTypes_ShouldReturnZero(ClaimTransactionType transactionType)
        {
            // Arrange
            var transactionTypeId = 123;

            // Act
            var result = await _service.FindClaimIdByTransactionTypeIdAsync(transactionType, transactionTypeId, _cancellationToken);

            // Assert
            Assert.Equal(0, result);
        }

        #endregion



        #region AddClaimTransactionAsync Tests

        [Fact]
        public async Task AddClaimTransactionAsync_ShouldCallRepositoryAdd()
        {
            // Arrange
            var claimTransaction = new ClaimTransactionEntity
            {
                ClaimId = 123,
                BilledAmount = 1000m,
                DateCreated = DateTime.Now
            };

            // Act
            await _service.AddClaimTransactionAsync(claimTransaction, _cancellationToken);

            // Assert
            _claimTransactionRepositoryMock.Verify(x => x.AddAsync(claimTransaction), Times.Once);
        }

        #endregion

        #region UpdateClaimTransaction Tests

        [Fact]
        public void UpdateClaimTransaction_ShouldCallRepositoryUpdate()
        {
            // Arrange
            var claimTransaction = new ClaimTransactionEntity
            {
                Id = 456,
                ClaimId = 123,
                BilledAmount = 1000m,
                InsurancePayment = 500m,
                DateModified = DateTime.Now
            };

            // Act
            _service.UpdateClaimTransaction(claimTransaction, _cancellationToken);

            // Assert
            _claimTransactionRepositoryMock.Verify(x => x.Update(claimTransaction), Times.Once);
        }

        #endregion

    }
}
