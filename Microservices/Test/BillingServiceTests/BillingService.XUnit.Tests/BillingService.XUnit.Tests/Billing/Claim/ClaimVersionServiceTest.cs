using AutoFixture;
using BillingService.Domain.Models;
using BillingService.Domain.Services.Billing;
using BillingService.XUnit.Tests.Common;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Claim
{
    public class ClaimVersionServiceTest : BaseTest
    {
        private readonly Mock<IRepository<BillingDbContext, ClaimVersionEntity>> _claimVersionRepository;
        private readonly ClaimVersionService _claimVersionService;
        public ClaimVersionServiceTest()
        {
            _claimVersionRepository = new Mock<IRepository<BillingDbContext, ClaimVersionEntity>>();
            _claimVersionService = new ClaimVersionService(_claimVersionRepository.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEntity_WhenFound()
        {
            var claimVersion = Fixture.Build<ClaimVersionEntity>()
                                         .With(x => x.Id, 10)
                                         .With(x => x.ClaimId, 1100)
                                         .Create();
            _claimVersionRepository.Setup(r => r.GetByIdAsync(claimVersion.Id))
                                   .ReturnsAsync(claimVersion);

            var result = await _claimVersionService.GetByIdAsync(claimVersion.Id);

            Assert.NotNull(result);
            Assert.Equal(claimVersion, result);
            Assert.Equal(claimVersion.Id, result.Id);
            Assert.Equal(claimVersion.ClaimId, result.ClaimId);
            _claimVersionRepository.Verify(r => r.GetByIdAsync(claimVersion.Id), Times.Once);

        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNewEntity_WhenNotFound()
        {
            // Arrange
            _claimVersionRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((ClaimVersionEntity)null);

            // Act
            var result = await _claimVersionService.GetByIdAsync(999);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ClaimVersionEntity>(result);
            Assert.Equal(0, result.Id);

            _claimVersionRepository.Verify(r => r.GetByIdAsync(999), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldCallRepositoryWithCorrectId()
        {
            // Arrange
            int id = 55;

            _claimVersionRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((ClaimVersionEntity)null);

            // Act
            await _claimVersionService.GetByIdAsync(id);

            // Assert
            _claimVersionRepository.Verify(r => r.GetByIdAsync(55), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnEntityWithDefaultValues_WhenNotFound()
        {
            // Arrange
            _claimVersionRepository
                .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((ClaimVersionEntity)null);

            // Act
            var result = await _claimVersionService.GetByIdAsync(123);

            // Assert
            Assert.Equal(0, result.Id);
            Assert.Equal(default(Guid), result.Identifier);
            Assert.Null(result.ClaimIdentifier);
            Assert.Equal(0, result.ClaimId);
            Assert.Equal(0, result.AccountInfoId);
            Assert.Equal(0, result.MemberId);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnSameEntityInstance_FromRepository()
        {
            // Arrange
            var entity = Fixture.Build<ClaimVersionEntity>().With(x => x.Id, 77).Create();

            _claimVersionRepository
                .Setup(r => r.GetByIdAsync(77))
                .ReturnsAsync(entity);

            // Act
            var result = await _claimVersionService.GetByIdAsync(77);

            // Assert
            Assert.Same(entity, result); 
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateClaimVersion_AndReturnId()
        {
            // Arrange
            var claim = Fixture.Build<ClaimDetailsModel>()
                .With(x => x.Id, 10)
                .With(x => x.ClaimIdentifier, "CLM-999")
                .With(x => x.ClaimStatus, ClaimStatus.Pending)
                .With(x => x.DiagnosisCodes, new List<ClaimDiagnosisCodeModel>
                {
            new ClaimDiagnosisCodeModel { DiagnosisCode = "A01" },
            new ClaimDiagnosisCodeModel { DiagnosisCode = "B02" }
                })
                .Create();

            int accountInfoId = 100;
            int memberId = 200;

            var savedEntity = new ClaimVersionEntity { Id = 999 };

            _claimVersionRepository
                .Setup(r => r.AddAsync(It.IsAny<ClaimVersionEntity>()))
                .Callback<ClaimVersionEntity>(e => savedEntity = e)
                .Returns(Task.CompletedTask);

            _claimVersionRepository
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _claimVersionService.CreateAsync(claim, accountInfoId, memberId);

            // Assert
            Assert.Equal(savedEntity.Id, result);

            Assert.Equal(10, savedEntity.ClaimId);
            Assert.Equal("CLM-999", savedEntity.ClaimIdentifier);
            Assert.Equal(accountInfoId, savedEntity.AccountInfoId);
            Assert.Equal(memberId, savedEntity.MemberId);
            Assert.Equal(ClaimStatus.Pending, savedEntity.Status);

            Assert.NotEqual(Guid.Empty, savedEntity.Identifier);
            Assert.Equal("A01, B02", savedEntity.DiagnosisCodes);

            _claimVersionRepository.Verify(r => r.AddAsync(It.IsAny<ClaimVersionEntity>()), Times.Once);
            _claimVersionRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        }
        [Fact]
        public async Task CreateAsync_ShouldMapAllFieldsCorrectly()
        {
            // Arrange
            var claim = Fixture.Build<ClaimDetailsModel>()
                .With(x => x.Id, 1)
                .With(x => x.PatientName, "John Doe")
                .With(x => x.ResponsibleParty, "Jane Doe")
                .With(x => x.DateOfServiceStart, DateTime.Today)
                .With(x => x.DateOfServiceEnd, DateTime.Today.AddDays(5))
                .With(x => x.AuthorizationNumber, "AUTH123")
                .With(x => x.BalanceAmount, 100)
                .With(x => x.PaymentAmount, 50)
                .With(x => x.BilledAmount, 150)
                .With(x => x.PatientResponsibilityAmount, 30)
                .With(x => x.PlaceOfService, "Hospital")
                .With(x => x.ServiceFacility, "Facility A")
                .With(x => x.SubmissionReason, 2)
                .With(x => x.PatientReleaseAgreement, 1)
                .With(x => x.AuthorizePayment, 3)
                .With(x => x.SubmissionCode, "SC01")
                .With(x => x.OriginalClaim, "Org123")
                .With(x => x.Note, "Sample note")
                .Create();

            ClaimVersionEntity captured = null;

            _claimVersionRepository.Setup(r => r.AddAsync(It.IsAny<ClaimVersionEntity>()))
                .Callback<ClaimVersionEntity>(c => captured = c)
                .Returns(Task.CompletedTask);

            _claimVersionRepository.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _claimVersionService.CreateAsync(claim, 10, 20);

            // Assert
            Assert.NotNull(captured);

            Assert.Equal("John Doe", captured.ClientName);
            Assert.Equal("Jane Doe", captured.ResponsibleParty);
            Assert.Equal(claim.DateOfServiceStart, captured.StartDate);
            Assert.Equal(claim.DateOfServiceEnd, captured.EndDate);
            Assert.Equal("AUTH123", captured.AuthorizationNumber);

            Assert.Equal(100, captured.BalanceAmount);
            Assert.Equal(50, captured.PaymentAmount);
            Assert.Equal(150, captured.BilledAmount);
            Assert.Equal(30, captured.PatientResponsibilityAmount);

            Assert.Equal("Hospital", captured.PlaceOfService);
            Assert.Equal("Facility A", captured.ServiceProvider);

            Assert.Equal(2, captured.SubmissionReason);
            Assert.Equal("Yes", captured.AuthorizedReleaseOfInfo);
            Assert.Equal("Not applicable", captured.AuthorizePayment);
            Assert.Equal("SC01", captured.SubmissionCode);
            Assert.Equal("Org123", captured.OriginalClaim);
            Assert.Equal("Sample note", captured.Note);
        }

        [Fact]
        public async Task CreateAsync_ShouldMapEmptyDiagnosisCodesToEmptyString()
        {
            // Arrange
            var claim = Fixture.Build<ClaimDetailsModel>()
                .With(x => x.DiagnosisCodes, new List<ClaimDiagnosisCodeModel>())
                .Create();

            ClaimVersionEntity captured = null;

            _claimVersionRepository
                .Setup(r => r.AddAsync(It.IsAny<ClaimVersionEntity>()))
                .Callback<ClaimVersionEntity>(e => captured = e)
                .Returns(Task.CompletedTask);

            _claimVersionRepository
                .Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _claimVersionService.CreateAsync(claim, 10, 20);

            // Assert
            Assert.NotNull(captured);
            Assert.Equal("", captured.DiagnosisCodes);
        }
        [Fact]
        public async Task CreateAsync_ShouldSetAuditFields()
        {
            // Arrange
            var claim = Fixture.Create<ClaimDetailsModel>();
            int memberId = 500;

            ClaimVersionEntity captured = null;

            _claimVersionRepository
                .Setup(r => r.AddAsync(It.IsAny<ClaimVersionEntity>()))
                .Callback<ClaimVersionEntity>(e => captured = e);

            _claimVersionRepository.Setup(r => r.SaveChangesAsync())
                .Returns(Task.CompletedTask);

            // Act
            await _claimVersionService.CreateAsync(claim, 1, memberId);

            // Assert
            Assert.NotNull(captured);
            Assert.Equal(memberId, captured.CreatedBy);
            Assert.Equal(memberId, captured.ModifiedBy);
            Assert.NotEqual(default(DateTime), captured.DateCreated);
            Assert.NotNull(captured.DateLastModified);
        }

        private string InvokeGetConfirmationType(int? typeId)
        {
            // Use reflection for testing the private method
            var method = typeof(ClaimVersionService)
                .GetMethod("GetConfirmationType", BindingFlags.NonPublic | BindingFlags.Instance);

            return (string)method.Invoke(_claimVersionService, new object[] { typeId });
        }

        [Fact]
        public void GetConfirmationType_ReturnsYes_WhenTypeIdIs1()
        {
            var result = InvokeGetConfirmationType(1);
            Assert.Equal("Yes", result);
        }

        [Fact]
        public void GetConfirmationType_ReturnsNo_WhenTypeIdIs2()
        {
            var result = InvokeGetConfirmationType(2);
            Assert.Equal("No", result);
        }

        [Fact]
        public void GetConfirmationType_ReturnsNotApplicable_WhenTypeIdIs3()
        {
            var result = InvokeGetConfirmationType(3);
            Assert.Equal("Not applicable", result);
        }

        [Fact]
        public void GetConfirmationType_ReturnsNull_WhenTypeIdIsZero()
        {
            var result = InvokeGetConfirmationType(0);
            Assert.Null(result);
        }

        [Fact]
        public void GetConfirmationType_ReturnsNull_WhenTypeIdIsNull()
        {
            var result = InvokeGetConfirmationType(null);
            Assert.Null(result);
        }

        [Fact]
        public void GetConfirmationType_ReturnsNull_WhenInvalidNumberProvided()
        {
            var result = InvokeGetConfirmationType(999);
            Assert.Null(result);
        }

    }
}
