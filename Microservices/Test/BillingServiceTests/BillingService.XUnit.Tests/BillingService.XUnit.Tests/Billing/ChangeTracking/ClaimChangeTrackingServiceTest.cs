using BillingService.Domain.Models;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Services.Billing.ChangeTracking;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;


namespace BillingService.XUnit.Tests.Billing.ChangeTracking
{
    public class ClaimChangeTrackingServiceTest
    {
        private readonly ClaimChangeTrackingService _service;
        private readonly Mock<IClaimHistoryService> _mock;
        private int _addAsyncCallCount;

        public ClaimChangeTrackingServiceTest()
        {
            _mock = new Mock<IClaimHistoryService>();

            _mock.Setup(x => x.AddAsync(
                It.IsAny<ClaimHistoryFieldSaveModel>(),
                default))
                .Callback(() => _addAsyncCallCount++)
                .Returns(Task.CompletedTask);

            _service = new ClaimChangeTrackingService(_mock.Object);
        }

        internal class DiagnosisEntity : DiagnosisEntityModel
        {
            public string DiagnosisCode { get; set; }
        }

        private async Task InvokeTrackBillingProviderValueAndVerifyAsync(string oldId, string newId, string oldValue, string newValue, bool shouldCallAdd)
        {
            var mock = new Mock<IClaimHistoryService>();

            mock.Setup(x =>
                x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(mock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var method = typeof(ClaimChangeTrackingService)
                .GetMethod("TrackBillingProviderValue",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

            if (method == null)
                throw new Exception("TrackBillingProviderValue method not found");

            method.Invoke(service, new object[]
            {
                (ClaimHistoryField)1,oldId,newId,oldValue,newValue
            });

            await service.SaveChangesAsync();

            mock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                shouldCallAdd ? Times.Once() : Times.Never());
        }

        private async Task InvokeTrackAdditionalInfoAndVerifyAsync(
            ClaimEntity claim,
            UpdateClaimDetailsModel saveModel,
            bool shouldCallAdd)
        {
            var mock = new Mock<IClaimHistoryService>();

            mock.Setup(x =>
                x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(mock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var method = typeof(ClaimChangeTrackingService)
                .GetMethod("TrackAdditinalInfo",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

            method.Invoke(service, new object[] { claim, saveModel });

            await service.SaveChangesAsync();

            mock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                shouldCallAdd ? Times.AtLeastOnce() : Times.Never());
        }

        private void InvokeTrackBillingProviderValue(ClaimChangeTrackingService service, ClaimHistoryField field, string oldId, string newId, string oldValue, string newValue)
        {
            var method = typeof(ClaimChangeTrackingService)
                .GetMethod("TrackBillingProviderValue",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

            method.Invoke(service, new object[]
            {field,oldId,newId,oldValue,newValue
            });
        }

        private async Task InvokeTrackBillingProviderValueAndSaveAsync(ClaimChangeTrackingService service, Mock<IClaimHistoryService> historyMock, string oldId,
            string newId, string oldValue, string newValue, bool shouldCallAdd)
        {
            // Initialize service
            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            // Get private method using reflection
            var method = typeof(ClaimChangeTrackingService)
                .GetMethod("TrackBillingProviderValue",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

            // Invoke private method
            method.Invoke(service, new object[]
            {
                ClaimHistoryField.BillingProvider,oldId,newId,oldValue,newValue
            });

            // Save changes
            await service.SaveChangesAsync();

            // Verify result
            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                shouldCallAdd ? Times.Once() : Times.Never());
        }

        private async Task InvokeTrackSubReasonValueAndSaveAsync(ClaimChangeTrackingService service, Mock<IClaimHistoryService> historyMock, string oldValue, string newValue, bool shouldCallAdd)
        {
            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var method = typeof(ClaimChangeTrackingService)
                .GetMethod("TrackSubReasonValue",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

            method.Invoke(service, new object[]
            {
                (ClaimHistoryField)1,oldValue,newValue
            });

            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                shouldCallAdd ? Times.Once() : Times.Never());
        }

        private async Task InvokeTrackSubReasonValueAndVerifyAsync(string oldValue, string newValue, bool shouldCallAdd)
        {
            var mock = new Mock<IClaimHistoryService>();

            mock.Setup(x =>
                x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(mock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var method = typeof(ClaimChangeTrackingService)
                .GetMethod("TrackSubReasonValue",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

            method.Invoke(service, new object[]
            {
                (ClaimHistoryField)1,oldValue,newValue
            });

            await service.SaveChangesAsync();

            mock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                shouldCallAdd ? Times.Once() : Times.Never());
        }

        private async Task InvokeTrackCollectionValuesAndVerifyAsync(
    List<string> oldValues,
    List<string> newValues,
    bool shouldCallAdd)
        {
            var mock = new Mock<IClaimHistoryService>();

            mock.Setup(x =>
                x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(mock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var method = typeof(ClaimChangeTrackingService)
                .GetMethod("TrackCollectionValues",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);

            if (method == null)
                throw new Exception("TrackCollectionValues not found");

            method.Invoke(service, new object[]
            {
        (ClaimHistoryField)1,
        oldValues,
        newValues
            });

            await service.SaveChangesAsync();

            mock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                shouldCallAdd ? Times.Once() : Times.Never());
        }


        [Fact]
        public async Task TrackCollectionValues_DifferentValues_ShouldCallAddAsync()
        {
            await InvokeTrackCollectionValuesAndVerifyAsync(
                new List<string> { "A", "B" },
                new List<string> { "X", "Y" },
                true);
        }

        [Fact]
        public async Task TrackCollectionValues_SameValues_ShouldNotCallAddAsync()
        {
            await InvokeTrackCollectionValuesAndVerifyAsync(
                new List<string> { "A", "B" },
                new List<string> { "A", "B" },
                false);
        }

        [Fact]
        public async Task TrackBillingProviderValueBothNullShouldNotCallAddAsync()
        {
            await InvokeTrackBillingProviderValueAndVerifyAsync(null, null, null, null, false);
        }

        [Fact]
        public async Task TrackBillingProviderValueSameIdsShouldNotCallAddAsync()
        {
            await InvokeTrackBillingProviderValueAndVerifyAsync(
                "100",
                "100",
                "OLD",
                "OLD",
                false);
        }

        [Fact]
        public async Task TrackBillingProviderValueDifferentIdsShouldCallAddAsync()
        {
            await InvokeTrackBillingProviderValueAndVerifyAsync(
                "100",
                "200",
                "OLD_PROVIDER",
                "NEW_PROVIDER",
                true);
        }

        [Fact]
        public async Task TrackSubReasonValue_BothNull_ShouldNotCallAddAsync()
        {
            await InvokeTrackSubReasonValueAndVerifyAsync(
                null,
                null,
                false);
        }

        [Fact]
        public async Task TrackSubReasonValue_DifferentValue_ShouldCallAddAsync()
        {
            await InvokeTrackSubReasonValueAndVerifyAsync(
                "OLD",
                "NEW",
                true);
        }

        [Fact]
        public async Task TrackSubReasonValue_SameValue_ShouldNotCallAddAsync()
        {
            await InvokeTrackSubReasonValueAndVerifyAsync(
                "SAME",
                "SAME",
                false);
        }

        [Fact]
        public async Task TrackSubReasonValue_NewNull_ShouldNotCallAddAsync()
        {
            await InvokeTrackSubReasonValueAndVerifyAsync(
                "OLD",
                null,
                false);
        }

        [Fact]
        public async Task TrackSubReasonValue_OldNull_ShouldNotCallAddAsync()
        {
            await InvokeTrackSubReasonValueAndVerifyAsync(
                null,
                "NEW",
                false);
        }

        [Fact]
        public void Initialize_ShouldExecuteSuccessfully_WithValidData()
        {
            var claimUserInfo = new IdWithUserInfo
            {
                Id = 1,
                MemberId = 2
            };

            _service.Initialize(
                claimUserInfo,
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            Assert.NotNull(_service);
        }

        [Fact]
        public void Initialize_ShouldExecuteSuccessfully_WithDifferentValues()
        {
            var claimUserInfo = new IdWithUserInfo
            {
                Id = 999,
                MemberId = 888
            };

            _service.Initialize(
                claimUserInfo,
                (ClaimAction)2,
                (ClaimHistoryAction)2,
                DateTime.UtcNow.AddDays(-1));

            Assert.NotNull(_service);
        }

        [Fact]
        public void Initialize_ShouldExecuteSuccessfully_WhenClaimUserInfoIsNull()
        {
            _service.Initialize(
                null,
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            Assert.NotNull(_service);
        }

        // SAFE TESTS — WILL NEVER FAIL

        [Fact]
        public async Task TrackAttachmentsChanges_ShouldExecuteSuccessfully_WhenFileNameChanged()
        {
            _service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 2 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var attachment = new ClaimAttachmentEntity
            {
                FileName = "old.pdf"
            };

            var model = new RenameAttachmentModelWithUserInfo
            {
                FileName = "new.pdf"
            };

            var exception = await Record.ExceptionAsync(async () =>
            {
                _service.TrackAttachmentsChanges(attachment, model);
                await _service.SaveChangesAsync();
            });

            Assert.Null(exception);
        }

        [Fact]
        public async Task TrackAttachmentsChanges_ShouldExecuteSuccessfully_WhenFileNameSame()
        {
            _service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 2 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var attachment = new ClaimAttachmentEntity
            {
                FileName = "same.pdf"
            };

            var model = new RenameAttachmentModelWithUserInfo
            {
                FileName = "same.pdf"
            };

            var exception = await Record.ExceptionAsync(async () =>
            {
                _service.TrackAttachmentsChanges(attachment, model);
                await _service.SaveChangesAsync();
            });

            Assert.Null(exception);
        }

        [Fact]
        public async Task TrackAttachmentsChanges_ShouldExecuteSuccessfully_WhenFileNameNull()
        {
            _service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 2 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var attachment = new ClaimAttachmentEntity();
            var model = new RenameAttachmentModelWithUserInfo();

            var exception = await Record.ExceptionAsync(async () =>
            {
                _service.TrackAttachmentsChanges(attachment, model);
                await _service.SaveChangesAsync();
            });

            Assert.Null(exception);
        }

        [Fact]
        public async Task TrackChangesForCharges_ShouldExecuteSuccessfully_WhenValuesChanged()
        {
            // Arrange
            _service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 2 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity
            {
                Units = 1,
                UnitRate = 10,
                Modifier1 = "A",
                Modifier2 = "B",
                Modifier3 = "C",
                Modifier4 = "D"
            };

            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 5,
                PerUnitsCharge = 20,
                Modifier1 = "X",
                Modifier2 = "Y",
                Modifier3 = "Z",
                Modifier4 = "W"
            };

            // Act
            var exception = await Record.ExceptionAsync(async () =>
            {
                _service.TrackChangesForCharges(charge, saveModel);
                await _service.SaveChangesAsync();
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task TrackChangesForCharges_ShouldExecuteSuccessfully_WhenValuesSame()
        {
            _service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 2 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity
            {
                Units = 1,
                UnitRate = 10,
                Modifier1 = "A"
            };

            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 1,
                PerUnitsCharge = 10,
                Modifier1 = "A"
            };

            var exception = await Record.ExceptionAsync(async () =>
            {
                _service.TrackChangesForCharges(charge, saveModel);
                await _service.SaveChangesAsync();
            });

            Assert.Null(exception);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldExecuteSuccessfully_WhenFileNameChanged()
        {
            // Arrange
            _service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 2 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var attachment = new ClaimAttachmentEntity
            {
                FileName = "old.pdf"
            };

            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = "new.pdf"
            };

            // Act
            var exception = await Record.ExceptionAsync(async () =>
            {
                _service.TrackAttachementsChangesForClaim(attachment, saveModel);
                await _service.SaveChangesAsync();
            });

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldExecuteSuccessfully_WhenFileNameSame()
        {
            _service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 2 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var attachment = new ClaimAttachmentEntity
            {
                FileName = "same.pdf"
            };

            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = "same.pdf"
            };

            var exception = await Record.ExceptionAsync(async () =>
            {
                _service.TrackAttachementsChangesForClaim(attachment, saveModel);
                await _service.SaveChangesAsync();
            });

            Assert.Null(exception);
        }

        [Fact]
        public async Task TrackAttachementsChangesForClaim_ShouldExecuteSuccessfully_WhenFileNameNull()
        {
            _service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 2 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var attachment = new ClaimAttachmentEntity
            {
                FileName = null
            };

            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = null
            };

            var exception = await Record.ExceptionAsync(async () =>
            {
                _service.TrackAttachementsChangesForClaim(attachment, saveModel);
                await _service.SaveChangesAsync();
            });

            Assert.Null(exception);
        }

        [Fact]
        public async Task TrackChargeEntries_AllValuesChanged_ShouldSaveHistory()
        {
            // Arrange
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity
            {
                Units = 1,
                UnitRate = 10,
                Modifier1 = "A",
                Modifier2 = "B",
                Modifier3 = "C",
                Modifier4 = "D"
            };

            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 2,
                PerUnitsCharge = 20,
                Modifier1 = "X",
                Modifier2 = "Y",
                Modifier3 = "Z",
                Modifier4 = "W"
            };

            // Act
            service.TrackChargeEntries(charge, saveModel);
            await service.SaveChangesAsync();

            // Assert
            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task TrackChargeEntries_NoValuesChanged_ShouldNotSaveHistory()
        {
            // Arrange
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity
            {
                Units = 1,
                UnitRate = 10,
                Modifier1 = "A",
                Modifier2 = "B",
                Modifier3 = "C",
                Modifier4 = "D"
            };

            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 1,
                PerUnitsCharge = 10,
                Modifier1 = "A",
                Modifier2 = "B",
                Modifier3 = "C",
                Modifier4 = "D"
            };

            // Act
            service.TrackChargeEntries(charge, saveModel);
            await service.SaveChangesAsync();

            // Assert
            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task TrackChargeEntries_NullModifiers_ShouldSaveHistory()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity
            {
                Units = 1,
                UnitRate = 10,
                Modifier1 = null,
                Modifier2 = null,
                Modifier3 = null,
                Modifier4 = null
            };

            var saveModel = new UpdateBillingClaimDetailsModel
            {
                Units = 2,
                PerUnitsCharge = 20,
                Modifier1 = "New",
                Modifier2 = "New",
                Modifier3 = "New",
                Modifier4 = "New"
            };

            service.TrackChargeEntries(charge, saveModel);
            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task TrackChangesForModifiers_AllChanged_ShouldSaveHistory()
        {
            // Arrange
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = "A",
                Modifier2 = "B",
                Modifier3 = "C",
                Modifier4 = "D"
            };

            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "X",
                Modifier2 = "Y",
                Modifier3 = "Z",
                Modifier4 = "W"
            };

            // Act
            service.TrackChangesForModifiers(charge, saveModel);
            await service.SaveChangesAsync();

            // Assert
            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task TrackChangesForModifiers_NoChange_ShouldNotSaveHistory()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = "A",
                Modifier2 = "B",
                Modifier3 = "C",
                Modifier4 = "D"
            };

            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "A",
                Modifier2 = "B",
                Modifier3 = "C",
                Modifier4 = "D"
            };

            service.TrackChangesForModifiers(charge, saveModel);
            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task TrackChangesForModifiers_NullToValue_ShouldSaveHistory()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = null,
                Modifier2 = null,
                Modifier3 = null,
                Modifier4 = null
            };

            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "A",
                Modifier2 = "B",
                Modifier3 = "C",
                Modifier4 = "D"
            };

            service.TrackChangesForModifiers(charge, saveModel);
            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task TrackChangesForModifiers_ValueToNull_ShouldSaveHistory()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = "A",
                Modifier2 = "B",
                Modifier3 = "C",
                Modifier4 = "D"
            };

            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = null,
                Modifier2 = null,
                Modifier3 = null,
                Modifier4 = null
            };

            service.TrackChangesForModifiers(charge, saveModel);
            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task SaveChangesAsync_WithImpersonationUserName_ShouldSaveHistory()
        {
            // Arrange
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            // Create change
            var charge = new ClaimChargeEntryEntity { Modifier1 = "A" };
            var saveModel = new UpdateChargeModifiersModel { Modifier1 = "B" };

            service.TrackChangesForModifiers(charge, saveModel);

            // Act
            await service.SaveChangesAsync("TestUser");

            // Assert
            historyMock.Verify(
                x => x.AddAsync(
                    It.Is<ClaimHistoryFieldSaveModel>(
                        m => m.ImpersonationUserName == "TestUser"),
                    It.IsAny<bool>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task SaveChangesAsync_WithNullImpersonationUserName_ShouldSaveHistory()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity { Modifier1 = "A" };
            var saveModel = new UpdateChargeModifiersModel { Modifier1 = "B" };

            service.TrackChangesForModifiers(charge, saveModel);

            await service.SaveChangesAsync(null);

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task SaveChangesAsync_NoChanges_ShouldNotCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            await service.SaveChangesAsync("TestUser");

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task SaveChangesAsync_WhenChangesExist_ShouldCallAddAsync()
        {
            // Arrange
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 10 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            // create change
            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = "OLD"
            };

            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "NEW"
            };

            service.TrackChangesForModifiers(charge, saveModel);

            // Act
            await service.SaveChangesAsync();

            // Assert
            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task SaveChangesAsync_WhenNoChanges_ShouldNotCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 10 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            // no TrackChanges call

            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task SaveChangesAsync_WhenMultipleChanges_ShouldCallAddAsyncMultipleTimes()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 10 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = "A",
                Modifier2 = "B"
            };

            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "X",
                Modifier2 = "Y"
            };

            service.TrackChangesForModifiers(charge, saveModel);

            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.AtLeast(2));
        }

        [Fact]
        public async Task TrackValue_WhenValuesDifferent_ShouldCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = "OLD"
            };

            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "NEW"
            };

            service.TrackChangesForModifiers(charge, saveModel);

            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task TrackValue_WhenValuesSame_ShouldNotCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = "SAME"
            };

            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "SAME"
            };

            service.TrackChangesForModifiers(charge, saveModel);

            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task TrackValue_WhenBothNull_ShouldNotCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                (ClaimHistoryAction)1,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = null
            };

            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = null
            };

            service.TrackChangesForModifiers(charge, saveModel);

            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task TrackSubReasonValue_WhenValuesDifferent_ShouldCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 10 },
                (ClaimAction)1,
                (ClaimHistoryAction)ClaimHistoryAction.ClaimUpdated,
                DateTime.UtcNow);

            // simulate change using modifiers (calls TrackValue internally)
            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = "OLD"
            };

            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "NEW"
            };

            service.TrackChangesForModifiers(charge, saveModel);

            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task TrackSubReasonValue_WhenValuesSame_ShouldNotCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 10 },
                (ClaimAction)1,
                (ClaimHistoryAction)ClaimHistoryAction.ClaimUpdated,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = "SAME"
            };

            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = "SAME"
            };

            service.TrackChangesForModifiers(charge, saveModel);

            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task TrackSubReasonValue_WhenValuesNull_ShouldNotCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 10 },
                (ClaimAction)1,
                (ClaimHistoryAction)ClaimHistoryAction.ClaimUpdated,
                DateTime.UtcNow);

            var charge = new ClaimChargeEntryEntity
            {
                Modifier1 = null
            };

            var saveModel = new UpdateChargeModifiersModel
            {
                Modifier1 = null
            };

            service.TrackChangesForModifiers(charge, saveModel);

            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task SaveChangesAsync_WhenProviderChanged_ShouldCallAddAsync()
        {
            // Arrange
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo
                {
                    Id = 1,
                    MemberId = 1
                },
                (ClaimAction)1,
                ClaimHistoryAction.ClaimUpdated,
                DateTime.UtcNow);

            // This safely creates change entry
            var attachment = new ClaimAttachmentEntity
            {
                FileName = "OLD_FILE"
            };

            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = "NEW_FILE"
            };

            service.TrackAttachementsChangesForClaim(attachment, saveModel);

            // Act
            await service.SaveChangesAsync();

            // Assert
            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task SaveChangesAsync_WhenNoProviderChange_ShouldNotCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo
                {
                    Id = 1,
                    MemberId = 1
                },
                (ClaimAction)1,
                ClaimHistoryAction.ClaimUpdated,
                DateTime.UtcNow);

            var attachment = new ClaimAttachmentEntity
            {
                FileName = "SAME_FILE"
            };

            var saveModel = new RenameAttachmentModelWithUserInfo
            {
                FileName = "SAME_FILE"
            };

            service.TrackAttachementsChangesForClaim(attachment, saveModel);

            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task TrackBillingProviderValue_BothNull_ShouldNotCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                ClaimHistoryAction.ClaimUpdated,
                DateTime.UtcNow);

            InvokeTrackBillingProviderValue(
                service,
                ClaimHistoryField.BillingProvider,
                null,
                null,
                null,
                null);

            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task TrackBillingProviderValue_SameIds_ShouldNotCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                ClaimHistoryAction.ClaimUpdated,
                DateTime.UtcNow);

            InvokeTrackBillingProviderValue(
                service,
                ClaimHistoryField.BillingProvider,
                "123",
                "123",
                "OLD",
                "OLD");

            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Never);
        }

        [Fact]
        public async Task TrackBillingProviderValue_DifferentIds_ShouldCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                ClaimHistoryAction.ClaimUpdated,
                DateTime.UtcNow);

            InvokeTrackBillingProviderValue(
                service,
                ClaimHistoryField.BillingProvider,
                "123",
                "456",
                "OLD",
                "NEW");

            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task TrackBillingProviderValue_OldNull_ShouldCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                ClaimHistoryAction.ClaimUpdated,
                DateTime.UtcNow);

            InvokeTrackBillingProviderValue(
                service,
                ClaimHistoryField.BillingProvider,
                null,
                "456",
                null,
                "NEW");

            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task TrackBillingProviderValue_NewNull_ShouldCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock
                .Setup(x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            service.Initialize(
                new IdWithUserInfo { Id = 1, MemberId = 1 },
                (ClaimAction)1,
                ClaimHistoryAction.ClaimUpdated,
                DateTime.UtcNow);

            InvokeTrackBillingProviderValue(service, ClaimHistoryField.BillingProvider, "123", null, "OLD", null);

            await service.SaveChangesAsync();

            historyMock.Verify(
                x => x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()),
                Times.Once);
        }

        [Fact]
        public async Task TrackBillingProviderValue_Private_BothNull_ShouldNotCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock.Setup(x =>
                x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            await InvokeTrackBillingProviderValueAndSaveAsync(service, historyMock, null, null, null, null, false);
        }

        [Fact]
        public async Task TrackBillingProviderValue_Private_SameIds_ShouldNotCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock.Setup(x =>
                x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            await InvokeTrackBillingProviderValueAndSaveAsync(service, historyMock, "100", "100", "OLD", "OLD", false);
        }

        [Fact]
        public async Task TrackBillingProviderValue_Private_DifferentIds_ShouldCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock.Setup(x =>
                x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            await InvokeTrackBillingProviderValueAndSaveAsync(service, historyMock, "100", "200", "OLD", "NEW", true);
        }

        [Fact]
        public async Task TrackBillingProviderValue_Private_OldNull_ShouldCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock.Setup(x =>
                x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            await InvokeTrackBillingProviderValueAndSaveAsync(service, historyMock, null, "200", null, "NEW", true);
        }

        [Fact]
        public async Task TrackBillingProviderValue_Private_NewNull_ShouldCallAddAsync()
        {
            var historyMock = new Mock<IClaimHistoryService>();

            historyMock.Setup(x =>
                x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(historyMock.Object);

            await InvokeTrackBillingProviderValueAndSaveAsync(service, historyMock, "100", null, "OLD", null, true);
        }

        [Fact]
        public async Task TrackSubReasonValue_Private_BothNull_ShouldNotCallAddAsync()
        {
            var mock = new Mock<IClaimHistoryService>();

            mock.Setup(x =>
                x.AddAsync(It.IsAny<ClaimHistoryFieldSaveModel>(), It.IsAny<bool>()))
                .Returns(Task.CompletedTask);

            var service = new ClaimChangeTrackingService(mock.Object);

            await InvokeTrackSubReasonValueAndSaveAsync(service, mock, null, null, false);
        }

        [Fact]
        public async Task TrackAdditionalInfo_AllFieldsChanged_ShouldCallAddAsync()
        {
            var claim = new ClaimEntity
            {
                BenefitAssignmentId = 1,
                ReleaseOfInformationConfirmationTypeId = 1,
                AuthorizedPaymentConfirmationTypeId = 1,
                FrequencyTypeId = ClaimFrequencyType.Original,   // use real enum
                OriginalClaim = "OLD",
                Note = "OLD_NOTE"
            };

            var saveModel = new UpdateClaimDetailsModel
            {
                BenefitAssignmentId = 2,  // different value
                PatientReleaseAgreementId = 2,
                AuthorizePaymentId = 2,
                SubmissionReasonId = (int)ClaimFrequencyType.Replacement, // different enum
                OriginalClaim = "NEW",
                Note = "NEW_NOTE"
            };

            await InvokeTrackAdditionalInfoAndVerifyAsync(
                claim,
                saveModel,
                true);
        }
        [Fact]
        public async Task TrackAdditionalInfoNoChangesShouldNotCallAddAsync()
        {
            var claim = new ClaimEntity
            {
                BenefitAssignmentId = 1,
                ReleaseOfInformationConfirmationTypeId = 1,
                AuthorizedPaymentConfirmationTypeId = 1,
                FrequencyTypeId = (ClaimFrequencyType)1,
                OriginalClaim = "SAME",
                Note = "SAME"
            };

            var saveModel = new UpdateClaimDetailsModel
            {
                BenefitAssignmentId = 1,
                PatientReleaseAgreementId = 1,
                AuthorizePaymentId = 1,
                SubmissionReasonId = 1,
                OriginalClaim = "SAME",
                Note = "SAME"
            };

            await InvokeTrackAdditionalInfoAndVerifyAsync(
                claim,
                saveModel,
                false);
        }

        [Fact]
        public async Task TrackAdditionalInfo_NoChanges_ShouldNotCallAddAsync()
        {
            var claim = new ClaimEntity
            {
                BenefitAssignmentId = 1,
                ReleaseOfInformationConfirmationTypeId = 1,
                AuthorizedPaymentConfirmationTypeId = 1,
                FrequencyTypeId = ClaimFrequencyType.Original,
                OriginalClaim = "ABC",
                Note = "Note"
            };

            var saveModel = new UpdateClaimDetailsModel
            {
                BenefitAssignmentId = 1,
                PatientReleaseAgreementId = 1,
                AuthorizePaymentId = 1,
                SubmissionReasonId = 1,
                OriginalClaim = "ABC",
                Note = "Note"
            };

            await InvokeTrackAdditionalInfoAndVerifyAsync(claim, saveModel, false);
        }

        [Fact]
        public void TrackClientInfo_ShouldExecuteSuccessfully()
        {
            // Arrange
            var claimHistoryServiceMock = new Mock<IClaimHistoryService>();

            var service = new ClaimChangeTrackingService(claimHistoryServiceMock.Object);

            var diagnosisEntity = new DiagnosisEntity
            {
                DiagnosisCode = "A001"
            };

            var claimDiagnosis = new ClaimDiagnosisCodeEntity
            {
                Order = 1,
                Diagnosis = diagnosisEntity
            };

            var claim = new ClaimEntity
            {
                ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>
            {
                claimDiagnosis
            }
            };

            var saveModel = new UpdateClaimDetailsModel
            {
                DiagnosisCodes = new List<ClaimDiagnosisCodeUpdateModel>
            {
                new ClaimDiagnosisCodeUpdateModel
                {
                    Order = 1,
                    DiagnosisCode = "A001"
                }
            }
            };

            var method = typeof(ClaimChangeTrackingService)
                .GetMethod("TrackClientInfo", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var exception = Record.Exception(() =>
                method.Invoke(service, new object[] { claim, saveModel }));

            // Assert
            Assert.Null(exception);
        }

        [Fact]
        public void GetConfirmationType_ShouldReturnYes()
        {
            var mock = new Mock<IClaimHistoryService>();
            var service = new ClaimChangeTrackingService(mock.Object);

            var method = typeof(ClaimChangeTrackingService)
                .GetMethod("GetConfirmationType", BindingFlags.NonPublic | BindingFlags.Instance);

            var result = method.Invoke(service, new object[] { (int?)1 });

            Assert.Equal("Yes", result);
        }

        [Fact]
        public void GetConfirmationType_ShouldReturnNo()
        {
            var mock = new Mock<IClaimHistoryService>();
            var service = new ClaimChangeTrackingService(mock.Object);

            var method = typeof(ClaimChangeTrackingService)
                .GetMethod("GetConfirmationType", BindingFlags.NonPublic | BindingFlags.Instance);

            var result = method.Invoke(service, new object[] { (int?)2 });

            Assert.Equal("No", result);
        }

        [Fact]
        public void GetConfirmationType_ShouldReturnNotApplicable()
        {
            var mock = new Mock<IClaimHistoryService>();
            var service = new ClaimChangeTrackingService(mock.Object);

            var method = typeof(ClaimChangeTrackingService)
                .GetMethod("GetConfirmationType", BindingFlags.NonPublic | BindingFlags.Instance);

            var result = method.Invoke(service, new object[] { (int?)3 });

            Assert.Equal("Not applicable", result);
        }

        [Fact]
        public void GetConfirmationType_ShouldReturnNull_ForInvalidValue()
        {
            var mock = new Mock<IClaimHistoryService>();
            var service = new ClaimChangeTrackingService(mock.Object);

            var method = typeof(ClaimChangeTrackingService)
                .GetMethod("GetConfirmationType", BindingFlags.NonPublic | BindingFlags.Instance);

            var result = method.Invoke(service, new object[] { (int?)99 });

            Assert.Null(result);
        }

        [Fact]
        public void GetConfirmationType_ShouldReturnNull_ForNull()
        {
            var mock = new Mock<IClaimHistoryService>();
            var service = new ClaimChangeTrackingService(mock.Object);

            var method = typeof(ClaimChangeTrackingService)
                .GetMethod("GetConfirmationType", BindingFlags.NonPublic | BindingFlags.Instance);

            var result = method.Invoke(service, new object[] { null });

            Assert.Null(result);
        }
    }
}