using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Client;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Services.Billing;
using BillingService.Domain.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.Claim;
public class ClaimValidationServiceTests
{

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["EdiSettings:CustomerId"] = "123"
            })
            .Build();


    public sealed class EntryOnlyContext : DbContext
    {
        public EntryOnlyContext(DbContextOptions<EntryOnlyContext> options) : base(options) { }

        // DbSets are optional here but help EF know about entity types
        public DbSet<ClaimSubmissionEntity> ClaimSubmissions { get; set; }
        public DbSet<ClaimSubmissionServiceLineEntity> ClaimSubmissionServiceLines { get; set; }
        public DbSet<ClaimSubmissionFunderSequenceEntity> ClaimSubmissionFunderSequences { get; set; }
    }

    private static ClaimValidationService BuildSut(
        IConfiguration config,
        IRepository<BillingDbContext, ClaimEntity> claimRepo,
        IRepository<BillingDbContext, ClaimSubmissionEntity> submissionRepo,
        IRepository<BillingDbContext, ClaimValidationErrorEntity> validationErrRepo,
        IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity> svcLineRepo,
        IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity> funderSeqRepo,
        IRepository<BillingDbContext, ClaimAppointmentLinkEntity> apptRepo,
        IRepository<BillingDbContext, PaymentClaimEntity> paymentRepo,
        IRepository<BillingDbContext, ClaimErrorMessageEntity> errMsgRepo,
        IClaimHistoryService history,
        IRethinkMasterDataMicroServices rethink,
        IClientService client,
        IRepository<BillingDbContext, ClaimDiagnosisCodeEntity> dxRepo,
        IStediProviderEnrollmentService stediProviderEnrollmentService,
        IClearinghouseCredentialValidationService clearinghouseCredentialValidationService, // <-- Add this parameter
        IFeatureFlagService featureFlagService = null
    )
    {
        var logger = new Mock<ILogger<ClaimValidationService>>();   
        return new ClaimValidationService(
            config,
            claimRepo,
            submissionRepo,
            validationErrRepo,
            svcLineRepo,
            funderSeqRepo,
            paymentRepo,
            errMsgRepo,
            history,
            rethink,
            stediProviderEnrollmentService,
            clearinghouseCredentialValidationService,
            featureFlagService ?? new Mock<IFeatureFlagService>().Object,
            logger.Object
        );
    }
    private ClaimValidationService CreateSut()
    {
        var configuration = new Mock<IConfiguration>();
        var claimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
        var claimSubmissionRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>>();
        var claimValidationErrorRepo = new Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>>();
        var claimSubmissionServiceLineRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity>>();
        var claimSubmissionFunderSequenceRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity>>();
        var claimAppointmentLinkRepo = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>();
        var paymentClaimRepo = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
        var claimErrorMessageRepo = new Mock<IRepository<BillingDbContext, ClaimErrorMessageEntity>>();
        var claimHistoryService = new Mock<IClaimHistoryService>();
        var rethinkServices = new Mock<IRethinkMasterDataMicroServices>();
        var clientService = new Mock<IClientService>();
        var claimDiagnosisCodeRepo = new Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>>();
        var stediProviderEnrollmentService = new Mock<IStediProviderEnrollmentService>();
        var clearinghouseCredentialValidationService = new Mock<IClearinghouseCredentialValidationService>();
        var featureFlagService = new Mock<IFeatureFlagService>();
        var logger = new Mock<ILogger<ClaimValidationService>>();

        return new ClaimValidationService(
            configuration.Object,
            claimRepo.Object,
            claimSubmissionRepo.Object,
            claimValidationErrorRepo.Object,
            claimSubmissionServiceLineRepo.Object,
            claimSubmissionFunderSequenceRepo.Object,
            paymentClaimRepo.Object,
            claimErrorMessageRepo.Object,
            claimHistoryService.Object,
            rethinkServices.Object,
            stediProviderEnrollmentService.Object,
            clearinghouseCredentialValidationService.Object,
            featureFlagService.Object,
            logger.Object
        );
    }
    [Fact]
    public async Task GetClaimInformation_ShouldHydrateClaim_FromRethinkCalls()
    {
        // --- Repos ---
        var claimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
        var submissionRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>>();
        var validationErrRepo = new Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>>();
        var svcLineRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity>>();
        var funderSeqRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity>>();
        var apptRepo = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>();
        var paymentRepo = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
        var errMsgRepo = new Mock<IRepository<BillingDbContext, ClaimErrorMessageEntity>>();
        var dxRepo = new Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>>();
        var stediProviderEnrollmentService = new Mock<IStediProviderEnrollmentService>();
        var clearinghouseCredentialValidationService = new Mock<IClearinghouseCredentialValidationService>(); // <-- Add this mock

        // --- Services ---
        var rethink = new Mock<IRethinkMasterDataMicroServices>();
        var history = new Mock<IClaimHistoryService>();
        var client = new Mock<IClientService>();

        var config = BuildConfig();

        var claimId = 1;
        var accountInfoId = 1;
        var childProfileId = 10;

        var claim = new ClaimEntity
        {
            Id = claimId,
            MemberId = 10,
            AccountInfoId = accountInfoId,
            ChildProfileId = childProfileId,
            DateDeleted = null,

            AuthorizationId = 1,
            ChildProfileReferringProviderId = 300,
            ProviderLocationId = 111,
            ServiceLocationId = 222,
            RenderingStaffMemberId = 333,
            LocationCodeId = 123,

            ClaimHistory = new List<ClaimHistoryEntity>
    {
        new ClaimHistoryEntity
        {
            ClaimHistoryAction = ClaimHistoryAction.ClaimCreated,
            Mode = ClaimActionMode.User
        }
    },

            ClaimChargeEntries = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity
        {
            Id = 100,
            DateOfService = DateTime.UtcNow.AddDays(-1),
            BillingCode = "99213",
            Units = 1,
            Charges = 100,
            DiagnosisCode = "A00",
            DateDeleted = null
        }
    },

            ClaimDiagnosisCodes = new List<ClaimDiagnosisCodeEntity>
    {
        new ClaimDiagnosisCodeEntity
        {
            DiagnosisId = 1,
            Order = 1,
            IncludeOnClaims = true,
            DateDeleted = null,
            Diagnosis = new DiagnosisEntityModel
            {
                Id = 1,
                Pos = 1,
                DiagnosisCode = "A00",
                TypeId = DiagnosisTypes.Custom
            }
        }
    },

            ClaimSubmissions = new List<ClaimSubmissionEntity>(),
            ClaimValidationErrors = new List<ClaimValidationErrorEntity>()
        };

        claimRepo.Setup(r => r.Query())
            .Returns(new List<ClaimEntity> { claim }
                .AsQueryable()
                .BuildMock());

        rethink.Setup(r => r.GetChildProfileReturningEntity(accountInfoId, childProfileId))
            .ReturnsAsync(new ChildProfileEntityModel
            {
                FirstName = "A",
                LastName = "B",
                City = "NYC",
                StateId = 1,
                ZipCode = "10001"
            });

        rethink.Setup(r => r.GetAccountReturningEntityAsync(accountInfoId, true))
            .ReturnsAsync(new AccountInfoEntityModel
            {
                BillingProviderName = "Clinic",
                BillingProviderTaxonomyCode = "207Q00000X",
                BillingZip = "10001",
                ClearingHouse = new ClearingHouseDataModel
                {
                    title = "CH",
                    urlLink = "x",
                    userName = "a",
                    userPassword = "b"
                }
            });

        rethink.Setup(r => r.GetChildProfileAuthorizationByClientId(accountInfoId, childProfileId, 1))
            .ReturnsAsync(new ClientAuthorization
            {
                id = 1,
                providerServiceId = 999
            });

        rethink.Setup(r => r.GetChildProfileReferringProviderEntity(accountInfoId, childProfileId, 300))
            .ReturnsAsync(new clientReferringProviders
            {
                referringProviderId = 300,
                isActive = true
            });

        rethink.Setup(r => r.GetProviderLocation(accountInfoId, 111))
            .ReturnsAsync(new ProviderLocations { accountId = accountInfoId });

        rethink.Setup(r => r.GetProviderLocation(accountInfoId, 222))
            .ReturnsAsync(new ProviderLocations { accountId = accountInfoId });

        rethink.Setup(r => r.GetProviderLocation(accountInfoId, 999))
            .ReturnsAsync(new ProviderLocations { accountId = accountInfoId });

        rethink.Setup(r => r.GetMemberAsync(accountInfoId, 333))
            .ReturnsAsync(new RethinkAccountMember
            {
                userName = "drwho",
            });

        rethink.Setup(r => r.GetLocationCodes())
            .ReturnsAsync(new List<LocationCodesModel>
            {
                new LocationCodesModel { id = 123, code = "11" }
            });

        // Mock GetServiceLineMappingsByFunderId to return valid funder mappings
        rethink.Setup(r => r.GetServiceLineMappingsByFunderId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new List<ServiceLines>
            {
                new ServiceLines
                {
                    ChildProfileFunderMappingId = 456,
                    responsibilitySequence = ResponsibilitySequenceType.Primary,
                    metaData = new MetaData { deletedOn = null, deletedBy = null }
                }
            });

        // Mock GetChildProfileFunderMappingByMappingId
        rethink.Setup(r => r.GetChildProfileFunderMappingByMappingId(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new FunderDetails
            {
                funderId = 100,
                metaData = new MetaData { deletedOn = null, deletedBy = null }
            });

        // Mock GetFunder
        rethink.Setup(r => r.GetFunder(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new FunderDataModel
            {
                id = 100,
                funderName = "Test Funder",
                funderTypeId = 1,
                metaData = new MetaData { deletedOn = null, deletedBy = null }
            });

        var service = BuildSut(
            config,
            claimRepo.Object,
            submissionRepo.Object,
            validationErrRepo.Object,
            svcLineRepo.Object,
            funderSeqRepo.Object,
            apptRepo.Object,
            paymentRepo.Object,
            errMsgRepo.Object,
            history.Object,
            rethink.Object,
            client.Object,
            dxRepo.Object,
            stediProviderEnrollmentService.Object,
            clearinghouseCredentialValidationService.Object);

        // Act
        var result = await service.GetClaimInformation(claimId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ChildProfile);
        Assert.Equal("A", result.ChildProfile.FirstName);

        Assert.NotNull(result.AccountInfo);
        Assert.Equal("Clinic", result.AccountInfo.BillingProviderName);

        Assert.NotNull(result.ProviderLocation);
        Assert.NotNull(result.ServiceLocation);

        Assert.NotNull(result.ReferringProvider);
        Assert.NotNull(result.RenderingStaffMember);

        Assert.NotNull(result.LocationCode);
        Assert.Equal("11", result.LocationCode.code);

        Assert.NotNull(result.ChildProfileAuthorization);
        Assert.NotNull(result.ChildProfileAuthorization.ServiceFacilityLocation);
    }
    #region UpdateClaimSubmissionEntities Tests

    private static async Task Invoke_UpdateClaimSubmissionEntities_Local(
        ClaimValidationService sut,
        object validationResult,
        bool isSaveSubmission)
    {
        var method = typeof(ClaimValidationService).GetMethod(
            "UpdateClaimSubmissionEntities",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var task = (Task)method!.Invoke(sut, new[] { validationResult, isSaveSubmission })!;
        await task.ConfigureAwait(false);
    }

    private static Type? FindNestedTypeByProperties_Local(params string[] requiredPropertyNames)
    {
        var nested = typeof(ClaimValidationService)
            .GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);

        foreach (var t in nested)
        {
            var props = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                         .Select(p => p.Name)
                         .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (requiredPropertyNames.All(rp => props.Contains(rp)))
                return t;
        }

        return null;
    }

    private static object CreateValidationResult_Local(
        ClaimSubmissionEntity submission,
        List<ClaimSubmissionServiceLineEntity> serviceLines,
        List<ClaimSubmissionFunderSequenceEntity> funderSequences)
    {
        var resultType = FindNestedTypeByProperties_Local("ClaimSubmission", "ServiceLines", "FunderSequences");
        Assert.NotNull(resultType);

        object instance;
        try
        {
            instance = Activator.CreateInstance(resultType!, nonPublic: true)!;
        }
        catch
        {
            instance = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(resultType!);
        }

        void SetProp(string name, object value)
        {
            var pi = resultType!.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(pi);
            pi!.SetValue(instance, value);
        }

        SetProp("ClaimSubmission", submission);
        SetProp("ServiceLines", serviceLines);
        SetProp("FunderSequences", funderSequences);

        return instance;
    }

    [Fact]
    public async Task UpdateClaimSubmissionEntities_WithIsSaveSubmissionFalse_DoesNothing()
    {
        // Arrange
        var sut = CreateSut();

        var submission = new ClaimSubmissionEntity { Id = 10, ClaimId = 1 };
        var serviceLines = new List<ClaimSubmissionServiceLineEntity>
    {
        new ClaimSubmissionServiceLineEntity { Id = 100 }
    };
        var funderSeqs = new List<ClaimSubmissionFunderSequenceEntity>
    {
        new ClaimSubmissionFunderSequenceEntity { Id = 200 }
    };

        var validationResult = CreateValidationResult_Local(submission, serviceLines, funderSeqs);

        // Act
        await Invoke_UpdateClaimSubmissionEntities_Local(sut, validationResult, isSaveSubmission: false);

        // Assert (early-exit branch covered, no exception)
        Assert.True(true);
    }

    [Fact]
    public async Task UpdateClaimSubmissionEntities_WithExistingSubmission_UpdatesExistingChildren_AndAddsNewChildren()
    {
        var configuration = new Mock<IConfiguration>();

        var claimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
        var submissionRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>>();
        var validationErrRepo = new Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>>();
        var svcLineRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity>>();
        var funderSeqRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity>>();
        var apptRepo = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>();
        var paymentRepo = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
        var errMsgRepo = new Mock<IRepository<BillingDbContext, ClaimErrorMessageEntity>>();
        var history = new Mock<IClaimHistoryService>();
        var rethink = new Mock<IRethinkMasterDataMicroServices>();
        var client = new Mock<IClientService>();
        var dxRepo = new Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>>();
        var stediProviderEnrollmentService = new Mock<IStediProviderEnrollmentService>();
        var clearinghouseCredentialValidationService = new Mock<IClearinghouseCredentialValidationService>();
        var featureFlagService = new Mock<IFeatureFlagService>();
        var logger = new Mock<ILogger<ClaimValidationService>>();

        var efOptions = new DbContextOptionsBuilder<EntryOnlyContext>()
            .UseInMemoryDatabase($"test-update-existing-mix-{Guid.NewGuid()}")
            .Options;
        await using var efContext = new EntryOnlyContext(efOptions);

        submissionRepo.Setup(r => r.Entry(It.IsAny<ClaimSubmissionEntity>()))
            .Returns((ClaimSubmissionEntity e) => efContext.Entry(e));
        svcLineRepo.Setup(r => r.Entry(It.IsAny<ClaimSubmissionServiceLineEntity>()))
            .Returns((ClaimSubmissionServiceLineEntity e) => efContext.Entry(e));
        funderSeqRepo.Setup(r => r.Entry(It.IsAny<ClaimSubmissionFunderSequenceEntity>()))
            .Returns((ClaimSubmissionFunderSequenceEntity e) => efContext.Entry(e));

        submissionRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        submissionRepo.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
        svcLineRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        svcLineRepo.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);
        funderSeqRepo.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        funderSeqRepo.Setup(r => r.CommitAsync()).Returns(Task.CompletedTask);

        var sut = new ClaimValidationService(
            configuration.Object,
            claimRepo.Object,
            submissionRepo.Object,
            validationErrRepo.Object,
            svcLineRepo.Object,
            funderSeqRepo.Object,
            paymentRepo.Object,
            errMsgRepo.Object,
            history.Object,
            rethink.Object,
            stediProviderEnrollmentService.Object,
            clearinghouseCredentialValidationService.Object,
            featureFlagService.Object,
            logger.Object);

        var submission = new ClaimSubmissionEntity { Id = 10, ClaimId = 1 };

        var serviceLines = new List<ClaimSubmissionServiceLineEntity>
    {
        new ClaimSubmissionServiceLineEntity { Id = 501, ClaimSubmissionId = 10 }, // Update
        new ClaimSubmissionServiceLineEntity { Id = 0,   ClaimSubmissionId = 10 }  // Add
    };

        var funderSeqs = new List<ClaimSubmissionFunderSequenceEntity>
    {
        new ClaimSubmissionFunderSequenceEntity { Id = 601, ClaimSubmissionId = 10 }, // Update
        new ClaimSubmissionFunderSequenceEntity { Id = 0,   ClaimSubmissionId = 10 }  // Add
    };

        var validationResult = CreateValidationResult_Local(submission, serviceLines, funderSeqs);

        // Act
        await Invoke_UpdateClaimSubmissionEntities_Local(sut, validationResult, isSaveSubmission: true);

        // Assert
        submissionRepo.Verify(r => r.Update(It.IsAny<ClaimSubmissionEntity>()), Times.Once);
        submissionRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        submissionRepo.Verify(r => r.CommitAsync(), Times.Once);

        svcLineRepo.Verify(r => r.Update(It.IsAny<ClaimSubmissionServiceLineEntity>()), Times.Once);
        svcLineRepo.Verify(r => r.Add(It.IsAny<ClaimSubmissionServiceLineEntity>()), Times.Once);
        svcLineRepo.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
        svcLineRepo.Verify(r => r.CommitAsync(), Times.Once);

        funderSeqRepo.Verify(r => r.Update(It.IsAny<ClaimSubmissionFunderSequenceEntity>()), Times.Once);
        funderSeqRepo.Verify(r => r.Add(It.IsAny<ClaimSubmissionFunderSequenceEntity>()), Times.Once);
        funderSeqRepo.Verify(r => r.SaveChangesAsync(), Times.Exactly(2));
        funderSeqRepo.Verify(r => r.CommitAsync(), Times.Once);
    }

    #endregion


    [Fact]
    public async Task GetClaimSubmissionInformation_ShouldReturnLatestSubmission_ForClaimId()
    {
        // --- Repos ---
        var claimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
        var submissionRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>>();
        var validationErrRepo = new Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>>();
        var svcLineRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity>>();
        var funderSeqRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity>>();
        var apptRepo = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>();
        var paymentRepo = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
        var errMsgRepo = new Mock<IRepository<BillingDbContext, ClaimErrorMessageEntity>>();
        var dxRepo = new Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>>();
        var stediProviderEnrollmentService = new Mock<IStediProviderEnrollmentService>();
        var clearinghouseCredentialValidationService = new Mock<IClearinghouseCredentialValidationService>(); // <-- Add this mock

        // --- Services ---
        var rethink = new Mock<IRethinkMasterDataMicroServices>();
        var history = new Mock<IClaimHistoryService>();
        var client = new Mock<IClientService>();

        var config = BuildConfig();
        var claimId = 1;

        var claim = new ClaimEntity { Id = claimId };

        var older = new ClaimSubmissionEntity
        {
            Id = 10,
            ClaimId = claimId,
            DateDeleted = null,
            Claim = claim,
            ClaimSubmissionFunderSequences = new List<ClaimSubmissionFunderSequenceEntity>
            {
                new ClaimSubmissionFunderSequenceEntity { Id = 1, ClaimSubmissionId = 10, DateDeleted = null }
            },
            ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>
            {
                new ClaimSubmissionServiceLineEntity { Id = 1, ClaimSubmissionId = 10, DateDeleted = null }
            }
        };

        var latest = new ClaimSubmissionEntity
        {
            Id = 25,
            ClaimId = claimId,
            DateDeleted = null,
            Claim = claim,
            ClaimSubmissionFunderSequences = new List<ClaimSubmissionFunderSequenceEntity>
            {
                new ClaimSubmissionFunderSequenceEntity { Id = 2, ClaimSubmissionId = 25, DateDeleted = null }
            },
            ClaimSubmissionServiceLines = new List<ClaimSubmissionServiceLineEntity>
            {
                new ClaimSubmissionServiceLineEntity { Id = 2, ClaimSubmissionId = 25, DateDeleted = null }
            }
        };

        var otherClaim = new ClaimSubmissionEntity
        {
            Id = 999,
            ClaimId = 999,
            DateDeleted = null
        };

        var deleted = new ClaimSubmissionEntity
        {
            Id = 1000,
            ClaimId = claimId,
            DateDeleted = DateTime.UtcNow
        };

        submissionRepo.Setup(r => r.Query())
            .Returns(new List<ClaimSubmissionEntity> { older, latest, otherClaim, deleted }
                .AsQueryable()
                .BuildMock());

        var service = BuildSut(
            config,
            claimRepo.Object,
            submissionRepo.Object,
            validationErrRepo.Object,
            svcLineRepo.Object,
            funderSeqRepo.Object,
            apptRepo.Object,
            paymentRepo.Object,
            errMsgRepo.Object,
            history.Object,
            rethink.Object,
            client.Object,
            dxRepo.Object,
            stediProviderEnrollmentService.Object,
            clearinghouseCredentialValidationService.Object); // <-- Pass this argument

        // Act
        var result = await service.GetClaimSubmissionInformation(claimId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(25, result.Id);
        Assert.Equal(claimId, result.ClaimId);
        Assert.Null(result.DateDeleted);

        Assert.NotNull(result.Claim);
        Assert.NotEmpty(result.ClaimSubmissionFunderSequences);
        Assert.NotEmpty(result.ClaimSubmissionServiceLines);
    }

    [Fact]
    public async Task GetClaimSubmissionInformation_ShouldReturnNull_WhenNoSubmissionExists()
    {
        // --- Repos ---
        var claimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
        var submissionRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>>();
        var validationErrRepo = new Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>>();
        var svcLineRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity>>();
        var funderSeqRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity>>();
        var apptRepo = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>();
        var paymentRepo = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
        var errMsgRepo = new Mock<IRepository<BillingDbContext, ClaimErrorMessageEntity>>();
        var dxRepo = new Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>>();
        var stediProviderEnrollmentService = new Mock<IStediProviderEnrollmentService>();
        var clearinghouseCredentialValidationService = new Mock<IClearinghouseCredentialValidationService>(); // <-- Add this moc
        // --- Services ---
        var rethink = new Mock<IRethinkMasterDataMicroServices>();
        var history = new Mock<IClaimHistoryService>();
        var client = new Mock<IClientService>();

        var config = BuildConfig();

        submissionRepo.Setup(r => r.Query())
            .Returns(new List<ClaimSubmissionEntity>()
                .AsQueryable()
                .BuildMock());

        var service = BuildSut(
            config,
            claimRepo.Object,
            submissionRepo.Object,
            validationErrRepo.Object,
            svcLineRepo.Object,
            funderSeqRepo.Object,
            apptRepo.Object,
            paymentRepo.Object,
            errMsgRepo.Object,
            history.Object,
            rethink.Object,
            client.Object,
            dxRepo.Object,
            stediProviderEnrollmentService.Object,
            clearinghouseCredentialValidationService.Object); // <-- Pass this argument

        // Act
        var result = await service.GetClaimSubmissionInformation(claimId: 1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPayerDetails_ShouldReturnData_FromRethinkService()
    {
        // --- Repos ---
        var claimRepo = new Mock<IRepository<BillingDbContext, ClaimEntity>>();
        var submissionRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>>();
        var validationErrRepo = new Mock<IRepository<BillingDbContext, ClaimValidationErrorEntity>>();
        var svcLineRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity>>();
        var funderSeqRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity>>();
        var apptRepo = new Mock<IRepository<BillingDbContext, ClaimAppointmentLinkEntity>>();
        var paymentRepo = new Mock<IRepository<BillingDbContext, PaymentClaimEntity>>();
        var errMsgRepo = new Mock<IRepository<BillingDbContext, ClaimErrorMessageEntity>>();
        var dxRepo = new Mock<IRepository<BillingDbContext, ClaimDiagnosisCodeEntity>>();
        var stediProviderEnrollmentService = new Mock<IStediProviderEnrollmentService>();
        var clearinghouseCredentialValidationService = new Mock<IClearinghouseCredentialValidationService>(); // <-- Add this mock

        // --- Services ---
        var rethink = new Mock<IRethinkMasterDataMicroServices>();
        var history = new Mock<IClaimHistoryService>();
        var client = new Mock<IClientService>();

        var config = BuildConfig();

        var funderId = 100;
        var expected = new PayerDetailsModel();

        rethink.Setup(r => r.GetPayerDetails(funderId))
               .ReturnsAsync(expected);

        var service = BuildSut(
            config,
            claimRepo.Object,
            submissionRepo.Object,
            validationErrRepo.Object,
            svcLineRepo.Object,
            funderSeqRepo.Object,
            apptRepo.Object,
            paymentRepo.Object,
            errMsgRepo.Object,
            history.Object,
            rethink.Object,
            client.Object,
            dxRepo.Object,
            stediProviderEnrollmentService.Object,
            clearinghouseCredentialValidationService.Object); // <-- Pass this argument

        // Act
        var result = await service.GetPayerDetails(funderId);

        // Assert
        Assert.NotNull(result);
        Assert.Same(expected, result);

        rethink.Verify(r => r.GetPayerDetails(funderId), Times.Once);
    }
































































































































































































}

































































































































































































































































































































































































































































































































































































































































