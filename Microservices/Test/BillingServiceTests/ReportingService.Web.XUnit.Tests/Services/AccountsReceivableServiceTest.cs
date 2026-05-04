using k8s.Models;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Reporting;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using SummationService.Domain.Interfaces;
using SummationService.Domain.Services;
using System.Linq.Expressions;
using Rethink.Services.Common.Models.ReportingModels;
using BillingService.XUnit.Tests.Common.Mocks;

namespace ReportingService.Web.XUnit.Tests.Services
{
    public class AccountsReceivableServiceTest
    {
        private readonly Mock<IRepository<ReportingDbContext, AccountsReceivableEntity>> _mockAccountsReceivableRepo;
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _mockClaimRepo;
        private readonly Mock<IHelperService> _mockHelperService;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimEntity>> _mockPaymentClaimRepo;
        private readonly Mock<IRepository<ReportingDbContext, FundersEntity>> _mockFunderRepo;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> _mockPaymentClaimServiceLineRepo;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>> _mockPaymentClaimServiceLineAdjustmentRepo;
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _mockClaimChargeEntryRepo;
        private readonly Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>> _mockClaimAppointmentLinkRepo;
        private readonly Mock<IRepository<BillingDbContext, ClaimVersionEntity>> _mockClaimVersionRepo;
        private readonly Mock<IRepository<BillingDbContext, ClaimSearchRenderingProviderEntity>> _mockRenderingProviderRepo;
        private readonly Mock<IRepository<ReportingDbContext, ClientsEntity>> _mockClientRepo;
        private readonly Mock<IRepository<BillingDbContext, PatientInvoiceDetailsEntity>> _mockPatientInvoiceRepo;

        private readonly AccountsReceivableService _service;
        private readonly Mock<IAccountsReceivableService> _mockService;

        public AccountsReceivableServiceTest()
        {
            _mockAccountsReceivableRepo = new Mock<IRepository<ReportingDbContext, AccountsReceivableEntity>>();
            _mockClaimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
            _mockHelperService = new Mock<IHelperService>();
            _mockPaymentClaimRepo = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
            _mockFunderRepo = new Mock<IRepository<ReportingDbContext, FundersEntity>>();
            _mockPaymentClaimServiceLineRepo = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>>();
            _mockPaymentClaimServiceLineAdjustmentRepo = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>>();
            _mockClaimChargeEntryRepo = new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>();
            _mockClaimAppointmentLinkRepo = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>();
            _mockClaimVersionRepo = new Mock<IRepository<BillingDbContext, ClaimVersionEntity>>();
            _mockRenderingProviderRepo = new Mock<IRepository<BillingDbContext, ClaimSearchRenderingProviderEntity>>();
            _mockClientRepo = new Mock<IRepository<ReportingDbContext, ClientsEntity>>();
            _mockPatientInvoiceRepo = new Mock<IRepository<BillingDbContext, PatientInvoiceDetailsEntity>>();

            _service = new AccountsReceivableService(
                _mockAccountsReceivableRepo.Object,
                _mockClaimRepo.Object,
                _mockHelperService.Object,
                _mockPaymentClaimRepo.Object,
                _mockFunderRepo.Object,
                _mockPaymentClaimServiceLineRepo.Object,
                _mockPaymentClaimServiceLineAdjustmentRepo.Object,
                _mockClaimChargeEntryRepo.Object,
                _mockClaimAppointmentLinkRepo.Object,
                _mockClaimVersionRepo.Object,
                _mockRenderingProviderRepo.Object,
                _mockAccountsReceivableRepo.Object, // same as accountsReceivableRepository
                _mockClientRepo.Object,
                _mockPatientInvoiceRepo.Object
            );
            _mockService = new Mock<IAccountsReceivableService>();
        }

        #region AddOrUpdateAccountsReceivableAsync Tests

        [Fact]
        public async Task AddOrUpdateAccountsReceivableAsync_ReturnsFalse_WhenClaimIdNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.FindClaimIdByTransactionTypeIdAsync(It.IsAny<ClaimTransactionType>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync((int?)null);

            // Act
            var result = await _mockService.Object.AddOrUpdateAccountsReceivableAsync(ClaimTransactionType.patientPayment, 1, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddOrUpdateAccountsReceivableAsync_ReturnsFalse_WhenClaimIdIsZero()
        {
            // Arrange
            _mockHelperService.Setup(h => h.GetClaimIdFromChargeEntryIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int?)null);

            // Act
            var result = await _service.AddOrUpdateAccountsReceivableAsync(ClaimTransactionType.billedAmount, 1, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddOrUpdateAccountsReceivableAsync_ReturnsFalse_WhenClaimNotFound()
        {
            // Arrange
            _mockHelperService.Setup(h => h.GetClaimIdFromChargeEntryIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _mockClaimRepo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<ClaimEntity>(new List<ClaimEntity>()));

            // Act
            var result = await _service.AddOrUpdateAccountsReceivableAsync(ClaimTransactionType.billedAmount, 1, CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddOrUpdateAccountsReceivableAsync_ReturnsTrue_WhenNewAccountsReceivableCreated()
        {
            // Arrange
            var claim = new ClaimEntity
            {
                Id = 1,
                billedDate = DateTime.Now,
                AccountInfoId = 1,
                PrimaryFunderId = 1,
                ChildProfileId = 1,
                ClaimStatus = ClaimStatus.Billed
            };

            _mockHelperService.Setup(h => h.GetClaimIdFromChargeEntryIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            _mockClaimRepo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<ClaimEntity>(new List<ClaimEntity> { claim }));
            _mockAccountsReceivableRepo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<AccountsReceivableEntity>(new List<AccountsReceivableEntity>()));
            _mockHelperService.Setup(h => h.GetChargeEntriesByClaimId(It.IsAny<int>()))
                .ReturnsAsync(new List<ClaimChargeEntryEntity>());
            _mockHelperService.Setup(h => h.GetBilledAmountByClaimIdAsync(It.IsAny<int>()))
                .ReturnsAsync(100);
            _mockHelperService.Setup(h => h.CalculateClaimPaymentSumAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(0);
            _mockHelperService.Setup(h => h.GetAdjustmentsFromClaimIdAsync(It.IsAny<int>(), It.IsAny<ClaimTransactionType>()))
                .ReturnsAsync(new List<Tuple<bool?, decimal?>>());
            _mockHelperService.Setup(h => h.CalculateClaimWriteOffSumAsync(It.IsAny<int>()))
                .ReturnsAsync(0);
            _mockAccountsReceivableRepo.Setup(r => r.AddAsync(It.IsAny<AccountsReceivableEntity>()))
                .Returns(Task.CompletedTask);
            _mockAccountsReceivableRepo.Setup(r => r.CommitAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.AddOrUpdateAccountsReceivableAsync(ClaimTransactionType.billedAmount, 1, CancellationToken.None);

            // Assert
            Assert.True(result);
            _mockAccountsReceivableRepo.Verify(r => r.AddAsync(It.IsAny<AccountsReceivableEntity>()), Times.Once);
            _mockAccountsReceivableRepo.Verify(r => r.CommitAsync(), Times.Once);
        }

        #endregion

        #region GetFundersAsync Tests

        [Fact]
        public async Task GetFundersAsync_ReturnsEmptyList_WhenNoFunders()
        {
            _mockFunderRepo.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<FundersEntity, bool>>>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new TestAsyncEnumerable<FundersEntity>(new List<FundersEntity>()));

            var result = await _service.GetFundersAsync(CancellationToken.None);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFundersAsync_ReturnsFundersList()
        {
            var funders = new List<FundersEntity>
            {
                new() { FunderId = 1, FunderName = "Funder A" },
                new() { FunderId = 2, FunderName = "Funder B" }
            };
            _mockFunderRepo.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<FundersEntity, bool>>>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new TestAsyncEnumerable<FundersEntity>(funders));

            var result = await _service.GetFundersAsync(CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Equal("Funder A", result[0].FunderName);
            Assert.Equal("Funder B", result[1].FunderName);
        }

        [Fact]
        public async Task GetFundersAsync_ReturnsMappedFundersWithCorrectIds()
        {
            var funders = new List<FundersEntity>
            {
                new() { FunderId = 10, FunderName = "Test Funder" }
            };
            _mockFunderRepo.Setup(x => x.GetAllAsync(It.IsAny<Expression<Func<FundersEntity, bool>>>(), It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new TestAsyncEnumerable<FundersEntity>(funders));

            var result = await _service.GetFundersAsync(CancellationToken.None);

            Assert.Single(result);
            Assert.Equal(10, result[0].FunderId);
            Assert.Equal("Test Funder", result[0].FunderName);
        }

        #endregion

        #region FindClaimIdByTransactionTypeIdAsync Tests

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_ReturnsClaim_ForBilledAmount()
        {
            // Arrange
            _mockHelperService.Setup(h => h.GetClaimIdFromChargeEntryIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(123);

            // Act
            var result = await _service.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.billedAmount, 1, CancellationToken.None);

            // Assert
            Assert.Equal(123, result);
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_ReturnsClaim_ForDeleteCharge()
        {
            // Arrange
            _mockHelperService.Setup(h => h.GetClaimIdFromChargeEntryIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(456);

            // Act
            var result = await _service.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.deleteCharge, 1, CancellationToken.None);

            // Assert
            Assert.Equal(456, result);
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_ReturnsClaim_ForWriteOff()
        {
            // Arrange
            _mockHelperService.Setup(h => h.GetClaimIdFromWriteOffIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(789);

            // Act
            var result = await _service.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.writeOff, 1, CancellationToken.None);

            // Assert
            Assert.Equal(789, result);
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_ReturnsClaim_ForInsurancePayment()
        {
            // Arrange
            _mockHelperService.Setup(h => h.GetClaimIdFromPaymentIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(100);

            // Act
            var result = await _service.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.insurancePayment, 1, CancellationToken.None);

            // Assert
            Assert.Equal(100, result);
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_ReturnsClaim_ForAdjustment()
        {
            // Arrange
            _mockHelperService.Setup(h => h.GetClaimIdFromAdjustmentIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(200);

            // Act
            var result = await _service.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.adjustment, 1, CancellationToken.None);

            // Assert
            Assert.Equal(200, result);
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_ReturnsClaim_ForPatientResponsibility()
        {
            // Arrange
            _mockHelperService.Setup(h => h.GetClaimIdFromAdjustmentIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(300);

            // Act
            var result = await _service.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.patientResponsibility, 1, CancellationToken.None);

            // Assert
            Assert.Equal(300, result);
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_ReturnsTransactionTypeId_ForSubmitClaim()
        {
            // Act
            var result = await _service.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.submitClaim, 999, CancellationToken.None);

            // Assert
            Assert.Equal(999, result);
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_ReturnsTransactionTypeId_ForDeleteClaim()
        {
            // Act
            var result = await _service.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.deleteClaim, 888, CancellationToken.None);

            // Assert
            Assert.Equal(888, result);
        }

        [Fact]
        public async Task FindClaimIdByTransactionTypeIdAsync_ReturnsZero_ForPatientPayment()
        {
            // Act
            var result = await _service.FindClaimIdByTransactionTypeIdAsync(ClaimTransactionType.patientPayment, 1, CancellationToken.None);

            // Assert
            Assert.Equal(0, result);
        }

        #endregion

        #region GetClaimByIdAsync Tests

        [Fact]
        public async Task GetClaimByIdAsync_ReturnsClaim_WhenClaimExists()
        {
            // Arrange
            var claim = new ClaimEntity { Id = 1, billedDate = DateTime.Now };
            _mockClaimRepo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<ClaimEntity>(new List<ClaimEntity> { claim }));

            // Act
            var result = await _service.GetClaimByIdAsync(1, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetClaimByIdAsync_ReturnsNull_WhenClaimDoesNotExist()
        {
            // Arrange
            _mockClaimRepo.Setup(r => r.Query()).Returns(new TestAsyncEnumerable<ClaimEntity>(new List<ClaimEntity>()));

            // Act
            var result = await _service.GetClaimByIdAsync(999, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetAccountsReceivableByIdAsync Tests

        [Fact]
        public async Task GetAccountsReceivableByIdAsync_ReturnsAccountsReceivable_WhenExists()
        {
            // Arrange
            var ar = new AccountsReceivableEntity 
            { 
                Id = 1, 
                ClaimId = 1, 
                DateDeleted = null 
            };
            _mockAccountsReceivableRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<AccountsReceivableEntity>(new List<AccountsReceivableEntity> { ar }));

            // Act
            var result = await _service.GetAccountsReceivableByIdAsync(1, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.ClaimId);
        }

        [Fact]
        public async Task GetAccountsReceivableByIdAsync_ReturnsNull_WhenNotExists()
        {
            // Arrange
            _mockAccountsReceivableRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<AccountsReceivableEntity>(new List<AccountsReceivableEntity>()));

            // Act
            var result = await _service.GetAccountsReceivableByIdAsync(999, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAccountsReceivableByIdAsync_ReturnsNull_WhenDeleted()
        {
            // Arrange
            var ar = new AccountsReceivableEntity 
            { 
                Id = 1, 
                ClaimId = 1, 
                DateDeleted = DateTime.Now 
            };
            _mockAccountsReceivableRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<AccountsReceivableEntity>(new List<AccountsReceivableEntity> { ar }));

            // Act
            var result = await _service.GetAccountsReceivableByIdAsync(1, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAccountsReceivableByIdAsync_ReturnsMostRecent_WhenMultipleExist()
        {
            // Arrange
            var ar1 = new AccountsReceivableEntity 
            { 
                Id = 1, 
                ClaimId = 1, 
                DateDeleted = null 
            };
            var ar2 = new AccountsReceivableEntity 
            { 
                Id = 2, 
                ClaimId = 1, 
                DateDeleted = null 
            };
            _mockAccountsReceivableRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<AccountsReceivableEntity>(new List<AccountsReceivableEntity> { ar1, ar2 }));

            // Act
            var result = await _service.GetAccountsReceivableByIdAsync(1, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Id);
        }

        #endregion

        #region PrepareAccountsReceivableAsync Tests

        [Fact]
        public async Task PrepareAccountsReceivableAsync_ReturnsNull_WhenClaimNotBilled()
        {
            // Arrange
            var claim = new ClaimEntity { Id = 1, billedDate = null };

            // Act
            var result = await _service.PrepareAccountsReceivableAsync(ClaimTransactionType.billedAmount, claim, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task PrepareAccountsReceivableAsync_ReturnsNull_WhenDeleteClaimAndDifferentDate()
        {
            // Arrange
            var claim = new ClaimEntity 
            { 
                Id = 1, 
                billedDate = DateTime.Now.AddDays(-5) 
            };
            var existingAr = new AccountsReceivableEntity 
            { 
                Id = 1, 
                ClaimId = 1, 
                DateCreated = DateTime.Now.AddDays(-2),
                DateDeleted = null 
            };
            _mockAccountsReceivableRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<AccountsReceivableEntity>(new List<AccountsReceivableEntity> { existingAr }));

            // Act
            var result = await _service.PrepareAccountsReceivableAsync(ClaimTransactionType.deleteClaim, claim, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task PrepareAccountsReceivableAsync_CreatesNewEntity_WhenNotExists()
        {
            // Arrange
            var claim = new ClaimEntity 
            { 
                Id = 1, 
                billedDate = DateTime.Now,
                AccountInfoId = 10,
                PrimaryFunderId = 20,
                ChildProfileId = 30,
                ClaimStatus = ClaimStatus.Billed
            };
            _mockAccountsReceivableRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<AccountsReceivableEntity>(new List<AccountsReceivableEntity>()));
            _mockHelperService.Setup(h => h.GetChargeEntriesByClaimId(It.IsAny<int>()))
                .ReturnsAsync(new List<ClaimChargeEntryEntity>());
            _mockHelperService.Setup(h => h.GetBilledAmountByClaimIdAsync(It.IsAny<int>()))
                .ReturnsAsync(100);
            _mockHelperService.Setup(h => h.CalculateClaimPaymentSumAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(0);
            _mockHelperService.Setup(h => h.GetAdjustmentsFromClaimIdAsync(It.IsAny<int>(), It.IsAny<ClaimTransactionType>()))
                .ReturnsAsync(new List<Tuple<bool?, decimal?>>());
            _mockHelperService.Setup(h => h.CalculateClaimWriteOffSumAsync(It.IsAny<int>()))
                .ReturnsAsync(0);

            // Act
            var result = await _service.PrepareAccountsReceivableAsync(ClaimTransactionType.billedAmount, claim, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Id);
            Assert.Equal(1, result.ClaimId);
            Assert.Equal(10, result.AccountInfoId);
            Assert.Equal(20, result.FunderId);
            Assert.Equal(30, result.ClientId);
        }

        #endregion

        #region AddAccountsReceivableAsync Tests

        [Fact]
        public async Task AddAccountsReceivableAsync_CallsRepositoryAdd()
        {
            // Arrange
            var ar = new AccountsReceivableEntity { ClaimId = 1 };
            _mockAccountsReceivableRepo.Setup(r => r.AddAsync(It.IsAny<AccountsReceivableEntity>()))
                .Returns(Task.CompletedTask);

            // Act
            await _service.AddAccountsReceivableAsync(ar, CancellationToken.None);

            // Assert
            _mockAccountsReceivableRepo.Verify(r => r.AddAsync(ar), Times.Once);
        }

        #endregion

        #region UpdateAccountsReceivable Tests

        [Fact]
        public void UpdateAccountsReceivable_CallsRepositoryUpdate()
        {
            // Arrange
            var ar = new AccountsReceivableEntity { Id = 1, ClaimId = 1 };

            // Act
            _service.UpdateAccountsReceivable(ar, CancellationToken.None);

            // Assert
            _mockAccountsReceivableRepo.Verify(r => r.Update(ar), Times.Once);
        }

        #endregion

        #region GetAccountsReceivablesAsync Tests

        [Fact]
        public async Task GetAccountsReceivablesAsync_ReturnsEmptyList_WhenNoData()
        {
            // Arrange
            var model = new AccountsRecievablesRequestModel
            {
                PayerOrFunder = new List<int> { 1 },
                closingDate = DateTime.Now,
                AccountInfoId = 1,
                Skip = 0,
                Take = 10
            };
            _mockHelperService.Setup(h => h.GetAccountsReceivableEntitiesByFunderIdAsync(
                It.IsAny<List<int>>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AccountsReceivableQueryModel>());

            // Act
            var result = await _service.GetAccountsReceivablesAsync(model, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.AccountsReceivables);
            Assert.Equal(0, result.totalCount);
        }

        [Fact]
        public async Task GetAccountsReceivablesAsync_AppliesDefaultSorting_WhenNoSortingProvided()
        {
            // Arrange
            var model = new AccountsRecievablesRequestModel
            {
                PayerOrFunder = new List<int> { 1 },
                closingDate = DateTime.Now,
                AccountInfoId = 1,
                Skip = 0,
                Take = 10
            };
            _mockHelperService.Setup(h => h.GetAccountsReceivableEntitiesByFunderIdAsync(
                It.IsAny<List<int>>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AccountsReceivableQueryModel>());

            // Act
            var result = await _service.GetAccountsReceivablesAsync(model, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(model.SortingModels);
            Assert.Single(model.SortingModels);
            Assert.Equal("BilledDate", model.SortingModels[0].Field);
            Assert.Equal("desc", model.SortingModels[0].Dir);
        }

        [Fact]
        public async Task GetAccountsReceivablesAsync_AppliesPagination_WhenTakeIsPositive()
        {
            // Arrange
            var model = new AccountsRecievablesRequestModel
            {
                PayerOrFunder = new List<int> { 1 },
                closingDate = DateTime.Now,
                AccountInfoId = 1,
                Skip = 0,
                Take = 2
            };
            
            var data = new List<AccountsReceivableQueryModel>
            {
                new() { ClaimId = 1, BilledDate = DateTime.Now.AddDays(-10), NetReceivable = 100 },
                new() { ClaimId = 2, BilledDate = DateTime.Now.AddDays(-20), NetReceivable = 200 },
                new() { ClaimId = 3, BilledDate = DateTime.Now.AddDays(-30), NetReceivable = 300 }
            };
            
            _mockHelperService.Setup(h => h.GetAccountsReceivableEntitiesByFunderIdAsync(
                It.IsAny<List<int>>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(data);

            // Act
            var result = await _service.GetAccountsReceivablesAsync(model, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.AccountsReceivables.Count);
            Assert.Equal(3, result.totalCount);
        }

        #endregion

        #region GetAccountsReceivablesChargeLevelAsync Tests

        [Fact]
        public async Task GetAccountsReceivablesChargeLevelAsync_ReturnsEmptyList_WhenNoData()
        {
            // Arrange
            var model = new AccountsRecievablesRequestModel
            {
                PayerOrFunder = new List<int> { 1 },
                closingDate = DateTime.Now,
                Skip = 0,
                Take = 10
            };
            
            _mockAccountsReceivableRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<AccountsReceivableEntity>(new List<AccountsReceivableEntity>()));
            _mockFunderRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<FundersEntity>(new List<FundersEntity>()));
            _mockClientRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<ClientsEntity>(new List<ClientsEntity>()));
            _mockClaimChargeEntryRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<ClaimChargeEntryEntity>(new List<ClaimChargeEntryEntity>()));
            _mockClaimRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<ClaimEntity>(new List<ClaimEntity>()));
            _mockClaimVersionRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<ClaimVersionEntity>(new List<ClaimVersionEntity>()));
            _mockPaymentClaimRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<PaymentClaimEntity>(new List<PaymentClaimEntity>()));
            _mockPaymentClaimServiceLineRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<PaymentClaimServiceLineEntity>(new List<PaymentClaimServiceLineEntity>()));
            _mockPaymentClaimServiceLineAdjustmentRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<PaymentClaimServiceLineAdjustmentEntity>(new List<PaymentClaimServiceLineAdjustmentEntity>()));
            _mockClaimAppointmentLinkRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<ClaimAppointmentLinkEntity>(new List<ClaimAppointmentLinkEntity>()));
            _mockPatientInvoiceRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<PatientInvoiceDetailsEntity>(new List<PatientInvoiceDetailsEntity>()));
            _mockRenderingProviderRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<ClaimSearchRenderingProviderEntity>(new List<ClaimSearchRenderingProviderEntity>()));

            // Act
            var result = await _service.GetAccountsReceivablesChargeLevelAsync(model, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.AccountsReceivables);
            Assert.Equal(0, result.totalCount);
        }

        [Fact]
        public async Task GetAccountsReceivablesChargeLevelAsync_AppliesDefaultSorting_WhenNoSortingProvided()
        {
            // Arrange
            var model = new AccountsRecievablesRequestModel
            {
                PayerOrFunder = new List<int> { 1 },
                closingDate = DateTime.Now,
                Skip = 0,
                Take = 10
            };
            
            _mockAccountsReceivableRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<AccountsReceivableEntity>(new List<AccountsReceivableEntity>()));
            _mockFunderRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<FundersEntity>(new List<FundersEntity>()));
            _mockClientRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<ClientsEntity>(new List<ClientsEntity>()));
            _mockClaimChargeEntryRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<ClaimChargeEntryEntity>(new List<ClaimChargeEntryEntity>()));
            _mockClaimRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<ClaimEntity>(new List<ClaimEntity>()));
            _mockClaimVersionRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<ClaimVersionEntity>(new List<ClaimVersionEntity>()));
            _mockPaymentClaimRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<PaymentClaimEntity>(new List<PaymentClaimEntity>()));
            _mockPaymentClaimServiceLineRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<PaymentClaimServiceLineEntity>(new List<PaymentClaimServiceLineEntity>()));
            _mockPaymentClaimServiceLineAdjustmentRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<PaymentClaimServiceLineAdjustmentEntity>(new List<PaymentClaimServiceLineAdjustmentEntity>()));
            _mockClaimAppointmentLinkRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<ClaimAppointmentLinkEntity>(new List<ClaimAppointmentLinkEntity>()));
            _mockPatientInvoiceRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<PatientInvoiceDetailsEntity>(new List<PatientInvoiceDetailsEntity>()));
            _mockRenderingProviderRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<ClaimSearchRenderingProviderEntity>(new List<ClaimSearchRenderingProviderEntity>()));

            // Act
            await _service.GetAccountsReceivablesChargeLevelAsync(model, CancellationToken.None);

            // Assert
            Assert.NotNull(model.SortingModels);
            Assert.Single(model.SortingModels);
            Assert.Equal("dateOfService", model.SortingModels[0].Field);
            Assert.Equal("desc", model.SortingModels[0].Dir);
        }

        #endregion

        #region ExportToExcelAsync Tests

        [Fact]
        public async Task ExportToExcelAsync_ReturnsExcelFile_WithValidData()
        {
            // Arrange
            var model = new AccountsRecievablesRequestModel
            {
                PayerOrFunder = new List<int> { 1 },
                closingDate = DateTime.Now
            };
            
            var response = new AccountsReceivablesResponseModel
            {
                AccountsReceivables = new List<AccountsReceivablesResponse>
                {
                    new() { FunderName = "Test Funder", ClientId = 1, BilledAmount = 100 }
                }
            };
            
            _mockFunderRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<FundersEntity>(new List<FundersEntity> 
                { 
                    new() { FunderId = 1, FunderName = "Test Funder", DateDeleted = null } 
                }));

            // Act
            var result = await _service.ExportToExcelAsync(model, response, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public async Task ExportToExcelAsync_ReturnsExcelFile_WithEmptyData()
        {
            // Arrange
            var model = new AccountsRecievablesRequestModel
            {
                PayerOrFunder = new List<int> { 1 },
                closingDate = DateTime.Now
            };
            
            var response = new AccountsReceivablesResponseModel
            {
                AccountsReceivables = new List<AccountsReceivablesResponse>()
            };
            
            _mockFunderRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<FundersEntity>(new List<FundersEntity>()));

            // Act
            var result = await _service.ExportToExcelAsync(model, response, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        #endregion

        #region ExportToExcelChargeLevelAsync Tests

        [Fact]
        public async Task ExportToExcelChargeLevelAsync_ReturnsExcelFile_WithValidData()
        {
            // Arrange
            var model = new AccountsRecievablesRequestModel
            {
                PayerOrFunder = new List<int> { 1 },
                closingDate = DateTime.Now
            };
            
            var response = new AccountsReceivablesChargeLevelResponseModel
            {
                AccountsReceivables = new List<AccountsReceivablesChargeLevelResponse>
                {
                    new() 
                    { 
                        FunderName = "Test Funder", 
                        ClientId = 1, 
                        BilledAmount = 100,
                        DateOfService = DateTime.Now 
                    }
                }
            };
            
            _mockFunderRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<FundersEntity>(new List<FundersEntity> 
                { 
                    new() { FunderId = 1, FunderName = "Test Funder", DateDeleted = null } 
                }));

            // Act
            var result = await _service.ExportToExcelChargeLevelAsync(model, response, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        [Fact]
        public async Task ExportToExcelChargeLevelAsync_ReturnsExcelFile_WithEmptyData()
        {
            // Arrange
            var model = new AccountsRecievablesRequestModel
            {
                PayerOrFunder = new List<int> { 1 },
                closingDate = DateTime.Now
            };
            
            var response = new AccountsReceivablesChargeLevelResponseModel
            {
                AccountsReceivables = new List<AccountsReceivablesChargeLevelResponse>()
            };
            
            _mockFunderRepo.Setup(r => r.Query())
                .Returns(new TestAsyncEnumerable<FundersEntity>(new List<FundersEntity>()));

            // Act
            var result = await _service.ExportToExcelChargeLevelAsync(model, response, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Length > 0);
        }

        #endregion
    }
}

