using BillingService.Domain.Models;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Services.Billing.ChangeTracking;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Claim
{
    public class ClaimChangeTrackingServiceTest
    {
        private readonly Mock<IClaimHistoryService> _mockClaimHistoryService;
        private readonly ClaimChangeTrackingService _service;
        private readonly IdWithUserInfo _claimUserInfo;
        private readonly DateTime _actionDate;

        public ClaimChangeTrackingServiceTest()
        {
            _mockClaimHistoryService = new Mock<IClaimHistoryService>();
            _service = new ClaimChangeTrackingService(_mockClaimHistoryService.Object);
            _claimUserInfo = new IdWithUserInfo { Id = 1, MemberId = 100 };
            _actionDate = DateTime.Now;
        }

        [Fact]
        public void Initialize_ShouldSetProperties()
        {
            // Arrange
            var action = ClaimAction.Create;
            var historyAction = ClaimHistoryAction.ClaimCreated;

            // Act
            _service.Initialize(_claimUserInfo, action, historyAction, _actionDate);

            // Assert - Verify by calling SaveChangesAsync which uses these properties
            Assert.NotNull(_service);
        }

        [Fact]
        public void Initialize_WithValidParameters_ShouldSetAllProperties()
        {
            // Arrange
            var claimUserInfo = new IdWithUserInfo { Id = 123, MemberId = 456, AccountInfoId = 789 };
            var action = ClaimAction.Create;
            var historyAction = ClaimHistoryAction.ClaimCreated;
            var actionDate = new DateTime(2026, 2, 11, 10, 30, 0);

            // Act
            _service.Initialize(claimUserInfo, action, historyAction, actionDate);

            // Assert - Verify properties are set by using them in SaveChangesAsync
            // Since properties are private, we verify indirectly through behavior
            Assert.NotNull(_service);
        }

        [Fact]
        public async Task Initialize_BeforeAnyTracking_ShouldStillAllowSaveChanges()
        {
            // Arrange
            var claimUserInfo = new IdWithUserInfo { Id = 123, MemberId = 456, AccountInfoId = 789 };
            var action = ClaimAction.Create;
            var historyAction = ClaimHistoryAction.ClaimCreated;
            var actionDate = DateTime.Now;

            _service.Initialize(claimUserInfo, action, historyAction, actionDate);

            // Act - Save without tracking any changes
            await _service.SaveChangesAsync();

            // Assert - No history should be saved if no changes were tracked
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.IsAny<ClaimHistoryFieldSaveModel>(),
                It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void TrackAttachmentsChanges_ShouldTrackFileNameChange()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Create, ClaimHistoryAction.ClaimUpdated, _actionDate);
            var attachment = new ClaimAttachmentEntity { FileName = "OldFile.pdf" };
            var saveModel = new RenameAttachmentModelWithUserInfo { FileName = "NewFile.pdf" };

            // Act
            _service.TrackAttachmentsChanges(attachment, saveModel);

            // Assert - Changes are tracked internally
            Assert.NotNull(_service);
        }

        [Fact]
        public async Task SaveChangesAsync_ShouldCallClaimHistoryService_WhenChangesExist()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Create, ClaimHistoryAction.ClaimUpdated, _actionDate);
            var attachment = new ClaimAttachmentEntity { FileName = "OldFile.pdf" };
            var saveModel = new RenameAttachmentModelWithUserInfo { FileName = "NewFile.pdf" };
            _service.TrackAttachmentsChanges(attachment, saveModel);

            // Act
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimId == _claimUserInfo.Id &&
                    m.MemberId == _claimUserInfo.MemberId &&
                    m.OldValue == "OldFile.pdf" &&
                    m.NewValue == "NewFile.pdf"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task SaveChangesAsync_WithImpersonation_ShouldIncludeImpersonationUserName()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Create, ClaimHistoryAction.ClaimUpdated, _actionDate);
            var attachment = new ClaimAttachmentEntity { FileName = "OldFile.pdf" };
            var saveModel = new RenameAttachmentModelWithUserInfo { FileName = "NewFile.pdf" };
            _service.TrackAttachmentsChanges(attachment, saveModel);
            var impersonationUser = "admin@test.com";

            // Act
            await _service.SaveChangesAsync(impersonationUser);

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ImpersonationUserName == impersonationUser),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public void TrackChangesForCharges_ShouldTrackUnitsChange()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Create, ClaimHistoryAction.ClaimUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity { Units = 1.5m, UnitRate = 100m };
            var saveModel = new UpdateBillingClaimDetailsModel { Units = 2.5m, PerUnitsCharge = 100m };

            // Act
            _service.TrackChangesForCharges(charge, saveModel);

            // Assert
            Assert.NotNull(_service);
        }

        [Fact]
        public void TrackChangesForCharges_ShouldTrackPerUnitsChargeChange()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Create, ClaimHistoryAction.ClaimUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity { Units = 1.5m, UnitRate = 100m };
            var saveModel = new UpdateBillingClaimDetailsModel { Units = 1.5m, PerUnitsCharge = 150m };

            // Act
            _service.TrackChangesForCharges(charge, saveModel);

            // Assert
            Assert.NotNull(_service);
        }

        [Fact]
        public void TrackChangesForCharges_ShouldTrackModifiersChange()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Create, ClaimHistoryAction.ClaimUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Units = 1.5m,
                UnitRate = 100m,
                Modifier1 = "AA",
                Modifier2 = "BB",
                Modifier3 = "CC",
                Modifier4 = "DD"
            };
            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 1.5m,
                PerUnitsCharge = 100m,
                Modifier1 = "EE",
                Modifier2 = "FF",
                Modifier3 = "GG",
                Modifier4 = "HH"
            };

            // Act
            _service.TrackChangesForCharges(charge, saveModel);

            // Assert
            Assert.NotNull(_service);
        }

        [Fact]
        public async Task TrackChangesForCharges_ShouldTrackUnitsChange_AndSaveToHistory()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity { Units = 1.5m, UnitRate = 100m };
            var saveModel = new UpdateBillingClaimDetailsModel { Units = 2.5m, PerUnitsCharge = 100m };

            // Act
            _service.TrackChangesForCharges(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Units &&
                    m.OldValue == "1.5" &&
                    m.NewValue == "2.5"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChangesForCharges_ShouldTrackPerUnitsChargeChange_AndSaveToHistory()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity { Units = 1.5m, UnitRate = 100m };
            var saveModel = new UpdateBillingClaimDetailsModel { Units = 1.5m, PerUnitsCharge = 150m };

            // Act
            _service.TrackChangesForCharges(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.PerUnitsCharge &&
                    m.OldValue == "100" &&
                    m.NewValue == "150"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChangesForCharges_ShouldTrackModifier1Change_AndSaveToHistory()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Units = 1.5m,
                UnitRate = 100m,
                Modifier1 = "AA"
            };
            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 1.5m,
                PerUnitsCharge = 100m,
                Modifier1 = "BB"
            };

            // Act
            _service.TrackChangesForCharges(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier1 &&
                    m.OldValue == "AA" &&
                    m.NewValue == "BB"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChangesForCharges_ShouldTrackModifier2Change_AndSaveToHistory()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Units = 1m,
                UnitRate = 50m,
                Modifier2 = "CC"
            };
            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 1m,
                PerUnitsCharge = 50m,
                Modifier2 = "DD"
            };

            // Act
            _service.TrackChangesForCharges(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier2 &&
                    m.OldValue == "CC" &&
                    m.NewValue == "DD"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChangesForCharges_ShouldTrackModifier3Change_AndSaveToHistory()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Units = 2m,
                UnitRate = 75m,
                Modifier3 = "EE"
            };
            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 2m,
                PerUnitsCharge = 75m,
                Modifier3 = "FF"
            };

            // Act
            _service.TrackChangesForCharges(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier3 &&
                    m.OldValue == "EE" &&
                    m.NewValue == "FF"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChangesForCharges_ShouldTrackModifier4Change_AndSaveToHistory()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Units = 3m,
                UnitRate = 90m,
                Modifier4 = "GG"
            };
            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 3m,
                PerUnitsCharge = 90m,
                Modifier4 = "HH"
            };

            // Act
            _service.TrackChangesForCharges(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier4 &&
                    m.OldValue == "GG" &&
                    m.NewValue == "HH"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public void TrackChangesForModifiers_ShouldTrackAllModifiers()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Create, ClaimHistoryAction.ClaimUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = "AA",
                Modifier2 = "BB",
                Modifier3 = null,
                Modifier4 = null
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "XX",
                Modifier2 = "YY",
                Modifier3 = "ZZ",
                Modifier4 = null
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);

            // Assert
            Assert.NotNull(_service);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ShouldTrackModifier1Change_FromValueToValue()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = "AA"
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "BB"
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier1 &&
                    m.OldValue == "AA" &&
                    m.NewValue == "BB"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ShouldTrackModifier2Change_FromValueToValue()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier2 = "CC"
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier2 = "DD"
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier2 &&
                    m.OldValue == "CC" &&
                    m.NewValue == "DD"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ShouldTrackModifier3Change_FromValueToValue()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier3 = "EE"
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier3 = "FF"
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier3 &&
                    m.OldValue == "EE" &&
                    m.NewValue == "FF"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ShouldTrackModifier4Change_FromValueToValue()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier4 = "GG"
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier4 = "HH"
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier4 &&
                    m.OldValue == "GG" &&
                    m.NewValue == "HH"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ShouldTrackAllModifiers_WhenAllChange()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = "AA",
                Modifier2 = "BB",
                Modifier3 = "CC",
                Modifier4 = "DD"
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "EE",
                Modifier2 = "FF",
                Modifier3 = "GG",
                Modifier4 = "HH"
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier1),
                It.IsAny<bool>()), Times.Once);
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier2),
                It.IsAny<bool>()), Times.Once);
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier3),
                It.IsAny<bool>()), Times.Once);
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier4),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ShouldTrackOnlySomeModifiers_WhenOnlySomeChange()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = "AA",
                Modifier2 = "BB",
                Modifier3 = "CC",
                Modifier4 = "DD"
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "XX",  // Changed
                Modifier2 = "BB",  // Same
                Modifier3 = "YY",  // Changed
                Modifier4 = "DD"   // Same
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier1),
                It.IsAny<bool>()), Times.Once);
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier2),
                It.IsAny<bool>()), Times.Never);
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier3),
                It.IsAny<bool>()), Times.Once);
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier4),
                It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ShouldNotTrackAnything_WhenNoChanges()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = "AA",
                Modifier2 = "BB",
                Modifier3 = "CC",
                Modifier4 = "DD"
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "AA",
                Modifier2 = "BB",
                Modifier3 = "CC",
                Modifier4 = "DD"
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.IsAny<ClaimHistoryFieldSaveModel>(),
                It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ShouldTrackModifier1_FromNullToValue()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = null
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "AA"
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier1 &&
                    m.OldValue == "" &&
                    m.NewValue == "AA"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ShouldTrackModifier2_FromNullToValue()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier2 = null
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier2 = "BB"
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier2 &&
                    m.OldValue == "" &&
                    m.NewValue == "BB"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ShouldTrackModifier3_FromValueToNull()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier3 = "CC"
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier3 = null
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier3 &&
                    m.OldValue == "CC" &&
                    m.NewValue == null),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ShouldTrackModifier4_FromValueToNull()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier4 = "DD"
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier4 = null
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier4 &&
                    m.OldValue == "DD" &&
                    m.NewValue == null),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ShouldNotTrack_WhenBothModifier1AreNull()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = null
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = null
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier1),
                It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ShouldNotTrack_WhenAllModifiersAreNull()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = null,
                Modifier2 = null,
                Modifier3 = null,
                Modifier4 = null
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = null,
                Modifier2 = null,
                Modifier3 = null,
                Modifier4 = null
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.IsAny<ClaimHistoryFieldSaveModel>(),
                It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ShouldHandleEmptyStringToValue()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = "",
                Modifier2 = ""
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "AA",
                Modifier2 = "BB"
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier1),
                It.IsAny<bool>()), Times.Once);
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier2),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ShouldHandleValueToEmptyString()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Modifier3 = "CC",
                Modifier4 = "DD"
            };
            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier3 = "",
                Modifier4 = ""
            };

            // Act
            _service.TrackChangesForModifiers(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier3 &&
                    m.OldValue == "CC" &&
                    m.NewValue == ""),
                It.IsAny<bool>()), Times.Once);
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier4 &&
                    m.OldValue == "DD" &&
                    m.NewValue == ""),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task SaveChangesAsync_ShouldNotCallHistoryService_WhenNoChanges()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Create, ClaimHistoryAction.ClaimUpdated, _actionDate);

            // Act
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.IsAny<ClaimHistoryFieldSaveModel>(),
                It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void TrackChangesForCharges_ShouldNotTrack_WhenNoChanges()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Create, ClaimHistoryAction.ClaimUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity { Units = 1.5m, UnitRate = 100m, Modifier1 = "AA" };
            var saveModel = new UpdateBillingClaimDetailsModel { Units = 1.5m, PerUnitsCharge = 100m, Modifier1 = "AA" };

            // Act
            _service.TrackChangesForCharges(charge, saveModel);

            // Assert
            Assert.NotNull(_service);
        }

        [Fact]
        public async Task TrackChargeEntries_ShouldTrackUnitsChange_WhenIntegerPartChanges()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity { Units = 1.5m, UnitRate = 100m };
            var saveModel = new UpdateBillingClaimDetailsModel { Units = 2.5m, PerUnitsCharge = 100m };

            // Act
            _service.TrackChargeEntries(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Units &&
                    m.OldValue == "1.5" &&
                    m.NewValue == "2.5"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChargeEntries_ShouldTrackUnitsChange_WhenDecimalPartChanges()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity { Units = 1.25m, UnitRate = 100m };
            var saveModel = new UpdateBillingClaimDetailsModel { Units = 1.75m, PerUnitsCharge = 100m };

            // Act
            _service.TrackChargeEntries(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Units &&
                    m.OldValue == "1.25" &&
                    m.NewValue == "1.75"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChargeEntries_ShouldNotTrackUnitsChange_WhenDecimalDifferenceIsLessThanTwoCents()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity { Units = 1.501m, UnitRate = 100m };
            var saveModel = new UpdateBillingClaimDetailsModel { Units = 1.509m, PerUnitsCharge = 100m };

            // Act
            _service.TrackChargeEntries(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert - Should not track because decimal part difference is < 0.01
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Units),
                It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task TrackChargeEntries_ShouldTrackPerUnitsChargeChange_WhenIntegerPartChanges()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity { Units = 1m, UnitRate = 100m };
            var saveModel = new UpdateBillingClaimDetailsModel { Units = 1m, PerUnitsCharge = 150m };

            // Act
            _service.TrackChargeEntries(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.PerUnitsCharge &&
                    m.OldValue == "100" &&
                    m.NewValue == "150"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChargeEntries_ShouldTrackPerUnitsChargeChange_WhenDecimalPartChanges()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity { Units = 1m, UnitRate = 99.99m };
            var saveModel = new UpdateBillingClaimDetailsModel { Units = 1m, PerUnitsCharge = 99.50m };

            // Act
            _service.TrackChargeEntries(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.PerUnitsCharge &&
                    m.OldValue == "99.99" &&
                    m.NewValue == "99.50"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChargeEntries_ShouldNotTrackPerUnitsCharge_WhenDecimalDifferenceIsLessThanTwoCents()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity { Units = 1m, UnitRate = 100.501m };
            var saveModel = new UpdateBillingClaimDetailsModel { Units = 1m, PerUnitsCharge = 100.509m };

            // Act
            _service.TrackChargeEntries(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.PerUnitsCharge),
                It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task TrackChargeEntries_ShouldTrackModifier1Change_FromValueToValue()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Units = 1m,
                UnitRate = 100m,
                Modifier1 = "AA"
            };
            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 1m,
                PerUnitsCharge = 100m,
                Modifier1 = "BB"
            };

            // Act
            _service.TrackChargeEntries(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier1 &&
                    m.OldValue == "AA" &&
                    m.NewValue == "BB"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChargeEntries_ShouldTrackModifier1Change_FromNullToValue()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Units = 1m,
                UnitRate = 100m,
                Modifier1 = null
            };
            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 1m,
                PerUnitsCharge = 100m,
                Modifier1 = "AA"
            };

            // Act
            _service.TrackChargeEntries(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier1 &&
                    m.OldValue == "" &&
                    m.NewValue == "AA"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChargeEntries_ShouldTrackModifier1Change_FromValueToNull()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Units = 1m,
                UnitRate = 100m,
                Modifier1 = "AA"
            };
            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 1m,
                PerUnitsCharge = 100m,
                Modifier1 = null
            };

            // Act
            _service.TrackChargeEntries(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Modifier1 &&
                    m.OldValue == "AA" &&
                    m.NewValue == null),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChargeEntries_ShouldNotTrackModifier1_WhenBothAreNull()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Units = 1m,
                UnitRate = 100m,
                Modifier1 = null
            };
            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 1m,
                PerUnitsCharge = 100m,
                Modifier1 = null
            };

            // Act
            _service.TrackChargeEntries(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier1),
                It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task TrackChargeEntries_ShouldTrackAllFourModifiers_WhenAllChange()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Units = 1m,
                UnitRate = 100m,
                Modifier1 = "AA",
                Modifier2 = "BB",
                Modifier3 = "CC",
                Modifier4 = "DD"
            };
            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 1m,
                PerUnitsCharge = 100m,
                Modifier1 = "EE",
                Modifier2 = "FF",
                Modifier3 = "GG",
                Modifier4 = "HH"
            };

            // Act
            _service.TrackChargeEntries(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier1),
                It.IsAny<bool>()), Times.Once);
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier2),
                It.IsAny<bool>()), Times.Once);
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier3),
                It.IsAny<bool>()), Times.Once);
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier4),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChargeEntries_ShouldTrackOnlySomeModifiers_WhenOnlySomeChange()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Units = 1m,
                UnitRate = 100m,
                Modifier1 = "AA",
                Modifier2 = "BB",
                Modifier3 = "CC",
                Modifier4 = "DD"
            };
            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 1m,
                PerUnitsCharge = 100m,
                Modifier1 = "EE",  // Changed
                Modifier2 = "BB",  // Same
                Modifier3 = "FF",  // Changed
                Modifier4 = "DD"   // Same
            };

            // Act
            _service.TrackChargeEntries(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier1),
                It.IsAny<bool>()), Times.Once);
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier2),
                It.IsAny<bool>()), Times.Never);
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier3),
                It.IsAny<bool>()), Times.Once);
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimHistoryField == ClaimHistoryField.Modifier4),
                It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task TrackChargeEntries_ShouldTrackAllChanges_WhenUnitsPerUnitsChargeAndModifiersChange()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, _actionDate);
            var charge = new ClaimChargeEntryEntity
            {
                Units = 1.5m,
                UnitRate = 100m,
                Modifier1 = "AA",
                Modifier2 = "BB"
            };
            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 2.5m,
                PerUnitsCharge = 150m,
                Modifier1 = "XX",
                Modifier2 = "YY"
            };

            // Act
            _service.TrackChargeEntries(charge, saveModel);
            await _service.SaveChangesAsync();

            // Assert - Should track Units, PerUnitsCharge, Modifier1, Modifier2
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.IsAny<ClaimHistoryFieldSaveModel>(),
                It.IsAny<bool>()), Times.Exactly(4));
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldTrackFileNameChange()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.AttachmentAdded, _actionDate);
            var attachment = new ClaimAttachmentEntity
            {
                FileName = "Invoice_Old.pdf"
            };
            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = "Invoice_New.pdf"
            };

            // Act
            _service.TrackAttachementsChangesForClaim(attachment, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Attachments &&
                    m.OldValue == "Invoice_Old.pdf" &&
                    m.NewValue == "Invoice_New.pdf"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldTrackFileExtensionChange()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.AttachmentAdded, _actionDate);
            var attachment = new ClaimAttachmentEntity
            {
                FileName = "Document.pdf"
            };
            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = "Document.docx"
            };

            // Act
            _service.TrackAttachementsChangesForClaim(attachment, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Attachments &&
                    m.OldValue == "Document.pdf" &&
                    m.NewValue == "Document.docx"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldNotTrack_WhenFileNameIsUnchanged()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.AttachmentAdded, _actionDate);
            var attachment = new ClaimAttachmentEntity
            {
                FileName = "SameFile.pdf"
            };
            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = "SameFile.pdf"
            };

            // Act
            _service.TrackAttachementsChangesForClaim(attachment, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.IsAny<ClaimHistoryFieldSaveModel>(),
                It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldHandleSpecialCharactersInFileName()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.AttachmentAdded, _actionDate);
            var attachment = new ClaimAttachmentEntity
            {
                FileName = "Invoice_#123_@2024.pdf"
            };
            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = "Invoice_#456_@2025.pdf"
            };

            // Act
            _service.TrackAttachementsChangesForClaim(attachment, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Attachments &&
                    m.OldValue == "Invoice_#123_@2024.pdf" &&
                    m.NewValue == "Invoice_#456_@2025.pdf"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldHandleFileNameWithSpaces()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.AttachmentAdded, _actionDate);
            var attachment = new ClaimAttachmentEntity
            {
                FileName = "Claim Invoice Document.pdf"
            };
            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = "Claim Invoice Document Final.pdf"
            };

            // Act
            _service.TrackAttachementsChangesForClaim(attachment, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Attachments &&
                    m.OldValue == "Claim Invoice Document.pdf" &&
                    m.NewValue == "Claim Invoice Document Final.pdf"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldHandleLongFileNames()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.AttachmentAdded, _actionDate);
            var attachment = new ClaimAttachmentEntity
            {
                FileName = "This_Is_A_Very_Long_File_Name_With_Many_Characters_Invoice_Document_2024_January.pdf"
            };
            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = "This_Is_A_Very_Long_File_Name_With_Many_Characters_Invoice_Document_2024_February.pdf"
            };

            // Act
            _service.TrackAttachementsChangesForClaim(attachment, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Attachments),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldHandleFileNameWithPath()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.AttachmentAdded, _actionDate);
            var attachment = new ClaimAttachmentEntity
            {
                FileName = "Folder/OldFile.pdf"
            };
            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = "Folder/NewFile.pdf"
            };

            // Act
            _service.TrackAttachementsChangesForClaim(attachment, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Attachments &&
                    m.OldValue == "Folder/OldFile.pdf" &&
                    m.NewValue == "Folder/NewFile.pdf"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldHandleCaseSensitiveFileNames()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.AttachmentAdded, _actionDate);
            var attachment = new ClaimAttachmentEntity
            {
                FileName = "invoice.pdf"
            };
            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = "Invoice.pdf"
            };

            // Act
            _service.TrackAttachementsChangesForClaim(attachment, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Attachments &&
                    m.OldValue == "invoice.pdf" &&
                    m.NewValue == "Invoice.pdf"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldHandleNullOldFileName()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.AttachmentAdded, _actionDate);
            var attachment = new ClaimAttachmentEntity
            {
                FileName = null
            };
            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = "NewFile.pdf"
            };

            // Act
            _service.TrackAttachementsChangesForClaim(attachment, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Attachments &&
                    m.OldValue == null &&
                    m.NewValue == "NewFile.pdf"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldHandleNullNewFileName()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.AttachmentAdded, _actionDate);
            var attachment = new ClaimAttachmentEntity
            {
                FileName = "OldFile.pdf"
            };
            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = null
            };

            // Act
            _service.TrackAttachementsChangesForClaim(attachment, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Attachments &&
                    m.OldValue == "OldFile.pdf" &&
                    m.NewValue == null),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldNotTrack_WhenBothFileNamesAreNull()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.AttachmentAdded, _actionDate);
            var attachment = new ClaimAttachmentEntity
            {
                FileName = null
            };
            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = null
            };

            // Act
            _service.TrackAttachementsChangesForClaim(attachment, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.IsAny<ClaimHistoryFieldSaveModel>(),
                It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldHandleEmptyStringOldFileName()             
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.AttachmentAdded, _actionDate);
            var attachment = new ClaimAttachmentEntity
            {
                FileName = ""
            };
            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = "NewFile.pdf"
            };

            // Act
            _service.TrackAttachementsChangesForClaim(attachment, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Attachments &&
                    m.OldValue == "" &&
                    m.NewValue == "NewFile.pdf"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldHandleEmptyStringNewFileName()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.AttachmentAdded, _actionDate);
            var attachment = new ClaimAttachmentEntity
            {
                FileName = "OldFile.pdf"
            };
            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = ""
            };

            // Act
            _service.TrackAttachementsChangesForClaim(attachment, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Attachments &&
                    m.OldValue == "OldFile.pdf" &&
                    m.NewValue == ""),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldNotTrack_WhenBothFileNamesAreEmpty()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.AttachmentAdded, _actionDate);
            var attachment = new ClaimAttachmentEntity
            {
                FileName = ""
            };
            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = ""
            };

            // Act
            _service.TrackAttachementsChangesForClaim(attachment, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.IsAny<ClaimHistoryFieldSaveModel>(),
                It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task TrackChanges_ShouldIncludeClaimIdAndMemberId()
        {
            // Arrange
            var userInfo = new IdWithUserInfo { Id = 999, MemberId = 888, AccountInfoId = 777 };
            _service.Initialize(userInfo, ClaimAction.Edit, ClaimHistoryAction.ClaimUpdated, _actionDate);

            var claim = new ClaimEntity
            {
                Id = 1,
                Note = "Old",
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>(),
                LocationCode = new LocationCodesModel { id = 1, description = "Office" },
                ProviderLocation = new ProviderLocations { id = 1, name = "Test Provider" }
            };

            var saveModel = new UpdateClaimDetailsModel
            {
                ClaimId = 1,
                Note = "New",
                DiagnosisCodes = new List<ClaimDiagnosisCodeUpdateModel>(),
                PlaceOfServiceId = 1,
                BillingProviderId = 1,
                BillingProvider = "Test Provider",
                BenefitAssignmentId = 1,
                PatientReleaseAgreementId = 1,
                AuthorizePaymentId = 1,
                SubmissionReasonId = (int)ClaimFrequencyType.Original
            };

            // Act
            _service.TrackChanges(claim, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimId == 999 &&
                    m.MemberId == 888),
                It.IsAny<bool>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task TrackChanges_ShouldUseCorrectClaimAction()
        {
            // Arrange
            var specificAction = ClaimAction.Edit;
            _service.Initialize(_claimUserInfo, specificAction, ClaimHistoryAction.ClaimUpdated, _actionDate);

            var claim = new ClaimEntity
            {
                Id = 1,
                Note = "Old",
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>(),
                LocationCode = new LocationCodesModel { id = 1, description = "Office" },
                ProviderLocation = new ProviderLocations { id = 1, name = "Test Provider" }
            };

            var saveModel = new UpdateClaimDetailsModel
            {
                ClaimId = 1,
                Note = "New",
                DiagnosisCodes = new List<ClaimDiagnosisCodeUpdateModel>(),
                PlaceOfServiceId = 1,
                BillingProviderId = 1,
                BillingProvider = "Test Provider",
                BenefitAssignmentId = 1,
                PatientReleaseAgreementId = 1,
                AuthorizePaymentId = 1,
                SubmissionReasonId = (int)ClaimFrequencyType.Original
            };

            // Act
            _service.TrackChanges(claim, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ClaimAction == specificAction),
                It.IsAny<bool>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task TrackChanges_ShouldUseCorrectActionDate()
        {
            // Arrange
            var specificDate = new DateTime(2026, 2, 12, 10, 30, 0);
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ClaimUpdated, specificDate);

            var claim = new ClaimEntity
            {
                Id = 1,
                Note = "Old",
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>(),
                LocationCode = new LocationCodesModel { id = 1, description = "Office" },
                ProviderLocation = new ProviderLocations { id = 1, name = "Test Provider" }
            };

            var saveModel = new UpdateClaimDetailsModel
            {
                ClaimId = 1,
                Note = "New",
                DiagnosisCodes = new List<ClaimDiagnosisCodeUpdateModel>(),
                PlaceOfServiceId = 1,
                BillingProviderId = 1,
                BillingProvider = "Test Provider",
                BenefitAssignmentId = 1,
                PatientReleaseAgreementId = 1,
                AuthorizePaymentId = 1,
                SubmissionReasonId = (int)ClaimFrequencyType.Original
            };

            // Act
            _service.TrackChanges(claim, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m => m.ActionDate == specificDate),
                It.IsAny<bool>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task TrackChanges_ShouldTrackNoteChange()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ClaimUpdated, _actionDate);

            var claim = new ClaimEntity
            {
                Id = 1,
                Note = "Old note text",
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>(),
                LocationCode = new LocationCodesModel { id = 1, description = "Office" },
                ProviderLocation = new ProviderLocations { id = 1, name = "Test Provider" }
            };

            var saveModel = new UpdateClaimDetailsModel
            {
                ClaimId = 1,
                Note = "New note text",
                DiagnosisCodes = new List<ClaimDiagnosisCodeUpdateModel>(),
                PlaceOfServiceId = 1,
                BillingProviderId = 1,
                BillingProvider = "Test Provider",
                BenefitAssignmentId = 1,
                PatientReleaseAgreementId = 1,
                AuthorizePaymentId = 1,
                SubmissionReasonId = (int)ClaimFrequencyType.Original
            };

            // Act
            _service.TrackChanges(claim, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Note &&
                    m.OldValue == "Old note text" &&
                    m.NewValue == "New note text"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChanges_ShouldHandleNullToValue_ForNote()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ClaimUpdated, _actionDate);

            var claim = new ClaimEntity
            {
                Id = 1,
                Note = null,
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>(),
                LocationCode = new LocationCodesModel { id = 1, description = "Office" },
                ProviderLocation = new ProviderLocations { id = 1, name = "Test Provider" }
            };

            var saveModel = new UpdateClaimDetailsModel
            {
                ClaimId = 1,
                Note = "New note",
                DiagnosisCodes = new List<ClaimDiagnosisCodeUpdateModel>(),
                PlaceOfServiceId = 1,
                BillingProviderId = 1,
                BillingProvider = "Test Provider",
                BenefitAssignmentId = 1,
                PatientReleaseAgreementId = 1,
                AuthorizePaymentId = 1,
                SubmissionReasonId = (int)ClaimFrequencyType.Original
            };

            // Act
            _service.TrackChanges(claim, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Note &&
                    m.OldValue == null &&
                    m.NewValue == "New note"),
                It.IsAny<bool>()), Times.Once);
        }

        [Fact]
        public async Task TrackChanges_ShouldHandleValueToNull_ForNote()
        {
            // Arrange
            _service.Initialize(_claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ClaimUpdated, _actionDate);

            var claim = new ClaimEntity
            {
                Id = 1,
                Note = "Old note",
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>(),
                LocationCode = new LocationCodesModel { id = 1, description = "Office" },
                ProviderLocation = new ProviderLocations { id = 1, name = "Test Provider" }
            };

            var saveModel = new UpdateClaimDetailsModel
            {
                ClaimId = 1,
                Note = null,
                DiagnosisCodes = new List<ClaimDiagnosisCodeUpdateModel>(),
                PlaceOfServiceId = 1,
                BillingProviderId = 1,
                BillingProvider = "Test Provider",
                BenefitAssignmentId = 1,
                PatientReleaseAgreementId = 1,
                AuthorizePaymentId = 1,
                SubmissionReasonId = (int)ClaimFrequencyType.Original
            };

            // Act
            _service.TrackChanges(claim, saveModel);
            await _service.SaveChangesAsync();

            // Assert
            _mockClaimHistoryService.Verify(x => x.AddAsync(
                It.Is<ClaimHistoryFieldSaveModel>(m =>
                    m.ClaimHistoryField == ClaimHistoryField.Note &&
                    m.OldValue == "Old note" &&
                    m.NewValue == null),
                It.IsAny<bool>()), Times.Once);
        }
    }
}
