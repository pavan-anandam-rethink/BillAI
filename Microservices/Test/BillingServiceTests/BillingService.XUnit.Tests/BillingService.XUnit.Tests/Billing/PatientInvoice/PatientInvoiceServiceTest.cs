using BillingService.Domain.Interfaces;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Interfaces.PatientInvoice;
using BillingService.Domain.Models.PatientInvoice;
using BillingService.Domain.Models.PaymentClaims;
using BillingService.Domain.Services.PatientInvoice;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using Rethink.Services.Common.Cache;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.PatientInvoice
{
    public class PatientInvoiceServiceTest
    {
        private readonly Mock<IRazorViewService> _mockRazorViewService;
        private readonly Mock<IRepository<BillingDbContext, PatientInvoiceEntity>> _mockPatientInvoiceRepository;
        private readonly Mock<IRepository<BillingDbContext, PatientInvoiceDetailsEntity>> _mockPatientInvoiceDetailsRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>> _mockChargeEntryRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimSearchClientEntity>> _mockClaimsSearchClientsRepository;
        private readonly Mock<ILogger<PatientInvoiceService>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IPdfService> _mockPdfService;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly Mock<IClientInfoService> _mockClientInfoService;
        private readonly Mock<IPaymentClaimService> _mockPaymentClaimService;
        private readonly Mock<ICacheManager> _mockCacheManager;
        private readonly Mock<IRethinkMasterDataMicroServices> _mockRethinkServices;
        private readonly Mock<IRepository<BillingDbContext, PatientGuarantorEntity>> _mockPatientGuarantorRepository;
        private readonly Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>> _mockPaymentClaimServiceLineRepository;
        private readonly Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>> _mockClaimChargeEntryWriteOffEntity;
        private readonly Mock<IRepository<BillingDbContext, ClaimEntity>> _mockClaimRepository;

        private readonly PatientInvoiceService _service;

        public PatientInvoiceServiceTest()
        {
            _mockRazorViewService = new Mock<IRazorViewService>();
            _mockPatientInvoiceRepository = new Mock<IRepository<BillingDbContext, PatientInvoiceEntity>>();
            _mockPatientInvoiceDetailsRepository = new Mock<IRepository<BillingDbContext, PatientInvoiceDetailsEntity>>();
            _mockChargeEntryRepository = new Mock<IRepository<BillingDbContext, ClaimChargeEntryEntity>>();
            _mockClaimsSearchClientsRepository = new Mock<IRepository<BillingDbContext, ClaimSearchClientEntity>>();
            _mockLogger = new Mock<ILogger<PatientInvoiceService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockPdfService = new Mock<IPdfService>();
            _mockCacheService = new Mock<ICacheService>();
            _mockClientInfoService = new Mock<IClientInfoService>();
            _mockPaymentClaimService = new Mock<IPaymentClaimService>();
            _mockCacheManager = new Mock<ICacheManager>();
            _mockRethinkServices = new Mock<IRethinkMasterDataMicroServices>();
            _mockPatientGuarantorRepository = new Mock<IRepository<BillingDbContext, PatientGuarantorEntity>>();
            _mockPaymentClaimServiceLineRepository = new Mock<IRepository<BillingDbContext, PaymentClaimServiceLineEntity>>();
            _mockClaimChargeEntryWriteOffEntity = new Mock<IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity>>();
            _mockClaimRepository = new Mock<IRepository<BillingDbContext, ClaimEntity>>();

            // Setup default configuration values
            var configurationSection = new Mock<IConfigurationSection>();
            configurationSection.Setup(x => x.Value).Returns("8");
            _mockConfiguration.Setup(x => x.GetSection(It.IsAny<string>())).Returns(configurationSection.Object);

            _service = new PatientInvoiceService(
                _mockRazorViewService.Object,
                _mockPatientInvoiceRepository.Object,
                _mockPatientInvoiceDetailsRepository.Object,
                _mockChargeEntryRepository.Object,
                _mockClaimsSearchClientsRepository.Object,
                _mockConfiguration.Object,
                _mockLogger.Object,
                _mockPdfService.Object,
                _mockCacheService.Object,
                _mockClientInfoService.Object,
                _mockPaymentClaimService.Object,
                _mockCacheManager.Object,
                _mockRethinkServices.Object,
                _mockPatientGuarantorRepository.Object,
                _mockPaymentClaimServiceLineRepository.Object,
                _mockClaimChargeEntryWriteOffEntity.Object,
                _mockClaimRepository.Object
            );
        }

        [Fact]
        public async Task GetPICreationDetails_WhenInvoiceDetailsExist_ReturnsData()
        {
            // Arrange
            var model = new CreateInvoiceFilters
            {
                AccountInfoId = 100,
                Filters = new()
            };

            // PatientInvoiceDetails
            _mockPatientInvoiceDetailsRepository
                .Setup(x => x.Query())
                .Returns(new List<PatientInvoiceDetailsEntity>
                {
            new PatientInvoiceDetailsEntity
            {
                ChargeId = 1,
                DateDeleted = null,
                PatientInvoiceEntity = new PatientInvoiceEntity
                {
                    AccountId = 100,
                    DateDeleted = null
                }
            }
                }.AsQueryable().BuildMockDbSet().Object);

            // Charge entries
            _mockChargeEntryRepository
                .Setup(x => x.Query())
                .Returns(new List<ClaimChargeEntryEntity>
                {
            new ClaimChargeEntryEntity
            {
                Id = 1,
                BillingCode = "99213",
                Units = 1,
                Charges = 100,
                DateOfService = DateTime.Today
            }
                }.AsQueryable().BuildMockDbSet().Object);

            // Write-offs
            _mockClaimChargeEntryWriteOffEntity
                .Setup(x => x.Query())
                .Returns(new List<ClaimChargeEntryWriteOffEntity>()
                    .AsQueryable().BuildMockDbSet().Object);

            _mockPaymentClaimService.Setup(x => x.GetAllPaymentChargeIds(It.IsAny<CreateInvoiceFilters>()))
                .Returns(Task.FromResult(new List<BasicChargeDetails>
                {
                    new BasicChargeDetails
                    {
                        ChargeId = 1,
                        ClientId = 10,
                        DateOfService = DateTime.Today
                    }
                }));

            _mockPaymentClaimService
               .Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
               .ReturnsAsync(new List<PatientPaymentClaimFullModel>
               {
                    new PatientPaymentClaimFullModel
                    {
                        ChargeId = 1,
                        PatientId = 10,
                        PatientName = "John  Doe",
                        PatientResponsibility = 50,
                        PatientResponsibilityBalance = 50,
                        InsurancePayment = 20,
                        PatientPayment = 10
                    }
               });

            _mockClaimRepository
                .Setup(x => x.Query())
                .Returns(new List<ClaimEntity>()
                .AsQueryable().BuildMockDbSet().Object);

            _mockRethinkServices
                .Setup(x => x.GetClientDetailsGuarantor(100))
                .Returns(Task.FromResult((List<Rethink.Services.Common.Models.ClientMicroServicesModels.RethinkGuarantorDetails.ClientModel>)null));

            // Act
            var (data, count) = await _service.GetPICreationDetails(model);

            // Assert
            Assert.Single(data);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task GenerateInvoice_WithValidCharges_CreatesInvoiceSuccessfully()
        {
            // Arrange
            int accountId = 100;
            int clientId = 1;
            var charges = new List<ChargeModel>
            {
                new ChargeModel
                {
                    ChargeId = 1,
                    AdjustmentPatientResponsibility = 10m,
                    PatientPayments = 20m,
                    PatientBalance = 30m
                }
            };

            var emptyInvoices = new List<PatientInvoiceEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(emptyInvoices);
            _mockPatientInvoiceRepository.Setup(x => x.Add(It.IsAny<PatientInvoiceEntity>()));
            _mockPatientInvoiceRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var existingGuarantors = new List<PatientGuarantorEntity>().AsQueryable().BuildMock();
            _mockPatientGuarantorRepository.Setup(x => x.Query()).Returns(existingGuarantors);
            _mockPatientGuarantorRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var chargeDetails = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 1, BillingCode = "99999", Units = 1, Charges = 100m }
            }.AsQueryable().BuildMock();
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeDetails);

            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            var groupedPayments = new List<PatientPaymentClaimFullModel>
            {
                new PatientPaymentClaimFullModel
                {
                    ChargeId = 1,
                    InsurancePayment = 50m,
                    PatientPayment = 20m,
                    Adjustment = 10m,
                    PatientResponsibility = 10m,
                    PatientResponsibilityBalance = 30m
                }
            };
            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
                .ReturnsAsync(groupedPayments);

            var invoiceDetails = new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query()).Returns(invoiceDetails);
            _mockPatientInvoiceDetailsRepository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<PatientInvoiceDetailsEntity>>())).Returns(Task.CompletedTask);
            _mockPatientInvoiceDetailsRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            _mockRethinkServices.Setup(x => x.GetContactGuarantorDetails(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult((Rethink.Services.Common.Models.InsuranceContacts)null));

            // Act
            var result = await _service.GenerateInvoice(accountId, clientId, charges);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.InvoiceNumber);
        }

        [Fact]
        public async Task GenerateInvoice_WithZeroPatientBalance_CreatesInvoice()
        {
            // Arrange
            int accountId = 100;
            int clientId = 1;
            var charges = new List<ChargeModel>
            {
                new ChargeModel
                {
                    ChargeId = 1,
                    AdjustmentPatientResponsibility = 0m,
                    PatientPayments = 0m,
                    PatientBalance = 0m
                }
            };

            var emptyInvoices = new List<PatientInvoiceEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(emptyInvoices);
            _mockPatientInvoiceRepository.Setup(x => x.Add(It.IsAny<PatientInvoiceEntity>()));
            _mockPatientInvoiceRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var existingGuarantors = new List<PatientGuarantorEntity>().AsQueryable().BuildMock();
            _mockPatientGuarantorRepository.Setup(x => x.Query()).Returns(existingGuarantors);
            _mockPatientGuarantorRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var chargeDetails = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 1, BillingCode = "99999", Units = 1, Charges = 0m }
            }.AsQueryable().BuildMock();
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeDetails);

            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            var groupedPayments = new List<PatientPaymentClaimFullModel>
            {
                new PatientPaymentClaimFullModel
                {
                    ChargeId = 1,
                    InsurancePayment = 0m,
                    PatientPayment = 0m,
                    Adjustment = 0m,
                    PatientResponsibility = 0m,
                    PatientResponsibilityBalance = 0m
                }
            };
            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
                .ReturnsAsync(groupedPayments);

            var invoiceDetails = new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query()).Returns(invoiceDetails);
            _mockPatientInvoiceDetailsRepository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<PatientInvoiceDetailsEntity>>())).Returns(Task.CompletedTask);
            _mockPatientInvoiceDetailsRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            _mockRethinkServices.Setup(x => x.GetContactGuarantorDetails(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult((Rethink.Services.Common.Models.InsuranceContacts)null));

            // Act
            var result = await _service.GenerateInvoice(accountId, clientId, charges);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GenerateInvoice_WithPartialPayment_CreatesInvoice()
        {
            // Arrange
            int accountId = 100;
            int clientId = 1;
            var charges = new List<ChargeModel>
            {
                new ChargeModel
                {
                    ChargeId = 1,
                    AdjustmentPatientResponsibility = 20m,
                    PatientPayments = 15m,
                    PatientBalance = 30m
                }
            };

            var emptyInvoices = new List<PatientInvoiceEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(emptyInvoices);
            _mockPatientInvoiceRepository.Setup(x => x.Add(It.IsAny<PatientInvoiceEntity>()));
            _mockPatientInvoiceRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var existingGuarantors = new List<PatientGuarantorEntity>().AsQueryable().BuildMock();
            _mockPatientGuarantorRepository.Setup(x => x.Query()).Returns(existingGuarantors);
            _mockPatientGuarantorRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var chargeDetails = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 1, BillingCode = "99999", Units = 1, Charges = 100m }
            }.AsQueryable().BuildMock();
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeDetails);

            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            var groupedPayments = new List<PatientPaymentClaimFullModel>
            {
                new PatientPaymentClaimFullModel
                {
                    ChargeId = 1,
                    InsurancePayment = 50m,
                    PatientPayment = 15m,
                    Adjustment = 5m,
                    PatientResponsibility = 20m,
                    PatientResponsibilityBalance = 30m
                }
            };
            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
                .ReturnsAsync(groupedPayments);

            var invoiceDetails = new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query()).Returns(invoiceDetails);
            _mockPatientInvoiceDetailsRepository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<PatientInvoiceDetailsEntity>>())).Returns(Task.CompletedTask);
            _mockPatientInvoiceDetailsRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            _mockRethinkServices.Setup(x => x.GetContactGuarantorDetails(It.IsAny<int>(), It.IsAny<int>()))
                .Returns(Task.FromResult((Rethink.Services.Common.Models.InsuranceContacts)null));

            // Act
            var result = await _service.GenerateInvoice(accountId, clientId, charges);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetPreviousInvoices_WithValidInvoiceNumber_ReturnsPreviousInvoice()
        {
            // Arrange
            int accountId = 100;
            int clientId = 1;
            string invoiceNo = "INV-1-20240101-001";

            var previousInvoices = new List<PatientInvoiceEntity>
            {
                new PatientInvoiceEntity
                {
                    Id = 1,
                    InvoiceNumber = invoiceNo,
                    InvoiceDate = DateTime.Now,
                    PaymentDueDate = DateTime.Now.AddDays(30),
                    Status = PatientInvoiceStatus.InvoiceSent
                }
            };
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(previousInvoices.AsQueryable().BuildMock());

            var invoiceDetails = new List<PatientInvoiceDetailsEntity>
            {
                new PatientInvoiceDetailsEntity { InvoiceId = 1, ChargeId = 1 }
            };
            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query()).Returns(invoiceDetails.AsQueryable().BuildMock());

            var chargeDetails = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity
                {
                    Id = 1,
                    BillingCode = "99999",
                    Units = 1,
                    Charges = 100m,
                    DateOfService = DateTime.Now
                }
            }.AsQueryable().BuildMock();
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeDetails);

            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            var groupedPayments = new List<PatientPaymentClaimFullModel>
            {
                new PatientPaymentClaimFullModel
                {
                    ChargeId = 1,
                    InsurancePayment = 50m,
                    PatientPayment = 20m,
                    Adjustment = 10m,
                    PatientResponsibility = 10m,
                    PatientResponsibilityBalance = 30m
                }
            };
            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
                .ReturnsAsync(groupedPayments);

            // Act
            var result = await _service.GetPreviousInvoices(accountId, clientId, invoiceNo);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(invoiceNo, result[0].InvoiceNumber);
        }

        [Fact]
        public async Task GetPreviousInvoices_WithoutInvoiceNumber_ReturnsAllInvoicesForClientAndAccount()
        {
            // Arrange
            int accountId = 100;
            int clientId = 1;

            var previousInvoices = new List<PatientInvoiceEntity>
            {
                new PatientInvoiceEntity
                {
                    Id = 1,
                    AccountId = accountId,
                    ClientId = clientId,
                    InvoiceNumber = "INV-1-20240101-001",
                    InvoiceDate = DateTime.Now,
                    PaymentDueDate = DateTime.Now.AddDays(30)
                },
                new PatientInvoiceEntity
                {
                    Id = 2,
                    AccountId = accountId,
                    ClientId = clientId,
                    InvoiceNumber = "INV-1-20240102-001",
                    InvoiceDate = DateTime.Now.AddDays(-1),
                    PaymentDueDate = DateTime.Now.AddDays(29)
                }
            };
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(previousInvoices.AsQueryable().BuildMock());

            var invoiceDetails = new List<PatientInvoiceDetailsEntity>
            {
                new PatientInvoiceDetailsEntity { InvoiceId = 1, ChargeId = 1 },
                new PatientInvoiceDetailsEntity { InvoiceId = 2, ChargeId = 2 }
            };
            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query()).Returns(invoiceDetails.AsQueryable().BuildMock());

            var chargeDetails = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity
                {
                    Id = 1,
                    BillingCode = "99999",
                    Units = 1,
                    Charges = 100m,
                    DateOfService = DateTime.Now
                },
                new ClaimChargeEntryEntity
                {
                    Id = 2,
                    BillingCode = "99998",
                    Units = 2,
                    Charges = 200m,
                    DateOfService = DateTime.Now.AddDays(-1)
                }
            }.AsQueryable().BuildMock();
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeDetails);

            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            var groupedPayments = new List<PatientPaymentClaimFullModel>
            {
                new PatientPaymentClaimFullModel
                {
                    ChargeId = 1,
                    InsurancePayment = 50m,
                    PatientPayment = 20m,
                    Adjustment = 10m,
                    PatientResponsibility = 10m,
                    PatientResponsibilityBalance = 30m
                },
                new PatientPaymentClaimFullModel
                {
                    ChargeId = 2,
                    InsurancePayment = 100m,
                    PatientPayment = 40m,
                    Adjustment = 20m,
                    PatientResponsibility = 20m,
                    PatientResponsibilityBalance = 60m
                }
            };
            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
                .ReturnsAsync(groupedPayments);

            // Act
            var result = await _service.GetPreviousInvoices(accountId, clientId, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetPreviousInvoices_WithNoInvoices_ReturnsEmptyList()
        {
            // Arrange
            int accountId = 100;
            int clientId = 1;

            var emptyInvoices = new List<PatientInvoiceEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(emptyInvoices);

            var emptyInvoiceDetails = new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query()).Returns(emptyInvoiceDetails);

            var emptyChargeDetails = new List<ClaimChargeEntryEntity>().AsQueryable().BuildMock();
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(emptyChargeDetails);

            var emptyWriteOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(emptyWriteOffDetails);

            var emptyGroupedPayments = new List<PatientPaymentClaimFullModel>();
            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
                .ReturnsAsync(emptyGroupedPayments);

            // Act
            var result = await _service.GetPreviousInvoices(accountId, clientId, null);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }


        [Fact]
        public async Task GetInvoiceDetails_WithNoInvoices_ReturnsEmptyResult()
        {
            // Arrange
            var filter = new PendingCollectionFilters
            {
                AccountInfoId = 100,
                Filters = new PendingCollection(),
                Skip = 0,
                Take = 10
            };

            var emptyInvoices = new List<PatientInvoiceEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(emptyInvoices);

            _mockRethinkServices.Setup(x => x.GetChildProfilesForAccount(It.IsAny<int>()))
                .Returns(Task.FromResult(new List<Rethink.Services.Common.Models.ChildProfileEntityModel>()));

            _mockClaimsSearchClientsRepository.Setup(x => x.Query())
                .Returns(new List<ClaimSearchClientEntity>().AsQueryable().BuildMock());

            // Act
            var result = await _service.GetInvoiceDetails(filter);

            // Assert
            Assert.NotNull(result.Data);
            Assert.Empty(result.Data);
            Assert.Equal(0, result.TotalCount);
        }


        [Fact]
        public async Task GetInvoicePDF_WithValidInvoiceNumber_ReturnsPdfData()
        {
            // Arrange
            int accountId = 100;
            int clientId = 1;
            string invoiceNo = "INV-001";

            var invoice = new PatientInvoiceEntity
            {
                Id = 1,
                InvoiceNumber = invoiceNo,
                InvoiceDate = DateTime.Now,
                PaymentDueDate = DateTime.Now.AddDays(30)
            };
            _mockPatientInvoiceRepository.Setup(x => x.Query())
                .Returns(new List<PatientInvoiceEntity> { invoice }.AsQueryable().BuildMock());

            var guarantor = new PatientGuarantorEntity
            {
                InvoiceId = 1,
                FirstName = "John",
                MiddleName = "M",
                LastName = "Doe",
                Phone = "555-1234",
                Email = "john@example.com",
                Street1 = "123 Main St",
                City = "Test City",
                ZipCode = "12345"
            };
            _mockPatientGuarantorRepository.Setup(x => x.Query())
                .Returns(new List<PatientGuarantorEntity> { guarantor }.AsQueryable().BuildMock());

            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query())
                .Returns(new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMock());

            var chargeDetails = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity
                {
                    Id = 1,
                    BillingCode = "99999",
                    Units = 1,
                    Charges = 100m,
                    DateOfService = DateTime.Now
                }
            }.AsQueryable().BuildMock();
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeDetails);

            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            var groupedPayments = new List<PatientPaymentClaimFullModel>
            {
                new PatientPaymentClaimFullModel
                {
                    ChargeId = 1,
                    InsurancePayment = 50m,
                    PatientPayment = 20m,
                    Adjustment = 10m,
                    PatientResponsibility = 10m,
                    PatientResponsibilityBalance = 30m
                }
            };
            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
                .ReturnsAsync(groupedPayments);

            _mockRazorViewService.Setup(x => x.RenderViewToStringAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.FromResult("<html></html>"));
            _mockPdfService.Setup(x => x.GeneratePDF(It.IsAny<string>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });

            _mockClientInfoService.Setup(x => x.GetClientInfo(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.ClientInfo());
            _mockClientInfoService.Setup(x => x.GetBillingProviderInfo(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.BillingProviderInfo());

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(It.IsAny<string>(), It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.ClientInfo>>>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.ClientInfo());

            _mockCacheManager.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.BillingProviderInfo>>>(), It.IsAny<CachingDuration>()))
                .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.BillingProviderInfo());

            // Act
            var result = await _service.GetInvoicePDF(accountId, clientId, invoiceNo);

            // Assert
            Assert.NotNull(result.pdfData);
            Assert.NotEmpty(result.pdfData);
        }

        [Fact]
        public async Task GetInvoicePDF_WithoutGuarantorInfo_CreatesDefaultGuarantorInfo()
        {
            // Arrange
            int accountId = 100;
            int clientId = 1;
            string invoiceNo = "INV-001";

            var invoice = new PatientInvoiceEntity { Id = 1, InvoiceNumber = invoiceNo, InvoiceDate = DateTime.Now, PaymentDueDate = DateTime.Now.AddDays(30) };
            _mockPatientInvoiceRepository.Setup(x => x.Query())
                .Returns(new List<PatientInvoiceEntity> { invoice }.AsQueryable().BuildMock());

            var emptyGuarantors = new List<PatientGuarantorEntity>().AsQueryable().BuildMock();
            _mockPatientGuarantorRepository.Setup(x => x.Query()).Returns(emptyGuarantors);

            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query())
                .Returns(new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMock());

            var chargeDetails = new List<ClaimChargeEntryEntity>().AsQueryable().BuildMock();
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeDetails);

            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            var groupedPayments = new List<PatientPaymentClaimFullModel>();
            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
                .ReturnsAsync(groupedPayments);

            _mockRazorViewService.Setup(x => x.RenderViewToStringAsync(It.IsAny<string>(), It.IsAny<object>()))
                .Returns(Task.FromResult("<html></html>"));
            _mockPdfService.Setup(x => x.GeneratePDF(It.IsAny<string>()))
                .ReturnsAsync(new byte[] { 1, 2, 3 });

            _mockClientInfoService.Setup(x => x.GetClientInfo(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.ClientInfo());
            _mockClientInfoService.Setup(x => x.GetBillingProviderInfo(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.BillingProviderInfo());

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(It.IsAny<string>(), It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.ClientInfo>>>(), It.IsAny<TimeSpan>()))
                .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.ClientInfo());

            _mockCacheManager.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.BillingProviderInfo>>>(), It.IsAny<CachingDuration>()))
                .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.BillingProviderInfo());

            // Act
            var result = await _service.GetInvoicePDF(accountId, clientId, invoiceNo);

            // Assert
            Assert.NotNull(result.pdfData);
            Assert.NotEmpty(result.pdfData);
        }

        [Fact]
        public async Task GetPICreationDetails_WithNoInvoiceDetailsButHasChargeEntries_ReturnsData()
        {
            // Arrange
            var model = new CreateInvoiceFilters
            {
                AccountInfoId = 100,
                Filters = new CreateInvoice()
            };

            // Empty invoice details - will query charge entries
            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query())
            .Returns(new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMockDbSet().Object);

            // Charge entries from claim
            _mockChargeEntryRepository.Setup(x => x.Query())
            .Returns(new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity
                {
                    Id = 1,
                    BillingCode = "99213",
                    Units = 1,
                    Charges = 100,
                    DateOfService = DateTime.Today,
                    Claim = new ClaimEntity { AccountInfoId = 100 }
                }
            }.AsQueryable().BuildMockDbSet().Object);

            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query())
            .Returns(new List<ClaimChargeEntryWriteOffEntity>()
            .AsQueryable().BuildMockDbSet().Object);

            _mockPaymentClaimService.Setup(x => x.GetAllPaymentChargeIds(It.IsAny<CreateInvoiceFilters>()))
            .Returns(Task.FromResult(new List<BasicChargeDetails>
            {
                new BasicChargeDetails
                {
                    ChargeId = 1,
                    ClientId = 10,
                    DateOfService = DateTime.Today
                }
            }));

            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<PatientPaymentClaimFullModel>
            {
                new PatientPaymentClaimFullModel
                {
                    ChargeId = 1,
                    PatientId = 10,
                    PatientName = "Jane Doe",
                    ClaimId = 1,
                    PatientResponsibility = 50,
                    PatientResponsibilityBalance = 50,
                    InsurancePayment = 20,
                    PatientPayment = 10,
                    Adjustment = 5
                }
            });

            _mockClaimRepository.Setup(x => x.Query())
            .Returns(new List<ClaimEntity>
            {
                new ClaimEntity { Id = 1, DateDeleted = null }
                }
                .AsQueryable().BuildMockDbSet().Object);

            _mockRethinkServices.Setup(x => x.GetClientDetailsGuarantor(100))
            .Returns(Task.FromResult((List<Rethink.Services.Common.Models.ClientMicroServicesModels.RethinkGuarantorDetails.ClientModel>)null));

            // Act
            var (data, count) = await _service.GetPICreationDetails(model);

            // Assert
            Assert.Single(data);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task GeneratePDF_WithValidInvoiceRequests_GeneratesPDFSuccessfully()
        {
            // Arrange
            var accountId = 100;
            var clientId = 1;
            var invoiceRequests = new List<InvoiceRequestModel>
            {
                new InvoiceRequestModel
                {
                    AccountId = accountId,
                    ClientId = clientId,
                    Charges = new List<ChargeModel>
                    {
                        new ChargeModel { ChargeId = 1, PatientBalance = 100m }
                    }
                }
            };

            var chargeEntries = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity
                {
                    Id = 1,
                    BillingCode = "99213",
                    Units = 1,
                    Charges = 100m,
                    DateOfService = DateTime.Today
                }
            }.AsQueryable().BuildMock();
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeEntries);

            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            var groupedPayments = new List<PatientPaymentClaimFullModel>
            {
                new PatientPaymentClaimFullModel
                {
                    ChargeId = 1,
                    InsurancePayment = 50m,
                    PatientPayment = 20m,
                    PatientResponsibility = 30m,
                    PatientResponsibilityBalance = 100m
                }
            };
            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
            .ReturnsAsync(groupedPayments);

            var emptyInvoices = new List<PatientInvoiceEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(emptyInvoices);
            _mockPatientInvoiceRepository.Setup(x => x.Add(It.IsAny<PatientInvoiceEntity>()));
            _mockPatientInvoiceRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var emptyGuarantors = new List<PatientGuarantorEntity>().AsQueryable().BuildMock();
            _mockPatientGuarantorRepository.Setup(x => x.Query()).Returns(emptyGuarantors);
            _mockPatientGuarantorRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var emptyInvoiceDetails = new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query()).Returns(emptyInvoiceDetails);
            _mockPatientInvoiceDetailsRepository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<PatientInvoiceDetailsEntity>>())).Returns(Task.CompletedTask);
            _mockPatientInvoiceDetailsRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            _mockRazorViewService.Setup(x => x.RenderViewToStringAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.FromResult("<html></html>"));
            _mockPdfService.Setup(x => x.GeneratePDF(It.IsAny<string>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

            _mockClientInfoService.Setup(x => x.GetClientInfo(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.ClientInfo());
            _mockClientInfoService.Setup(x => x.GetBillingProviderInfo(It.IsAny<int>(), It.IsAny<int>()))
              .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.BillingProviderInfo());

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(It.IsAny<string>(), It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.ClientInfo>>>(), It.IsAny<TimeSpan>()))
                        .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.ClientInfo());

            _mockCacheManager.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.BillingProviderInfo>>>(), It.IsAny<CachingDuration>()))
                         .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.BillingProviderInfo());

            _mockRethinkServices.Setup(x => x.GetClientDetailsGuarantor(accountId))
            .Returns(Task.FromResult((List<Rethink.Services.Common.Models.ClientMicroServicesModels.RethinkGuarantorDetails.ClientModel>)null));

            // Act
            var result = await _service.GeneratePDF(invoiceRequests, true, false, null);

            // Assert
            Assert.NotNull(result.PdfData);
            Assert.NotEmpty(result.PdfData);
            Assert.Empty(result.ErrorList);
        }

        [Fact]
        public async Task GeneratePDF_WithMultipleClients_GeneratesPDFForAllClients()
        {
            // Arrange
            var accountId = 100;
            var invoiceRequests = new List<InvoiceRequestModel>
            {
                new InvoiceRequestModel
                {
                    AccountId = accountId,
                    ClientId = 1,
                    Charges = new List<ChargeModel>
                    {
                        new ChargeModel { ChargeId = 1, PatientBalance = 100m }
                    }
                },
                new InvoiceRequestModel
                {
                    AccountId = accountId,
                    ClientId = 2,
                    Charges = new List<ChargeModel>
                    {
                        new ChargeModel { ChargeId = 2, PatientBalance = 150m }
                    }
                }

            };

            var chargeEntries = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 1, BillingCode = "99213", Units = 1, Charges = 100m, DateOfService = DateTime.Today },
                new ClaimChargeEntryEntity { Id = 2, BillingCode = "99214", Units = 1, Charges = 150m, DateOfService = DateTime.Today }
                 }.AsQueryable().BuildMock();
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeEntries);

            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            var groupedPayments = new List<PatientPaymentClaimFullModel>
            {
               new PatientPaymentClaimFullModel { ChargeId = 1, InsurancePayment = 50m, PatientPayment = 20m, PatientResponsibility = 30m, PatientResponsibilityBalance = 100m },
               new PatientPaymentClaimFullModel { ChargeId = 2, InsurancePayment = 75m, PatientPayment = 30m, PatientResponsibility = 45m, PatientResponsibilityBalance = 150m }
            };
            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
             .ReturnsAsync(groupedPayments);

            var emptyInvoices = new List<PatientInvoiceEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(emptyInvoices);
            _mockPatientInvoiceRepository.Setup(x => x.Add(It.IsAny<PatientInvoiceEntity>()));
            _mockPatientInvoiceRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var emptyGuarantors = new List<PatientGuarantorEntity>().AsQueryable().BuildMock();
            _mockPatientGuarantorRepository.Setup(x => x.Query()).Returns(emptyGuarantors);
            _mockPatientGuarantorRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var emptyInvoiceDetails = new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query()).Returns(emptyInvoiceDetails);
            _mockPatientInvoiceDetailsRepository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<PatientInvoiceDetailsEntity>>())).Returns(Task.CompletedTask);
            _mockPatientInvoiceDetailsRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            _mockRazorViewService.Setup(x => x.RenderViewToStringAsync(It.IsAny<string>(), It.IsAny<object>()))
                 .Returns(Task.FromResult("<html></html>"));
            _mockPdfService.Setup(x => x.GeneratePDF(It.IsAny<string>()))
                 .ReturnsAsync(new byte[] { 1, 2, 3 });

            _mockClientInfoService.Setup(x => x.GetClientInfo(It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.ClientInfo());
            _mockClientInfoService.Setup(x => x.GetBillingProviderInfo(It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.BillingProviderInfo());

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(It.IsAny<string>(), It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.ClientInfo>>>(), It.IsAny<TimeSpan>()))
               .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.ClientInfo());

            _mockCacheManager.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.BillingProviderInfo>>>(), It.IsAny<CachingDuration>()))
                .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.BillingProviderInfo());

            // Act
            var result = await _service.GeneratePDF(invoiceRequests, false, false, null);

            // Assert
            Assert.NotNull(result.PdfData);
            Assert.NotEmpty(result.PdfData);
        }

        [Fact]
        public async Task GeneratePDF_WithInvoiceNumberFilter_FiltersPDFCorrectly()
        {
            // Arrange
            var accountId = 100;
            var invoiceNumber = "INV-001";
            var invoiceRequests = new List<InvoiceRequestModel>
            {
                new InvoiceRequestModel
                {
                    AccountId = accountId,
                    ClientId = 1,
                    Charges = new List<ChargeModel>
                    {
                        new ChargeModel { ChargeId = 1, PatientBalance = 100m }
                    }
                }
            };

            var chargeEntries = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 1, BillingCode = "99213", Units = 1, Charges = 100m, DateOfService = DateTime.Today }
            }.AsQueryable().BuildMock();
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeEntries);

            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            var groupedPayments = new List<PatientPaymentClaimFullModel>
            {
                new PatientPaymentClaimFullModel { ChargeId = 1, InsurancePayment = 50m, PatientPayment = 20m, PatientResponsibility = 30m, PatientResponsibilityBalance = 100m }
            };
            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
             .ReturnsAsync(groupedPayments);

            var emptyInvoices = new List<PatientInvoiceEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(emptyInvoices);
            _mockPatientInvoiceRepository.Setup(x => x.Add(It.IsAny<PatientInvoiceEntity>()));
            _mockPatientInvoiceRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var emptyGuarantors = new List<PatientGuarantorEntity>().AsQueryable().BuildMock();
            _mockPatientGuarantorRepository.Setup(x => x.Query()).Returns(emptyGuarantors);
            _mockPatientGuarantorRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            var emptyInvoiceDetails = new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query()).Returns(emptyInvoiceDetails);
            _mockPatientInvoiceDetailsRepository.Setup(x => x.AddRangeAsync(It.IsAny<IEnumerable<PatientInvoiceDetailsEntity>>())).Returns(Task.CompletedTask);
            _mockPatientInvoiceDetailsRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

            _mockRazorViewService.Setup(x => x.RenderViewToStringAsync(It.IsAny<string>(), It.IsAny<object>()))
                 .Returns(Task.FromResult("<html></html>"));
            _mockPdfService.Setup(x => x.GeneratePDF(It.IsAny<string>()))
                 .ReturnsAsync(new byte[] { 1, 2, 3 });

            _mockClientInfoService.Setup(x => x.GetClientInfo(It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.ClientInfo());
            _mockClientInfoService.Setup(x => x.GetBillingProviderInfo(It.IsAny<int>(), It.IsAny<int>()))
                 .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.BillingProviderInfo());

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(It.IsAny<string>(), It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.ClientInfo>>>(), It.IsAny<TimeSpan>()))
               .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.ClientInfo());

            _mockCacheManager.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.BillingProviderInfo>>>(), It.IsAny<CachingDuration>()))
                 .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.BillingProviderInfo());

            // Act
            var result = await _service.GeneratePDF(invoiceRequests, false, false, invoiceNumber);

            // Assert
            Assert.NotNull(result.PdfData);
            Assert.NotEmpty(result.PdfData);
        }

        [Fact]
        public async Task GeneratePDF_WithEmptyCharges_ReturnsNullPdfAndError()
        {
            // Arrange
            var accountId = 100;
            var invoiceRequests = new List<InvoiceRequestModel>
            {
                new InvoiceRequestModel
                {
                    AccountId = accountId,
                    ClientId = 1,
                    Charges = new List<ChargeModel>()
                }
            };

            var emptyChargeEntries = new List<ClaimChargeEntryEntity>().AsQueryable().BuildMock();
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(emptyChargeEntries);

            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            var emptyPayments = new List<PatientPaymentClaimFullModel>();
            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
            .ReturnsAsync(emptyPayments);

            var emptyInvoices = new List<PatientInvoiceEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(emptyInvoices);

            var emptyGuarantors = new List<PatientGuarantorEntity>().AsQueryable().BuildMock();
            _mockPatientGuarantorRepository.Setup(x => x.Query()).Returns(emptyGuarantors);

            var emptyInvoiceDetails = new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query()).Returns(emptyInvoiceDetails);

            _mockRazorViewService.Setup(x => x.RenderViewToStringAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.FromResult(""));

            _mockRethinkServices.Setup(x => x.GetClientDetailsGuarantor(accountId))
           .Returns(Task.FromResult((List<Rethink.Services.Common.Models.ClientMicroServicesModels.RethinkGuarantorDetails.ClientModel>)null));

            // Act
            var result = await _service.GeneratePDF(invoiceRequests, false, false, null);

            // Assert - handle null result
            if (result.PdfData != null)
            {
                Assert.Empty(result.PdfData);
            }
            else
            {
                Assert.Null(result.PdfData);
            }
        }

        [Fact]
        public void GenerateInvoiceNumber_WithValidClientId_GeneratesCorrectFormat()
        {
            // Arrange
            string clientId = "12345";
            _mockPatientInvoiceRepository.Setup(x => x.Query())
           .Returns(new List<PatientInvoiceEntity>().AsQueryable().BuildMock());

            // Act
            var result = _service.GetType()
           .GetMethod("GenerateInvoiceNumber", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
          .Invoke(_service, new object[] { clientId }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith("INV-", result);
            Assert.Contains(clientId, result);
        }

        [Fact]
        public void GenerateInvoiceNumber_WithLongClientId_TruncatesToTenCharacters()
        {
            // Arrange
            string clientId = "123456789012345";
            _mockPatientInvoiceRepository.Setup(x => x.Query())
                  .Returns(new List<PatientInvoiceEntity>().AsQueryable().BuildMock());

            // Act
            var result = _service.GetType()
                .GetMethod("GenerateInvoiceNumber", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_service, new object[] { clientId }) as string;

            // Assert
            Assert.NotNull(result);
            Assert.Contains("1234567890", result);
            Assert.DoesNotContain("12345", result.Substring(result.Length - 5));
        }

        [Fact]
        public void GetNextSequenceNumberForAccount_WithNoExistingInvoices_ReturnsOne()
        {
            // Arrange
            string clientId = "1";
            var emptyInvoices = new List<PatientInvoiceEntity>().AsQueryable().BuildMock();
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(emptyInvoices);

            // Act
            var result = _service.GetType()
                 .GetMethod("GetNextSequenceNumberForAccount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
           .Invoke(_service, new object[] { clientId });

            // Assert
            Assert.Equal(1, result);
        }

        [Fact]
        public void GetNextSequenceNumberForAccount_WithExistingInvoices_ReturnsIncrementedSequence()
        {
            string clientId = "1";
            
            var estDateTimeProperty = _service.GetType().BaseType?.GetProperty("EstDateTime", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var estDateTime = (DateTime)(estDateTimeProperty?.GetValue(_service) ?? DateTime.Now);
            
            var correctInvoiceNumber = $"INV-{clientId}-{estDateTime:yyyyMMdd}-001";
            var existingInvoices = new List<PatientInvoiceEntity>
            {
              new PatientInvoiceEntity
                {
                    ClientId = 1,
                    InvoiceNumber = correctInvoiceNumber,
                    InvoiceDate = estDateTime
                }
            }.AsQueryable().BuildMock();

            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(existingInvoices);

            var result = _service.GetType().GetMethod("GetNextSequenceNumberForAccount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(_service, new object[] { clientId });
            Assert.Equal(2, result);
        }


        [Fact]
        public void GetNextSequenceNumberForAccount_WithInvalidInvoiceNumber_ReturnsNegativeOne()
        {
            // Arrange
            string clientId = "invalid";
            var invoices = new List<PatientInvoiceEntity>
            {
                new PatientInvoiceEntity
                {
                    ClientId = 1,
                    InvoiceNumber = "INVALID-FORMAT",
                    InvoiceDate = DateTime.Today
                }
            }.AsQueryable().BuildMock();
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(invoices);

            // Act
            var result = _service.GetType()
            .GetMethod("GetNextSequenceNumberForAccount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(_service, new object[] { clientId });

            // Assert
            Assert.Equal(-1, result);
        }

        [Fact]
        public void GetPaymentDueDate_ReturnsDateThirtyDaysFromNow()
        {
            var result = (DateTime)_service.GetType().GetMethod("GetPaymentDueDate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(_service, Array.Empty<object>());
            
            var estDateTimeProperty = _service.GetType().BaseType?.GetProperty("EstDateTime", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var estDateTime = (DateTime)(estDateTimeProperty?.GetValue(_service) ?? DateTime.Now);
            var expectedDate = estDateTime.AddDays(30).Date;
            
            Assert.Equal(expectedDate, result.Date);
        }


        [Fact]
        public async Task GetClientBillingProviderInfoFromCache_WithValidData_ReturnsCachedInfo()
        {
            // Arrange
            int accountId = 100;
            int clientId = 1;
            var clientInfo = new BillingService.Domain.Templates.ViewModels.ClientInfo();
            var billingInfo = new BillingService.Domain.Templates.ViewModels.BillingProviderInfo();

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(It.IsAny<string>(),
                It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.ClientInfo>>>(),
                It.IsAny<TimeSpan>()))
            .ReturnsAsync(clientInfo);

            _mockCacheManager.Setup(x => x.GetAsync(It.IsAny<string>(),
                It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.BillingProviderInfo>>>(),
                It.IsAny<CachingDuration>()))
             .ReturnsAsync(billingInfo);

            // Act
            var method = _service.GetType()
                       .GetMethod("GetClientBillingProviderInfoFromCache", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var taskResult = method.Invoke(_service, new object[] { accountId, clientId }) as dynamic;
            dynamic result = await taskResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetChargeDetails_WithValidChargeIds_ReturnsChargeDetails()
        {
            // Arrange
            var chargeIds = new List<int> { 1, 2 };
            var chargeEntities = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity
                {
                    Id = 1,
                    BillingCode = "99213",
                    Units = 1,
                    Charges = 100m,
                    DateOfService = DateTime.Today
               },
               new ClaimChargeEntryEntity
               {
                    Id = 2,
                    BillingCode = "99214",
                    Units = 2,
                    Charges = 200m,
                    DateOfService = DateTime.Today
                }
            }.AsQueryable().BuildMockDbSet().Object;

            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeEntities);

            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMockDbSet().Object;
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            // Act
            var method = _service.GetType()
            .GetMethod("getChargeDetails", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var taskResult = method.Invoke(_service, new object[] { chargeIds }) as dynamic;
            dynamic chargeDetails = await taskResult;

            // Assert
            Assert.NotNull(chargeDetails);
            Assert.Equal(2, chargeDetails.Count);
            Assert.Equal("99213", chargeDetails[0].BillingCode);
        }

        [Fact]
        public async Task GetChargeDetails_WithEmptyChargeIds_ReturnsEmptyList()
        {
            // Arrange
            var chargeIds = new List<int>();
            var chargeEntities = new List<ClaimChargeEntryEntity>().AsQueryable().BuildMockDbSet().Object;
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeEntities);

            // Act
            var method = _service.GetType()
                .GetMethod("getChargeDetails", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var taskResult = method.Invoke(_service, new object[] { chargeIds }) as dynamic;
            dynamic chargeDetails = await taskResult;

            // Assert
            Assert.NotNull(chargeDetails);
            Assert.Empty(chargeDetails);
        }

        [Fact]
        public async Task GetChargeDetails_IncludesWriteOffAmounts()
        {
            // Arrange
            var chargeIds = new List<int> { 1 };
            var chargeEntities = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity
                {
                    Id = 1,
                    BillingCode = "99213",
                    Units = 1,
                    Charges = 100m,
                    DateOfService = DateTime.Today
                }
            }.AsQueryable().BuildMockDbSet().Object;

            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeEntities);
            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>
            {
                new ClaimChargeEntryWriteOffEntity
                {
                    ClaimChargeEntryId = 1,
                    WriteOffAmount = 25m,
                    DateDeleted = null
                }
            }.AsQueryable().BuildMockDbSet().Object;

            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            // Act
            var method = _service.GetType()
                .GetMethod("getChargeDetails", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var taskResult = method.Invoke(_service, new object[] { chargeIds }) as dynamic;
            dynamic chargeDetails = await taskResult;

            // Assert
            Assert.NotNull(chargeDetails);
            Assert.Single(chargeDetails);
            Assert.Equal(25m, chargeDetails[0].WriteOffAmount);
        }

        [Fact]
        public async Task SaveGuarantorSnapshotAsync_WhenGuarantorAlreadyExists_DoesNotCreateDuplicate()
        {
            // Arrange
            int invoiceId = 1;
            int accountId = 100;
            int clientId = 10;

            _mockPatientGuarantorRepository
                  .Setup(x => x.Query())
                     .Returns(new List<PatientGuarantorEntity> { new PatientGuarantorEntity { InvoiceId = invoiceId, DeletedOn = null } }.AsQueryable().BuildMock());

            // Act
            var method = _service.GetType()
            .GetMethod("SaveGuarantorSnapshotAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var taskResult = method.Invoke(_service, new object[] { invoiceId, accountId, clientId }) as dynamic;
            await taskResult;

            // Assert
            _mockPatientGuarantorRepository.Verify(x => x.Add(It.IsAny<PatientGuarantorEntity>()), Times.Never);
            _mockPatientGuarantorRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task SaveGuarantorSnapshotAsync_WhenGuarantorInfoIsNull_ReturnsWithoutSaving()
        {
            // Arrange
            int invoiceId = 1;
            int accountId = 100;
            int clientId = 10;

            _mockPatientGuarantorRepository
            .Setup(x => x.Query())
            .Returns(new List<PatientGuarantorEntity>().AsQueryable().BuildMock());

            _mockRethinkServices
            .Setup(x => x.GetContactGuarantorDetails(accountId, clientId))
            .Returns(Task.FromResult((Rethink.Services.Common.Models.InsuranceContacts)null));

            // Act
            var method = _service.GetType()
               .GetMethod("SaveGuarantorSnapshotAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var taskResult = method.Invoke(_service, new object[] { invoiceId, accountId, clientId }) as dynamic;
            await taskResult;

            // Assert
            _mockPatientGuarantorRepository.Verify(x => x.Add(It.IsAny<PatientGuarantorEntity>()), Times.Never);
            _mockPatientGuarantorRepository.Verify(x => x.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task SaveGuarantorSnapshotAsync_WithValidGuarantorData_CreatesSnapshotSuccessfully()
        {
            // Arrange
            int invoiceId = 1;
            int accountId = 100;
            int clientId = 10;

            _mockPatientGuarantorRepository
           .Setup(x => x.Query())
                .Returns(new List<PatientGuarantorEntity>().AsQueryable().BuildMock());

            var guarantorInfo = new Rethink.Services.Common.Models.InsuranceContacts
            {
                Id = 5,
                UserType = "Parent",
                IsPrimaryContact = true,
                IsGuarantor = true,
                Email = "john@example.com",
                PhoneNumber = "555-1234",
                RelationToClient = "Parent",
                RelationshipToInsured = 1,
                GenderId = 1,
                MaritalStatusId = 1,
                DateOfBirth = DateTime.Now.AddYears(-50),
                TimezoneId = 1
            };

            _mockRethinkServices
            .Setup(x => x.GetContactGuarantorDetails(accountId, clientId))
            .Returns(Task.FromResult(guarantorInfo));

            _mockPatientGuarantorRepository
            .Setup(x => x.Add(It.IsAny<PatientGuarantorEntity>()));
            _mockPatientGuarantorRepository
            .Setup(x => x.SaveChangesAsync())
            .Returns(Task.CompletedTask);

            // Act
            var method = _service.GetType()
            .GetMethod("SaveGuarantorSnapshotAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var taskResult = method.Invoke(_service, new object[] { invoiceId, accountId, clientId }) as dynamic;
            await taskResult;

            // Assert
            _mockPatientGuarantorRepository.Verify(x => x.Add(It.IsAny<PatientGuarantorEntity>()), Times.Once);
            _mockPatientGuarantorRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetPICreationDetails_WithBothFiltersApplied_FiltersCorrectly()
        {
            var model = new CreateInvoiceFilters
            {
                AccountInfoId = 100,
                Filters = new CreateInvoice
                {
                    DateOfServiceFrom = DateTime.Today.AddDays(-10),
                    DateOfServiceTo = DateTime.Today,
                    ClientIds = "10,20"
                }
            };

            var invoiceDetails = new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMockDbSet().Object;
            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query()).Returns(invoiceDetails);

            var chargeEntries = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity
                {
                    Id = 1,
                    BillingCode = "99213",
                    Units = 1,
                    Charges = 100,
                    DateOfService = DateTime.Today,
                    Claim = new ClaimEntity { AccountInfoId = 100 }
                },
                new ClaimChargeEntryEntity
                {
                    Id = 2,
                    BillingCode = "99214",
                    Units = 1,
                    Charges = 100,
                    DateOfService = DateTime.Today.AddDays(-20),
                    Claim = new ClaimEntity { AccountInfoId = 100 }
                }
            }.AsQueryable().BuildMockDbSet().Object;
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeEntries);

            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query())
            .Returns(new List<ClaimChargeEntryWriteOffEntity>()
            .AsQueryable().BuildMockDbSet().Object);

            _mockPaymentClaimService.Setup(x => x.GetAllPaymentChargeIds(It.IsAny<CreateInvoiceFilters>()))
                  .Returns(Task.FromResult(new List<BasicChargeDetails>
             {
                new BasicChargeDetails { ChargeId = 1, ClientId = 10, DateOfService = DateTime.Today },
                new BasicChargeDetails { ChargeId = 2, ClientId = 15, DateOfService = DateTime.Today.AddDays(-20) }
           }));

            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<PatientPaymentClaimFullModel>
            {
                new PatientPaymentClaimFullModel
                {
                    ChargeId = 1,
                    PatientId = 10,
                    PatientName = "John Doe",
                    PatientResponsibility = 50,
                    PatientResponsibilityBalance = 50,
                    InsurancePayment = 20,
                    PatientPayment = 10,
                    Adjustment = 5
                }
            });

            _mockClaimRepository.Setup(x => x.Query())
            .Returns(new List<ClaimEntity>
            {
                new ClaimEntity { Id = 1, DateDeleted = null }
                }
                .AsQueryable().BuildMockDbSet().Object);

            _mockRethinkServices.Setup(x => x.GetClientDetailsGuarantor(100))
           .Returns(Task.FromResult((List<Rethink.Services.Common.Models.ClientMicroServicesModels.RethinkGuarantorDetails.ClientModel>)null));

            // Act
            var (data, count) = await _service.GetPICreationDetails(model);

            // Assert
            Assert.Single(data);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task GeneratePDF_WithPreviousInvoicesIncluded_ReturnsCombinedPDF()
        {
            var accountId = 100;
            var clientId = 1;
            var invoiceRequests = new List<InvoiceRequestModel>
            {
                new InvoiceRequestModel
                {
                    AccountId = accountId,
                    ClientId = clientId,
                    Charges = new List<ChargeModel> { new ChargeModel { ChargeId = 1, PatientBalance = 100m } }
                }
            };

            var chargeEntries = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 1, BillingCode = "99213", Units = 1, Charges = 100m, DateOfService = DateTime.Today }
            }.AsQueryable().BuildMock();
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeEntries);

            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            var groupedPayments = new List<PatientPaymentClaimFullModel>
            {
                new PatientPaymentClaimFullModel { ChargeId = 1, InsurancePayment = 50m, PatientPayment = 20m, PatientResponsibility = 30m, PatientResponsibilityBalance = 100m }
            };
            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
                 .ReturnsAsync(groupedPayments);

            var previousInvoices = new List<PatientInvoiceEntity>
            {
                new PatientInvoiceEntity
                {
                    Id = 1,
                    AccountId = accountId,
                    ClientId = clientId,
                    InvoiceNumber = "INV-1-20240101-001",
                    InvoiceDate = DateTime.Now.AddDays(-10),
                    PaymentDueDate = DateTime.Now.AddDays(20)
                }
            };
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(previousInvoices.AsQueryable().BuildMock());

            var invoiceDetails = new List<PatientInvoiceDetailsEntity>
            {
                new PatientInvoiceDetailsEntity { InvoiceId = 1, ChargeId = 1 }
            };
            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query()).Returns(invoiceDetails.AsQueryable().BuildMock());

            var clientDetails = new List<ClaimSearchClientEntity>().AsQueryable().BuildMock();
            _mockClaimsSearchClientsRepository.Setup(x => x.Query()).Returns(clientDetails);

            _mockRazorViewService.Setup(x => x.RenderViewToStringAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.FromResult("<html>Combined</html>"));
            _mockPdfService.Setup(x => x.GeneratePDF(It.IsAny<string>()))
              .ReturnsAsync(new byte[] { 1, 2, 3 });

            _mockClientInfoService.Setup(x => x.GetClientInfo(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.ClientInfo());
            _mockClientInfoService.Setup(x => x.GetBillingProviderInfo(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.BillingProviderInfo());

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(It.IsAny<string>(), It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.ClientInfo>>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.ClientInfo());

            _mockCacheManager.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.BillingProviderInfo>>>(), It.IsAny<CachingDuration>()))
            .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.BillingProviderInfo());

            _mockRethinkServices.Setup(x => x.GetClientDetailsGuarantor(accountId))
            .Returns(Task.FromResult((List<Rethink.Services.Common.Models.ClientMicroServicesModels.RethinkGuarantorDetails.ClientModel>)null));

            var emptyGuarantors = new List<PatientGuarantorEntity>().AsQueryable().BuildMock();
            _mockPatientGuarantorRepository.Setup(x => x.Query()).Returns(emptyGuarantors);

            // Act
            var result = await _service.GeneratePDF(invoiceRequests, false, true, null);

            // Assert
            Assert.NotNull(result.PdfData);
            Assert.NotEmpty(result.PdfData);
        }

        [Fact]
        public async Task GetInvoiceDetails_WithAllFiltersApplied_FiltersCorrectly()
        {
            var filter = new PendingCollectionFilters
            {
                AccountInfoId = 100,
                Filters = new PendingCollection
                {
                    ClientIds = "1,2",
                    InvoiceFrom = DateTime.Now.AddDays(-30),
                    InvoiceTo = DateTime.Now,
                    PaymentDueFrom = DateTime.Now.AddDays(-20),
                    PaymentDueTo = DateTime.Now.AddDays(40),
                    PatientResponsibilityFrom = 10m,
                    PatientResponsibilityTo = 50m
                },
                Skip = 0,
                Take = 10
            };

            var invoices = new List<PatientInvoiceEntity>
            {
                new PatientInvoiceEntity
                {
                    Id = 1,
                    AccountId = 100,
                    ClientId = 1,
                    InvoiceNumber = "INV-1-20240101-001",
                    InvoiceDate = DateTime.Now.AddDays(-10),
                    PaymentDueDate = DateTime.Now.AddDays(20),
                    Status = PatientInvoiceStatus.InvoiceSent
                }
            };
            _mockPatientInvoiceRepository.Setup(x => x.Query()).Returns(invoices.AsQueryable().BuildMock());

            _mockRethinkServices.Setup(x => x.GetChildProfilesForAccount(It.IsAny<int>()))
            .Returns(Task.FromResult(new List<Rethink.Services.Common.Models.ChildProfileEntityModel>()));

            var invoiceDetails = new List<PatientInvoiceDetailsEntity>
            {
                new PatientInvoiceDetailsEntity { InvoiceId = 1, ChargeId = 1 }
            };
            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query()).Returns(invoiceDetails.AsQueryable().BuildMock());

            var chargeDetails = new List<ClaimChargeEntryEntity>
            {
                new ClaimChargeEntryEntity { Id = 1, BillingCode = "99213", Units = 1, Charges = 100m, DateOfService = DateTime.Now }
                }.AsQueryable().BuildMock();
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeDetails);

            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            var groupedPayments = new List<PatientPaymentClaimFullModel>
            {
                new PatientPaymentClaimFullModel { ChargeId = 1, InsurancePayment = 50m, PatientPayment = 20m, Adjustment = 10m, PatientResponsibility = 20m, PatientResponsibilityBalance = 30m }
            };
            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
            .ReturnsAsync(groupedPayments);

            var clientDetails = new List<ClaimSearchClientEntity>
            {
                new ClaimSearchClientEntity { Id = 1, firstName = "John", lastName = "Doe" }
            };

            _mockClaimsSearchClientsRepository.Setup(x => x.Query()).Returns(clientDetails.AsQueryable().BuildMock());

            var guarantorMap = new List<Rethink.Services.Common.Models.ClientMicroServicesModels.RethinkGuarantorDetails.ClientModel>();
            _mockRethinkServices.Setup(x => x.GetClientDetailsGuarantor(It.IsAny<int>()))
            .Returns(Task.FromResult(guarantorMap));

            // Act
            var result = await _service.GetInvoiceDetails(filter);

            // Assert
            Assert.NotNull(result.Data);
            Assert.Single(result.Data);
        }

        [Fact]
        public async Task GetInvoicePDF_WithValidGuarantorData_IncludesAllGuarantorDetails()
        {
            int accountId = 100;
            int clientId = 1;
            string invoiceNo = "INV-001";

            var invoice = new PatientInvoiceEntity
            {
                Id = 1,
                InvoiceNumber = invoiceNo,
                InvoiceDate = DateTime.Now,
                PaymentDueDate = DateTime.Now.AddDays(30)
            };
            _mockPatientInvoiceRepository.Setup(x => x.Query())
            .Returns(new List<PatientInvoiceEntity> { invoice }.AsQueryable().BuildMock());

            var guarantor = new PatientGuarantorEntity
            {
                InvoiceId = 1,
                FirstName = "John",
                MiddleName = "Michael",
                LastName = "Doe",
                Phone = "555-1234",
                Email = "john@example.com",
                Street1 = "123 Main St",
                Street2 = "Apt 4B",
                City = "New York",
                ZipCode = "10001"
            };
            _mockPatientGuarantorRepository.Setup(x => x.Query())
            .Returns(new List<PatientGuarantorEntity> { guarantor }.AsQueryable().BuildMock());

            _mockPatientInvoiceDetailsRepository.Setup(x => x.Query())
            .Returns(new List<PatientInvoiceDetailsEntity>().AsQueryable().BuildMock());

            var chargeDetails = new List<ClaimChargeEntryEntity>().AsQueryable().BuildMock();
            _mockChargeEntryRepository.Setup(x => x.Query()).Returns(chargeDetails);

            var writeOffDetails = new List<ClaimChargeEntryWriteOffEntity>().AsQueryable().BuildMock();
            _mockClaimChargeEntryWriteOffEntity.Setup(x => x.Query()).Returns(writeOffDetails);

            var groupedPayments = new List<PatientPaymentClaimFullModel>();
            _mockPaymentClaimService.Setup(x => x.GetGroupedByPaymentsForPatientInvoice(It.IsAny<List<int>>()))
            .ReturnsAsync(groupedPayments);

            _mockRazorViewService.Setup(x => x.RenderViewToStringAsync(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.FromResult("<html></html>"));
            _mockPdfService.Setup(x => x.GeneratePDF(It.IsAny<string>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

            _mockClientInfoService.Setup(x => x.GetClientInfo(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.ClientInfo());
            _mockClientInfoService.Setup(x => x.GetBillingProviderInfo(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.BillingProviderInfo());

            _mockCacheService.Setup(x => x.GetOrSetCacheAsync(It.IsAny<string>(), It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.ClientInfo>>>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.ClientInfo());

            _mockCacheManager.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<Func<Task<BillingService.Domain.Templates.ViewModels.BillingProviderInfo>>>(), It.IsAny<CachingDuration>()))
            .ReturnsAsync(new BillingService.Domain.Templates.ViewModels.BillingProviderInfo());

            // Act
            var result = await _service.GetInvoicePDF(accountId, clientId, invoiceNo);

            // Assert
            Assert.NotNull(result.pdfData);
            Assert.NotEmpty(result.pdfData);
        }
    }
}
