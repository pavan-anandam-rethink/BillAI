using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Client;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Clients;
using BillingService.Domain.Services.Billing.EDI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Rethink.Services.Common.Dtos.Billing;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Helpers;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.BillingSettings;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using Rethink.Services.Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class ClaimManagerService : BaseService, IClaimManagerService
    {
        private readonly IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity> _billingClaimSubmissionFunderSequenceRepository;

        private readonly IRepository<BillingDbContext, ClaimSubmissionEntity> _billingClaimSubmissionRepository;
        private readonly IRepository<BillingDbContext, ClaimDiagnosisCodeEntity> _claimDiagnosisCodeRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimEntity> _billingPaymentClaimRepository;
        private readonly IRepository<BillingDbContext, ClaimEntity> _billingClaimRepository;
        private readonly IRepository<BillingDbContext, FunderSettingsEntity> _funderSettingRepo;

        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        private readonly IClaimValidationService _claimValidationService;
        private readonly IClientService _clientService;
        private readonly IServiceProvider _serviceProvider;

        private readonly string _submitterRethinkEmail;
        private readonly string _submitterRethinkPhone;
        private readonly string _submitterRethinkName;
        private readonly string _submitterRethinkId;
        private readonly string _billerRethinkId;
        private readonly string _customerId;
        private readonly bool _testMode;
        private readonly string _senderId;
        private readonly string _receiverId;
        private List<CountryModel> _countries;
        private List<StateModel> _states;
        private const int _claimSubmissionIdentifierLength = _claimIdentifierLength + 1;// [6+1+5+1+1]+1; 
        private const string _chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const int _claimIdentifierLength = 14;// 6+1+5+1+1; 
        private readonly IRethinkMasterDataMicroServices _rethinkMicroservicesRepository;

        public ClaimManagerService(
            IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity> billingClaimSubmissionFunderSequenceRepository,
            IRepository<BillingDbContext, ClaimAppointmentLinkEntity> billingClaimAppointmentLinkRepository,
            IRepository<BillingDbContext, ClaimSubmissionEntity> billingClaimSubmissionRepository,
            IRepository<BillingDbContext, ClaimDiagnosisCodeEntity> claimDiagnosisCodeRepository,
            IRepository<BillingDbContext, PaymentClaimEntity> billingPaymentClaimRepository,
            IRepository<BillingDbContext, ClaimEntity> billingClaimRepository,
            IRethinkMasterDataMicroServices rethinkServices,
            IClaimValidationService claimValidationService,
            IConfiguration configuration,
            IClientService clientService,
           IServiceProvider serviceProvider,
            IRethinkMasterDataMicroServices rethinkMicroservicesRepository,
            IRepository<BillingDbContext, FunderSettingsEntity> funderSettingRepo
            )
        {
            _billingClaimSubmissionFunderSequenceRepository = billingClaimSubmissionFunderSequenceRepository;
            _billingClaimSubmissionRepository = billingClaimSubmissionRepository;
            _billingPaymentClaimRepository = billingPaymentClaimRepository;
            _claimDiagnosisCodeRepository = claimDiagnosisCodeRepository;
            _billingClaimRepository = billingClaimRepository;
            _claimValidationService = claimValidationService;
            _rethinkServices = rethinkServices;
            _clientService = clientService;
            _serviceProvider = serviceProvider;
            _submitterRethinkEmail = configuration["EdiSettings:SubmitterRethinkEmail"] ?? "claimsprocessing@rethink.com";
            _submitterRethinkName = configuration["EdiSettings:SubmitterRethinkName"] ?? "Rethink Behavioral Health";
            _submitterRethinkPhone = configuration["EdiSettings:SubmitterRethinkPhone"] ?? String.Empty;
            _submitterRethinkId = configuration["EdiSettings:SubmitterRethinkId"];
            _billerRethinkId = configuration["EdiSettings:BillerRethinkId"];
            _testMode = "1".Equals(configuration["EdiSettings:TestMode"]);
            _customerId = configuration["EdiSettings:CustomerId"];
           
            _rethinkMicroservicesRepository = rethinkMicroservicesRepository;
            _funderSettingRepo = funderSettingRepo;
            _senderId= configuration["Clearinghouses:Stedi:SenderId"];
            _receiverId= configuration["Clearinghouses:Stedi:ReceiverId"];

        }

        /// <summary>
        /// To be called when initializing a new Claim in the system. Typically happens
        /// during the rendering process (e.g. when appointments are being completed and
        /// grouped into a single claim).
        /// </summary>
        public async Task<ClaimEntity> InitializeClaim(int memberId,
                                                       int accountInfoId,
                                                       int childProfileId,
                                                       int primaryFunderId,
                                                       DateTime startDate,
                                                       DateTime endDate)
        {
            var claimEntity = new ClaimEntity
            {
                AccountInfoId = accountInfoId,
                ChildProfileId = childProfileId,
                LastBilledFunderId = primaryFunderId,
                PrimaryFunderId = primaryFunderId,
                StartDate = startDate,
                EndDate = endDate,
                MemberId = memberId,
                FrequencyTypeId = ClaimFrequencyType.Original

            };
            MarkCreated(claimEntity, memberId);

            claimEntity.ClaimIdentifier = await GenerateClaimIdentifier(startDate, childProfileId);

            await _billingClaimRepository.AddAsync(claimEntity);
            await _billingClaimRepository.SaveChangesAsync();

            return claimEntity;
        }

        /// <summary>
        /// To be called when initializing a new Claim in the system. Typically happens
        /// during the rendering process (e.g. when appointments are being completed and
        /// grouped into a single claim).
        /// </summary>
        private async Task<ClaimSubmissionEntity> InitializeClaimSubmission(int submittingMemberId,
                                                                           ClaimEntity claim,
                                                                           ClaimFrequencyType frequencyType,
                                                                           ClaimSubmissionType submissionType,
                                                                           ClaimDocumentType documentType,
                                                                           ResponsibilitySequenceType responsibilitySequence,
                                                                           bool saveClaimSubmission)
        {
            var submission = new ClaimSubmissionEntity
            {
                SubmitDate = EstDateTime,
                ClaimId = claim.Id,
                ClaimFilePath = "N/A",
                FrequencyType = frequencyType,
                SubmissionType = submissionType,
                DocumentType = documentType,
                ResponsibilitySequence = responsibilitySequence.AsString(),
                SubmissionStatus = (documentType == ClaimDocumentType.HCFA1500Multi ||
                                          documentType == ClaimDocumentType.HCFA1500Single ||
                                          documentType == ClaimDocumentType.UB04Multi ||
                                          documentType == ClaimDocumentType.UB04Single) ? ClaimSubmissionStatus.FunderPending
                                                                                            : ClaimSubmissionStatus.ClearingHousePending,

            };
            MarkCreated(submission, submittingMemberId);

            submission.ClaimSubmissionIdentifier = await GenerateClaimSubmissionIdentifier(claim);

            if (saveClaimSubmission)
            {
                await _billingClaimSubmissionRepository.AddAsync(submission);
                await _billingClaimSubmissionRepository.SaveChangesAsync();
            }
            return submission;
        }

        public async Task<int> SubmitInitialClaim(int claimId,
                                                  int submittingMemberId,
                                                  ClaimDocumentType documentType,   // 837P, HCFA1500, etc.
                                                  ResponsibilitySequenceType responsibilitySequence)
        {

            var submissionType = ClaimSubmissionType.Original;
            var frequencyType = ClaimFrequencyType.Original;
            var latestClaimSubmission = await _claimValidationService.GetClaimSubmissionInformation(claimId);
            return await SubmitClaim(claimId, submittingMemberId, frequencyType,
                                     submissionType, documentType, responsibilitySequence,
                                     latestClaimSubmission);
        }

        public async Task<int> SubmitClaimRebill(int claimId,
                                                 int submittingMemberId,
                                                 ClaimFrequencyType frequencyType)    // Original, Corrected, Void
        {
            var latestClaimSubmission = await _claimValidationService.GetClaimSubmissionInformation(claimId);

            //if (latestClaimSubmission == null)
            //{
            //    throw new Exception("No prior claim submission to rebill");
            //}

            //Commented above code as disccuesed with Robert- Prior claim submission data not required for Rebill(if its not available we should not restrict to rebill claim.)

            var submissionType = ClaimSubmissionType.Rebill;
            var responsibilitySequence = Convert.ToString(ResponsibilitySequenceType.Primary);
            return await SubmitClaim(claimId, submittingMemberId, frequencyType,
                                     submissionType, ClaimDocumentType.Doc837P, ResponsibilitySequenceTypeHelper.FromString(responsibilitySequence),
                                     latestClaimSubmission);
        }

        public async Task<int> SubmitClaimTransfer(int claimId,
                                                   int submittingMemberId,
                                                   ClaimFrequencyType frequencyType,   // if this is an Original, Corrected, Void transfer
                                                   ClaimDocumentType documentType,
                                                   int? secondaryFunderId = null,
                                                   string controlNumber = null,
                                                   bool IsRebillPostSecondaryBilling = false)    // the new document type 837P, HCFA1500, etc.
        {
            var latestClaimSubmission = await _claimValidationService.GetClaimSubmissionInformation(claimId);
            if (latestClaimSubmission == null)
            {
                throw new Exception("No prior claim submission to transfer");
            }
            latestClaimSubmission.PayerClaimControlNumber = controlNumber;

            var submissionType = IsRebillPostSecondaryBilling == false ? ClaimSubmissionType.Transfer : ClaimSubmissionType.TransferRebill;
            return await SubmitClaim(claimId, submittingMemberId, frequencyType,
                                     submissionType, documentType, ResponsibilitySequenceType.Secondary, // currently as per the requirement we will support only secondary funder
                                     latestClaimSubmission, secondaryFunderId);
        }

        /// <summary>
        /// returns the ClaimSubmissionId related to the submission
        /// </summary>
        /// <returns></returns>
        private async Task<int> SubmitClaim(int claimId,
                                           int submittingMemberId,
                                           ClaimFrequencyType frequencyType,    // Original, Corrected, Void
                                           ClaimSubmissionType submissionType,  // Original, Transfer, Rebill, Transfer & Rebill 
                                           ClaimDocumentType documentType,      // 837P, HCFA1500, etc.
                                           ResponsibilitySequenceType responsibilitySequence, // Primary, Secondary, etc.
                                           ClaimSubmissionEntity priorClaimSubmission,         // used for Corrected, rebill
                                           int? secondaryFunderId = null
                                          )
        {
            var claim = await _claimValidationService.GetClaimInformation(claimId);

            ClaimSubmissionEntity claimSubmission = null;
            if (claim.FrequencyTypeId.HasValue)
                frequencyType = claim.FrequencyTypeId.Value;

            if (priorClaimSubmission == null)
            {
                claimSubmission = await InitializeClaimSubmission(submittingMemberId,
                                                                    claim,
                                                                    frequencyType,
                                                                    submissionType,
                                                                    documentType,
                                                                    responsibilitySequence,
                                                                    true);
            }
            else
            {
                claimSubmission = CloneClaimSubmissionFor(priorClaimSubmission,
                                                            frequencyType,
                                                            submissionType,
                                                            documentType,
                                                            responsibilitySequence);

                if (submissionType == ClaimSubmissionType.Rebill || submissionType == ClaimSubmissionType.Transfer)
                {
                    claimSubmission.ClaimSubmissionIdentifier = await GenerateClaimSubmissionIdentifier(claim);
                }

                MarkCreated(claimSubmission, submittingMemberId);

            }
            await _claimValidationService.PrepareClaimSubmission(claim,
                                            claimSubmission,
                                            priorClaimSubmission,
                                            submittingMemberId, secondaryFunderId);
            return claimSubmission.Id;
        }

        public async Task<string> GenerateEdi(ClearingHouseClaimModel claimModelDto)
        {
            var claimDetail = await _billingClaimRepository.Query().FirstOrDefaultAsync(x => x.Id == claimModelDto.claimId);
            claimModelDto.clearinghouseId = claimModelDto.clearinghouseId != 0 ? claimModelDto.clearinghouseId : await _rethinkServices.GetClearingHouseId(claimDetail.AccountInfoId);
            var claimSubmission = await GetFullClaimSubmission(claimModelDto.claimId, false, claimModelDto.isSecondary);
            if (claimSubmission == null)
            {
                throw new Exception("No claim submission data found, rethink microservices failed");
            }

            // Get the Billing Funder Setting for Claim Filing Indicator
            var funderId = claimSubmission.FunderId ?? claimSubmission.Claim?.PrimaryFunderId ?? claimSubmission.Claim?.SecondaryFunderId;
            var funderSetting = await _funderSettingRepo.Query()
                .Include(f => f.ClaimFilingIndicator)
                .FirstOrDefaultAsync(fs => fs.AccountInfoId == claimSubmission.Claim.AccountInfoId && fs.FunderId == funderId && fs.DateDeleted == null);

            var renderingProviders = await _rethinkServices.GetRenderingProvidersAsync(claimSubmission.Claim.AccountInfoId);

            var auth = await _rethinkServices.GetChildProfileAuthorizationByClientId(claimSubmission.Claim.AccountInfoId, claimDetail.ChildProfileId, claimSubmission.ChildProfileAuthorizationId);
            var isOverrideProvider = auth?.renderingProviderStaffId != null;

            if (isOverrideProvider)
            {
                claimSubmission.ResolvedRenderingProviderName = renderingProviders.FirstOrDefault(x => x.StaffMemberId == auth.renderingProviderStaffId.Value)?.Name;
            }
            else
            {
                claimSubmission.ResolvedRenderingProviderName = claimSubmission.ResolvedRenderingProviderName;

            }

            var billingFunderSettings = new BillingFunderSettings
            {
                ClaimFilingIndicator = funderSetting?.ClaimFilingIndicator?.Code ?? "ZZ",
                IncludeTaxonomyCode = funderSetting?.IncludeTaxonomyCode ?? false,
                AuthorizationStatus = claimSubmission.ChildProfileAuthorization != null ? claimSubmission.ChildProfileAuthorization.authorizationSubmissionTypeId : (int)AuthorizationStatus.Yes
            };

            var claimService = _serviceProvider.GetRequiredService<IClaimService>();

            var otherBillingProvider = claimDetail.BillTo == 0 ?
                await claimService.GetBillingProviderDetailsIdAsync(claimSubmission.ClaimId) : null;

            var result = await _rethinkServices.GetClearingHouseDetails();
            if (result != null)
            {
                var clearinghouse = result.Data.FirstOrDefault(C => C.id == claimModelDto.clearinghouseId);
                if (clearinghouse != null)
                {
                    var generator = new EdiGenerator(_testMode,
                                             _billerRethinkId,
                                             _submitterRethinkId,
                                             _submitterRethinkName,
                                             _submitterRethinkEmail,
                                             _submitterRethinkPhone,
                                             _customerId,
                                             clearinghouse.title,
                                             clearinghouse.taxId,
                                             _rethinkMicroservicesRepository, _senderId, _receiverId);
                    return await generator.GenerateEdi(claimSubmission, claimModelDto, billingFunderSettings, otherBillingProvider);
                }
                else
                    return string.Empty;
            }
            else
                return string.Empty;

        }

        public async Task<string> Generate270Edi(Eligibility270DTO eligibility270Dto)
        {
            // this method will get data if you want to run on localhost without integration from BH side hence commented the below line
            //var claimSubmission = await GetFullFunderDetails(eligibility270Dto.AccountInfoId, eligibility270Dto.Id, eligibility270Dto.ClientId, eligibility270Dto.ClientFunderId,eligibility270Dto.ChildProfileRenderingProviderId);

            if (eligibility270Dto == null)
            {
                return  "No funder data found";
            }
            var generator = new EdiGenerator(_testMode,_billerRethinkId,_submitterRethinkId,_submitterRethinkName,_submitterRethinkEmail,_submitterRethinkPhone,_customerId,"","",_rethinkMicroservicesRepository,"","");

            return await generator.Generate270Edi(eligibility270Dto);
        }

        public async Task UpdateClaimSubmissionStatusAsync(int id, int memberId, ClaimSubmissionStatus status, bool commitImmediately = true)
        {
            var submissionEntity = await _billingClaimSubmissionRepository.GetByIdAsync(id);

            if (submissionEntity == null) throw new NullReferenceException($"Claim submission with id: {id} not found!");

            submissionEntity.SubmissionStatus = status;
            MarkUpdated(submissionEntity, memberId);
            _billingClaimSubmissionRepository.Update(submissionEntity);

            if (commitImmediately) await _billingClaimSubmissionRepository.CommitAsync();
        }

        public async Task UpdateClaimStatusAsync(int id, ClaimStatus status, int memberId, bool commitImmediately = true, bool isBilledDateUpdate = false)
        {
            var claimEntity = await _billingClaimRepository.GetByIdAsync(id);

            if (claimEntity == null) throw new Exception($"Claim with id: {id} not found!");

            claimEntity.ClaimStatus = status;

            // Do not change the rebill in any condition except the electronic submission
            // Bug No. 234897
            //if (claimEntity.ClaimStatus == ClaimStatus.Billed || claimEntity.ClaimStatus == ClaimStatus.Rebill)
            //{
            //    claimEntity.billedDate = EstDateTime;
            //}
            //else { claimEntity.billedDate = null; }
            if (isBilledDateUpdate && claimEntity.ClaimStatus == ClaimStatus.Billed)
            {
                claimEntity.billedDate = EstDateTime;
            }

            MarkUpdated(claimEntity, memberId);
            _billingClaimRepository.Update(claimEntity);

            if (commitImmediately) await _billingClaimRepository.CommitAsync();
        }

        public async Task<ClaimHFCAModel> LookupHCFAClaimDetails(int memberId, int accountInfoId, int claimId)
        {
            var latestClaimSubmission = await GetLatestClaimSubmission(claimId);
            if (latestClaimSubmission == null)
            {
                return await CreateHCFAModel();

            }

            if (latestClaimSubmission.DocumentType != ClaimDocumentType.HCFA1500Single)
            {
                //throw new Exception($"Prior claim submission was not a HCFA/CMS 1500 claim. Previous submission was {latestClaimSubmission.DocumentType}");
            }


            return await CreateHCFAModel(latestClaimSubmission);
        }

        public async Task<ClaimHFCAModel> CreateHCFAClaim(int memberId,
                                                         int accountInfoId,
                                                         int claimId,
                                                         ClaimFrequencyType frequencyType,
                                                         ClaimSubmissionType submissionType,
                                                         ResponsibilitySequenceType responsibilitySequence)
        {

            var latestClaimSubmission = await GetLatestClaimSubmission(claimId);
            int claimSubmissionId = 0;

            if (submissionType == ClaimSubmissionType.Rebill)
            {
                claimSubmissionId = await SubmitClaimRebill(claimId,
                                                            memberId,
                                                            frequencyType);
            }
            else if (submissionType == ClaimSubmissionType.Transfer)
            {
                claimSubmissionId = await SubmitClaimTransfer(claimId,
                                                              memberId,
                                                              frequencyType,
                                                              ClaimDocumentType.HCFA1500Single);
            }
            else
            {
                claimSubmissionId = await SubmitClaim(claimId,
                                                      memberId,
                                                      frequencyType,
                                                      submissionType,
                                                      ClaimDocumentType.HCFA1500Single,
                                                      responsibilitySequence,
                                                      latestClaimSubmission);
            }

            var newClaimSubmission = await GetFullClaimSubmission(claimSubmissionId);
            return await CreateHCFAModel(newClaimSubmission);
        }

        private async Task<ClaimHFCAModel> CreateHCFAModel(ClaimSubmissionEntity claimSubmission = null)
        {
            if (claimSubmission == null) return new ClaimHFCAModel();

            var claim = claimSubmission.Claim;
            claim.ClaimChargeEntries = claim.ClaimChargeEntries.Where(x => x.DateDeleted == null).ToList();
            var funderSequences = claimSubmission.ClaimSubmissionFunderSequences;
            var funderSequence = funderSequences.FirstOrDefault(fs => fs.FunderId == claimSubmission.FunderId && fs.FunderResponsibilitySequence == claimSubmission.ResponsibilitySequence);
            var nextResponsibilitySequence = ResponsibilitySequenceTypeHelper.FromOrdinal(ResponsibilitySequenceTypeHelper.FromString(claimSubmission.ResponsibilitySequence).AsOrdinal() + 1).AsString();
            var nextFunderSequence = funderSequences.FirstOrDefault(fs => fs.FunderResponsibilitySequence == nextResponsibilitySequence);
            var billingProviderOption = funderSequence?.ServiceLineBillingProviderOption ?? BillingProviderOptionType.Unknown;
            var billingProviderIsIndividual = billingProviderOption == BillingProviderOptionType.Individual;
            var renderingProviderNPI = claimSubmission.RenderingProviderStaffNpiNumber;
            var funderMappings = await _rethinkServices.GetChildProfileFunderMappingByMappingId(claimSubmission.Claim.AccountInfoId, claimSubmission.Claim.ChildProfileId, claimSubmission.Claim.ClientFunderId ?? 0);

            var hcfa = new ClaimHFCAModel();

            hcfa.Id = claim.Id;
            hcfa.FunderId = funderSequence.FunderId;
            hcfa.FunderName = funderSequence.FunderName;
            hcfa.FunderAddress = funderSequence.InsuranceAddress1;
            hcfa.FunderAddress2 = funderSequence.InsuranceAddress2;
            hcfa.FunderCity = funderSequence.InsuranceCity;
            hcfa.FunderZip = funderSequence.InsuranceZip;
            hcfa.FunderState = funderSequence.InsuranceState;
            hcfa.FunderMobile = claimSubmission.FunderDetails.phone;

            hcfa.InsuredCoverageTypeId = claimSubmission.FunderDetails.funderCoverageTypeId;
            hcfa.InsuredNumber = funderSequence.InsurancePolicyNumber ?? "";
            hcfa.InsuredPolicyGroupNumber = funderSequence.InsuranceGroupNumber ?? "";
            hcfa.InsuredName = (funderSequence.SubscriberLastName + ", " + funderSequence.SubscriberFirstName + ", " + funderSequence.SubscriberMiddleName).Trim(' ').Trim(',');
            hcfa.InsuredAddress = funderSequence.SubscriberAddress1 ?? "";
            hcfa.InsuredAddress2 = funderSequence.SubscriberAddress2 ?? "";
            hcfa.InsuredCity = funderSequence.SubscriberCity ?? "";
            hcfa.InsuredZip = funderSequence.SubscriberZip ?? "";
            hcfa.InsuredState = funderSequence.SubscriberState;
            hcfa.InsuredMobile = funderMappings?.InsuranceContact?.PhoneNumber ?? "";
            hcfa.InsuredSex = GetGenderId(funderSequence.SubscriberGender);
            hcfa.InsuredDOB = funderSequence.SubscriberDOB ?? DateTime.MinValue;
            hcfa.InsurancePlanName = funderSequence.InsurancePlanName ?? "";

            hcfa.PatientName = (claimSubmission.ChildProfileLastName + ", " +
                                                claimSubmission.ChildProfileFirstName + ", " +
                                                claimSubmission.ChildProfileMiddleName).Trim(' ').Trim(',');
            hcfa.PatientAddress = claimSubmission.ChildProfileAddress1 ?? "";
            hcfa.PatientAddress2 = claimSubmission.ChildProfileAddress2 ?? "";
            hcfa.PatientCity = claimSubmission.ChildProfileCity ?? "";
            hcfa.PatientState = claimSubmission.ChildProfileState ?? "";
            hcfa.PatientZip = claimSubmission.ChildProfileZip ?? "";
            hcfa.PatientDOB = claimSubmission.ChildProfileDOB ?? DateTime.MinValue;
            hcfa.PatientSex = GetGenderId(claimSubmission.ChildProfileGender);
            hcfa.PatientMobile = "";
            hcfa.PatientRelationShipToInsured = funderSequence.RelationshipToSubscriber;
            hcfa.ReleaseOfInformationConfirmationType = claimSubmission.ReleaseOfInformationConfirmationType ?? "";
            hcfa.AuthorizedPaymentConfirmationType = claimSubmission.AuthorizedPaymentConfirmationType ?? "";
            hcfa.PatientFunderSignatureDate = funderSequence.ReleaseOfInformationConfirmationDate;

            var diagnosisCodeIds = await _claimDiagnosisCodeRepository.Query().Where(x => x.ClaimId == claim.Id).OrderBy(x => x.Order).ToListAsync();

            var diagnosisCodesList = new List<Diagnosis>();

            foreach (var diagnosisCode in diagnosisCodeIds)
            {
                var diagnosis = await _rethinkServices.GetDiagnosisById(diagnosisCode.DiagnosisId);
                diagnosisCodesList.Add(diagnosis);
            }

            hcfa.PatientDiagnosis = diagnosisCodesList.Select(x => x.diagnosisCode).ToList();
            var isAuthorizationNotNeeded = claimSubmission.ChildProfileAuthorization?.authorizationSubmissionTypeId == (int)AuthorizationStatus.NotNeeded;

            hcfa.AuthorizationNumber = isAuthorizationNotNeeded ? "" : claimSubmission.AuthorizationNumber ?? "";
            hcfa.AuthorizationStartDate = isAuthorizationNotNeeded ? DateTime.UtcNow : claimSubmission.ChildProfileAuthorization?.startDate ?? claimSubmission.Claim?.DateCreated ?? DateTime.UtcNow;
            hcfa.AuthorizationEndDate = isAuthorizationNotNeeded ? DateTime.UtcNow : claimSubmission.ChildProfileAuthorization?.endDate ?? claimSubmission.Claim?.DateCreated ?? DateTime.UtcNow;

            hcfa.LocationNumber = claimSubmission.ServiceLocationNpiNumber ?? "";


            hcfa.ClaimChargeEntries = claim.ClaimChargeEntries.Select(ce => new ClaimChargeEntryEntity
            {
                Units = ce.Units,
                BillingCode = ce.BillingCode ?? "",
                Modifier1 = ce.Modifier1 ?? "",
                Modifier2 = ce.Modifier2 ?? "",
                Modifier3 = ce.Modifier3 ?? "",
                Modifier4 = ce.Modifier4 ?? "",
                UnitType = ce.UnitType,
                DateOfService = ce.DateOfService,
                DateCreated = ce.DateCreated,

                DiagnosisCode = ce.DiagnosisCode ?? "",
                Charges = ce.Charges,
                Claim = new ClaimEntity
                {
                    RenderingStaffMemberId = ce.Claim.RenderingStaffMemberId,
                    LocationCode = ce.Claim.LocationCode,
                    RenderingProviderNPI = renderingProviderNPI,
                    StartDate = ce.Claim.StartDate,
                }
            }).ToList();


            var serviceLocation = claim.ServiceLocationId.HasValue ? await _rethinkServices.GetProviderLocation(claim.AccountInfoId, claim.ServiceLocationId.Value) : null;
            var providerLocation = claim.ProviderLocationId.HasValue ? await _rethinkServices.GetProviderLocation(claim.AccountInfoId, claim.ProviderLocationId.Value) : null;

            hcfa.MedicalRecordNumber = funderSequence.MedicalRecordNumber ?? "";
            hcfa.TotalCharge = claimSubmission.ClaimSubmissionServiceLines.Sum(c => c.Charges) ?? 0;
            hcfa.Paid = claim.PaymentClaims.Sum(p => p.TotalPayment) ?? 0;
            hcfa.RenderingProviderName = (claimSubmission.ResolvedRenderingProviderFirstName + ", " +
                                                 claimSubmission.ResolvedRenderingProviderName + ", " +
                                                 claimSubmission.ResolvedRenderingProviderMiddleName).Trim(' ').Trim(',');
            hcfa.ServiceLineBillingProviderOption = billingProviderOption;

            var claimBillingProviderDetials = claimSubmission?.Claim?.ClaimBillingProviders?.FirstOrDefault();

            var hasValidProviderLocation = providerLocation != null && providerLocation.id != 0;
            if (!hasValidProviderLocation && claimBillingProviderDetials != null)
            {
                hcfa.ProviderName = GetProviderName(claimBillingProviderDetials);
                hcfa.ProviderPhoneNumber = providerLocation?.phone ?? "";
                hcfa.ProviderAddress1 = claimBillingProviderDetials.AddressLine1 ?? "";
                hcfa.ProviderAddress2 = claimBillingProviderDetials.AddressLine2 ?? "";
                hcfa.ProviderCity = claimBillingProviderDetials.City ?? "";
                hcfa.ProviderState = claimBillingProviderDetials.State ?? "";
                hcfa.ProviderZip = claimBillingProviderDetials.Zip ?? "";
                hcfa.ProviderZipExt = claimBillingProviderDetials.ZipExt ?? "";
                hcfa.ProviderLocationNPI = claimBillingProviderDetials.NPI ?? "";
                hcfa.ProviderTaxonomyCode = claimBillingProviderDetials.TaxonomyCode ?? "";
            }
            else if (hasValidProviderLocation)
            {
                hcfa.ProviderName = providerLocation.agencyName ?? "";
                hcfa.ProviderLocation = providerLocation?.name ?? "";
                hcfa.ProviderPhoneNumber = providerLocation.phone ?? "";
                hcfa.ProviderAddress1 = providerLocation.address.street1 ?? "";
                hcfa.ProviderAddress2 = providerLocation.address.street2 ?? "";
                hcfa.ProviderCity = providerLocation.address.city ?? "";
                hcfa.ProviderZip = providerLocation.address.zip ?? "";
                hcfa.ProviderState = claimSubmission.LocationBillingProviderState ?? "";
                hcfa.ProviderCountry = claimSubmission.LocationBillingProviderCountry ?? "";
                hcfa.ProviderLocationNPI = providerLocation.npiNumber ?? "";
                hcfa.ProviderLocationTaxId = providerLocation.federalTaxId ?? "";
            }
            hcfa.ServiceLocation = claimSubmission.ServiceLocationName ?? "";
            hcfa.ServiceCountry = claimSubmission.ServiceLocationCountry ?? "";
            hcfa.ServiceState = claimSubmission.ServiceLocationState ?? "";

            if (serviceLocation != null)
            {
                hcfa.ServiceName = serviceLocation.agencyName ?? "";
                hcfa.ServicePhoneNumber = serviceLocation.phone ?? "";
                hcfa.ServiceLocationTaxId = serviceLocation.federalTaxId ?? "";
                hcfa.ServiceLocationNPI = serviceLocation.npiNumber ?? "";
                if (serviceLocation.address != null)
                {
                    hcfa.ServiceAddress1 = serviceLocation.address.street1 ?? "";
                    hcfa.ServiceAddress2 = serviceLocation.address.street2 ?? "";
                    hcfa.ServiceCity = serviceLocation.address.city ?? "";
                    hcfa.ServiceZip = serviceLocation.address.zip ?? "";
                }
                else
                {
                    hcfa.ServiceAddress1 = "";
                    hcfa.ServiceAddress2 = "";
                    hcfa.ServiceCity = "";
                    hcfa.ServiceZip = "";
                }

            }
            else
            {
                hcfa.ServiceName = "";
                hcfa.ServicePhoneNumber = "";
                hcfa.ServiceAddress1 = "";
                hcfa.ServiceAddress2 = "";
                hcfa.ServiceCity = "";
                hcfa.ServiceLocationTaxId = "";
                hcfa.ServiceLocationNPI = "";
                hcfa.ServiceZip = "";
            }


            hcfa.FederalTaxId = billingProviderIsIndividual ? claimSubmission.AccountFederalTaxId : claimSubmission.ResolvedBillingProviderFederalTaxID;
            hcfa.AuthoriseReleaseOfInfo = claimSubmission.Claim.AuthorizedPaymentConfirmationTypeId;

            hcfa.ReferringProviderName = (claimSubmission.ReferringProviderFirstName + ", " +
                                                claimSubmission.ReferringProviderLastName).Trim(' ').Trim(',');
            hcfa.ReferringProviderNPI = claimSubmission.ReferringProviderNpiNumber;

            await MapHcfaAdressAsync(hcfa, claim, claimSubmission);

            if (nextFunderSequence != null)
            {
                hcfa.SecondaryInsuredName = (nextFunderSequence.SubscriberFirstName + ", " +
                                             funderSequence.SubscriberLastName + ", " +
                                             funderSequence.SubscriberMiddleName).Trim(' ').Trim(',');
                hcfa.SecondaryInsuredNumber = nextFunderSequence.InsurancePolicyNumber ??
                                              nextFunderSequence.InsuranceGroupNumber;
                hcfa.SecondaryInsurancePlanName = nextFunderSequence.InsurancePlanName;

            }

            return hcfa;

        }

        public async Task<ClaimEntity> GetFullClaim(int claimId)
        {
            var claim = await _billingClaimRepository.Query()
                                                        .Include(cs => cs.ClaimValidationErrors)
                                                            .ThenInclude(err => err.ClaimErrorMessage)
                                                        .Include(c => c.ClaimChargeEntries)
                                                        .Include(c => c.ClaimDiagnosisCodes)
                                                        .Include(c => c.ClaimAppointmentLinks)
                                                        .Include(c => c.ClaimSubmissions)
                                                        .Include(c => c.ClaimHistory)
                                                        .FirstOrDefaultAsync(c => c.Id == claimId &&
                                                                            c.DateDeleted == null);

            if (claim.ClaimDiagnosisCodes.Count > 0)
            {
                claim.ClaimDiagnosisCodes = claim.ClaimDiagnosisCodes
                    .Where(cd => cd.DateDeleted == null)
                    .ToList();

                await Parallel.ForEachAsync(claim.ClaimDiagnosisCodes, new ParallelOptions { MaxDegreeOfParallelism = 5 },
                async (claimDiagnos, token) =>
                {
                    claimDiagnos.Diagnosis = await _rethinkServices.GetClientDiagnosisByIdReturningEntityAsync(claimDiagnos.DiagnosisId);
                });
            }
            claim.ChildProfileAuthorization = null;
            if (claim.AuthorizationId.HasValue)
            {
                claim.ChildProfileAuthorization = await _rethinkServices.GetChildProfileAuthorizationByClientId(claim.AccountInfoId, claim.ChildProfileId, claim.AuthorizationId.Value);
                if (claim.ChildProfileAuthorization != null)
                {

                    var childProfileFunderServiceLineMapping = _rethinkServices.GetChildProfileFunderServiceLineMappingEntity(claim.AccountInfoId, claim.ChildProfileId, claim.ClientFunderId ?? 0, claim.ChildProfileAuthorization.childProfileFunderServiceLineMappingId.FirstOrDefault());
                    var childProfileReferringProvider = claim.ChildProfileAuthorization.childProfileReferringProviderId != null ? _rethinkServices.GetChildProfileReferringProviderEntity(claim.AccountInfoId, claim.ChildProfileId, claim.ChildProfileAuthorization.childProfileReferringProviderId.Value) : null;
                    var billingProvider = _rethinkServices.GetProviderLocation(claim.AccountInfoId, claim.ChildProfileAuthorization.billingProviderId ?? 0);
                    var childProfileAuthorizationDiagnosisCodes = _rethinkServices.GetChildProfileAuthorizationDiagnosisCodesAsync(claim.AccountInfoId, claim.ChildProfileId, claim.ChildProfileAuthorization.childProfileDiagnosisId, claim.ChildProfileAuthorization.id);
                    var providerLocationIdAsync = _clientService.GetClientFacilityIdAsync(claim.ChildProfileId, claim.AccountInfoId);

                    await Task.WhenAll(new Task?[]
                    { childProfileFunderServiceLineMapping, childProfileReferringProvider, billingProvider, childProfileAuthorizationDiagnosisCodes, providerLocationIdAsync }
                    .Where(t => t is not null).Cast<Task>());

                    claim.ChildProfileAuthorization.ChildProfileReferringProvider = childProfileReferringProvider != null ? await childProfileReferringProvider : null;
                    var providerLocationId = await providerLocationIdAsync;
                    var serviceFacilityLocation = _rethinkServices.GetProviderLocation(claim.AccountInfoId, providerLocationId);
                    claim.ChildProfileAuthorization.ServiceFacilityLocation = serviceFacilityLocation != null ? await serviceFacilityLocation : null;
                    claim.ChildProfileAuthorization.BillingProvider = billingProvider != null ? await billingProvider : null;
                    claim.ChildProfileAuthorization.ChildProfileAuthorizationDiagnosisCodes = childProfileAuthorizationDiagnosisCodes != null ? await childProfileAuthorizationDiagnosisCodes : null;
                    claim.ChildProfileAuthorization.ChildProfileFunderServiceLineMapping = childProfileFunderServiceLineMapping != null ? await childProfileFunderServiceLineMapping : null;

                    if (claim.ChildProfileAuthorization.ChildProfileFunderServiceLineMapping != null && claim.ChildProfileAuthorization.ChildProfileFunderServiceLineMapping.ChildProfileFunderMapping != null)
                    {
                        claim.ChildProfileAuthorization.ChildProfileFunderServiceLineMapping.ChildProfileFunderMapping.Funder = await _rethinkServices.GetFunder(claim.AccountInfoId, claim.ChildProfileAuthorization.ChildProfileFunderServiceLineMapping.ChildProfileFunderMapping.funderId);
                    }
                }
            }

            var childProfile = _rethinkServices.GetChildProfileReturningEntity(claim.AccountInfoId, claim.ChildProfileId);
            var accountInfo = _rethinkServices.GetAccountReturningEntityAsync(claim.AccountInfoId, true);
            var renderingStaffMember = claim.RenderingStaffMemberId.HasValue ? _rethinkServices.GetMemberAsync(claim.AccountInfoId, claim.RenderingStaffMemberId.Value) : null;
            var referringProvider = claim.ChildProfileReferringProviderId.HasValue ? _rethinkServices.GetChildProfileReferringProviderEntity(claim.AccountInfoId, claim.ChildProfileId, claim.ChildProfileReferringProviderId ?? 0) : null;
            var providerLocation = claim.ProviderLocationId.HasValue ? _rethinkServices.GetProviderLocation(claim.AccountInfoId, claim.ProviderLocationId.Value) : null;
            var serviceLocation = claim.ServiceLocationId.HasValue ? _rethinkServices.GetProviderLocation(claim.AccountInfoId, claim.ServiceLocationId.Value) : null;
            var clientFunder = claim.ClientFunderId.HasValue ? _rethinkServices.GetChildProfileFunderMappingByMappingId(claim.AccountInfoId, claim.ChildProfileId, claim.ClientFunderId ?? 0) : null;
            var locationCodes = _rethinkServices.GetLocationCodes();

            await Task.WhenAll(new Task?[]
            { childProfile, accountInfo, renderingStaffMember, referringProvider, providerLocation, serviceLocation, clientFunder, locationCodes}
            .Where(t => t is not null).Cast<Task>());

            claim.ChildProfile = childProfile != null ? await childProfile : null;
            claim.AccountInfo = accountInfo != null ? await accountInfo : null;
            claim.ServiceLocation = serviceLocation != null ? await serviceLocation : null;
            claim.RenderingStaffMember = renderingStaffMember != null ? await renderingStaffMember : null;
            claim.ReferringProvider = referringProvider != null ? await referringProvider : null;
            claim.ProviderLocation = providerLocation != null ? await providerLocation : null;
            claim.ClientFunder = clientFunder != null ? await clientFunder : null;
            var locarionCodes = locationCodes != null ? await locationCodes : null;

            var locationCodeById = locarionCodes.FirstOrDefault(x => x.id == claim.LocationCodeId);
            claim.LocationCode = locationCodeById;
            claim.ServiceLocation = claim.ServiceLocationId.HasValue ? await _rethinkServices.GetProviderLocation(claim.AccountInfoId, claim.ServiceLocationId.Value) : null;
            claim.ClientFunder = claim.ClientFunderId.HasValue ? await _rethinkServices.GetChildProfileFunderMappingByMappingId(claim.AccountInfoId, claim.ChildProfileId, claim.ClientFunderId ?? 0) : null;
            return claim;
        }

        //---------------------------------------------------------------------------------
        // private
        //---------------------------------------------------------------------------------
        private async Task<ClaimSubmissionEntity> GetFullClaimSubmission(int claimSubmissionId, bool isRebill = true, bool isSecondary = false)
        {
            ClaimSubmissionEntity submission = null;
            var query = _billingClaimSubmissionRepository.Query().AsNoTracking()
                                                                       .Include(cs => cs.Claim)
                                                                           .ThenInclude(c => c.PaymentClaims)
                                                                       .Include(cs => cs.Claim)
                                                                           .ThenInclude(c => c.ClaimChargeEntries)
                                                                       .Include(cs=>cs.Claim)
                                                                            .ThenInclude(c=>c.ClaimBillingProviders)
                                                                       .Include(cs => cs.ClaimValidationErrors)
                                                                           .ThenInclude(err => err.ClaimErrorMessage)
                                                                       .Include(c => c.ClaimSubmissionServiceLines)
                                                                           .ThenInclude(sl => sl.ClaimChargeEntry)
                                                                       .Where(cs => cs.DateDeleted == null
                                                                       && (isRebill ? cs.Id == claimSubmissionId :
                                                                       cs.ClaimId == claimSubmissionId))
                                                                       .OrderByDescending(cs => cs.Id);


            var initialSubmission = await query.FirstOrDefaultAsync();
            if (initialSubmission != null)
            {
                // Filter Claim related entities
                if (initialSubmission.Claim != null)
                {
                    initialSubmission.Claim.PaymentClaims = initialSubmission.Claim.PaymentClaims
                                                            .Where(pc => pc.DateDeleted == null)
                                                            .ToList();
                    initialSubmission.Claim.ClaimChargeEntries = initialSubmission.Claim.ClaimChargeEntries
                                                                .Where(cce => cce.DateDeleted == null)
                                                                .ToList();
                }
                // Filter ClaimValidationErrors
                initialSubmission.ClaimValidationErrors = initialSubmission.ClaimValidationErrors
                                                            .Where(cve => cve.DateDeleted == null)
                                                            .ToList();

                // Filter ClaimSubmissionServiceLines
                var filteredServiceLines = initialSubmission.ClaimSubmissionServiceLines
                                                            .Where(ssl => ssl.ClaimChargeEntry != null
                                                            && ssl.ClaimChargeEntry.DateDeleted == null)
                                                            .ToList();

                initialSubmission.ClaimSubmissionServiceLines = filteredServiceLines;

                // Fetch latest ClaimSubmissionFunderSequences for the current submission
                var funderSequences = await _billingClaimSubmissionFunderSequenceRepository.Query()
                    .Where(fs => fs.ClaimSubmissionId == initialSubmission.Id && fs.DateDeleted == null)
                    .GroupBy(fs => fs.FunderResponsibilitySequence)
                    .Select(group => group.OrderByDescending(fs => fs.SequenceOrder).FirstOrDefault())
                    .ToListAsync();

                initialSubmission.ClaimSubmissionFunderSequences = funderSequences;

                submission = initialSubmission;
            }

            if (submission != null && submission.AuthorizationNumber == null && submission.Claim.AuthorizationNumber != null)
            {
                submission.AuthorizationNumber = submission.Claim.AuthorizationNumber.Trim();
            }

            var childProfileAuthorization = await _rethinkServices.GetChildProfileAuthorizationByClientId(submission.Claim.AccountInfoId, submission.Claim.ChildProfileId, submission.ChildProfileAuthorizationId);

            submission.ChildProfileAuthorization = childProfileAuthorization;

            submission.Claim.ChildProfile = await _rethinkServices.GetChildProfileReturningEntity(submission.Claim.AccountInfoId, submission.Claim.ChildProfileId);

            submission.Claim.RenderingStaffMember = await _rethinkServices.GetMemberAsync(submission.Claim.AccountInfoId, submission.Claim.RenderingStaffMemberId ?? 0);

            submission.FunderDetails = await _rethinkServices.GetFunder(submission.Claim.AccountInfoId, submission.Claim.PrimaryFunderId);

            var providerLocation = await _rethinkServices.GetProviderLocation(submission.Claim.AccountInfoId, submission.Claim.ProviderLocationId.Value);

            submission.Claim.ServiceLocation = submission.Claim.ServiceLocationId.HasValue ? await _rethinkServices.GetProviderLocation(submission.Claim.AccountInfoId, submission.Claim.ServiceLocationId.Value) : providerLocation;

            if (submission.Claim.ChildProfileReferringProviderId.HasValue)
            {
                var clientReferringProvider = await _rethinkServices.GetChildProfileReferringProviderEntity(submission.Claim.AccountInfoId, submission.Claim.ChildProfileId, submission.Claim.ChildProfileReferringProviderId ?? 0);

                submission.Claim.ReferringProvider = clientReferringProvider;
            }

            var locationCodes = await _rethinkServices.GetLocationCodes();
            submission.Claim.LocationCode = locationCodes.Where(x => x.id == submission.Claim.LocationCodeId).FirstOrDefault();

            var unitTypes = await _rethinkServices.GetUnitTypesAsync();
            foreach (var claimChargEntry in submission.Claim.ClaimChargeEntries)
            {
                var unitType = unitTypes.FirstOrDefault(x => x.id == claimChargEntry.UnitTypeId);
                if (unitType != null)
                {
                    claimChargEntry.UnitType = unitType;
                }
            }

            //Secondary Billing changes            

            if (isSecondary)
            {
                var validPaymentTypes = new HashSet<int>
                   {
                       (int)PaymentTypes.InsurancePayment,
                       (int)PaymentTypes.ERAReceived,
                       (int)PaymentTypes.OtherPayment
                   };

                var priorSubmissions = await _billingClaimSubmissionRepository.Query()
                       .Where(cs => cs.ClaimId == initialSubmission.ClaimId &&
                                    cs.Id != initialSubmission.Id &&
                                    cs.DateDeleted == null)
                       .OrderByDescending(cs => cs.Id)
                       .Select(cs => new { cs.Id, cs.ResponsibilitySequence })
                       .ToListAsync();

                var currentSequence = ResponsibilitySequenceHelper.GetEnumFromString<ResponsibilitySequenceType>(submission.ResponsibilitySequence);
                if (!currentSequence.HasValue)
                    return submission;

                var priorSequences = Enum.GetValues(typeof(ResponsibilitySequenceType))
                       .Cast<ResponsibilitySequenceType>()
                       .Where(seq => seq < currentSequence)
                       .Select(seq => seq.GetEnumMemberValue())
                       .ToList();

                var latestPriorSubmissionIds = priorSequences
                   .Select(seq => priorSubmissions.FirstOrDefault(ps => ps.ResponsibilitySequence == seq)?.Id)
                   .Where(id => id.HasValue)
                   .Select(id => id.Value)
                   .ToList();

                if (!latestPriorSubmissionIds.Any())
                    return submission;

                var priorFunderSequences = await _billingClaimSubmissionFunderSequenceRepository.Query()
                           .Where(fs => latestPriorSubmissionIds.Contains(fs.ClaimSubmissionId) &&
                                        fs.DateDeleted == null)
                           .GroupBy(fs => fs.ClaimSubmissionId)
                           .Select(g => g.OrderByDescending(fs => fs.SequenceOrder).FirstOrDefault())
                           .ToListAsync();

                submission.ClaimSubmissionFunderSequences = submission.ClaimSubmissionFunderSequences
                   .Concat(priorFunderSequences)
                   .ToList();

                var baseIdentifier = "";
                baseIdentifier = !string.IsNullOrEmpty(submission.ClaimSubmissionIdentifier) && submission.ClaimSubmissionIdentifier.Length > 1
                               ? submission.ClaimSubmissionIdentifier.Substring(0, submission.ClaimSubmissionIdentifier.Length - 1)
                               : submission.ClaimSubmissionIdentifier;

                var EraSecondaryBaseIdentifier = await _billingPaymentClaimRepository.Query()
                    .Where(fs => fs.ClaimId == submission.ClaimId && fs.ClaimStatus == fs.ClaimStatusOrig)
                    .OrderByDescending(fs => fs.DateCreated)
                    .Select(fs => fs.ClaimIdentifierOrig)
                    .FirstOrDefaultAsync();

                if (EraSecondaryBaseIdentifier != null)
                {
                    baseIdentifier = EraSecondaryBaseIdentifier;
                }

                var latestClaimStatus = await _billingPaymentClaimRepository.Query()
                    .Where(pc => pc.ClaimIdentifier == baseIdentifier)
                    .OrderByDescending(pc => pc.DateLastModified)
                    .Select(pc => pc.ClaimStatusOrig)
                    .FirstOrDefaultAsync();


                ResponsibilitySequenceType? previousSequence;

                if (latestClaimStatus != null)
                {
                    previousSequence = ResponsibilitySequenceHelper.GetCurrentSequence(currentSequence.Value);

                }
                else
                {
                    previousSequence = ResponsibilitySequenceHelper.GetPreviousSequence(currentSequence.Value);
                }


                if (!previousSequence.HasValue)
                    return submission; // Return as-is if no previous sequence exists

                string previousSequenceValue = previousSequence?.GetEnumMemberValue();

                // Get latest funder ID
                int? latestFunderId = await _billingClaimSubmissionRepository.Query()
                    .Where(fs => fs.ClaimId == submission.ClaimId &&
                                 fs.ResponsibilitySequence == previousSequenceValue &&
                                 fs.DateDeleted == null)
                    .OrderByDescending(fs => fs.Id)
                    .Select(fs => fs.FunderId)
                    .FirstOrDefaultAsync();

                if (!latestFunderId.HasValue)
                    return submission; // Return submission as-is if no prior funder found

                // Fetch latest payment only if valid funder ID exists

                PaymentClaimEntity? latestPayment;

                latestPayment = await _billingPaymentClaimRepository.Query()
                    .Where(pc => pc.ClaimId == submission.ClaimId &&
                                 pc.DateDeleted == null &&
                                 pc.Payment.HcFunderId == latestFunderId &&
                                 validPaymentTypes.Contains(pc.Payment.PaymentTypeId))
                    .Include(pc => pc.PaymentClaimServiceLines)
                        .ThenInclude(pcs => pcs.PaymentClaimServiceLineAdjustments)
                    .Include(pc => pc.PaymentClaimAdjustments)
                    .Include(pc => pc.Payment)
                    .OrderByDescending(pc => pc.Payment.PaymentDate)
                    .FirstOrDefaultAsync();

                if (latestPayment == null && previousSequenceValue == "S")
                {
                    var hcPaymentIds = await _billingPaymentClaimRepository.Query()
                        .Where(pc => pc.ClaimId == submission.ClaimId)
                        .OrderByDescending(pc => pc.ClaimStatusOrig)
                        .Select(pc => pc.PaymentId)
                        .ToListAsync();

                    var containsData = await _billingPaymentClaimRepository.Query()
                        .Where(pc => hcPaymentIds.Contains(pc.Payment.Id) &&
                                     pc.Payment.FunderID == "1205" &&
                                     pc.Payment.PaymentEraUploadId != null &&
                                     pc.Payment.IsManualPayment == false)
                        .Include(pc => pc.Payment)
                        .OrderByDescending(pc => pc.Payment.PostDate)
                        .FirstOrDefaultAsync();

                    if (containsData?.Payment != null)
                    {
                        var EraServiceLineData = await _billingPaymentClaimRepository.Query()
                            .Where(pc => pc.ClaimId == submission.ClaimId &&
                                         pc.DateDeleted == null &&
                                         pc.Payment.HcFunderId == 1205 &&
                                         validPaymentTypes.Contains(pc.Payment.PaymentTypeId))
                            .OrderByDescending(pc => pc.DateCreated)
                            .Include(pc => pc.PaymentClaimServiceLines)
                                .ThenInclude(pcs => pcs.PaymentClaimServiceLineAdjustments)
                            .Include(pc => pc.PaymentClaimAdjustments)
                            .Include(pc => pc.Payment)
                            .FirstOrDefaultAsync();


                        latestPayment = await _billingPaymentClaimRepository.Query()
                            .Where(pc => pc.ClaimId == submission.ClaimId &&
                                         pc.DateDeleted == null &&
                                         pc.Payment.HcFunderId == 1205 &&
                                         validPaymentTypes.Contains(pc.Payment.PaymentTypeId))
                            .Include(pc => pc.PaymentClaimServiceLines)
                                .ThenInclude(pcs => pcs.PaymentClaimServiceLineAdjustments)
                            .Include(pc => pc.PaymentClaimAdjustments)
                            .Include(pc => pc.Payment)
                            .OrderByDescending(pc => pc.Payment.PaymentDate)
                            .FirstOrDefaultAsync();

                        var singleServiceLine = EraServiceLineData?.PaymentClaimServiceLines.SingleOrDefault();

                        if (singleServiceLine?.PaymentClaimServiceLineAdjustments != null)
                        {
                            latestPayment.PaymentClaimServiceLines.ToList().ForEach(sl => sl.PaymentClaimServiceLineAdjustments = singleServiceLine.PaymentClaimServiceLineAdjustments);

                            var prAdjustment = singleServiceLine.PaymentClaimServiceLineAdjustments.FirstOrDefault(adj => adj.AdjustmentGroupCode == "PR");
                            if (prAdjustment != null)
                            {
                                latestPayment.PatientRespAmount = prAdjustment.AdjustmentAmount;
                            }
                        }
                    }
                }

                if (latestPayment == null)
                    return submission; // Return submission as-is if no prior payment found

                // Filter out deleted records in memory

                latestPayment.PaymentClaimServiceLines = latestPayment.PaymentClaimServiceLines?
                    .Where(pcs => pcs.DateDeleted == null)
                    .ToList();

                foreach (var serviceLine in latestPayment.PaymentClaimServiceLines ?? Enumerable.Empty<PaymentClaimServiceLineEntity>())
                {
                    serviceLine.PaymentClaimServiceLineAdjustments = serviceLine.PaymentClaimServiceLineAdjustments?
                        .Where(adjustment => adjustment.DateDeleted == null)
                        .ToList();
                }

                latestPayment.PaymentClaimAdjustments = latestPayment.PaymentClaimAdjustments?
                    .Where(adj => adj.DateDeleted == null)
                    .ToList();

                // Ensure non-null collections for submission object
                submission.PriorFunderLatestClaimPayment = latestPayment;
                submission.PriorFunderLatestClaimPayment.PaymentClaimServiceLines ??= new List<PaymentClaimServiceLineEntity>();
                submission.PriorFunderLatestClaimPayment.PaymentClaimAdjustments ??= new List<PaymentClaimAdjustmentEntity>();
                foreach (var serviceLine in submission.PriorFunderLatestClaimPayment.PaymentClaimServiceLines)
                {
                    serviceLine.PaymentClaimServiceLineAdjustments ??= new List<PaymentClaimServiceLineAdjustmentEntity>();
                }
            }

            //Secondary Billing changes end

            return submission;

        }

        private ClaimSubmissionEntity CloneClaimSubmissionFor(ClaimSubmissionEntity claimSubmission,
                                                              ClaimFrequencyType frequencyType,
                                                              ClaimSubmissionType submissionType,
                                                              ClaimDocumentType documentType,
                                                              ResponsibilitySequenceType responsibilitySequence)
        {
            List<string> additionalPropertyNamesToSkip = new List<string> { "Claim", "ClaimValidationErrors" };

            // commenting this code as we need servicelines to compare
            //if (frequencyType == ClaimFrequencyType.Original ||
            //    frequencyType == ClaimFrequencyType.Replacement)
            //{
            //    // for replacement claims, we will be re-creating the service lines and funder sequences from the latest data
            //    // so skip loading the service lines and funder sequences
            //    additionalPropertyNamesToSkip.AddRange(new List<string>
            //    {
            //        "ClaimSubmissionServiceLines",
            //        "ClaimSubmissionFunderSequences",
            //    });
            //}

            var newClaimSubmission = new ClaimSubmissionEntity();
            EntityPropertyCopier.Copy(claimSubmission, newClaimSubmission, additionalPropertyNamesToSkip);
            newClaimSubmission.SubmitDate = EstDateTime;
            newClaimSubmission.FrequencyType = frequencyType;
            newClaimSubmission.SubmissionType = submissionType;
            newClaimSubmission.DocumentType = documentType;
            newClaimSubmission.ResponsibilitySequence = responsibilitySequence.AsString();
            newClaimSubmission.SubmissionStatus = (documentType == ClaimDocumentType.HCFA1500Single) ? ClaimSubmissionStatus.FunderPending : ClaimSubmissionStatus.ClearingHousePending;
            newClaimSubmission.SubmissionStatus = (documentType == ClaimDocumentType.HCFA1500Multi ||
                                                         documentType == ClaimDocumentType.HCFA1500Single ||
                                                         documentType == ClaimDocumentType.UB04Multi ||
                                                         documentType == ClaimDocumentType.UB04Single) ? ClaimSubmissionStatus.FunderPending
                                                                                                           : ClaimSubmissionStatus.ClearingHousePending;
            return newClaimSubmission;
        }


        /// <summary>
        /// Fetch the latest claim in the chain of claim submissions
        /// </summary>
        private async Task<ClaimSubmissionEntity> GetLatestClaimSubmission(int claimId)
        {
            var lastSubmission = await _billingClaimSubmissionRepository.Query().AsNoTracking()
                                                                        .Where(clmSub => clmSub.ClaimId == claimId &&
                                                                                        clmSub.DateDeleted == null)
                                                                        .OrderByDescending(clmSub => clmSub.Id)
                                                                        .FirstOrDefaultAsync();
            return (lastSubmission == null) ? null : await GetFullClaimSubmission(lastSubmission.Id);// look it up again to get all the includes
        }

        private async Task<string> GenerateClaimIdentifier(DateTime dateOfService, int childProfileId)
        {
            /*
                Format for claim identifiers:

                  DateOfService (StartDate of claim)
                    2 digit year : 21-99
                    Month: 01-12
                    Day: 01-31
                  [-]
                  Child Profile ID: The ChildProfileId (int converted to hexatridecimal (Base 36)) padded to 5 digits.
                  [-]
                  Claim Sequence: 0-9,A-Z (int converted to hexatridecimal (Base 36))

                Claim Example: 221102-00E3P-2 
                    DateOfService = 2022-11-02
                    ChildProfileId = 18277
                    Sequence = 2nd for that date of service

                    * The size for this with dashes is 14 (6+1+5+1+1)
                    * The size for this without dashes is 12 (6+5+1)
                     
                     
                The  hexatridecimal format is a Base 36 number that contains characters 0-9 and A-Z 
                to represent the integer in a much shorter length (every position in the identifier 
                can contain the integer value 0-35 instead of 0-9). For example, a 5 digit 
                hexatridecimal ClientProfileId will allow for up to 60  million client identifiers.   

             */


            // Update the claim identifier generation to handle potential collisions by incrementing the middle part
            // (child profile id in base36) within the same day until a unique identifier is found even in the Deleted state.
            // This is to handle scenarios where multiple claims for the same child profile and date of service might be generated at the same time,
            // which could lead to the same claim identifier being generated if they have the same sequence number.

            var nextSeq = await GetNextClaimSequence(dateOfService, childProfileId);
            string result;

            do
            {
                var random5Digit = Random.Shared.Next(10000, 99999);
                var combinedValue = childProfileId + random5Digit;
                result = $"{dateOfService:yy}{dateOfService:MM}{dateOfService:dd}-" +
                         $"{combinedValue.ToBase36().PadLeft(5, '0')}-" +
                         $"{nextSeq}";

            } while (await BatchIdExists(result));

            return result;
        }

        private async Task<bool> BatchIdExists(string claimIdentifier)
        {
            return await _billingClaimRepository.Query().AnyAsync(c => c.ClaimIdentifier == claimIdentifier);
        }

        public async Task<string> GetNextClaimSequence(DateTime dateOfService, int childProfileId)
        {

            var lastIdentifier = await _billingClaimRepository.Query().Where(clm => clm.DateDeleted == null &&
                                                                               clm.ChildProfileId == childProfileId &&
                                                                               clm.ClaimIdentifier.Substring(0, 6) == dateOfService.ToString("yyMMdd"))
                                                                 .OrderByDescending(clm => clm.ClaimIdentifier)
                                                                 .Select(x => x.ClaimIdentifier)
                                                                 .FirstOrDefaultAsync();

            if (lastIdentifier?.Length == _claimIdentifierLength)
            {
                // grab the last digit to get the sequence
                var sequence = lastIdentifier.Substring(_claimIdentifierLength - 1);
                var seqNum = sequence.FromBase36();
                var nextSeq = seqNum + 1;

                if (nextSeq >= 36)
                    throw new Exception($"Exceeded allowed creation for claim {lastIdentifier}. Only 36 creations are allowed.");
                return nextSeq.ToBase36();
            }

            return "1";
        }

        private async Task<string> GenerateClaimSubmissionIdentifier(ClaimEntity claim)
        {
            var nextSeq = await GetNextClaimSubmissionSequence(claim);

            return $"{claim.ClaimIdentifier}{nextSeq}"; // 0-35 represented in Base 36 (0-9, A-Z)


        }

        private async Task<string> GetNextClaimSubmissionSequence(ClaimEntity claim)
        {

            var lastIdentifier = await _billingClaimSubmissionRepository.Query().Where(clmSub => clmSub.ClaimId == claim.Id &&
                                                                                            clmSub.DateDeleted == null)
                                                                           .OrderByDescending(clmSub => clmSub.ClaimSubmissionIdentifier)
                                                                           .Select(clmSub => clmSub.ClaimSubmissionIdentifier)
                                                                           .FirstOrDefaultAsync();

            if (lastIdentifier?.Length == _claimSubmissionIdentifierLength)
            {
                // grab the last digit to get the sequence
                var sequence = lastIdentifier.Substring(_claimSubmissionIdentifierLength - 1);
                var seqNum = sequence.FromBase36();
                var nextSeq = seqNum + 1;
                if (nextSeq >= 36)
                    throw new Exception($"Exceeded allowed submissions for claim {claim.ClaimIdentifier}. Only 36 submissions are allowed.");
                return nextSeq.ToBase36();
            }

            return "1";
        }

        private async Task<string> GetState(int? stateId)
        {
            if (_states == null)
            {
                _states = await _rethinkServices.GetStateList();
            }
            var state = _states.FirstOrDefault(s => s.id == stateId);
            return state?.abbreviation;
        }

        private async Task<string> GetCountry(int? countryId)
        {
            if (_countries == null)
            {
                _countries = await _rethinkServices.GetCountryList(); //_bhCountryRepository.Query().ToListAsync();
            }
            var country = _countries.FirstOrDefault(s => s.id == countryId);
            return country?.name;
        }

        private string GetGender(int genderId)
        {
            switch (genderId)
            {
                case 1: return "M";
                case 2: return "F";
                default: return "U";
            }
        }
        private int GetGenderId(string gender)
        {
            switch (gender)
            {
                case "M": return 1;
                case "F": return 2;
                default: return 3;
            }
        }

        private async Task MapHcfaAdressAsync(ClaimHFCAModel hcfa, ClaimEntity claim, ClaimSubmissionEntity claimSubmission)
        {
            var billingProviderIsIndividual = hcfa.ServiceLineBillingProviderOption == BillingProviderOptionType.Individual;
            var isIndividualAndGroup = hcfa.ServiceLineBillingProviderOption == BillingProviderOptionType.GroupAndIndividual;

            if (billingProviderIsIndividual)
            {
                var accountInfo = await _rethinkServices.GetAccountReturningEntityAsync(claim.AccountInfoId);
                var isInternationalAccount = accountInfo != null && (accountInfo.IsInternational ?? false);
                var serviceLocationIsBillingLocation = claim.ServiceLocation.isBillingLocation;

                var mainLocationList = await _rethinkServices.GetProviderLocationList(claim.AccountInfoId);
                var mainLoc = mainLocationList.data.Where(x => x.isMainLocation == true).FirstOrDefault();

                var mainLocation = !serviceLocationIsBillingLocation ? mainLoc : claim.ServiceLocation;

                hcfa.AccountName = hcfa.RenderingProviderName;
                hcfa.AccountAddress1 = serviceLocationIsBillingLocation ? claimSubmission.ServiceLocationAddress1 : mainLocation?.address.street1;
                hcfa.AccountCity = serviceLocationIsBillingLocation ? claimSubmission.ServiceLocationCity : mainLocation?.address.city;

                var countryname = await GetCountry(mainLocation.address.countryId);
                var statename = await GetState(mainLocation.address.stateId);

                hcfa.AccountState = isInternationalAccount
                    ? serviceLocationIsBillingLocation ? claimSubmission.ServiceLocationCountry : countryname
                    : serviceLocationIsBillingLocation ? claimSubmission.ServiceLocationState : statename;

                hcfa.AccountZip = serviceLocationIsBillingLocation ? claimSubmission.ServiceLocationZip : mainLocation?.address.zip;
                hcfa.AccountPhone = serviceLocationIsBillingLocation ? claim.ServiceLocation.phone : mainLocation?.phone;
                hcfa.AccountNPI = claimSubmission.RenderingProviderStaffNpiNumber;
            }
            else
            {
                hcfa.AccountName = claimSubmission.AccountBillingProviderName;
                hcfa.AccountAddress1 = claimSubmission.AccountBillingAddress1;
                hcfa.AccountCity = claimSubmission.AccountBillingCity ?? claimSubmission.AccountBillingTown;
                hcfa.AccountState = claimSubmission.AccountBillingState;
                hcfa.AccountZip = claimSubmission.AccountBillingZip;
                hcfa.AccountPhone = claimSubmission.AccountPhoneNumber;
                hcfa.AccountNPI = isIndividualAndGroup ? claimSubmission.ResolvedBillingProviderNpi : claimSubmission.AccountBillingProviderTaxonomyCode;
            }
        }

        public async Task<ChildProfileInfo> GetPatientInfoById(int patientId, int accountInfoId)
        {
            try
            {
                var clients = await _rethinkServices.GetChildProfileReturningEntity(accountInfoId, patientId);
                var clientDetails = await _rethinkServices.GetClientDetails(clients.AccountInfoId, patientId);
                var funderMappings = await _rethinkServices.GetChildProfileFunderMappings(clients.AccountInfoId, patientId);
                /*var serviceIntesityType = serviceIntesityTypeId.serviceIntensityTypeId == 1 ? "Intensive" : "Non-Intensive";*/
                var primaryPolicies = new List<string>();
                var secondaryPolicies = new List<string>();
                var InsuranceContacts = await _rethinkServices.GetInsuranceContactsIds(clients.AccountInfoId, patientId);
                foreach (var item in InsuranceContacts.data)
                {
                    var insuranceTypeId = await _rethinkServices.GetInsuranceContactsType(clients.AccountInfoId, patientId, item.Id);
                    if (insuranceTypeId.insuranceTypeId != null)
                    {
                        var funderId = funderMappings.data.FirstOrDefault(x => x.childProfileInsuranceContactId == item.Id).funderId;
                        var funders = await _rethinkServices.GetFunder(clients.AccountInfoId, funderId);
                        var funderName = funders.funderName;
                        if (insuranceTypeId.insuranceTypeId == 1)
                        {
                            primaryPolicies.Add(funderName);
                        }
                        else if (insuranceTypeId.insuranceTypeId == 2)
                        {
                            secondaryPolicies.Add(funderName);
                        }
                    }
                }
                var location = await _rethinkServices.GetProviderLocationName(clients.AccountInfoId, patientId);
                var primary = string.Join(", ", primaryPolicies.ToArray());
                var secondary = string.Join(", ", secondaryPolicies.ToArray());

                var client = new ChildProfileInfo
                {
                    PatientId = clients.Id,
                    PatientName = $"{clients.FirstName} {clients.MiddleName} {clients.LastName}",
                    DateOfBirth = clients.DateOfBirth,
                    Age = ClientDemographics.CalculateAge(clients.DateOfBirth),
                    Gender = clients.GenderId == 1 ? "Male" : "Female",
                    Uci = clients.UCI,
                    ServiceIntensity = ((ServiceIntensityTypes)clientDetails.serviceIntensityTypeId).ToString(),
                    Location = location,
                    PrimaryPolicy = !(primary.IsNullOrEmpty()) ? primary : "",
                    SecondaryPolicy = !(secondary.IsNullOrEmpty()) ? secondary : "",
                    Address = $"{clients.Address}\n{clients.City}, {clients.Town} {clients?.StateLU?.name} {clients.ZipCode}\n{clients?.CountryLU?.name}"
                };

                return client;
            }
            catch (Exception)
            {
                return null;
            }

        }

        private string GetProviderName(ClaimBillingProviderEntity provider)
        {
            if (provider == null) return string.Empty;

            return provider.ProviderType switch
            {
                var type when type == BillingProviderOptionType.Person.ToString()
                    => $"{provider.FirstName} {provider.LastNameOrFacilityName}".Trim(),

                var type when type == BillingProviderOptionType.Entity.ToString()
                    => provider.LastNameOrFacilityName ?? "",

                _ => ""
            };
        }
    }
}