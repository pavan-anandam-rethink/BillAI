using BillingService.Domain.Interfaces.History;
using BillingService.Domain.Models.BillingSettings;
using BillingService.Domain.Services.FunderSetting;
using BillingService.XUnit.Tests.Common.Mocks;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Entities.Billing.Feature;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Enums.Billing.History;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.FunderSetting;

public class FunderSettingServiceTests
{
    private readonly Mock<IRepository<BillingDbContext, FunderSettingsEntity>> _funderSettingRepoMock;
    private readonly Mock<IRepository<BillingDbContext, ClaimFilingIndicatorEntity>> _claimFilingIndicatorRepoMock;
    private readonly Mock<IRepository<BillingDbContext, TimezonesEntity>> _timezonesRepoMock;
    private readonly Mock<ILogger<FunderSettingService>> _loggerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly FunderSettingService _service;

    public FunderSettingServiceTests()
    {
        _funderSettingRepoMock = new Mock<IRepository<BillingDbContext, FunderSettingsEntity>>();
        _claimFilingIndicatorRepoMock = new Mock<IRepository<BillingDbContext, ClaimFilingIndicatorEntity>>();
        _timezonesRepoMock = new Mock<IRepository<BillingDbContext, TimezonesEntity>>();
        _loggerMock = new Mock<ILogger<FunderSettingService>>();
        _auditServiceMock = new Mock<IAuditService>();

        _funderSettingRepoMock.Setup(x => x.AddAsync(It.IsAny<FunderSettingsEntity>()))
            .Returns(Task.CompletedTask);
        _funderSettingRepoMock.Setup(x => x.UpdateAsync(It.IsAny<FunderSettingsEntity>()))
            .Returns(Task.CompletedTask);
        _funderSettingRepoMock.Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        _service = new FunderSettingService(
            _funderSettingRepoMock.Object,
            _auditServiceMock.Object,
            _claimFilingIndicatorRepoMock.Object,
            _timezonesRepoMock.Object,
            _loggerMock.Object);
    }

    private static FunderSettingRequest BuildValidRequest()
    {
        return new FunderSettingRequest
        {
            Id = null,
            AccountInfoId = 1,
            FunderId = 10,
            FunderName = "TestFunder",
            ClaimFilingIndicatorId = 5,
            IncludeTaxonomyCode = true,
            ChangedBy = 99,
            Data = new FunderSettingsRequest
            {
                ScheduleType = 1,
                ScheduleTime = "08:00",
                ScheduleTimeZone = 3,
                WeeklyDays = "Mon,Tue",
                MonthlyFrequency = 1,
                CombineChargesForSameClient = true
            }
        };
    }

    private static FunderSettingsEntity BuildExistingEntity(int accountInfoId = 1, int funderId = 10)
    {
        return new FunderSettingsEntity
        {
            Id = 42,
            AccountInfoId = accountInfoId,
            FunderId = funderId,
            FunderName = "OldFunder",
            ClaimFilingIndicatorId = 5,
            IncludeTaxonomyCode = false,
            ScheduleTime = TimeSpan.FromHours(9),
            ScheduleTimeZone = 3,
            WeeklyDays = "Wed",
            MonthlyFrequency = 1,
            CombineChargesForSameClient = false,
            DateDeleted = null
        };
    }

    private void SetupFunderSettingQuery(params FunderSettingsEntity[] entities)
    {
        var asyncEnum = new TestAsyncEnumerable<FunderSettingsEntity>(entities.ToList());
        _funderSettingRepoMock.Setup(r => r.Query()).Returns(asyncEnum.AsQueryable());
    }

    private void SetupClaimFilingIndicatorQuery(int id, string description)
    {
        var list = new List<ClaimFilingIndicatorEntity>
        {
            new ClaimFilingIndicatorEntity { Id = id, Description = description }
        };
        var asyncEnum = new TestAsyncEnumerable<ClaimFilingIndicatorEntity>(list);
        _claimFilingIndicatorRepoMock.Setup(r => r.Query()).Returns(asyncEnum.AsQueryable());
    }

    private void SetupTimezoneQuery(int id, string displayName)
    {
        var list = new List<TimezonesEntity>
        {
            new TimezonesEntity { Id = id, DisplayName = displayName }
        };
        var asyncEnum = new TestAsyncEnumerable<TimezonesEntity>(list);
        _timezonesRepoMock.Setup(r => r.Query()).Returns(asyncEnum.AsQueryable());
    }

    private void SetupLookupRepos()
    {
        SetupClaimFilingIndicatorQuery(5, "CI-Desc");
        SetupTimezoneQuery(3, "Eastern Time");
    }

    [Fact]
    public async Task UpdateFunderSettingsAsync_NoExisting_Should_Create_And_AuditInsert()
    {
        var model = BuildValidRequest();

        SetupFunderSettingQuery();
        SetupLookupRepos();

        await _service.UpdateFunderSettingsAsync(model);

        _funderSettingRepoMock.Verify(r => r.AddAsync(It.Is<FunderSettingsEntity>(e =>
            e.AccountInfoId == model.AccountInfoId &&
            e.FunderId == model.FunderId &&
            e.FunderName == model.FunderName)), Times.Once);

        _funderSettingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);

        _auditServiceMock.Verify(a => a.TrackAsync(
            ActionType.I,
            model.ChangedBy,
            model.AccountInfoId,
            model.FunderId,
            nameof(FunderSettingsEntity),
            It.Is<FunderSettingsAuditEntity>(x => x == null),
            It.IsAny<FunderSettingsAuditEntity>(),
            It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFunderSettingsAsync_ExistingFound_Should_Update_And_AuditUpdate()
    {
        var model = BuildValidRequest();
        var existing = BuildExistingEntity();

        SetupFunderSettingQuery(existing);
        SetupLookupRepos();

        await _service.UpdateFunderSettingsAsync(model);

        _funderSettingRepoMock.Verify(r => r.UpdateAsync(It.Is<FunderSettingsEntity>(e =>
            e.FunderName == model.FunderName &&
            e.IncludeTaxonomyCode == model.IncludeTaxonomyCode &&
            e.ClaimFilingIndicatorId == model.ClaimFilingIndicatorId &&
            e.ScheduleTimeZone == model.Data.ScheduleTimeZone &&
            e.WeeklyDays == model.Data.WeeklyDays &&
            e.MonthlyFrequency == model.Data.MonthlyFrequency &&
            e.CombineChargesForSameClient == model.Data.CombineChargesForSameClient)), Times.Once);

        _funderSettingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);

        _auditServiceMock.Verify(a => a.TrackAsync(
            ActionType.U,
            model.ChangedBy,
            model.AccountInfoId,
            model.FunderId,
            nameof(FunderSettingsEntity),
            It.IsAny<FunderSettingsAuditEntity>(),
            It.IsAny<FunderSettingsAuditEntity>(),
            It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateFunderSettingsAsync_ExistingFound_Should_Apply_AllFields()
    {
        var model = BuildValidRequest();
        var existing = BuildExistingEntity();

        SetupFunderSettingQuery(existing);
        SetupLookupRepos();

        await _service.UpdateFunderSettingsAsync(model);

        Assert.Equal(model.AccountInfoId, existing.AccountInfoId);
        Assert.Equal(model.FunderId, existing.FunderId);
        Assert.Equal(model.FunderName, existing.FunderName);
        Assert.Equal(model.ClaimFilingIndicatorId, existing.ClaimFilingIndicatorId);
        Assert.Equal(model.IncludeTaxonomyCode, existing.IncludeTaxonomyCode);
        Assert.Equal(TimeSpan.Parse(model.Data.ScheduleTime), existing.ScheduleTime);
        Assert.Equal(model.Data.ScheduleTimeZone, existing.ScheduleTimeZone);
        Assert.Equal(model.Data.WeeklyDays, existing.WeeklyDays);
        Assert.Equal(model.Data.MonthlyFrequency, existing.MonthlyFrequency);
        Assert.Equal(model.Data.CombineChargesForSameClient, existing.CombineChargesForSameClient);
    }

    [Fact]
    public async Task UpdateFunderSettingsAsync_ExistingIsDeleted_Should_TreatAsCreate()
    {
        var model = BuildValidRequest();
        var deleted = BuildExistingEntity();
        deleted.DateDeleted = DateTime.UtcNow;

        SetupFunderSettingQuery(deleted);
        SetupLookupRepos();

        await _service.UpdateFunderSettingsAsync(model);

        _funderSettingRepoMock.Verify(r => r.AddAsync(It.IsAny<FunderSettingsEntity>()), Times.Once);
        _funderSettingRepoMock.Verify(r => r.UpdateAsync(It.IsAny<FunderSettingsEntity>()), Times.Never);
    }

    [Fact]
    public async Task UpdateFunderSettingsAsync_RepoThrows_Should_Log_And_Rethrow()
    {
        var model = BuildValidRequest();

        _funderSettingRepoMock.Setup(r => r.Query())
            .Throws(new InvalidOperationException("DB error"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateFunderSettingsAsync(model));

        Assert.Equal("DB error", ex.Message);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<InvalidOperationException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateFunderSettingsAsync_InvalidScheduleTime_Update_Should_Throw_ArgumentException()
    {
        var model = BuildValidRequest();
        model.Data.ScheduleTime = "not-a-timespan";
        var existing = BuildExistingEntity();

        SetupFunderSettingQuery(existing);
        SetupLookupRepos();

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateFunderSettingsAsync(model));

        Assert.Contains("ScheduleTime", ex.Message);
    }

    //[Fact]
    //public async Task UpdateFunderSettingsAsync_InvalidScheduleTimeZone_Should_Throw_ArgumentException()
    //{
    //    var model = BuildValidRequest();
    //    var existing = BuildExistingEntity();
    //    existing.ScheduleTimeZone = -1;

    //    SetupFunderSettingQuery(existing);
    //    SetupLookupRepos();

    //    var ex = await Assert.ThrowsAsync<ArgumentException>(
    //        () => _service.UpdateFunderSettingsAsync(model));

    //    Assert.Contains("ScheduleTimeZone", ex.Message);
    //}

    //[Fact]
    //public async Task UpdateFunderSettingsAsync_InvalidBillingScheduleType_Should_Throw_ArgumentException()
    //{
    //    var model = BuildValidRequest();
    //    var existing = BuildExistingEntity();
    //    existing.BillingScheduleType = "invalid";

    //    SetupFunderSettingQuery(existing);
    //    SetupLookupRepos();

    //    var ex = await Assert.ThrowsAsync<ArgumentException>(
    //        () => _service.UpdateFunderSettingsAsync(model));

    //    Assert.Contains("BillingScheduleType", ex.Message);
    //}

    //[Fact]
    //public async Task UpdateFunderSettingsAsync_InvalidMonthlyFrequency_Should_Throw_ArgumentException()
    //{
    //    var model = BuildValidRequest();
    //    var existing = BuildExistingEntity();
    //    existing.MonthlyFrequency = -1;

    //    SetupFunderSettingQuery(existing);
    //    SetupLookupRepos();

    //    var ex = await Assert.ThrowsAsync<ArgumentException>(
    //        () => _service.UpdateFunderSettingsAsync(model));

    //    Assert.Contains("MonthlyFrequency", ex.Message);
    //}


    [Fact]
    public async Task UpdateFunderSettingsAsync_ClaimIndicatorNotFound_Should_StillCreate()
    {
        var model = BuildValidRequest();

        SetupFunderSettingQuery();
        var empty = new TestAsyncEnumerable<ClaimFilingIndicatorEntity>(new List<ClaimFilingIndicatorEntity>());
        _claimFilingIndicatorRepoMock.Setup(r => r.Query()).Returns(empty.AsQueryable());
        SetupTimezoneQuery(3, "Eastern Time");

        await _service.UpdateFunderSettingsAsync(model);

        _funderSettingRepoMock.Verify(r => r.AddAsync(It.IsAny<FunderSettingsEntity>()), Times.Once);
        _funderSettingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateFunderSettingsAsync_TimezoneNotFound_Should_StillCreate()
    {
        var model = BuildValidRequest();

        SetupFunderSettingQuery();
        SetupClaimFilingIndicatorQuery(5, "CI-Desc");
        var empty = new TestAsyncEnumerable<TimezonesEntity>(new List<TimezonesEntity>());
        _timezonesRepoMock.Setup(r => r.Query()).Returns(empty.AsQueryable());

        await _service.UpdateFunderSettingsAsync(model);

        _funderSettingRepoMock.Verify(r => r.AddAsync(It.IsAny<FunderSettingsEntity>()), Times.Once);
        _funderSettingRepoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
