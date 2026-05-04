using Billing.FolderStructure.Core.Enum;
using BillingService.Domain.Enums;
using BillingService.Domain.Extensions;
using BillingService.Domain.Models.Claims;
using EdiFabric.Core.Model.Edi;
using EdiFabric.Core.Model.Edi.X12;
using EdiFabric.Framework;
using EdiFabric.Framework.Writers;
using EdiFabric.Templates.Hipaa5010;
using Rethink.Services.Common.Dtos.Billing;
using Rethink.Services.Common.Entities.Base;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Extensions;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.BillingSettings;
using Rethink.Services.Common.Models.Claim;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing.EDI
{
    public abstract class RepositorySettings
    {
        public string IsaReceiverId { get; set; }
        public string GsReceiverId { get; set; }
        public string SecurityInfo { get; set; }
        public string ProfessionalClaimsVersion { get; set; }
        public string Professional270Version { get; set; }

        public string ReceiverId { get; set; }
    }

    public class ChangeHealthcareRepositorySettings : RepositorySettings
    {
        public ChangeHealthcareRepositorySettings()
        {
            SecurityInfo = "CYCTRANS";
            ProfessionalClaimsVersion = "005010X222A1";
            Professional270Version="005010X279A1";
        }
    }

    public class EdiGenerator
    {
        private readonly RepositorySettings _repositorySettings = new ChangeHealthcareRepositorySettings();
        private readonly string _billerRethinkId;
        private readonly string _submitterRethinkId;
        private readonly string _submitterRethinkName;
        private readonly string _submitterRethinkEmail;
        private readonly string _submitterRethinkPhone;
        private string _testMode;
        private readonly string _customerId;
        private readonly string _ClearingHouseName;
        private readonly string _taxId;
        private readonly IRethinkMasterDataMicroServices _rethinkMicroservicesRepository;
        private readonly string _senderId;
        private readonly string _receiverId;

        public EdiGenerator(
                            bool testMode,
                            string billerRethinkId,
                            string submitterRethinkId,
                            string submitterRethinkName,
                            string submitterRethinkEmail,
                            string submitterRethinkPhone,
                            string customerId,
                            string clearingHouseName,
                            string taxId,
                            IRethinkMasterDataMicroServices rethinkMicroservicesRepository,
                            string senderId,
                            string receiverId
                           )
        {
            _testMode = testMode ? "T" : "P";
            _submitterRethinkName = submitterRethinkName;
            _submitterRethinkEmail = submitterRethinkEmail;
            _submitterRethinkPhone = submitterRethinkPhone;
            _billerRethinkId = billerRethinkId;
            _submitterRethinkId = submitterRethinkId;
            _customerId = customerId;
            _ClearingHouseName = clearingHouseName;
            _taxId = taxId;
            _repositorySettings.GsReceiverId = _billerRethinkId;
            _repositorySettings.IsaReceiverId = _billerRethinkId;
            _repositorySettings.ReceiverId = _billerRethinkId;
            _rethinkMicroservicesRepository = rethinkMicroservicesRepository;
            _senderId = senderId;
            _receiverId = receiverId;
        }

        private class OrderedDxCode
        {
            public OrderedDxCode(string diagnosisCode, int order, DiagnosisTypes diagnosisType)
            {
                DiagnosisCode = diagnosisCode;
                Order = order;
                DiagnosisType = diagnosisType;
            }
            public string DiagnosisCode { get; }
            public int Order { get; }
            public DiagnosisTypes DiagnosisType { get; }
        }

        public async Task<string> GenerateEdi(ClaimSubmissionEntity claimSubmission, ClearingHouseClaimModel claimModelDto, BillingFunderSettings billingFunderSettings, ClaimBillingProviderOtherDto otherBillingProvider)
        {
            switch (claimSubmission.DocumentType)
            {
                case ClaimDocumentType.Doc837P:
                    return await Generate837PEdi(claimSubmission, claimModelDto, billingFunderSettings, otherBillingProvider);
                case ClaimDocumentType.Unknown:
                case ClaimDocumentType.Doc837I:
                case ClaimDocumentType.HCFA1500Single:
                case ClaimDocumentType.HCFA1500Multi:
                case ClaimDocumentType.UB04Single:
                case ClaimDocumentType.UB04Multi:
                case ClaimDocumentType.NonInsurance:
                default:
                    throw new Exception($"Unsupported document type {claimSubmission.DocumentType}");
            }

        }

        private async Task<string> Generate837PEdi(ClaimSubmissionEntity claimSubmission, ClearingHouseClaimModel claimModelDto, BillingFunderSettings billingFunderSettings, ClaimBillingProviderOtherDto otherBillingProvider)
        {          
            var controlNumber = claimSubmission.Id.ToString().PadLeft(9, '0');

            var renderingProviders = await _rethinkMicroservicesRepository.GetAllRenderingProvidersAsync(claimSubmission.Claim.AccountInfoId);

            var claim837p = await BuildClaim(claimSubmission, controlNumber, claimModelDto, renderingProviders, billingFunderSettings, otherBillingProvider);
            var edi=string.Empty;
            if (claimModelDto.clearinghouseId == (int)BillingClearingHousesEnum.Stedi)
            {
                edi = ToStediEdi(claim837p, controlNumber);
            }
            else
            {
                edi = ToEdi(claim837p, controlNumber);
            }
        
            return edi;
        }

        public string ToEdi(EdiMessage claim, string groupControlNumber)
        {
            var separators = Separators.X12;
            //separators.ComponentDataElement = ':'; // the colon is more commonly used than '>' which it defaults to
            var settings = new X12WriterSettings()
            {
                Separators = separators,
                PreserveWhitespace = true
            };
            using (var stream = new MemoryStream())
            {
                using (var writer = new X12Writer(stream))
                {
                    writer.Write(SegmentBuilders.BuildIsa(groupControlNumber,
                                                          _repositorySettings.SecurityInfo,
                                                          _submitterRethinkId,
                                                          _repositorySettings.IsaReceiverId,
                                                          _testMode));
                    writer.Write(SegmentBuilders.BuildGs(groupControlNumber,
                                                        _customerId,
                                                        _repositorySettings.GsReceiverId,
                                                        _repositorySettings.ProfessionalClaimsVersion));

                    writer.Write(claim, true, separators);
                }

                return stream.LoadToString();
            }

        }

        public string ToEdi270(EdiMessage claim, string groupControlNumber)
        {
            var separators = Separators.X12;
            separators.DataElement = '*';
            separators.ComponentDataElement = ':';    // <-- important
            separators.RepetitionDataElement = '^';

            var settings = new X12WriterSettings
            {
                Separators = separators,
                PreserveWhitespace = true
            };

            using var stream = new MemoryStream();
            var isa = SegmentBuilders270.BuildIsa(
                        controlNumber: groupControlNumber,
                        securityInfo: null,                        // leave ISA04 as 10 spaces
                        senderId: _customerId,             // YOUR sender ID (right-padded inside builder)
                        receiverId: _repositorySettings.IsaReceiverId, // Availity receiver ID
                        testIndicator: _testMode,                       // true => "T" in ISA15
                        senderQualifier: "01",                     // or "ZZ" depending on partner; change as needed
                        receiverQualifier: "ZZ",
                        ackRequested: "1",
                        repetitionSeparator: '^',
                        componentElementSeparator: ':'
                    );

            // Ensure ISA16 (component separator) is set on the object (fallback)
            EnsureIsa16(isa, ':');

            var gs = SegmentBuilders270.BuildGs(
                 groupControlNumber,
                 _submitterRethinkId,
                 _repositorySettings.GsReceiverId,
                _repositorySettings.Professional270Version
            );

            // Use writer with settings (IMPORTANT)
            using (var writer = new X12Writer(stream, settings))
            {
                writer.Write(isa);
                writer.Write(gs);
                writer.Write(claim, true, separators);
            }


            return stream.LoadToString();
        }

        /// <summary>
        /// Build claim.
        /// Original from http://www.x12.org/examples/005010X222/commercial-health-insurance/
        /// </summary>
        public async Task<TS837P> BuildClaim(ClaimSubmissionEntity data,
                                             string controlNumber, ClearingHouseClaimModel claimModelDto,
                                             ClientListUserModel renderingProviders, BillingFunderSettings billingFunderSettings, ClaimBillingProviderOtherDto otherBillingProvider)
        {
            try
            {

                var result = new TS837P();
                bool useOtherBillingProvider =
                                otherBillingProvider != null &&
                               !string.IsNullOrWhiteSpace(otherBillingProvider.ProviderType);

                //  Indicates the start of a claim transaction set and assigns a control number.
                result.ST = new ST
                {
                    TransactionSetIdentifierCode_01 = "837",
                    TransactionSetControlNumber_02 = controlNumber,
                    ImplementationConventionPreference_03 = "005010X222A1"
                };

                //  Indicates when the claim was created and the submission identifier (same as control number). 
                result.BHT_BeginningOfHierarchicalTransaction = new BHT_BeginningOfHierarchicalTransaction_8();
                result.BHT_BeginningOfHierarchicalTransaction.HierarchicalStructureCode_01 = "0019";
                result.BHT_BeginningOfHierarchicalTransaction.TransactionSetPurposeCode_02 = "00";
                result.BHT_BeginningOfHierarchicalTransaction.SubmitterTransactionIdentifier_03 = data.ClaimSubmissionIdentifier.TrimAndEmptyIfNullOrWhitespace();
                result.BHT_BeginningOfHierarchicalTransaction.TransactionSetCreationDate_04 = $"{data.SubmitDate:yyyyMMdd}";
                result.BHT_BeginningOfHierarchicalTransaction.TransactionSetCreationTime_05 = $"{data.SubmitDate:hhmm}";
                result.BHT_BeginningOfHierarchicalTransaction.TransactionTypeCode_06 = "CH";

                result.AllNM1 = new All_NM1_837P_6();

                //  Begin 1000A Loop SUBMITTER
                result.AllNM1.Loop1000A = new Loop_1000A_837P
                {
                    NM1_SubmitterName = new NM1_InformationReceiverName_4
                    {
                        EntityIdentifierCode_01 = "41",
                        EntityTypeQualifier_02 = "2",
                        ResponseContactLastorOrganizationName_03 = _submitterRethinkName.TrimAndEmptyIfNullOrWhitespace(),
                        IdentificationCodeQualifier_08 = "46",
                        ResponseContactIdentifier_09 = $"{_taxId.TrimAndEmptyIfNullOrWhitespace()}"
                    }
                };

                //  Repeating PER
                //  SUBMITTER CONTACT PERSON 
                result.AllNM1.Loop1000A.PER_SubmitterEDIContactInformation = new List<PER_BillingProviderContactInformation>();

                result.AllNM1.Loop1000A.PER_SubmitterEDIContactInformation.Add(
                    new PER_BillingProviderContactInformation
                    {
                        ContactFunctionCode_01 = "IC",
                        CommunicationNumberQualifier_03 = "EM",
                        ResponseContactCommunicationNumber_04 = _submitterRethinkEmail.TrimAndEmptyIfNullOrWhitespace(),
                        CommunicationNumberQualifier_05 = "TE",
                        ResponseContactCommunicationNumber_06 = _submitterRethinkPhone.TrimAndEmptyIfNullOrWhitespace()
                    });
                //  End 1000A Loop SUBMITTER

                //  Begin 1000B Loop RECEIVER
                result.AllNM1.Loop1000B = new Loop_1000B_837P
                {
                    NM1_ReceiverName = new NM1_ReceiverName
                    {
                        EntityIdentifierCode_01 = "40",
                        EntityTypeQualifier_02 = "2",
                        ResponseContactLastorOrganizationName_03 = data.ClearinghouseSubmitterName.TrimAndNullIfWhitespace() ?? _ClearingHouseName.TrimAndEmptyIfNullOrWhitespace(),
                        IdentificationCodeQualifier_08 = "46",
                        ResponseContactIdentifier_09 =data.ClearinghouseIdentifier.TrimAndNullIfWhitespace() ?? _repositorySettings.ReceiverId.TrimAndEmptyIfNullOrWhitespace()

                    }
                };
                //  End 1000B Loop RECEIVER

                //  Repeating 2000A Loops
                result.Loop2000A = new List<Loop_2000A_837P>();
                //  Begin 2000A Loop BILLING PROVIDER Hierarchy
                var loop2000A = new Loop_2000A_837P
                {
                    //  Hierarchical Number 1
                    HL_BillingProviderHierarchicalLevel = new HL_BillingProviderHierarchicalLevel
                    {
                        HierarchicalIDNumber_01 = "1",
                        HierarchicalLevelCode_03 = "20",
                        HierarchicalChildCode_04 = "1"
                    },
                    PRV_BillingProviderSpecialtyInformation = new PRV_BillingProviderSpecialtyInformation
                    {
                        ProviderCode_01 = "BI",
                        ReferenceIdentificationQualifier_02 = "PXC",
                        ProviderTaxonomyCode_03 = useOtherBillingProvider
                                                ? otherBillingProvider.TaxonomyCode.TrimAndEmptyIfNullOrWhitespace()
                                                : data.LocationBillingProviderTaxonomyCode.TrimAndEmptyIfNullOrWhitespace()
                        //ProviderTaxonomyCode_03 = "251S00000X"
                    }
                };


                //  Begin 2010AA Loop BILLING PROVIDER
                //AS discussed with Robert, changing below logic based on firstname to avoid 277 issue.
                //string entityTypeQualifier_02 = data.FunderBillingProviderOption.HasValue &&
                //                                data.FunderBillingProviderOption.Value == BillingProviderOptionType.Individual ? "1" : "2";


                var entityTypeQualifier_02 = useOtherBillingProvider
                                            ? (string.Equals(otherBillingProvider.ProviderType, "Person", StringComparison.OrdinalIgnoreCase) ? "1" : "2")
                                            : (!string.IsNullOrEmpty(data.ResolvedBillingProviderFirstName) ? "1" : "2");

                loop2000A.AllNM1 = new All_NM1_837P
                {
                    Loop2010AA = new Loop_2010AA_837P
                    {
                        NM1_BillingProviderName = new NM1_BillingProviderName_2
                        {
                            EntityIdentifierCode_01 = "85",
                            EntityTypeQualifier_02 = entityTypeQualifier_02,
                            ResponseContactLastorOrganizationName_03 = useOtherBillingProvider
                                ? otherBillingProvider.LastNameOrFacilityName.TrimAndEmptyIfNullOrWhitespace()
                                : data.ResolvedBillingProviderName.TrimAndEmptyIfNullOrWhitespace(),
                            ResponseContactFirstName_04 = entityTypeQualifier_02 == "1"
                                ? (useOtherBillingProvider
                                    ? otherBillingProvider.FirstName.TrimAndEmptyIfNullOrWhitespace()
                                    : data.ResolvedBillingProviderFirstName.TrimAndEmptyIfNullOrWhitespace())
                                : "",
                            ResponseContactMiddleName_05 = entityTypeQualifier_02 == "1"
                                ? (useOtherBillingProvider
                                    ? ""
                                    : data.ResolvedBillingProviderMiddleName.TrimAndEmptyIfNullOrWhitespace())
                                : "",
                            IdentificationCodeQualifier_08 = "XX",
                            ResponseContactIdentifier_09 = useOtherBillingProvider ? otherBillingProvider.NPI : data.ResolvedBillingProviderNpi.TrimAndEmptyIfNullOrWhitespace()
                        },
                        N3_BillingProviderAddress = new N3_AdditionalPatientInformationContactAddress
                        {
                            ResponseContactAddressLine_01 = useOtherBillingProvider ? otherBillingProvider.AddressLine1 : 
                                                                                    (data.LocationBillingProviderAddress1 ??
                                                                                    data.AccountAddress1).Trim().Substring(0, Math.Min((data.LocationBillingProviderAddress1 ??
                                                                                    data.AccountAddress1).Length, 55)),

                            ResponseContactAddressLine_02 = useOtherBillingProvider ? otherBillingProvider.AddressLine2
                                                                                    : (data.LocationBillingProviderAddress2 ??
                                                                                        data.AccountAddress2).Trim().Substring(0, Math.Min((data.LocationBillingProviderAddress2 ??
                                                                                        data.AccountAddress2).Length, 55)),
                        },
                        N4_BillingProviderCity_State_ZIPCode = new N4_AdditionalPatientInformationContactCity()
                        {
                            AdditionalPatientInformationContactCityName_01 = useOtherBillingProvider ? otherBillingProvider.City
                                                                                                     : (data.LocationBillingProviderCity ??
                                                                                                        data.AccountBillingCity).Trim().Substring(0, Math.Min((data.LocationBillingProviderCity ??
                                                                                                         data.AccountBillingCity).Length, 30)),

                            AdditionalPatientInformationContactStateCode_02 = useOtherBillingProvider ? otherBillingProvider.State
                                                                                                    : (data.LocationBillingProviderState ??
                                                                                                        data.AccountBillingState).Trim().Substring(0, Math.Min((data.LocationBillingProviderState ??
                                                                                                        data.AccountBillingState).Length, 2)),


                            AdditionalPatientInformationContactPostalZoneorZIPCode_03 = useOtherBillingProvider ? $"{otherBillingProvider.Zip}-{otherBillingProvider.ZipExt}"
                                                                                                                : (data.LocationBillingProviderZip ??
                                                                                                                    data.AccountBillingZip).Trim().Substring(0, Math.Min((data.LocationBillingProviderZip ??
                                                                                                                    data.AccountBillingZip).Length, 15)),
                        },
                        AllREF = new All_REF_837P_8
                        {
                            REF_BillingProviderTaxIdentification = new REF_BillingProviderTaxIdentification
                            {
                                ReferenceIdentificationQualifier_01 = "EI",
                                ReferenceIdentificationREF_02 = useOtherBillingProvider ? otherBillingProvider.TaxId.TrimAndEmptyIfNullOrWhitespace()
                                                                                        : data.ResolvedBillingProviderFederalTaxID.TrimAndEmptyIfNullOrWhitespace()
                            }
                        },

                    }
                };

                PER_BillingProviderContactInformation billingProviderContactInfo = null;
                if (!string.IsNullOrWhiteSpace(data.AccountBillingProviderEmail) ||
                    !string.IsNullOrWhiteSpace(data.AccountBillingProviderPhone) ||
                    !string.IsNullOrWhiteSpace(data.AccountBillingProviderFax))
                {
                    billingProviderContactInfo = new PER_BillingProviderContactInformation()
                    {
                        ContactFunctionCode_01 = "IC"
                    };
                    if (!string.IsNullOrWhiteSpace(data.AccountBillingProviderEmail))
                    {
                        billingProviderContactInfo.CommunicationNumberQualifier_03 = "EM";
                        billingProviderContactInfo.ResponseContactCommunicationNumber_04 = data.AccountBillingProviderEmail.TrimAndEmptyIfNullOrWhitespace();
                    }
                    if (!string.IsNullOrWhiteSpace(data.AccountBillingProviderPhone))
                    {
                        billingProviderContactInfo.CommunicationNumberQualifier_05 = "TE";
                        billingProviderContactInfo.ResponseContactCommunicationNumber_06 = data.AccountBillingProviderPhone.TrimAndEmptyIfNullOrWhitespace();
                    }
                    if (!string.IsNullOrWhiteSpace(data.AccountBillingProviderFax))
                    {
                        billingProviderContactInfo.CommunicationNumberQualifier_03 = "FX";
                        billingProviderContactInfo.ResponseContactCommunicationNumber_04 = data.AccountBillingProviderFax.TrimAndEmptyIfNullOrWhitespace();
                    }
                    loop2000A.AllNM1.Loop2010AA.PER_BillingProviderContactInformation = new List<PER_BillingProviderContactInformation>
                {
                    billingProviderContactInfo
                };
                }
                //  End 2010AA Loop BILLING PROVIDER


                //  Begin 2010AB Loop PAY-TO PROVIDER - not used
                /*
                loop2000A.AllNM1.Loop2010AB = new Loop_2010AB_837P
                {
                    NM1_Pay_AddressName =
                        new NM1_Pay
                        {
                            EntityIdentifierCode_01 = "87",
                            EntityTypeQualifier_02 = "???",
                            ResponseContactLastorOrganizationName_03 = "???"
                        },
                    N3_Pay_ToAddress_ADDRESS =
                        new N3_AdditionalPatientInformationContactAddress {ResponseContactAddressLine_01 = "???"},
                    N4_Pay_AddressCity_State_ZIPCode = new N4_AdditionalPatientInformationContactCity
                    {
                        AdditionalPatientInformationContactCityName_01 = "???",
                        AdditionalPatientInformationContactStateCode_02 = "???",
                        AdditionalPatientInformationContactPostalZoneorZIPCode_03 = "???"
                    }
                };
                */
                //  End 2010AB Loop PAY-TO PROVIDER

                //  Repeating 2000B Loops
                loop2000A.Loop2000B = new List<Loop_2000B_837P>();
                var currentFunder = GetResponsibilitySequence(data.ClaimSubmissionFunderSequences, data.ResponsibilitySequence);
                // Begin 2000B Loop SUBSCRIBER Hierarchy
                var loop2000B = new Loop_2000B_837P
                {
                    // Hierarchical Number 2
                    HL_SubscriberHierarchicalLevel = new HL_SubscriberHierarchicalLevel
                    {
                        HierarchicalIDNumber_01 = "2",
                        HierarchicalParentIDNumber_02 = "1",
                        HierarchicalLevelCode_03 = "22", // Subscriber
                        HierarchicalChildCode_04 = currentFunder.RelationshipToSubscriber.HasValue &&
                                                  currentFunder.RelationshipToSubscriber.Value == 1 ? "0" : "1",
                    },
                    // SUBSCRIBER GROUP #
                    SBR_SubscriberInformation = new SBR_SubscriberInformation
                    {
                        PayerResponsibilitySequenceNumberCode_01 = data.ResponsibilitySequence,
                        IndividualRelationshipCode_02 = currentFunder.RelationshipToSubscriber.HasValue &&
                                                       currentFunder.RelationshipToSubscriber.Value == 1 ? "18" : null,
                        InsuredGrouporPolicyNumber_03 = currentFunder.InsuranceGroupNumber.TrimAndEmptyIfNullOrWhitespace(),
                        ClaimFilingIndicatorCode_09 = billingFunderSettings.ClaimFilingIndicator
                    },
                    AllNM1 = new All_NM1_837P_2
                    {
                        // Begin 2010BA Loop SUBSCRIBER
                        Loop2010BA = new Loop_2010BA_837P
                        {
                            NM1_SubscriberName = new NM1_SubscriberName_5
                            {
                                EntityIdentifierCode_01 = "IL",
                                EntityTypeQualifier_02 = "1",
                                ResponseContactLastorOrganizationName_03 = currentFunder.SubscriberLastName.TrimAndEmptyIfNullOrWhitespace(),
                                ResponseContactFirstName_04 = currentFunder.SubscriberFirstName.TrimAndEmptyIfNullOrWhitespace(),
                                ResponseContactMiddleName_05 = currentFunder.SubscriberMiddleName.TrimAndEmptyIfNullOrWhitespace(),
                                IdentificationCodeQualifier_08 = "MI",
                                ResponseContactIdentifier_09 = !string.IsNullOrEmpty(currentFunder.InsurancePolicyNumber)
                                ? Regex.Replace(currentFunder.InsurancePolicyNumber, @"[^a-zA-Z0-9]", "")[..Math.Min(Regex.Replace(currentFunder.InsurancePolicyNumber, @"[^a-zA-Z0-9]", "").Length, 80)]
                                : ""
                            }
                        }
                    }
                };
                // Add conditional properties separately
                if (currentFunder.RelationshipToSubscriber.HasValue && currentFunder.RelationshipToSubscriber.Value == 1)
                {
                    loop2000B.AllNM1.Loop2010BA.DMG_SubscriberDemographicInformation = new DMG_PatientDemographicInformation
                    {
                        DateTimePeriodFormatQualifier_01 = "D8",
                        DependentBirthDate_02 = currentFunder.SubscriberDOB?.ToEdiString(),
                        DependentGenderCode_03 = currentFunder.SubscriberGender.TrimAndEmptyIfNullOrWhitespace()
                    };
                    loop2000B.AllNM1.Loop2010BA.N3_SubscriberAddress = new N3_AdditionalPatientInformationContactAddress
                    {
                        ResponseContactAddressLine_01 = !string.IsNullOrEmpty(currentFunder.SubscriberAddress1)
                            ? currentFunder.SubscriberAddress1.Trim().Substring(0, Math.Min(currentFunder.SubscriberAddress1.Trim().Length, 55))
                            : "",
                        ResponseContactAddressLine_02 = !string.IsNullOrEmpty(currentFunder.SubscriberAddress2)
                            ? currentFunder.SubscriberAddress2.Trim().Substring(0, Math.Min(currentFunder.SubscriberAddress2.Trim().Length, 55))
                            : ""
                    };
                    loop2000B.AllNM1.Loop2010BA.N4_SubscriberCity_State_ZIPCode = new N4_AdditionalPatientInformationContactCity
                    {
                        AdditionalPatientInformationContactCityName_01 = !string.IsNullOrEmpty(currentFunder.SubscriberCity)
                            ? currentFunder.SubscriberCity.Trim().Substring(0, Math.Min(currentFunder.SubscriberCity.Trim().Length, 30))
                            : "",
                        AdditionalPatientInformationContactStateCode_02 = !string.IsNullOrEmpty(currentFunder.SubscriberState)
                            ? currentFunder.SubscriberState.Trim().Substring(0, Math.Min(currentFunder.SubscriberState.Trim().Length, 2))
                            : "",
                        AdditionalPatientInformationContactPostalZoneorZIPCode_03 = !string.IsNullOrEmpty(currentFunder.SubscriberZip)
                            ? currentFunder.SubscriberZip.Trim().Substring(0, Math.Min(currentFunder.SubscriberZip.Trim().Length, 15))
                            : ""
                    };
                }
                // Begin 2010BB Loop PAYER
                loop2000B.AllNM1.Loop2010BB = new Loop_2010BB_837P
                {
                    NM1_PayerName = new NM1_OtherPayerName
                    {
                        EntityIdentifierCode_01 = "PR",
                        EntityTypeQualifier_02 = "2",
                        ResponseContactLastorOrganizationName_03 = currentFunder.FunderName.TrimAndEmptyIfNullOrWhitespace(),
                        IdentificationCodeQualifier_08 = "PI",
                        ResponseContactIdentifier_09 = currentFunder.FunderVendorId.TrimAndEmptyIfNullOrWhitespace()
                    }
                };

                //  Repeating 2000C Loops
                loop2000B.Loop2000C = new List<Loop_2000C_837P>();

                Loop_2000C_837P loop2000C = null;
                // If patient is not the subscriber (e.g. patient is child)
                if (currentFunder.RelationshipToSubscriber.Value != 1 /*self*/)
                {
                    //  Begin 2000C Loop PATIENT Hierarchy
                    loop2000C = new Loop_2000C_837P
                    {
                        HL_PatientHierarchicalLevel = new HL_DependentLevel
                        {
                            //  Hierarchical Number 3
                            HierarchicalIDNumber_01 = "3",
                            HierarchicalParentIDNumber_02 = "2",
                            HierarchicalLevelCode_03 = "23",
                            HierarchicalChildCode_04 = "0"
                        },
                        PAT_PatientInformation = new PAT_PatientInformation
                        {
                            IndividualRelationshipCode_01 =
                                currentFunder.RelationshipToSubscriber == 2 ? "19" /*child*/
                                                                                 : "53" /*spouse*/
                        },
                        Loop2010CA = new Loop_2010CA_837P
                        {
                            //  Begin 2010CA Loop PATIENT
                            NM1_PatientName = new NM1_PatientName_3
                            {
                                //  PATIENT name
                                EntityIdentifierCode_01 = "QC",
                                EntityTypeQualifier_02 = "1",
                                ResponseContactLastorOrganizationName_03 = data.ChildProfileLastName.TrimAndEmptyIfNullOrWhitespace(),
                                ResponseContactFirstName_04 = data.ChildProfileFirstName.TrimAndEmptyIfNullOrWhitespace(),
                                ResponseContactMiddleName_05 = data.ChildProfileMiddleName.TrimAndEmptyIfNullOrWhitespace(),
                            },
                            N3_PatientAddress = new N3_AdditionalPatientInformationContactAddress
                            {
                                //  PATIENT ADDRESS
                                ResponseContactAddressLine_01 = !String.IsNullOrEmpty(data.ChildProfileAddress1) ? data.ChildProfileAddress1.Trim().Substring(0, Math.Min(data.ChildProfileAddress1.Trim().Length, 55)) : "",
                                ResponseContactAddressLine_02 = !String.IsNullOrEmpty(data.ChildProfileAddress2) ? data.ChildProfileAddress2.Trim().Substring(0, Math.Min(data.ChildProfileAddress2.Trim().Length, 55)) : "",
                            },
                            N4_PatientCity_State_ZIPCode = new N4_AdditionalPatientInformationContactCity
                            {
                                //  PATIENT LOCATION
                                AdditionalPatientInformationContactCityName_01 = !String.IsNullOrEmpty(data.ChildProfileCity) ? data.ChildProfileCity.Trim().Substring(0, Math.Min(data.ChildProfileCity.Trim().Length, 30)) : "",
                                AdditionalPatientInformationContactStateCode_02 = !String.IsNullOrEmpty(data.ChildProfileState) ? data.ChildProfileState.Trim().Substring(0, Math.Min(data.ChildProfileState.Trim().Length, 2)) : "",
                                AdditionalPatientInformationContactPostalZoneorZIPCode_03 = !String.IsNullOrEmpty(data.ChildProfileState) ? data.ChildProfileZip.Trim().Substring(0, Math.Min(data.ChildProfileZip.Trim().Length, 15)) : "",
                                //CountryCode_04                                            = data.ChildProfileCountry 

                            },
                            DMG_PatientDemographicInformation = new DMG_PatientDemographicInformation
                            {
                                //  PATIENT gender and DOB
                                DateTimePeriodFormatQualifier_01 = "D8",
                                DependentBirthDate_02 = data.ChildProfileDOB?.ToEdiString(),
                                DependentGenderCode_03 = data.ChildProfileGender.TrimAndEmptyIfNullOrWhitespace()
                            }
                        }
                    };
                    //  End 2010CA Loop PATIENT
                }
                else
                {
                    loop2000C = new Loop_2000C_837P();
                }

                //  Repeating 2300 Loops
                loop2000C.Loop2300 = new List<Loop_2300_837P>();

                string frequency;
                switch (data.FrequencyType)
                {
                    case ClaimFrequencyType.Replacement:
                        frequency = "7";
                        break;
                    case ClaimFrequencyType.Void:
                        frequency = "8";
                        break;
                    case ClaimFrequencyType.Original:
                    default:
                        frequency = "1";
                        break;
                }

                //  Begin 2300 Loop CLAIM
                var loop2300 = new Rethink_Loop_2300_837P
                {
                    CLM_ClaimInformation = new CLM_ClaimInformation_3
                    {
                        //  Claim info
                        PatientControlNumber_01 = data.ClaimSubmissionIdentifier.TrimAndEmptyIfNullOrWhitespace(),
                        TotalClaimChargeAmount_02 = data.ClaimSubmissionServiceLines.Sum(sl => sl.Charges ?? 0).ToEdiString(),
                        HealthCareServiceLocationInformation_05 =
                            new C023_HealthCareServiceLocationInformation_2
                            {
                                FacilityTypeCode_01 = data.PlaceOfServiceCode.TrimAndEmptyIfNullOrWhitespace(),
                                FacilityCodeQualifier_02 = "B",
                                ClaimFrequencyTypeCode_03 = frequency
                            },
                        ProviderorSupplierSignatureIndicator_06 = "Y",
                        AssignmentorPlanParticipationCode_07 = "A",

                        //Bug : 218400
                        //BenefitsAssignmentCertificationIndicator_08 = data.AuthorizedPaymentConfirmationType.TrimAndEmptyIfNullOrWhitespace(),
                        //ReleaseofInformationCode_09 = data.ReleaseOfInformationConfirmationType.TrimAndNullIfWhitespace() == "N" ? "I" : "Y",

                        BenefitsAssignmentCertificationIndicator_08 = "Y",
                        ReleaseofInformationCode_09 = "Y",

                        //End Bug: 218400

                        PatientSignatureSourceCode_10 = "P", //, P = Patient was not present for service
                        //DelayReasonCode_20                          = "???" // TODO: collect from UI 
                    }
                    //,
                    //AllREF = new All_REF_837P_6
                    //{
                    //    REF_PriorAuthorization = new REF_OtherPayerPriorAuthorizationNumber
                    //    {
                    //        ReferenceIdentificationQualifier_01 = "G1",
                    //        ReferenceIdentificationREF_02 = !String.IsNullOrEmpty(data.AuthorizationNumber) ? data.AuthorizationNumber.Trim().Substring(0, Math.Min(data.AuthorizationNumber.Trim().Length, 50)) : "" // The spec says to ignore 03 and 04
                    //    }
                    //}

                };

                All_REF_837P_6 allRef = null;

                if (!string.IsNullOrEmpty(data.AuthorizationNumber) && billingFunderSettings.AuthorizationStatus != (int)AuthorizationStatus.NotNeeded)
                {
                    allRef ??= new All_REF_837P_6();
                    allRef.REF_PriorAuthorization = new REF_OtherPayerPriorAuthorizationNumber
                    {
                        ReferenceIdentificationQualifier_01 = "G1",
                        ReferenceIdentificationREF_02 = data.AuthorizationNumber.Trim().Substring(0, Math.Min(data.AuthorizationNumber.Trim().Length, 50))
                    };
                }

                //Added by Navnath for claim notes
                if (!string.IsNullOrEmpty(data.Claim.Note))
                {
                    loop2300.NTE_ClaimNote = new NTE_ClaimNote_2()
                    {

                        NoteReferenceCode_01 = "ADD",
                        BillingNoteText_02 = data.Claim.Note?.Trim().Substring(0, Math.Min(data.Claim.Note.Trim().Length, 80))
                    };
                }
                //end

                #region RHD-3458 Remove the AMT segment completely for all payers
                //if (data.TotalPatientPaid != null)
                //{
                //    loop2300.AMT_PatientAmountPaid = new AMT_PatientAmountPaid
                //    {
                //        AmountQualifierCode_01 = "F5",
                //        TotalClaimChargeAmount_02 = data.TotalPatientPaid?.ToEdiString()
                //    };
                //}

                #endregion

                // for voids and replacements, we need to send the previous ClaimSubmissionIdentifier in 2300.REF02

                if ((data.FrequencyType == ClaimFrequencyType.Replacement || data.FrequencyType == ClaimFrequencyType.Void)
                        && !string.IsNullOrEmpty(data.Claim?.OriginalClaim))
                {
                    allRef ??= new All_REF_837P_6();
                    allRef.REF_PayerClaimControlNumber = new REF_OtherPayerClaimControlNumber
                    {
                        ReferenceIdentificationQualifier_01 = "F8",
                        ReferenceIdentificationREF_02 = data.Claim?.OriginalClaim.TrimAndEmptyIfNullOrWhitespace()// from the previous claim

                    };
                }
                // if this is a COB transfer, we will send the 835's PayerClaimControlNumber in 2300.REF02
                //else if (!string.IsNullOrWhiteSpace(data.PayerClaimControlNumber))
                //{
                //    allRef ??= new All_REF_837P_6();
                //    allRef.REF_PayerClaimControlNumber = new REF_OtherPayerClaimControlNumber
                //    {
                //        ReferenceIdentificationQualifier_01 = "F8",
                //        ReferenceIdentificationREF_02 = data.PayerClaimControlNumber.TrimAndEmptyIfNullOrWhitespace() // from a previous 835 payment
                //    };

                //}

                if (allRef != null)
                {
                    loop2300.AllREF = allRef;
                }

                var diagnosisCodesInOrder = GetDiagnosisCodesInOrder(data);

                if (diagnosisCodesInOrder.Any())
                {
                    var dCode = diagnosisCodesInOrder.First();
                    loop2300.AllHI = new All_HI_837P
                    {
                        HI_HealthCareDiagnosisCode = new HI_DependentHealthCareDiagnosisCode_2
                        {
                            HealthCareCodeInformation_01 =
                                new C022_HealthCareCodeInformation_8
                                {
                                    // CodeListQualifierCode_01 = dCode.DiagnosisType == DiagnosisTypes.ICD09 ? "BK" : "ABK",       // discussed with Robert and commented.
                                    CodeListQualifierCode_01 = "ABK",
                                    IndustryCode_02 = dCode.DiagnosisCode.TrimAndEmptyIfNullOrWhitespace()
                                },
                        }
                    };

                    foreach (var diagnosisCode in diagnosisCodesInOrder.Skip(1))
                    {
                        var diagCodeEntry = new C022_HealthCareCodeInformation_4
                        {
                            CodeListQualifierCode_01 = "ABF",
                            IndustryCode_02 = diagnosisCode.DiagnosisCode.TrimAndEmptyIfNullOrWhitespace()
                        };
                        switch (diagnosisCode.Order)
                        {
                            // there is not a more elegant way to do this short of using reflection which would reduce clarity
                            case 2: loop2300.AllHI.HI_HealthCareDiagnosisCode.HealthCareCodeInformation_02 = diagCodeEntry; break;
                            case 3: loop2300.AllHI.HI_HealthCareDiagnosisCode.HealthCareCodeInformation_03 = diagCodeEntry; break;
                            case 4: loop2300.AllHI.HI_HealthCareDiagnosisCode.HealthCareCodeInformation_04 = diagCodeEntry; break;
                            case 5: loop2300.AllHI.HI_HealthCareDiagnosisCode.HealthCareCodeInformation_05 = diagCodeEntry; break;
                            case 6: loop2300.AllHI.HI_HealthCareDiagnosisCode.HealthCareCodeInformation_06 = diagCodeEntry; break;
                            case 7: loop2300.AllHI.HI_HealthCareDiagnosisCode.HealthCareCodeInformation_07 = diagCodeEntry; break;
                            case 8: loop2300.AllHI.HI_HealthCareDiagnosisCode.HealthCareCodeInformation_08 = diagCodeEntry; break;
                            case 9: loop2300.AllHI.HI_HealthCareDiagnosisCode.HealthCareCodeInformation_09 = diagCodeEntry; break;
                            case 10: loop2300.AllHI.HI_HealthCareDiagnosisCode.HealthCareCodeInformation_10 = diagCodeEntry; break;
                            case 11: loop2300.AllHI.HI_HealthCareDiagnosisCode.HealthCareCodeInformation_11 = diagCodeEntry; break;
                            case 12: loop2300.AllHI.HI_HealthCareDiagnosisCode.HealthCareCodeInformation_12 = diagCodeEntry; break;
                        }
                        ;
                    }
                }

                //support for 2310 A, B, and C

                if ((!string.IsNullOrEmpty(data.ReferringProviderLastName) || !string.IsNullOrEmpty(data.ReferringProviderFirstName)) &&
                    !string.IsNullOrEmpty(data.ReferringProviderNpiNumber))
                {
                    loop2300.Loop2310A = new List<Loop_2310A_837P>()
                   {
                       new Loop_2310A_837P()
                       {
                           NM1_ReferringProviderName = new NM1_ReferringProviderName()
                           {
                               EntityIdentifierCode_01 = "DN",
                               EntityTypeQualifier_02 = "1",
                               ResponseContactLastorOrganizationName_03 = data.ReferringProviderLastName.TrimAndEmptyIfNullOrWhitespace(),
                               ResponseContactFirstName_04 = data.ReferringProviderFirstName.TrimAndEmptyIfNullOrWhitespace(),
                               IdentificationCodeQualifier_08 = "XX",
                               ResponseContactIdentifier_09 = data.ReferringProviderNpiNumber.TrimAndEmptyIfNullOrWhitespace(),
                           }
                       }
                   };
                }

                //AS discussed with Robert, changing below logic based on firstname to avoid 277 issue.
                //var renderingProviderType = data.FunderBillingProviderOption.HasValue &&
                //                                data.FunderBillingProviderOption.Value == BillingProviderOptionType.Individual ? "1" : "2";

                var renderingProviderType = !string.IsNullOrEmpty(data.ResolvedRenderingProviderFirstName) ? "1" : "2";

                var renderingProviderIds = data.Claim.ClaimChargeEntries.Select(x => x.RenderingProviderId).ToList();
                var filteredProviders = renderingProviders.data
                    .Where(p => renderingProviderIds.Contains(p.memberId))
                    .ToList();


                bool singleRenderingProvider = renderingProviderIds.Distinct().Count() == 1;
                if (singleRenderingProvider)
                {

                    var provider = filteredProviders.FirstOrDefault();

                    var taxonomy = provider?.identifiers?
                        .FirstOrDefault(i =>
                            string.Equals(i.identifierType, "TaxonomyCode", StringComparison.OrdinalIgnoreCase))
                        ?.value
                        ?.TrimAndEmptyIfNullOrWhitespace() ?? null;

                    loop2300.Loop2310B = new List<Loop_2310B_837P>()
                    {
                        new Loop_2310B_837P()
                        {
                            NM1_RenderingProviderName = new NM1_RenderingProviderName()
                            {
                                EntityIdentifierCode_01                  = "82",
                                EntityTypeQualifier_02                   = renderingProviderType.TrimAndEmptyIfNullOrWhitespace(),
                                ResponseContactLastorOrganizationName_03 = data.ResolvedRenderingProviderName.TrimAndEmptyIfNullOrWhitespace(),
                                ResponseContactFirstName_04              = data.ResolvedRenderingProviderFirstName.TrimAndEmptyIfNullOrWhitespace(),
                                ResponseContactMiddleName_05             = data.ResolvedRenderingProviderMiddleName.TrimAndEmptyIfNullOrWhitespace(),
                                IdentificationCodeQualifier_08           = "XX",
                                ResponseContactIdentifier_09             = data.ResolvedRenderingProviderNpi.TrimAndEmptyIfNullOrWhitespace(),
                            },
                            PRV_RenderingProviderSpecialtyInformation = 
                            billingFunderSettings.IncludeTaxonomyCode && !string.IsNullOrEmpty(taxonomy) ?
                                new PRV_RenderingProviderSpecialtyInformation
                                {
                                    ProviderCode_01 = "PE",        // Rendering Provider
                                    ReferenceIdentificationQualifier_02 = "PXC",  // Taxonomy Code
                                    ProviderTaxonomyCode_03 = taxonomy
                                } : null
                        }
                    };
                }

                // BUG 209284 : Changes to handle- if service location name and npi number is null or empty then dont add that loop into edi file.

                loop2300.Loop2310C = new List<Rethink_Loop_2310C_837P>();

                //Prod issue fix: Bug-220008
                //if (!string.IsNullOrWhiteSpace(data.ServiceLocationName) && !string.IsNullOrWhiteSpace(data.ServiceLocationNpiNumber))
                //{

                if (!string.IsNullOrWhiteSpace(data.ServiceLocationName) &&
                    !string.IsNullOrWhiteSpace(data.ServiceLocationNpiNumber) &&
                    !string.Equals(data.ResolvedBillingProviderNpi.TrimAndEmptyIfNullOrWhitespace(),
                    data.ServiceLocationNpiNumber.TrimAndEmptyIfNullOrWhitespace(), StringComparison.Ordinal))
                {
                    loop2300.Loop2310C.Add(new Rethink_Loop_2310C_837P()
                    {
                        NM1_ServiceFacilityLocationName = new NM1_ServiceFacilityLocation()
                        {
                            EntityIdentifierCode_01 = "77",
                            EntityTypeQualifier_02 = "2",
                            ResponseContactLastorOrganizationName_03 = data.ServiceLocationName.TrimAndEmptyIfNullOrWhitespace(),
                            IdentificationCodeQualifier_08 = "XX",
                            ResponseContactIdentifier_09 = data.ServiceLocationNpiNumber.TrimAndEmptyIfNullOrWhitespace()
                        },
                        N3_ServiceFacilityLocationAddress = new N3_AdditionalPatientInformationContactAddress()
                        {
                            ResponseContactAddressLine_01 = !String.IsNullOrEmpty(data.ServiceLocationAddress1) ? data.ServiceLocationAddress1.Trim().Substring(0, Math.Min(data.ServiceLocationAddress1.Trim().Length, 55)) : "",
                            ResponseContactAddressLine_02 = !String.IsNullOrEmpty(data.ServiceLocationAddress2) ? data.ServiceLocationAddress2.Trim().Substring(0, Math.Min(data.ServiceLocationAddress2.Trim().Length, 55)) : ""
                        },
                        N4_ServiceFacilityLocationCity_State_ZIPCode = new N4_AdditionalPatientInformationContactCity()
                        {
                            AdditionalPatientInformationContactCityName_01 = data.ServiceLocationCity.TrimAndEmptyIfNullOrWhitespace(),
                            AdditionalPatientInformationContactStateCode_02 = data.ServiceLocationState.TrimAndEmptyIfNullOrWhitespace(),
                            AdditionalPatientInformationContactPostalZoneorZIPCode_03 = data.ServiceLocationZip.TrimAndEmptyIfNullOrWhitespace(),
                            //CountryCode_04 = data.ServiceLocationCountry
                        },
                        REF_AdditionalLocationInformation = new List<REF_AdditionalLocationIdentification>()
                           {
                               !string.IsNullOrEmpty(data.LocationBillingProviderCommercialNumber) ?
                               new REF_AdditionalLocationIdentification()
                               {
                                   ReferenceIdentificationQualifier_01 = "G2",
                                   ReferenceIdentificationREF_02 = data.LocationBillingProviderCommercialNumber.TrimAndEmptyIfNullOrWhitespace()
                               } : null,
                               !string.IsNullOrEmpty(data.LocationBillingProviderStateLicenseNumber) ?
                               new REF_AdditionalLocationIdentification()
                               {
                                   ReferenceIdentificationQualifier_01 = "0B",
                                   ReferenceIdentificationREF_02 = data.LocationBillingProviderStateLicenseNumber.TrimAndEmptyIfNullOrWhitespace()
                               } : null,
                               !string.IsNullOrEmpty(data.LocationBillingProviderLocationNumber) ?
                               new REF_AdditionalLocationIdentification()
                               {
                                   ReferenceIdentificationQualifier_01 = "LU",
                                   ReferenceIdentificationREF_02 = data.LocationBillingProviderLocationNumber.TrimAndEmptyIfNullOrWhitespace()
                               } : null,
                           }
                    });
                }

                // End - Changes to handle- if service location name and npi number is null or empty then dont add that loop into edi file.

                foreach (var funderSequence in data.ClaimSubmissionFunderSequences)
                {
                    // The current funder is accounted for above so we skip it here
                    if (funderSequence.Id == currentFunder.Id)
                    {
                        continue;
                    }

                    if (claimModelDto.isSecondary)
                    //&& data.PriorFunderLatestClaimPayment != null &&
                    //data.PriorFunderLatestClaimPayment.TotalPayment > 0           --As per discussion with Robert we will send all the data in case of service line level adjustment as well
                    {
                        Loop_2320_837P loop2320 = new Loop_2320_837P();
                        string relationship;
                        switch (funderSequence.RelationshipToSubscriber ?? 2)
                        {
                            case 1:
                                relationship = "18"; // Self
                                break;
                            case 2:
                            default:
                                relationship = "19"; // Child
                                break;
                        }

                        loop2320.SBR_OtherSubscriberInformation = new SBR_OtherSubscriberInformation()
                        {
                            PayerResponsibilitySequenceNumberCode_01 = funderSequence.FunderResponsibilitySequence.TrimAndEmptyIfNullOrWhitespace(),
                            IndividualRelationshipCode_02 = relationship.TrimAndEmptyIfNullOrWhitespace(),
                            InsuredGrouporPolicyNumber_03 = funderSequence.InsuranceGroupNumber.TrimAndNullIfWhitespace() ?? funderSequence.InsurancePolicyNumber.TrimAndEmptyIfNullOrWhitespace(),
                            OtherInsuredGroupName_04 = funderSequence.InsuranceGroupName,
                            //TODO: InsuranceTypeCode_05             = ???,// Required when the payer identified in Loop ID-2330B for this iteration of Loop ID-2320 is Medicare and Medicare is not the primary payer (Loop ID-2320 SBR01 is not P)
                            ClaimFilingIndicatorCode_09 = funderSequence.InsuranceCoverageType.TrimAndEmptyIfNullOrWhitespace(),
                        };


                        //CAS

                        if (data.PriorFunderLatestClaimPayment?.PaymentClaimAdjustments != null &&
                            //claimModelDto.AdjustmentLevel == AdjustmentLevel.Claim &&
                            data.PriorFunderLatestClaimPayment.PaymentClaimAdjustments.Any())
                        {
                            loop2320.CAS_ClaimLevelAdjustments = new List<CAS_ClaimLevelAdjustments>();
                            var casGroups = data.PriorFunderLatestClaimPayment.PaymentClaimAdjustments.GroupBy(cas => cas.AdjustmentGroupCode);
                            foreach (var casGroup in casGroups)
                            {
                                var groupCode = casGroup.Key;
                                var casEntries = casGroup.ToList();
                                // Note: looking up the entry for each of the trio is inefficient, but its make the process very clear
                                // and obvious here. In reality the performance loss here is meaningless (microseconds).
                                var adjustments = new CAS_ClaimLevelAdjustments()
                                {
                                    ClaimAdjustmentGroupCode_01 = groupCode,
                                    AdjustmentReasonCode_02 = GetCASEntry<PaymentClaimAdjustmentEntity>(casEntries, 1)?.AdjustmentReasonCode,
                                    AdjustmentAmount_03 = GetCASEntry(casEntries, 1)?.AdjustmentAmount?.ToEdiString(),
                                    AdjustmentQuantity_04 = GetCASEntry(casEntries, 1)?.AdjustmentQuantity?.ToEdiString(),

                                    AdjustmentReasonCode_05 = GetCASEntry(casEntries, 2)?.AdjustmentReasonCode,
                                    AdjustmentAmount_06 = GetCASEntry(casEntries, 2)?.AdjustmentAmount?.ToEdiString(),
                                    AdjustmentQuantity_07 = GetCASEntry(casEntries, 2)?.AdjustmentQuantity?.ToEdiString(),

                                    AdjustmentReasonCode_08 = GetCASEntry(casEntries, 3)?.AdjustmentReasonCode,
                                    AdjustmentAmount_09 = GetCASEntry(casEntries, 3)?.AdjustmentAmount?.ToEdiString(),
                                    AdjustmentQuantity_10 = GetCASEntry(casEntries, 3)?.AdjustmentQuantity?.ToEdiString(),

                                    AdjustmentReasonCode_11 = GetCASEntry(casEntries, 4)?.AdjustmentReasonCode,
                                    AdjustmentAmount_12 = GetCASEntry(casEntries, 4)?.AdjustmentAmount?.ToEdiString(),
                                    AdjustmentQuantity_13 = GetCASEntry(casEntries, 4)?.AdjustmentQuantity?.ToEdiString(),

                                    AdjustmentReasonCode_14 = GetCASEntry(casEntries, 5)?.AdjustmentReasonCode,
                                    AdjustmentAmount_15 = GetCASEntry(casEntries, 5)?.AdjustmentAmount?.ToEdiString(),
                                    AdjustmentQuantity_16 = GetCASEntry(casEntries, 5)?.AdjustmentQuantity?.ToEdiString(),

                                    AdjustmentReasonCode_17 = GetCASEntry(casEntries, 6)?.AdjustmentReasonCode,
                                    AdjustmentAmount_18 = GetCASEntry(casEntries, 6)?.AdjustmentAmount?.ToEdiString(),
                                    AdjustmentQuantity_19 = GetCASEntry(casEntries, 6)?.AdjustmentQuantity?.ToEdiString(),
                                };
                                loop2320.CAS_ClaimLevelAdjustments.Add(adjustments);
                            }
                        }


                        loop2320.AllAMT = new All_AMT_837P_2()
                        {
                            AMT_CoordinationofBenefits_COB_PayerPaidAmount = new AMT_CoordinationofBenefits()
                            {
                                AmountQualifierCode_01 = "D",
                                TotalClaimChargeAmount_02 = data.PriorFunderLatestClaimPayment?.TotalPayment?.ToEdiString() ?? "0.00",
                            },
                            AMT_RemainingPatientLiability = new AMT_RemainingPatientLiability()
                            {
                                AmountQualifierCode_01 = "EAF",
                                TotalClaimChargeAmount_02 = data.PriorFunderLatestClaimPayment?.PatientRespAmount?.ToEdiString() ?? "0.00",
                            }
                        };

                        loop2320.OI_OtherInsuranceCoverageInformation = new OI_OtherInsuranceCoverageInformation_2()
                        {
                            //Bug : 218400 - As per discussion with Robert we decided to pass hardcode value as Y everytime in primary and secondary submissions.
                            //BenefitsAssignmentCertificationIndicator_03 = data.AuthorizedPaymentConfirmationType.TrimAndEmptyIfNullOrWhitespace(),
                            //ReleaseofInformationCode_06 = data.ReleaseOfInformationConfirmationType.TrimAndEmptyIfNullOrWhitespace(),

                            BenefitsAssignmentCertificationIndicator_03 = "Y",
                            ReleaseofInformationCode_06 = "Y",

                        };
                        loop2300.Loop2320 = new List<Loop_2320_837P> { loop2320 };

                        All_REF_837P allRef2330B = null;

                        if (!string.IsNullOrWhiteSpace(data.PayerClaimControlNumber))
                        {
                            allRef2330B ??= new All_REF_837P();
                            allRef2330B.REF_OtherPayerClaimControlNumber = new REF_OtherPayerClaimControlNumber
                            {
                                ReferenceIdentificationQualifier_01 = "F8",
                                ReferenceIdentificationREF_02 = data.PayerClaimControlNumber.TrimAndEmptyIfNullOrWhitespace() // from a previous 835 payment
                            };

                        }

                        if (!string.IsNullOrEmpty(data.AuthorizationNumber) && billingFunderSettings.AuthorizationStatus != (int)AuthorizationStatus.NotNeeded)
                        {
                            allRef2330B ??= new All_REF_837P();
                            allRef2330B.REF_OtherPayerPriorAuthorizationNumber = new REF_OtherPayerPriorAuthorizationNumber
                            {
                                ReferenceIdentificationQualifier_01 = "G1",
                                ReferenceIdentificationREF_02 = data.AuthorizationNumber.Trim().Substring(0, Math.Min(data.AuthorizationNumber.Trim().Length, 50))
                            };
                        }

                        loop2320.AllNM1 = new All_NM1_837P_4()
                        {
                            Loop2330A = new Loop_2330A_837P()
                            {
                                NM1_OtherSubscriberName = new NM1_OtherSubscriberName()
                                {
                                    EntityIdentifierCode_01 = "IL", // Insured or Subscriber
                                    EntityTypeQualifier_02 = "1",  // Person
                                    ResponseContactLastorOrganizationName_03 = funderSequence.SubscriberLastName.TrimAndEmptyIfNullOrWhitespace(),
                                    ResponseContactFirstName_04 = funderSequence.SubscriberFirstName.TrimAndEmptyIfNullOrWhitespace(),
                                    ResponseContactMiddleName_05 = funderSequence.SubscriberMiddleName.TrimAndEmptyIfNullOrWhitespace(),
                                    IdentificationCodeQualifier_08 = "MI", // Member Identification Number
                                    ResponseContactIdentifier_09 = funderSequence.InsurancePolicyNumber.TrimAndEmptyIfNullOrWhitespace()
                                }
                            },

                        };

                        var Loop2330B = new Loop_2330B_837P()
                        {
                            NM1_OtherPayerName = new NM1_OtherPayerName()
                            {
                                EntityIdentifierCode_01 = "PR",
                                EntityTypeQualifier_02 = "2",
                                ResponseContactLastorOrganizationName_03 = funderSequence.FunderName.TrimAndEmptyIfNullOrWhitespace(),
                                IdentificationCodeQualifier_08 = "PI",
                                ResponseContactIdentifier_09 = funderSequence.FunderVendorId.TrimAndEmptyIfNullOrWhitespace()
                            }
                        };


                        if (allRef2330B != null)
                        {
                            Loop2330B.AllREF = allRef2330B;
                        }

                        loop2320.AllNM1.Loop2330B = Loop2330B;
                    }

                    // We can skip loop 2330C - we don't have the data and it does not apply
                }
                // End loop 2300

                //  Repeating 2400 Loops
                loop2300.Loop2400 = new List<Loop_2400_837P>();

                int ServiceLineIndex = 1;
                foreach (var serviceLine in data.ClaimSubmissionServiceLines)
                {
                    var loop2400 = new Loop_2400_837P
                    {
                        LX_ServiceLineNumber = new LX_HeaderNumber
                        {
                            AssignedNumber_01 = $"{ServiceLineIndex}"
                        },
                        SV1_ProfessionalService = new SV1_ProfessionalService
                        {
                            CompositeMedicalProcedureIdentifier_01 =
                                new C003_CompositeMedicalProcedureIdentifier_12
                                {
                                    ProductorServiceIDQualifier_01 = "HC",
                                    ProcedureCode_02 = serviceLine.BillingCode.TrimAndEmptyIfNullOrWhitespace(),
                                    ProcedureModifier_03 = serviceLine.Modifier1.TrimAndEmptyIfNullOrWhitespace(),
                                    ProcedureModifier_04 = serviceLine.Modifier2.TrimAndEmptyIfNullOrWhitespace(),
                                    ProcedureModifier_05 = serviceLine.Modifier3.TrimAndEmptyIfNullOrWhitespace(),
                                    ProcedureModifier_06 = serviceLine.Modifier4.TrimAndEmptyIfNullOrWhitespace(),
                                    Description_07 = serviceLine.BillingCodeDescription.TrimAndEmptyIfNullOrWhitespace(),
                                },
                            LineItemChargeAmount_02 = serviceLine.Charges?.ToEdiString() ?? "0.00",
                            UnitorBasisforMeasurementCode_03 = "UN",
                            ServiceUnitCount_04 = serviceLine.Units.HasValue
                                                    ? (serviceLine.Units.Value % 1 == 0
                                                    ? $"{(int)serviceLine.Units.Value}"
                                                    : $"{serviceLine.Units.Value:0.00}")
                                                    : "0.00",
                            //PlaceofServiceCode_05 = data.PlaceOfServiceCode,      //commented as per EDI mapping sheet
                            CompositeDiagnosisCodePointer_07 = new C004_CompositeDiagnosisCodePointer
                            {
                                DiagnosisCodePointer_01 = $"{serviceLine.DiagnosisCodeOrder ?? 1}"
                            }
                        },
                        AllDTP = serviceLine.DateOfService.HasValue ? new All_DTP_837P
                        {
                            DTP_Date_ServiceDate = new DTP_ClaimLevelServiceDate
                            {
                                DateTimeQualifier_01 = "472",
                                DateTimePeriodFormatQualifier_02 = "D8",
                                DateTimePeriod_03 = serviceLine.DateOfService.Value.ToEdiString()
                            }
                        } : null
                    };

                    if (data.FrequencyType == ClaimFrequencyType.Replacement || data.FrequencyType == ClaimFrequencyType.Void)
                    {
                        loop2400.AllNTE = !string.IsNullOrWhiteSpace(serviceLine.ClaimChargeEntry.NoteText) ? new All_NTE_837P()
                        {
                            NTE_LineNote = new NTE_LineNote()
                            {
                                // ADD	= Additional Information
                                // DCP	= Goals, Rehabilitation Potential, or Discharge Plans
                                NoteReferenceCode_01 = "ADD",
                                BillingNoteText_02 = serviceLine.ClaimChargeEntry.NoteText?.Trim().Substring(0, Math.Min(serviceLine.ClaimChargeEntry.NoteText.Trim().Length, 80)),
                            }
                        } : null;
                    }

                    if (!singleRenderingProvider && serviceLine != null)
                    {
                        var provider = filteredProviders
                            .FirstOrDefault(p => p.memberId == serviceLine.ClaimChargeEntry.RenderingProviderId);

                        var taxonomy = provider?.identifiers?
                            .FirstOrDefault(i =>
                                string.Equals(i.identifierType, "taxonomyCode", StringComparison.OrdinalIgnoreCase))
                            ?.value
                            .TrimAndEmptyIfNullOrWhitespace() ?? null;

                        loop2400.Loop2420A = new List<Loop_2420A_837P>
                        {
                            new Loop_2420A_837P
                            {
                                NM1_RenderingProviderName = new NM1_RenderingProviderName
                                {
                                    EntityIdentifierCode_01 = "82",
                                    EntityTypeQualifier_02 = renderingProviderType.TrimAndEmptyIfNullOrWhitespace(),
                                    ResponseContactLastorOrganizationName_03 = provider?.name.lastName.TrimAndEmptyIfNullOrWhitespace() ?? "",
                                    ResponseContactFirstName_04 = provider?.name.firstName.TrimAndEmptyIfNullOrWhitespace() ?? "",
                                    ResponseContactMiddleName_05 = provider?.name.middleName.TrimAndEmptyIfNullOrWhitespace() ?? "",
                                    IdentificationCodeQualifier_08 = "XX",
                                    ResponseContactIdentifier_09 = provider?.identifiers
                                        .FirstOrDefault(i => string.Equals(i.identifierType, "NPINumber", StringComparison.OrdinalIgnoreCase))
                                        ?.value
                                        .TrimAndEmptyIfNullOrWhitespace() ?? ""
                                },
                                PRV_RenderingProviderSpecialtyInformation =
                                billingFunderSettings.IncludeTaxonomyCode && !string.IsNullOrEmpty(taxonomy) ?
                                    new PRV_RenderingProviderSpecialtyInformation
                                    {
                                        ProviderCode_01 = "PE",        // Rendering Provider
                                        ReferenceIdentificationQualifier_02 = "PXC",  // Taxonomy Code
                                        ProviderTaxonomyCode_03 = taxonomy
                                    } : null
                            }
                        };
                    }

                    loop2300.Loop2400.Add(loop2400);

                    if (claimModelDto.isSecondary && claimModelDto.AdjustmentLevel == AdjustmentLevel.ServiceLine && data.PriorFunderLatestClaimPayment != null)
                    {
                        var priorServiceLinePmt = data.PriorFunderLatestClaimPayment.PaymentClaimServiceLines.FirstOrDefault(sl => sl.DateOfService == serviceLine.DateOfService &&
                                                                                                                                   sl.ServiceCode.TrimAndEmptyIfNullOrWhitespace() == serviceLine.BillingCode.TrimAndEmptyIfNullOrWhitespace() &&
                                                                                                                                  (sl.ProcedureModifier1.TrimAndEmptyIfNullOrWhitespace() == serviceLine.Modifier1.TrimAndEmptyIfNullOrWhitespace() ||
                                                                                                                                   sl.ProcedureModifier2.TrimAndEmptyIfNullOrWhitespace() == serviceLine.Modifier2.TrimAndEmptyIfNullOrWhitespace() ||
                                                                                                                                   sl.ProcedureModifier3.TrimAndEmptyIfNullOrWhitespace() == serviceLine.Modifier3.TrimAndEmptyIfNullOrWhitespace() ||
                                                                                                                                   sl.ProcedureModifier4.TrimAndEmptyIfNullOrWhitespace() == serviceLine.Modifier4.TrimAndEmptyIfNullOrWhitespace()) &&
                                                                                                                                   sl.ClaimChargeEntryId == serviceLine.ClaimChargeEntryId);
                        if (priorServiceLinePmt != null)
                        {
                            var currentSequence = ResponsibilitySequenceHelper.GetEnumFromString<ResponsibilitySequenceType>(data.ResponsibilitySequence);
                            var priorSequences = Enum.GetValues(typeof(ResponsibilitySequenceType))
                                                   .Cast<ResponsibilitySequenceType>()
                                                   .Where(seq => seq < currentSequence)
                                                   .Select(seq => seq.GetEnumMemberValue())
                                                   .ToList();

                            var loop_2430_837P = new Loop_2430_837P()
                            {
                                SVD_LineAdjudicationInformation = new SVD_LineAdjudicationInformation_3()
                                {
                                    //OtherPayerPrimaryIdentifier_01 = data.PriorFunderLatestClaimPayment.Payment.FunderID,

                                    OtherPayerPrimaryIdentifier_01 = data.ClaimSubmissionFunderSequences
                                                                    .Where(p => priorSequences.Contains(p.FunderResponsibilitySequence))
                                                                    .Select(p => p.FunderVendorId)
                                                                    .FirstOrDefault(),

                                    ServiceLinePaidAmount_02 = priorServiceLinePmt.PaymentAmount?.ToEdiString() ?? "0.00",
                                    CompositeMedicalProcedureIdentifier_03 = new C003_CompositeMedicalProcedureIdentifier_12()
                                    {
                                        ProductorServiceIDQualifier_01 = "HC", //Health Care Financing Administration Common Procedural Coding System (HCPCS) Codes
                                        ProcedureCode_02 = priorServiceLinePmt.ServiceCode.TrimAndEmptyIfNullOrWhitespace(),
                                        ProcedureModifier_03 = priorServiceLinePmt.ProcedureModifier1.TrimAndEmptyIfNullOrWhitespace(),
                                        ProcedureModifier_04 = priorServiceLinePmt.ProcedureModifier2.TrimAndEmptyIfNullOrWhitespace(),
                                        ProcedureModifier_05 = priorServiceLinePmt.ProcedureModifier3.TrimAndEmptyIfNullOrWhitespace(),
                                        ProcedureModifier_06 = priorServiceLinePmt.ProcedureModifier4.TrimAndEmptyIfNullOrWhitespace(),
                                        Description_07 = priorServiceLinePmt.ProcedureDesc.TrimAndEmptyIfNullOrWhitespace()
                                    },

                                    //commented as we are getting error in 277 saying its not required.

                                    //ProductServiceID_04 = priorServiceLinePmt.ServiceCode.TrimAndEmptyIfNullOrWhitespace(),


                                    PaidServiceUnitCount_05 = decimal.TryParse(priorServiceLinePmt.ProcedureUnits, out var units) ? (units % 1 == 0 ? ((int)units).ToString() : units.ToString("0.00")) : "0.00",
                                },
                                DTP_LineCheckorRemittanceDate = new DTP_ClaimCheckOrRemittanceDate()
                                {
                                    DateTimeQualifier_01 = "573", // Date Claim Paid
                                    DateTimePeriodFormatQualifier_02 = "D8", //CCYYMMDD
                                    DateTimePeriod_03 = priorServiceLinePmt.DateOfService?.ToEdiString()
                                },

                                AMT_RemainingPatientLiability = null // not required if sending 2320.AMT
                            };

                            loop2400.Loop2430 = new List<Loop_2430_837P>()
                        {
                            loop_2430_837P
                        };

                            //CAS
                            if (priorServiceLinePmt.PaymentClaimServiceLineAdjustments != null &&
                                priorServiceLinePmt.PaymentClaimServiceLineAdjustments.Any())
                            {
                                loop_2430_837P.CAS_LineAdjustment = new List<CAS_ClaimLevelAdjustments>();
                                var casGroups = priorServiceLinePmt.PaymentClaimServiceLineAdjustments.GroupBy(cas => cas.AdjustmentGroupCode);
                                foreach (var casGroup in casGroups)
                                {
                                    var groupCode = casGroup.Key;
                                    var casEntries = casGroup.ToList();
                                    // Note: looking up the entry for each of the trio is inefficient, but its make the process very clear
                                    // and obvious here. In reality the performance loss here is meaningless (microseconds).
                                    var adjustments = new CAS_ClaimLevelAdjustments()
                                    {
                                        ClaimAdjustmentGroupCode_01 = groupCode,
                                        AdjustmentReasonCode_02 = GetCASEntry(casEntries, 1)?.AdjustmentReasonCode,
                                        AdjustmentAmount_03 = GetCASEntry(casEntries, 1)?.AdjustmentAmount?.ToEdiString(),
                                        AdjustmentQuantity_04 = GetCASEntry(casEntries, 1)?.AdjustmentQuantity?.ToEdiString(),

                                        AdjustmentReasonCode_05 = GetCASEntry(casEntries, 2)?.AdjustmentReasonCode,
                                        AdjustmentAmount_06 = GetCASEntry(casEntries, 2)?.AdjustmentAmount?.ToEdiString(),
                                        AdjustmentQuantity_07 = GetCASEntry(casEntries, 2)?.AdjustmentQuantity?.ToEdiString(),

                                        AdjustmentReasonCode_08 = GetCASEntry(casEntries, 3)?.AdjustmentReasonCode,
                                        AdjustmentAmount_09 = GetCASEntry(casEntries, 3)?.AdjustmentAmount?.ToEdiString(),
                                        AdjustmentQuantity_10 = GetCASEntry(casEntries, 3)?.AdjustmentQuantity?.ToEdiString(),

                                        AdjustmentReasonCode_11 = GetCASEntry(casEntries, 4)?.AdjustmentReasonCode,
                                        AdjustmentAmount_12 = GetCASEntry(casEntries, 4)?.AdjustmentAmount?.ToEdiString(),
                                        AdjustmentQuantity_13 = GetCASEntry(casEntries, 4)?.AdjustmentQuantity?.ToEdiString(),

                                        AdjustmentReasonCode_14 = GetCASEntry(casEntries, 5)?.AdjustmentReasonCode,
                                        AdjustmentAmount_15 = GetCASEntry(casEntries, 5)?.AdjustmentAmount?.ToEdiString(),
                                        AdjustmentQuantity_16 = GetCASEntry(casEntries, 5)?.AdjustmentQuantity?.ToEdiString(),

                                        AdjustmentReasonCode_17 = GetCASEntry(casEntries, 6)?.AdjustmentReasonCode,
                                        AdjustmentAmount_18 = GetCASEntry(casEntries, 6)?.AdjustmentAmount?.ToEdiString(),
                                        AdjustmentQuantity_19 = GetCASEntry(casEntries, 6)?.AdjustmentQuantity?.ToEdiString(),
                                    };
                                    loop_2430_837P.CAS_LineAdjustment.Add(adjustments);
                                }
                            }

                        }
                    }


                    //  End 2400 Loop SERVICE LINE 
                    loop2300.Loop2400.Add(loop2400);

                    ServiceLineIndex++;
                }

                //  End 2300 Loop CLAIM
                loop2000C.Loop2300.Add(loop2300);

                //  End 2000C Loop PATIENT Hierarchy
                loop2000B.Loop2000C.Add(loop2000C);

                //  End 2000B Loop SUBSCRIBER Hierarchy
                loop2000A.Loop2000B.Add(loop2000B);

                //  End 2000A Loop BILLING PROVIDER Hierarchy
                result.Loop2000A.Add(loop2000A);

                return result;
            }
            catch
            {
                throw;
            }
        }

        private T GetCASEntry<T>(List<T> casEntries, int adjustmentIndex)
            where T : IEntity
        {

            if (adjustmentIndex > casEntries.Count)
            {
                return default(T); // null
            }

            return casEntries[adjustmentIndex - 1];
        }

        private ClaimSubmissionFunderSequenceEntity GetResponsibilitySequence(ICollection<ClaimSubmissionFunderSequenceEntity> submissionFunderSequences, string responsibilitySequence)
        {
            return submissionFunderSequences.FirstOrDefault(fs => fs.FunderResponsibilitySequence == responsibilitySequence);
        }

        private List<OrderedDxCode> GetDiagnosisCodesInOrder(ClaimSubmissionEntity data)
        {

            var codes = data.ClaimSubmissionServiceLines.Select(sl => new OrderedDxCode(sl.DiagnosisCode.ToEdiDiagnosisString(),
                                                                                              sl.DiagnosisCodeOrder ?? 1,
                                                                                              sl.DiagnosisCodeType ?? DiagnosisTypes.ICD10))
                                                        .GroupBy(dco => dco.DiagnosisCode)
                                                        .Select(dcog => dcog.First())
                                                        .OrderBy(dco => dco.Order)
                                                        .ToList();
            return codes;


        }

        public async Task<string> Generate270Edi(Eligibility270DTO eligibility270Dto)
        {
            eligibility270Dto.PayerId =eligibility270Dto.PayerId;
            var controlNumber = eligibility270Dto.PayerId.ToString().PadLeft(9, '0');

            var ts270 = await Build270Request(
                                                eligibility270Dto,
                                                controlNumber,
                                                _repositorySettings.Professional270Version
                                            );

            var output = ToEdi270(ts270, controlNumber);
            return output;
        }

        public async Task<TS270> Build270Request(Eligibility270DTO data, string stControl, string gsVersion)
        {
            var requestDate = (data?.EffectiveDate != null && data.EffectiveDate != default(DateTime)) ? data.EffectiveDate : DateTime.UtcNow;

            var ts = new TS270();
            // ST segment
            ts.ST = new ST
            {
                TransactionSetIdentifierCode_01 = "270",
                TransactionSetControlNumber_02 = stControl,
                ImplementationConventionPreference_03 = gsVersion
            };

            ts.BHT_BeginningOfHierarchicalTransaction = new BHT_BeginningOfHierarchicalTransaction
            {
                HierarchicalStructureCode_01 = "0022",
                TransactionSetPurposeCode_02 = "13",
                SubmitterTransactionIdentifier_03 =data.PayerId.ToString(),
                TransactionSetCreationDate_04 = requestDate.ToString("yyyyMMdd"),
                TransactionSetCreationTime_05 = requestDate.ToString("HHmm")
            };

            // Build 2000A loop (Information Source = Payer)
            var loop2000A = new Loop_2000A_270
            {
                HL_InformationSourceLevel = new HL_BillingProviderHierarchicalLevel
                {
                    HierarchicalIDNumber_01 = "1",
                    HierarchicalLevelCode_03 = "20",
                    HierarchicalChildCode_04 = "1"
                },
                Loop2100A = new Loop_2100A_270
                {
                    NM1_InformationSourceName = new NM1_InformationSourceName
                    {
                        EntityIdentifierCode_01 = "PR",
                        EntityTypeQualifier_02 = "2",
                        ResponseContactLastorOrganizationName_03 = data.ClearingHousePayerName.Split('(')[0].Trim(),
                        IdentificationCodeQualifier_08 = "PI",
                        ResponseContactIdentifier_09 =Convert.ToString(data.PayerId)
                    }
                }
            };

            // Build 2000B loop (Information Receiver = Provider)
            Loop_2000B_270? loop2000B = null;
            var providerNameSource = !string.IsNullOrEmpty(data.ChildProfileRenderingProviderName)
                                                                ? data.ChildProfileRenderingProviderName
                                                                : data.ChildProfileReferringProviderName;

            var providerName = SplitPersonName(providerNameSource);

            loop2000B = new Loop_2000B_270
            {
                HL_InformationReceiverLevel = new HL_InformationReceiverLevel
                {
                    HierarchicalIDNumber_01 = "2",
                    HierarchicalParentIDNumber_02 = "1",
                    HierarchicalLevelCode_03 = "21",
                    HierarchicalChildCode_04 = "1"
                },
                Loop2100B = new Loop_2100B_270
                {
                    NM1_InformationReceiverName = new NM1_InformationReceiverName
                    {
                        EntityIdentifierCode_01 = "1P",
                        EntityTypeQualifier_02 = "2",
                        ResponseContactFirstName_04 = string.IsNullOrEmpty(providerName.FirstName) ? string.Empty : providerName.FirstName,
                        ResponseContactMiddleName_05 =  string.IsNullOrEmpty(providerName.MiddleName) ? string.Empty : providerName.MiddleName,
                        ResponseContactLastorOrganizationName_03 = string.IsNullOrEmpty(providerName.LastName) ? string.Empty : providerName.LastName,
                        IdentificationCodeQualifier_08 = "XX",
                        ResponseContactIdentifier_09 = data.ChildProfileReferringProviderId
                    }
                }
            };

            var gender = (Gender)data.GenderId;

            // Build 2000C loop (Subscriber)
            var clientName = SplitPersonName(data.ClientName);

            var loop2000C = new Loop_2000C_270
            {
                HL_SubscriberLevel = new HL_SubscriberHierarchicalLevel
                {
                    HierarchicalIDNumber_01 = "3",
                    HierarchicalParentIDNumber_02 = "2",
                    HierarchicalLevelCode_03 = "22",
                    HierarchicalChildCode_04 = "0"
                },
                Loop2100C = new Loop_2100C_270
                {
                    NM1_SubscriberName = new NM1_SubscriberName
                    {
                        EntityIdentifierCode_01 = "IL",
                        EntityTypeQualifier_02 = "1",
                        ResponseContactFirstName_04 = string.IsNullOrEmpty(clientName.FirstName) ? string.Empty : clientName.FirstName,
                        ResponseContactMiddleName_05 = string.IsNullOrEmpty(clientName.MiddleName) ? string.Empty : clientName.MiddleName,
                        ResponseContactLastorOrganizationName_03 = string.IsNullOrEmpty(clientName.LastName) ? string.Empty : clientName.LastName,
                        IdentificationCodeQualifier_08 = "MI",
                        ResponseContactIdentifier_09 = data.SubscriberId
                    },
                    DMG_SubscriberDemographicInformation = new DMG_DependentDemographicInformation
                    {
                        DateTimePeriodFormatQualifier_01 = "D8",
                        DependentBirthDate_02 = data.DOB?.ToString("yyyyMMdd"),
                        DependentGenderCode_03 =gender.ToCode()
                    },
                    DTP_SubscriberDate = new List<DTP_SubscriberOrDependentDate>
                                            {
                                                new DTP_SubscriberOrDependentDate
                                                {
                                                    DateTimeQualifier_01 = "291",
                                                    DateTimePeriodFormatQualifier_02 = "D8",
                                                    DateTimePeriod_03 = requestDate.ToString("yyyyMMdd")
                                                }
                                            },
                    Loop2110C = new List<Loop_2110C_270>
                                            {
                                                new Loop_2110C_270
                                                {
                                                    EQ_SubscriberEligibilityorBenefitInquiry = new EQ_SubscriberEligibilityorBenefitInquiry
                                                    {
                                                        ServiceTypeCode_01 = new List<string> { "30" }
                                                    }
                                                }
                                            }
                }
            };
            loop2000B.Loop2000C = new List<Loop_2000C_270> { loop2000C };


            // assemble nested lists            
            loop2000A.Loop2000B = new List<Loop_2000B_270> { loop2000B };
            ts.Loop2000A = new List<Loop_2000A_270> { loop2000A };


            ts.SE = new SE
            {
                NumberofIncludedSegments_01 = "12",
                TransactionSetControlNumber_02 = stControl
            };

            return ts;
        }

        private void EnsureIsa16(object isaObj, char componentSep = ':')
        {
            if (isaObj == null) return;

            var isaType = isaObj.GetType();
            var candidates = new[]
            {
                "ComponentElementSeparator_16",
                "InterchangeControlElementSeparator_16",
                "ComponentElementSeparator",
                "ElementSeparator",
                "ISA16"
            };

            foreach (var name in candidates)
            {
                var p = isaType.GetProperty(name,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (p != null && p.CanWrite)
                {
                    if (p.PropertyType == typeof(char))
                        p.SetValue(isaObj, componentSep);
                    else
                        p.SetValue(isaObj, componentSep.ToString());

                    return;
                }
            }
        }

        private (string? FirstName, string? MiddleName, string? LastName) SplitPersonName(string? fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return (null, null, null);

            var parts = fullName
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
                return (null, null, parts[0]);

            if (parts.Length == 2)
                return (parts[0], null, parts[1]);

            return (
                parts[0],
                string.Join(" ", parts.Skip(1).Take(parts.Length - 2)),
                parts[^1]
            );
        }


        public string ToStediEdi(EdiMessage claim, string groupControlNumber)
        {
            var separators = Separators.X12;

            var settings = new X12WriterSettings()
            {
                Separators = separators,
                PreserveWhitespace = true
            };
            using (var stream = new MemoryStream())
            {
                using (var writer = new X12Writer(stream))
                {
                    writer.Write(SegmentBuilders.BuildIsaForStedi(
                                                            groupControlNumber,
                                                           _senderId,
                                                            _receiverId,
                                                            _testMode
                                                        ));

                    writer.Write(SegmentBuilders.BuildGs(
                                                            groupControlNumber,
                                                           _senderId,
                                                           _receiverId,
                                                            _repositorySettings.ProfessionalClaimsVersion
                                                        ));
                    writer.Write(claim, true, separators);
                }

                return stream.LoadToString();
            }
        }

    }
}
