using BillingService.Domain.Services.Billing.EDI;
using Moq;
using Rethink.Services.Common.Dtos.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.BillingSettings;
using Rethink.Services.Common.Models.Claim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BillingService.XUnit.Tests.Billing.EDI
{
    public class EdiGeneratorTests
    {
        // Helper factory methods
        private static ClaimSubmissionEntity BuildBaseClaim(
        bool withBillingProviderFirstName = true,
        bool subscriberIsSelf = true,
        ClaimFrequencyType frequency = ClaimFrequencyType.Original,
        bool addAuthorization = true,
        bool addClaimNote = true,
        bool addServiceLocation = true,
        bool serviceLocationNpiSameAsBilling = false,
        int diagnosisCount = 1,
        bool withServiceDate = true,
        bool withServiceNoteOnVoidOrReplace = false,
        bool secondary = false,
        bool includeClaimLevelCas = false,
        bool includeServiceLineCas = false,
        bool priorPaymentPresent = false
        )
        {
            var billingProviderFirstName = withBillingProviderFirstName ? "John" : string.Empty;
            var billingProviderNpi = "1234567890";
            var serviceLocationNpi = serviceLocationNpiSameAsBilling ? billingProviderNpi : "0987654321";

            var claim = new ClaimEntity
            {
                OriginalClaim = frequency is ClaimFrequencyType.Replacement or ClaimFrequencyType.Void ? "OLD-CLM-001" : null,
                Note = addClaimNote ? "Some claim-level note" : string.Empty,
                ClaimChargeEntries = new List<ClaimChargeEntryEntity>()
            };

            var funderCurrent = new ClaimSubmissionFunderSequenceEntity
            {
                Id = 1,
                FunderResponsibilitySequence = "P",
                RelationshipToSubscriber = subscriberIsSelf ? 1 : 2,
                SubscriberFirstName = subscriberIsSelf ? "SelfFirst" : "ParentFirst",
                SubscriberLastName = subscriberIsSelf ? "SelfLast" : "ParentLast",
                SubscriberMiddleName = subscriberIsSelf ? "SelfMiddle" : string.Empty,
                SubscriberGender = "M",
                SubscriberDOB = new DateTime(1990, 2, 3),
                SubscriberAddress1 = subscriberIsSelf ? "123 Main St" : string.Empty,
                SubscriberAddress2 = subscriberIsSelf ? "Apt4" : string.Empty,
                SubscriberCity = subscriberIsSelf ? "City" : string.Empty,
                SubscriberState = subscriberIsSelf ? "ST" : string.Empty,
                SubscriberZip = subscriberIsSelf ? "12345" : string.Empty,
                FunderName = "Payer A",
                FunderVendorId = "PAYERA",
                InsuranceGroupNumber = "GRP123",
                InsurancePolicyNumber = "POL123"
            };

            var funderSecondary = new ClaimSubmissionFunderSequenceEntity
            {
                Id = 2,
                FunderResponsibilitySequence = "S",
                RelationshipToSubscriber = 2,
                SubscriberFirstName = "ChildFirst",
                SubscriberLastName = "ChildLast",
                SubscriberMiddleName = "ChildMiddle",
                FunderName = "Payer B",
                FunderVendorId = "PAYERB",
                InsuranceGroupNumber = "GRP456",
                InsurancePolicyNumber = "POL456"
            };

            var serviceLines = new List<ClaimSubmissionServiceLineEntity>{new ClaimSubmissionServiceLineEntity
    {
                ClaimChargeEntryId =1001,
                BillingCode = "97153",
                BillingCodeDescription = "ABA therapy",
                Modifier1 = "U1",
                Modifier2 = "",
                Modifier3 = "",
                Modifier4 = "",
                Charges =123.45m,
                Units =3,
                DiagnosisCode = "F84.0",
                DiagnosisCodeOrder =1,
                DateOfService = withServiceDate ? new DateTime(2024,1,10) : null,
                ClaimChargeEntry = new ClaimChargeEntryEntity { NoteText = withServiceNoteOnVoidOrReplace ? "Line note here" : string.Empty }
                },
                new ClaimSubmissionServiceLineEntity
                {
                ClaimChargeEntryId =1002,
                BillingCode = "97155",
                BillingCodeDescription = "ABA supervision",
                Modifier1 = "U1",
                Charges =200.00m,
                Units =2.5m,
                DiagnosisCode = diagnosisCount >=2 ? "R62.0" : "F84.0",
                DiagnosisCodeOrder =2,
                DateOfService = withServiceDate ? new DateTime(2024,1,11) : null,
                ClaimChargeEntry = new ClaimChargeEntryEntity { NoteText = withServiceNoteOnVoidOrReplace ? "Another line note" : string.Empty }
                }
     };

            // add many diagnosis orders up to12
            if (diagnosisCount > 2)
            {
                for (int i = 3; i <= Math.Min(diagnosisCount, 12); i++)
                {
                    serviceLines.Add(new ClaimSubmissionServiceLineEntity
                    {
                        ClaimChargeEntryId = 1000 + i,
                        BillingCode = "97156",
                        BillingCodeDescription = "Parent training",
                        Modifier1 = "",
                        Charges = 50.00m,
                        Units = 1,
                        DiagnosisCode = "DX" + i,
                        DiagnosisCodeOrder = i,
                        DateOfService = withServiceDate ? new DateTime(2024, 1, 11) : null,
                        ClaimChargeEntry = new ClaimChargeEntryEntity { NoteText = withServiceNoteOnVoidOrReplace ? "Note " + i : string.Empty }
                    });
                }
            }

            PaymentClaimEntity priorPayment = null;
            if (priorPaymentPresent)
            {
                priorPayment = new PaymentClaimEntity
                {
                    TotalPayment = 99.99m,
                    PatientRespAmount = 10.00m,
                    PaymentClaimAdjustments = includeClaimLevelCas ? new List<PaymentClaimAdjustmentEntity>
                {
                new PaymentClaimAdjustmentEntity { AdjustmentGroupCode = "CO", AdjustmentReasonCode = "45", AdjustmentAmount =5.00m, AdjustmentQuantity =1 },
                new PaymentClaimAdjustmentEntity { AdjustmentGroupCode = "CO", AdjustmentReasonCode = "97", AdjustmentAmount =3.00m, AdjustmentQuantity =1 }
                } : new List<PaymentClaimAdjustmentEntity>(),
                    PaymentClaimServiceLines = includeServiceLineCas ? new List<PaymentClaimServiceLineEntity>
                {
                new PaymentClaimServiceLineEntity
                {
                ClaimChargeEntryId =1001,
                DateOfService = serviceLines.First().DateOfService,
                ServiceCode = serviceLines.First().BillingCode,
                ProcedureModifier1 = serviceLines.First().Modifier1,
                ProcedureModifier2 = serviceLines.First().Modifier2,
                ProcedureModifier3 = serviceLines.First().Modifier3,
                ProcedureModifier4 = serviceLines.First().Modifier4,
                ProcedureDesc = serviceLines.First().BillingCodeDescription,
                ProcedureUnits = "2",
                PaymentAmount =50.00m,
                PaymentClaimServiceLineAdjustments = new List<PaymentClaimServiceLineAdjustmentEntity>
                {
                new PaymentClaimServiceLineAdjustmentEntity { AdjustmentGroupCode = "PR", AdjustmentReasonCode = "1", AdjustmentAmount =20.00m, AdjustmentQuantity =1 }
                }
                }
                } : new List<PaymentClaimServiceLineEntity>()
                };
            }

            var entity = new ClaimSubmissionEntity
            {
                Id = 123,
                SubmitDate = new DateTime(2024, 1, 15, 13, 45, 0),
                ClaimSubmissionIdentifier = "SUB123",
                DocumentType = ClaimDocumentType.Doc837P,
                ResponsibilitySequence = "P",
                Claim = claim,
                ResolvedBillingProviderFirstName = billingProviderFirstName,
                ResolvedBillingProviderMiddleName = withBillingProviderFirstName ? "A" : string.Empty,
                ResolvedBillingProviderName = withBillingProviderFirstName ? "John Billing" : "Org Billing",
                ResolvedBillingProviderNpi = billingProviderNpi,
                ResolvedBillingProviderFederalTaxID = "11-1111111",
                LocationBillingProviderTaxonomyCode = "251S00000X",
                LocationBillingProviderAddress1 = "456 Clinic Rd",
                LocationBillingProviderAddress2 = "Suite100",
                LocationBillingProviderCity = "ClinicCity",
                LocationBillingProviderState = "CL",
                LocationBillingProviderZip = "67890",
                AccountAddress1 = "456 Clinic Rd",
                AccountAddress2 = "Suite100",
                AccountBillingCity = "ClinicCity",
                AccountBillingState = "CL",
                AccountBillingZip = "67890",
                AccountBillingProviderEmail = "billing@clinic.com",
                AccountBillingProviderPhone = "555-555-5555",
                AccountBillingProviderFax = string.Empty,
                PlaceOfServiceCode = "11",
                FrequencyType = frequency,
                AuthorizationNumber = addAuthorization ? "AUTH-123" : null,
                ServiceLocationName = addServiceLocation ? "Clinic South" : string.Empty,
                ServiceLocationNpiNumber = addServiceLocation ? serviceLocationNpi : string.Empty,
                ServiceLocationAddress1 = addServiceLocation ? "789 Service Ave" : string.Empty,
                ServiceLocationAddress2 = addServiceLocation ? "Bldg2" : string.Empty,
                ServiceLocationCity = addServiceLocation ? "SrvCity" : string.Empty,
                ServiceLocationState = addServiceLocation ? "SC" : string.Empty,
                ServiceLocationZip = addServiceLocation ? "11111" : string.Empty,
                LocationBillingProviderCommercialNumber = "COMM1",
                LocationBillingProviderStateLicenseNumber = "LIC1",
                LocationBillingProviderLocationNumber = "LOC1",
                ReferringProviderFirstName = "RefFirst",
                ReferringProviderLastName = "RefLast",
                ReferringProviderNpiNumber = "2222333344",
                ResolvedRenderingProviderFirstName = "RndFirst",
                ResolvedRenderingProviderMiddleName = "RndMid",
                ResolvedRenderingProviderName = "Rnd Name",
                ResolvedRenderingProviderNpi = "3333444455",
                ChildProfileFirstName = "ChildFirst",
                ChildProfileMiddleName = "ChildMid",
                ChildProfileLastName = "ChildLast",
                ChildProfileAddress1 = "100 Child St",
                ChildProfileAddress2 = "",
                ChildProfileCity = "KidCity",
                ChildProfileState = "KS",
                ChildProfileZip = "22222",
                ChildProfileDOB = new DateTime(2018, 5, 20),
                ChildProfileGender = "F",
                ClearinghouseSubmitterName = "Change Healthcare",
                ClearinghouseIdentifier = "CH",
                ClaimSubmissionFunderSequences = secondary ? new List<ClaimSubmissionFunderSequenceEntity> { funderCurrent, funderSecondary } : new List<ClaimSubmissionFunderSequenceEntity> { funderCurrent },
                ClaimSubmissionServiceLines = serviceLines,
                PriorFunderLatestClaimPayment = priorPayment,
                PayerClaimControlNumber = secondary && priorPaymentPresent ? "PCN-123" : null
            };

            return entity;
        }

        private static ClearingHouseClaimModel BuildClaimModel(bool isSecondary = false, AdjustmentLevel adjLevel = AdjustmentLevel.Claim)
        {
            return new ClearingHouseClaimModel
            {
                isSecondary = isSecondary,
                AdjustmentLevel = adjLevel
            };
        }

        private static Eligibility270DTO BuildEligibilityDto(
        string payerName,
        DateTime? submitDate = null,
        string providerName = "Ref Prov",
        string renderingName = "Rnd Prov",
        string npi = "9998887776",
        string clientName = "First Middle Last",
        string SubscriberId = "42",
        DateTime? dob = null,
        int genderId = 1,
        string payerId = "C1044"
        )
        {
            return new Eligibility270DTO
            {
                ClearingHousePayerName = payerName,
                EffectiveDate = submitDate ?? default,
                ChildProfileReferringProviderName = providerName,
                ChildProfileRenderingProviderName = renderingName,
                StaffMemberNpiNumber = npi,
                ClientName = clientName,
                SubscriberId = SubscriberId,
                DOB = dob ?? new DateTime(2010, 4, 3),
                GenderId = genderId,
                PayerId = payerId
            };
        }

        private static ClientListUserModel EmptyRenderingProviders() =>
            new ClientListUserModel { data = new List<ClientUserModel>() };

        private static EdiGenerator CreateGenerator(bool testMode = true)
        {
            var rethinkMicroservicesRepository = new Mock<IRethinkMasterDataMicroServices>().Object;
            return new EdiGenerator(
            testMode,
            billerRethinkId: "BILLERID",
            submitterRethinkId: "SUBMITTERID",
            submitterRethinkName: "Submitter Org",
            submitterRethinkEmail: "submitter@org.com",
            submitterRethinkPhone: "555-111-2222",
            customerId: "CUSTOMERID",
            clearingHouseName: "Change Healthcare",
            taxId: "12-3456789",           
             rethinkMicroservicesRepository: rethinkMicroservicesRepository,
              senderId: "380484393921",
            receiverId: "STEDITEST"
            );
        }

        private BillingFunderSettings GetBillingFunderSettings()
        {
            return new BillingFunderSettings
            {
                ClaimFilingIndicator = "CI",
                IncludeTaxonomyCode = true
            };
        }

        [Fact]
        public async Task GenerateEdi_UnsupportedType_ShouldThrow()
        {
            var gen = CreateGenerator(true);
            var claim = BuildBaseClaim();
            claim.DocumentType = ClaimDocumentType.Doc837I; // unsupported in switch

            var model = BuildClaimModel();
            await Assert.ThrowsAsync<Exception>(async () => await gen.GenerateEdi(claim, model, GetBillingFunderSettings(), null));
        }

        [Theory]
        [InlineData(true, "1")] // individual
        [InlineData(false, "2")] // organization
        public async Task BuildClaim_BillingProviderType_ShouldSetEntityTypeQualifier(bool withFirstName, string expected)
        {
            var gen = CreateGenerator(true);
            var claim = BuildBaseClaim(withBillingProviderFirstName: withFirstName);
            var model = BuildClaimModel();
            var ts = await gen.BuildClaim(claim, "000000123", model, EmptyRenderingProviders(), GetBillingFunderSettings(), null);
            Assert.NotNull(ts);
            Assert.Equal(expected, ts.Loop2000A.First().AllNM1.Loop2010AA.NM1_BillingProviderName.EntityTypeQualifier_02);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BuildClaim_SubscriberSelfVsChild_ShouldPopulateCorrectly(bool subscriberIsSelf)
        {
            var gen = CreateGenerator(true);
            var claim = BuildBaseClaim(subscriberIsSelf: subscriberIsSelf);
            var model = BuildClaimModel();
            var ts = await gen.BuildClaim(claim, "000000123", model, EmptyRenderingProviders(), GetBillingFunderSettings(), null);
            Assert.NotNull(ts);
            var loop2000B = ts.Loop2000A.First().Loop2000B.First();
            Assert.Equal(subscriberIsSelf ? "0" : "1", loop2000B.HL_SubscriberHierarchicalLevel.HierarchicalChildCode_04);
            if (!subscriberIsSelf)
            {
                Assert.NotNull(loop2000B.Loop2000C);
                Assert.NotNull(loop2000B.Loop2000C.First().Loop2010CA.NM1_PatientName);
            }
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(12)]
        public async Task BuildClaim_DiagnosisCodes_ShouldPopulateHI(int diagCount)
        {
            var gen = CreateGenerator(true);
            var claim = BuildBaseClaim(diagnosisCount: diagCount);
            var model = BuildClaimModel();
            var ts = await gen.BuildClaim(claim, "000000123", model, EmptyRenderingProviders(), GetBillingFunderSettings(), null);
            var hi = ((Rethink_Loop_2300_837P)ts.Loop2000A.First().Loop2000B.First().Loop2000C.First().Loop2300.First()).AllHI;
            Assert.NotNull(hi);
        }

        [Theory]
        [InlineData(ClaimFrequencyType.Original, "1", false)]
        [InlineData(ClaimFrequencyType.Replacement, "7", true)]
        [InlineData(ClaimFrequencyType.Void, "8", true)]
        public async Task BuildClaim_FrequencyAndOriginalClaim_ShouldSetCodes(ClaimFrequencyType freq, string expectedFreqCode, bool expectRefF8)
        {
            var gen = CreateGenerator(true);
            var claim = BuildBaseClaim(frequency: freq);
            var model = BuildClaimModel();
            var ts = await gen.BuildClaim(claim, "000000123", model, EmptyRenderingProviders(), GetBillingFunderSettings(), null);
            var loop2300 = (Rethink_Loop_2300_837P)ts.Loop2000A.First().Loop2000B.First().Loop2000C.First().Loop2300.First();
            Assert.Equal(expectedFreqCode, loop2300.CLM_ClaimInformation.HealthCareServiceLocationInformation_05.ClaimFrequencyTypeCode_03);
            if (expectRefF8)
            {
                Assert.NotNull(loop2300.AllREF);
                Assert.NotNull(loop2300.AllREF.REF_PayerClaimControlNumber);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BuildClaim_AuthorizationNumber_ShouldToggleRefG1(bool addAuthorization)
        {
            var gen = CreateGenerator(true);
            var claim = BuildBaseClaim(addAuthorization: addAuthorization);
            var model = BuildClaimModel();
            var ts = await gen.BuildClaim(claim, "000000123", model, EmptyRenderingProviders(), GetBillingFunderSettings(), null);
            var loop2300 = (Rethink_Loop_2300_837P)ts.Loop2000A.First().Loop2000B.First().Loop2000C.First().Loop2300.First();
            if (addAuthorization)
            {
                Assert.NotNull(loop2300.AllREF);
                Assert.NotNull(loop2300.AllREF.REF_PriorAuthorization);
            }
            else
            {
                Assert.True(loop2300.AllREF == null || loop2300.AllREF.REF_PriorAuthorization == null);
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task BuildClaim_ClaimNote_ShouldPopulateWhenPresent(bool addNote)
        {
            var gen = CreateGenerator(true);
            var claim = BuildBaseClaim(addClaimNote: addNote);
            var model = BuildClaimModel();
            var ts = await gen.BuildClaim(claim, "000000123", model, EmptyRenderingProviders(), GetBillingFunderSettings(), null);
            var loop2300 = (Rethink_Loop_2300_837P)ts.Loop2000A.First().Loop2000B.First().Loop2000C.First().Loop2300.First();
            if (addNote)
                Assert.NotNull(loop2300.NTE_ClaimNote);
            else
                Assert.Null(loop2300.NTE_ClaimNote);
        }

        [Theory]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(false, false, false)]
        public async Task BuildClaim_ServiceLocation_ShouldAddLoop2310CConditionally(bool present, bool sameNpiAsBilling, bool expectedAdded)
        {
            var gen = CreateGenerator(true);
            var claim = BuildBaseClaim(addServiceLocation: present, serviceLocationNpiSameAsBilling: sameNpiAsBilling);
            var model = BuildClaimModel();
            var ts = await gen.BuildClaim(claim, "000000123", model, EmptyRenderingProviders(), GetBillingFunderSettings(), null);
            var loop2300 = (Rethink_Loop_2300_837P)ts.Loop2000A.First().Loop2000B.First().Loop2000C.First().Loop2300.First();
            var added = loop2300.Loop2310C != null && loop2300.Loop2310C.Any();
            Assert.Equal(expectedAdded, added);
        }

        [Theory]
        [InlineData(true, true, true, AdjustmentLevel.Claim)]
        [InlineData(true, true, true, AdjustmentLevel.ServiceLine)]
        [InlineData(true, false, false, AdjustmentLevel.Claim)]
        [InlineData(false, false, false, AdjustmentLevel.Claim)]
        public async Task BuildClaim_SecondaryPayerAndCas_ShouldPopulateConditionally(bool isSecondary, bool priorPaymentPresent, bool expect2320, AdjustmentLevel lvl)
        {
            var gen = CreateGenerator(true);
            var claim = BuildBaseClaim(secondary: isSecondary, priorPaymentPresent: priorPaymentPresent, includeClaimLevelCas: lvl == AdjustmentLevel.Claim, includeServiceLineCas: lvl == AdjustmentLevel.ServiceLine);
            var model = BuildClaimModel(isSecondary: isSecondary, adjLevel: lvl);
            var ts = await gen.BuildClaim(claim, "000000123", model, EmptyRenderingProviders(), GetBillingFunderSettings(), null);
            var loop2300 = (Rethink_Loop_2300_837P)ts.Loop2000A.First().Loop2000B.First().Loop2000C.First().Loop2300.First();
            var has2320 = loop2300.Loop2320 != null && loop2300.Loop2320.Any();
            Assert.Equal(isSecondary, has2320);
        }

        [Theory]
        [InlineData(true, 3, true, true)]
        [InlineData(false, 2.5, true, true)]
        [InlineData(true, 1, false, false)]
        public async Task BuildClaim_ServiceLineVariants_ShouldPopulateCorrectly(bool integerUnits, decimal unitsValue, bool withDate, bool expectDtp)
        {
            var gen = CreateGenerator(true);
            var claim = BuildBaseClaim(withServiceDate: withDate);
            claim.ClaimSubmissionServiceLines.First().Units = integerUnits ? Math.Round(unitsValue, 0) : unitsValue;
            var model = BuildClaimModel();
            var ts = await gen.BuildClaim(claim, "000000123", model, EmptyRenderingProviders(), GetBillingFunderSettings(), null);
            var loop2300 = (Rethink_Loop_2300_837P)ts.Loop2000A.First().Loop2000B.First().Loop2000C.First().Loop2300.First();
            var sl = loop2300.Loop2400.First();
            Assert.False(string.IsNullOrEmpty(sl.SV1_ProfessionalService.ServiceUnitCount_04));
            if (expectDtp)
                Assert.NotNull(sl.AllDTP);
            else
                Assert.Null(sl.AllDTP);
        }

        // 7. Build270Request
        [Fact]
        public async Task Build270Request_HappyPath_ShouldPopulateSegments()
        {
            var gen = CreateGenerator(true);
            var dto = BuildEligibilityDto("Availity (54321)", submitDate: new DateTime(2024, 1, 12), providerName: "First Last", renderingName: "Rnd First Last", clientName: "Client One", genderId: 1);
            var ts = await gen.Build270Request(dto, "000000321", "005010X279A1");
            Assert.NotNull(ts);
            Assert.Equal("270", ts.ST.TransactionSetIdentifierCode_01);
            Assert.NotNull(ts.BHT_BeginningOfHierarchicalTransaction);
            Assert.NotNull(ts.Loop2000A);
        }

        [Fact]
        public async Task Build270Request_DefaultSubmitDate_ShouldUseUtcNowFormat()
        {
            var gen = CreateGenerator(true);
            var dto = BuildEligibilityDto("Availity (54321)", submitDate: default);
            var ts = await gen.Build270Request(dto, "000000321", "005010X279A1");
            Assert.NotNull(ts);
            Assert.Equal(8, ts.BHT_BeginningOfHierarchicalTransaction.TransactionSetCreationDate_04.Length);
        }

        [Theory]
        [InlineData("First", null, null, null, null)]
        [InlineData("First Last", "Last", "First", null, "Last")]
        [InlineData("First Middle Last", "Last", "First", "Middle", "Last")]
        public async Task Build270Request_NameSplitting_ShouldMapProperly(string fullName, string expLast, string expFirst, string expMid, string expLastAssert)
        {
            var gen = CreateGenerator(true);
            var dto = BuildEligibilityDto("Availity (54321)", clientName: fullName);
            var ts = await gen.Build270Request(dto, "000000321", "005010X279A1");
            var nm1 = ts.Loop2000A.First().Loop2000B.First().Loop2000C.First().Loop2100C.NM1_SubscriberName;
            Assert.Equal(expFirst ?? string.Empty, nm1.ResponseContactFirstName_04);
        }



        [Fact]
        public void SplitPersonName_Null_ReturnsNulls()
        {
            var gen = CreateGenerator(true);
            var res = InvokePrivate<(string First, string Middle, string Last)>(gen, "SplitPersonName", new object[] { null });
            Assert.Null(res.First);
            Assert.Null(res.Middle);
            Assert.Null(res.Last);
        }

        [Theory]
        [InlineData("Solo", null, null, "Solo")]
        [InlineData("First Last", "First", null, "Last")]
        [InlineData("First Middle Last", "First", "Middle", "Last")]
        public void SplitPersonName_Variants_ShouldSplit(string input, string first, string middle, string last)
        {
            var gen = CreateGenerator(true);
            var res = InvokePrivate<(string First, string Middle, string Last)>(gen, "SplitPersonName", new object[] { input });
            Assert.Equal(first, res.First);
            Assert.Equal(middle, res.Middle);
            Assert.Equal(last, res.Last);
        }

        [Fact]
        public void GetCASEntry_IndexOutOfRange_ReturnsNull()
        {
            var gen = CreateGenerator(true);
            var list = new List<PaymentClaimAdjustmentEntity>
        {
        new PaymentClaimAdjustmentEntity()
        };
            var res = InvokePrivate<PaymentClaimAdjustmentEntity>(gen, "GetCASEntry", new object[] { list, 5 });
            Assert.Null(res);
        }

        [Fact]
        public void GetCASEntry_IndexInRange_ReturnsItem()
        {
            var gen = CreateGenerator(true);
            var list = new List<PaymentClaimAdjustmentEntity>
        {
        new PaymentClaimAdjustmentEntity { AdjustmentReasonCode = "A" },
        new PaymentClaimAdjustmentEntity { AdjustmentReasonCode = "B" }
        };
            var res = InvokePrivate<PaymentClaimAdjustmentEntity>(gen, "GetCASEntry", new object[] { list, 2 });
            Assert.NotNull(res);
            Assert.Equal("B", res.AdjustmentReasonCode);
        }

        private static T InvokePrivate<T>(object target, string methodName, object[] args)
        {
            var mi = target.GetType().GetMethod(methodName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (mi != null && mi.IsGenericMethodDefinition)
            {
                mi = mi.MakeGenericMethod(typeof(T));
            }
            return (T)mi.Invoke(target, args);
        }

    }
}