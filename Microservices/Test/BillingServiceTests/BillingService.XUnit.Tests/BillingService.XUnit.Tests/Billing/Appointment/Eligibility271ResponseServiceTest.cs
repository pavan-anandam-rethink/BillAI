using AutoFixture;
using BillingService.Domain.Services.Billing;
using BillingService.Domain.Interfaces.Billing;
using Moq;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Models.Claim;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Appointment
{
    public class Eligibility271ResponseServiceTest
    {
        private readonly Mock<IRepository<BillingDbContext, Eligibility271ResponseEntity>> _repository;
        private readonly IEligibility271ResponseService _eligibility271ResponseService;
        public Eligibility271ResponseServiceTest()
        {
            _repository = new Mock<IRepository<BillingDbContext, Eligibility271ResponseEntity>>();
            _eligibility271ResponseService = new Eligibility271ResponseService(_repository.Object);
        }



        [Fact]
        public async Task GetEligibilityResponse_ShouldReturnMappedResponse_WhenEntityExists()
        {
            // Arrange
            var fixture = new Fixture();

            var request = fixture.Build<EligibilityRequest>()
                .With(x => x.CreatedDate, DateTime.Today)
                .Create();

            var entity = fixture.Build<Eligibility271ResponseEntity>()
                .With(x => x.FunderId, request.FunderId)
                .With(x => x.CreatedBy, request.CreatedBy)
                .With(x => x.CreatedDate, request.CreatedDate)
                .Create();

            var entities = new List<Eligibility271ResponseEntity> { entity };

            _repository
                .Setup(r => r.GetMany(
                    It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                    null,
                    null,
                    0,
                    0
                ))
                .Returns(entities);

            // Act
            var result = await _eligibility271ResponseService.GetEligibilityResponse(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(entity.FunderId, result.FunderId);
            Assert.Equal(entity.AccountId, result.AccountId);
            Assert.Equal(entity.EffectiveStartDate, result.EffectiveStartDate);
            Assert.Equal(entity.EffectiveEndDate, result.EffectiveEndDate);
            Assert.Equal(entity.CoverageStatus, result.CoverageStatus);
            Assert.Equal(entity.SubscriberStartDate, result.SubscriberStartDate);
            Assert.Equal(entity.SubscriberEndDate, result.SubscriberEndDate);
            Assert.Equal(entity.PlanStartDate, result.PlanStartDate);
            Assert.Equal(entity.PlanEndDate, result.PlanEndDate);

            _repository.Verify(r => r.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                 null,
                    null,
                    0,
                    0
            ), Times.Once);
        }

        [Fact]
        public void GetEligibilityResponse_ShouldReturnNull_WhenNoEntityExists()
        {
            // Arrange
            var fixture = new Fixture();

            var request = fixture.Build<EligibilityRequest>()
                .With(x => x.CreatedDate, DateTime.Today)
                .Create();

            _repository
                .Setup(r => r.GetMany(
                    It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                    null,
                    null,
                    0,
                    0))
                .Returns(new List<Eligibility271ResponseEntity>());

            // Act
            var task = _eligibility271ResponseService.GetEligibilityResponse(request);

            // Assert
            Assert.Null(task);

            _repository.Verify(r => r.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                null,
                null,
                0,
                0),
                Times.Once);
        }



        [Fact]
        public async Task GetEligibilityResponse_ShouldReturnFirstEntity_WhenMultipleEntitiesExist()
        {
            // Arrange
            var fixture = new Fixture();

            var request = fixture.Build<EligibilityRequest>()
                .With(x => x.CreatedDate, DateTime.Today)
                .Create();

            var firstEntity = fixture.Build<Eligibility271ResponseEntity>()
                .With(x => x.FunderId, request.FunderId)
                .With(x => x.CreatedBy, request.CreatedBy)
                .With(x => x.CreatedDate, request.CreatedDate)
                .Create();

            var secondEntity = fixture.Build<Eligibility271ResponseEntity>()
                .With(x => x.FunderId, request.FunderId)
                .With(x => x.CreatedBy, request.CreatedBy)
                .With(x => x.CreatedDate, request.CreatedDate)
                .Create();

            _repository
                .Setup(r => r.GetMany(
                    It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                    null,
                    null,
                    0,
                    0))
                .Returns(new List<Eligibility271ResponseEntity>
                {
            firstEntity,
            secondEntity
                });

            // Act
            var result = await _eligibility271ResponseService.GetEligibilityResponse(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(firstEntity.FunderId, result.FunderId);
            Assert.Equal(firstEntity.AccountId, result.AccountId);

            _repository.Verify(r => r.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                null,
                null,
                0,
                0),
                Times.Once);
        }


        [Fact]
        public void GetEligibilityResponse_ShouldReturnNull_WhenCreatedDateIsNull()
        {
            // Arrange
            var fixture = new Fixture();

            var request = fixture.Build<EligibilityRequest>()
                .With(x => x.CreatedDate, DateTime.Today)
                .Create();

            var entityWithNullDate = fixture.Build<Eligibility271ResponseEntity>()
                .With(x => x.FunderId, request.FunderId)
                .With(x => x.CreatedBy, request.CreatedBy)
                .With(x => x.CreatedDate, (DateTime?)null)
                .Create();

            _repository
                .Setup(r => r.GetMany(
                    It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                    null,
                    null,
                    0,
                    0))
                .Returns(new List<Eligibility271ResponseEntity>());

            // Act
            var task = _eligibility271ResponseService.GetEligibilityResponse(request);

            // Assert
            Assert.Null(task);

            _repository.Verify(r => r.GetMany(
                It.IsAny<Expression<Func<Eligibility271ResponseEntity, bool>>>(),
                null,
                null,
                0,
                0), Times.Once);
        }
    }
}
