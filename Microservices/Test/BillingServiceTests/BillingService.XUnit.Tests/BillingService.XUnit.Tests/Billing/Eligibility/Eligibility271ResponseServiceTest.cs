using BillingService.Domain.Services.Billing;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Models.Claim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

public class Eligibility271ResponseServiceTests
{
    private readonly Mock<IRepository<BillingDbContext, Eligibility271ResponseEntity>> _repoMock;
    private readonly Eligibility271ResponseService _service;

    public Eligibility271ResponseServiceTests()
    {
        _repoMock = new Mock<IRepository<BillingDbContext, Eligibility271ResponseEntity>>();
        _service = new Eligibility271ResponseService(_repoMock.Object);
    }

    [Fact]
    public async Task GetEligibilityResponse_EntityExists_ReturnsResponse()
    {
        var today = DateTime.Today;

        var entity = new Eligibility271ResponseEntity
        {
            FunderId = 1,
            AccountId = 10,
            CreatedBy = 100,
            CreatedDate = today,
            CoverageStatus = "Active",
            EffectiveStartDate = today,
            EffectiveEndDate = today,
            SubscriberStartDate = today,
            SubscriberEndDate = today,
            PlanStartDate = today,
            PlanEndDate = today,
            FailureResponse = "OK"
        };

        var list = new List<Eligibility271ResponseEntity> { entity };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = 1,
            CreatedBy = 100,
            CreatedDate = today
        };

        var result = await _service.GetEligibilityResponse(request);

        Assert.NotNull(result);
        Assert.Equal(1, result.FunderId);
        Assert.Equal("OK", result.FailureResponse);
    }

    [Fact]
    public async Task GetEligibilityResponse_FailureResponseNull_ReturnsEmpty()
    {
        var today = DateTime.Today;

        var entity = new Eligibility271ResponseEntity
        {
            FunderId = 2,
            AccountId = 20,
            CreatedBy = 200,
            CreatedDate = today,
            FailureResponse = null
        };

        var list = new List<Eligibility271ResponseEntity> { entity };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = 2,
            CreatedBy = 200,
            CreatedDate = today
        };

        var result = await _service.GetEligibilityResponse(request);

        Assert.Equal(string.Empty, result.FailureResponse);
    }

    [Fact]
    public async Task GetEligibilityResponse_MultipleEntities_ReturnsFirst()
    {
        var today = DateTime.Today;

        var entity1 = new Eligibility271ResponseEntity
        {
            FunderId = 1,
            AccountId = 10,
            CreatedBy = 100,
            CreatedDate = today,
            CoverageStatus = "Active",
            FailureResponse = "First"
        };

        var entity2 = new Eligibility271ResponseEntity
        {
            FunderId = 1,
            AccountId = 20,
            CreatedBy = 100,
            CreatedDate = today,
            CoverageStatus = "Inactive",
            FailureResponse = "Second"
        };

        var list = new List<Eligibility271ResponseEntity> { entity1, entity2 };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = 1,
            CreatedBy = 100,
            CreatedDate = today
        };

        var result = await _service.GetEligibilityResponse(request);

        Assert.NotNull(result);
        Assert.Equal(10, result.AccountId);
        Assert.Equal("First", result.FailureResponse);
    }

    [Fact]
    public async Task GetEligibilityResponse_MapsAllProperties_Correctly()
    {
        var today = DateTime.Today;
        var startDate = today.AddDays(-30);
        var endDate = today.AddDays(30);

        var entity = new Eligibility271ResponseEntity
        {
            FunderId = 5,
            AccountId = 50,
            CreatedBy = 500,
            CreatedDate = today,
            CoverageStatus = "Eligible",
            EffectiveStartDate = startDate,
            EffectiveEndDate = endDate,
            SubscriberStartDate = startDate.AddDays(1),
            SubscriberEndDate = endDate.AddDays(-1),
            PlanStartDate = startDate.AddDays(2),
            PlanEndDate = endDate.AddDays(-2),
            FailureResponse = "Success"
        };

        var list = new List<Eligibility271ResponseEntity> { entity };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = 5,
            CreatedBy = 500,
            CreatedDate = today
        };

        var result = await _service.GetEligibilityResponse(request);

        Assert.NotNull(result);
        Assert.Equal(5, result.FunderId);
        Assert.Equal(50, result.AccountId);
        Assert.Equal("Eligible", result.CoverageStatus);
        Assert.Equal(startDate, result.EffectiveStartDate);
        Assert.Equal(endDate, result.EffectiveEndDate);
        Assert.Equal(startDate.AddDays(1), result.SubscriberStartDate);
        Assert.Equal(endDate.AddDays(-1), result.SubscriberEndDate);
        Assert.Equal(startDate.AddDays(2), result.PlanStartDate);
        Assert.Equal(endDate.AddDays(-2), result.PlanEndDate);
        Assert.Equal("Success", result.FailureResponse);
    }

    [Fact]
    public async Task GetEligibilityResponse_NullDates_MapsCorrectly()
    {
        var today = DateTime.Today;

        var entity = new Eligibility271ResponseEntity
        {
            FunderId = 3,
            AccountId = 30,
            CreatedBy = 300,
            CreatedDate = today,
            CoverageStatus = "Active",
            EffectiveStartDate = null,
            EffectiveEndDate = null,
            SubscriberStartDate = null,
            SubscriberEndDate = null,
            PlanStartDate = null,
            PlanEndDate = null,
            FailureResponse = "OK"
        };

        var list = new List<Eligibility271ResponseEntity> { entity };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = 3,
            CreatedBy = 300,
            CreatedDate = today
        };

        var result = await _service.GetEligibilityResponse(request);

        Assert.NotNull(result);
        Assert.Null(result.EffectiveStartDate);
        Assert.Null(result.EffectiveEndDate);
        Assert.Null(result.SubscriberStartDate);
        Assert.Null(result.SubscriberEndDate);
        Assert.Null(result.PlanStartDate);
        Assert.Null(result.PlanEndDate);
    }

    [Fact]
    public async Task GetEligibilityResponse_NullCoverageStatus_MapsCorrectly()
    {
        var today = DateTime.Today;

        var entity = new Eligibility271ResponseEntity
        {
            FunderId = 4,
            AccountId = 40,
            CreatedBy = 400,
            CreatedDate = today,
            CoverageStatus = null,
            FailureResponse = "OK"
        };

        var list = new List<Eligibility271ResponseEntity> { entity };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = 4,
            CreatedBy = 400,
            CreatedDate = today
        };

        var result = await _service.GetEligibilityResponse(request);

        Assert.NotNull(result);
        Assert.Null(result.CoverageStatus);
    }

    [Fact]
    public async Task GetEligibilityResponse_EmptyFailureResponse_ReturnsEmpty()
    {
        var today = DateTime.Today;

        var entity = new Eligibility271ResponseEntity
        {
            FunderId = 6,
            AccountId = 60,
            CreatedBy = 600,
            CreatedDate = today,
            FailureResponse = string.Empty
        };

        var list = new List<Eligibility271ResponseEntity> { entity };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = 6,
            CreatedBy = 600,
            CreatedDate = today
        };

        var result = await _service.GetEligibilityResponse(request);

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.FailureResponse);
    }

    [Fact]
    public async Task GetEligibilityResponse_ZeroFunderId_WorksCorrectly()
    {
        var today = DateTime.Today;

        var entity = new Eligibility271ResponseEntity
        {
            FunderId = 0,
            AccountId = 10,
            CreatedBy = 100,
            CreatedDate = today,
            FailureResponse = "Zero Funder"
        };

        var list = new List<Eligibility271ResponseEntity> { entity };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = 0,
            CreatedBy = 100,
            CreatedDate = today
        };

        var result = await _service.GetEligibilityResponse(request);

        Assert.NotNull(result);
        Assert.Equal(0, result.FunderId);
    }

    [Fact]
    public async Task GetEligibilityResponse_NegativeFunderId_WorksCorrectly()
    {
        var today = DateTime.Today;

        var entity = new Eligibility271ResponseEntity
        {
            FunderId = -1,
            AccountId = 10,
            CreatedBy = 100,
            CreatedDate = today,
            FailureResponse = "Negative Funder"
        };

        var list = new List<Eligibility271ResponseEntity> { entity };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = -1,
            CreatedBy = 100,
            CreatedDate = today
        };

        var result = await _service.GetEligibilityResponse(request);

        Assert.NotNull(result);
        Assert.Equal(-1, result.FunderId);
    }

    [Fact]
    public async Task GetEligibilityResponse_FutureCreatedDate_WorksCorrectly()
    {
        var futureDate = DateTime.Today.AddDays(10);

        var entity = new Eligibility271ResponseEntity
        {
            FunderId = 9,
            AccountId = 90,
            CreatedBy = 900,
            CreatedDate = futureDate,
            FailureResponse = "Future"
        };

        var list = new List<Eligibility271ResponseEntity> { entity };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = 9,
            CreatedBy = 900,
            CreatedDate = futureDate
        };

        var result = await _service.GetEligibilityResponse(request);

        Assert.NotNull(result);
        Assert.Equal("Future", result.FailureResponse);
    }

    [Fact]
    public async Task GetEligibilityResponse_PastCreatedDate_WorksCorrectly()
    {
        var pastDate = DateTime.Today.AddYears(-1);

        var entity = new Eligibility271ResponseEntity
        {
            FunderId = 10,
            AccountId = 100,
            CreatedBy = 1000,
            CreatedDate = pastDate,
            FailureResponse = "Past"
        };

        var list = new List<Eligibility271ResponseEntity> { entity };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = 10,
            CreatedBy = 1000,
            CreatedDate = pastDate
        };

        var result = await _service.GetEligibilityResponse(request);

        Assert.NotNull(result);
        Assert.Equal("Past", result.FailureResponse);
    }

    [Fact]
    public async Task GetEligibilityResponse_DateWithTime_IgnoresTimePortion()
    {
        var today = DateTime.Today;
        var todayWithTime = today.AddHours(14).AddMinutes(30);

        var entity = new Eligibility271ResponseEntity
        {
            FunderId = 11,
            AccountId = 110,
            CreatedBy = 1100,
            CreatedDate = todayWithTime,
            FailureResponse = "With Time"
        };

        var list = new List<Eligibility271ResponseEntity> { entity };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = 11,
            CreatedBy = 1100,
            CreatedDate = today
        };

        var result = await _service.GetEligibilityResponse(request);

        Assert.NotNull(result);
        Assert.Equal("With Time", result.FailureResponse);
    }

    [Fact]
    public async Task GetEligibilityResponse_LongFailureResponse_HandlesCorrectly()
    {
        var today = DateTime.Today;
        var longResponse = new string('A', 1000);

        var entity = new Eligibility271ResponseEntity
        {
            FunderId = 12,
            AccountId = 120,
            CreatedBy = 1200,
            CreatedDate = today,
            FailureResponse = longResponse
        };

        var list = new List<Eligibility271ResponseEntity> { entity };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = 12,
            CreatedBy = 1200,
            CreatedDate = today
        };

        var result = await _service.GetEligibilityResponse(request);

        Assert.NotNull(result);
        Assert.Equal(longResponse, result.FailureResponse);
    }

    [Fact]
    public async Task GetEligibilityResponse_SpecialCharactersInCoverageStatus_HandlesCorrectly()
    {
        var today = DateTime.Today;

        var entity = new Eligibility271ResponseEntity
        {
            FunderId = 13,
            AccountId = 130,
            CreatedBy = 1300,
            CreatedDate = today,
            CoverageStatus = "Active-Premium@2024!#$%",
            FailureResponse = "OK"
        };

        var list = new List<Eligibility271ResponseEntity> { entity };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = 13,
            CreatedBy = 1300,
            CreatedDate = today
        };

        var result = await _service.GetEligibilityResponse(request);

        Assert.NotNull(result);
        Assert.Equal("Active-Premium@2024!#$%", result.CoverageStatus);
    }

    [Fact]
    public async Task GetEligibilityResponse_ReturnsCompletedTask()
    {
        var today = DateTime.Today;

        var entity = new Eligibility271ResponseEntity
        {
            FunderId = 14,
            AccountId = 140,
            CreatedBy = 1400,
            CreatedDate = today,
            FailureResponse = "Sync"
        };

        var list = new List<Eligibility271ResponseEntity> { entity };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = 14,
            CreatedBy = 1400,
            CreatedDate = today
        };

        var task = _service.GetEligibilityResponse(request);

        Assert.True(task.IsCompleted);
        var result = await task;
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetEligibilityResponse_WhitespaceFailureResponse_PreservesWhitespace()
    {
        var today = DateTime.Today;

        var entity = new Eligibility271ResponseEntity
        {
            FunderId = 15,
            AccountId = 150,
            CreatedBy = 1500,
            CreatedDate = today,
            FailureResponse = "   "
        };

        var list = new List<Eligibility271ResponseEntity> { entity };

        _repoMock
            .Setup(x => x.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                It.IsAny<Func<IQueryable<Eligibility271ResponseEntity>, IOrderedQueryable<Eligibility271ResponseEntity>>>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .Returns(() => list);

        var request = new EligibilityRequest
        {
            FunderId = 15,
            CreatedBy = 1500,
            CreatedDate = today
        };

        var result = await _service.GetEligibilityResponse(request);

        Assert.NotNull(result);
        Assert.Equal("   ", result.FailureResponse);
    }
}