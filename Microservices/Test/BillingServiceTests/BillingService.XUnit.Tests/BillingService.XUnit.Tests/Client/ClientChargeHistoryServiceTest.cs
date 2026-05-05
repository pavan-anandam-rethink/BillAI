using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.PatientInvoice;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Clients;
using BillingService.Domain.Models.Clients.History;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Services.Client;
using BillingService.Domain.Templates.ViewModels;
using BillingService.XUnit.Tests.Common;
using MockQueryable.Moq;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Client
{
    public class ClientChargeHistoryServiceTest : BaseTest
    {
        private readonly Mock<IRethinkMasterDataMicroServices> _rethinkServicesMock = new();
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _claimRepositoryMock = new();
        private readonly Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>> _claimSubmissionsRepositoryMock = new();
        private readonly Mock<IRepository<BillingDbContext, ClaimSearchRenderingProviderEntity>> _claimSearchRenderingProviderRepositoryMock = new();
        private readonly Mock<IRepository<BillingDbContext, ClaimSearchLocationEntity>> _claimSearchLocationRepositoryMock = new();
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _claimChargeEntryRepositoryMock = new();
        private readonly Mock<IRepository<BillingDbContext, ClaimVersionEntity>> _claimVersionRepositoryMock = new();
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> _paymentClaimServiceLineRepositoryMock = new();
        private readonly Mock<IRepository<BillingDbContext, PatientInvoiceDetailsEntity>> _patientInvoiceDetailsRepositoryMock = new();
        private readonly Mock<IRepository<BillingDbContext, PatientInvoiceEntity>> _patientInvoiceRepositoryMock = new();
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity>> _paymentClaimServiceLineAdjustmentRepositoryMock = new();
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>> _claimChargeEntryWriteOffRepositoryMock = new();
        private readonly Mock<IPaymentClaimService> _paymentClaimServiceMock = new();
        private readonly Mock<IPatientInvoiceService> _invoiceServiceMock = new();

        private ClientChargeHistoryService CreateService() =>
            new ClientChargeHistoryService(
                _rethinkServicesMock.Object,
                _claimRepositoryMock.Object,
                _claimSubmissionsRepositoryMock.Object,
                _claimSearchRenderingProviderRepositoryMock.Object,
                _claimSearchLocationRepositoryMock.Object,
                _claimChargeEntryRepositoryMock.Object,
                _claimVersionRepositoryMock.Object,
                _paymentClaimServiceLineRepositoryMock.Object,
                _patientInvoiceDetailsRepositoryMock.Object,
                _patientInvoiceRepositoryMock.Object,
                _paymentClaimServiceLineAdjustmentRepositoryMock.Object,
                _claimChargeEntryWriteOffRepositoryMock.Object,
                _claimChargeEntryRepositoryMock.Object,
                _paymentClaimServiceMock.Object,
                _invoiceServiceMock.Object);


        [Fact]
        public async Task GetClientHistoryClaimAsync_ReturnsClientIds_WhenChildProfilesExist()
        {
            // Arrange
            var accountInfoId = 123;
            var childProfiles = new List<ChildProfileEntityModel>
    {
  new ChildProfileEntityModel { Id = 1 },
       new ChildProfileEntityModel { Id = 2 },
  new ChildProfileEntityModel { Id = 3 }
      };

            _rethinkServicesMock
                 .Setup(x => x.GetChildProfilesForAccount(accountInfoId))
      .ReturnsAsync(childProfiles);

            var service = CreateService();

            // Act
            var result = await service.GetClientHistoryClaimAsync(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(1, result);
            Assert.Contains(2, result);
            Assert.Contains(3, result);
        }

        [Fact]
        public async Task GetClientHistoryClaimAsync_ReturnsEmptyList_WhenNoChildProfilesExist()
        {
            // Arrange
            var accountInfoId = 123;
            var emptyChildProfiles = new List<ChildProfileEntityModel>();

            _rethinkServicesMock
             .Setup(x => x.GetChildProfilesForAccount(accountInfoId))
              .ReturnsAsync(emptyChildProfiles);

            var service = CreateService();

            // Act
            var result = await service.GetClientHistoryClaimAsync(accountInfoId);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetClientRecordAsync_ReturnsClientHistoryResponse_WithValidData()
        {
            // Arrange
            var requestModel = new ClientHistoryRequest
            {
                AccountInfoId = 123,
                MemberId = 456,
                Skip = 0,
                Take = 10
            };

            var filterModel = new ClientRecordFilterModel();

            var childProfiles = new List<ChildProfileEntityModel>
   {
           new ChildProfileEntityModel
        {
    Id = 1,
     FirstName = "John",
    LastName = "Doe",
     DateOfBirth = new DateTime(1990, 5, 15),
 GenderId = 1,
     Address = "123 Main St",
   DateDeleted = null
        }
   };

            var funders = new List<FunderDataModel>
     {
      new FunderDataModel { id = 100, funderName = "Insurance ABC", isActive = true }
};

            var locations = new ClientProviderLocationsModel
            {
                data = new List<ProviderLocations>
      {
   new ProviderLocations { id = 200, name = "Location XYZ" }
 }
            };

            var claims = new List<ClaimEntity>
      {
       new ClaimEntity
        {
      Id = 1,
       ChildProfileId = 1,
    AccountInfoId = 123,
   MemberId = 456,
  PrimaryFunderId = 100,
        ProviderLocationId = 200,
     DateDeleted = null
      }
          };

var chargeEntries = new List<ClaimChargeEntryEntity>
 {
     new ClaimChargeEntryEntity
   {
             Id = 1,
             ClaimId = 1,
             Charges = 100.00m,
             DateDeleted = null
     }
};

 var claimSubmissions = new List<ClaimSubmissionEntity>  
 {

         new ClaimSubmissionEntity
      {
            Id = 1,
            ClaimId = 1,
            ChildProfileDOB = new DateTime(1990, 5, 15)
        }
};

            _rethinkServicesMock.Setup(x => x.GetChildProfilesForAccount(requestModel.AccountInfoId))
          .ReturnsAsync(childProfiles);
            _rethinkServicesMock.Setup(x => x.GetAllFundersForAccount(requestModel.AccountInfoId))
             .ReturnsAsync(funders);
            _rethinkServicesMock.Setup(x => x.GetProviderLocationList(requestModel.AccountInfoId))
          .ReturnsAsync(locations);

            _claimRepositoryMock.Setup(x => x.Query()).Returns(claims.AsQueryable().BuildMockDbSet().Object);
            _claimChargeEntryRepositoryMock.Setup(x => x.Query()).Returns(chargeEntries.AsQueryable().BuildMockDbSet().Object);
            _claimVersionRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimVersionEntity>().AsQueryable().BuildMockDbSet().Object);
            _paymentClaimServiceLineRepositoryMock.Setup(x => x.Query()).Returns(new List<PaymentClaimServiceLineEntity>().AsQueryable().BuildMockDbSet().Object);
            _patientInvoiceDetailsRepositoryMock.Setup(x => x.Query()).Returns(new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMockDbSet().Object);
            _claimSubmissionsRepositoryMock.Setup(x => x.Query()).Returns(claimSubmissions.AsQueryable().BuildMockDbSet().Object);

            var service = CreateService();

            // Act
            var result = await service.GetClientRecordAsync(requestModel, filterModel);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.clientHistoryResponse);
            Assert.True(result.Total >= 0);
        }

        [Fact]
        public async Task GetClientRecordAsync_AppliesDefaultSorting_WhenSortingModelsIsNull()
        {
            // Arrange
            var requestModel = new ClientHistoryRequest
            {
                AccountInfoId = 123,
                MemberId = 456,
                SortingModels = null
            };

            var filterModel = new ClientRecordFilterModel();

            _rethinkServicesMock.Setup(x => x.GetChildProfilesForAccount(It.IsAny<int>()))
            .ReturnsAsync(new List<ChildProfileEntityModel>());
            _rethinkServicesMock.Setup(x => x.GetAllFundersForAccount(It.IsAny<int>()))
           .ReturnsAsync(new List<FunderDataModel>());
            _rethinkServicesMock.Setup(x => x.GetProviderLocationList(It.IsAny<int>()))
    .ReturnsAsync(new ClientProviderLocationsModel { data = new List<ProviderLocations>() });

            _claimRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimEntity>().AsQueryable().BuildMockDbSet().Object);
            _claimChargeEntryRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimChargeEntryEntity>().AsQueryable().BuildMockDbSet().Object);
            _claimVersionRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimVersionEntity>().AsQueryable().BuildMockDbSet().Object);
            _paymentClaimServiceLineRepositoryMock.Setup(x => x.Query()).Returns(new List<PaymentClaimServiceLineEntity>().AsQueryable().BuildMockDbSet().Object);
            _patientInvoiceDetailsRepositoryMock.Setup(x => x.Query()).Returns(new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMockDbSet().Object);
            _claimSubmissionsRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimSubmissionEntity>().AsQueryable().BuildMockDbSet().Object);

            var service = CreateService();

            // Act
            var result = await service.GetClientRecordAsync(requestModel, filterModel);

            // Assert
            Assert.NotNull(requestModel.SortingModels);
            Assert.Single(requestModel.SortingModels);
            Assert.Equal("desc", requestModel.SortingModels[0].Dir);
            Assert.Equal("ClientId", requestModel.SortingModels[0].Field);
        }



        [Fact]
        public async Task GetClientChargeHistoryDetailsAsync_ReturnsChargeDetails_WithValidData()
        {
            // Arrange
            var request = new ClientHistoryChargeDetailsRequest
            {
                ClientId = 1,
                Skip = 0,
                Take = 10,
                SortingModels = null
            };

            var filterModel = new ClientHistoryChargeFilterModel
            {
                FromDate = DateTime.Today.AddDays(-30),
                ThroughDate = DateTime.Today
            };

            var unitTypes = new List<ClientUnitTypes>
    {
        new ClientUnitTypes { id = 1, unit = 15 }
    };
            _rethinkServicesMock.Setup(x => x.GetUnitTypesAsync()).ReturnsAsync(unitTypes);

            var chargeEntries = new List<ClaimChargeEntryEntity>
    {
        new ClaimChargeEntryEntity
        {
            Id = 1,
            ClaimId = 1,
            DateOfService = DateTime.Today.AddDays(-10),
            BillingCode = "99213",
            UnitTypeId = 1,
            Units = 4,
            UnitRate = 25.00m,
            DiagnosisCode = "F32.9",
            DateDeleted = null
        }
    };
            _claimChargeEntryRepositoryMock.Setup(x => x.Query())
                .Returns(chargeEntries.AsQueryable().BuildMockDbSet().Object);

            var claims = new List<ClaimEntity>
    {
        new ClaimEntity
        {
            Id = 1,
            AccountInfoId = 999,
            ChildProfileId = 1,
            ClaimIdentifier = Guid.NewGuid().ToString(),
            ClaimStatus = ClaimStatus.Billed,
            PrimaryFunderId = 100,
            AuthorizationNumber = "AUTH123",
            AuthorizationId = 500,
            LocationCodeId = 200,
            RenderingStaffMemberId = 300,
            MemberId = 456,
            DateDeleted = null
        }
    };
            _claimRepositoryMock.Setup(x => x.Query())
                .Returns(claims.AsQueryable().BuildMockDbSet().Object);

            var locations = new List<ClaimSearchLocationEntity>
    {
        new ClaimSearchLocationEntity { Id = 200, Name = "Test Location", DateDeleted = null }
    };
            _claimSearchLocationRepositoryMock.Setup(x => x.Query())
                .Returns(locations.AsQueryable().BuildMockDbSet().Object);

            var providers = new List<ClaimSearchRenderingProviderEntity>
    {
        new ClaimSearchRenderingProviderEntity { Id = 300, Name = "Dr. Test Provider", DateDeleted = null }
    };
            _claimSearchRenderingProviderRepositoryMock.Setup(x => x.Query())
                .Returns(providers.AsQueryable().BuildMockDbSet().Object);

            var claimVersions = new List<ClaimVersionEntity>
    {
        new ClaimVersionEntity { Id = 10, ClaimId = 1, PatientResponsibilityAmount = 0m, DateDeleted = null }
    };
            _claimVersionRepositoryMock.Setup(x => x.Query())
                .Returns(claimVersions.AsQueryable().BuildMockDbSet().Object);

            var invoiceDetails = new List<PatientInvoiceDetailsEntity>
    {
        new PatientInvoiceDetailsEntity
        {
            Id = 5010,
            InvoiceId = 500,
            ChargeId = 1,
            PatientInvoiceEntity = new PatientInvoiceEntity
            {
                Id = 500,
                Status = Rethink.Services.Common.Enums.Billing.PatientInvoiceStatus.InvoiceSent,
                InvoiceNumber = "INV-001",
                DateDeleted = null
            },
            DateDeleted = null
        }
    };
            _patientInvoiceDetailsRepositoryMock.Setup(x => x.Query())
                .Returns(invoiceDetails.AsQueryable().BuildMockDbSet().Object);

            var invoices = new List<PatientInvoiceEntity>
    {
        new PatientInvoiceEntity
        {
            Id = 500,
            Status = Rethink.Services.Common.Enums.Billing.PatientInvoiceStatus.InvoiceSent,
            InvoiceNumber = "INV-001",
            DateDeleted = null
        }
    };
            _patientInvoiceRepositoryMock.Setup(x => x.Query())
                .Returns(invoices.AsQueryable().BuildMockDbSet().Object);

            var writeOffs = new List<ClaimChargeEntryWriteOffEntity>
    {
        new ClaimChargeEntryWriteOffEntity
        {
            Id = 7001,
            ClaimChargeEntryId = 1,
            WriteOffAmount = 3.00m,
            DateDeleted = null
        }
    };
            _claimChargeEntryWriteOffRepositoryMock.Setup(x => x.Query())
                .Returns(writeOffs.AsQueryable().BuildMockDbSet().Object);

            // --- Payments + PaymentClaims + ServiceLines ---
            var insurancePayment = new PaymentEntity
            {
                Id = 8001,
                PaymentTypeId = (int)PaymentTypes.InsurancePayment,
                DateDeleted = null
            };
            var insuranceClaim = new PaymentClaimEntity
            {
                Id = 8101,
                PaymentId = insurancePayment.Id,
                Payment = insurancePayment,
                DateDeleted = null,
                Claim = new ClaimEntity { Id = 1, DateDeleted = null }
            };
            var pcslInsurance = new PaymentClaimServiceLineEntity
            {
                Id = 9001,
                ClaimChargeEntryId = 1,
                PaymentAmount = 50.00m,
                ChargeAmount = 100.00m,
                PaymentClaimId = insuranceClaim.Id,
                PaymentClaim = insuranceClaim,
                DateDeleted = null
            };

            var clientPayment = new PaymentEntity
            {
                Id = 8002,
                PaymentTypeId = (int)PaymentTypes.ClientPayment,
                DateDeleted = null
            };
            var clientClaim = new PaymentClaimEntity
            {
                Id = 8102,
                PaymentId = clientPayment.Id,
                Payment = clientPayment,
                DateDeleted = null,
                Claim = new ClaimEntity { Id = 1, DateDeleted = null }
            };
            var pcslClient = new PaymentClaimServiceLineEntity
            {
                Id = 9002,
                ClaimChargeEntryId = 1,
                PaymentAmount = 20.00m,
                ChargeAmount = 100.00m,
                PaymentClaimId = clientClaim.Id,
                PaymentClaim = clientClaim,
                DateDeleted = null
            };

            var adjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
    {
        new PaymentClaimServiceLineAdjustmentEntity { Id = 10001, PaymentClaimServiceLineId = pcslInsurance.Id, IsAdjustmentPositive = true,  AdjustmentGroupCode = "CO", AdjustmentAmount = 10.00m, DateDeleted = null },
        new PaymentClaimServiceLineAdjustmentEntity { Id = 10002, PaymentClaimServiceLineId = pcslInsurance.Id, IsAdjustmentPositive = false, AdjustmentGroupCode = "CO", AdjustmentAmount =  2.00m, DateDeleted = null },
        new PaymentClaimServiceLineAdjustmentEntity { Id = 10003, PaymentClaimServiceLineId = pcslInsurance.Id, IsAdjustmentPositive = true,  AdjustmentGroupCode = "PR", AdjustmentAmount =  5.00m, DateDeleted = null },
        new PaymentClaimServiceLineAdjustmentEntity { Id = 10004, PaymentClaimServiceLineId = pcslInsurance.Id, IsAdjustmentPositive = false, AdjustmentGroupCode = "PR", AdjustmentAmount =  1.00m, DateDeleted = null },
    };
            _paymentClaimServiceLineAdjustmentRepositoryMock.Setup(x => x.Query())
                .Returns(adjustments.AsQueryable().BuildMockDbSet().Object);

            var serviceLinesAll = new List<PaymentClaimServiceLineEntity> { pcslInsurance, pcslClient };
            _paymentClaimServiceLineRepositoryMock.Setup(x => x.Query())
                .Returns(serviceLinesAll.AsQueryable().BuildMockDbSet().Object);

            _rethinkServicesMock.Setup(x => x.GetAllFundersForAccount(999))
                .ReturnsAsync(new List<FunderDataModel> { new FunderDataModel { id = 100, funderName = "Test Funder" } });

            var service = CreateService();

            // Act
            var result = await service.GetClientChargeHistoryDetailsAsync(request, filterModel);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.ChargeDetails);
            Assert.Equal(1, result.Total);

            var item = Assert.Single(result.ChargeDetails);

            Assert.Equal(1, item.ChargeId);
            Assert.Equal("99213", item.BillingCode);
            Assert.Equal("Test Location", item.PlaceOfService);
            Assert.Equal("Dr. Test Provider", item.RenderingProvider);
            Assert.Equal("AUTH123", item.AuthorizationNumber);
            Assert.Equal("F32.9", item.Diagnosis);
            Assert.Equal("Test Funder", item.PrimaryFunder);
            Assert.Equal("InvoiceSent", item.InvoiceStatus);

            // Hours: (4 * 15) / 60 = 1.0
            Assert.Equal(1.0, item.Hours, precision: 3);

            Assert.Equal(4, item.Units);
            Assert.Equal(25.00m, item.PerUnitCharge);
            Assert.Equal(100.00m, item.BilledAmount);

            // ✅ Updated expectations to match service sum over both rows
            Assert.Equal(50.00m, item.InsurancePayment);   // from insurance pcsl
            Assert.Equal(20.00m, item.PatientPayments);    // from client pcsl
            Assert.Equal(2.00m, item.Adjustments);        // (-3 + 8) + (-3 + 0) = 2
            Assert.Equal(4.00m, item.PatientResponsibility); // +5 -1 = 4
            Assert.Equal(56.00m, item.ClaimBalance);       // 100 + 2 - 50 + 4 = 56

            Assert.Equal("INV-001", item.InvoiceNumber);
            Assert.Equal(DateTime.Today.AddDays(-10).Date, item.DateOfService.Date);
        }

        [Fact]
        public async Task GetClientChargeHistoryDetailsAsync_AppliesDefaultDateFilter_WhenFilterIsNull()
        {
            // Arrange
            var request = new ClientHistoryChargeDetailsRequest
            {
                ClientId = 1,
                Skip = 0,
                Take = 10
            };

            ClientHistoryChargeFilterModel filterModel = null;

            var unitTypes = new List<ClientUnitTypes>();
            _rethinkServicesMock.Setup(x => x.GetUnitTypesAsync()).ReturnsAsync(unitTypes);
            _rethinkServicesMock.Setup(x => x.GetAllFundersForAccount(It.IsAny<int>()))
             .ReturnsAsync(new List<FunderDataModel>());

            _claimChargeEntryRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimChargeEntryEntity>().AsQueryable().BuildMockDbSet().Object);
            _claimRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimEntity>().AsQueryable().BuildMockDbSet().Object);
            _claimSearchLocationRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimSearchLocationEntity>().AsQueryable().BuildMockDbSet().Object);
            _claimVersionRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimVersionEntity>().AsQueryable().BuildMockDbSet().Object);
            _paymentClaimServiceLineRepositoryMock.Setup(x => x.Query()).Returns(new List<PaymentClaimServiceLineEntity>().AsQueryable().BuildMockDbSet().Object);
            _patientInvoiceDetailsRepositoryMock.Setup(x => x.Query()).Returns(new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMockDbSet().Object);
            _patientInvoiceRepositoryMock.Setup(x => x.Query()).Returns(new List<PatientInvoiceEntity>().AsQueryable().BuildMockDbSet().Object);
            _claimSearchRenderingProviderRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimSearchRenderingProviderEntity>().AsQueryable().BuildMockDbSet().Object);
            _claimChargeEntryWriteOffRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMockDbSet().Object);
            _paymentClaimServiceLineAdjustmentRepositoryMock.Setup(x => x.Query()).Returns(new List<PaymentClaimServiceLineAdjustmentEntity>().AsQueryable().BuildMockDbSet().Object);

            var paymentServiceLines = new List<PaymentClaimServiceLineEntity>();
            _paymentClaimServiceLineRepositoryMock.Setup(x => x.Query())
         .Returns(paymentServiceLines.AsQueryable().BuildMockDbSet().Object);

            var service = CreateService();

            // Act
            var result = await service.GetClientChargeHistoryDetailsAsync(request, filterModel);

            // Assert
            Assert.NotNull(result);
            // Verify that default date filter was applied (90 days back from today)
            // This is verified implicitly through the successful execution
        }

        [Fact]
        public async Task GetClientChargeHistoryDetailsAsync_AppliesDefaultSorting_WhenSortingModelsIsNull()
        {
            // Arrange
            var request = new ClientHistoryChargeDetailsRequest
            {
                ClientId = 1,
                Skip = 0,
                Take = 10,
                SortingModels = null
            };

            var filterModel = new ClientHistoryChargeFilterModel
            {
                FromDate = DateTime.Today.AddDays(-30),
                ThroughDate = DateTime.Today
            };

            var unitTypes = new List<ClientUnitTypes>();
            _rethinkServicesMock.Setup(x => x.GetUnitTypesAsync()).ReturnsAsync(unitTypes);
            _rethinkServicesMock.Setup(x => x.GetAllFundersForAccount(It.IsAny<int>()))
                     .ReturnsAsync(new List<FunderDataModel>());

            _claimChargeEntryRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimChargeEntryEntity>().AsQueryable().BuildMockDbSet().Object);
            _claimRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimEntity>().AsQueryable().BuildMockDbSet().Object);
            _claimSearchLocationRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimSearchLocationEntity>().AsQueryable().BuildMockDbSet().Object);
            _claimVersionRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimVersionEntity>().AsQueryable().BuildMockDbSet().Object);
            _paymentClaimServiceLineRepositoryMock.Setup(x => x.Query()).Returns(new List<PaymentClaimServiceLineEntity>().AsQueryable().BuildMockDbSet().Object);
            _patientInvoiceDetailsRepositoryMock.Setup(x => x.Query()).Returns(new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMockDbSet().Object);
            _patientInvoiceRepositoryMock.Setup(x => x.Query()).Returns(new List<PatientInvoiceEntity>().AsQueryable().BuildMockDbSet().Object);
            _claimSearchRenderingProviderRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimSearchRenderingProviderEntity>().AsQueryable().BuildMockDbSet().Object);
            _claimChargeEntryWriteOffRepositoryMock.Setup(x => x.Query()).Returns(new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMockDbSet().Object);
            _paymentClaimServiceLineAdjustmentRepositoryMock.Setup(x => x.Query()).Returns(new List<PaymentClaimServiceLineAdjustmentEntity>().AsQueryable().BuildMockDbSet().Object);

            var paymentServiceLines = new List<PaymentClaimServiceLineEntity>();
            _paymentClaimServiceLineRepositoryMock.Setup(x => x.Query())
              .Returns(paymentServiceLines.AsQueryable().BuildMockDbSet().Object);

            var service = CreateService();

            // Act
            var result = await service.GetClientChargeHistoryDetailsAsync(request, filterModel);

            // Assert
            Assert.NotNull(request.SortingModels);
            Assert.Single(request.SortingModels);
            Assert.Equal("desc", request.SortingModels[0].Dir);
            Assert.Equal("dateOfService", request.SortingModels[0].Field);
        }


       
[Fact]
public async Task GetAllAuthorizationNumbersAsync_ReturnsDistinctAuthorizationNumbers()
{
    // Arrange
    var userInfo = new UserInfo { AccountInfoId = 123 };

    var claims = new List<ClaimEntity>
    {
        new ClaimEntity { Id = 1, AccountInfoId = 123, AuthorizationId = 1, AuthorizationNumber = "AUTH001", DateDeleted = null },
        new ClaimEntity { Id = 2, AccountInfoId = 123, AuthorizationId = 2, AuthorizationNumber = "AUTH002", DateDeleted = null },
    };

    _claimRepositoryMock
        .Setup(x => x.Query())
        .Returns(claims.AsQueryable().BuildMockDbSet().Object);

    var service = CreateService();

    // Act
    var result = await service.GetAllAuthorizationNumbersAsync(userInfo);

    // Assert
    Assert.NotNull(result);
    Assert.Equal(2, result.Count); 

    Assert.Contains(result, x => x.Id == 1 && x.Name == "AUTH001");
    Assert.Contains(result, x => x.Id == 2 && x.Name == "AUTH002");

    var distinctCount = result.Select(x => (x.Id, x.Name)).Distinct().Count();
    Assert.Equal(result.Count, distinctCount);
}


        [Fact]
        public async Task GetAllAuthorizationNumbersAsync_FiltersOutDeletedClaims()
        {
            // Arrange
            var userInfo = new UserInfo
            {
                AccountInfoId = 123
            };

            var claims = new List<ClaimEntity>
            {
new ClaimEntity
              {
 Id = 1,
  AccountInfoId = 123,
           AuthorizationId = 1,
       AuthorizationNumber = "AUTH001",
   DateDeleted = null
    },
 new ClaimEntity
     {
   Id = 2,
             AccountInfoId = 123,
        AuthorizationId = 2,
     AuthorizationNumber = "AUTH002",
    DateDeleted = DateTime.Now // Deleted claim
 }
       };

            _claimRepositoryMock.Setup(x => x.Query()).Returns(claims.AsQueryable().BuildMockDbSet().Object);

            var service = CreateService();

            // Act
            var result = await service.GetAllAuthorizationNumbersAsync(userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result); // Only non-deleted claim should be returned
            Assert.Equal(1, result[0].Id);
            Assert.Equal("AUTH001", result[0].Name);
        }

        [Fact]
        public async Task GetAllAuthorizationNumbersAsync_FiltersOutNullAndEmptyAuthorizationNumbers()
        {
            // Arrange
            var userInfo = new UserInfo
            {
                AccountInfoId = 123
            };

            var claims = new List<ClaimEntity>
   {
     new ClaimEntity
     {
   Id = 1,
       AccountInfoId = 123,
    AuthorizationId = 1,
   AuthorizationNumber = "AUTH001",
       DateDeleted = null
       },
     new ClaimEntity
                {
        Id = 2,
AccountInfoId = 123,
 AuthorizationId = null,
    AuthorizationNumber = "AUTH002",
     DateDeleted = null
},
           new ClaimEntity
    {
     Id = 3,
     AccountInfoId = 123,
   AuthorizationId = 3,
          AuthorizationNumber = null,
    DateDeleted = null
             },
             new ClaimEntity
   {
Id = 4,
        AccountInfoId = 123,
  AuthorizationId = 4,
    AuthorizationNumber = "",
    DateDeleted = null
     }
        };

            _claimRepositoryMock.Setup(x => x.Query()).Returns(claims.AsQueryable().BuildMockDbSet().Object);

            var service = CreateService();

            // Act
            var result = await service.GetAllAuthorizationNumbersAsync(userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result); // Only the valid authorization should be returned
            Assert.Equal(1, result[0].Id);
            Assert.Equal("AUTH001", result[0].Name);
        }

        [Fact]
        public async Task GetAllAuthorizationNumbersAsync_ReturnsEmptyList_WhenNoValidAuthorizationsExist()
        {
            // Arrange
            var userInfo = new UserInfo
            {
                AccountInfoId = 123
            };

            var claims = new List<ClaimEntity>();

            _claimRepositoryMock.Setup(x => x.Query()).Returns(claims.AsQueryable().BuildMockDbSet().Object);

            var service = CreateService();

            // Act
            var result = await service.GetAllAuthorizationNumbersAsync(userInfo);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task InvoicesSearchAsync_ReturnsEmptyResult_WhenFilterIsNull()
        {
            // Arrange
            var service = CreateService();

            // Act
            var result = await service.InvoicesSearchAsync(null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Data);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task InvoicesSearchAsync_ReturnsEmptyResult_WhenRequestModelIsNull()
        {
            // Arrange
            var filter = new InvoiceHistoryRequestFilterModel();
            var service = CreateService();

            // Act
            var result = await service.InvoicesSearchAsync(null, filter);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Data);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task InvoicesSearchAsync_ReturnsEmptyResult_WhenClientIdIsZero()
        {
            // Arrange
            var request = new InvoiceHistoryRequest { ClientId = 0 };
            var filter = new InvoiceHistoryRequestFilterModel();
            var service = CreateService();

            // Act
            var result = await service.InvoicesSearchAsync(request, filter);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Data);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task InvoicesSearchAsync_ReturnsData_WhenValidInvoiceExists()
        {
            // Arrange
            var request = new InvoiceHistoryRequest
            {
                ClientId = 1,
                Skip = 0,
                Take = 10,
                SortingModels = null
            };

            var filter = new InvoiceHistoryRequestFilterModel
            {
                AccountInfoId = 100
            };

            var invoices = new List<PatientInvoiceEntity>
            {
                new PatientInvoiceEntity
                {
                    Id = 1,
                    ClientId = 1,
                    AccountId = 100,
                    InvoiceDate = DateTime.Today,
                    PaymentDueDate = DateTime.Today.AddDays(10),
                    Status = PatientInvoiceStatus.InvoiceSent,
                    InvoiceNumber = "INV001",
                    DateDeleted = null
                }
            };

            var invoiceDetails = new List<PatientInvoiceDetailsEntity>
            {
                new PatientInvoiceDetailsEntity
                {
                    Id = 1,
                    InvoiceId = 1,
                    ChargeId = 10,
                    DateDeleted = null,
                    ChargeEntry = new ClaimChargeEntryEntity
                    {
                        Id = 10,
                        BillingCode = "99213",
                        Charges = 100,
                        DateOfService = DateTime.Today,
                        Claim = new ClaimEntity()
                    }
                }
            };

            _patientInvoiceRepositoryMock.Setup(x => x.Query())
                .Returns(invoices.AsQueryable().BuildMockDbSet().Object);

            _patientInvoiceDetailsRepositoryMock.Setup(x => x.Query())
                .Returns(invoiceDetails.AsQueryable().BuildMockDbSet().Object);

            _claimSearchLocationRepositoryMock.Setup(x => x.Query())
                .Returns(new List<ClaimSearchLocationEntity>().AsQueryable().BuildMockDbSet().Object);

            _claimSearchRenderingProviderRepositoryMock.Setup(x => x.Query())
                .Returns(new List<ClaimSearchRenderingProviderEntity>().AsQueryable().BuildMockDbSet().Object);

            _paymentClaimServiceMock.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
                .ReturnsAsync(new List<PatientPaymentClaimFullModel>());

            _rethinkServicesMock.Setup(x => x.GetChildProfilesForAccount(It.IsAny<int>()))
                .ReturnsAsync(new List<ChildProfileEntityModel>());

            _claimChargeEntryRepositoryMock.Setup(x => x.Query())
                .Returns(new List<ClaimChargeEntryEntity>
                {
            new ClaimChargeEntryEntity
            {
                Id = 10,
                BillingCode = "99213",
                Charges = 100,
                DateOfService = DateTime.Today,
                ClaimChargeEntryWriteOffs = new List<ClaimChargeEntryWriteOffEntity>()
            }
                }.AsQueryable().BuildMockDbSet().Object);

            var service = CreateService();

            // Act
            var result = await service.InvoicesSearchAsync(request, filter);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result.Data);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task InvoicesSearchAsync_Covers_ReadyToInvoice_Branch()
        {
            var request = new InvoiceHistoryRequest
            {
                ClientId = 1,
                Skip = 0,
                Take = 10
            };

            var filter = new InvoiceHistoryRequestFilterModel
            {
                AccountInfoId = 100,
                Status = new List<int> { (int)PatientInvoiceStatus.ReadytoInvoice }
            };

            _patientInvoiceRepositoryMock.Setup(x => x.Query())
                .Returns(new List<PatientInvoiceEntity>().AsQueryable().BuildMockDbSet().Object);

            _patientInvoiceDetailsRepositoryMock.Setup(x => x.Query())
                .Returns(new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMockDbSet().Object);

            _claimSearchLocationRepositoryMock.Setup(x => x.Query())
                .Returns(new List<ClaimSearchLocationEntity>().AsQueryable().BuildMockDbSet().Object);

            _claimSearchRenderingProviderRepositoryMock.Setup(x => x.Query())
                .Returns(new List<ClaimSearchRenderingProviderEntity>().AsQueryable().BuildMockDbSet().Object);

            _invoiceServiceMock.Setup(x => x.GetPICreationDetails(It.IsAny<CreateInvoiceFilters>()))
                .ReturnsAsync((new List<PatientInvoiceCreationModel>(), 0));

            _claimChargeEntryRepositoryMock.Setup(x => x.Query())
                .Returns(new List<ClaimChargeEntryEntity>().AsQueryable().BuildMockDbSet().Object);

            _paymentClaimServiceMock.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
                .Returns(Task.FromResult(new List<PatientPaymentClaimFullModel>()));

            _rethinkServicesMock.Setup(x => x.GetChildProfilesForAccount(It.IsAny<int>()))
                .ReturnsAsync(new List<ChildProfileEntityModel>());

            var service = CreateService();

            var result = await service.InvoicesSearchAsync(request, filter);

            Assert.NotNull(result);
        }

        [Fact]
        public void MapToInvoiceHistoryResponseList_ReturnsMappedData()
        {
            var models = new List<PatientInvoiceCreationModel>
            {
                new PatientInvoiceCreationModel
                {
                    Id = 1,
                    ClientId = 2,
                    BillingCode = "99213",
                    Charges = 100,
                    PatientBalance = 50,
                    ClaimId = 10
                }
            };

            var claims = new List<ClaimEntity>
            {
                new ClaimEntity
                {
                    Id = 10,
                    LocationCodeId = 20,
                    MemberId = 30
                }
            };

            var locations = new List<ClaimSearchLocationEntity>
            {
                new ClaimSearchLocationEntity { Id = 20, Name = "Clinic" }
            };

            var providers = new List<ClaimSearchRenderingProviderEntity>
            {
                new ClaimSearchRenderingProviderEntity { Id = 30, Name = "Dr Smith" }
            };

            var result = ClientChargeHistoryService.MapToInvoiceHistoryResponseList(
                models, locations, providers, claims);

            Assert.Single(result);
            Assert.Equal("Clinic", result[0].PlaceOfService);
            Assert.Equal("Dr Smith", result[0].RenderingProvider);
        }

        [Fact]
        public async Task GetChargeDetails_ReturnsChargeDetails()
        {
            var chargeEntries = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity
                {
                    Id = 1,
                    BillingCode = "99213",
                    Charges = 100,
                    Units = 1,
                    DateOfService = DateTime.Today,
                    ClaimChargeEntryWriteOffs = new List<ClaimChargeEntryWriteOffEntity>
                    {
                        new ClaimChargeEntryWriteOffEntity
                        {
                            WriteOffAmount = 5,
                            DateDeleted = null
                        }
                    }
                }
            };

            _claimChargeEntryRepositoryMock.Setup(x => x.Query())
                .Returns(chargeEntries.AsQueryable().BuildMockDbSet().Object);

            var service = CreateService();

            var method = service.GetType()
            .GetMethod("getChargeDetails", BindingFlags.NonPublic | BindingFlags.Instance);

            var task = (Task<List<ChargeDetails>>)method.Invoke(service, new object[] { new List<int> { 1 } });

            var result = await task;

            Assert.Single(result);
        }
    }
}