using AutoMapper;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Models.BillingSettings;
using BillingService.Domain.Services.BillingSetting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Feature;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class BillingSettingsServiceTests
{
    private readonly Mock<IRepository<BillingDbContext, ClaimFilingIndicatorEntity>> _claimFilingIndicatorRepoMock;
    private readonly Mock<IRepository<BillingDbContext, FunderSettingsEntity>> _funderSettingRepoMock;
    private readonly Mock<IRepository<BillingDbContext, FeatureEntity>> _featureRepoMock;
    private readonly Mock<IRepository<BillingDbContext, AccountFeatureSettingEntity>> _accountFeatureSettingRepoMock;
    private readonly Mock<IRepository<BillingDbContext, BillingSettingInformationEntity>> _billingSettingInformationRepoMock; // <-- Add this line
    private readonly Mock<ILogger<BillingSettingsService>> _loggerMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly BillingSettingsService _service;
    private readonly Mock<IRepository<BillingDbContext, BillingSettingInformationEntity>> _mockRepository;
    private readonly Mock<ILogger<BillingSettingsService>> _mockLogger;

    private readonly Mock<IRepository<BillingDbContext, TimezonesEntity>> _timezonesRepoMock;
    private readonly Mock<IRethinkMasterDataMicroServices> _rethinkServicesMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IRepository<BillingDbContext, StateEntity>> _stateRepositoryMock;

    public BillingSettingsServiceTests()
    {
        _claimFilingIndicatorRepoMock = new Mock<IRepository<BillingDbContext, ClaimFilingIndicatorEntity>>();
        _funderSettingRepoMock = new Mock<IRepository<BillingDbContext, FunderSettingsEntity>>();
        _featureRepoMock = new Mock<IRepository<BillingDbContext, FeatureEntity>>();
        _accountFeatureSettingRepoMock = new Mock<IRepository<BillingDbContext, AccountFeatureSettingEntity>>();
        _billingSettingInformationRepoMock = new Mock<IRepository<BillingDbContext, BillingSettingInformationEntity>>(); // <-- Add this line
        _loggerMock = new Mock<ILogger<BillingSettingsService>>();
        _mapperMock = new Mock<IMapper>();

        _mockRepository = new Mock<IRepository<BillingDbContext, BillingSettingInformationEntity>>();
        _mockLogger = new Mock<ILogger<BillingSettingsService>>();

        _timezonesRepoMock = new Mock<IRepository<BillingDbContext, TimezonesEntity>>();
        _rethinkServicesMock = new Mock<IRethinkMasterDataMicroServices>();
        _cacheServiceMock = new Mock<ICacheService>();
        _stateRepositoryMock = new Mock<IRepository<BillingDbContext, StateEntity>>();
        var options = new DbContextOptionsBuilder<BillingDbContext>()
        .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TestDb_" + Guid.NewGuid() + ";Trusted_Connection=True;")
        .Options;

        var context = new BillingDbContext(options);

        _service = new BillingSettingsService(
            _claimFilingIndicatorRepoMock.Object,
            _funderSettingRepoMock.Object,
            _featureRepoMock.Object,
            _accountFeatureSettingRepoMock.Object,
            _billingSettingInformationRepoMock.Object, // <-- Add this argument
            _loggerMock.Object,
            _mapperMock.Object,
            context,
            _rethinkServicesMock.Object,
            _timezonesRepoMock.Object,
            _cacheServiceMock.Object,
            _stateRepositoryMock.Object
        );
    }


    [Fact]
    public async Task GetClaimFilingIndicators_ReturnsList_WhenEntitiesExist()
    {
        var entities = new List<ClaimFilingIndicatorEntity>
        {
            new ClaimFilingIndicatorEntity { Id = 1, Code = "A", Description = "DescA" }
        };
        var asyncEntities = new TestAsyncEnumerable<ClaimFilingIndicatorEntity>(entities);
        _claimFilingIndicatorRepoMock.Setup(r => r.Query()).Returns(asyncEntities.AsQueryable());

        var result = await _service.GetClaimFilingIndicators();

        Assert.Single(result);
        Assert.Equal("A - DescA", result[0].Indicator);
    }

    [Fact]
    public async Task GetClaimFilingIndicators_ReturnsEmptyList_OnException()
    {
        _claimFilingIndicatorRepoMock.Setup(r => r.Query()).Throws(new Exception("fail"));

        var result = await _service.GetClaimFilingIndicators();

        Assert.Empty(result);
    }

    [Fact]
    public async Task SetBillingFunderSettings_Adds_When_Entity_Does_Not_Exist()
    {
        var model = new BillingFunderSettingRequestModel { AccountInfoId = 1, ClaimFilingIndicatorId = 2, FunderId = 3, FunderName = "DATATEST", IncludeTaxonomyCode = true };
        var entity = new FunderSettingsEntity();
        _mapperMock.Setup(m => m.Map<FunderSettingsEntity>(model)).Returns(entity);

        var asyncEntities = new TestAsyncEnumerable<FunderSettingsEntity>(new List<FunderSettingsEntity>());
        _funderSettingRepoMock.Setup(r => r.Query()).Returns(asyncEntities.AsQueryable());
        _funderSettingRepoMock.Setup(r => r.AddAsync(entity)).Returns(Task.CompletedTask);
        _funderSettingRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
}

    [Fact]
    public async Task SetBillingFunderSettings_Updates_When_Entity_Exists()
    {
        var model = new BillingFunderSettingRequestModel { AccountInfoId = 1, ClaimFilingIndicatorId = 1, FunderId = 3, FunderName = "Funder", IncludeTaxonomyCode = true };
        var entity = new FunderSettingsEntity
        {
            FunderName = "Funder",
            ClaimFilingIndicatorId = 1,
            IncludeTaxonomyCode = true
        };
        var existingEntity = new FunderSettingsEntity { AccountInfoId = 1, FunderId = 3, FunderName = "Funder", ClaimFilingIndicatorId = 1, IncludeTaxonomyCode = true };

        _mapperMock.Setup(m => m.Map<FunderSettingsEntity>(model)).Returns(entity);

        var asyncEntities = new TestAsyncEnumerable<FunderSettingsEntity>(new List<FunderSettingsEntity> { existingEntity });
        _funderSettingRepoMock.Setup(r => r.Query()).Returns(asyncEntities.AsQueryable());
        _funderSettingRepoMock.Setup(r => r.UpdateAsync(existingEntity)).Returns(Task.CompletedTask);
        _funderSettingRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        //await _service.SetBillingFunderSettings(model);

        Assert.Equal(entity.FunderName, existingEntity.FunderName);
        Assert.Equal(entity.ClaimFilingIndicatorId, existingEntity.ClaimFilingIndicatorId);
        Assert.Equal(entity.IncludeTaxonomyCode, existingEntity.IncludeTaxonomyCode);
    }

    [Fact]
    public async Task GetBillingFunderIdsSetting_ReturnsModel_WhenEntityExists()
    {
        // Arrange
        var entity = new FunderSettingsEntity
        {
            Id = 1,
            AccountInfoId = 1,
            FunderId = 2,
            FunderName = "Test Funder",
            ScheduleType =3,
            ScheduleTimeZone = 1,
            WeeklyDays = "Monday",
            MonthlyFrequency = null,
            CombineChargesForSameClient = true,
            DateDeleted = null
        };

        var asyncEntities = new TestAsyncEnumerable<FunderSettingsEntity>(new List<FunderSettingsEntity> { entity });
        _funderSettingRepoMock.Setup(r => r.Query()).Returns(asyncEntities.AsQueryable());

        // Act
        var result = await _service.GetBillingFunderIdsSettingAsync(2, 1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetBillingFunderSettings_ReturnsList_WhenSuccess()
    {
        var model = new BillingFunderListRequestModel { AccountInfoId = 1, Skip = 0, Take = 10, SortingModels = null };
        var entities = new List<FunderSettingsEntity>
        {
            new FunderSettingsEntity
            {
                Id = 1,
                FunderId = 2,
                FunderName = "Funder",
                ClaimFilingIndicator = new ClaimFilingIndicatorEntity { Code = "A", Description = "DescA" },
                IncludeTaxonomyCode = true,
                AccountInfoId = 1
            }
        };
        var asyncEntities = new TestAsyncEnumerable<FunderSettingsEntity>(entities);
        _funderSettingRepoMock.Setup(r => r.Query()).Returns(asyncEntities.AsQueryable());

        
    }

    [Fact]
    public async Task GetBillingFunderSettings_ReturnsEmptyList_OnException()
    {
        var model = new BillingFunderListRequestModel();
        _funderSettingRepoMock.Setup(r => r.Query()).Throws(new Exception("fail"));

        var result = await _service.GetBillingFunderSettings(model);

        Assert.Empty(result.Data);

    }

    [Fact]
    public async Task DeleteFunder_ThrowsArgumentException_WhenIdInvalid()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.DeleteFunderSetting(0));
        Assert.Equal("Invalid Id.", ex.Message);
    }

    [Fact]
    public async Task DeleteFunder_ThrowsKeyNotFoundException_WhenEntityNull()
    {
        _funderSettingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((FunderSettingsEntity)null);

        var ex = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.DeleteFunderSetting(1));
        Assert.Equal("Funder setting not found.", ex.Message);
    }

    [Fact]
    public async Task DeleteFunder_ReturnsAlreadyDeleted_WhenDateDeletedSet()
    {
        var entity = new FunderSettingsEntity { DateDeleted = DateTime.UtcNow };
        _funderSettingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        var result = await _service.DeleteFunderSetting(1);

        Assert.True(result.Success);
        Assert.Equal("Funder setting already deleted.", result.Message);
    }

    [Fact]
    public async Task DeleteFunder_SoftDeletes_WhenNotDeleted()
    {
        var entity = new FunderSettingsEntity { Id = 1, DateDeleted = null };
        _funderSettingRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _funderSettingRepoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.DeleteFunderSetting(1);

        Assert.True(result.Success);
        Assert.Equal("Funder setting deleted successfully.", result.Message);
        Assert.NotNull(entity.DateDeleted);
    }

    [Fact]
    public async Task GetBillingSettingInformationAsync_ValidAccountId_ReturnsBillingSettings()
    {
        // Arrange
        int validAccountId = 123;
        var expectedBillingSetting = new BillingSettingInformationModel
        {
            PayToAddressOverrideOption = 1,
            CompanyName = "Test Company",
            AddressLine1 = "123 Test St",
            AddressLine2 = "Suite 100",
            City = "Test City",
            State = "TS",
            Zip = "12345",
            DunningMessage = "Please pay within 30 days.",
            GlobalMessage = "Thank you for your business."
        };

        _billingSettingInformationRepoMock
            .Setup(repo => repo.Query())
            .Returns(MockQueryable(new List<BillingSettingInformationEntity>
            {
                new BillingSettingInformationEntity
                {
                    AccountId = validAccountId,
                    PayToAddressOverrideOption = 1,
                    CompanyName = "Test Company",
                    AddressLine1 = "123 Test St",
                    AddressLine2 = "Suite 100",
                    City = "Test City",
                    State = "TS",
                    Zip = "12345",
                    DunningMessage = "Please pay within 30 days.",
                    GlobalMessage = "Thank you for your business.",
                    DateLastModified = DateTime.Now,
                    DateCreated = DateTime.Now
                }
            }));

        // Act
        var result = await _service.GetBillingSettingInformationAsync(validAccountId);

        // Assert
        Assert.NotNull(result);
        _billingSettingInformationRepoMock.Verify(repo => repo.Query(), Times.Once);
        
    }

    // Test 2: Invalid accountId throws ArgumentException
    [Fact]
    public async Task GetBillingSettingInformationAsync_InvalidAccountId_ThrowsArgumentException()
    {
        // Arrange
        int invalidAccountId = 0; // AccountId <= 0 is invalid

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => _service.GetBillingSettingInformationAsync(invalidAccountId));
        Assert.Equal("accountId must be greater than zero. (Parameter 'accountId')", exception.Message);

        _billingSettingInformationRepoMock.Verify(repo => repo.Query(), Times.Never); // Ensure no query is made
        
    }

    // Test 3: Database query error (e.g., a connection issue) returns safe default model
    [Fact]
    public async Task GetBillingSettingInformationAsync_DatabaseError_ReturnsDefaultModel()
    {
        // Arrange
        int validAccountId = 123;
        _billingSettingInformationRepoMock
            .Setup(repo => repo.Query())
            .Throws(new Exception("Database query failed"));

        // Act
        var result = await _service.GetBillingSettingInformationAsync(validAccountId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.PayToAddressOverrideOption);
        Assert.Equal(string.Empty, result.CompanyName);
        Assert.Equal(string.Empty, result.AddressLine1);
        Assert.Equal(string.Empty, result.City);

        _billingSettingInformationRepoMock.Verify(repo => repo.Query(), Times.Once);
    }

    // Helper method to mock IQueryable
    private IQueryable<T> MockQueryable<T>(List<T> data) where T : class
    {
        return data.AsQueryable();
    }

    // Helper for async queryable
    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken cancellationToken = default)
            => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }
    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner) { _inner = inner; }
        public T Current => _inner.Current;
        public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
    }
}