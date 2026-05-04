using Billing.FolderStructure.Core.Models;
using Billing.FolderStructure.Core.Services;
using Billing.FolderStructure.Core.Utils;
using BillingService.Domain.Interfaces.Files;
using EdiFabric;
using EdiFabric.Core.Model.Edi.X12;
using EdiFabric.Templates.Hipaa5010;
using EraParserService.Domain.Services;
using EraParserService.Domain.Services.EdiExtensionParsers;
using EraParserService.Domain.Services.EdiParsers.Edi277;
using EraParserService.Domain.Services.EdiParsers.Edi999;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Domain.Interfaces;
using System.Text;
using Thon.Hotels.FishBus;

namespace ClearingHouse.Tests
{
    public class EdiProcessingServiceTest
    {
        private readonly EdiProcessingService _service;
        private readonly TestBillingDbContext _dbContext;
        private readonly Mock<IBillingBlobService> _billingBlobService;
        private readonly Mock<IEraValidationService> _validationService;
        private readonly Mock<IBlobProcessingService> _blobProcessingService;
        private readonly Mock<IConfiguration> _config;
        private readonly Mock<IKeyVaultProviderService> _keyVault;
        private readonly Mock<IEdiFilesDownload> _ediFilesDownload;

        public EdiProcessingServiceTest()
        {
            SerialKey.Set("a0cc9b55de394861aa176dde0db031c1");

            // ---------------- DbContext (REAL InMemory) ----------------
            // BaseDbContext requires SqlServerOptionsExtension in options,
            // so we include UseSqlServer here, but override it later.
            var options = new DbContextOptionsBuilder<BillingDbContext>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=FakeBillingDb;Trusted_Connection=True;")
                .Options;

            _dbContext = new TestBillingDbContext(options);

            // Seed data
            _dbContext.Payments.Add(new PaymentEntity
            {
                ReferenceNumber = "300004763",
                AccountInfoId = 1,
                IsManualPayment = false,
                DateDeleted = null
            });

            _dbContext.SaveChanges();

            // ---------------- Mocks ----------------
            _billingBlobService = new Mock<IBillingBlobService>();

            var paymentEraRepo = new Mock<IRepository<BillingDbContext, PaymentEraUploadEntity>>();
            _blobProcessingService = new Mock<IBlobProcessingService>();
            _validationService = new Mock<IEraValidationService>();
            var paymentService = new Mock<IPaymentService>();
            var claimAckParser = new Mock<IClaimAckParser>();
            var edi999Parser = new Mock<IEdi999Parser>();
            var claimsSummaryParser = new Mock<IClaimsSummaryDataParser>();
            var messageBus = new Mock<IMessageBus>();
            var billingFilePath = new Mock<IBillingFilePath>();
            var funderSeqRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity>>();
            var claimSubmissionRepo = new Mock<IRepository<BillingDbContext, ClaimSubmissionEntity>>();
            _config = new Mock<IConfiguration>();
            _keyVault = new Mock<IKeyVaultProviderService>();
            _ediFilesDownload = new Mock<IEdiFilesDownload>();

            // Mock CreatePayment
            paymentService.Setup(x => x.CreatePayment(
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<int?>(),
                    It.IsAny<PaymentTypes>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .ReturnsAsync(new PaymentEntity
                {
                    Id = 999,
                    PaymentIdentifier = "TEST-PMT",
                    AccountInfoId = 2
                });

            // ---------------- Configuration ----------------

            _config.Setup(c => c["EdiSettings:SubmitterRethinkId"])
                  .Returns("1579664");

            _config.Setup(c => c["EdiSettings:BillerRethinkId"])
                  .Returns("030240928");

            var apiUrlSection = new Mock<IConfigurationSection>();
            apiUrlSection.Setup(x => x.Value).Returns("BillingApiUrlSecret");

            var apiKeySection = new Mock<IConfigurationSection>();
            apiKeySection.Setup(x => x.Value).Returns("BillingApiKeySecret");

            _config.Setup(c => c.GetSection("BillingApiUrl")).Returns(apiUrlSection.Object);
            _config.Setup(c => c.GetSection("BillingApiKey")).Returns(apiKeySection.Object);

            _keyVault.Setup(k => k.GetSecretAsync("BillingApiUrlSecret"))
                    .ReturnsAsync("https://billing.api.test");

            _keyVault.Setup(k => k.GetSecretAsync("BillingApiKeySecret"))
                    .ReturnsAsync("test-api-key");

            // Mock SaveClaimEdiFilePath
            _ediFilesDownload.Setup(x => x.SaveClaimEdiFilePath(
                    It.IsAny<BillingRequest>(),
                    It.IsAny<string>(),
                    It.IsAny<ClaimSubmissionEntity>()))
                .ReturnsAsync(true);

            // ---------------- Logger ----------------
            var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug());

            // ---------------- Service ----------------
            _service = new EdiProcessingService(
                _billingBlobService.Object,
                paymentEraRepo.Object,
                loggerFactory,
                _dbContext,
                _blobProcessingService.Object,
                _validationService.Object,
                paymentService.Object,
                claimAckParser.Object,
                edi999Parser.Object,
                claimsSummaryParser.Object,
                _config.Object,
                messageBus.Object,
                billingFilePath.Object,
                funderSeqRepo.Object,
                _keyVault.Object,
                claimSubmissionRepo.Object,
                _ediFilesDownload.Object
                );
        }

        [Fact]
        public async Task ProcessFile_Valid835Era_NoDuplicate_ReturnsSuccess()
        {
            // ---------------- Arrange ----------------
            var downloadData = new EdiDownloadData
            {
                FileIdentifier = "ERA_New.edi",
                AccountInfoId = 2,
                DownloadDateTime = DateTime.UtcNow
            };

            var claim = new ClaimEntity
            {
                AccountInfoId = 2
            };

            _dbContext.Claims.Add(claim);
            await _dbContext.SaveChangesAsync();

            _dbContext.ClaimSubmissions.Add(
                new ClaimSubmissionEntity
                {
                    ClaimSubmissionIdentifier = "260125-09QB9-12",
                    AccountFederalTaxId = "123456789",
                    AccountNpiNumber = "98765",
                    ClaimId = claim.Id,
                    Claim = claim
                }
            );

            await _dbContext.SaveChangesAsync();

            // Build TS835 object
            var ts835 = new TS835
            {
                BPR_FinancialInformation = new BPR_FinancialInformation_2
                {
                    PaymentMethodCode_04 = "ACH",
                    TotalPremiumPaymentAmount_02 = "1500",
                    CreditorDebitFlagCode_03 = "C",
                    CheckIssueorEFTEffectiveDate_16 = DateTime.UtcNow.ToString("yyyyMMdd")
                },
                TRN_ReassociationTraceNumber = new TRN_DependentTraceNumber
                {
                    CurrentTransactionTraceNumber_02 = "300004764",
                    OriginatingCompanyIdentifier_03 = "1388600003"
                },
                AllN1 = new All_N1_835
                {
                    Loop1000A = new Loop_1000A_835
                    {
                        N1_PayerIdentification = new N1_PayerIdentification
                        {
                            PremiumPayerName_02 = "Funder",
                            IntermediaryBankIdentifier_04 = "12345"
                        },
                        N3_PayerAddress = new N3_AdditionalPatientInformationContactAddress
                        {
                            ResponseContactAddressLine_01 = "123 Street"
                        },
                        N4_PayerCity_State_ZIPCode = new N4_AdditionalPatientInformationContactCity
                        {
                            AdditionalPatientInformationContactCityName_01 = "City",
                            AdditionalPatientInformationContactStateCode_02 = "ST",
                            AdditionalPatientInformationContactPostalZoneorZIPCode_03 = "12345"
                        },
                        REF_AdditionalPayerIdentification = new List<REF_AdditionalPayerIdentification>(),
                        AllPER = new All_PER_835
                        {
                            PER_PayerBusinessContactInformation = new PER_PayerBusinessContactInformation(),
                            PER_PayerTechnicalContactInformation = new List<PER_PayerTechnicalContactInformation>
                            {
                                new PER_PayerTechnicalContactInformation
                                {
                                    ResponseContactName_02 = "John Doe",
                                    CommunicationNumberQualifier_03 = "EM",
                                    ResponseContactCommunicationNumber_04 = "test@example.com"
                                }
                            },
                            PER_PayerWEBSite = new PER_PayerWEBSite()
                        }
                    },
                    Loop1000B = new Loop_1000B_835
                    {
                        N1_PayeeIdentification = new N1_PayeeIdentification
                        {
                            PremiumPayerName_02 = "Payee",
                            IdentificationCodeQualifier_03 = "XX",
                            IntermediaryBankIdentifier_04 = "98765"
                        },
                        N3_PayeeAddress = new N3_AdditionalPatientInformationContactAddress
                        {
                            ResponseContactAddressLine_01 = "456 Street"
                        },
                        N4_PayeeCity_State_ZIPCode = new N4_AdditionalPatientInformationContactCity
                        {
                            AdditionalPatientInformationContactCityName_01 = "Town",
                            AdditionalPatientInformationContactStateCode_02 = "TS",
                            AdditionalPatientInformationContactPostalZoneorZIPCode_03 = "67890"
                        },
                        REF_PayeeAdditionalIdentification = new List<REF_PayeeAdditionalIdentification>
                        {
                            new REF_PayeeAdditionalIdentification
                            {
                                ReferenceIdentificationQualifier_01 = "TJ",
                                ReferenceIdentificationREF_02 = "123456789"
                            }
                        }
                    }
                },
                Loop2000 = new List<Loop_2000_835>
                {
                    new Loop_2000_835
                    {
                        Loop2100 = new List<Loop_2100_835>
                        {
                            new Loop_2100_835
                            {
                                CLP_ClaimPaymentInformation = new CLP_ClaimPaymentInformation
                                {
                                    PatientControlNumber_01 = "260125-09QB9-12",
                                    ClaimStatusCode_02 = "1",
                                    TotalClaimChargeAmount_03 = "512216",
                                    ClaimPaymentAmount_04 = "16",
                                    PatientResponsibilityAmount_05 = "20",
                                    ClaimFilingIndicatorCode_06 = "HM",
                                    PayerClaimControlNumber_07 = "2025133BZ6052",
                                    FacilityTypeCode_08 = "03",
                                    ClaimFrequencyCode_09 = "F",
                                    PatientStatusCode_10 = "01",
                                    DiagnosisRelatedGroupDRGCode_11 = "123",
                                    DiagnosisRelatedGroupDRGWeight_12 = "1.0",
                                    DischargeFraction_13 = "100",
                                    YesNoConditionorResponseCode_14 = "Y"
                                },
                                AllNM1 = new All_NM1_835
                                {
                                    NM1_PatientName = new NM1_PatientName_2
                                    {
                                        EntityIdentifierCode_01 = "QC",
                                        EntityTypeQualifier_02 = "1",
                                        ResponseContactLastorOrganizationName_03 = "Doe",
                                        ResponseContactFirstName_04 = "John",
                                        ResponseContactMiddleName_05 = "M",
                                        IdentificationCodeQualifier_08 = "MI",
                                        ResponseContactIdentifier_09 = "123456789"
                                    }
                                },
                                Loop2110 = new List<Loop_2110_835>(),
                                PER_ClaimContactInformation = new List<PER_ClaimContactInformation>(),
                                AMT_ClaimSupplementalInformation = new List<AMT_ClaimSupplementalInformation>(),
                                QTY_ClaimSupplementalInformationQuantity = new List<QTY_ClaimSupplementalInformationQuantity>(),
                                AllREF = new All_REF_835(),
                                AllDTM = new All_DTM_835()
                            }
                        }
                    }
                },
                SE = new SE
                {
                    TransactionSetControlNumber_02 = "000000512",
                    NumberofIncludedSegments_01 = "42"
                }
            };

            // Build raw EDI string
            var ediString = @"ISA*00*          *00*          *ZZ*1579664       *ZZ*030240928     *250220*0925*^*00501*000000512*0*P*:~
                GS*HP*1579664*030240928*20250220*092502*000000512*X*005010X221A1~
                ST*835*000000512~
                BPR*H*1500*C*ACH************20250220~
                TRN*1*300004764*1388600003~
                DTM*405*20250220~
                N1*PR*Funder~
                N3*123 Street~
                N4*City*ST*12345~
                PER*BL*Rethink Behavioral Health*TE*8007479886*EM*billingdev@rethinkfirst.com~
                N1*PE*Payee*XX*98765~
                N3*456 Street~
                N4*Town*TS*67890~
                REF*TJ*123456789~
                LX*1~
                CLP*260125-09QB9-12*1*512216*16*20*HM*2025133BZ6052*03~
                NM1*QC*1*Doe*John****MI*123456789~
                NM1*82*1*Provider*Rendering****XX*1639701121~
                DTM*232*20250220~
                DTM*233*20250220~
                DTM*050*20250220~
                SE*20*000000512~
                GE*1*000000512~
                IEA*1*000000512~";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ediString));

            // ---------------- Act ----------------
            var result = await _service.ProcessFile(downloadData, stream);

            // ---------------- Assert ----------------
            Assert.NotNull(result);
            Assert.True(HandlerResult.IsSuccess(result));
        }

        [Fact]
        public async Task ProcessFile_Valid835Era_ValidationFails_ReturnsFailed()
        {
            // Arrange
            var downloadData = new EdiDownloadData
            {
                FileIdentifier = "ERA_ValidationFail.edi",
                AccountInfoId = 1,
                DownloadDateTime = DateTime.UtcNow
            };

            // Seed Claim + ClaimSubmission
            var claim = new ClaimEntity { AccountInfoId = 1 };
            _dbContext.Claims.Add(claim);
            await _dbContext.SaveChangesAsync();

            _dbContext.ClaimSubmissions.Add(new ClaimSubmissionEntity
            {
                ClaimSubmissionIdentifier = "260125-09QB9-12",
                AccountFederalTaxId = "123456789",
                AccountNpiNumber = "98765",
                ClaimId = claim.Id,
                Claim = claim
            });
            await _dbContext.SaveChangesAsync();

            // Force validation failure
            _validationService
                .Setup(v => v.ValidateEraPayments(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<List<PaymentEntity>>()))
                .ThrowsAsync(new Exception("Validation failed"));

            var edi =
                @"ISA*00*          *00*          *ZZ*1579664       *ZZ*030240928     *250220*0925*^*00501*000000512*0*P*:~
                GS*HP*1579664*030240928*20250220*092502*000000512*X*005010X221A1~
                ST*835*000000512~
                BPR*H*1500*C*ACH************20250220~
                TRN*1*300004764*1388600003~
                DTM*405*20250220~
                N1*PR*Funder~
                N3*123 Street~
                N4*City*ST*12345~
                PER*BL*Rethink Behavioral Health*TE*8007479886*EM*billingdev@rethinkfirst.com~
                N1*PE*Payee*XX*98765~
                N3*456 Street~
                N4*Town*TS*67890~
                REF*TJ*123456789~
                LX*1~
                CLP*260125-09QB9-12*1*512216*16*20*HM*2025133BZ6052*03~
                NM1*QC*1*Doe*John****MI*123456789~
                NM1*82*1*Provider*Rendering****XX*1639701121~
                DTM*232*20250220~
                DTM*233*20250220~
                DTM*050*20250220~
                SE*20*000000512~
                GE*1*000000512~
                IEA*1*000000512~";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(edi));

            // Act
            var result = await _service.ProcessFile(downloadData, stream);

            // Assert
            Assert.NotNull(result);
            Assert.False(HandlerResult.IsSuccess(result)); // Validation failure → should fail
        }

    }

}
