using Billing.FolderStructure.Core.Enum;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Quartz.Util;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Helpers;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Models.ClientMicroServicesModels;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class ClaimValidationService : BaseService, IClaimValidationService
    {
        #region Variables

        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        private readonly IClaimHistoryService _claimHistoryService;
        private readonly string _customerId;
        private readonly int minLengthInsuredId = 2;
        private readonly int maxLengthInsuredId = 80;

        private readonly IRepository<BillingDbContext, ClaimEntity> _billingClaimRepository;
        private readonly IRepository<BillingDbContext, ClaimSubmissionEntity> _billingClaimSubmissionRepository;
        private readonly IRepository<BillingDbContext, ClaimValidationErrorEntity> _billingClaimValidationErrorRepository;
        private readonly IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity> _billingClaimSubmissionServiceLineRepository;
        private readonly IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity> _billingClaimSubmissionFunderSequenceRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimEntity> _billingPaymentClaimRepository;
        private readonly IRepository<BillingDbContext, ClaimErrorMessageEntity> _billingClaimErrorMessageRepository;
        private readonly IStediProviderEnrollmentService _stediProviderEnrollmentService;
        private readonly IClearinghouseCredentialValidationService _clearinghouseCredentialValidationService;
        private readonly IFeatureFlagService _featureFlagService;
        private readonly ILogger<ClaimValidationService> _logger;

        private List<StateModel> _states;
        private List<CountryModel> _countries;
        private static readonly DateTime? _validDateCheck = new DateTime(1900, 1, 1);
        private static readonly DateTime? _endOfTimeDate = new DateTime(2099, 12, 31);

        public ClaimValidationService(IConfiguration configuration,
            IRepository<BillingDbContext, ClaimEntity> billingClaimRepository,
            IRepository<BillingDbContext, ClaimSubmissionEntity> billingClaimSubmissionRepository,
            IRepository<BillingDbContext, ClaimValidationErrorEntity> billingClaimValidationErrorRepository,
            IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity> billingClaimSubmissionServiceLineRepository,
            IRepository<BillingDbContext, ClaimSubmissionFunderSequenceEntity> billingClaimSubmissionFunderSequenceRepository,
            IRepository<BillingDbContext, PaymentClaimEntity> billingPaymentClaimRepository,
            IRepository<BillingDbContext, ClaimErrorMessageEntity> billingClaimErrorMessageRepository,
            IClaimHistoryService claimHistoryService,
            IRethinkMasterDataMicroServices rethinkServices,
            IStediProviderEnrollmentService stediProviderEnrollmentService,
            IClearinghouseCredentialValidationService clearinghouseCredentialValidationService,
            IFeatureFlagService featureFlagService,
            ILogger<ClaimValidationService> logger
            )
        {
            _billingClaimRepository = billingClaimRepository;
            _billingClaimSubmissionRepository = billingClaimSubmissionRepository;
            _billingClaimValidationErrorRepository = billingClaimValidationErrorRepository;
            _billingClaimSubmissionServiceLineRepository = billingClaimSubmissionServiceLineRepository;
            _billingClaimSubmissionFunderSequenceRepository = billingClaimSubmissionFunderSequenceRepository;
            _billingPaymentClaimRepository = billingPaymentClaimRepository;
            _billingClaimErrorMessageRepository = billingClaimErrorMessageRepository;
            _claimHistoryService = claimHistoryService;
            _rethinkServices = rethinkServices;
            _stediProviderEnrollmentService = stediProviderEnrollmentService;  
            _customerId = configuration["EdiSettings:CustomerId"];
            _clearinghouseCredentialValidationService = clearinghouseCredentialValidationService;
            _featureFlagService = featureFlagService;
            _logger= logger;
        }
        #endregion

        #region Validation Models

        public class ClaimValidationError
        {
            public ClaimValidationError(string message,
                                        ClaimErrorMessageEntity claimErrorMessage,
                                        Exception exception = null)
            {
                Message = message;
                ClaimErrorMessage = claimErrorMessage;
                Exception = exception;
            }

            public string Message { get; set; }

            public ClaimErrorMessageEntity ClaimErrorMessage { get; set; }

            public Exception Exception { get; set; }
        }


        public class ValidationErrorList
        {
            private readonly List<ClaimErrorMessageEntity> _claimErrorMessages;

            public ValidationErrorList(List<ClaimErrorMessageEntity> claimErrorMessages)
            {
                _claimErrorMessages = claimErrorMessages;
            }

            public List<ClaimValidationError> Errors { get; } = new List<ClaimValidationError>();

            public bool HasErrors
            {
                get
                {
                    return (int)MaxSeverity <= (int)ClaimErrorSeverity.Error;
                }
            }

            public ClaimErrorSeverity MaxSeverity
            {
                get
                {
                    if (Errors.Any())
                    {
                        return (ClaimErrorSeverity)Errors.Max(e => (int)(e.ClaimErrorMessage?.Severity ?? ClaimErrorSeverity.Unknown));
                    }
                    return ClaimErrorSeverity.NoError;
                }
            }

            public void AddError(ClaimErrorNumber errorNum, string message, Exception exception = null)
            {
                var claimErrorMessage = _claimErrorMessages.FirstOrDefault(cem => cem.ErrorNumber == errorNum) ??
                                        _claimErrorMessages.First(cem => cem.ErrorNumber == ClaimErrorNumber.Unknown);

                var error = new ClaimValidationError((message == null && claimErrorMessage.ErrorNumber == ClaimErrorNumber.Unknown) ? errorNum.ToString() : message, claimErrorMessage, exception);
                Errors.Add(error);
            }
        }

        private class ClaimSubmissionValidationResult
        {
            public ClaimSubmissionValidationResult(ValidationErrorList errors,
                                                   ClaimSubmissionEntity claimSubmission,
                                                   List<ClaimSubmissionServiceLineEntity> serviceLines,
                                                   List<ClaimSubmissionFunderSequenceEntity> funderSequences)
            {
                Errors = errors;
                ClaimSubmission = claimSubmission ?? new ClaimSubmissionEntity();
                ServiceLines = serviceLines ?? new List<ClaimSubmissionServiceLineEntity>();
                FunderSequences = funderSequences ?? new List<ClaimSubmissionFunderSequenceEntity>();
            }
            public ClaimSubmissionEntity ClaimSubmission { get; private set; }
            public List<ClaimSubmissionServiceLineEntity> ServiceLines { get; private set; }
            public List<ClaimSubmissionFunderSequenceEntity> FunderSequences { get; private set; }
            public ValidationErrorList Errors { get; }

        }

        private class RenderingStaffMemberInfo
        {
            public RethinkStaffMember StaffMember { get; set; }
            public PropagatingStaffMember PropagatingStaffMember { get; set; }

            public string StaffProviderFirstName => PropagatingStaffMember?.firstName ?? StaffMember?.name?.firstName;
            public string StaffProviderLastName => PropagatingStaffMember?.lastName ?? StaffMember?.name?.lastName;
            public string StaffProviderMiddleName => PropagatingStaffMember?.middleName ?? StaffMember?.name?.middleName;
            public string StaffProviderNPINumber => PropagatingStaffMember?.npiNumber ?? StaffMember?.identifiers?.FirstOrDefault(x => x.identifierType.ToLower() == "npinumber")?.value;
            public string StaffProviderTaxonomyCode => StaffMember?.taxonomyCode;
        }

        private class DiagnosisCode
        {
            public DiagnosisCode(string dxCode, DiagnosisTypes dxType)
            {
                Code = dxCode;
                DiagnosisType = dxType;
            }

            public string Code { get; }
            public DiagnosisTypes DiagnosisType { get; }

        }

        private class DiagnosisCodeOrder
        {
            public DiagnosisCodeOrder(ClaimChargeEntryEntity claimCharge,
                                      DiagnosisCode dxCode,
                                      DateTime? inactiveDate,
                                      int order)
            {
                ClaimCharge = claimCharge;
                DiagnosisCode = dxCode;
                Order = order;



            }


            public ClaimChargeEntryEntity ClaimCharge { get; }
            public DiagnosisCode DiagnosisCode { get; }

            public DateTime? InactiveDate { get; }

            public int Order { get; }
        }

        private class ClaimSubmissionData
        {
            public ClaimSubmissionData(ResponsibilitySequenceType responsibilitySequence)
            {
                ResponsibilitySequenceCurrent = responsibilitySequence;
            }
            public ClaimEntity Claim { get; set; }
            public ClaimSubmissionEntity ClaimSubmission { get; set; }
            public AccountInfoEntityModel AccountInfo { get; set; }
            public ClearingHouseDataModel ClearingHouse { get; set; }
            public ChildProfileEntityModel ChildProfile { get; set; }
            public ClientAuthorization Authorization { get; set; }
            public int AuthorizationType { get; set; }
            public clientReferringProviders ReferringProvider { get; set; }
            public LocationCodesModel LocationCode { get; set; }
            public ProviderLocations ProviderLocation { get; set; }
            public ProviderLocations ServiceFacilityLocation { get; set; }
            public ClientAddress ProviderLocationAddress { get; set; }
            public PaymentClaimEntity PriorFunderLatestClaimPayment { get; set; }
            public List<ClaimChargeEntryEntity> ChargeEntries { get; set; }
            public int AppointmentCount { get; set; }
            public RenderingStaffMemberInfo RenderingStaffMemberInfo { get; set; }
            public List<ServiceLines> FunderServiceLineMappings { get; set; }
            public decimal? PatientPaid { get; set; }
            public List<DiagnosisCodeOrder> DiagnosisCodes { get; set; }

            //-----------------------------------------------------
            // calculated properties
            //-----------------------------------------------------
            public ResponsibilitySequenceType ResponsibilitySequenceCurrent { get; set; }

            public FunderDetails FunderMappingCurrent
            {
                get
                {
                    return GetFunderMappingForResponsibilitySequence(ResponsibilitySequenceCurrent);
                }

            }
            public FunderDetails GetFunderMappingForResponsibilitySequence(ResponsibilitySequenceType responsibilitySequence)
            {
                return FunderServiceLineMappings.FirstOrDefault(slm => slm.responsibilitySequence == responsibilitySequence)?.ChildProfileFunderMapping;
            }

            public DateTime? EarliestDOS
            {
                get
                {
                    return ChargeEntries != null && ChargeEntries.Count > 0 ? ChargeEntries?.Min(ce => ce.DateOfService) : null;
                }
            }
            public DateTime? LatestDOS
            {
                get
                {
                    return ChargeEntries != null && ChargeEntries.Count > 0 ? ChargeEntries?.Max(ce => ce.DateOfService) : null;
                }
            }
        }
        #endregion

        public async Task ValidateClaimData(int claimId, int memberId, ClaimEntity claim, ResponsibilitySequenceType responsibilitySequence = ResponsibilitySequenceType.Primary, bool isSaveSubmission = false, int? secondaryFunderId = null)
        {
            var isPriorClaimSubmission = false;
            var data = await FetchClaimSubmissionData(claimId, claim, isSaveSubmission, responsibilitySequence, secondaryFunderId);
            if (data == null)
            {
                return;
            }
            if (isSaveSubmission && data.ClaimSubmission != null) isPriorClaimSubmission = true;
            var validationResult = await ValidateClaimSubmissionData(data,
                                                                    data.ClaimSubmission,
                                                                    ClaimFrequencyType.Original,
                                                                    ClaimSubmissionType.Original,
                                                                    ClaimDocumentType.Doc837P,
                                                                    responsibilitySequence,
                                                                    false,
                                                                    memberId);


            if (validationResult.Errors.Errors.Count == 0)
                await AddClaimHistory(data.Claim, ClaimAction.ScrubbingRules, ClaimHistoryAction.ScrubbingRulesApplied);
            else
                await AddClaimHistory(data.Claim, ClaimAction.ScrubbingRules, ClaimHistoryAction.ScrubbingErrorsAndAlertsFound, $"{validationResult.Errors.Errors.Count}");

            if (isPriorClaimSubmission) await UpdateClaimSubmissionEntities(validationResult, isSaveSubmission);
            await SaveClaimErrors(validationResult, memberId, data.Claim, null);
        }

        #region FetchClaimSubmissionData
        private async Task<ClaimSubmissionData> FetchClaimSubmissionData(int claimId, ClaimEntity existingClaimData, bool isSaveSubmission, ResponsibilitySequenceType responsibilitySequence, int? secondaryFunderId = null)
        {
            var claim = existingClaimData != null ? existingClaimData : await GetClaimInformation(claimId);
            if (claim == null)
            {
                return null;
            }
            var submission = await GetClaimSubmissionInformation(claimId);
            var diagnosisOrder = await GetDiagnosisInformation(claim);

            return await CreateClaimSubmissionData(claim, submission, diagnosisOrder, isSaveSubmission, responsibilitySequence, secondaryFunderId);
        }

        public async Task<ClaimEntity> GetClaimInformation(int claimId)
        {
            var claim = await _billingClaimRepository.Query()
                .Include(cs => cs.ClaimValidationErrors)
                    .ThenInclude(err => err.ClaimErrorMessage)
                .Include(c => c.ClaimChargeEntries)
                .Include(c => c.ClaimDiagnosisCodes)
                .Include(c => c.ClaimSubmissions)
                .Include(c => c.ClaimHistory)
                .FirstOrDefaultAsync(c => c.Id == claimId && c.DateDeleted == null);

            if (claim == null)
            {
                return null;
            }

            var isClaimManualCreated = claim.ClaimHistory.Any(x =>
                x.ClaimHistoryAction == ClaimHistoryAction.ClaimCreated &&
                x.Mode == ClaimActionMode.User);

            if (!isClaimManualCreated)
            {
                claim = await _billingClaimRepository.Query()
                    .Include(c => c.ClaimAppointmentLinks)
                    .FirstOrDefaultAsync(c => c.Id == claimId && c.DateDeleted == null);
            }

            var childProfile = _rethinkServices.GetChildProfileReturningEntity(claim.AccountInfoId, claim.ChildProfileId);
            var accountInfo = _rethinkServices.GetAccountReturningEntityAsync(claim.AccountInfoId, true);
            var auth = claim.AuthorizationId.HasValue ? _rethinkServices.GetChildProfileAuthorizationByClientId(claim.AccountInfoId, claim.ChildProfileId, claim.AuthorizationId.Value) : null;
            var referringProvider = claim.ChildProfileReferringProviderId.HasValue ? _rethinkServices.GetChildProfileReferringProviderEntity(claim.AccountInfoId, claim.ChildProfileId, claim.ChildProfileReferringProviderId ?? 0) : null;
            var providerLocation = claim.ProviderLocationId.HasValue ? _rethinkServices.GetProviderLocation(claim.AccountInfoId, claim.ProviderLocationId.Value) : null;
            var serviceLocation = claim.ServiceLocationId.HasValue ? _rethinkServices.GetProviderLocation(claim.AccountInfoId, claim.ServiceLocationId.Value) : null;
            var renderingStaffMember = claim.RenderingStaffMemberId.HasValue ? _rethinkServices.GetMemberAsync(claim.AccountInfoId, claim.RenderingStaffMemberId.Value) : null;

            var locationCodes = _rethinkServices.GetLocationCodes();

            await Task.WhenAll(new Task?[]
            { childProfile, accountInfo, referringProvider, providerLocation, serviceLocation, auth, locationCodes, renderingStaffMember }
            .Where(t => t is not null).Cast<Task>());

            claim.ChildProfile = childProfile != null ? await childProfile : null;
            claim.AccountInfo = accountInfo != null ? await accountInfo : null;
            claim.ServiceLocation = serviceLocation != null ? await serviceLocation : null;
            claim.ReferringProvider = referringProvider != null ? await referringProvider : null;
            claim.ProviderLocation = providerLocation != null ? await providerLocation : null;
            var locarionCodes = locationCodes != null ? await locationCodes : null;
            claim.ChildProfileAuthorization = auth != null ? await auth : null;
            claim.RenderingStaffMember = renderingStaffMember != null ? await renderingStaffMember : null;
            claim.LocationCode = locarionCodes.FirstOrDefault(x => x.id == claim.LocationCodeId);

            if (claim.ChildProfileAuthorization != null)
            {
                claim.ChildProfileAuthorization.ServiceFacilityLocation = await _rethinkServices.GetProviderLocation(claim.AccountInfoId, claim.ChildProfileAuthorization.providerServiceId);
            }
            return claim;
        }

        public async Task<ClaimSubmissionEntity> GetClaimSubmissionInformation(int claimId)
        {
            var query = await _billingClaimSubmissionRepository.Query().AsNoTracking()
                                                                       .Include(cs => cs.Claim)
                                                                       .Include(cs => cs.ClaimSubmissionFunderSequences)
                                                                       .Include(cs => cs.ClaimSubmissionServiceLines)
                                                                       .Where(cs => cs.DateDeleted == null
                                                                       && cs.ClaimId == claimId)
                                                                       .OrderByDescending(cs => cs.Id).FirstOrDefaultAsync();
            return query;
        }

        private async Task<List<DiagnosisCodeOrder>> GetDiagnosisInformation(ClaimEntity claim)
        {
            List<DiagnosisCodeOrder> diagnosisCodeOrder = null;
            var authorization = claim.ChildProfileAuthorization;

            if (authorization != null)
            {
                List<Diagnosis> authDiagCodeOrder = null;
                authDiagCodeOrder = authorization?.ChildProfileAuthorizationDiagnosisCodes != null ? authorization?.ChildProfileAuthorizationDiagnosisCodes.Where(cpdc => cpdc.includeOnClaims &&
                                                                                                            cpdc?.metaData?.deletedOn == null).OrderBy(cpdc => cpdc.order)
                                                                                                            .Select(x => x.Diagnosis)
                                                                                             .ToList() : null;
                if (claim.ClaimChargeEntries != null)
                {
                    diagnosisCodeOrder = GetDiagnosisCodeOrder(claim.ClaimChargeEntries.ToList(), authDiagCodeOrder, claim.AccountInfoId);
                }

                if (claim.ProviderLocation == null)
                {
                    claim.ProviderLocation = authorization.BillingProvider;
                }
            }
            if ((diagnosisCodeOrder != null && authorization != null)
                || authorization == null)
            {
                foreach (var claimDiagnos in claim.ClaimDiagnosisCodes)
                {
                    if (claimDiagnos.DateDeleted == null)
                    {
                        claimDiagnos.Diagnosis = await _rethinkServices.GetClientDiagnosisByIdReturningEntityAsync(claimDiagnos.DiagnosisId);
                    }
                }

                var noAuthDiagCodeOrder = claim.ClaimDiagnosisCodes.Where(cpdc => cpdc.IncludeOnClaims &&
                                                                                  cpdc.DateDeleted == null)
                                                                    .OrderBy(cpdc => cpdc.Order)
                                                                    .Select(cpdc => cpdc.Diagnosis)
                                                                    .ToList();

                var childProfileAuthorizationCodes = noAuthDiagCodeOrder
                    .Select(x => new Diagnosis
                    {
                        name = x.Name,
                        pos = (int)x.Pos,
                        diagnosisCode = x.DiagnosisCode,
                        description = x.Description,
                        diagnosisTypeId = x.TypeId,
                        id = x.Id,
                        metaData = new MetaData
                        {
                            deletedBy = x.DeletedBy,
                            deletedOn = x.DateDeleted,
                            modifiedBy = x.ModifiedBy,
                            modifiedOn = x.DateLastModified,
                            createdBy = x.CreatedBy,
                            createdOn = x.DateCreated
                        }
                    }).ToList();

                diagnosisCodeOrder = GetDiagnosisCodeOrder(claim.ClaimChargeEntries.ToList(), childProfileAuthorizationCodes, claim.AccountInfoId);
            }
            return diagnosisCodeOrder;
        }

        private async Task<ClaimSubmissionData> CreateClaimSubmissionData(ClaimEntity claim,
                                                            ClaimSubmissionEntity submission,
                                                            List<DiagnosisCodeOrder> diagnosisCodeOrder,
                                                            bool isSaveSubmission,
                                                            ResponsibilitySequenceType responsibilitySequence,
                                                            int? secondaryFunderId)
        {
            decimal patientPaid = 0;
            PaymentClaimEntity latestPayment = null;

            #region "Ordered Funder Mappings"
            var clientFunderId = secondaryFunderId.HasValue ? secondaryFunderId.Value : claim.ClientFunderId ?? claim.ChildProfileAuthorization.ChildProfileFunderServiceLineMapping.ChildProfileFunderMappingId;

            var orderedFunderMappings = await _rethinkServices.GetServiceLineMappingsByFunderId(claim.AccountInfoId, claim.ChildProfileId, clientFunderId);
            orderedFunderMappings = orderedFunderMappings?.Where(x => x.metaData.deletedOn == null && x.metaData.deletedBy == null).ToList() ?? null;
            if (orderedFunderMappings == null)
            {
                throw new Exception("No funder details found");
            }
            foreach (var item in orderedFunderMappings)
            {
                item.ChildProfileFunderMapping = await _rethinkServices.GetChildProfileFunderMappingByMappingId(claim.AccountInfoId, claim.ChildProfileId, item.ChildProfileFunderMappingId);
                item.ChildProfileFunderMapping.Funder = await _rethinkServices.GetFunder(claim.AccountInfoId, item.ChildProfileFunderMapping.funderId);
            }
            #endregion

            #region Rendering Provider
            var staff = await _rethinkServices.GetStaffMember(claim.AccountInfoId, claim.RenderingProviderTypeId ?? 0);
            if (staff == null || !claim.RenderingProviderTypeId.HasValue || (claim.RenderingProviderTypeId.HasValue && staff?.Member == null))
            {
                var renderingProviders = await _rethinkServices.GetAllRenderingProvidersAsync(claim.AccountInfoId);
                // assigning the rendering provider type id to remove RP missing issue
                if (renderingProviders == null)
                {
                    throw new NullReferenceException("Claim approval Failed — Rendering Provider missing.");
                }

                claim.RenderingProviderTypeId = renderingProviders.data.FirstOrDefault(x => x.memberId == claim.RenderingStaffMemberId)?.id;
                if (claim.RenderingProviderTypeId.HasValue)
                {
                    staff = await _rethinkServices.GetStaffMember(claim.AccountInfoId, (int)claim.RenderingProviderTypeId);
                }
            }

            RenderingStaffMemberInfo renderingStaffMemberInfo = new RenderingStaffMemberInfo()
            {
                StaffMember = staff,
                PropagatingStaffMember = null
            };
            #endregion

            if (isSaveSubmission)
            {
                #region "Patient Paid"
                var paymentsQuery = _billingPaymentClaimRepository.Query()
                    .Include(pc => pc.Payment)
                    .Where(pc => pc.ClaimId == claim.Id &&
                                 pc.Payment.HcFunderId != null &&
                                 pc.DateDeleted == null);

                var funderIds = paymentsQuery.Select(x => x.Payment.HcFunderId).Distinct().ToList();
                //Commented and added below change for Bug- 218640
                //var selfPayFunders = await _rethinkServices.GetFunderList(claim.AccountInfoId);

                var selfPayFunders = new List<FunderDataModel>();
                foreach (int funderId in funderIds)
                {
                    var selfPayFun = await _rethinkServices.GetFunder(claim.AccountInfoId, funderId);
                    if (selfPayFun != null)
                    {
                        selfPayFunders.Add(selfPayFun);
                    }
                }
                //end
                var selfPayFundersIds = selfPayFunders.Where(x => funderIds.Contains(x.id) &&
                                x.funderTypeId == (int)FunderType.SelfPay &&
                                x.metaData.deletedOn == null)
                    .Select(x => x.id).ToList();

                patientPaid = await paymentsQuery.Where(x => selfPayFundersIds.Contains(x.Payment.HcFunderId.Value))
                    .SumAsync(pc => pc.TotalCharge) ?? 0;
                #endregion

                #region "PriorFunderLatestClaimPayment"
                // we have to sort the list after the query because of the .AsOrdinal() method call - it can't be used in LINQ
                orderedFunderMappings = orderedFunderMappings.OrderBy(slm => slm.responsibilitySequence.AsOrdinal()).ToList();

                var referringProvider = claim.ReferringProvider; //?? authorization?.ChildProfileReferringProvider;

                var currentFunderMapping = orderedFunderMappings.FirstOrDefault(slm => slm.responsibilitySequence == responsibilitySequence)?.ChildProfileFunderMapping;
                FunderDataModel priorFunder = null;
                ResponsibilitySequenceType priorSequence;
                if (responsibilitySequence != ResponsibilitySequenceType.Primary)
                {
                    priorSequence = DecrementResponsibilitySequence(responsibilitySequence);
                    priorFunder = orderedFunderMappings.FirstOrDefault(slm => slm.responsibilitySequence == priorSequence)?.ChildProfileFunderMapping.Funder;
                }

                if (priorFunder != null)
                {
                    var priorFunderPayments = await _billingPaymentClaimRepository.Query()
                        .Where(pc => pc.ClaimId == claim.Id &&
                                     pc.DateDeleted == null &&
                                     pc.Payment.HcFunderId == priorFunder.id)
                        .Include(pc => pc.Payment)
                        .OrderByDescending(pc => pc.Payment.PaymentDate)
                        .ToListAsync();
                    latestPayment = priorFunderPayments.FirstOrDefault();
                }
                #endregion
            }

            return new ClaimSubmissionData(responsibilitySequence)
            {
                Claim = claim,
                ChargeEntries = claim.ClaimChargeEntries.Where(x => x.DateDeleted == null).ToList(),
                ClaimSubmission = submission,
                AccountInfo = claim.AccountInfo,
                ClearingHouse = claim.AccountInfo.ClearingHouse,
                ChildProfile = claim.ChildProfile,
                Authorization = claim.ChildProfileAuthorization,
                ReferringProvider = claim.ReferringProvider,
                FunderServiceLineMappings = orderedFunderMappings,
                LocationCode = claim.LocationCode,
                ProviderLocation = claim.ProviderLocation,
                AppointmentCount = claim.ClaimAppointmentLinks.Count(),
                ServiceFacilityLocation = claim.ServiceLocation ?? claim.ChildProfileAuthorization?.ServiceFacilityLocation,
                ProviderLocationAddress = claim.ProviderLocation != null ? new ClientAddress
                {
                    Id = claim.ProviderLocation.address.id,
                    street1 = claim.ProviderLocation.address.street1,
                    street2 = claim.ProviderLocation.address.street2,
                    city = claim.ProviderLocation.address.city,
                    town = claim.ProviderLocation.address.town,
                    stateId = claim.ProviderLocation.address.stateId,
                    zipCode = claim.ProviderLocation.address.zip,
                    countryId = claim.ProviderLocation.address.countryId,
                } : new(),
                DiagnosisCodes = diagnosisCodeOrder,
                PatientPaid = patientPaid,
                PriorFunderLatestClaimPayment = latestPayment,
                RenderingStaffMemberInfo = renderingStaffMemberInfo
            };
        }
        #endregion

        #region Update Submissions & Errors
        private async Task UpdateClaimSubmissionEntities(ClaimSubmissionValidationResult validationResult, bool isSaveSubmission)
        {
            var claimsubmission = validationResult.ClaimSubmission;
            claimsubmission.ClaimSubmissionServiceLines = validationResult.ServiceLines;
            claimsubmission.ClaimSubmissionFunderSequences = validationResult.FunderSequences;
            if (isSaveSubmission && claimsubmission != null)
            {
                _billingClaimSubmissionRepository.Entry(claimsubmission).Context.ChangeTracker.Clear();
                _billingClaimSubmissionRepository.Entry(claimsubmission).State = EntityState.Modified;
                if (claimsubmission.Id > 0)
                {
                    _billingClaimSubmissionRepository.Update(claimsubmission);
                    await _billingClaimSubmissionRepository.SaveChangesAsync();
                    foreach (var item in claimsubmission.ClaimSubmissionServiceLines)
                    {
                        _billingClaimSubmissionServiceLineRepository.Entry(item).Context.ChangeTracker.Clear();
                        _billingClaimSubmissionServiceLineRepository.Entry(item).State = EntityState.Modified;
                        if (item.Id > 0) _billingClaimSubmissionServiceLineRepository.Update(item);
                        else _billingClaimSubmissionServiceLineRepository.Add(item);
                        await _billingClaimSubmissionServiceLineRepository.SaveChangesAsync();
                    }
                    foreach (var item in claimsubmission.ClaimSubmissionFunderSequences)
                    {
                        _billingClaimSubmissionFunderSequenceRepository.Entry(item).Context.ChangeTracker.Clear();
                        _billingClaimSubmissionFunderSequenceRepository.Entry(item).State = EntityState.Modified;
                        if (item.Id > 0) _billingClaimSubmissionFunderSequenceRepository.Update(item);
                        else _billingClaimSubmissionFunderSequenceRepository.Add(item);
                        await _billingClaimSubmissionFunderSequenceRepository.SaveChangesAsync();
                    }
                }
                else
                {
                    _billingClaimSubmissionRepository.Add(claimsubmission);
                    await _billingClaimSubmissionRepository.SaveChangesAsync();
                    foreach (var item in claimsubmission.ClaimSubmissionServiceLines)
                    {
                        item.Id = 0;
                        item.ClaimSubmissionId = claimsubmission.Id;
                        item.ClaimChargeEntry = null;
                        _billingClaimSubmissionServiceLineRepository.Entry(item).Context.ChangeTracker.Clear();
                        _billingClaimSubmissionServiceLineRepository.Entry(item).State = EntityState.Added;
                        _billingClaimSubmissionServiceLineRepository.Add(item);
                        await _billingClaimSubmissionServiceLineRepository.SaveChangesAsync();
                    }
                    foreach (var item in claimsubmission.ClaimSubmissionFunderSequences)
                    {
                        item.Id = 0;
                        item.ClaimSubmissionId = claimsubmission.Id;
                        _billingClaimSubmissionFunderSequenceRepository.Entry(item).Context.ChangeTracker.Clear();
                        _billingClaimSubmissionFunderSequenceRepository.Entry(item).State = EntityState.Added;
                        _billingClaimSubmissionFunderSequenceRepository.Add(item);
                        await _billingClaimSubmissionFunderSequenceRepository.SaveChangesAsync();
                    }
                }
                await _billingClaimSubmissionRepository.CommitAsync();
                await _billingClaimSubmissionServiceLineRepository.CommitAsync();
                await _billingClaimSubmissionFunderSequenceRepository.CommitAsync();
            }
        }

        /// <summary>
        /// Supply EITHER the claim OR the claimSubmission, not both
        /// </summary>
        /// <returns></returns>
        private async Task SaveClaimErrors(ClaimSubmissionValidationResult validationResult,
                                           int memberId,
                                           ClaimEntity claim = null,
                                           ClaimSubmissionEntity claimSubmission = null)
        {
            var errors = new List<ClaimValidationErrorEntity>();
            foreach (var error in validationResult.Errors.Errors)
            {
                var newError = new ClaimValidationErrorEntity()
                {
                    ClaimSubmissionId = claimSubmission?.Id,
                    ClaimId = claimSubmission != null ? claimSubmission.ClaimId : claim?.Id,
                    ClaimErrorMessageId = error.ClaimErrorMessage.Id,
                    ClaimErrorSource = ClaimErrorSource.Billing,
                    ContextMessage = error.Message,
                    ValidationDate = EstDateTime
                };
                MarkCreated(newError, memberId);
                errors.Add(newError);
            }

            var toDelete = await _billingClaimValidationErrorRepository.Query()
                    .Where(err => err.ClaimId == (claimSubmission != null ? claimSubmission.ClaimId : claim.Id) &&
                                  err.DateDeleted == null && err.ClaimErrorSource != ClaimErrorSource.Era)
                    .ToListAsync();
            _billingClaimValidationErrorRepository.RemoveRange(toDelete);
            await _billingClaimValidationErrorRepository.AddRangeAsync(errors);
            await _billingClaimValidationErrorRepository.CommitAsync();
        }

        #endregion

        #region PrepareClaimSubmission
        public async Task PrepareClaimSubmission(ClaimEntity claim,
            ClaimSubmissionEntity claimSubmission,
            ClaimSubmissionEntity priorClaimSubmission, int submittingMemberId, int? secondaryFunderId = null)
        {
            var responsibilitySequence = ResponsibilitySequenceTypeHelper.FromString(claimSubmission.ResponsibilitySequence);
            var priorClaimSubmissionExists = priorClaimSubmission != null;
            var validationResult = new ClaimSubmissionValidationResult(null, claimSubmission, new List<ClaimSubmissionServiceLineEntity>(), new List<ClaimSubmissionFunderSequenceEntity>());
            var data = await FetchClaimSubmissionData(claim.Id, claim, true, responsibilitySequence, secondaryFunderId);
            validationResult = await ValidateClaimSubmissionData(data,
                                                                 claimSubmission,
                                                                 claimSubmission.FrequencyType,
                                                                 claimSubmission.SubmissionType,
                                                                 claimSubmission.DocumentType,
                                                                 responsibilitySequence,
                                                                 priorClaimSubmissionExists,
                                                                 submittingMemberId);
            if (validationResult.Errors.Errors.Count == 0)
                await AddClaimHistory(data.Claim, ClaimAction.ScrubbingRules, ClaimHistoryAction.ScrubbingRulesApplied);
            else
                await AddClaimHistory(data.Claim, ClaimAction.ScrubbingRules, ClaimHistoryAction.ScrubbingErrorsAndAlertsFound, $"{validationResult.Errors.Errors.Count}");

            if (priorClaimSubmission == null)
            {
                if (validationResult.ClaimSubmission.Id <= 0)
                {
                    await _billingClaimSubmissionRepository.AddAsync(validationResult.ClaimSubmission);
                }

                if (claimSubmission.DocumentType != ClaimDocumentType.HCFA1500Single && // already added and saved for hfca
                    claimSubmission.DocumentType != ClaimDocumentType.HCFA1500Multi &&
                    claimSubmission.FrequencyType == ClaimFrequencyType.Original)
                {
                    await _billingClaimSubmissionFunderSequenceRepository.AddRangeAsync(validationResult.FunderSequences);
                }

                await _billingClaimSubmissionServiceLineRepository.AddRangeAsync(validationResult.ServiceLines);
                await _billingClaimSubmissionServiceLineRepository.CommitAsync();

                claimSubmission = validationResult.ClaimSubmission;
            }

            if (priorClaimSubmissionExists)
            {
                claimSubmission.PriorClaimSubmissionId = priorClaimSubmission.Id;
            }

            claim.LastBilledFunderId = claimSubmission.FunderId;
            claim.SecondaryFunderId = secondaryFunderId.HasValue ? claimSubmission.FunderId : claim.SecondaryFunderId;
            MarkUpdated(claim, submittingMemberId);
            _billingClaimRepository.Update(claim);
            await _billingClaimRepository.CommitAsync();
            await UpdateClaimSubmissionEntities(validationResult, true);
            await SaveClaimErrors(validationResult, submittingMemberId, null, claimSubmission);
        }

        #endregion

        #region ValidateClaimSubmissionData
        private async Task<ClaimSubmissionValidationResult> PopulateClaimSubmissionData(ClaimSubmissionData data,
                                                        ClaimSubmissionValidationResult validationResult,
                                                       ClaimFrequencyType frequencyType,
                                                       int memberId)
        {

            #region "Claim Submission Part"
            var claimSubmission = validationResult.ClaimSubmission;
            var serviceLines = validationResult.ServiceLines;
            var funderSequences = validationResult.FunderSequences;

            if (claimSubmission.Id <= 0)
            {
                // when we submit for a new claim, we have to hook these things up because they are not loaded
                claimSubmission.ClaimId = data.Claim.Id;
                claimSubmission.SubmitDate = EstDateTime;
                claimSubmission.DateCreated = EstDateTime;
            }

            claimSubmission.ClaimSubmissionServiceLines = serviceLines;
            claimSubmission.ClaimSubmissionFunderSequences = funderSequences;

            claimSubmission.ReportPath = "N/A";
            claimSubmission.ClaimFilePath = "N/A";
            claimSubmission.DateLastModified = EstDateTime;

            var accountinfoId = data.Claim.AccountInfoId;
            if (claimSubmission.ChildProfileAuthorization == null)
            {
                claimSubmission.AuthorizationNumber = data.Authorization?.authorizationNumber ?? data.Claim.AuthorizationNumber;
                claimSubmission.ChildProfileAuthorization = data.Authorization;
                claimSubmission.ChildProfileAuthorizationId = data.Authorization?.id ?? 0;

            }

            // capture the current responsibility sequence
            claimSubmission.ResponsibilitySequence = data.ResponsibilitySequenceCurrent.AsString();

            // Account should always be set
            claimSubmission.AccountAddress1 = data.AccountInfo.AccountAddress1;
            claimSubmission.AccountAddress2 = data.AccountInfo.AccountAddress2;
            claimSubmission.AccountCity = data.AccountInfo.AccountCity;
            claimSubmission.AccountState = await GetState(data.AccountInfo.AccountStateId);
            claimSubmission.AccountZip = data.AccountInfo.AccountZip;
            claimSubmission.AccountCountry = await GetCountry(data.AccountInfo.AccountCountryId ?? 1);
            claimSubmission.AccountTown = data.AccountInfo.AccountTown;
            claimSubmission.AccountBillingAddress1 = data.AccountInfo.BillingAddress1;
            claimSubmission.AccountBillingAddress2 = data.AccountInfo.BillingAddress2 ?? data.AccountInfo.BillingAddress3; // we don't support both
            claimSubmission.AccountBillingCity = data.AccountInfo.BillingCity;
            claimSubmission.AccountBillingState = await GetState(data.AccountInfo.BillingStateId);
            claimSubmission.AccountBillingZip = data.AccountInfo.BillingZip;
            claimSubmission.AccountBillingCountry = await GetCountry(data.AccountInfo.BillingCountryId);
            claimSubmission.AccountBillingTown = data.AccountInfo.BillingTown;
            claimSubmission.AccountBillingProviderEmail = data.AccountInfo.BillingProviderEmail;
            claimSubmission.AccountBillingProviderFax = data.AccountInfo.BillingProviderFax;
            claimSubmission.AccountBillingProviderName = data.AccountInfo.BillingProviderName;
            claimSubmission.AccountBillingProviderPhone = data.AccountInfo.BillingProviderPhone;
            claimSubmission.AccountBillingProviderTaxonomyCode = data.AccountInfo.BillingProviderTaxonomyCode;
            claimSubmission.AccountFederalTaxId = data.AccountInfo.FederalTaxId;
            claimSubmission.AccountNpiNumber = data.AccountInfo.NationalProviderId;
            claimSubmission.AccountPhoneNumber = data.AccountInfo.PhoneNumber;

            claimSubmission.PlaceOfServiceCode = data.LocationCode?.code;
            claimSubmission.TotalPatientPaid = data.PatientPaid ?? 0;

            //if (data.ClearingHouse != null)
            //{
            //    claimSubmission.ClearinghouseIdentifier = data.ClearingHouse.ClearingHouseIdentifier;
            //    claimSubmission.ClearinghouseProviderIdentifier = data.ClearingHouse.ProviderIdentifier;
            //    claimSubmission.ClearinghouseSubmitterName = data.ClearingHouse.SubmitterName;
            //}

            if (data.FunderServiceLineMappings != null)
            {
                var currentFunderMapping = data.FunderMappingCurrent ?? data?.Authorization?.ChildProfileFunderServiceLineMapping?.ChildProfileFunderMapping;
                claimSubmission.FunderId = currentFunderMapping?.Funder.id;
                claimSubmission.FunderBillingProviderOption = currentFunderMapping?.Funder?.billingProviderOptionId == null ? null : (BillingProviderOptionType)currentFunderMapping.Funder?.billingProviderOptionId; //TODO: check about serviceLineBillingProviderOption usage in EdiGenerator

                claimSubmission.AuthorizedPaymentConfirmationType = GetConfirmationType(data.Claim.AuthorizedPaymentConfirmationTypeId) ??
                                                                        GetConfirmationType(currentFunderMapping?.authorizedPaymentConfirmationTypeId)
                                                                            ?? "W";
                claimSubmission.ReleaseOfInformationConfirmationType = GetConfirmationType(data.Claim.ReleaseOfInformationConfirmationTypeId) ??
                                                                    GetConfirmationType(currentFunderMapping?.releaseOfInformationConfirmationTypeId)
                                                                        ?? "W";

            }


            #region "ChildProfile"
            claimSubmission.ChildProfileAddress1 = data.ChildProfile != null ? data.ChildProfile.Address : "";
            claimSubmission.ChildProfileAddress2 = data.ChildProfile != null ? data.ChildProfile.Address2 : "";
            claimSubmission.ChildProfileCity = data.ChildProfile != null ? data.ChildProfile.City : "";
            claimSubmission.ChildProfileState = data.ChildProfile != null ? await GetState(data.ChildProfile.StateId) : "";
            claimSubmission.ChildProfileZip = data.ChildProfile != null ? data.ChildProfile.ZipCode : "";
            claimSubmission.ChildProfileCountry = data.ChildProfile != null ? "US" : "";
            claimSubmission.ChildProfileTown = data.ChildProfile != null ? data.ChildProfile.Town : "";
            claimSubmission.ChildProfileFirstName = data.ChildProfile != null ? data.ChildProfile.FirstName : "";
            claimSubmission.ChildProfileLastName = data.ChildProfile != null ? data.ChildProfile.LastName : "";
            claimSubmission.ChildProfileMiddleName = data.ChildProfile != null ? data.ChildProfile.MiddleName : "";
            claimSubmission.ChildProfileDOB = data.ChildProfile != null ? data.ChildProfile.DateOfBirth : null;
            claimSubmission.ChildProfileGender = data.ChildProfile != null ? GetGender(data.ChildProfile.GenderId) : "";
            #endregion

            #region "ProviderLocationAddress"
            claimSubmission.LocationBillingProviderAddress1 = data.ProviderLocationAddress != null ? data.ProviderLocationAddress.street1 : "";
            claimSubmission.LocationBillingProviderAddress2 = data.ProviderLocationAddress != null ? data.ProviderLocationAddress.street2 : "";
            claimSubmission.LocationBillingProviderCity = data.ProviderLocationAddress != null ? data.ProviderLocationAddress.city : "";
            claimSubmission.LocationBillingProviderState = data.ProviderLocationAddress != null ? await GetState(data.ProviderLocationAddress.stateId) : "";
            claimSubmission.LocationBillingProviderZip = data.ProviderLocationAddress != null ? data.ProviderLocationAddress.zipCode : "";
            claimSubmission.LocationBillingProviderCountry = data.ProviderLocationAddress != null ? await GetCountry(data.ProviderLocationAddress.countryId) : "";
            claimSubmission.LocationBillingProviderTown = data.ProviderLocationAddress != null ? data.ProviderLocationAddress.town : "";
            #endregion

            #region "ProviderLocation"
            if (data.ProviderLocation != null)
            {
                claimSubmission.LocationBillingProviderFederalTaxId = data.ProviderLocation != null ? data.ProviderLocation.federalTaxId : "";
                claimSubmission.LocationBillingProviderIsBillingLocation = data.ProviderLocation != null ? data.ProviderLocation.isBillingLocation : false;
                claimSubmission.LocationBillingProviderName = data.ProviderLocation != null ? data.ProviderLocation.agencyName : "";
                claimSubmission.LocationBillingProviderNpiNumber = data.ProviderLocation != null ? data.ProviderLocation.npiNumber : "";
                claimSubmission.LocationBillingProviderTaxonomyCode = data.ProviderLocation != null ? data.ProviderLocation.taxonomyCode : "";
                claimSubmission.LocationBillingProviderCommercialNumber = data.ProviderLocation != null ? data.ProviderLocation.providerCommercialNumber : "";
                claimSubmission.LocationBillingProviderStateLicenseNumber = data.ProviderLocation != null ? data.ProviderLocation.stateLicenseNumber : "";
                claimSubmission.LocationBillingProviderLocationNumber = data.ProviderLocation != null ? data.ProviderLocation.locationNumber : "";
            }
            #endregion

            #region "ReferringProvider
            claimSubmission.ReferringProviderFirstName = (data.ReferringProvider != null && data.ReferringProvider.ReferringProvider != null) ? data.ReferringProvider.ReferringProvider.name.firstName : "";
            claimSubmission.ReferringProviderLastName = (data.ReferringProvider != null && data.ReferringProvider.ReferringProvider != null) ? data.ReferringProvider.ReferringProvider.name.lastName : "";
            claimSubmission.ReferringProviderNpiNumber = (data.ReferringProvider != null && data.ReferringProvider.ReferringProvider != null) ? data.ReferringProvider.ReferringProvider.identifiers.FirstOrDefault(x => x.identifierType.ToLower() == "npinumber")?.value : "";

            #endregion

            #region "RenderingStaffMemberInfo"
            if (data.RenderingStaffMemberInfo != null)
            {
                claimSubmission.RenderingProviderStaffFirstName = data.RenderingStaffMemberInfo.StaffProviderFirstName;
                claimSubmission.RenderingProviderStaffLastName = data.RenderingStaffMemberInfo.StaffProviderLastName;
                claimSubmission.RenderingProviderStaffMiddleName = data.RenderingStaffMemberInfo.StaffProviderMiddleName;
                claimSubmission.RenderingProviderStaffNpiNumber = data.RenderingStaffMemberInfo.StaffProviderNPINumber;
                claimSubmission.RenderingProviderStaffTaxonomyCode = data.RenderingStaffMemberInfo.StaffProviderTaxonomyCode;
            }
            else
            {
                var staffMember = await _rethinkServices.GetStaffMember(data.AccountInfo.Id, data.Claim.RenderingProviderTypeId ?? 0);
                claimSubmission.RenderingProviderStaffFirstName = staffMember != null ? staffMember.name.firstName : "";
                claimSubmission.RenderingProviderStaffLastName = staffMember != null ? staffMember.name.lastName : "";
                claimSubmission.RenderingProviderStaffMiddleName = "";
                claimSubmission.RenderingProviderStaffNpiNumber = staffMember.identifiers.FirstOrDefault(x => x.identifierType.ToLower() == "npinumber")?.value;
                claimSubmission.RenderingProviderStaffTaxonomyCode = "";
            }

            #endregion

            #region "ServiceLocation"
            claimSubmission.ServiceLocationName = data.Claim.ServiceLocation != null ? data.Claim.ServiceLocation.name : "";
            claimSubmission.ServiceLocationNpiNumber = data.Claim.ServiceLocation != null ? data.Claim.ServiceLocation.npiNumber : "";
            claimSubmission.ServiceLocationAddress1 = data.Claim.ServiceLocation != null ? data.Claim.ServiceLocation.address.street1 : "";
            claimSubmission.ServiceLocationAddress2 = data.Claim.ServiceLocation != null ? data.Claim.ServiceLocation.address.street2 : "";
            claimSubmission.ServiceLocationCity = data.Claim.ServiceLocation != null ? data.Claim.ServiceLocation.address.city : "";
            claimSubmission.ServiceLocationState = data.Claim.ServiceLocation != null ? await GetState(data.Claim.ServiceLocation.address.stateId) : "";
            claimSubmission.ServiceLocationZip = data.Claim.ServiceLocation != null ? data.Claim.ServiceLocation.address.zip : "";
            claimSubmission.ServiceLocationCountry = data.Claim.ServiceLocation != null ? await GetCountry(data.Claim.ServiceLocation.address.countryId) : "";

            #endregion

            #region "PriorFunderLatestClaimPayment"
            claimSubmission.PayerClaimControlNumber = claimSubmission.PayerClaimControlNumber.IsNullOrWhiteSpace() ? data.PriorFunderLatestClaimPayment != null ? data.PriorFunderLatestClaimPayment.ControlNumber : "" : claimSubmission.PayerClaimControlNumber;
            claimSubmission.PriorFunderLatestClaimPayment = data.PriorFunderLatestClaimPayment != null ? data.PriorFunderLatestClaimPayment : null;

            #endregion
            #endregion

            #region "Funder Sequence"
            var sequenceOrder = 0;
            if (data.FunderServiceLineMappings != null && (frequencyType == ClaimFrequencyType.Original ||
                                                           frequencyType == ClaimFrequencyType.Replacement))
            {
                funderSequences = new List<ClaimSubmissionFunderSequenceEntity>();
                claimSubmission.ClaimSubmissionFunderSequences = await _billingClaimSubmissionFunderSequenceRepository.Query().Where(x => x.ClaimSubmissionId == claimSubmission.Id).ToListAsync();

                foreach (var serviceLineMapping in data.FunderServiceLineMappings)
                {
                    var funderMapping = serviceLineMapping.ChildProfileFunderMapping;
                    sequenceOrder += 1;

                    var funderSequnceExists = claimSubmission.ClaimSubmissionFunderSequences.FirstOrDefault(x => x.FunderId == funderMapping.funderId);
                    var payerDetails = await GetPayerDetails(funderMapping.funderId);

                    if (funderSequnceExists != null)
                    {
                        #region Columns
                        funderSequnceExists.SequenceOrder = sequenceOrder;
                        funderSequnceExists.ClaimSubmissionId = claimSubmission.Id;
                        funderSequnceExists.FunderId = funderMapping.funderId;

                        funderSequnceExists.FunderName = payerDetails?.payerName ?? "";
                        funderSequnceExists.FunderVendorId = payerDetails?.payerId ?? "";

                        //FunderName = funderMapping.Funder.funderName,
                        //FunderVendorId = funderMapping.Funder.vendorId,

                        funderSequnceExists.FunderResponsibilitySequence = serviceLineMapping.responsibilitySequence.AsString();
                        funderSequnceExists.InsuranceGroupNumber = funderMapping.InsuranceContact.InsuranceContactsType.insuranceGroupNumber;
                        funderSequnceExists.InsurancePolicyNumber = funderMapping.InsuranceContact.InsuranceContactsType.insurancePolicyNumber;
                        funderSequnceExists.InsuranceGroupName = null; // TODO
                        funderSequnceExists.InsurancePlanName = funderMapping.Funder.FunderInsurancePlans?.FirstOrDefault(fip => fip.id == funderMapping.funderUnsurancePlanId)?.planName;
                        //InsuranceCoverageType = GetInsuranceCoverageType(funderMapping.FunderCoverageTypeId ?? funderMapping.Funder.FunderCoverageTypeId ?? 33/*ZZ*/),
                        funderSequnceExists.InsuranceCoverageType = GetInsuranceCoverageType(funderMapping.Funder.funderCoverageTypeId ?? 33/*ZZ*/);

                        funderSequnceExists.InsuranceAddress1 = funderMapping.Funder.address?.street1;
                        funderSequnceExists.InsuranceAddress2 = funderMapping.Funder.address?.street2;
                        funderSequnceExists.InsuranceCity = funderMapping.Funder.address?.city;
                        funderSequnceExists.InsuranceState = await GetState(funderMapping.Funder.address?.stateId);
                        funderSequnceExists.InsuranceZip = funderMapping.Funder.address?.zip;
                        funderSequnceExists.InsuranceCountry = await GetCountry(funderMapping.Funder.address?.countryId);
                        funderSequnceExists.InsuranceTown = funderMapping.Funder.address?.town;

                        funderSequnceExists.SubscriberFirstName = funderMapping.InsuranceContact.Name?.firstName;
                        funderSequnceExists.SubscriberLastName = funderMapping.InsuranceContact.Name?.lastName;
                        funderSequnceExists.SubscriberMiddleName = funderMapping.InsuranceContact.Name?.middleName;
                        funderSequnceExists.SubscriberDOB = funderMapping.InsuranceContact.DateOfBirth;
                        funderSequnceExists.SubscriberGender = GetGender(funderMapping.InsuranceContact.GenderId);

                        funderSequnceExists.SubscriberAddress1 = funderMapping.InsuranceContact.Address?.street1;
                        funderSequnceExists.SubscriberAddress2 = funderMapping.InsuranceContact.Address?.street2;
                        funderSequnceExists.SubscriberCity = funderMapping.InsuranceContact.Address?.city;
                        funderSequnceExists.SubscriberState = await GetState(funderMapping.InsuranceContact.Address?.stateId);
                        funderSequnceExists.SubscriberZip = funderMapping.InsuranceContact.Address?.zipCode;
                        funderSequnceExists.SubscriberCountry = await GetCountry(funderMapping.InsuranceContact.Address?.countryId);
                        funderSequnceExists.SubscriberTown = funderMapping.InsuranceContact.Address?.town;
                        funderSequnceExists.RelationshipToSubscriber = funderMapping.InsuranceContact.InsuranceContactsType.relationshipToInsuredTypeId ?? 2; // default to dependent if not set
                        funderSequnceExists.ReleaseOfInformationConfirmationDate = funderMapping.releaseOfInformationConfirmationDate;
                        funderSequnceExists.MedicalRecordNumber = funderMapping.InsuranceContact.InsuranceContactsType.medicalRecordNumber;
                        funderSequnceExists.ServiceLineBillingProviderOption = await GetServiceLineBillingProviderOption(funderMapping.funderId, serviceLineMapping.serviceId, accountinfoId);

                        MarkUpdated(funderSequnceExists, memberId);
                        funderSequences.Add(funderSequnceExists);
                        #endregion
                    }
                    else
                    {
                        var funderSequence = new ClaimSubmissionFunderSequenceEntity()
                        {
                            #region Columns
                            SequenceOrder = sequenceOrder,
                            ClaimSubmissionId = claimSubmission.Id,
                            FunderId = funderMapping.funderId,

                            FunderName = payerDetails?.payerName ?? "",
                            FunderVendorId = payerDetails?.payerId ?? "",

                            //FunderName = funderMapping.Funder.funderName,
                            //FunderVendorId = funderMapping.Funder.vendorId,

                            FunderResponsibilitySequence = serviceLineMapping.responsibilitySequence.AsString(),
                            InsuranceGroupNumber = funderMapping.InsuranceContact.InsuranceContactsType.insuranceGroupNumber,
                            InsurancePolicyNumber = funderMapping.InsuranceContact.InsuranceContactsType.insurancePolicyNumber,
                            InsuranceGroupName = null, // TODO
                            InsurancePlanName = funderMapping.Funder.FunderInsurancePlans?.FirstOrDefault(fip => fip.id == funderMapping.funderUnsurancePlanId)?.planName,
                            //InsuranceCoverageType = GetInsuranceCoverageType(funderMapping.FunderCoverageTypeId ?? funderMapping.Funder.FunderCoverageTypeId ?? 33/*ZZ*/),
                            InsuranceCoverageType = GetInsuranceCoverageType(funderMapping.Funder.funderCoverageTypeId ?? 33/*ZZ*/),

                            InsuranceAddress1 = funderMapping.Funder.address?.street1,
                            InsuranceAddress2 = funderMapping.Funder.address?.street2,
                            InsuranceCity = funderMapping.Funder.address?.city,
                            InsuranceState = await GetState(funderMapping.Funder.address?.stateId),
                            InsuranceZip = funderMapping.Funder.address?.zip,
                            InsuranceCountry = await GetCountry(funderMapping.Funder.address?.countryId),
                            InsuranceTown = funderMapping.Funder.address?.town,

                            SubscriberFirstName = funderMapping.InsuranceContact.Name?.firstName,
                            SubscriberLastName = funderMapping.InsuranceContact.Name?.lastName,
                            SubscriberMiddleName = funderMapping.InsuranceContact.Name?.middleName,
                            SubscriberDOB = funderMapping.InsuranceContact.DateOfBirth,
                            SubscriberGender = GetGender(funderMapping.InsuranceContact.GenderId),

                            SubscriberAddress1 = funderMapping.InsuranceContact.Address?.street1,
                            SubscriberAddress2 = funderMapping.InsuranceContact.Address?.street2,
                            SubscriberCity = funderMapping.InsuranceContact.Address?.city,
                            SubscriberState = await GetState(funderMapping.InsuranceContact.Address?.stateId),
                            SubscriberZip = funderMapping.InsuranceContact.Address?.zipCode,
                            SubscriberCountry = await GetCountry(funderMapping.InsuranceContact.Address?.countryId),
                            SubscriberTown = funderMapping.InsuranceContact.Address?.town,
                            RelationshipToSubscriber = funderMapping.InsuranceContact.InsuranceContactsType.relationshipToInsuredTypeId ?? 2, // default to dependent if not set
                            ReleaseOfInformationConfirmationDate = funderMapping.releaseOfInformationConfirmationDate,
                            MedicalRecordNumber = funderMapping.InsuranceContact.InsuranceContactsType.medicalRecordNumber,
                            ServiceLineBillingProviderOption = await GetServiceLineBillingProviderOption(funderMapping.funderId, serviceLineMapping.serviceId,
                                                                        accountinfoId)
                        };
                        #endregion
                        MarkCreated(funderSequence, memberId);
                        funderSequences.Add(funderSequence);
                    }
                }
            }
            else
            {
                // For void claims, we have already cloned the funder sequences
                foreach (var funderSequence in funderSequences)
                {
                    MarkCreated(funderSequence, memberId);
                    funderSequence.ClaimSubmission = claimSubmission;
                }
            }
            #endregion

            #region "Service Lines"
            var svcLineIndex = 1;
            if ((frequencyType == ClaimFrequencyType.Original || frequencyType == ClaimFrequencyType.Replacement))
            {
                serviceLines = new List<ClaimSubmissionServiceLineEntity>();

                claimSubmission.ClaimSubmissionServiceLines = await _billingClaimSubmissionServiceLineRepository.Query().Where(x => x.ClaimSubmissionId == claimSubmission.Id).ToListAsync();

                // ADD OR UPDATE CHARGE ENTRIES
                foreach (var chargeEntry in data.ChargeEntries)
                {
                    if (data.DiagnosisCodes != null && data.DiagnosisCodes.Find(dc => dc.ClaimCharge.Id == chargeEntry.Id) != null)
                    {
                        var diagnosisCodeOrder = data.DiagnosisCodes.Any()
                                                    ? data.DiagnosisCodes.First(dc => dc.ClaimCharge.Id == chargeEntry.Id)
                                                    : null;
                        var serviceLineExists = claimSubmission.ClaimSubmissionServiceLines.FirstOrDefault(x => x.ClaimChargeEntryId == chargeEntry.Id);
                        if (serviceLineExists != null)
                        {
                            serviceLineExists.ClaimSubmissionId = claimSubmission.Id;
                            serviceLineExists.ClaimChargeEntryId = chargeEntry.Id;
                            serviceLineExists.ServiceLineIndex = svcLineIndex++;
                            serviceLineExists.ServiceLineIdentifier = svcLineIndex.ToString("00");
                            serviceLineExists.DateOfService = chargeEntry.DateOfService;
                            serviceLineExists.DiagnosisCode = chargeEntry.DiagnosisCode;
                            serviceLineExists.DiagnosisCodeType = await GetDiagnosisCodeType(accountinfoId, chargeEntry.DiagnosisCode);
                            serviceLineExists.BillingCode = chargeEntry.BillingCode;
                            serviceLineExists.BillingCodeDescription = chargeEntry.BillingCodeDescription;
                            serviceLineExists.Charges = chargeEntry.Charges;
                            serviceLineExists.DiagnosisCodeOrder = diagnosisCodeOrder?.Order;
                            serviceLineExists.Modifier1 = chargeEntry.Modifier1;
                            serviceLineExists.Modifier2 = chargeEntry.Modifier2;
                            serviceLineExists.Modifier3 = chargeEntry.Modifier3;
                            serviceLineExists.Modifier4 = chargeEntry.Modifier4;
                            serviceLineExists.UnitRate = chargeEntry.UnitRate ?? 0;
                            serviceLineExists.UnitTypeId = chargeEntry.UnitTypeId;
                            serviceLineExists.Units = chargeEntry.Units;
                            serviceLineExists.NoteText = chargeEntry.NoteText;
                            serviceLineExists.NoteCreatedBy = chargeEntry.NoteCreatedBy;
                            serviceLineExists.NoteCreatedDate = chargeEntry.NoteCreatedDate;

                            MarkUpdated(serviceLineExists, claimSubmission.ModifiedBy.GetValueOrDefault());
                            serviceLines.Add(serviceLineExists);
                        }
                        else
                        {
                            var serviceLine = new ClaimSubmissionServiceLineEntity()
                            {
                                ClaimSubmissionId = claimSubmission.Id,
                                ClaimChargeEntryId = chargeEntry.Id,
                                ServiceLineIndex = svcLineIndex++,
                                ServiceLineIdentifier = svcLineIndex.ToString("00"),
                                DateOfService = chargeEntry.DateOfService,
                                DiagnosisCode = chargeEntry.DiagnosisCode,
                                DiagnosisCodeType = await GetDiagnosisCodeType(accountinfoId, chargeEntry.DiagnosisCode),
                                BillingCode = chargeEntry.BillingCode,
                                BillingCodeDescription = chargeEntry.BillingCodeDescription,
                                Charges = chargeEntry.Charges,
                                DiagnosisCodeOrder = diagnosisCodeOrder?.Order,
                                Modifier1 = chargeEntry.Modifier1,
                                Modifier2 = chargeEntry.Modifier2,
                                Modifier3 = chargeEntry.Modifier3,
                                Modifier4 = chargeEntry.Modifier4,
                                UnitRate = chargeEntry.UnitRate ?? 0,
                                UnitTypeId = chargeEntry.UnitTypeId,
                                Units = chargeEntry.Units,
                                NoteText = chargeEntry.NoteText,
                                NoteCreatedBy = chargeEntry.NoteCreatedBy,
                                NoteCreatedDate = chargeEntry.NoteCreatedDate,
                            };
                            MarkCreated(serviceLine, claimSubmission.ModifiedBy.GetValueOrDefault());
                            serviceLines.Add(serviceLine);
                        }
                    }
                }
            }
            else
            {
                // For void claims, we have already cloned the funder sequences
                foreach (var serviceLine in serviceLines)
                {
                    MarkCreated(serviceLine, claimSubmission.ModifiedBy.GetValueOrDefault());
                    serviceLine.ClaimSubmission = claimSubmission;
                }
            }
            #endregion

            return new ClaimSubmissionValidationResult(validationResult.Errors, claimSubmission, serviceLines, funderSequences);
        }

        private async Task<ClaimSubmissionValidationResult> ValidateClaimSubmissionData(
                                                                ClaimSubmissionData data,
                                                                ClaimSubmissionEntity claimSubmission,
                                                                ClaimFrequencyType frequencyType,                  // Original, Corrected, Void
                                                                ClaimSubmissionType submissionType,                // Original, Transfer, Rebill, Transfer & Rebill 
                                                                ClaimDocumentType documentType,                    // 837P, HCFA1500, etc.
                                                                ResponsibilitySequenceType responsibilitySequence, // Primary, Secondary, etc.
                                                                bool priorClaimSubmissionExists,
                                                                int memberId)
        {
            #region "Validate data"

            var errors = new ValidationErrorList(await _billingClaimErrorMessageRepository.Query().ToListAsync());
            var latestDOS = data.LatestDOS;

            //------------------------------------------------------------------------------
            #region validation functions
            //------------------------------------------------------------------------------
            bool Check(bool test, ClaimErrorNumber errorNum, string msg = null)
            {
                if (!test)
                {
                    errors.AddError(errorNum, msg);
                }

                return test;
            }
            bool CheckAll(bool test, List<ClaimErrorNumber> errorNums, string msg = null)
            {
                if (!test)
                {
                    foreach (var errorNum in errorNums)
                    {
                        errors.AddError(errorNum, msg);
                    }
                }

                return test;
            }
            bool CheckBool(bool? test, ClaimErrorNumber errorNum, string msg = null)
            {
                return Check((test.HasValue ? test.Value : false), errorNum, msg);
            }
            bool CheckNotNull(int? val, ClaimErrorNumber errorNum, string msg = null)
            {
                return Check((val.HasValue && val.Value >= 0), errorNum, msg);
            }
            bool CheckString(string str, ClaimErrorNumber errorNum, string msg = null)
            {
                return Check((!string.IsNullOrWhiteSpace(str)), errorNum, msg);
            }

            bool CheckStringLenRange(string str, int minLen, int maxLen, ClaimErrorNumber errorNum, string msg = null)
            {
                string pattern = $@"^[a-zA-Z0-9]{{{minLen},{maxLen}}}$";
                bool isValid = Regex.IsMatch(str, pattern);
                
                return Check(isValid, errorNum, msg);
            }

            bool CheckStringMinLen(string str, int minLen, ClaimErrorNumber errorNum, string msg = null)
            {
                if (!str.IsNullOrEmpty() && str.Length == 10)
                { str = str.Replace("-", ""); }
                return Check((!string.IsNullOrWhiteSpace(str) && (str.Length >= minLen)), errorNum, msg);
            }
            bool CheckDecimal(decimal? num, ClaimErrorNumber errorNum, string msg = null)
            {
                return Check((num != null && num > 0.009m), errorNum, msg);
            }
            bool CheckConfirmation(string confStr, ClaimErrorNumber errorNum, string msg = null)
            {
                switch (confStr)
                {
                    case "Y":
                    case "N":
                    case "W":
                        return true;
                    default:
                        return Check(false, errorNum, msg);
                }
            }
            bool CheckNpi(string npi, ClaimErrorNumber errorNum, string msg = null)
            {
                return Check(IsValidNpi(npi), errorNum, msg);
            }
            bool CheckFederalTaxID(string taxID, ClaimErrorNumber errorNum, string msg = null)
            {
                return Check(IsValidFederalTaxID(taxID), errorNum, msg);
            }
            bool CheckInactive(DateTime? date, ClaimErrorNumber errorNum, string msg = null)
            {
                return Check(date == null || (date > latestDOS), errorNum, msg);
            }
            bool CheckDate(DateTime? date, ClaimErrorNumber errorNum, string msg = null)
            {
                return Check(date != null && (date > _validDateCheck), errorNum, msg);
            }
            //------------------------------------------------------------------------------
            #endregion
            //------------------------------------------------------------------------------


            List<ClaimSubmissionServiceLineEntity> serviceLines = null;
            if (priorClaimSubmissionExists && (claimSubmission?.ClaimSubmissionServiceLines != null || claimSubmission?.ClaimSubmissionServiceLines.Count() != 0))
            {
                // we need to clone the replacement/void service lines
                serviceLines = CloneServiceLines(claimSubmission.ClaimSubmissionServiceLines, memberId);
            }
            else
            {
                //serviceLines = claimSubmission?.ClaimSubmissionServiceLines?.ToList() ?? new List<ClaimSubmissionServiceLineEntity>();
                serviceLines = new List<ClaimSubmissionServiceLineEntity>();
            }


            List<ClaimSubmissionFunderSequenceEntity> funderSequences = null;
            if (priorClaimSubmissionExists && claimSubmission?.ClaimSubmissionFunderSequences != null)
            {
                // we need to clone the replacement/void service lines
                funderSequences = CloneFunderSequences(claimSubmission.ClaimSubmissionFunderSequences, memberId);
            }
            else
            {
                //funderSequences = claimSubmission?.ClaimSubmissionFunderSequences?.ToList() ?? new List<ClaimSubmissionFunderSequenceEntity>();
                funderSequences = new List<ClaimSubmissionFunderSequenceEntity>();
            }


            var validationResult = new ClaimSubmissionValidationResult(errors, claimSubmission, serviceLines, funderSequences);
            validationResult = await PopulateClaimSubmissionData(data,
                                              validationResult,
                                              frequencyType,
                                              memberId);

            var submission = validationResult.ClaimSubmission;

            // Address validations
            CheckString(data.ChildProfile.Address, ClaimErrorNumber.ChildProfileAddressMissingOrInvalid);
            CheckString(data.ChildProfile.City, ClaimErrorNumber.ChildProfileCityMissingOrInvalid);
            CheckNotNull(data.ChildProfile.StateId, ClaimErrorNumber.ChildProfileStateMissingOrInvalid);
            // commenting zip code error for bug - 231655 - Chetan 13-12-2024
            CheckStringMinLen(data.ChildProfile.ZipCode, 5, ClaimErrorNumber.ChildProfileZipMissingOrInvalid);

            CheckString(data.AccountInfo.BillingAddress1, ClaimErrorNumber.AccountBillingAddressMissingOrInvalid);
            CheckString(data.AccountInfo.BillingCity, ClaimErrorNumber.AccountBillingCityMissingOrInvalid);
            CheckNotNull(data.AccountInfo.BillingStateId, ClaimErrorNumber.AccountBillingStateMissingOrInvalid);
            CheckStringMinLen(data.AccountInfo.BillingZip, 5, ClaimErrorNumber.AccountBillingZipMissingOrInvalid);
            CheckString(submission.LocationBillingProviderTaxonomyCode, ClaimErrorNumber.BillingProviderTaxonomyMissing);

             // InsurancePolicyNumber Validation
            var firstFunderSequence = validationResult.FunderSequences.FirstOrDefault();
            if (firstFunderSequence != null && !string.IsNullOrWhiteSpace(firstFunderSequence?.InsurancePolicyNumber))
            {
                CheckStringLenRange(
                    firstFunderSequence.InsurancePolicyNumber,
                    minLengthInsuredId,
                    maxLengthInsuredId,
                    ClaimErrorNumber.InsurancePolicyNumberSize
                );
            }

            #region Check Provider enrollment for Stedi

            var accountId = submission.ClaimId != 0 ? data.Claim.AccountInfoId : data.AccountInfo.Id;
            var accountInfo = await _rethinkServices.GetAccountReturningEntityAsync(accountId);
            var clearingHouseId = accountInfo.ClearingHouseId;

            if (clearingHouseId == (int)BillingClearingHousesEnum.Stedi)
            {
                var isEnrollmentCheckEnabled = await _featureFlagService.IsProviderEnrollmentValidationEnabledAsync();

                _logger.LogInformation(
                    "{Service}.{Method} - Provider enrollment validation flag: Enabled={IsEnabled}, ClaimId={ClaimId}, AccountId={AccountId}, NPI={Npi}",
                    nameof(ClaimValidationService),
                    nameof(ValidateClaimSubmissionData),
                    isEnrollmentCheckEnabled,
                    data.Claim.Id,
                    accountId,
                    submission.LocationBillingProviderNpiNumber);

                if (isEnrollmentCheckEnabled)
                {
                    var isProviderEnroll = await _stediProviderEnrollmentService.VerifyProviderEnrollmentAsync(submission.LocationBillingProviderNpiNumber);
                    Check(isProviderEnroll, ClaimErrorNumber.StediProviderEnrollment, null);
                }
            }

            #endregion

            if (data.ProviderLocationAddress != null) // it is *valid* to have a null provider location in certain
                                                      // situations. If we have it, we will validate it.
            {
                CheckString(data.ProviderLocationAddress.street1, ClaimErrorNumber.BillingProviderAddressMissingOrInvalid);
                CheckString(data.ProviderLocationAddress.city, ClaimErrorNumber.BillingProviderCityMissingOrInvalid);
                CheckNotNull(data.ProviderLocationAddress.stateId, ClaimErrorNumber.BillingProviderStateMissingOrInvalid);
                CheckStringMinLen(data.ProviderLocationAddress.zipCode, 5, ClaimErrorNumber.BillingProviderZipMissingOrInvalid);
            }


            var serviceFacilityErrors = new List<ClaimErrorNumber>
            {
                ClaimErrorNumber.ServiceLocationAddressMissingOrInvalid,
                ClaimErrorNumber.ServiceLocationCityMissingOrInvalid,
                ClaimErrorNumber.ServiceLocationStateMissingOrInvalid,
                ClaimErrorNumber.ServiceLocationZipMissingOrInvalid
            };

            if (CheckAll(data.ServiceFacilityLocation != null && data.ServiceFacilityLocation.address != null, serviceFacilityErrors, "Missing Service Facility address"))
            {
                CheckString(data.ServiceFacilityLocation.address.street1, ClaimErrorNumber.ServiceLocationAddressMissingOrInvalid);
                CheckString(data.ServiceFacilityLocation.address.city, ClaimErrorNumber.ServiceLocationCityMissingOrInvalid);
                CheckNotNull(data.ServiceFacilityLocation.address.stateId, ClaimErrorNumber.ServiceLocationStateMissingOrInvalid);
                CheckStringMinLen(data.ServiceFacilityLocation.address.zip, 5, ClaimErrorNumber.ServiceLocationZipMissingOrInvalid);
            }
            var isClaimManuallyCreated = data.Claim.ClaimHistory.Any(x => x.ClaimHistoryAction == ClaimHistoryAction.ClaimCreated
                        && x.Mode == ClaimActionMode.User);
            if (!isClaimManuallyCreated)
            {
                Check(data.AppointmentCount > 0, ClaimErrorNumber.AppointmentNoLinksFound);
            }
            // Appointments

            // Authorization
            if (submission.ChildProfileAuthorization != null && data.Claim.ClaimStatus == ClaimStatus.Billed)
            {
                var authorization = submission.ChildProfileAuthorization;
                var claim = data.Claim;

                //Check(authorization.billingProviderId == claim.ProviderLocationId, ClaimErrorNumber.BillingProviderChanged, $"Billing Provider updated on auth as of ${authorization?.billingProviderDateUpdated?.ToShortDateString()} date"); //#DBMIGRATION
                Check(authorization.renderingProviderStaffId == claim.RenderingStaffMemberId, ClaimErrorNumber.RenderingProviderChanged, $"Rendering provider updated on auth as of ${authorization?.renderingProviderDateUpdated?.ToShortDateString()} date");
                Check(authorization.childProfileReferringProviderId == claim.ChildProfileReferringProviderId, ClaimErrorNumber.ReferringProviderChanged, $"Referring Provider updated on auth as of ${authorization?.referringProviderDateUpdated?.ToShortDateString()} date");
                //Check(authorization.serviceFacilityLocationId == claim.ServiceLocationId, ClaimErrorNumber.FacilityChanged, $"Service Facility updated on auth as of ${authorization?.serviceFacilityLocationDateUpdated?.ToShortDateString()} date"); //#DBMIGRATION
            }

            var providerBillingCode = new BillingCodeData();
            foreach (var item in data.Claim.ClaimChargeEntries)
            {
                providerBillingCode = item.BillingCodeId != 0 && item.BillingCodeId != null ? await _rethinkServices.GetProviderBillingCode(data.Claim.AccountInfoId, (int)item.BillingCodeId) : null;
                if (providerBillingCode != null && providerBillingCode.noAuthRequired == false)
                {
                    if (CheckNotNull(submission.ChildProfileAuthorizationId, ClaimErrorNumber.AuthorizationMissing))
                    {
                        Check((!string.IsNullOrWhiteSpace(data.Claim.AuthorizationNumber)), ClaimErrorNumber.AuthorizationNotFound, $"(val={submission.ChildProfileAuthorizationId})");
                        //Check((submission.ChildProfileAuthorization != null), ClaimErrorNumber.AuthorizationNotFound, $"(val={submission.ChildProfileAuthorizationId})");
                    }

                    CheckString(submission.AuthorizationNumber, ClaimErrorNumber.AuthorizationNumber);
                    CheckInactive(submission?.ChildProfileAuthorization?.metaData?.deletedOn, ClaimErrorNumber.AuthorizationInactive);
                    break;
                }
            }

            // BillingInformation
            if (CheckString(submission.PlaceOfServiceCode, ClaimErrorNumber.BillingInformationPlaceOfServiceMissing))
            {
                Check(IsValidPlaceOfService(submission.PlaceOfServiceCode), ClaimErrorNumber.BillingInformationPlaceOfServiceInvalid);
            }

            // BillingProvider

            // first resolve the billing provider
            var funderSequence = funderSequences.FirstOrDefault(fs => fs.FunderId == submission.FunderId && fs.FunderResponsibilitySequence == submission.ResponsibilitySequence);
            if (funderSequence != null && funderSequence.ServiceLineBillingProviderOption.HasValue)
            {
                switch (funderSequence.ServiceLineBillingProviderOption)
                {
                    case BillingProviderOptionType.Group:
                    case BillingProviderOptionType.GroupAndIndividual:
                        {
                            submission.ResolvedBillingProviderFirstName = null; // N/A for group
                            submission.ResolvedBillingProviderMiddleName = null; // N/A for group
                            if (submission.LocationBillingProviderIsBillingLocation ?? false)
                            {
                                submission.ResolvedBillingProviderName = submission.LocationBillingProviderName;
                                submission.ResolvedBillingProviderNpi = submission.LocationBillingProviderNpiNumber;
                                submission.ResolvedBillingProviderFederalTaxID = submission.LocationBillingProviderFederalTaxId;
                            }
                            else
                            {
                                submission.ResolvedBillingProviderName = submission.AccountBillingProviderName;
                                submission.ResolvedBillingProviderNpi = submission.AccountNpiNumber;
                                submission.ResolvedBillingProviderFederalTaxID = submission.AccountFederalTaxId;
                            }
                        }
                        break;
                    case BillingProviderOptionType.Individual:
                        {
                            submission.ResolvedBillingProviderName = submission.RenderingProviderStaffLastName;
                            submission.ResolvedBillingProviderNpi = submission.RenderingProviderStaffNpiNumber;
                            submission.ResolvedBillingProviderFirstName = submission.RenderingProviderStaffFirstName;
                            submission.ResolvedBillingProviderMiddleName = submission.RenderingProviderStaffMiddleName;

                        }
                        break;
                }
            }
            else
            {
                submission.ResolvedBillingProviderName = submission.LocationBillingProviderName;
                submission.ResolvedBillingProviderNpi = submission.LocationBillingProviderNpiNumber;
                submission.ResolvedBillingProviderFederalTaxID = submission.LocationBillingProviderFederalTaxId;
            }

            if (CheckString(submission.ResolvedBillingProviderName, ClaimErrorNumber.BillingProviderMissing))
            {
                if (CheckString(submission.ResolvedBillingProviderNpi, ClaimErrorNumber.BillingProviderNpiMissing))
                {
                    CheckNpi(submission.ResolvedBillingProviderNpi, ClaimErrorNumber.BillingProviderNpiInvalid);
                }

                if (CheckString(submission.ResolvedBillingProviderFederalTaxID, ClaimErrorNumber.BillingProviderFederalTaxIdMissing))
                {
                    CheckFederalTaxID(submission.ResolvedBillingProviderFederalTaxID, ClaimErrorNumber.BillingProviderFederalTaxIdInvalid);
                }
            }

            //Clearinghouse
            // #DBMIGRATION

            if (Check((data.Claim.AccountInfo.ClearingHouse != null), ClaimErrorNumber.ClearingHouseDetailsMissing))
            {
                CheckString(data.Claim.AccountInfo.ClearingHouse.title, ClaimErrorNumber.ClearingHouseTitleMissing);
                CheckString(data.Claim.AccountInfo.ClearingHouse.urlLink, ClaimErrorNumber.ClearingHouseURLLinkMissing);
                CheckString(data.Claim.AccountInfo.ClearingHouse.userName, ClaimErrorNumber.ClearingHouseUserNameMissing);
                CheckString(data.Claim.AccountInfo.ClearingHouse.userPassword, ClaimErrorNumber.ClearingHousePasswordMissing);
            }

            // Validate SFTP connection for clearinghouses
            _logger.LogInformation("Starting clearinghouse credential validation for AccountId={AccountId},ClaimIdentifier={ClaimIdentifier}", data.Claim.AccountInfoId,data.Claim.ClaimIdentifier);
            var clearinghouseValidationResult = await _clearinghouseCredentialValidationService.ValidateAllClearinghousesAsync();

            var claimChValidation = clearinghouseValidationResult.clearinghouseCredentialValidationResults?.FirstOrDefault(r => r.ClearinghouseId == data.Claim.AccountInfo.ClearingHouse.id);

            if (!claimChValidation.IsValid)
            {
                _logger.LogInformation("clearinghouseValidationResult is invalid for AccountId={AccountId},ClaimIdentifier={ClaimIdentifier}", data.Claim.AccountInfoId, data.Claim.ClaimIdentifier);

                errors.AddError(ClaimErrorNumber.ClearingHouseAuthenticationFailure,clearinghouseValidationResult.ErrorMessage ?? $"Clearinghouse SFTP connection validation failed. Failed: " +
                    $"{clearinghouseValidationResult.FailedValidations}/{clearinghouseValidationResult.TotalClearinghouses}");
            }
            // Confirmations
            CheckConfirmation(submission.AuthorizedPaymentConfirmationType, ClaimErrorNumber.ConfirmationsBenefitAssignmentIndicatorMissing);
            CheckConfirmation(submission.ReleaseOfInformationConfirmationType, ClaimErrorNumber.ConfirmationsReleaseOfInformationMissing);

            // DiagnosisCode
            Check(data.DiagnosisCodes?.Any() ?? false, ClaimErrorNumber.DiagnosisCodeMissing);

            if (data.DiagnosisCodes?.Any() == true)
            {
                foreach (var diagnosisCode in data.DiagnosisCodes)
                {
                    CheckInactive(diagnosisCode.InactiveDate, ClaimErrorNumber.DiagnosisCodeInactive, $"Code:{diagnosisCode.DiagnosisCode}, " +
                                                                                                      $"Inactive date:{diagnosisCode.InactiveDate?.ToShortDateString()}");
                    var verifiedDxCode = LookupDiagnosisCode(diagnosisCode.DiagnosisCode.Code, data.AccountInfo.Id);
                    Check(verifiedDxCode != null, ClaimErrorNumber.DiagnosisCodeInvalid, $"Unknown diagnosis code: {diagnosisCode.DiagnosisCode.Code}, type: {diagnosisCode.DiagnosisCode.DiagnosisType}");

                }
            }

            // Service Facility
            // TODO: we currently do not capture facility
            //FacilityMissing
            //FacilityNotFound

            // Funder
            var currentFunderMapping = data?.FunderMappingCurrent ?? data?.Authorization?.ChildProfileFunderServiceLineMapping?.ChildProfileFunderMapping;
            if (Check((currentFunderMapping != null), ClaimErrorNumber.FunderMissing))
            {
                var currentFunderSequence = validationResult.FunderSequences.First(fs => fs.FunderId == currentFunderMapping.funderId);
                CheckString(currentFunderSequence.FunderName, ClaimErrorNumber.FunderMissing);
                CheckBool(currentFunderMapping?.Funder?.isActive, ClaimErrorNumber.FunderNotInactive);


                var currentDate = EstDateTime;
                var funderPolicyState = (currentFunderMapping.endDate ?? currentDate.Date) >= currentDate.Date
                          && (currentFunderMapping.startDate ?? currentDate.Date) <= currentDate.Date;
                CheckBool(funderPolicyState, ClaimErrorNumber.FunderInactivePolicy);

                CheckString(currentFunderSequence.SubscriberLastName, ClaimErrorNumber.InsuranceContactMissing);
                CheckString(currentFunderSequence.SubscriberGender, ClaimErrorNumber.InsuranceContactMissingGender);
                CheckDate(currentFunderSequence.SubscriberDOB, ClaimErrorNumber.InsuranceContactMissingDOB);

                CheckString(currentFunderSequence.SubscriberAddress1, ClaimErrorNumber.SubscriberAddressMissingOrInvalid);
                CheckString(currentFunderSequence.SubscriberCity, ClaimErrorNumber.SubscriberCityMissingOrInvalid);
                CheckStringMinLen(currentFunderSequence.SubscriberState, 2, ClaimErrorNumber.SubscriberStateMissingOrInvalid);
                // commenting zip code error for bug - 231655 - Chetan 13-12-2024
                CheckStringMinLen(currentFunderSequence.SubscriberZip, 5, ClaimErrorNumber.SubscriberZipMissingOrInvalid);

                if (data.Claim.ClaimStatus == ClaimStatus.Billed)
                {
                    var funderModifiedDate = currentFunderMapping.metaData.modifiedOn;
                    var claimUpdatedDate = data.Claim.DateLastModified.GetValueOrDefault();
                    var funderWasntModified = !(funderModifiedDate != null &&
                                             new DateTime(Math.Max(submission.DateLastModified.Value.Ticks, claimUpdatedDate.Ticks)) < funderModifiedDate);
                    Check(funderWasntModified, ClaimErrorNumber.FunderInformationUpdated);
                }
            }

            // InsuranceContact

            var firstAppointmentId = data.Claim.ClaimAppointmentLinks.FirstOrDefault()?.AppointmentId;
            var appointment = (firstAppointmentId.HasValue) ? await _rethinkServices.GetAppointmentAsync(firstAppointmentId.Value) : null;

            var funders = (appointment != null) ? await _rethinkServices.GetFunder(data.Claim.AccountInfoId, appointment.funderId) : await _rethinkServices.GetFunder(data.Claim.AccountInfoId, currentFunderMapping.funderId);
            var existingFunder = await _billingClaimRepository.Query().Where(x => x.Id == data.Claim.Id).FirstOrDefaultAsync();

            if (funders == null)
            {
                throw new NullReferenceException("Funder is missing.");
            }

            if (funders?.id != existingFunder?.PrimaryFunderId)
            {
                existingFunder.PrimaryFunderId = funders.id;
                await _billingClaimRepository.UpdateAsync(existingFunder);
            }

            var requirementCheckReferringProvider = funders.referringProviderRequiredOnClaim;
            //Referring Provider
            if (requirementCheckReferringProvider)
            {
                CheckBool(data.ReferringProvider?.isActive, ClaimErrorNumber.ReferringProviderInactive);
                if (CheckString(submission.ReferringProviderLastName, ClaimErrorNumber.ReferringProviderMissing))
                {
                    if (CheckString(submission.ReferringProviderNpiNumber, ClaimErrorNumber.ReferringProviderNpiMissing))
                    {
                        CheckNpi(submission.ReferringProviderNpiNumber, ClaimErrorNumber.ReferringProviderNpiInvalid);
                    }
                }
            }
            submission.ResolvedRenderingProviderName = submission.RenderingProviderStaffLastName;
            submission.ResolvedRenderingProviderNpi = submission.RenderingProviderStaffNpiNumber;
            submission.ResolvedRenderingProviderFirstName = submission.RenderingProviderStaffFirstName;
            submission.ResolvedRenderingProviderMiddleName = submission.RenderingProviderStaffMiddleName;

            if (CheckString(submission.ResolvedRenderingProviderName, ClaimErrorNumber.RenderingProviderMissing))
            {
                if (CheckString(submission.ResolvedRenderingProviderNpi, ClaimErrorNumber.RenderingProviderNpiMissing))
                {
                    CheckNpi(submission.ResolvedRenderingProviderNpi, ClaimErrorNumber.RenderingProviderNpiInvalid);
                }
            }

            // Service Lines
            Check(data.Claim.ClaimChargeEntries.Where(x => x.DateDeleted == null).Any(), ClaimErrorNumber.ServiceLineNoChargeEntries);

            foreach (var serviceLine in data.Claim.ClaimChargeEntries.Where(x => x.DateDeleted == null).ToList())
            {
                CheckString(serviceLine.BillingCode, ClaimErrorNumber.ServiceLineBillingCodeMissing);
                CheckDecimal(serviceLine.Units, ClaimErrorNumber.ServiceLineBillingCodeQty);
                CheckDecimal(serviceLine.Charges, ClaimErrorNumber.ServiceLineBillingCodeAmount);
            }

            //Note validation in case of submission reason 7 or 8

            if ((ClaimFrequencyType)data.Claim.FrequencyTypeId == ClaimFrequencyType.Replacement ||
                (ClaimFrequencyType)data.Claim.FrequencyTypeId == ClaimFrequencyType.Void)
            {
                var claim = data.Claim;
                if (claim.Note.IsNullOrEmpty())
                {
                    CheckString(claim.Note, ClaimErrorNumber.NoteMissing);
                }
                if (claim.OriginalClaim.IsNullOrEmpty())
                {
                    CheckString(claim.OriginalClaim, ClaimErrorNumber.OriginalClaimMissing);
                }
            }

            if (validationResult.ClaimSubmission != null)
            {
                var existingClaimSubmission = await _billingClaimSubmissionRepository
                    .Query()
                    .FirstOrDefaultAsync(x => x.Id == validationResult.ClaimSubmission.Id);

                if (existingClaimSubmission != null && validationResult.ClaimSubmission != null)
                {
                    existingClaimSubmission.ChildProfileAddress1 = validationResult.ClaimSubmission.ChildProfileAddress1;
                    existingClaimSubmission.ChildProfileCity = validationResult.ClaimSubmission.ChildProfileCity;
                    existingClaimSubmission.ChildProfileState = validationResult.ClaimSubmission.ChildProfileState;
                    existingClaimSubmission.ChildProfileZip = validationResult.ClaimSubmission.ChildProfileZip;
                    existingClaimSubmission.ChildProfileDOB = validationResult.ClaimSubmission.ChildProfileDOB;

                    await _billingClaimSubmissionRepository.UpdateAsync(existingClaimSubmission);
                }

                foreach (var newFunderSeq in validationResult.FunderSequences ?? Enumerable.Empty<ClaimSubmissionFunderSequenceEntity>())
                {
                    var existingSequence = await _billingClaimSubmissionFunderSequenceRepository
                        .Query()
                        .FirstOrDefaultAsync(x => x.Id == newFunderSeq.Id);

                    if (existingSequence != null)
                    {
                        existingSequence.SubscriberAddress1 = newFunderSeq.SubscriberAddress1;
                        existingSequence.SubscriberCity = newFunderSeq.SubscriberCity;
                        existingSequence.SubscriberZip = newFunderSeq.SubscriberZip;
                        existingSequence.SubscriberState = newFunderSeq.SubscriberState;

                        await _billingClaimSubmissionFunderSequenceRepository.UpdateAsync(existingSequence);
                    }
                }
            }

            //End
            #endregion

            return new ClaimSubmissionValidationResult(errors, validationResult.ClaimSubmission, validationResult.ServiceLines, validationResult.FunderSequences);
        }

        #endregion

        #region Sub Functions
        private async Task<BillingProviderOptionType?> GetServiceLineBillingProviderOption(int funderId, int serviceId, int accountInfoId)
        {
            var serviceLineFunderMappingList = await _rethinkServices.GetServiceFundersEntityListByFunderId(accountInfoId, 0, funderId);
            var serviceLineFunderMapping =
                serviceLineFunderMappingList.FirstOrDefault(x => x.providerServiceId == serviceId);
            //var serviceLineFunderMapping = _bhServiceFunderRepository.Query().FirstOrDefault(sf => sf.FunderId == funderId && sf.ProviderServiceId == serviceId);
            return (BillingProviderOptionType?)serviceLineFunderMapping?.billingProviderOptionId ?? BillingProviderOptionType.Unknown;
        }
        private ResponsibilitySequenceType IncrementResponsibilitySequence(ResponsibilitySequenceType currentResponsibilitySequence)
        {
            var current = currentResponsibilitySequence.AsOrdinal();
            return ResponsibilitySequenceTypeHelper.FromOrdinal(current + 1);
        }
        private ResponsibilitySequenceType DecrementResponsibilitySequence(ResponsibilitySequenceType currentResponsibilitySequence)
        {
            var current = currentResponsibilitySequence.AsOrdinal();
            return ResponsibilitySequenceTypeHelper.FromOrdinal(current - 1);
        }

        private List<ClaimSubmissionServiceLineEntity> CloneServiceLines(ICollection<ClaimSubmissionServiceLineEntity> serviceLinesToClone, int memberId)
        {
            var result = new List<ClaimSubmissionServiceLineEntity>();
            foreach (var serviceLine in serviceLinesToClone)
            {
                var newServiceLine = new ClaimSubmissionServiceLineEntity();
                EntityPropertyCopier.Copy(serviceLine, newServiceLine, new List<string> { nameof(ClaimSubmissionServiceLineEntity.ClaimChargeEntry) });
                MarkCreated(newServiceLine, memberId);
                result.Add(newServiceLine);
            }
            return result;
        }

        private List<ClaimSubmissionFunderSequenceEntity> CloneFunderSequences(ICollection<ClaimSubmissionFunderSequenceEntity> funderSequencesToClone,
                                                                               int membreId)
        {
            var result = new List<ClaimSubmissionFunderSequenceEntity>();
            foreach (var funderSequence in funderSequencesToClone)
            {
                var newFunderSequence = new ClaimSubmissionFunderSequenceEntity();
                EntityPropertyCopier.Copy(funderSequence, newFunderSequence);
                MarkCreated(newFunderSequence, membreId);
                result.Add(newFunderSequence);
            }
            return result;
        }
        private string GetInsuranceCoverageType(int? funderFunderCoverageTypeId)
        {
            switch (funderFunderCoverageTypeId ?? 0)
            {
                /* limited existing coverage codes
                1	Medicare
                2	Medicaid
                3	Tricare
                4	CHAMPVA
                5	Group Health Plan
                6	FECA BLK LUNG
                7	Other
                */
                // existing codes
                case 2: return "MC"; // Medicaid
                case 3: return "CH"; // Champus/TRICARE

                // new codes
                case 11: return "11"; // Other Non-Federal Programs
                case 12: return "12"; // Preferred Provider Organization (PPO)
                case 13: return "13"; // Point of Service (POS)
                case 14: return "14"; // Exclusive Provider Organization (EPO)
                case 15: return "15"; // Indemnity Insurance
                case 16: return "16"; // Health Maintenance Organization (HMO) Medicare Risk
                case 17: return "17"; // Dental Maintenance Organization
                case 20: return "AM"; // Automobile Medical
                case 21: return "BL"; // Blue Cross/Blue Shield
                case 22: return "CI"; // Commercial Insurance Co.
                case 23: return "DS"; // Disability
                case 24: return "FI"; // Federal Employees Program
                case 25: return "HM"; // Health Maintenance Organization
                case 26: return "LM"; // Liability Medical
                case 27: return "MA"; // Medicare Part A
                case 28: return "MB"; // Medicare Part B
                case 29: return "OF"; // Other Federal Program (Use code OF when submitting Medicare Part D claims.)
                case 30: return "TV"; // Title V
                case 31: return "VA"; // Veterans Affairs Plan
                case 32: return "WC"; // Workers' Compensation Health Claim
                default:
                    return "ZZ"; // Mutually Defined (Use Code ZZ when Type of Insurance is not known.)
            }
        }
        private bool IsValidFederalTaxID(string taxID)
        {
            taxID = taxID.Replace("-", "");
            return !string.IsNullOrWhiteSpace(taxID) &&
                   taxID.Length == 9 &&
                   taxID.All(char.IsDigit);

        }

        private bool IsValidNpi(string npi)
        {
            /*
            NPI number format:

              * NPI numbers consist of 9 numeric digits followed by one numeric check 
                digit, for a total of 10 numeric digits.

              * The NPI numbers check digit is calculated using the Luhn Formula algorithm 
                for Modulus 10 "double-add-double".

              * NPIs will initially be issued with the first digit being either 1 or 2
            */
            bool isValid = !string.IsNullOrWhiteSpace(npi) &&
                           npi.All(char.IsDigit) &&
                           npi.Length == 10 &&
                           (npi[0] == '1' || npi[0] == '2');
            if (isValid)
            {
                var first9 = npi.Substring(0, 9);
                var checkDigit = int.Parse(npi.Substring(9, 1));
                var calcCheckDigit = GetNpiCheckDigit(first9);
                isValid = (calcCheckDigit == checkDigit);
            }

            return isValid;
        }

        private int GetNpiCheckDigit(string first9)
        {
            /*
             * Luhn algorithm - Note that this is a standard Luhn that is modified
                                specifically for 10 digit NPI numbers.

                Step 1 - Starting at the right most digit, which should be the check 
                         digit. Double the value of every second digit.
                Step 2 - Take the sum of all the individual digits.
                Step 2a- If a doubled value is 2-digits (i.e. 8 was doubled to 16), 
                         you add the two digits together (i.e. the doubled value of 
                         16 would be: 1 + 6)
                Step 3 - Because the NPI is 10-position, then add the constant 24,
                         to account for the "80840" prefix. 
                         Future: If the NPI is 15-position, do nothing. 
                         NOTE: When an NPI is used as a card issuer identifier on a 
                         standard health identification card, it is preceded by the 
                         prefix 80840, in which 80 indicates health applications and 
                         840 indicates the United States. The complete number would 
                         be 80840123456789 for an NPI of 123456789.
                Step 4 - Take the units digit (i.e. if the number was 67, you would 
                         take 7).
                Step 5 - Subtract the units digit from 10. (Do this step only if the 
                         units digit isn't zero)
                Step 6 - The resulting number is the check digit.             
             */

            var result = first9.Select(ch => (int)char.GetNumericValue(ch))
                               .Select((num, i) => i % 2 != 0
                                   ? num
                                   : ((num *= 2) >= 10 ? (num % 10) + 1 : num))
                               .Sum();
            // There are 2 types of NPI numbers, 10 and 15 digits. The only
            // difference is that the 15 digit has an "80840" prepended to 
            // it. Because of this, a 10-position NPI needs to add the
            // constant 24, to account for the "80840" that is missing.
            result += 24;

            result = 10 - (result % 10);
            return (result < 10) ? result : 0;
        }

        private bool IsValidPlaceOfService(string placeOfServiceCode)
        {
            // TODO:
            return true;
        }

        private List<DiagnosisCodeOrder> GetDiagnosisCodeOrder(List<ClaimChargeEntryEntity> claimChargeEntries,
                                                                           List<Diagnosis> authDiagCodeOrder, int accountInfoId)
        {
            var order = 1;

            var authDxInUseOnClaim = authDiagCodeOrder?.Where(dco => claimChargeEntries.Any(cce => cce.DiagnosisCode != null && cce.DiagnosisCode.Equals(dco.diagnosisCode, StringComparison.InvariantCultureIgnoreCase)));

            var diagChargeEntries = new List<ClaimChargeEntryEntity>();

            if (authDxInUseOnClaim != null)
            {
                diagChargeEntries = claimChargeEntries.Where(x => authDxInUseOnClaim.Select(y => y.diagnosisCode).Contains(x.DiagnosisCode)).ToList();
            }
            else
            {
                diagChargeEntries = claimChargeEntries;
            }
            var primaryCode = claimChargeEntries[0]?.DiagnosisCode;
            var diagType = new DiagnosisCode(primaryCode, GetDiagnosisCodeType(accountInfoId, primaryCode).Result);
            var dxInUserOnClaim = diagChargeEntries.Select(cce => new DiagnosisCodeOrder(cce, diagType, _endOfTimeDate, order));
            return dxInUserOnClaim.ToList();
        }

        private async Task<RenderingStaffMemberInfo> GetRenderingStaffInfoFromAppt(ClaimEntity claim,
                                                                         List<AppointmentRethinkModel> appointments)
        {
            // First look through associated appointments 
            var result = new RenderingStaffMemberInfo();
            foreach (var appt in appointments)
            {
                result.StaffMember = await _rethinkServices.GetStaffMember(claim.AccountInfoId, appt.staffId); ;

                result.PropagatingStaffMember = appt.propagatingStaffMemberId.HasValue
                                                  ? await _rethinkServices.GetPropagatingStaffMemberById(appt.propagatingStaffMemberId.Value)
                                                  : null;
                if (result.StaffMember != null)
                {
                    break;
                }
            }

            return result;
        }

        private ClaimSubmissionEntity CloneClaimSubmissionFor(ClaimEntity claim,
                                                              ClaimSubmissionEntity claimSubmission,
                                                              ClaimFrequencyType frequencyType,
                                                              ClaimSubmissionType submissionType,
                                                              ClaimDocumentType documentType,
                                                              ResponsibilitySequenceType responsibilitySequence)
        {
            List<string> additionalPropertyNamesToSkip = new List<string> { "Claim", "ClaimValidationErrors" };

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


        private async Task AddClaimHistory(ClaimEntity claim, ClaimAction action, ClaimHistoryAction historyAction, string value = null)
        {
            await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
            {
                ClaimId = claim.Id,
                MemberId = claim.MemberId,
                Mode = ClaimActionMode.System,
                ClaimAction = action,
                ClaimHistoryAction = historyAction,
                NewValue = value,
            });
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
        private async Task<DiagnosisCode> LookupDiagnosisCode(string diagnosisCode, int accountInfoId)
        {
            var diagnosisCodeToMatch = diagnosisCode.ToUpper();
            var dxCodes = await _rethinkServices.GetDiagnosisByCodeAsync(accountInfoId, diagnosisCodeToMatch);

            return dxCodes != null ? new DiagnosisCode(dxCodes.diagnosisCode.ToUpper(), dxCodes.diagnosisTypeId) : null;

        }

        public async Task<PayerDetailsModel> GetPayerDetails(int funderId)
        {
            return await _rethinkServices.GetPayerDetails(funderId);
        }

        /// <summary>
        /// This method takes the diagnosis code passed in and looks it up in the "master" list (e.g. not account specific list) to
        /// determine the type. This solves the problem where they have a "custom" code that is really an ICD9/10 code.
        /// </summary>
        /// <param name="diagnosisCodeToMatch"></param>
        /// <returns></returns>
        private async Task<DiagnosisTypes> GetDiagnosisCodeType(int accountInfoId, string diagnosisCodeToMatch)
        {

            var dxCode = await LookupDiagnosisCode(diagnosisCodeToMatch, accountInfoId);
            var dxType = (dxCode == null || dxCode.DiagnosisType == DiagnosisTypes.Custom) ? DiagnosisTypes.ICD10
                                                                                           : dxCode.DiagnosisType;

            return dxType;
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

        private string GetConfirmationType(int? confId)
        {
            switch (confId)
            {
                case null: return null;
                case 1: return "Y";
                case 2: return "N";
                default: return "W";
            }
        }

        private string GetGender(int? genderId)
        {
            return (genderId.HasValue ? GetGender(genderId.Value) : null);
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
        #endregion
    }
}