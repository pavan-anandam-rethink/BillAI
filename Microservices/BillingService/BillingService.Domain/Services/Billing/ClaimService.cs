using AutoMapper;
using BillingService.Domain.DataObjects.Base;
using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.Extensions;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Billing.ChangeTracking;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.Funders;
using BillingService.Domain.Models.PaymentPosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.Validation;
using Rethink.Services.Common.Entities.Billing.Claim.WriteOff;
using Rethink.Services.Common.Entities.Billing.PatientInvoice;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Models.Clients;
using Rethink.Services.Common.Models.RethinkDataEntityClasses;
using Rethink.Services.Common.Services;
using Rethink.Services.Common.Utils;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ClientFunderModel = BillingService.Domain.Models.Funders.ClientFunderModel;

namespace BillingService.Domain.Services.Billing
{
    public sealed class ClaimService : BaseService, IClaimService
    {

        private const string deniedReasonCodeKey = "deniedReasonCodes";
        private const string externalCodesCacheKey = "externalCodes";
        private const int cacheExpiration = 60;
        private const string statesCacheKey = "allStates";          // Cache key for GetStatesAsync method.
        private static readonly TimeSpan statesCacheDuration = TimeSpan.FromHours(24); // 24 hours
        private const int chunkSize = 100;
        private const string ErrorClientDeleted = "This claim cannot be opened because the associated client has been deleted from the system.";
        private const string ErrorNoFunderDetails = "Claim Submission Error: No funder details found";
        private const string ErrorNoClientFunderDetails = "Claim Submission Error: No client funder details found";
        private const string ErrorBillingProviderMissionAtLocation = "Billing Provider is not set for the selected Location.";

        private readonly IRepository<BillingDbContext, ClaimNoteEntity> _claimNoteRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimEntity> _paymentClaimRepository;
        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private readonly IRepository<BillingDbContext, ClaimChargeEntryEntity> _claimChargeEntryRepository;
        private readonly IRepository<BillingDbContext, ClaimAppointmentLinkEntity> _claimAppointmentLinkRepository;
        private readonly IRepository<BillingDbContext, MemberViewSettingEntity> _memberViewSettingRepository;
        private readonly IRepository<BillingDbContext, ClaimValidationErrorEntity> _claimValidationErrorRepository;
        private readonly IRepository<BillingDbContext, ClaimErrorMessageEntity> _claimErrorMessageRepository;
        private readonly IRepository<BillingDbContext, ClaimErrorCategoryEntity> _claimErrorCategoryRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineEntity> _paymentClaimServiceLineRepository;
        private readonly IRepository<BillingDbContext, ClaimDiagnosisCodeEntity> _claimDiagnosisCodeRepository;
        private readonly IRepository<BillingDbContext, ClaimSubmissionEntity> _claimSubmissionRepository;
        private readonly IRepository<BillingDbContext, ClearingHouseResponseDetailsEntity> _clearingHouseResponseRepository;
        private readonly IRepository<BillingDbContext, PaymentEntity> _paymentRepository;
        private readonly IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry> _claimAppointmentChargeEntryEntityRepository;
        private readonly IRepository<BillingDbContext, ClaimWriteOffEntity> _claimWriteOffRepository;
        private readonly IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity> _claimChargeEntryWriteOffRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> _paymentClaimServiceLineAdjustmentRepository;
        private readonly IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity> _claimSubmissionServiceLineRepository;
        private readonly IRepository<BillingDbContext, ClaimAttachmentEntity> _claimAttachmentRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimAdjustmentEntity> _claimAdjustmentRepository;
        private readonly IRepository<BillingDbContext, PatientInvoiceDetailsEntity> _patientInvoiceDetailsRepository;
        private readonly IRepository<BillingDbContext, CarcCodeEntity> _carcCodeRepository;   
        private readonly IRepository<BillingDbContext, ClaimFlagReasonMaster> _claimFlagReasonMasterRepository;
        private readonly IRepository<BillingDbContext, ClaimFlagTransaction> _claimFlagTransactionRepository;
        private readonly IRepository<BillingDbContext, ClaimBillingProviderEntity> _claimBillingProviderRepository;
        private readonly ICacheService _cacheService;
        private readonly IClaimManagerService _claimManagerService;
        private readonly IClaimValidationService _claimValidationService;
        private readonly IClaimHistoryService _claimHistoryService;
        private readonly IMapper _mapper;
        private readonly IDbHelper<BillingDbContext> _dbHelper;
        private readonly IClearingHouseService _clearingHouseService;
        private readonly IClaimChangeTrackingService _claimChangeTrackingService;
        private readonly IClaimVersionService _claimVersionService;
        private readonly IClaimUpdateService _claimUpdateService;

        private readonly IMessageBus _bus;
        private readonly ILogger<ClaimService> _logger;
        private readonly IRethinkMasterDataMicroServices _rethinkServices;

        private List<ClaimHistoryAction> billedHistoryActions = new List<ClaimHistoryAction>() {
                ClaimHistoryAction.BilledElectronically,
                ClaimHistoryAction.BilledInvoice,
                ClaimHistoryAction.BilledPaper
        };
        List<ClaimTransactionModel> claimTransactionData = [];
        private readonly IRepository<BillingDbContext, StateEntity> _stateRepository;
        private readonly IRepository<BillingDbContext, ExternalCodeEntity> _externalCodeRepository;

        public ClaimService(
            IRepository<BillingDbContext, PaymentClaimEntity> paymentClaimRepository,
            IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IRepository<BillingDbContext, ClaimChargeEntryEntity> claimChargeEntryRepository,
            IRepository<BillingDbContext, ClaimAppointmentLinkEntity> claimAppointmentLinkRepository,
            IRepository<BillingDbContext, MemberViewSettingEntity> memberViewSettingRepository,
            IRepository<BillingDbContext, ClaimSubmissionEntity> claimSubmissionRepository,
            IRepository<BillingDbContext, ClaimDiagnosisCodeEntity> claimDiagnosisCodeRepository,
            IRepository<BillingDbContext, ClaimValidationErrorEntity> claimValidationErrorRepository,
            IRepository<BillingDbContext, ClaimErrorMessageEntity> claimErrorMessageRepository,
            IRepository<BillingDbContext, ClaimErrorCategoryEntity> claimErrorCategoryRepository,
            IRepository<BillingDbContext, PaymentClaimServiceLineEntity> paymentClaimServiceLineRepository,
            IRepository<BillingDbContext, ClearingHouseResponseDetailsEntity> clearingHouseResponseRepository,
            IRepository<BillingDbContext, ClaimNoteEntity> ClaimNoteRepository,
            IRepository<BillingDbContext, PaymentEntity> paymentRepository,
            IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry> claimAppointmentChargeEntryEntityRepository,
            IRepository<BillingDbContext, ClaimWriteOffEntity> claimWriteOffRepository,
            IRepository<BillingDbContext, ClaimChargeEntryWriteOffEntity> claimChargeEntryWriteOffRepository,
            IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> paymentClaimServiceLineAdjustmentRepository,
            IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity> claimSubmissionServiceLineRepository,
            IRepository<BillingDbContext, ClaimAttachmentEntity> claimAttachmentRepository,
            IRepository<BillingDbContext, PaymentClaimAdjustmentEntity> claimAdjustmentRepository,
            IRepository<BillingDbContext, PatientInvoiceDetailsEntity> patientInvoiceDetailsRepository,
            IRepository<BillingDbContext, CarcCodeEntity> carcCodeRepository,
            ICacheService cacheService,
            IRethinkMasterDataMicroServices rethinkServices,
            IClaimHistoryService claimHistoryService,
            IClaimManagerService claimManagerService,
            IClaimValidationService claimValidationService,
            IClearingHouseService clearingHouseService,      
            IClaimChangeTrackingService claimChangeTrackingService,
            IClaimVersionService claimVersionService,
            IClaimUpdateService claimUpdateService,
            IMapper mapper,
            IDbHelper<BillingDbContext> dbHelper,
            IMessageBus bus,
            ILogger<ClaimService> logger,      
            IRepository<BillingDbContext, ClaimFlagReasonMaster> claimFlagReasonMasterRepository,
            IRepository<BillingDbContext, ClaimFlagTransaction> claimFlagTransactionRepository,
            IRepository<BillingDbContext, StateEntity> stateRepository,
            IRepository<BillingDbContext, ClaimBillingProviderEntity> claimBillingProviderRepository,
            IRepository<BillingDbContext, ExternalCodeEntity> externalCodeRepository)
        {
            _claimAppointmentChargeEntryEntityRepository = claimAppointmentChargeEntryEntityRepository;
            _rethinkServices = rethinkServices;
            _paymentRepository = paymentRepository;
            _paymentClaimRepository = paymentClaimRepository;
            _claimRepository = claimRepository;
            _claimNoteRepository = ClaimNoteRepository;
            _claimChargeEntryRepository = claimChargeEntryRepository;
            _claimAppointmentLinkRepository = claimAppointmentLinkRepository;
            _memberViewSettingRepository = memberViewSettingRepository;
            _claimWriteOffRepository = claimWriteOffRepository;
            _claimChargeEntryWriteOffRepository = claimChargeEntryWriteOffRepository;
            _mapper = mapper;
            _dbHelper = dbHelper;
            _claimSubmissionRepository = claimSubmissionRepository;
            _claimDiagnosisCodeRepository = claimDiagnosisCodeRepository;
            _claimValidationErrorRepository = claimValidationErrorRepository;
            _claimErrorMessageRepository = claimErrorMessageRepository;
            _claimErrorCategoryRepository = claimErrorCategoryRepository;
            _claimHistoryService = claimHistoryService;
            _claimManagerService = claimManagerService;
            _claimValidationService = claimValidationService;
            _clearingHouseService = clearingHouseService;
            _paymentClaimServiceLineRepository = paymentClaimServiceLineRepository; 
            _claimChangeTrackingService = claimChangeTrackingService;
            _clearingHouseResponseRepository = clearingHouseResponseRepository;
            _claimVersionService = claimVersionService;
            _claimUpdateService = claimUpdateService;
            _bus = bus;
            _paymentClaimServiceLineAdjustmentRepository = paymentClaimServiceLineAdjustmentRepository;
            _claimSubmissionServiceLineRepository = claimSubmissionServiceLineRepository;
            _claimAttachmentRepository = claimAttachmentRepository;
            _claimAdjustmentRepository = claimAdjustmentRepository;
            _patientInvoiceDetailsRepository = patientInvoiceDetailsRepository;
            _carcCodeRepository = carcCodeRepository;
            _cacheService = cacheService;
            _claimFlagReasonMasterRepository = claimFlagReasonMasterRepository;
            _claimFlagTransactionRepository = claimFlagTransactionRepository;
            _claimBillingProviderRepository = claimBillingProviderRepository;
            _stateRepository = stateRepository;
            _logger = logger;
            _externalCodeRepository = externalCodeRepository;
        }

        public async Task<ActionResponse> GetClaimByIdentifierAsync(string claimIdentifier, int accountInfoId)
        {
            var claim = await _claimRepository.Query()
                .Include(x => x.ClaimHistory)
                .Include("ClaimChargeEntries.ChargePayments")
                .FirstOrDefaultAsync(x => x.AccountInfoId == accountInfoId && x.DateDeleted == null && x.ClaimIdentifier == claimIdentifier);

            if (claim == null)
            {
                return ActionResponse.FailResult(ValidationErrorMessages.NotFound(EntityNames.Claim));
            }

            var histories = claim.ClaimHistory.OrderByDescending(ch => ch.DateCreated).ToList();

            var claimItem = _mapper.Map<ClaimItem>(claim);

            claimItem.BilledPreviously = !(claim.ClaimStatus.Equals(ClaimStatus.PendingReview)
                || claim.ClaimStatus.Equals(ClaimStatus.ReadyToBill)
                || claim.ClaimStatus.Equals(ClaimStatus.Rebill));
            //claimItem.BilledPreviously = histories.Any(hi => billedHistoryActions.Contains(hi.ClaimHistoryAction));

            if (claim.ClaimStatus == ClaimStatus.ReadyToBill)
            {
                if (histories.Count() > 0 && histories[0].ClaimAction == ClaimAction.Rebill)
                {
                    claimItem.ForbidAddAppointment = ModifyAppointmentsPermission.Allow;
                }
                else
                {
                    claimItem.ForbidAddAppointment = ModifyAppointmentsPermission.Forbid;
                }
            }
            else if (claimItem.BilledPreviously)
            {
                claimItem.ForbidAddAppointment = ModifyAppointmentsPermission.Warn;
            }
            else
            {
                claimItem.ForbidAddAppointment = ModifyAppointmentsPermission.Allow;
            }

            claimItem.IsManual = claim.ClaimHistory
                .Any(x => x.ClaimHistoryAction == ClaimHistoryAction.ClaimCreated
                        && x.Mode == ClaimActionMode.User);

            var result = _mapper.Map<ClaimModel>(claimItem);

            return ActionResponse.SuccessResult(result);
        }

        public async Task<List<int>> GetIdsForAccountAsync(int accountInfoId)
        {
            var query = _claimRepository.Query()
                .Where(x => x.AccountInfoId == accountInfoId && x.DateDeleted == null).Select(x => x.Id);

            var claimIds = await query.ToListAsync();

            return claimIds;
        }

        public async Task<List<ClaimDropdownModel>> GetAccountClaimByIdOrPatientNameAsync(ClaimSearchModel model)
        {
            var filteredClaims = new List<ClaimDropdownModel>();

            var payment = await _paymentRepository
                .Query()
                .Where(p => p.Id == model.PaymentId)
                .Include(c => c.PaymentClaims)
                .FirstOrDefaultAsync();

            var claims = await _claimRepository
                .Query()
                .Where(x => x.AccountInfoId == model.AccountInfoId
                                && x.DateDeleted == null
                                && x.ClaimStatus != ClaimStatus.PendingReview)
                .Select(c => new ClaimDropdownModel()
                {
                    Id = c.Id,
                    ClaimIdentifier = c.ClaimIdentifier,
                    ChildProfileId = c.ChildProfileId,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate
                })
                .ToListAsync();

            int.TryParse(model.SearchString, out var searchClaimId);

            var patientList = await _rethinkServices.GetChildProfileByName(model.AccountInfoId, model.SearchString);

            var patients = patientList
                .Select(p => new ClaimDropdownModel()
                {
                    ChildProfileId = p.Id,
                    PatientFirstName = p.FirstName,
                    PatientMiddleName = p.MiddleName,
                    PatientLastName = p.LastName
                })
                .ToList();

            var patientsIds = patients.Select(x => x.ChildProfileId);

            var attachedClaimsToPayment = payment.PaymentClaims.Where(x => x.DateDeleted == null).Select(x => x.ClaimId).ToList();

            filteredClaims = claims.Where(x => !attachedClaimsToPayment.Contains(x.Id) && patientsIds.Contains(x.ChildProfileId)).ToList();

            if (searchClaimId > 0)
            {
                var additionalClaim = claims.Where(x => x.Id == searchClaimId).FirstOrDefault();

                if (additionalClaim != null)
                {
                    var patient = await _rethinkServices.GetChildProfile(model.AccountInfoId, additionalClaim.ChildProfileId);
                    if (patient != null)
                    {
                        var additionalPatient = new ClaimDropdownModel
                        {
                            ChildProfileId = patient.id,
                            PatientFirstName = patient.name.firstName,
                            PatientMiddleName = patient.name.middleName,
                            PatientLastName = patient.name.lastName
                        };
                        patients.Add(additionalPatient);
                    }
                    filteredClaims.Add(additionalClaim);
                }
            }

            // code to search by claim identifier
            if (filteredClaims.Count == 0)
            {
                var additionalClaim = claims.Where(x => x.ClaimIdentifier.Contains(model.SearchString)).FirstOrDefault();

                if (additionalClaim != null)
                {
                    var patient = await _rethinkServices.GetChildProfile(model.AccountInfoId, additionalClaim.ChildProfileId);
                    if (patient != null)
                    {
                        var additionalPatient = new ClaimDropdownModel
                        {
                            ChildProfileId = patient.id,
                            PatientFirstName = patient.name.firstName,
                            PatientMiddleName = patient.name.middleName,
                            PatientLastName = patient.name.lastName
                        };
                        patients.Add(additionalPatient);
                    }
                    filteredClaims.Add(additionalClaim);
                }
            }

            var claimSub = new List<ClaimDropdownModel>();
            foreach (var claim in filteredClaims)
            {
                var submissionClaims = await _claimSubmissionRepository.Query().Where(x => x.ClaimId == claim.Id && x.DateDeleted == null).Select(x => x.Id).FirstOrDefaultAsync();
                if (submissionClaims != 0)
                {
                    claimSub.Add(claim);

                    var connectedPatient = patients.FirstOrDefault(p => p.ChildProfileId == claim.ChildProfileId);
                    if (connectedPatient != null)
                    {
                        claim.PatientName = FullNameExt.GetFullName(connectedPatient.PatientFirstName, connectedPatient.PatientMiddleName,
                            connectedPatient.PatientLastName);
                    }
                }
            }

            filteredClaims = claimSub;

            var result = filteredClaims.OrderBy(c => c.PatientName).ToList();

            return result;
        }

        // RHD-32726 making obsolete this method for performance tab as we are doing optimization to load the claims in batch and remove the foreach loop, we will remove this method after testing the new optimized method
      

        [Obsolete("GetClaimHeadersAsync")]
        public async Task<ClaimHeaderModelResponseModel> GetClaimHeadersAsyncold(ClaimGetRequestSortFilterWithUserInfo model)
        {
            var sqlParams = new List<SqlParameter>
            {
                new ("AccountInfoId", model.AccountInfoId),
                new ("Skip", model.Skip),
                new ("Take", model.Take)
            };

            var parameters = new List<SqlParameter>
            {
                new SqlParameter("AccountInfoId", model.AccountInfoId),
            };

            var filters = model.Filters;
            if (filters != null)
            {
                sqlParams.Add(new SqlParameter("ClaimNumber", filters.ClaimNumber));
                sqlParams.Add(new SqlParameter("PatientIds", filters.PatientIds));
                sqlParams.Add(new SqlParameter("ReasonCode", filters.ReasonCode));
                sqlParams.Add(new SqlParameter("FunderIds", filters.FunderIds));
                sqlParams.Add(new SqlParameter("AssigneeIds", filters.AssigneeIds));
                sqlParams.Add(new SqlParameter("LocationIds", filters.LocationIds));
                sqlParams.Add(new SqlParameter("ClaimIds", filters.ClaimIds));
                sqlParams.Add(new SqlParameter("ReasonIds", filters.ReasonIds));
                sqlParams.Add(new SqlParameter("BalanceFrom", filters.BalanceFrom));
                sqlParams.Add(new SqlParameter("BalanceTo", filters.BalanceTo));
                sqlParams.Add(new SqlParameter("BilledFrom", filters.BilledFrom));
                sqlParams.Add(new SqlParameter("BilledTo", filters.BilledTo));
                sqlParams.Add(new SqlParameter("PatientResponsibilityFrom", filters.PatientResponsibilityFrom));
                sqlParams.Add(new SqlParameter("PatientResponsibilityTo", filters.PatientResponsibilityTo));
                sqlParams.Add(new SqlParameter("DateOfServiceFrom", filters.DateOfServiceFrom));
                sqlParams.Add(new SqlParameter("DateOfServiceTo", filters.DateOfServiceTo));
                sqlParams.Add(new SqlParameter("RenderingProviderIds", filters.RenderingProviderIds));
                sqlParams.Add(new SqlParameter("StatusIds", filters.StatusIds));
                sqlParams.Add(new SqlParameter("Tab", filters.Tab));
                sqlParams.Add(new SqlParameter("ShowVoided", filters.ShowVoided));
                sqlParams.Add(new SqlParameter("ValidationIds", filters.ValidationIds));
                sqlParams.Add(new SqlParameter("ResponseIds", filters.ResponseIds));
                parameters.Add(new SqlParameter("ClaimNumber", filters.ClaimNumber));
                parameters.Add(new SqlParameter("ShowVoided", filters.ShowVoided));

            }

            var sort = model.SortingModels.FirstOrDefault();
            if (sort != null && sort.Dir != string.Empty)
            {
                sqlParams.Add(new SqlParameter("OrderField", sort.Field));
                sqlParams.Add(new SqlParameter("OrderDir", sort.Dir == "desc"));
            }
            ;

            var result =
                await _dbHelper.ExecuteListAsync<ClaimHeaderModel>("GetClaimsByAccountInfoId",
                    sqlParams);

            var memberdetails = await _rethinkServices.GetStaffMemberList(model.AccountInfoId);
            var memberIds = memberdetails.Select(m => m.memberId).ToList();
            var memberNamesDict = memberdetails.ToDictionary(m => m.memberId, m => m.name);

            var renderingProviders = await _rethinkServices.GetRenderingProvidersAsync(model.AccountInfoId);

            //Get Account Detail for Test Account
            var accountDetail = await _rethinkServices.GetAccountReturningEntityAsync(model.AccountInfoId, true);
            var accountType = accountDetail.AccountType;


            // Get the patients from BH Service.
            var clientUsersList = await _rethinkServices.GetChildProfilesForAccount(model.AccountInfoId);
 
            #region Added new Optimize code to remove the foreach loop and use dictionary for lookup to improve performance
            var clientUsersDict = clientUsersList.ToDictionary(x => x.Id);
            var isTestAccount = accountType == 1;

            foreach (var claim in result)
            {
                clientUsersDict.TryGetValue(claim.ChildProfileId, out var patientDetail);

                if (string.IsNullOrWhiteSpace(claim?.PatientName) && patientDetail?.DateDeleted == null)
                {
                    claim.PatientName = patientDetail != null
                        ? $"{patientDetail.FirstName} {patientDetail.MiddleName} {patientDetail.LastName}"
                        : string.Empty;
                }

                if (patientDetail == null)
                {
                    claim.IsClientDeleted = true;
                }

                claim.AssigneeName = memberNamesDict.TryGetValue(claim.AssigneeId, out var assigneeName)
                    ? FullNameExt.GetFullName(assigneeName.firstName, null, assigneeName.lastName)
                    : "Unassigned";

                claim.IsTestAccount = isTestAccount;
                var auth = await _rethinkServices.GetChildProfileAuthorizationByClientId(model.AccountInfoId, claim.ChildProfileId, claim.ChildProfileAuthorizationId );
                var isOverrideProvider = auth?.renderingProviderStaffId != null;

                if (isOverrideProvider)
                {
                    claim.RenderingProviderName = renderingProviders.FirstOrDefault(x=>x.StaffMemberId == auth.renderingProviderStaffId.Value)?.Name;
                }
                else
                {
                    claim.RenderingProviderName = claim.RenderingProviderName;
                    
                }
            }

            #endregion

            var totalCount = result.FirstOrDefault()?.TotalCount ?? 0;

            var claimsCountResult =
                (await _dbHelper.ExecuteListAsync<ClaimsCountModel>("GetClaimsCount",
                    parameters)).FirstOrDefault();

            switch (filters.Tab)
            {
                case (int)ClaimsTab.PendingReview:
                    claimsCountResult.PendingReviewTotalCount = totalCount;
                    break;
                case (int)ClaimsTab.ReadyToBill:
                    claimsCountResult.ReadyToBillTotalCount = totalCount;
                    break;
                case (int)ClaimsTab.BilledPending:
                    claimsCountResult.BillingPendingTotalCount = totalCount;
                    break;
                case (int)ClaimsTab.Completed:
                    claimsCountResult.ClosedTotalCount = totalCount;
                    break;
                case (int)ClaimsTab.Rejected:
                    claimsCountResult.RejectedTotalCount = totalCount;
                    break;
                case (int)ClaimsTab.Denied:
                    claimsCountResult.DeniedTotalCount = totalCount;
                    break;
                case (int)ClaimsTab.Flagged:
                    claimsCountResult.FlaggedTotalCount = totalCount;
                    break;
            }

            var response = new ClaimHeaderModelResponseModel()
            {
                Data = result,
                TotalCount = totalCount,
                ClaimsCount = claimsCountResult
            };



            // Refactored to batch load all claims in one query for performance
            var claimNumbers = response.Data.Select(d => d.ClaimNumber).ToList();
            var claims = await _claimRepository.Query()
                .Where(x => x.AccountInfoId == model.AccountInfoId && x.DateDeleted == null && claimNumbers.Contains(x.ClaimIdentifier))
                .Include(x => x.ClaimHistory)
                .ToListAsync();

            var claimsDict = claims.GroupBy(c => c.ClaimIdentifier).ToDictionary(g => g.Key, g => g.First());

            response.Data.ForEach(data =>
            {
                data.IsManual = claimsDict.TryGetValue(data.ClaimNumber, out var claim)
                && claim.ClaimHistory.Any(x => x.ClaimHistoryAction == ClaimHistoryAction.ClaimCreated && x.Mode == ClaimActionMode.User);
            });

            response.Data = response.Data.ApplySorting(model.SortingModels).ToList();
            return response;
        }

        public async Task<ClaimHeaderModelResponseModel> GetClaimHeadersAsync(ClaimGetRequestSortFilterWithUserInfo model)
        {          
            _logger.LogInformation("GetClaimHeadersAsync started");

            var sqlParams = new List<SqlParameter>
                {
                    new("AccountInfoId", model.AccountInfoId),
                    new("Skip", model.Skip),
                    new("Take", model.Take)
                };

            var parameters = new List<SqlParameter>{ new("AccountInfoId", model.AccountInfoId) };

            var filters = model.Filters;

            if (filters != null)
            {
                sqlParams.Add(new SqlParameter("ClaimNumber", filters.ClaimNumber));
                sqlParams.Add(new SqlParameter("PatientIds", filters.PatientIds));
                sqlParams.Add(new SqlParameter("ReasonCode", filters.ReasonCode));
                sqlParams.Add(new SqlParameter("FunderIds", filters.FunderIds));
                sqlParams.Add(new SqlParameter("AssigneeIds", filters.AssigneeIds));
                sqlParams.Add(new SqlParameter("LocationIds", filters.LocationIds));
                sqlParams.Add(new SqlParameter("ClaimIds", filters.ClaimIds));
                sqlParams.Add(new SqlParameter("ReasonIds", filters.ReasonIds));
                sqlParams.Add(new SqlParameter("BalanceFrom", filters.BalanceFrom));
                sqlParams.Add(new SqlParameter("BalanceTo", filters.BalanceTo));
                sqlParams.Add(new SqlParameter("BilledFrom", filters.BilledFrom));
                sqlParams.Add(new SqlParameter("BilledTo", filters.BilledTo));
                sqlParams.Add(new SqlParameter("PatientResponsibilityFrom", filters.PatientResponsibilityFrom));
                sqlParams.Add(new SqlParameter("PatientResponsibilityTo", filters.PatientResponsibilityTo));
                sqlParams.Add(new SqlParameter("DateOfServiceFrom", filters.DateOfServiceFrom));
                sqlParams.Add(new SqlParameter("DateOfServiceTo", filters.DateOfServiceTo));
                sqlParams.Add(new SqlParameter("RenderingProviderIds", filters.RenderingProviderIds));
                sqlParams.Add(new SqlParameter("StatusIds", filters.StatusIds));
                sqlParams.Add(new SqlParameter("Tab", filters.Tab));
                sqlParams.Add(new SqlParameter("ShowVoided", filters.ShowVoided));
                sqlParams.Add(new SqlParameter("ValidationIds", filters.ValidationIds));
                sqlParams.Add(new SqlParameter("ResponseIds", filters.ResponseIds));

                parameters.Add(new SqlParameter("ClaimNumber", filters.ClaimNumber));
                parameters.Add(new SqlParameter("ShowVoided", filters.ShowVoided));
            }

            var sort = model.SortingModels.FirstOrDefault();
            if (sort != null && !string.IsNullOrWhiteSpace(sort.Dir))
            {
                sqlParams.Add(new SqlParameter("OrderField", sort.Field));
                sqlParams.Add(new SqlParameter("OrderDir", sort.Dir == "desc"));
            }

            var claimsTask = _dbHelper.ExecuteListAsync<ClaimHeaderModel>("GetClaimsByAccountInfoId", sqlParams);

            var memberTask = _rethinkServices.GetStaffMemberList(model.AccountInfoId);
            var providerTask = _rethinkServices.GetRenderingProvidersAsync(model.AccountInfoId);
            var accountTask = _rethinkServices.GetAccountReturningEntityAsync(model.AccountInfoId, true);
            var childProfilesTask = _rethinkServices.GetChildProfilesForAccount(model.AccountInfoId);

            await Task.WhenAll(claimsTask, memberTask, providerTask, accountTask, childProfilesTask).ConfigureAwait(false);

            var result = (await claimsTask.ConfigureAwait(false)).ToList();

            var memberNamesDict = (await memberTask.ConfigureAwait(false)).ToDictionary(
                x => x.memberId,
                x => x.name);

            var providerDict = (await providerTask.ConfigureAwait(false)).ToDictionary(
                x => x.StaffMemberId,
                x => x.Name);

            var clientUsersDict = (await childProfilesTask.ConfigureAwait(false)).ToDictionary(x => x.Id);

            var isTestAccount = (await accountTask.ConfigureAwait(false)).AccountType == 1;

            var uniqueAuthRequests = result.Select(x => (ClientId: x.ChildProfileId,AuthorizationId: x.ChildProfileAuthorizationId)).Distinct().ToList();

            var authDict = await GetChildProfileAuthorizationsByClientIdsAsync(model.AccountInfoId,uniqueAuthRequests);
                       
            foreach (var claim in result)
            {
                clientUsersDict.TryGetValue(
                    claim.ChildProfileId,
                    out var patientDetail);

                if (string.IsNullOrWhiteSpace(claim.PatientName) &&
                    patientDetail?.DateDeleted == null)
                {
                    claim.PatientName = patientDetail != null
                        ? $"{patientDetail.FirstName} {patientDetail.MiddleName} {patientDetail.LastName}"
                        : string.Empty;
                }

                claim.IsClientDeleted = patientDetail == null;

                claim.AssigneeName =
                    memberNamesDict.TryGetValue(
                        claim.AssigneeId,
                        out var assignee)
                    ? FullNameExt.GetFullName(
                        assignee.firstName,
                        null,
                        assignee.lastName)
                    : "Unassigned";

                claim.IsTestAccount = isTestAccount;

                authDict.TryGetValue(
                    (claim.ChildProfileId, claim.ChildProfileAuthorizationId),
                    out var auth);

                if (auth?.renderingProviderStaffId != null &&
                    providerDict.TryGetValue(
                        auth.renderingProviderStaffId.Value,
                        out var providerName))
                {
                    claim.RenderingProviderName = providerName;
                }
            }

            var totalCount = result.FirstOrDefault()?.TotalCount ?? 0;

            var claimNumbers = result.Select(x => x.ClaimNumber).Distinct().ToList();

            var claimsCountTask = _dbHelper.ExecuteListAsync<ClaimsCountModel>("GetClaimsCount", parameters);
            var manualClaimIdentifiersTask = _claimRepository.Query()
                .AsNoTracking()
                .Where(x =>
                    x.AccountInfoId == model.AccountInfoId &&
                    x.DateDeleted == null &&
                    claimNumbers.Contains(x.ClaimIdentifier))
                .SelectMany(x => x.ClaimHistory
                    .Where(h =>
                        h.ClaimHistoryAction == ClaimHistoryAction.ClaimCreated &&
                        h.Mode == ClaimActionMode.User)
                    .Select(_ => x.ClaimIdentifier))
                .Distinct()
                .ToListAsync();

            await Task.WhenAll(claimsCountTask, manualClaimIdentifiersTask).ConfigureAwait(false);

            var claimsCountResult = (await claimsCountTask.ConfigureAwait(false)).FirstOrDefault();

            var manualClaimIdentifiers = await manualClaimIdentifiersTask.ConfigureAwait(false);

            var manualClaimsDict = manualClaimIdentifiers
                .ToDictionary(x => x, _ => true);

         
            if (claimsCountResult != null && filters != null)
            {
                switch (filters.Tab)
                {
                    case (int)ClaimsTab.PendingReview:
                        claimsCountResult.PendingReviewTotalCount = totalCount;
                        break;
                    case (int)ClaimsTab.ReadyToBill:
                        claimsCountResult.ReadyToBillTotalCount = totalCount;
                        break;
                    case (int)ClaimsTab.BilledPending:
                        claimsCountResult.BillingPendingTotalCount = totalCount;
                        break;
                    case (int)ClaimsTab.Completed:
                        claimsCountResult.ClosedTotalCount = totalCount;
                        break;
                    case (int)ClaimsTab.Rejected:
                        claimsCountResult.RejectedTotalCount = totalCount;
                        break;
                    case (int)ClaimsTab.Denied:
                        claimsCountResult.DeniedTotalCount = totalCount;
                        break;
                    case (int)ClaimsTab.Flagged:
                        claimsCountResult.FlaggedTotalCount = totalCount;
                        break;
                }
            }

            foreach (var claim in result)
            {
                claim.IsManual = manualClaimsDict.TryGetValue(
                    claim.ClaimNumber,
                    out var isManual) && isManual;
            }

            result = result.ApplySorting(model.SortingModels).ToList();
                      
            _logger.LogInformation("GetClaimHeadersAsync total completed for {result}", result.Count);

            return new ClaimHeaderModelResponseModel
            {
                Data = result,
                TotalCount = totalCount,
                ClaimsCount = claimsCountResult
            };
        }


        public async Task<Dictionary<(int ClientId, int AuthorizationId), ClientAuthorization>> GetChildProfileAuthorizationsByClientIdsAsync(int accountInfoId, List<(int ClientId, int AuthorizationId)> requests)
        {
            var authSw = Stopwatch.StartNew();
            _logger.LogInformation("GetClaimHeadersAsync-GetChildProfileAuthorizationsByClientIdsAsync Authorization batch start at {ElapsedMs} ms", authSw.ElapsedMilliseconds);

            var dict = new Dictionary<(int ClientId, int AuthorizationId), ClientAuthorization>();

            if (requests == null || requests.Count == 0)
            {
                authSw.Stop();
                return dict;
            }

            // One Health Plans list call per client (up to 500 auths) instead of one HTTP call per (client, auth) pair.
            var distinctClients = requests.Where(r => r.ClientId != 0).Select(r => r.ClientId).Distinct().ToList();
            var clientFetchGate = new SemaphoreSlim(8, 8);

            var batchTasks = distinctClients.Select(async clientId =>
            {
                await clientFetchGate.WaitAsync().ConfigureAwait(false);
                try
                {
                    var listModel = await _rethinkServices.GetClientAuthorizationsByClientId(accountInfoId, clientId).ConfigureAwait(false);
                    return (clientId, listModel?.data);
                }
                finally
                {
                    clientFetchGate.Release();
                }
            });

            var batchResults = await Task.WhenAll(batchTasks).ConfigureAwait(false);

            foreach (var (clientId, auths) in batchResults)
            {
                if (auths == null)
                {
                    continue;
                }

                foreach (var auth in auths)
                {
                    dict[(clientId, auth.id)] = auth;
                }
            }

            // ClientId 0 uses the "all authorizations" BH route — still resolve per requested id.
            var zeroClientRequests = requests.Where(r => r.ClientId == 0).Distinct().ToList();
            if (zeroClientRequests.Count > 0)
            {
                var fallbackGate = new SemaphoreSlim(8, 8);
                var fallbackTasks = zeroClientRequests.Select(async request =>
                {
                    await fallbackGate.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        var auth = await _rethinkServices.GetChildProfileAuthorizationByClientId(accountInfoId, request.ClientId, request.AuthorizationId).ConfigureAwait(false);
                        return (request.ClientId, request.AuthorizationId, auth);
                    }
                    finally
                    {
                        fallbackGate.Release();
                    }
                });

                foreach (var item in await Task.WhenAll(fallbackTasks).ConfigureAwait(false))
                {
                    dict[(item.ClientId, item.AuthorizationId)] = item.auth;
                }
            }

            // If an auth id was not present in the batched list response (e.g. paging), fetch individually.
            var missing = requests.Where(r => !dict.ContainsKey((r.ClientId, r.AuthorizationId))).ToList();
            if (missing.Count > 0)
            {
                var fillGate = new SemaphoreSlim(8, 8);
                var fillTasks = missing.Select(async request =>
                {
                    await fillGate.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        var auth = await _rethinkServices.GetChildProfileAuthorizationByClientId(accountInfoId, request.ClientId, request.AuthorizationId).ConfigureAwait(false);
                        return (request.ClientId, request.AuthorizationId, auth);
                    }
                    finally
                    {
                        fillGate.Release();
                    }
                });

                foreach (var item in await Task.WhenAll(fillTasks).ConfigureAwait(false))
                {
                    dict[(item.ClientId, item.AuthorizationId)] = item.auth;
                }
            }

            authSw.Stop();
            _logger.LogInformation("GetClaimHeadersAsync-GetChildProfileAuthorizationsByClientIdsAsync Authorization batch completed in {ElapsedMs} ms (clients={ClientCount}, pairs={PairCount})", authSw.ElapsedMilliseconds, distinctClients.Count, requests.Count);

            return dict;
        }

        public async Task<List<ClaimFilterOptionModel>> GetClaimPatientsAsync(ClaimFilterGetModel model)
        {
            var sqlParams = new List<SqlParameter>
            {
                new SqlParameter("AccountInfoId", model.AccountInfoId),
                new SqlParameter("Tab", model.Tab)
            };

            var patientName =
                await _dbHelper.ExecuteListAsync<ClaimClientFilterOptionModel>("GetClaimsPatientsFilters",
                    sqlParams);

            if (patientName == null) return new List<ClaimFilterOptionModel>();

            patientName = patientName.Select(s => new ClaimClientFilterOptionModel { Id = s.Id, Name = s.Name.Replace("  ", " ") }).ToList();

            var result = patientName.Select(x => new ClaimFilterOptionModel
            {
                Id = x.Id,
                Name = x.Name
            }).ToList();

            // Get the patients from BH Service.
            var clientUsersList = await _rethinkServices.GetChildProfilesForAccount(model.AccountInfoId);

            result.ForEach(client =>
            {
                if (string.IsNullOrWhiteSpace(client?.Name) || client.Name.Trim().Length < 1)
                {
                    var patientDetail = clientUsersList.FirstOrDefault(p => p.Id == client.Id && p.DateDeleted == null);
                    client.Name = patientDetail != null ? $"{patientDetail.FirstName} {patientDetail.MiddleName} {patientDetail.LastName}" : "Unknown";
                }
            });

            if (model.SearchValue != null)
                return result.Where(x => x.Name != null && x.Name.ToLower().Contains(model.SearchValue.ToLower())).ToList();
            else
                return result;
        }

        public async Task<List<ClaimFilterOptionModel>> GetClaimFundersAsync(ClaimFilterGetModel model)
        {
            var sqlParams = new List<SqlParameter>
            {
                new SqlParameter("AccountInfoId", model.AccountInfoId),
                new SqlParameter("Tab", model.Tab)
            };

            var result =
                await _dbHelper.ExecuteListAsync<ClaimFilterOptionModel>("GetClaimsFundersFilters",
            sqlParams);

            if (model.SearchValue != null)
                return result.Where(x => x.Name != null && x.Name.ToLower().Contains(model.SearchValue.ToLower())).ToList();
            else
                return result;
        }

        public async Task<List<ClaimFilterOptionModel>> GetClaimRenderingProvidersAsync(ClaimFilterGetModel model)
        {
            var sqlParams = new List<SqlParameter>
            {
                new SqlParameter("AccountInfoId", model.AccountInfoId),
                new SqlParameter("Tab", model.Tab)
            };

            var result =
                await _dbHelper.ExecuteListAsync<ClaimFilterOptionModel>("GetClaimsRPFilters",
                    sqlParams);

            if (model.SearchValue != null)
                return result.Where(x => x.Name != null && x.Name.ToLower().Contains(model.SearchValue.ToLower())).ToList();
            else
                return result;
        }

        public async Task<List<ClaimFilterOptionModel>> GetClaimTabStatusesAsync(ClaimFilterGetModel model)
        {
            var claimTabStatuses = await _claimRepository.Query()
                .Where(x => x.AccountInfoId == model.AccountInfoId &&
                       x.DateDeleted == null &&
                       x.ClaimIdentifier != null)
                .Where(FilterClaimsBySelectedTab(model.Tab))
                .Select(x => x.ClaimStatus)
                .Distinct()
                .ToListAsync();

            var result = claimTabStatuses
                .Select(status => new ClaimFilterOptionModel
                {
                    Id = (int)status,
                    Name = GetEnumDescription(status),

                })
                .OrderBy(x => x.Name)
                .ToList();

            return result;
        }

        public static string GetEnumDescription(Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());
            DescriptionAttribute[] attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
            if (attributes != null && attributes.Any())
            {
                return attributes.First().Description;
            }
            return value.ToString();
        }

        public async Task<List<ClaimFilterOptionModel>> GetClaimIdentifiersAsync(ClaimFilterGetModel model)
        {
            var result = await _claimRepository.Query()
                .Where(x => x.AccountInfoId == model.AccountInfoId &&
                            !x.DateDeleted.HasValue)
                .Where(FilterClaimsBySelectedTab(model.Tab))
                .Select(x => new ClaimFilterOptionModel
                {
                    Id = x.Id,
                    Name = x.ClaimIdentifier
                })
                .OrderBy(x => x.Name)
                .ToListAsync();

            return result;
        }

        public async Task<IQueryable<BillingClaimDetailsModel>> GetClaimChargesForAccountAsync(GetBillingClaimDetailsModel model)
        {

            var unitTypes = await _rethinkServices.GetUnitTypesAsync();

            var claim = await _claimRepository.Query()
                .Where(x => x.Id == model.ClaimId
                            && x.DateDeleted == null).FirstOrDefaultAsync();

            var accountInfoId = claim.AccountInfoId;
            var authorizationId = claim.AuthorizationId ?? 0;
            var renderingProviders = await _rethinkServices.GetRenderingProvidersAsync(accountInfoId);
            var authorization = await _rethinkServices.GetChildProfileAuthorizationByClientId(accountInfoId, claim.ChildProfileId, authorizationId);
            var isOverrideProvider = authorization?.renderingProviderStaffId != null;
            var queryResult = _claimChargeEntryRepository.Query()
                .Where(x => x.ClaimId == model.ClaimId
                            && x.DateDeleted == null)
                .Select(x => new BillingClaimDetailsModel
                {
                    Id = x.Id,
                    DOS = x.DateOfService,
                    BillingCode = x.BillingCode,
                    Modifiers = x.Modifier1 + x.Modifier2 + x.Modifier3 + x.Modifier4,
                    Modifier1 = x.Modifier1,
                    IncludeOnClaimMod1 = x.IncludeOnClaimMod1 ?? true,
                    Modifier2 = x.Modifier2,
                    IncludeOnClaimMod2 = x.IncludeOnClaimMod2 ?? true,
                    Modifier3 = x.Modifier3,
                    IncludeOnClaimMod3 = x.IncludeOnClaimMod3 ?? true,
                    Modifier4 = x.Modifier4,
                    IncludeOnClaimMod4 = x.IncludeOnClaimMod4 ?? true,
                    Units = x.Units,
                    PerUnitsCharge = x.UnitRate ?? 0,
                    UnitTypeId = x.UnitTypeId,
                    BilledAmount = x.Charges,
                    NoteText = x.NoteText,
                    NoteCreatedBy = x.NoteCreatedBy,
                    NoteCreatedDate = x.NoteCreatedDate,
                    RenderingProviderId = isOverrideProvider ? authorization.renderingProviderStaffId : x.RenderingProviderId
                });

            var claimDiagnosisIds = await _claimDiagnosisCodeRepository.Query()
                .Where(x => x.ClaimId == model.ClaimId && !x.DateDeleted.HasValue && x.IncludeOnClaims)
                .OrderByDescending(x => x.Order)
                .Distinct()
                .ToListAsync();

            claimDiagnosisIds = claimDiagnosisIds.OrderBy(x => x.Order).ToList();

            var claimDiagnosisCodesList = new List<string>();

            if (claimDiagnosisIds.Count > 0)
            {
                foreach (var claimDiagnosId in claimDiagnosisIds)
                {
                    var diagnosis = await _rethinkServices.GetDiagnosisById(claimDiagnosId.DiagnosisId);
                    if (diagnosis != null)
                    {
                        claimDiagnosisCodesList.Add(diagnosis.diagnosisCode);
                    }
                }
            }

            var claimDiagnosisCodes = string.Join(", ", claimDiagnosisCodesList);

            if (model.ChargeEntryId != null)
            {
                queryResult = queryResult.Where(x => x.Id == model.ChargeEntryId);
            }

            var billingClaimDetails = await queryResult.ToListAsync();

            var distinctChargeProviders = billingClaimDetails
                                        .Where(x => x.RenderingProviderId != null)
                                        .Select(x => x.RenderingProviderId.Value)
                                         .ToHashSet();


            var claimProviderId = claim.RenderingStaffMemberId == -2 || claim.RenderingStaffMemberId == null ? claim.MemberId : claim.RenderingStaffMemberId;

            var hideChargeProvider = distinctChargeProviders.Count == 1;

            foreach (var item in billingClaimDetails)
            {
                if (!hideChargeProvider)
                {
                    item.RenderingProvider = renderingProviders.FirstOrDefault(rp => rp.StaffMemberId == item.RenderingProviderId)?.Name;
                }
                else
                {
                    item.RenderingProvider = null;
                }

                var count = await _claimAppointmentChargeEntryEntityRepository.Query().Where(x => x.ClaimChargeEntryEntityId == item.Id).CountAsync();
                item.AssociatedAppointmentsCount = count;
            }

            var totalCount = billingClaimDetails.Count();

            var paymentClaims = await GetPaymentClaimsWithByClaimIdAsync(model.ClaimId);

            foreach (var bcd in billingClaimDetails)
            {
                bcd.UnitTypeValue = unitTypes.FirstOrDefault(x => x.id == bcd.UnitTypeId)?.unit ?? 0;
                var hoursTimeSpan = TimeSpan.FromMinutes(Convert.ToDouble(bcd.Units * bcd.UnitTypeValue));
                bcd.Hours = hoursTimeSpan.TotalHours;

                bcd.Diagnosis = claimDiagnosisCodes;

                bcd.TotalCount = totalCount;

                if (bcd.NoteCreatedBy != null)
                {
                    var member = await _rethinkServices.GetMemberAsync(accountInfoId, bcd.NoteCreatedBy ?? 0);

                    bcd.NoteCreatorName = FullNameExt.GetFullName(member?.firstName, member?.lastName);
                }

                var writeOffAmount = _claimChargeEntryWriteOffRepository.Query().Where(x => x.ClaimChargeEntryId == bcd.Id && x.DateDeleted == null).Sum(x => x.WriteOffAmount);
                bcd.AdjustmentAmount = writeOffAmount.HasValue ? (decimal)writeOffAmount * -1 : 0;
                if (claim.ClaimStatus == ClaimStatus.Denied)
                {
                    bcd.AdjustmentAmount = 0;
                }
                if (paymentClaims.Any())
                {
                    var pcServiceLines = paymentClaims.SelectMany(pc => pc.PaymentClaimServiceLines
                        .Where(sl => sl.ClaimChargeEntryId == bcd.Id && sl.DateDeleted == null)).ToList();
                    if (!pcServiceLines.Any())
                    {
                        bcd.ExpectedAmount = bcd.BilledAmount;
                        bcd.AdjustmentAmount = 0;
                        bcd.PatientAmount = 0;
                        bcd.BalanceAmount = bcd.BilledAmount - bcd.AdjustmentAmount;
                        bcd.PaymentAmount = pcServiceLines.Sum(x => x.PaymentAmount) ?? 0;
                    }
                    else
                    {
                        var funderId = await GetClaimFunderIdAsync(model.ClaimId);

                        var serviceCode = pcServiceLines.First().ServiceCode;
                        //Calculation of Expected Amount will be add post launch
                        bcd.ExpectedAmount = bcd.BilledAmount; // await CalculateExpectedAsync(funderId, serviceCode, bcd.Units);

                        var reasonCodes = pcServiceLines
                            .SelectMany(pc => pc.PaymentClaimServiceLineAdjustments
                                .Where(sl => sl.DateDeleted == null && !string.IsNullOrEmpty(sl.AdjustmentReasonCode))
                                .Select(sl => sl.AdjustmentReasonCode)).ToList();

                        bcd.ReasonCodes = reasonCodes.Select(x => x.ToString()).ToArray();

                        var adjustments = pcServiceLines.SelectMany(sl => sl.PaymentClaimServiceLineAdjustments).ToList();

                        if (claim.ClaimStatus == ClaimStatus.Denied)
                        {
                            bcd.AdjustmentAmount = 0;
                        }
                        else
                        {
                            bcd.AdjustmentAmount += adjustments.Where(x => x.DateDeleted == null && x.AdjustmentGroupCode != "PR" && x.IsAdjustmentPositive == true).Sum(adj => adj.AdjustmentAmount)
                           - adjustments.Where(x => x.DateDeleted == null && x.AdjustmentGroupCode != "PR" && x.IsAdjustmentPositive != true).Sum(adj => adj.AdjustmentAmount) ?? 0;
                        }

                        bcd.PatientAmount = adjustments.Where(x => x.DateDeleted == null && x.AdjustmentGroupCode == "PR" && x.IsAdjustmentPositive == true).Sum(adj => adj.AdjustmentAmount)
                            - adjustments.Where(x => x.DateDeleted == null && x.AdjustmentGroupCode == "PR" && x.IsAdjustmentPositive != true).Sum(adj => adj.AdjustmentAmount) ?? 0;
                        // bcd.PatientAmount = adjustments.Where(x => x.AdjustmentGroupCode == "PR" && x.DateDeleted == null).Sum(adj => adj.AdjustmentAmount) ?? 0;

                        bcd.PaymentAmount = (decimal)pcServiceLines.Where(x => (x.PaymentClaim.Payment.PaymentTypeId == (int)PaymentTypes.InsurancePayment) || x.PaymentClaim.Payment.PaymentTypeId == (int)PaymentTypes.ERAReceived).Sum(x => x.PaymentAmount);
                        bcd.BalanceAmount = bcd.BilledAmount - bcd.PaymentAmount + bcd.AdjustmentAmount + bcd.PatientAmount;

                    }
                }
                else
                {
                    bcd.BalanceAmount = bcd.BilledAmount - bcd.PatientAmount + bcd.AdjustmentAmount;
                }
            }

            var processingQuery = billingClaimDetails.AsQueryable();

            if (model.SortingModels != null && model.SortingModels.Count > 0)
            {
                processingQuery = processingQuery.OrderBy(model.SortingModels);
            }

            if (model.Take > 0)
            {
                processingQuery = processingQuery.Skip(model.Skip).Take(model.Take);
            }

            return processingQuery;
        }

        public async Task<ActionResponse> RemoveBillingClaimDetailAsync(RemoveBillingClaimDetailsModel model)
        {
            List<AppointmentBillingStatus> apptBillingStatus = [];

            var charge = await _claimChargeEntryRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == model.ChargeId && x.DateDeleted == null);

            if (charge != null)
            {
                var linkedAppointment = await _claimAppointmentLinkRepository.Query().Where(x => x.ClaimChargeEntriesId == charge.Id).ToListAsync();
                if (linkedAppointment != null)
                {
                    foreach (var linkedAppointmentLink in linkedAppointment)
                    {
                        var appointmentLinkChargeEntry = await _claimAppointmentChargeEntryEntityRepository.Query().Where(x => x.ClaimChargeEntryEntityId == charge.Id).FirstOrDefaultAsync();

                        SoftDelete(appointmentLinkChargeEntry, model.MemberId);

                        SoftDelete(linkedAppointmentLink, model.MemberId);

                        _claimAppointmentChargeEntryEntityRepository.Update(appointmentLinkChargeEntry);

                        _claimAppointmentLinkRepository.Update(linkedAppointmentLink);

                        apptBillingStatus.Add(PrepareAppointmentBillingStatus(linkedAppointmentLink.AppointmentId, RethinkBillingStatus.NotBilled));
                    }
                }
                SoftDelete(charge, model.MemberId);

                var paymentClaims = await _paymentClaimRepository.Query()
                .Where(x => x.ClaimId == charge.ClaimId &&
                            x.DateDeleted == null)
                .Include(x => x.PaymentClaimServiceLines)
                .ThenInclude(y => y.PaymentClaimServiceLineAdjustments)
                .ToListAsync();

                var writeOffs = await _claimChargeEntryWriteOffRepository.Query()
                    .Where(x => x.ClaimChargeEntryId == charge.Id && x.DateDeleted == null).ToListAsync();

                if (writeOffs.Any())
                {
                    foreach (var writeOff in writeOffs)
                    {
                        SoftDelete(writeOff, model.MemberId);
                        _claimChargeEntryWriteOffRepository.Update(writeOff);
                    }
                    await _claimChargeEntryWriteOffRepository.CommitAsync();
                }

                if (paymentClaims.Any())
                {
                    foreach (var paymentClaim in paymentClaims)
                    {

                        var serviceLine = paymentClaim.PaymentClaimServiceLines.FirstOrDefault(x => x.ClaimChargeEntryId == charge.Id && x.DateDeleted == null);
                        if (serviceLine != null)
                        {
                            foreach (var adjustment in serviceLine.PaymentClaimServiceLineAdjustments.Where(x => x.DateDeleted == null))
                            {
                                SoftDelete(adjustment, model.MemberId);
                                _paymentClaimServiceLineAdjustmentRepository.Update(adjustment);
                            }
                            SoftDelete(serviceLine, model.MemberId);
                            _paymentClaimServiceLineRepository.Update(serviceLine);
                            paymentClaim.TotalCharge -= charge.Charges;
                            paymentClaim.TotalChargeOrig -= charge.Charges;
                            paymentClaim.TotalPayment -= serviceLine.PaymentAmount;
                            paymentClaim.TotalPaymentOrig -= serviceLine.PaymentAmount;
                            _paymentClaimRepository.Update(paymentClaim);
                        }
                    }
                }

                await _paymentClaimServiceLineAdjustmentRepository.CommitAsync();
                await _paymentClaimServiceLineRepository.CommitAsync();
                await _paymentClaimRepository.CommitAsync();

                var claim = await _claimRepository.Query().Where(x => x.Id == charge.ClaimId).Select(x => x.MemberId).FirstOrDefaultAsync();

                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = charge.ClaimId,
                    MemberId = claim,
                    NewValue = charge.BillingCode,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.Delete,
                    ClaimHistoryAction = ClaimHistoryAction.ChargeEntryRemoved,
                    ActionDate = EstDateTime
                });
                _claimChargeEntryRepository.Update(charge);
                await _claimChargeEntryRepository.CommitAsync();

                if (apptBillingStatus.Count != 0)
                {
                    await _bus.SendBatchAsync(Queues.RT_Billing_Queue_AppointmentBillingStatus, apptBillingStatus);
                }

                //SOFT DELETE CHARGE ENTRY FROM CLAIM SUBMISSION CHARGE ENTRY TABLE
                var submissionChargeEntries = await _claimSubmissionServiceLineRepository.Query().Where(x => x.ClaimChargeEntryId == model.ChargeId).ToListAsync();
                if (submissionChargeEntries.Any())
                {
                    foreach (var item in submissionChargeEntries)
                    {
                        SoftDelete(item, model.MemberId);
                        _claimSubmissionServiceLineRepository.Update(item);
                    }
                    await _claimSubmissionServiceLineRepository.CommitAsync();
                }

                // code to delete patient invoice if deleted the charge
                var patientInvoiceDetails = await _patientInvoiceDetailsRepository.Query().Where(p => p.ChargeId == charge.Id).ToListAsync();
                if (patientInvoiceDetails.Any())
                {
                    foreach (var item in patientInvoiceDetails)
                    {
                        SoftDelete(item, model.MemberId);
                        _patientInvoiceDetailsRepository.Update(item);
                    }
                    await _patientInvoiceDetailsRepository.CommitAsync();
                }
                await _bus.SendAsync(PrepareClaimTransaction(charge.Id, ClaimTransactionType.deleteCharge), Topics.RT_Billing_ProcessClaimTxn);

                return ActionResponse.SuccessResult();
            }

            return ActionResponse.FailResult(ValidationErrorMessages.NotFound(EntityNames.Note));
        }

        public async Task<ClaimDetailsModel> GetClaimDetailsAsync(IdWithUserInfo model, ClaimEntity claim)
        {
            var result = new ClaimDetailsModel();

            claim = await _claimManagerService.GetFullClaim(model.Id);

            if (claim.ChildProfile == null)
            {
                await PrepareClaimError(ErrorClientDeleted, ClaimErrorNumber.FunderNotFound, claim.Id, model.MemberId);
                throw new Exception(ErrorClientDeleted);
            }

            var claimHasAuthorization = claim.ChildProfileAuthorization != null;
            //var funderId = (claim.ClaimSubmissions.Count > 0 && claim.ClaimSubmissions.OrderByDescending(cs => cs.Id).First().FunderId != null) ?
            //    claim.ClaimSubmissions.OrderByDescending(cs => cs.Id).First().FunderId.GetValueOrDefault() :
            //    claimHasAuthorization ? claim.ChildProfileAuthorization?.funderId : claim.PrimaryFunderId;

            var funderId = claimHasAuthorization ? claim.ChildProfileAuthorization?.funderId : claim.PrimaryFunderId;
            var clientFunderId = claimHasAuthorization ?
                claim.ChildProfileAuthorization?.ChildProfileFunderServiceLineMapping?.ChildProfileFunderMappingId :
                claim.ClientFunderId.GetValueOrDefault();
            var funder = await _rethinkServices.GetFunder(claim.AccountInfoId, funderId.Value);
            if (funder == null)
            {
                await PrepareClaimError(ErrorNoFunderDetails, ClaimErrorNumber.FunderNotFound, claim.Id, model.MemberId);
                throw new Exception(ErrorNoFunderDetails);
            }
            var funderName = funder?.funderName ?? string.Empty;
            var funderType = funder?.funderTypeId;
            var clientFunder = claimHasAuthorization ?
                claim.ChildProfileAuthorization.ChildProfileFunderServiceLineMapping.ChildProfileFunderMapping
                : await _rethinkServices.GetChildProfileFunderMappingByMappingId(claim.AccountInfoId, claim.ChildProfileId, clientFunderId ?? 0);

            if (clientFunder == null)
            {
                await PrepareClaimError(ErrorNoClientFunderDetails, ClaimErrorNumber.FunderNotFound, claim.Id, model.MemberId);
                throw new Exception(ErrorNoClientFunderDetails);
            }

            clientFunder.Funder = funder;
            clientFunder.Funder.ServiceFunders = await _rethinkServices.GetServiceFundersEntityListByFunderId(claim.AccountInfoId, claim.ChildProfileId, clientFunder.funderId);

            var BenefitAssignmentId = (clientFunder.isAutismCoveredBenefit == true || clientFunder.isAutismCoveredBenefit == null) ? 1 : 2;
            var ReleaseOfInformationConfirmationTypeId = clientFunder.releaseOfInformationConfirmationTypeId;
            var AuthorizedPaymentConfirmationTypeId = clientFunder.authorizedPaymentConfirmationTypeId;
            var policyNumber = clientFunder?.InsuranceContact?.InsuranceContactsType.insurancePolicyNumber;
            bool referringProviderRequiredOnClaim = claim.ChildProfileAuthorization?.Funder != null ? claim.ChildProfileAuthorization.Funder.referringProviderRequiredOnClaim : (clientFunder?.Funder != null ? clientFunder.Funder.referringProviderRequiredOnClaim : false);
            var billingProviderOption = 0;
            var serviceLine = new ChildProfileServiceLines();
            var serviceLineMapping = new ServiceLines();

            if (claimHasAuthorization && claim.ChildProfileAuthorization.ChildProfileFunderServiceLineMapping != null)
            {
                serviceLineMapping = claim.ChildProfileAuthorization.ChildProfileFunderServiceLineMapping;
            }
            else
            {
                serviceLineMapping = await _rethinkServices.GetChildProfileFunderServiceLineMappingEntity(model.AccountInfoId, claim.ChildProfileId, claim.ClientFunder?.id ?? 0, claim.ClientFunderServiceLineId ?? 0);
            }
            if (serviceLineMapping != null)
            {
                serviceLine = await _rethinkServices.GetServiceLine(model.AccountInfoId, serviceLineMapping.serviceId);
                if (serviceLine != null)
                {
                    var sFunder = clientFunder.Funder.ServiceFunders.Where(x => x.providerServiceId == serviceLineMapping.serviceId).FirstOrDefault();
                    if (sFunder != null && sFunder.billingProviderOptionId.HasValue)
                    {
                        billingProviderOption = sFunder.billingProviderOptionId.Value;
                    }
                }
            }


            if (!string.IsNullOrWhiteSpace(policyNumber))
            {
                funderName = funderName + " - " + policyNumber;
            }

            var childProfileFunderMapping = claim.ChildProfileAuthorization?.ChildProfileFunderServiceLineMapping?
                                                .ChildProfileFunderMapping ?? claim.ClientFunder;

            //var isClaimUpdatedAfterAuth = claimHasAuthorization
            //    ? claim.DateLastModified > claim.ChildProfileAuthorization.metaData?.modifiedOn 
            //    : true;
            var groupedClaimDiagnosis = claim.ClaimDiagnosisCodes?
                        .Where(c => c.DateDeleted == null && c.IncludeOnClaims)
                        .GroupBy(c => c.DiagnosisId)
                        .Select(g => g.First());
            var rendringProviders = await _rethinkServices.GetRenderingProvidersAsync(claim.AccountInfoId);
            var overrideStaffId = claim.ChildProfileAuthorization?.renderingProviderStaffId;
            var providerName = overrideStaffId.HasValue ? rendringProviders
                                        .FirstOrDefault(x => x.StaffMemberId == overrideStaffId.Value)?.Name : null;

            if (claim.ProviderLocation?.name == null && claim.ProviderLocationId != 0)
            {
                await PrepareClaimError(ErrorBillingProviderMissionAtLocation, ClaimErrorNumber.BillingProviderMissing, claim.Id, model.MemberId);
                throw new NullReferenceException($"The Claim Details can’t be displayed because {ErrorBillingProviderMissionAtLocation}. Please assign a Billing Provider to this Location to continue.");
            }

            result = new ClaimDetailsModel
            {
                Id = claim.Id,
                ClaimIdentifier = claim?.ClaimIdentifier,
                PatientId = claim.ChildProfile.Id,
                PatientName = FullNameExt.GetFullName(claim?.ChildProfile.FirstName,
                                            claim?.ChildProfile.MiddleName,
                                            claim?.ChildProfile.LastName),
                ResponsibleParty = FullNameExt.GetFullName(childProfileFunderMapping?.InsuranceContact?.Name?.firstName,
                    childProfileFunderMapping?.InsuranceContact?.Name?.middleName,
                    childProfileFunderMapping?.InsuranceContact?.Name?.lastName),
                DateOfServiceStart = claim.StartDate,
                DateOfServiceEnd = claim.EndDate,
                DiagnosisCodes = groupedClaimDiagnosis?.Select(g => new ClaimDiagnosisCodeModel
                {
                    Description = g.Diagnosis?.Description,
                    DiagnosisCode = g.Diagnosis?.DiagnosisCode,
                    DiagnosisId = g.DiagnosisId,
                    Order = g.Order,
                    IncludeOnClaims = g.IncludeOnClaims
                }).ToList() ?? [],

                AuthorizationNumber = claim?.AuthorizationNumber,
                AuthorizationStatus = claim?.ChildProfileAuthorization?.authorizationSubmissionTypeId != null
                    ? ((AuthorizationStatus)claim.ChildProfileAuthorization.authorizationSubmissionTypeId).ToString()
                    : null,
                RenderingProviderId = overrideStaffId ?? claim?.RenderingStaffMemberId,
                RenderingProvider = overrideStaffId.HasValue ? FullNameExt.GetFullName(providerName) : FullNameExt.GetFullName(claim.RenderingStaffMember?.firstName,
                                                claim.RenderingStaffMember?.middleName,
                                                claim.RenderingStaffMember?.lastName),

                ReferringProviderId = claim?.ChildProfileReferringProviderId,
                ReferringProvider = FullNameExt.GetFullName(claim.ReferringProvider?.ReferringProvider?.name?.firstName,
                                                claim.ReferringProvider?.ReferringProvider?.name?.lastName),
                ReferringProviderRequiredOnClaim = referringProviderRequiredOnClaim,

                BillingProviderId = claim?.ProviderLocationId,
                BillingProvider = claim.ProviderLocationId.HasValue ? claim?.ProviderLocation?.name : null,

                ServiceFacilityId = claim?.ServiceLocationId,
                ServiceFacility = claim.ServiceLocation?.name,

                PlaceOfServiceId = claim.LocationCodeId,
                PlaceOfService = $"{claim?.LocationCode?.code} - {claim?.LocationCode?.description}",

                BilledAmount = claim.ClaimChargeEntries.Sum(c => c.Charges),
                SubmissionReason = (int)claim.FrequencyTypeId.Value,
                PatientReleaseAgreement = claim.ReleaseOfInformationConfirmationTypeId ??
                                            childProfileFunderMapping?.releaseOfInformationConfirmationTypeId,
                AuthorizePayment = claim.AuthorizedPaymentConfirmationTypeId ??
                                    childProfileFunderMapping?.authorizedPaymentConfirmationTypeId,

                BenefitsAssignment = claim?.BenefitAssignmentId,
                OriginalClaim = claim?.OriginalClaim,
                Note = claim?.Note,
                ClaimStatus = claim.ClaimStatus,
                FunderId = funderId,
                FunderName = funderName,
                FunderTypeId = (FunderType?)funderType,
                BillingProviderOptionId = (BillingProviderOptionType?)billingProviderOption ?? (BillingProviderOptionType?)funder.billingProviderOptionId,

                AuthorizationDetails = new AuthorizationDetailsModel
                {
                    RenderingProviderId = claim?.ChildProfileAuthorization?.renderingProviderStaffId,
                },
                ServiceLineId = claim.ClientFunderServiceLineId, //serviceLine.Id,
                ServiceLine = serviceLine.name,
                ServiceId = serviceLineMapping.serviceId,
                PrimaryFunderId = claim.PrimaryFunderId,
                SecondaryFunderId = claim?.SecondaryFunderId
            };


            if (result == null)
                return null;

            if (result.ClaimStatus != ClaimStatus.Billed && result.ClaimStatus != ClaimStatus.Closed && result.ClaimStatus != ClaimStatus.VoidClosed)
            {
                int? renderingProviderTypeId = claim.ChildProfileAuthorization?.authorizationRenderingProviderTypeId;
                if (claim.ClaimAppointmentLinks.Any())
                {
                    var appId = claim.ClaimAppointmentLinks.FirstOrDefault().AppointmentId;

                    var app = await _rethinkServices.GetAppointmentAsync(appId);

                    // #DBMIGRATION 
                    //< TAKE THE CLIENT AUTHORIZATION RENDERING PROVIDER ID IN PLACE OF APPOINTMENT RENDERING PROVIDER ID
                    //if (app.PropagatingClientAuthRenderingProviderId.HasValue)
                    //{
                    //    var propagatingAuthorization = await _bhPropagatingChildProfileAuthorizationRepository.Query()
                    //            .FirstOrDefaultAsync(p => p.Id == app.PropagatingClientAuthRenderingProviderId.Value);
                    //    renderingProviderTypeId = propagatingAuthorization.AuthorizationRenderingProviderTypeId;
                    //    result.RenderingProviderId = propagatingAuthorization.RenderingProviderStaffId;
                    //}
                    if (claim.AuthorizationId.HasValue && !result.RenderingProviderId.HasValue) result.RenderingProviderId = claim.ChildProfileAuthorization.renderingProviderStaffId;
                    //>

                    var propagatingAccountInfo = app?.propagatingAccountInfoId != null ? await _rethinkServices.GetPropagatingAccountInfo(app.propagatingAccountInfoId ?? 0) : null;

                    if (renderingProviderTypeId != null && !result.RenderingProviderId.HasValue)
                    {
                        if (renderingProviderTypeId == (int)AuthorizationRenderingProviderTypes.Agency)
                        {
                            result.RenderingProvider = propagatingAccountInfo?.ToString() ?? claim.AccountInfo.AccountOrganizationName;
                        }
                        else if (renderingProviderTypeId == (int)AuthorizationRenderingProviderTypes.ProviderAssignedToAppointment)
                        {
                            if (app != null)
                            {
                                app.StaffMember = await _rethinkServices.GetStaffMember(claim.AccountInfoId, app.staffId);
                                app.StaffMember.Member = await _rethinkServices.GetMemberAsync(claim.AccountInfoId, app.StaffMember.memberId);
                                result.RenderingProviderId = app.StaffMember.memberId;
                                result.RenderingProvider = FullNameExt.GetFullName(app.StaffMember.Member.firstName,
                                                                        app.StaffMember.Member.middleName,
                                                                        app.StaffMember.Member.lastName);
                            }
                        }
                        else if (renderingProviderTypeId == (int)AuthorizationRenderingProviderTypes.StaffMember)
                        {
                            if (result.RenderingProviderId != null && string.IsNullOrEmpty(result.RenderingProvider))
                            {
                                var staffMember = await _rethinkServices.GetStaffMember(model.AccountInfoId, result.RenderingProviderId ?? 0);

                                if (staffMember != null)
                                {
                                    result.RenderingProvider = FullNameExt.GetFullName(staffMember.name.firstName,
                                                                            staffMember.name.middleName,
                                                                            staffMember.name.lastName);
                                }
                            }
                        }
                    }
                    //else
                    //{
                    //    app.StaffMember = await _rethinkServices.GetStaffMember(claim.AccountInfoId, app.staffId);
                    //    app.StaffMember.Member = await _rethinkServices.GetMemberAsync(claim.AccountInfoId, app.StaffMember.memberId);
                    //    result.RenderingProviderId = app.StaffMember.memberId;
                    //    result.RenderingProvider = FullNameExt.GetFullName(app.StaffMember.Member.firstName,
                    //                                            app.StaffMember.Member.middleName,
                    //                                            app.StaffMember.Member.lastName);
                    //}

                    if (app != null && app.PlaceOfService != null)
                    {
                        result.PlaceOfService = $"{app.PlaceOfService.code} - {app.PlaceOfService.description}";
                        result.PlaceOfServiceId = app.PlaceOfService.id;
                    }
                }
                else
                {
                    if (renderingProviderTypeId == (int)AuthorizationRenderingProviderTypes.Agency && !result.RenderingProviderId.HasValue)
                    {
                        claim.AccountInfo = await _rethinkServices.GetAccountReturningEntityAsync(claim.AccountInfoId);
                        result.RenderingProvider = claim.AccountInfo.AccountOrganizationName;
                    }
                }
            }

            var payment = await _paymentClaimRepository.Query()
                .Where(x => x.ClaimId == model.Id)
                .OrderByDescending(x => x.DateCreated)
                .FirstOrDefaultAsync();

            if (payment != null)
            {
                result.PatientResponsibilityAmount = payment.PatientRespAmount ?? 0;
                result.PaymentAmount = payment.TotalCharge ?? 0;
                result.BalanceAmount = result.BilledAmount - result.PaymentAmount
                                                            - payment.PaymentClaimServiceLines
                                                                .SelectMany(x => x.PaymentClaimServiceLineAdjustments)
                                                                .Where(x => x.AdjustmentReasonCode != "PR")
                                                                .Sum(x => x.AdjustmentAmount) ?? 0;
            }

            var submission = await _claimSubmissionRepository
                .Query()
                .Where(x => x.DateDeleted == null && x.ClaimId == result.Id)
                .Select(x => new { x.ClaimSubmissionIdentifier })
                .FirstOrDefaultAsync();

            if (submission != null)
            {
                result.SubmissionCode = submission.ClaimSubmissionIdentifier;
            }


            return result;
        }

        public async Task<List<ClaimHFCAModel>> GetHFCAClaimDetailsAsync(IdsWithUserInfo model)
        {
            var hfcaData = new List<ClaimHFCAModel>();

            foreach (var claimId in model.Ids)
            {
                var hfcaModel = await _claimManagerService.LookupHCFAClaimDetails(model.MemberId, model.AccountInfoId, claimId);
                hfcaData.Add(hfcaModel);
            }
            return hfcaData;
        }

        public async Task<bool> UpdateClaimAsync(UpdateDetails updateDetails)
        {
            var claimId = updateDetails.claimModel != null ? updateDetails.claimModel.ClaimId : updateDetails.chargeEntryModel.BillingClaimDetailsModels.FirstOrDefault().ClaimId;
            var existingClaimRenderingProviderId = (await _claimRepository.Query().FirstOrDefaultAsync(x => x.Id == claimId))?.RenderingStaffMemberId;
            if (!(updateDetails.claimModel.RenderingProviderId == 0) && !(updateDetails.claimModel.RenderingProviderId == existingClaimRenderingProviderId))
            {
                updateDetails.chargeEntryModel.BillingClaimDetailsModels.ToList().ForEach(x => x.RenderingProviderId = updateDetails.claimModel.RenderingProviderId);
            }
            var ids = updateDetails.chargeEntryModel.BillingClaimDetailsModels
            .Select(x => x.RenderingProviderId)
            .Distinct()
            .ToList();
            updateDetails.claimModel.RenderingProviderId = ids.Count > 1 ? 0 : ids.FirstOrDefault();
            // Check if there is only one billing code associated with the diagnosis code and use its rendering provider if available, otherwise use the rendering provider from the claim info
            var renderingProvidersAtChargeLevel = updateDetails.chargeEntryModel?.BillingClaimDetailsModels?
                .Select(bcd => bcd.RenderingProviderId)
                .Distinct()
                .ToList();

            if ((updateDetails.chargeEntryModel?.BillingClaimDetailsModels?.Count == 1
                && updateDetails.claimModel?.RenderingProviderId != updateDetails.chargeEntryModel?.BillingClaimDetailsModels[0]?.RenderingProviderId)
                || (updateDetails.chargeEntryModel?.BillingClaimDetailsModels?.Count > 1 && renderingProvidersAtChargeLevel.Count == 1))
            {
                updateDetails.claimModel.RenderingProviderId = updateDetails.chargeEntryModel?.BillingClaimDetailsModels[0].RenderingProviderId ?? 0;
            }

            if (updateDetails.isClaimUpdated)
                await UpdateClaimDetailsAsync(updateDetails.claimModel, updateDetails.AccountInfoId, updateDetails.MemberId);
            if (updateDetails.isChargeEntryUpdated)
                await UpdateBillingClaimDetailsAsync(updateDetails.chargeEntryModel, updateDetails.MemberId);

            await _claimChangeTrackingService.SaveChangesAsync(updateDetails.ImpersonationUserName);
            await _claimValidationService.ValidateClaimData(claimId, updateDetails.MemberId, null, ResponsibilitySequenceType.Primary, true);

            //for other charge entries
            if (updateDetails.BillingProviderRequest != null)
            {
                await AddUpdateBillingProviderAsync(updateDetails.MemberId, updateDetails.BillingProviderRequest);
            }

            return true;
        }

        public async Task<bool> UpdateClaimDetailsAsync(UpdateClaimDetailsModel model, int accountInfoId, int memberId, bool isValidateRequired = false)
        {
            var claimEntity = await _claimManagerService.GetFullClaim(model.ClaimId);

            var claimUserInfo = new IdWithUserInfo
            {
                Id = model.ClaimId,
                MemberId = model.MemberId,
                AccountInfoId = accountInfoId
            };

            _claimChangeTrackingService.Initialize(claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, EstDateTime);
            _claimChangeTrackingService.TrackChanges(claimEntity, model);

            claimEntity.LocationCodeId = model.PlaceOfServiceId;
            claimEntity.OriginalClaim = string.IsNullOrEmpty(model.OriginalClaim) ? null : model.OriginalClaim;
            claimEntity.Note = string.IsNullOrEmpty(model.Note) ? null : model.Note;
            var childProfileAuthEntity = claimEntity.ChildProfileAuthorization;

            //delete missing diagnosis
            var editedDiagnosesIds = model.DiagnosisCodes.Select(d => d.DiagnosisId);
            var diagnosesToDelete = claimEntity.ClaimDiagnosisCodes.Where(x => !editedDiagnosesIds.Contains(x.DiagnosisId));
            var diagnosesToUpdate = claimEntity.ClaimDiagnosisCodes.Where(x => editedDiagnosesIds.Contains(x.DiagnosisId));
            var diagnosesToDeleteIds = diagnosesToDelete.Select(x => x.DiagnosisId).ToList();
            var diagnosesToUpdateIds = diagnosesToUpdate.Select(x => x.DiagnosisId).ToList();


            foreach (var diagnosis in diagnosesToDelete)
            {
                SoftDelete(diagnosis, model.MemberId);
                _claimDiagnosisCodeRepository.Update(diagnosis);
            }

            //create new diagnosis entity
            foreach (var diagnosisCode in model.DiagnosisCodes.Where(x => !diagnosesToDeleteIds.Contains(x.DiagnosisId) && !diagnosesToUpdateIds.Contains(x.DiagnosisId)))
            {
                var diagnosisCodeEntity = new ClaimDiagnosisCodeEntity
                {
                    ClaimId = model.ClaimId,
                    DiagnosisId = diagnosisCode.DiagnosisId,
                    Order = diagnosisCode.Order,
                    IncludeOnClaims = diagnosisCode.IncludeOnClaims
                };
                MarkCreated(diagnosisCodeEntity, model.MemberId);

                _claimDiagnosisCodeRepository.Add(diagnosisCodeEntity);
            }

            foreach (var item in claimEntity.ClaimChargeEntries)
            {

                item.DiagnosisCode = model.DiagnosisCodes.Where(x => x.Order == 1).FirstOrDefault().DiagnosisCode;
                MarkUpdated(item, model.MemberId);
                _claimChargeEntryRepository.Update(item);
            }

            //update already existed
            foreach (var diagnosisCode in model.DiagnosisCodes.Where(x => diagnosesToUpdateIds.Contains(x.DiagnosisId)))
            {
                var oldDiagnosisCode =
                    claimEntity.ClaimDiagnosisCodes.FirstOrDefault(x => x.DiagnosisId == diagnosisCode.DiagnosisId);

                oldDiagnosisCode.Order = diagnosisCode.Order;
                oldDiagnosisCode.IncludeOnClaims = diagnosisCode.IncludeOnClaims;

                MarkUpdated(oldDiagnosisCode, model.MemberId);
                _claimDiagnosisCodeRepository.Update(oldDiagnosisCode);
            }


            // UPDATE CHARGE DETAILS FOR EDITING THE DIAGNOSIS CODE 
            var firstDiagnosisCode = model.DiagnosisCodes.Select(x => x.DiagnosisCode).FirstOrDefault();
            var oldChargeEntries = await _claimChargeEntryRepository.Query().Where(x => x.ClaimId == model.ClaimId).ToListAsync();
            foreach (var item in oldChargeEntries)
            {
                item.DiagnosisCode = firstDiagnosisCode;
                _claimChargeEntryRepository.Update(item);
            }


            //providers
            claimEntity.ServiceLocationId = model.ServiceFacilityId;
            claimEntity.RenderingStaffMemberId = model.RenderingProviderId;
            var renderingProviders = await _rethinkServices.GetAllRenderingProvidersAsync(accountInfoId);
            claimEntity.RenderingProviderTypeId = renderingProviders.data.FirstOrDefault(x => x.memberId == model.RenderingProviderId)?.id ?? 0;
            claimEntity.ChildProfileReferringProviderId = model.ReferringProviderId;
            claimEntity.ProviderLocationId = model.BillingProviderId;
            //TODO is this prop?
            claimEntity.BillTo = model.BillingProviderId;

            //additionalInfo
            claimEntity.ReleaseOfInformationConfirmationTypeId = model.PatientReleaseAgreementId;
            claimEntity.AuthorizedPaymentConfirmationTypeId = model.AuthorizePaymentId;
            claimEntity.FrequencyTypeId = (ClaimFrequencyType)model.SubmissionReasonId;
            claimEntity.BenefitAssignmentId = model.BenefitAssignmentId;

            _claimRepository.Update(claimEntity);

            //var submissionEntity = await _claimSubmissionRepository.Query()
            //    .Where(clmSub => clmSub.ClaimId == claimEntity.Id && clmSub.DateDeleted == null)
            //    .OrderByDescending(clmSub => clmSub.SubmitDate)
            //    .FirstOrDefaultAsync();

            //if (submissionEntity != null)
            //{
            //    //Submit Reason
            //    submissionEntity.FrequencyType = (ClaimFrequencyType)model.SubmissionReasonId;
            //    _claimSubmissionRepository.Update(submissionEntity);
            //}


            await _claimRepository.CommitAsync();

            await _bus.SendAsync(new ClaimCreateEnd
            {
                ClaimId = claimEntity.Id,
                AccountInfoId = claimEntity.AccountInfoId,
                RenderingProviderTypeId = claimEntity.RenderingProviderTypeId,
                RenderingProviderId = claimEntity.RenderingStaffMemberId
            }, Queues.RT_Billing_ClaimCreationEnd);

            if (isValidateRequired) await _claimValidationService.ValidateClaimData(claimEntity.Id, memberId, null, ResponsibilitySequenceType.Primary, true);

            return true;
        }

        public async Task<List<BillingClaimDetailsModel>> UpdateBillingClaimAsync(UpdateBillingClaimDetailsListModel model, int memberId, bool isValidateRequired = false)
        {
            var result = await UpdateBillingClaimDetailsAsync(model, model.MemberId, isValidateRequired, true);
            return result;
        }

        public async Task<List<BillingClaimDetailsModel>> UpdateBillingClaimDetailsAsync(UpdateBillingClaimDetailsListModel model, int memberId, bool isValidateRequired = false, bool saveChanges = false)
        {
            var modelList = new List<GetBillingClaimDetailsModel>();
            var resultList = new List<BillingClaimDetailsModel>();
            int claimId = model.BillingClaimDetailsModels.Select(x => x.ClaimId).FirstOrDefault();
            claimTransactionData = new List<ClaimTransactionModel>();

            foreach (var modelItem in model.BillingClaimDetailsModels)
            {
                // NEW ROW (CREATE) WHEN Id == 0
                if (modelItem.Id == 0)
                {
                    // Load minimal claim (no need for full graph for insert)
                    var claimEntityLite = await _claimRepository.Query()
                        .Where(c => c.Id == modelItem.ClaimId && c.DateDeleted == null)
                        .Select(c => new { c.Id, c.AccountInfoId, c.RenderingStaffMemberId })
                        .FirstOrDefaultAsync();

                    if (claimEntityLite == null)
                        throw new NullReferenceException("Parent Claim not found for new Charge Entry.");

                    var newCharge = new ClaimChargeEntryEntity
                    {
                        ClaimId = modelItem.ClaimId,
                        Modifier1 = modelItem.Modifier1,
                        Modifier2 = modelItem.Modifier2,
                        Modifier3 = modelItem.Modifier3,
                        Modifier4 = modelItem.Modifier4,
                        Units = modelItem.Units,
                        UnitRate = modelItem.PerUnitsCharge,
                        Charges = modelItem.Units * modelItem.PerUnitsCharge,
                        BillingCode = modelItem.BillingCode,
                        DateOfService = modelItem.DateOfService,
                        DiagnosisCode = modelItem.Diagnosis,
                        NoteText = modelItem.NoteText,
                        NoteCreatedBy = memberId,
                        NoteCreatedDate = EstDateTime,
                        RenderingProviderId = modelItem.RenderingProviderId
                    };

                    MarkCreated(newCharge, memberId);
                    _claimChargeEntryRepository.Add(newCharge);
                    await _claimChargeEntryRepository.CommitAsync(); // ensure Id is generated

                    // History (ChargeEntryAdded)
                    await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                    {
                        ClaimId = modelItem.ClaimId,
                        MemberId = memberId,
                        Mode = ClaimActionMode.User,
                        ClaimAction = ClaimAction.Added,
                        ClaimHistoryAction = ClaimHistoryAction.ChargeEntryAdded,
                        NewValue = newCharge.BillingCode,
                        ActionDate = EstDateTime
                    }, false);

                    // Track transaction for AR report
                    claimTransactionData.Add(PrepareClaimTransaction(newCharge.Id, ClaimTransactionType.billedAmount));

                    // Prepare retrieval model
                    modelList.Add(new GetBillingClaimDetailsModel
                    {
                        ClaimId = modelItem.ClaimId,
                        ChargeEntryId = newCharge.Id
                    });

                    // Skip update logic
                    continue;
                }

                // UPDATE EXISTING ROW
                var claimUserInfo = new IdWithUserInfo
                {
                    Id = modelItem.ClaimId,
                    MemberId = model.MemberId,
                };

                var claimChargeEntryEntity = await _claimChargeEntryRepository.Query()
                    .Include(x => x.Claim)
                    .ThenInclude(x => x.PaymentClaims)
                    .ThenInclude(x => x.PaymentClaimServiceLines)
                    .FirstOrDefaultAsync(x => x.Id == modelItem.Id
                                                && x.ClaimId == modelItem.ClaimId && x.DateDeleted == null);

                if (claimChargeEntryEntity == null)
                    throw new NullReferenceException("Charge Entry not found");

                _claimChangeTrackingService.Initialize(claimUserInfo, ClaimAction.Edit, ClaimHistoryAction.ChargeEntryUpdated, EstDateTime);
                _claimChangeTrackingService.TrackChangesForCharges(claimChargeEntryEntity, modelItem);

                claimChargeEntryEntity.RenderingProviderId = modelItem.RenderingProviderId;
                claimChargeEntryEntity.Modifier1 = modelItem.Modifier1;
                claimChargeEntryEntity.Modifier2 = modelItem.Modifier2;
                claimChargeEntryEntity.Modifier3 = modelItem.Modifier3;
                claimChargeEntryEntity.Modifier4 = modelItem.Modifier4;
                claimChargeEntryEntity.Units = modelItem.Units;
                claimChargeEntryEntity.UnitRate = modelItem.PerUnitsCharge;
                var previousCharges = claimChargeEntryEntity.Charges;
                claimChargeEntryEntity.Charges = modelItem.Units * modelItem.PerUnitsCharge;
                var chargeDifference = claimChargeEntryEntity.Charges - previousCharges;
                _claimChargeEntryRepository.Update(claimChargeEntryEntity);

                var paymentClaimsTobeUpdated = claimChargeEntryEntity.Claim.PaymentClaims.ToList();
                if (paymentClaimsTobeUpdated.Any())
                {
                    foreach (var paymentClaim in paymentClaimsTobeUpdated)
                    {
                        paymentClaim.TotalCharge += chargeDifference;
                        paymentClaim.TotalChargeOrig += chargeDifference;
                        var serviceLine = paymentClaim.PaymentClaimServiceLines.FirstOrDefault(x => x.ClaimChargeEntryId == modelItem.Id);
                        if (serviceLine != null)
                        {
                            serviceLine.ChargeAmount = claimChargeEntryEntity.Charges;
                            serviceLine.ChargeAmountOrig = claimChargeEntryEntity.Charges;
                            serviceLine.ProcedureUnits = claimChargeEntryEntity.Units.ToString();
                            serviceLine.ProcedureModifier1 = modelItem.Modifier1;
                            serviceLine.ProcedureModifier2 = modelItem.Modifier2;
                            serviceLine.ProcedureModifier3 = modelItem.Modifier3;
                            serviceLine.ProcedureModifier4 = modelItem.Modifier4;
                            serviceLine.ProcedureUnitsOrig = claimChargeEntryEntity.Units.ToString();
                            serviceLine.ProcedureModifier1Orig = modelItem.Modifier1;
                            serviceLine.ProcedureModifier2Orig = modelItem.Modifier2;
                            serviceLine.ProcedureModifier3Orig = modelItem.Modifier3;
                            serviceLine.ProcedureModifier4Orig = modelItem.Modifier4;
                            serviceLine.ExpectedAmount = claimChargeEntryEntity.Charges;
                            _paymentClaimServiceLineRepository.Update(serviceLine);
                        }
                        _paymentClaimRepository.Update(paymentClaim);
                    }
                }

                var claimModel = new GetBillingClaimDetailsModel
                {
                    ClaimId = modelItem.ClaimId,
                    ChargeEntryId = modelItem.Id,
                };

                modelList.Add(claimModel);
                if (previousCharges != claimChargeEntryEntity.Charges)
                {
                    claimTransactionData.Add(PrepareClaimTransaction(claimChargeEntryEntity.Id, ClaimTransactionType.billedAmount));
                }
            }

            // Commit payment updates
            await _paymentClaimRepository.SaveChangesAsync();
            await _paymentClaimRepository.CommitAsync();
            await _paymentClaimServiceLineRepository.SaveChangesAsync();
            await _paymentClaimServiceLineRepository.CommitAsync();

            // Commit charge updates (new & updated)
            await _claimChargeEntryRepository.CommitAsync();

            if (saveChanges)
            {
                await _claimChangeTrackingService.SaveChangesAsync();
            }

            if (isValidateRequired) await _claimValidationService.ValidateClaimData(model.BillingClaimDetailsModels.FirstOrDefault().ClaimId, memberId, null, ResponsibilitySequenceType.Primary, true);

            // Build result list
            foreach (var claimModel in modelList)
            {
                var updatedLine = (await GetClaimChargesForAccountAsync(claimModel)).FirstOrDefault();
                if (updatedLine != null)
                {
                    resultList.Add(updatedLine);
                }
            }

            sendMessage(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);

            return resultList;
        }

        private async Task<int> GetClaimFunderIdAsync(int claimId)
        {
            var claim = await _claimRepository.Query().AsNoTracking()
                .Include(c => c.ClaimSubmissions)
                .FirstOrDefaultAsync(x => x.Id == claimId && x.DateDeleted == null);

            var claimSubmission = claim.ClaimSubmissions
                .FirstOrDefault(cs => cs.ResponsibilitySequence == ResponsibilitySequenceType.Primary.AsString() &&
                                                                              cs.DateDeleted == null);
            if (claimSubmission != null && claimSubmission.FunderId.HasValue)
            {
                return claimSubmission.FunderId.Value;
            }
            var childProfileAuth = await _rethinkServices.GetChildProfileAuthorizationById(claim.AccountInfoId, claim.AuthorizationId ?? 0);

            return childProfileAuth?.funderId ?? 0;
        }

        private async Task<List<PaymentClaimEntity>> GetPaymentClaimsWithByClaimIdAsync(int claimId)
        {
            var paymentClaims = await _paymentClaimRepository.Query()
                .Where(x => x.ClaimId == claimId && x.ClaimStatus != "22" &&
                            x.DateDeleted == null)
                .Include(x => x.Payment)
                .Include(x => x.PaymentClaimServiceLines)
                .ThenInclude(y => y.PaymentClaimServiceLineAdjustments)
                .ToListAsync();

            return paymentClaims;
        }

        public async Task<MemberViewSettingEntity> SaveSelectedColumnsAsync(int accountInfoId, int memberId, List<string> selectedColumns)
        {
            var memberViewSettingExist = _memberViewSettingRepository.Query().FirstOrDefault(x => x.Id == memberId);

            if (memberViewSettingExist != null)
            {
                memberViewSettingExist.Client = selectedColumns.Contains("client");
                memberViewSettingExist.Funder = selectedColumns.Contains("funder");
                memberViewSettingExist.RenderingProvider = selectedColumns.Contains("renderingProvider");
                memberViewSettingExist.PlaceOfService = selectedColumns.Contains("placeOfService");
                memberViewSettingExist.DateOfService = selectedColumns.Contains("dateOfService");
                memberViewSettingExist.Authorization = selectedColumns.Contains("authorization");
                memberViewSettingExist.Expected = selectedColumns.Contains("expected");
                memberViewSettingExist.Billed = selectedColumns.Contains("billed");
                memberViewSettingExist.Payment = selectedColumns.Contains("payment");
                memberViewSettingExist.PatientResponsible = selectedColumns.Contains("patientResponsible");
                memberViewSettingExist.Balance = selectedColumns.Contains("balance");
                memberViewSettingExist.BilledDate = selectedColumns.Contains("billedDate");
                memberViewSettingExist.Status = selectedColumns.Contains("status");
                memberViewSettingExist.AssigneeName = selectedColumns.Contains("assigneeName");
                memberViewSettingExist.Validation = selectedColumns.Contains("validation");
                memberViewSettingExist.Adjustment = selectedColumns.Contains("adjustment");
                memberViewSettingExist.Actions = selectedColumns.Contains("actions");

                _memberViewSettingRepository.Update(memberViewSettingExist);
                await _memberViewSettingRepository.CommitAsync();
            }
            else
            {
                memberViewSettingExist = new MemberViewSettingEntity
                {
                    Id = memberId,
                    Client = selectedColumns.Contains("client"),
                    Funder = selectedColumns.Contains("funder"),
                    RenderingProvider = selectedColumns.Contains("renderingProvider"),
                    PlaceOfService = selectedColumns.Contains("placeOfService"),
                    DateOfService = selectedColumns.Contains("dateOfService"),
                    Authorization = selectedColumns.Contains("authorization"),
                    Expected = selectedColumns.Contains("expected"),
                    Billed = selectedColumns.Contains("billed"),
                    Payment = selectedColumns.Contains("payment"),
                    PatientResponsible = selectedColumns.Contains("patientResponsible"),
                    Balance = selectedColumns.Contains("balance"),
                    BilledDate = selectedColumns.Contains("billedDate"),
                    Status = selectedColumns.Contains("status"),
                    AssigneeName = selectedColumns.Contains("assigneeName"),
                    Validation = selectedColumns.Contains("validation"),
                    Adjustment = selectedColumns.Contains("adjustment"),
                    Actions = selectedColumns.Contains("actions")
                };

                _memberViewSettingRepository.Add(memberViewSettingExist);
                await _memberViewSettingRepository.CommitAsync();
            }

            return memberViewSettingExist;
        }

        public async Task<MemberViewSettingEntity> GetMemberViewSettingsAsync(int memberId)
        {
            var memberViewSettingEntity = _memberViewSettingRepository.Query().FirstOrDefault(x => x.Id == memberId);

            if (memberViewSettingEntity == null)
            {
                memberViewSettingEntity = new MemberViewSettingEntity
                {
                    Id = memberId,
                    Client = true,
                    Funder = true,
                    RenderingProvider = true,
                    PlaceOfService = true,
                    DateOfService = true,
                    Authorization = true,
                    Expected = true,
                    Billed = true,
                    Payment = true,
                    PatientResponsible = true,
                    Balance = true,
                    BilledDate = true,
                    Status = true,
                    AssigneeName = true,
                    Validation = true,
                    Adjustment = true,
                    Actions = true
                };

                _memberViewSettingRepository.Add(memberViewSettingEntity);
                await _memberViewSettingRepository.CommitAsync();
            }

            return memberViewSettingEntity;
        }

        public async Task<List<ClaimErrorAlertViewModel>> GetClaimErrorsAndAlertsAsync(int claimId)
        {
            var claimValidationErrors = await _claimValidationErrorRepository.Query()
                .Include(x => x.ClaimErrorMessage)
                    .ThenInclude(x => x.ClaimErrorCategory)
                .Include(x => x.EraValidationError)
                    .ThenInclude(x => x.GroupCode)
                .Include(x => x.EraValidationError)
                    .ThenInclude(x => x.AdjustmentCode)
                .Where(x => x.DateDeleted == null && x.ClaimId == claimId)
                .ToListAsync();

            var clearingHouseResponseDetails = _clearingHouseResponseRepository.Query().Where(x => x.ClaimId == claimId).ToList();

            var result = new List<ClaimErrorAlertViewModel>();

            foreach (var error in claimValidationErrors)
            {
                var claimErrorMessage = error.ClaimErrorMessage;
                var hasEraValidationError = error.EraValidationErrorId.HasValue;
                var eraGropCode = hasEraValidationError ? error.EraValidationError.GroupCode : null;
                var eraAdjustmentCode = hasEraValidationError ? error.EraValidationError.AdjustmentCode : null;
                var errorAlertViewModel = new ClaimErrorAlertViewModel();
                errorAlertViewModel.Id = error.Id;
                errorAlertViewModel.Type = claimErrorMessage.Severity == ClaimErrorSeverity.Error ? "Error" : "Alert";
                errorAlertViewModel.Source = claimErrorMessage.ClaimErrorCategory.Name;

                errorAlertViewModel.ErrorCode = hasEraValidationError ?
                    $"{eraGropCode.Code}{(!String.IsNullOrEmpty(eraAdjustmentCode?.Code) ? "-" + eraAdjustmentCode?.Code : string.Empty)}" :
                    claimErrorMessage.ShortDescription;

                errorAlertViewModel.Description = hasEraValidationError ?
                    $"{eraGropCode.Description}{(!String.IsNullOrEmpty(eraAdjustmentCode?.Description) ? "-" + eraAdjustmentCode?.Description : string.Empty)}" :
                    claimErrorMessage.LongDescription;

                errorAlertViewModel.Message = error.ContextMessage;
                errorAlertViewModel.AdjustmentLevel = hasEraValidationError ? error.EraValidationError.AdjustmentLevel.ToString() : AdjustmentLevel.Claim.ToString();
                errorAlertViewModel.ClaimErrorSource = error.ClaimErrorSource;
                errorAlertViewModel.RefValidationId = error.RefValidationId;
                var responseDetails = clearingHouseResponseDetails.FirstOrDefault(x => x.ClaimValidationErrorId == error.Id);
                if (responseDetails != null)
                {
                    errorAlertViewModel.Type = "Response";
                    errorAlertViewModel.BatchId = responseDetails.BatchId;
                    errorAlertViewModel.FileType = responseDetails.ResponseFileTypeId == ClaimResponseFileType.File999 ? "999" : "277";
                    errorAlertViewModel.ResponseDate = responseDetails.DateCreated;

                    errorAlertViewModel.CodeDescription = new List<string>();
                    if (eraGropCode != null)
                        errorAlertViewModel.CodeDescription.Add($"{eraGropCode.Code} - {eraGropCode.Description}");
                    if (eraAdjustmentCode != null)
                        errorAlertViewModel.CodeDescription.Add($"{eraAdjustmentCode.Code} - {eraAdjustmentCode.Description}");
                }
                result.Add(errorAlertViewModel);
            }

            foreach (var item in clearingHouseResponseDetails.Where(x => x.ClaimValidationErrorId == 0))
            {
                var errorAlertViewModel = new ClaimErrorAlertViewModel();
                errorAlertViewModel.Type = "Response";
                errorAlertViewModel.Source = _claimErrorCategoryRepository.Query().FirstOrDefault(x => x.NumberBase == 1600)?.Name;
                errorAlertViewModel.Description = item.ResponseFileTypeId == ClaimResponseFileType.File999 ? "999 Accepted" : (item.IsAccepted == false ? "277 Received" : "277 Accepted");
                errorAlertViewModel.CodeDescription = new List<string>() { errorAlertViewModel.Description };
                errorAlertViewModel.BatchId = item.BatchId;
                errorAlertViewModel.FileType = item.ResponseFileTypeId == ClaimResponseFileType.File999 ? "999" : "277";
                errorAlertViewModel.ResponseDate = item.DateCreated;

                result.Add(errorAlertViewModel);
            }

            return result;
        }

        public async Task<ClaimErrorsSourcesModel> GetErrorsSourcesAsync()
        {
            var errorsSources = await _claimErrorCategoryRepository.Query().ToListAsync();

            var errorsSourcesList = new List<string>();

            foreach (var es in errorsSources)
            {
                errorsSourcesList.Add(es.Name);
            }

            var result = new ClaimErrorsSourcesModel
            {
                ErrorsSources = errorsSourcesList.ToArray()
            };

            return result;
        }

        public async Task<ClaimErrorsCodesModel> GetErrorsCodesAsync()
        {
            var errorsCodes = await _claimErrorMessageRepository.Query().ToListAsync();

            var errorsCodesList = new List<ClaimErrorsCodes>();

            foreach (var ec in errorsCodes)
            {
                var errorCode = new ClaimErrorsCodes
                {
                    Name = ec.ShortDescription
                };

                errorsCodesList.Add(errorCode);
            }

            var result = new ClaimErrorsCodesModel
            {
                ErrorsCodes = errorsCodesList.ToArray()
            };

            return result;
        }

        public async Task<int> SaveClaimAsync(ClaimSaveModelWithUserInfo model)
        {
            var claim = await _claimManagerService.InitializeClaim(model.MemberId,
                                                               model.AccountInfoId,
                                                               model.Claim.ClaimInfo.ClientId,
                                                               model.Claim.ClaimInfo.FunderId,
                                                               model.Claim.Provider.DateOfServiceStart,
                                                               model.Claim.Provider.DateOfServiceEnd);

            var saveModel = model.Claim;

            var authorizationNumberId = saveModel.ClaimInfo.AuthorizationNumberId;

            int limit = 50;
            var renderingProviderId = 0;

            // Check if there is only one billing code associated with the diagnosis code and use its rendering provider if available, otherwise use the rendering provider from the claim info
            var renderingProvidersAtChargeLevel = saveModel.DiagnosisCode?.BillingCodes?
                .Select(bcd => bcd.RenderingProviderStaffId)
                .Distinct()
                .ToList();

            // Check if there is only one billing code associated with the diagnosis code and use its rendering provider if available, otherwise use the rendering provider from the claim info
            if ((saveModel.DiagnosisCode?.BillingCodes?.Count == 1
                && saveModel.Provider?.RenderingProviderId != saveModel.DiagnosisCode?.BillingCodes[0]?.RenderingProviderStaffId)
                || (saveModel.DiagnosisCode?.BillingCodes?.Count > 1 && renderingProvidersAtChargeLevel.Count == 1))
            {
                renderingProviderId = saveModel.DiagnosisCode?.BillingCodes?[0].RenderingProviderStaffId ?? 0;
            }
            else
            {
                renderingProviderId = saveModel.Provider?.RenderingProviderId ?? 0;
            }

            if (authorizationNumberId.HasValue)
            {
                var authorizationData = await _rethinkServices.GetChildProfileAuthorizationByClientId(model.AccountInfoId, saveModel.ClaimInfo.ClientId, authorizationNumberId.Value);
                renderingProviderId = renderingProviderId == 0 ? authorizationData?.renderingProviderStaffId ?? 0 : renderingProviderId;

                claim.AuthorizationId = authorizationNumberId.Value;

                var authorization = authorizationData?.authorizationNumber?.ToString().Trim();
                claim.AuthorizationNumber = authorization.Length > limit ? authorization.Substring(0, limit) : authorization;

                var clientFunder = await _rethinkServices.GetChildProfileFunderMappings(model.AccountInfoId, saveModel.ClaimInfo.ClientId);
                var clientFunderMappings = clientFunder?.data?.FirstOrDefault(x => x.funderId == authorizationData?.funderId);

                var funderMappings = await _rethinkServices.GetChildProfileFunderMappingByMappingId(claim.AccountInfoId, saveModel.ClaimInfo.ClientId, clientFunderMappings.id);

                claim.ReleaseOfInformationConfirmationTypeId = funderMappings.releaseOfInformationConfirmationTypeId;
                claim.AuthorizedPaymentConfirmationTypeId = funderMappings.authorizedPaymentConfirmationTypeId;
                claim.BenefitAssignmentId = (funderMappings.isAutismCoveredBenefit == true || funderMappings.isAutismCoveredBenefit == null) ? 1 : 2;
            }
            if (model.BillingProviderRequest?.BillingProvider != null)
            {
                model.BillingProviderRequest.ClaimId = claim.Id;
                await AddUpdateBillingProviderAsync(model.MemberId, model.BillingProviderRequest);
            }

            if (saveModel.ClaimInfo.AllowManualAuthorization)
            {
                claim.AuthorizationNumber = saveModel.ClaimInfo.AuthorizationNumber.Trim();
            }

            var providerLocation = saveModel.Provider.BillingProviderId.HasValue ?
                await _rethinkServices.GetProviderLocation(model.AccountInfoId, (int)saveModel.Provider.BillingProviderId) :
                null;
            claim.ProviderLocationId = saveModel?.Provider.BillingProviderId;
            if (providerLocation != null
                && !providerLocation.isBillingLocation
                && !providerLocation.isMainLocation)
            {
                var mainLocation = await _rethinkServices.GetMainLocation(claim.AccountInfoId);
                claim.ProviderLocationId = mainLocation.id;
            }

            claim.PrimaryFunderId = saveModel.ClaimInfo.FunderId;
            claim.LastBilledFunderId = saveModel.ClaimInfo.FunderId;
            claim.ClientFunderId = saveModel.ClaimInfo.ClientFunderId;
            claim.ClientFunderServiceLineId = saveModel.ClaimInfo.ServiceLineId;
            claim.ClaimStatus = ClaimStatus.PendingReview;
            claim.RenderingStaffMemberId = renderingProviderId;
            var renderingProviders = await _rethinkServices.GetAllRenderingProvidersAsync(claim.AccountInfoId);
            var rpType = renderingProviders.data.FirstOrDefault(x => x.memberId == claim.RenderingStaffMemberId);
            if (rpType != null)
            {
                claim.RenderingProviderTypeId = rpType.id;
            }
            claim.ServiceLocationId = saveModel.Provider.ServiceFacilityLocationId;
            claim.ChildProfileReferringProviderId = saveModel.Provider.ReferringProviderId;
            claim.LocationCodeId = saveModel.ClaimInfo.PlaceOfServiceCodeId;
            claim.BillTo = saveModel.Provider.BillingProviderId;

            var funderMappings2 = await _rethinkServices.GetChildProfileFunderMappingByMappingId(claim.AccountInfoId, claim.ChildProfileId, claim.ClientFunderId.Value);
            claim.ReleaseOfInformationConfirmationTypeId = funderMappings2.releaseOfInformationConfirmationTypeId ?? (int)AuthoriseReleaseInfo.NoSingatureOnFile;
            claim.AuthorizedPaymentConfirmationTypeId = funderMappings2.authorizedPaymentConfirmationTypeId;
            claim.BenefitAssignmentId = (funderMappings2.isAutismCoveredBenefit == true || funderMappings2.isAutismCoveredBenefit == null) ? 1 : 2;

            // check if the secondary funder is available & update the flag
            claim.IsSecondaryPayerAvailable = false;
            var secondaryFunderDetails = await _claimUpdateService.CheckAndGetSecondaryFunderDetails(model.AccountInfoId, claim);
            if (secondaryFunderDetails != null && secondaryFunderDetails.funders.Any())
            {
                claim.IsSecondaryPayerAvailable = true;
            }

            await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
            {
                ClaimId = claim.Id,
                MemberId = model.MemberId,
                Mode = ClaimActionMode.User,
                ClaimAction = ClaimAction.Create,
                ClaimHistoryAction = ClaimHistoryAction.ClaimCreated,
                ActionDate = EstDateTime,
                ImpersonationUserName = saveModel.ImpersonationUserName
            });

            /*diagnosisCodes*/
            ClaimDiagnosisCodeEntity firstClaimDiagnosisCode = null;
            foreach (var diagnosisCode in saveModel.DiagnosisCode.DiagnosisCodesToSave)
            {
                var claimDiagnosisCode = new ClaimDiagnosisCodeEntity
                {
                    ClaimId = claim.Id,
                    DiagnosisId = diagnosisCode.DiagnosisId,
                    Order = diagnosisCode.Order,
                    IncludeOnClaims = diagnosisCode.IncludeOnClaims
                };

                MarkCreated(claimDiagnosisCode, model.MemberId);
                _claimDiagnosisCodeRepository.Add(claimDiagnosisCode);
                if (firstClaimDiagnosisCode == null)
                {
                    firstClaimDiagnosisCode = claimDiagnosisCode;
                }
            }
            /*billingCodes*/
            int? lastBillingCodeRenderingProviderId = null;
            foreach (var billingCode in saveModel.DiagnosisCode.BillingCodes)
            {
                var provider = await _rethinkServices.GetProviderBillingCode(claim.AccountInfoId, billingCode.BillingCodeId);
                var providerBillingCodes = new BillingCodeData()
                {
                    billingCode = provider.billingCode,
                    billingCode2 = provider.billingCode2,
                    id = provider.id
                };
                var providerBillingCode = providerBillingCodes;

                if (providerBillingCode == null)
                {
                    continue;
                }
                var claimChargeEntry = new ClaimChargeEntryEntity
                {
                    ClaimId = claim.Id,
                    BillingCode = billingCode.IsSecondaryCode ? providerBillingCode.billingCode2 : providerBillingCode.billingCode,
                    UnitTypeId = billingCode.UnitTypeId,
                    Units = billingCode.NoOfUnits,
                    DateOfService = billingCode.IndividualDateOfService,
                    Modifier1 = billingCode.Modifier1,
                    Modifier2 = billingCode.Modifier2,
                    Modifier3 = billingCode.Modifier3,
                    Modifier4 = billingCode.Modifier4,
                    UnitRate = billingCode.Rate,
                    Charges = billingCode.TotalCharges,
                    NoteText = billingCode.Note,
                    NoteCreatedBy = model.MemberId,
                    NoteCreatedDate = EstDateTime,
                    BillingCodeId = billingCode.BillingCodeId,
                    RenderingProviderId = billingCode.RenderingProviderStaffId
                };

                lastBillingCodeRenderingProviderId = billingCode.RenderingProviderStaffId;

                await AddClaimHistory(ClaimHistoryAction.ChargeEntryNoteAdded);
                await AddClaimHistory(ClaimHistoryAction.ChargeEntryNoteDescAdded);


                async Task AddClaimHistory(ClaimHistoryAction claimHistoryAction)
                {
                    await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                    {
                        ClaimId = claimChargeEntry.ClaimId,
                        MemberId = model.MemberId,
                        Mode = ClaimActionMode.User,
                        ClaimAction = ClaimAction.Added,
                        ClaimHistoryAction = claimHistoryAction,
                        NewValue = claimChargeEntry.NoteText,
                        ActionDate = EstDateTime,
                        ImpersonationUserName = saveModel.ImpersonationUserName
                    });
                }


                // RICH: I am not sure how this is supposed to work. The code was only setting the diagnosis code if there was only
                //       one in the dx code list. I changed it to set it to the first dxCode.
                if (firstClaimDiagnosisCode != null)
                {
                    var diagnos = await _rethinkServices.GetClientDiagnosisAsync(claim.AccountInfoId, claim.ChildProfileId);
                    var diagnosisCodes = diagnos.FirstOrDefault(x => x.diagnosisId == firstClaimDiagnosisCode.DiagnosisId);
                    DiagnosisEntityModel diagnosisEntity = null;
                    if (diagnosisCodes != null)
                    {
                        diagnosisEntity = new DiagnosisEntityModel()
                        {
                            Id = diagnosisCodes.diagnosisId,
                            DiagnosisCode = diagnosisCodes.diagnosisCode,
                        };
                    }
                    else
                    {
                        var diagnosisCode = await _rethinkServices.GetDiagnosisById(firstClaimDiagnosisCode.DiagnosisId);
                        diagnosisEntity = new DiagnosisEntityModel()
                        {
                            Id = diagnosisCode.id,
                            DiagnosisCode = diagnosisCode.diagnosisCode
                        };
                    }
                    var diagnosis = diagnosisEntity;
                    claimChargeEntry.DiagnosisCode = diagnosis.DiagnosisCode;
                }

                MarkCreated(claimChargeEntry, model.MemberId);
                _claimChargeEntryRepository.Add(claimChargeEntry);

            }

            await _claimRepository.CommitAsync();

            await _bus.SendAsync(new ClaimCreateEnd
            {
                ClaimId = claim.Id,
                AccountInfoId = claim.AccountInfoId,
                RenderingProviderId = lastBillingCodeRenderingProviderId ?? 0,
                RenderingProviderTypeId = claim.RenderingProviderTypeId,
                FunderId = claim.PrimaryFunderId,
                ClientId = claim.ChildProfileId,
                ChildProfileAuthorizationId = claim.AuthorizationId
            }, Queues.RT_Billing_ClaimCreationEnd);


            claimTransactionData = new List<ClaimTransactionModel>();
            foreach (var chargeEntryId in claim.ClaimChargeEntries)
            {
                claimTransactionData.Add(PrepareClaimTransaction(chargeEntryId.Id, ClaimTransactionType.billedAmount));
            }
            sendMessage(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);

            return claim.Id;
        }


        public async Task<List<ClaimApprovalResponseModel>> ApproveClaimsAsync(int accountInfoId, int memberId, int[] claimsIds)
        {
            claimTransactionData = new List<ClaimTransactionModel>();
            var claimErrors = new List<ClaimApprovalResponseModel>();

            var claimsList = await _claimRepository.Query()
                .Where(x => claimsIds.Contains(x.Id))
                .ToListAsync();

            var claimsDict = claimsList.ToDictionary(c => c.Id);

            var mappingTasks = claimsList.ToDictionary(
                c => c.Id,
                c => _rethinkServices.GetChildProfileFunderMappingByMappingId(accountInfoId, c.ChildProfileId, c.ClientFunderId.Value)
            );

            await Task.WhenAll(mappingTasks.Values);

            var mappings = mappingTasks.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.Result
            );

            foreach (var claimId in claimsIds)
            {
                if (!claimsDict.TryGetValue(claimId, out var claim))
                {
                    claimErrors.Add(new ClaimApprovalResponseModel
                    {
                        Claimid = claimId,
                        Error = "Claim not found."
                    });

                    await PrepareClaimError("Claim not found", ClaimErrorNumber.FunderNotFound, claimId, memberId);
                    throw new ArgumentNullException("Claim not found");
                }

                var mapping = mappings[claimId];

                if (mapping == null)
                {
                    claimErrors.Add(new ClaimApprovalResponseModel
                    {
                        Claimid = claim.Id,
                        Error = "Claim approval Failed — Please add the Funder first, then try again."
                    });

                    await PrepareClaimError("Claim approval Failed — Please add the Funder first, then try again.", ClaimErrorNumber.FunderNotFound, claimId, memberId);
                    claim.ClaimStatus = ClaimStatus.ApprovalFailed;
                    MarkUpdated(claim, memberId);
                    await _claimRepository.CommitAsync();
                    throw new ArgumentNullException("Claim approval Failed — Please add the Funder first, then try again.");
                }

                if (mapping.insuranceType == ResponsibilitySequenceType.Secondary)
                {
                    claimErrors.Add(new ClaimApprovalResponseModel
                    {
                        Claimid = claim.Id,
                        Error = "Claim approval pending — update Secondary Funder to Primary and complete the appointment."
                    });

                    await PrepareClaimError("Claim approval pending — update Secondary Funder to Primary and complete the appointment.", ClaimErrorNumber.FunderNotFound, claimId, memberId);
                    claim.ClaimStatus = ClaimStatus.ApprovalFailed;
                    MarkUpdated(claim, memberId);
                    await _claimRepository.CommitAsync();
                    throw new ArgumentNullException("Claim approval pending — update Secondary Funder to Primary and complete the appointment.");
                }

                else
                {
                    var respsonibilitySequence = await GetClaimResponsibilitySequenceAsync(claim.AccountInfoId, claim.ChildProfileId, claim.ClientFunderId, claim.ClientFunderServiceLineId);
                    try
                    {
                        await _claimManagerService.SubmitInitialClaim(claimId, memberId, ClaimDocumentType.Doc837P, respsonibilitySequence);
                        if (claim.ClaimStatus == ClaimStatus.PendingReview)
                        {
                            await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                            {
                                ClaimId = claim.Id,
                                MemberId = memberId,
                                Mode = ClaimActionMode.User,
                                ClaimAction = ClaimAction.Approve,
                                ClaimHistoryAction = ClaimHistoryAction.MovedToReadyToBill,
                                OldValue = $"{claim.ClaimStatus}",
                            });

                            claim.ClaimStatus = ClaimStatus.ReadyToBill;

                            MarkUpdated(claim, memberId);
                            var entry = _claimRepository.Entry(claim);
                            if (entry?.Context != null)
                            {
                                entry.Context.ChangeTracker.Clear();
                                entry.State = EntityState.Modified;
                            }

                            _claimRepository.Update(claim);
                            await _claimRepository.CommitAsync();
                        }
                        claimTransactionData.Add(PrepareClaimTransaction(claimId, ClaimTransactionType.submitClaim));
                        claimErrors.Add(new ClaimApprovalResponseModel
                        {
                            Claimid = claim.Id,
                        });
                    }
                    catch (Exception ex)
                    {
                        //Updating claim Status
                        claim.ClaimStatus = ClaimStatus.ApprovalFailed;
                        MarkUpdated(claim, memberId);
                        await _claimRepository.CommitAsync();

                        claimErrors.Add(new ClaimApprovalResponseModel
                        {
                            Claimid = claim.Id,
                            Error = $"{ex.InnerException?.Message ?? ex.Message}"
                        });

                        await PrepareClaimError("Claim approval Failed", ClaimErrorNumber.FunderNotFound, claimId, memberId);
                        throw new ArgumentNullException("Claim approval Failed");
                    }
                }
            }

            sendMessage(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);

            return claimErrors;
        }

        public async Task<int[]> UnapproveClaimsAsync(int accountInfoId, int memberId, int[] claimsIds)
        {
            claimTransactionData = new List<ClaimTransactionModel>();
            foreach (var claimId in claimsIds)
            {
                var claim = _claimRepository.Query().FirstOrDefault(x => x.Id == claimId);
                if (claim != null && claim.ClaimStatus == ClaimStatus.ReadyToBill || claim.ClaimStatus == ClaimStatus.Rebill)
                {
                    await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                    {
                        ClaimId = claim.Id,
                        MemberId = memberId,
                        Mode = ClaimActionMode.User,
                        ClaimAction = ClaimAction.Unapprove,
                        ClaimHistoryAction = ClaimHistoryAction.MovedToPendingReview,
                        OldValue = $"{claim.ClaimStatus}",
                    });

                    claim.ClaimStatus = ClaimStatus.PendingReview;

                    MarkUpdated(claim, memberId);
                    _claimRepository.Update(claim);
                    claimTransactionData.Add(PrepareClaimTransaction(claimId, ClaimTransactionType.submitClaim));

                }
            }

            await _claimRepository.CommitAsync();

            sendMessage(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);

            return claimsIds;
        }

        public async Task<int[]> FlagClaimsAsync(int accountInfoId, int memberId, int[] claimsIds, string impersonationUserName = null)
        {
            List<int> flagIds = new List<int>();
            claimTransactionData = new List<ClaimTransactionModel>();
            foreach (var claimId in claimsIds)
            {
                var claim = _claimRepository.Query().FirstOrDefault(x => x.Id == claimId);
                if (claim.IsFlagged != true)
                {
                    claim.IsFlagged = true;

                    MarkUpdated(claim, memberId);
                    _claimRepository.Update(claim);

                    await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                    {
                        ClaimId = claimId,
                        MemberId = memberId,
                        Mode = ClaimActionMode.User,
                        ClaimAction = ClaimAction.Flag,
                        ClaimHistoryAction = ClaimHistoryAction.Flagged,
                        ImpersonationUserName = impersonationUserName,

                    }, false);
                    flagIds.Add(claimId);
                    claimTransactionData.Add(PrepareClaimTransaction(claimId, ClaimTransactionType.submitClaim));

                }
            }

            await _claimRepository.CommitAsync();
            int[] flags = flagIds.ToArray();

            sendMessage(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);

            return flags;
        }

        public async Task<int[]> FlagClaimsAsync(int accountInfoId, int memberId, int[] claimIds, int[] reasonIds, string? notes, int? claimReasonTransactionId = null, string impersonationUserName = null)
        {
            var now = DateTime.UtcNow;
            claimTransactionData = new List<ClaimTransactionModel>();

            // Fetch all claims
            var claims = _claimRepository.Query()
                .Where(c => claimIds.Contains(c.Id))
                .ToList();

            if (!claims.Any())
                return Array.Empty<int>();


            if (!claimReasonTransactionId.HasValue)
            {
                // Add mode: flag unflagged claims
                foreach (var claim in claims.Where(c => c.IsFlagged != true))
                {
                    claim.IsFlagged = true;
                    MarkUpdated(claim, memberId);
                }
                _claimRepository.UpdateRange(claims);

                // Add claim history
                var histories = claims.Select(c => new ClaimHistorySaveModel
                {
                    ClaimId = c.Id,
                    MemberId = memberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.Flag,
                    ClaimHistoryAction = ClaimHistoryAction.Flagged,
                    ImpersonationUserName = impersonationUserName,
                }).ToList();
                await _claimHistoryService.AddAsync(histories, false);
            }
            else
            {
                // Edit mode: soft-delete the existing ClaimFlagTransaction
                var existingTxn = _claimFlagTransactionRepository.Query()
                    .FirstOrDefault(t => t.Id == claimReasonTransactionId.Value && t.DateDeleted == null);

                if (existingTxn != null)
                {
                    existingTxn.DateDeleted = EstDateTime;
                    existingTxn.ModifiedBy = memberId;
                    existingTxn.DateLastModified = EstDateTime;
                    _claimFlagTransactionRepository.Update(existingTxn);
                }
            }

            // Prepare new transactions in-memory
            var newTransactions = new List<ClaimFlagTransaction>();
            foreach (var claim in claims)
            {
                foreach (var reasonId in reasonIds)
                {
                    newTransactions.Add(new ClaimFlagTransaction
                    {
                        AccountInfoId = accountInfoId,
                        HcClaimId = claim.Id,
                        ReasonId = reasonId,
                        Comment = notes,
                        ActionType = claimReasonTransactionId.HasValue ? ClaimFlagActionMode.Updated.GetDescription() : ClaimFlagActionMode.Flagged.GetDescription(),
                        DateCreated = EstDateTime,
                        CreatedBy = memberId
                    });
                }
            }

            await _claimFlagTransactionRepository.AddRangeAsync(newTransactions); // batch insert
            await _claimRepository.CommitAsync();

            if (!claimReasonTransactionId.HasValue)
            {
                foreach (var claim in claims)
                {
                    claimTransactionData.Add(PrepareClaimTransaction(claim.Id, ClaimTransactionType.submitClaim));
                }
                sendMessage(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);
            }

            return claims.Select(c => c.Id).ToArray();
        }



        public async Task<int[]> UnflagClaimsAsync(int accountInfoId, int memberId, int[] claimsIds, string impersonationUserName = null)
        {
            List<int> unFlagIds = new List<int>();
            claimTransactionData = new List<ClaimTransactionModel>();
            foreach (var claimId in claimsIds)
            {
                var claim = _claimRepository.Query().FirstOrDefault(x => x.Id == claimId);
                if (claim != null && claim.IsFlagged != false)
                {
                    claim.IsFlagged = false;
                    MarkUpdated(claim, memberId);
                    _claimRepository.Update(claim);

                    var activeFlagTransactions = _claimFlagTransactionRepository.Query()
                    .Where(t => t.HcClaimId == claimId && t.DateDeleted == null).ToList();

                    if (activeFlagTransactions.Any())
                    {
                        foreach (var txn in activeFlagTransactions)
                        {
                            txn.DateDeleted = DateTime.UtcNow;
                            txn.ModifiedBy = memberId;
                            txn.DateLastModified = DateTime.UtcNow;
                            txn.ActionType = ClaimFlagActionMode.Unflagged.GetDescription();
                            _claimFlagTransactionRepository.Update(txn);
                        }
                    }

                    await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                    {
                        ClaimId = claimId,
                        MemberId = memberId,
                        Mode = ClaimActionMode.User,
                        ClaimAction = ClaimAction.Unflag,
                        ClaimHistoryAction = ClaimHistoryAction.Unflagged,
                        ImpersonationUserName = impersonationUserName,
                    }, false);
                    unFlagIds.Add(claimId);
                    claimTransactionData.Add(PrepareClaimTransaction(claimId, ClaimTransactionType.submitClaim));
                }
            }

            await _claimRepository.CommitAsync();
            await _claimFlagTransactionRepository.CommitAsync();
            int[] unFlags = unFlagIds.ToArray();
            sendMessage(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);


            return unFlags;
        }

        public async Task<List<ClaimDeleteResultModel>> DeleteClaimsAsync(int accountInfoId, int memberId, int[] claimIds, string? impersonationUserName = null)
        {
            List<AppointmentBillingStatus> apptBillingStatus = [];
            var claimIdentifiers = new List<ClaimDeleteResultModel>();
            claimTransactionData = new List<ClaimTransactionModel>();
            foreach (var claimId in claimIds)
            {
                var claim = _claimRepository.Query().FirstOrDefault(x => x.Id == claimId);
                if (claim != null)
                {
                    SoftDelete(claim, memberId);
                    _claimRepository.Entry(claim).Context.ChangeTracker.Clear();
                    _claimRepository.Entry(claim).State = EntityState.Modified;
                    _claimRepository.Update(claim);

                    // claim appointment link
                    var linkedAppointments = await _claimAppointmentLinkRepository.Query().Where(x => x.ClaimId == claim.Id).ToListAsync();
                    if (linkedAppointments.Any())
                    {
                        foreach (var link in linkedAppointments)
                        {
                            SoftDelete(link, memberId);
                            _claimAppointmentLinkRepository.Update(link);
                            apptBillingStatus.Add(PrepareAppointmentBillingStatus(link.AppointmentId, RethinkBillingStatus.NotBilled));
                        }
                        await _claimAppointmentLinkRepository.CommitAsync();
                        if (apptBillingStatus.Count != 0)
                        {
                            await _bus.SendBatchAsync(Queues.RT_Billing_Queue_AppointmentBillingStatus, apptBillingStatus);
                        }
                    }

                    //claim charge entries
                    var chargeEntries = await _claimChargeEntryRepository.Query().Where(x => x.ClaimId == claimId).ToListAsync();
                    if (chargeEntries.Any())
                    {
                        foreach (var chargeEntry in chargeEntries)
                        {
                            SoftDelete(chargeEntry, memberId);
                            _claimChargeEntryRepository.Update(chargeEntry);

                            var linkChargeEntry = await _claimAppointmentChargeEntryEntityRepository.Query().Where(x => x.ClaimChargeEntryEntityId == chargeEntry.Id).ToListAsync();
                            foreach (var linkCharge in linkChargeEntry)
                            {
                                SoftDelete(linkCharge, memberId);
                                _claimAppointmentChargeEntryEntityRepository.Update(linkCharge);
                            }
                            await _claimAppointmentChargeEntryEntityRepository.CommitAsync();

                            var writeOffs = await _claimChargeEntryWriteOffRepository.Query().Where(x => x.ClaimChargeEntryId == chargeEntry.Id && x.DateDeleted == null).ToListAsync();

                            if (writeOffs.Any())
                            {
                                foreach (var writeOff in writeOffs)
                                {
                                    SoftDelete(writeOff, memberId);
                                    _claimChargeEntryWriteOffRepository.Update(writeOff);
                                }
                                await _claimChargeEntryWriteOffRepository.CommitAsync();
                            }

                            // code to delete patient invoice if deleted the charge
                            var patientInvoiceDetails = await _patientInvoiceDetailsRepository.Query().Where(p => p.ChargeId == chargeEntry.Id).ToListAsync();
                            if (patientInvoiceDetails.Any())
                            {
                                foreach (var item in patientInvoiceDetails)
                                {
                                    SoftDelete(item, memberId);
                                    _patientInvoiceDetailsRepository.Update(item);
                                }
                                await _patientInvoiceDetailsRepository.CommitAsync();
                            }
                            claimTransactionData.Add(PrepareClaimTransaction(chargeEntry.Id, ClaimTransactionType.deleteCharge));
                        }
                        await _claimChargeEntryRepository.CommitAsync();
                    }

                    // writeoffs
                    var claimWriteOffs = await _claimWriteOffRepository.Query().Where(x => x.ClaimId == claimId).ToListAsync();
                    if (claimWriteOffs.Any())
                    {
                        foreach (var writeOff in claimWriteOffs)
                        {
                            SoftDelete(writeOff, memberId);
                            _claimWriteOffRepository.Update(writeOff);
                        }
                        await _claimWriteOffRepository.CommitAsync();
                    }

                    // payments
                    var paymentClaims = await _paymentClaimRepository.Query().Where(x => x.ClaimId == claimId && x.DateDeleted == null)
                                                        .Include(x => x.PaymentClaimAdjustments)
                                                        .Include(x => x.PaymentClaimServiceLines)
                                                        .ThenInclude(y => y.PaymentClaimServiceLineAdjustments)
                                                        .ToListAsync();

                    if (paymentClaims.Any())
                    {
                        foreach (var paymentClaim in paymentClaims)
                        {
                            SoftDelete(paymentClaim, memberId);
                            _paymentClaimRepository.Update(paymentClaim);
                            foreach (var serviceLineCharge in paymentClaim.PaymentClaimServiceLines)
                            {
                                SoftDelete(serviceLineCharge, memberId);
                                _paymentClaimServiceLineRepository.Update(serviceLineCharge);

                                var adjustments = await _paymentClaimServiceLineAdjustmentRepository.Query().Where(x => x.PaymentClaimServiceLineId == serviceLineCharge.Id).ToListAsync();
                                foreach (var adjustment in adjustments)
                                {
                                    SoftDelete(adjustment, memberId);
                                    _paymentClaimServiceLineAdjustmentRepository.Update(adjustment);
                                }
                            }

                            foreach (var claimAdjustment in paymentClaim.PaymentClaimAdjustments)
                            {
                                SoftDelete(claimAdjustment, memberId);
                                _claimAdjustmentRepository.Update(claimAdjustment);
                            }
                            await _claimAdjustmentRepository.CommitAsync();
                        }
                    }

                    await _paymentClaimServiceLineAdjustmentRepository.CommitAsync();
                    await _paymentClaimServiceLineRepository.CommitAsync();
                    await _paymentClaimRepository.CommitAsync();

                    // claim attachments
                    var claimAttachments = await _claimAttachmentRepository.Query().Where(x => x.ClaimId == claimId).ToListAsync();
                    if (claimAttachments.Any())
                    {
                        foreach (var attachment in claimAttachments)
                        {
                            SoftDelete(attachment, memberId);
                            _claimAttachmentRepository.Update(attachment);
                        }
                        await _claimAttachmentRepository.CommitAsync();
                    }

                    // claim diagnosis codes
                    var claimDiagnosisCodes = await _claimDiagnosisCodeRepository.Query().Where(x => x.ClaimId == claimId).ToListAsync();
                    if (claimDiagnosisCodes.Any())
                    {
                        foreach (var dia in claimDiagnosisCodes)
                        {
                            SoftDelete(dia, memberId);
                            _claimDiagnosisCodeRepository.Update(dia);
                        }
                        await _claimDiagnosisCodeRepository.CommitAsync();
                    }
                    // claim notes
                    var claimNotes = await _claimNoteRepository.Query().Where(x => x.ClaimId == claimId).ToListAsync();
                    if (claimNotes.Any())
                    {
                        foreach (var note in claimNotes)
                        {
                            SoftDelete(note, memberId);
                            _claimNoteRepository.Update(note);
                        }
                        await _claimNoteRepository.CommitAsync();
                    }
                    // claim notes
                    var errors = await _claimValidationErrorRepository.Query().Where(x => x.ClaimId == claimId).ToListAsync();
                    if (errors.Any())
                    {
                        foreach (var error in errors)
                        {
                            SoftDelete(error, memberId);
                            _claimValidationErrorRepository.Update(error);
                        }
                        await _claimValidationErrorRepository.CommitAsync();
                    }

                    // claim submissions
                    var submissions = await _claimSubmissionRepository.Query().Where(x => x.ClaimId == claimId).ToListAsync();
                    foreach (var submission in submissions)
                    {
                        SoftDelete(submission, memberId);
                        _claimSubmissionRepository.Update(submission);

                        var submissionServiceLines = await _claimSubmissionServiceLineRepository.Query().Where(x => x.ClaimSubmissionId == submission.Id).ToListAsync();
                        foreach (var submissionSL in submissionServiceLines)
                        {
                            SoftDelete(submissionSL, memberId);
                            _claimSubmissionServiceLineRepository.Update(submissionSL);
                        }
                        await _claimSubmissionServiceLineRepository.CommitAsync();
                    }

                    await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                    {
                        ClaimId = claimId,
                        MemberId = memberId,
                        Mode = ClaimActionMode.User,
                        ClaimAction = ClaimAction.Delete,
                        ClaimHistoryAction = ClaimHistoryAction.Deleted,
                        ImpersonationUserName = impersonationUserName
                    });

                    claimIdentifiers.Add(new ClaimDeleteResultModel
                    {
                        Id = claim.Id,
                        ClaimIdentifier = claim.ClaimIdentifier,
                        AppointmentIds = linkedAppointments.Select(x => x.AppointmentId)
                    });
                    claimTransactionData.Add(PrepareClaimTransaction(claim.Id, ClaimTransactionType.deleteClaim));
                }
            }
            await _claimRepository.CommitAsync();

            sendMessage(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);

            return claimIdentifiers;
        }

        public async Task<int[]> MarkBilledClaimsAsync(int accountInfoId, int memberId, int[] claimsIds)
        {
            claimTransactionData = new List<ClaimTransactionModel>();
            List<AppointmentBillingStatus> apptBillingStatus = [];

            foreach (var claimId in claimsIds)
            {
                await _claimManagerService.UpdateClaimStatusAsync(claimId, ClaimStatus.Billed, memberId, false, true);
                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = claimId,
                    MemberId = memberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.Printed,
                    ClaimHistoryAction = ClaimHistoryAction.BilledPaper
                });

                var claimAppointmentsIds = await _claimAppointmentLinkRepository.Query()
                                                .Where(x => x.DateDeleted == null && x.ClaimId == claimId)
                                                .Select(x => x.AppointmentId)
                                                .ToListAsync();

                foreach (var appointmentId in claimAppointmentsIds)
                {
                    apptBillingStatus.Add(PrepareAppointmentBillingStatus(appointmentId, RethinkBillingStatus.Billed));
                }

                claimTransactionData.Add(PrepareClaimTransaction(claimId, ClaimTransactionType.submitClaim));
            }

            await _claimRepository.CommitAsync();

            if (apptBillingStatus.Count != 0)
            {
                await _bus.SendBatchAsync(Queues.RT_Billing_Queue_AppointmentBillingStatus, apptBillingStatus);
            }

            sendMessage(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);

            return claimsIds;
        }

        public async Task<List<string>> SubmitClaimsAsync(ClaimsSubmitModel model)
        {
            var claimIdentifiers = new List<string>();
            List<ClaimTransactionModel> claimTransactionData = [];

            List<AppointmentBillingStatus> apptBillingStatus = [];
            foreach (var claimId in model.Ids)
            {
                var claim = new ClaimEntity();
                try
                {
                    claim = _claimRepository.Query().Include(x => x.ClaimSubmissions).FirstOrDefault(x => x.Id == claimId);
                    if (claim != null)
                    {
                        var clearingHouseId = await _rethinkServices.GetClearingHouseId(claim.AccountInfoId);
                        if (clearingHouseId != 0)
                        {
                            var pendingSubmission = claim.ClaimSubmissions
                                .OrderByDescending(x => x.DateCreated)
                                .FirstOrDefault(x => x.SubmissionStatus == ClaimSubmissionStatus.FunderPending ||
                                                     x.SubmissionStatus == ClaimSubmissionStatus.ClearingHousePending);

                            var submitModel = new ClaimSubmitModel
                            {
                                Id = claim.Id,
                                AccountInfoId = model.AccountInfoId,
                                ClaimStatus = claim.ClaimStatus,
                                MemberId = model.MemberId,
                                ClearinghouseId = clearingHouseId,
                                FrequencyTypeId = claim.FrequencyTypeId,
                                IsSecondary = model.IsSecondary,
                                AdjustmentLevel = model.IsSecondary ? model.AdjustmentLevel : AdjustmentLevel.Claim,
                            };

                            if (pendingSubmission != null) submitModel.PendingClaimSubmissionId = pendingSubmission.Id;

                            var claimVersionId = await _claimVersionService.CreateAsync(
                                await GetClaimDetailsAsync(new IdWithUserInfo
                                {
                                    Id = claim.Id,
                                    AccountInfoId = claim.AccountInfoId,
                                    MemberId = claim.MemberId
                                }, null), model.AccountInfoId, model.MemberId);

                            var claimHistoryVersionSaveModel = new ClaimHistoryVersionSaveModel
                            {
                                ClaimId = claim.Id,
                                MemberId = model.MemberId,
                                Mode = ClaimActionMode.User,
                                ClaimVersionId = claimVersionId,
                                ClaimHistoryAction = ClaimHistoryAction.MovedToBilledPending,
                                ClaimAction = ClaimAction.Submit,
                                ImpersonationUserName = model.ImpersonationUserName
                            };

                            if (model.IsSecondary)
                            {
                                var funderDetails = model.SecondaryFunderDetails.FirstOrDefault(x => x.ClaimId == claimId);
                                await _claimManagerService.SubmitClaimTransfer(claimId, model.MemberId, claim.FrequencyTypeId ?? ClaimFrequencyType.Original, ClaimDocumentType.Doc837P, funderDetails.SecondaryFunderId, funderDetails.ControlNumber);
                                claimHistoryVersionSaveModel.ClaimHistoryAction = ClaimHistoryAction.BillNextFunder;
                                claimHistoryVersionSaveModel.ClaimAction = ClaimAction.BillNextFunder;
                                claimHistoryVersionSaveModel.OldValue = $"{claim.ClaimStatus}";
                            }
                            await _claimHistoryService.AddAsync(claimHistoryVersionSaveModel, true);
                            await _clearingHouseService.SubmitClaimAsync(submitModel);
                        }
                        claim.billedDate = EstDateTime;
                        claim.ClaimStatus = model.IsSecondary ? ClaimStatus.BillNextFunder : ClaimStatus.Pending;
                        _claimRepository.Entry(claim).Context.ChangeTracker.Clear();
                        _claimRepository.Entry(claim).State = EntityState.Modified;
                        _claimRepository.Update(claim);
                        await _claimRepository.CommitAsync();

                        claimIdentifiers.Add(claim.ClaimIdentifier);

                        var claimAppointmentsIds = await _claimAppointmentLinkRepository.Query()
                                                    .Where(x => x.DateDeleted == null && x.ClaimId == claim.Id)
                                                    .Select(x => x.AppointmentId)
                                                    .ToListAsync();

                        foreach (var appointmentId in claimAppointmentsIds)
                        {
                            apptBillingStatus.Add(PrepareAppointmentBillingStatus(appointmentId, RethinkBillingStatus.Billed));
                        }

                        claimTransactionData.Add(PrepareClaimTransaction(claimId, ClaimTransactionType.submitClaim));
                        // NO NEED TO CALL THIS AS VALIDATE IS HAPPENING ON EDIT OF CLAIM
                        //await _claimManagerService.ValidateClaimData(claimId, memberId, null);
                    }
                }
                catch (Exception ex)
                {
                    // update the claim status to 'Submission Failed'
                    claim.ClaimStatus = ClaimStatus.SubmissionFailed;
                    _claimRepository.Update(claim);
                    await _claimRepository.CommitAsync();

                    throw ex;
                }
            }

            if (apptBillingStatus.Count != 0)
            {
                await _bus.SendBatchAsync(Queues.RT_Billing_Queue_AppointmentBillingStatus, apptBillingStatus);
            }
            sendMessage(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);

            return claimIdentifiers;
        }

        public async Task<List<string>> VoidClaimsAsync(int accountInfoId, int memberId, ClaimsVoidModel claimsToVoid, int? clearingHouseId)
        {
            var claimIdentifiers = new List<string>();
            claimTransactionData = new List<ClaimTransactionModel>();
            foreach (var claimId in claimsToVoid.ClaimIds)
            {
                var claim = _claimRepository.Query().Include(x => x.ClaimSubmissions).FirstOrDefault(x => x.Id == claimId);
                if (claim != null && claim.ClaimStatus != ClaimStatus.Closed && claim.ClaimStatus != ClaimStatus.VoidClosed)
                {
                    DateTime reminddate = DateTime.Now;

                    var Note = new ClaimNoteEntity
                    {
                        RemindDate = reminddate,
                        Note = claimsToVoid.claimNote,
                        ClaimId = claim.Id
                    };

                    MarkCreated(Note, memberId);
                    _claimNoteRepository.Add(Note);
                    var submitToClearinghouse = claimsToVoid.SubmitToClearinghouse;

                    claim.ClaimStatus = submitToClearinghouse ? ClaimStatus.Void : ClaimStatus.VoidClosed;
                    claim.Note = claimsToVoid.Note;

                    _claimRepository.Entry(claim).Context.ChangeTracker.Clear();
                    _claimRepository.Entry(claim).State = EntityState.Modified;
                    _claimRepository.Update(claim);

                    await _claimRepository.CommitAsync();

                    if (submitToClearinghouse)
                    {
                        var oldFrequencyTypeId = claim.FrequencyTypeId;
                        var voidFrequencyTypeId = ClaimFrequencyType.Void;
                        claim.FrequencyTypeId = voidFrequencyTypeId;

                        await _claimManagerService.SubmitClaimRebill(claimId, memberId, voidFrequencyTypeId);
                        await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                        {
                            ClaimId = claim.Id,
                            MemberId = memberId,
                            Mode = ClaimActionMode.User,
                            ClaimAction = ClaimAction.Void,
                            ClaimHistoryAction = ClaimHistoryAction.MovedToReadyToBill,
                            OldValue = $"{claim.ClaimStatus}"
                        });

                        await _claimHistoryService.AddAsync(new ClaimHistoryFieldSaveModel
                        {
                            ClaimId = claim.Id,
                            MemberId = memberId,
                            Mode = ClaimActionMode.User,
                            ClaimAction = ClaimAction.Void,
                            ClaimHistoryAction = ClaimHistoryAction.FrequencyCodeUpdated,
                            ClaimHistoryField = ClaimHistoryField.FrequencyCode,
                            OldValue = $"{oldFrequencyTypeId}",
                            NewValue = $"{voidFrequencyTypeId}"
                        });
                    }
                    else
                    {
                        await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                        {
                            ClaimId = claim.Id,
                            MemberId = memberId,
                            Mode = ClaimActionMode.User,
                            ClaimAction = ClaimAction.Void,
                            ClaimHistoryAction = ClaimHistoryAction.MovedToClosed,
                            OldValue = $"{claim.ClaimStatus}"
                        });
                    }


                    await _claimNoteRepository.CommitAsync();

                    claimIdentifiers.Add(claim.ClaimIdentifier);
                    claimTransactionData.Add(PrepareClaimTransaction(claimId, ClaimTransactionType.submitClaim));
                }
            }

            sendMessage(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);

            return claimIdentifiers;
        }

        public async Task<List<string>> CompleteSelectedClaimsAsync(int[] claimsToCompleteIds, int accountInfoId, int memberId)
        {
            var claimIdentifiers = new List<string>();
            foreach (var claimId in claimsToCompleteIds)
            {
                var claim = _claimRepository.Query().Include(x => x.ClaimSubmissions).FirstOrDefault(x => x.Id == claimId);
                if (claim != null)
                {
                    await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                    {
                        ClaimId = claim.Id,
                        MemberId = memberId,
                        Mode = ClaimActionMode.User,
                        ClaimAction = ClaimAction.Completed,
                        ClaimHistoryAction = ClaimHistoryAction.MovedToClosed,
                        OldValue = $"{claim.ClaimStatus}"
                    });

                    claim.ClaimStatus = ClaimStatus.Closed;
                    _claimRepository.Entry(claim).Context.ChangeTracker.Clear();
                    _claimRepository.Entry(claim).State = EntityState.Modified;
                    _claimRepository.Update(claim);

                    claimIdentifiers.Add(claim.ClaimIdentifier);
                }
            }

            await _claimRepository.CommitAsync();

            return claimIdentifiers;
        }

        public async Task<List<string>> RebillClaimsAsync(int accountInfoId, int memberId, ClaimsRebillModel claimsToRebill, int? clearingHouseId = 0)
        {
            var claimIdentifiers = new List<string>();
            claimTransactionData = new List<ClaimTransactionModel>();
            foreach (var claimId in claimsToRebill.ClaimIds)
            {
                var claim = _claimRepository.Query().Include(x => x.ClaimSubmissions).FirstOrDefault(x => x.Id == claimId);
                var initialClaimStatus = claim.ClaimStatus;

                if (claim.ClaimStatus == ClaimStatus.PendingReview || claim.ClaimStatus == ClaimStatus.ReadyToBill || claim.ClaimStatus == ClaimStatus.Rebill)
                {
                    claimIdentifiers.Add(claim.ClaimIdentifier);
                    continue;
                }
                if (claim != null && claim.ClaimStatus != ClaimStatus.Rebill)
                {
                    try
                    {
                        var oldFrequencyTypeId = claim.FrequencyTypeId;
                        var newFrequencyTypeId = (ClaimFrequencyType)claimsToRebill.SubmissionReasonCode;

                        DateTime dateNow = DateTime.Now;
                        string formattedDateNow = dateNow.ToString("d");
                        if (claimsToRebill.Note != "") { claim.Note = claim.Note + formattedDateNow + "-RE-" + claimsToRebill.Note + "\n"; }

                        claim.Id = claimId;
                        claim.ClaimStatus = ClaimStatus.Rebill;
                        claim.FrequencyTypeId = newFrequencyTypeId;

                        var Note = new ClaimNoteEntity
                        {
                            RemindDate = DateTime.Now,
                            Note = claimsToRebill.ClaimNote,
                            ClaimId = claimId
                        };

                        await _claimManagerService.SubmitClaimRebill(claimId, memberId, newFrequencyTypeId);
                        _claimRepository.Entry(claim).Context.ChangeTracker.Clear();
                        _claimRepository.Entry(claim).State = EntityState.Modified;
                        _claimRepository.Update(claim);
                        await _claimRepository.CommitAsync();

                        MarkCreated(Note, memberId);
                        _claimNoteRepository.Add(Note);
                        await _claimNoteRepository.CommitAsync();

                        await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                        {
                            ClaimId = claim.Id,
                            MemberId = memberId,
                            Mode = ClaimActionMode.User,
                            ClaimAction = ClaimAction.Rebill,
                            ClaimHistoryAction = ClaimHistoryAction.RebilledInternalReason,
                            NewValue = claimsToRebill.RebillReason
                        });

                        var versionId = await _claimVersionService.CreateAsync(
                        await GetClaimDetailsAsync(new IdWithUserInfo
                        {
                            Id = claim.Id,
                            AccountInfoId = claim.AccountInfoId,
                            MemberId = claim.MemberId
                        }, null), accountInfoId, memberId);

                        await _claimHistoryService.AddAsync(new ClaimHistoryVersionSaveModel
                        {
                            ClaimId = claim.Id,
                            MemberId = memberId,
                            Mode = ClaimActionMode.User,
                            ClaimAction = ClaimAction.Rebill,
                            ClaimHistoryAction = ClaimHistoryAction.MovedToReadyToBill,
                            ClaimVersionId = versionId,
                            OldValue = $"{claim.ClaimStatus}"
                        });

                        await _claimHistoryService.AddAsync(new ClaimHistoryFieldSaveModel
                        {
                            ClaimId = claim.Id,
                            MemberId = memberId,
                            Mode = ClaimActionMode.User,
                            ClaimAction = ClaimAction.Rebill,
                            ClaimHistoryAction = ClaimHistoryAction.FrequencyCodeUpdated,
                            ClaimHistoryField = ClaimHistoryField.FrequencyCode,
                            OldValue = $"{oldFrequencyTypeId}",
                            NewValue = $"{newFrequencyTypeId}"
                        });

                        claimIdentifiers.Add(claim.ClaimIdentifier);
                        claimTransactionData.Add(PrepareClaimTransaction(claimId, ClaimTransactionType.submitClaim));

                    }
                    catch (Exception)
                    {
                        if (_claimRepository.Entry(claim).State != EntityState.Detached)
                        {
                            _claimRepository.Entry(claim).State = EntityState.Unchanged;
                        }
                        continue;
                    }
                }
            }

            sendMessage(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);

            return claimIdentifiers;
        }

        public async Task<List<string>> SecondaryBillingRebillClaimsAsync(SecondaryBillingClaimsRebillModel claimsToRebill)
        {
            var claimIdentifiers = new List<string>();
            claimTransactionData = new List<ClaimTransactionModel>();
            ClaimSubmitModel submitModel = null;

            var claim = await _claimRepository.Query()
                       .Include(x => x.ClaimSubmissions)
                       .FirstOrDefaultAsync(x => x.Id == claimsToRebill.ClaimId);

            if (claim == null) return claimIdentifiers;

            var clearingHouseId = await _rethinkServices.GetClearingHouseId(claim.AccountInfoId);
            if (clearingHouseId == 0) return claimIdentifiers;

            var pendingSubmission = claim.ClaimSubmissions
                .OrderByDescending(x => x.DateCreated)
                .FirstOrDefault(x => x.SubmissionStatus == ClaimSubmissionStatus.FunderPending ||
                                     x.SubmissionStatus == ClaimSubmissionStatus.ClearingHousePending);

            submitModel = new ClaimSubmitModel
            {
                Id = claim.Id,
                AccountInfoId = claim.AccountInfoId,
                ClaimStatus = claim.ClaimStatus,
                MemberId = claim.MemberId,
                ClearinghouseId = clearingHouseId,
                FrequencyTypeId = claim.FrequencyTypeId,
                IsSecondary = claimsToRebill.IsSecondary,
                AdjustmentLevel = claimsToRebill.AdjustmentLevel,
                PendingClaimSubmissionId = pendingSubmission != null ? Convert.ToInt32(pendingSubmission.Id) : 0
            };
            try
            {
                var oldFrequencyTypeId = claim.FrequencyTypeId;
                claim.ClaimStatus = ClaimStatus.BillNextFunder;
                claim.FrequencyTypeId = ClaimFrequencyType.Original;

                _claimRepository.Entry(claim).Context.ChangeTracker.Clear();
                _claimRepository.Entry(claim).State = EntityState.Modified;
                _claimRepository.Update(claim);
                await _claimRepository.CommitAsync();

                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = claim.Id,
                    MemberId = claimsToRebill.MemberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.BillNextFunder,
                    ClaimHistoryAction = ClaimHistoryAction.RebilledInternalReason,
                    NewValue = "Corrected Claim"
                });

                var versionId = await _claimVersionService.CreateAsync(
                    await GetClaimDetailsAsync(new IdWithUserInfo
                    {
                        Id = claim.Id,
                        AccountInfoId = claim.AccountInfoId,
                        MemberId = claim.MemberId
                    }, null),
                    claimsToRebill.AccountInfoId,
                    claimsToRebill.MemberId);

                var claimHistoryVersionSaveModel = new ClaimHistoryVersionSaveModel
                {
                    ClaimId = claim.Id,
                    MemberId = claimsToRebill.MemberId,
                    Mode = ClaimActionMode.User,
                    ClaimVersionId = versionId,
                    ClaimAction = ClaimAction.BillNextFunder,
                    ClaimHistoryAction = ClaimHistoryAction.BillNextFunder,
                    OldValue = $"{claim.ClaimStatus}"
                };

                var funderDetails = claimsToRebill.SecondaryFunderDetails
                    .FirstOrDefault(x => x.ClaimId == claimsToRebill.ClaimId);

                if (funderDetails != null)
                {
                    await _claimManagerService.SubmitClaimTransfer(
                        claim.Id,
                        claimsToRebill.MemberId,
                        claim.FrequencyTypeId ?? ClaimFrequencyType.Original,
                        ClaimDocumentType.Doc837P,
                        funderDetails.SecondaryFunderId,
                        funderDetails.ControlNumber,
                        true);
                }
                await _claimHistoryService.AddAsync(claimHistoryVersionSaveModel, true);
                await _claimHistoryService.AddAsync(new ClaimHistoryFieldSaveModel
                {
                    ClaimId = claim.Id,
                    MemberId = claimsToRebill.MemberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.Rebill,
                    ClaimHistoryAction = ClaimHistoryAction.FrequencyCodeUpdated,
                    ClaimHistoryField = ClaimHistoryField.FrequencyCode,
                    OldValue = $"{oldFrequencyTypeId}",
                    NewValue = "Corrected Claim"
                });

                claimIdentifiers.Add(claim.ClaimIdentifier);
                claimTransactionData.Add(PrepareClaimTransaction(claim.Id, ClaimTransactionType.submitClaim));
                await _clearingHouseService.SubmitClaimAsync(submitModel);
            }
            catch (Exception)
            {
                if (_claimRepository.Entry(claim).State != EntityState.Detached)
                {
                    _claimRepository.Entry(claim).State = EntityState.Unchanged;
                }
            }

            sendMessage(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);
            return claimIdentifiers;

        }

        public async Task<ClaimNextFundersAndControlNumberModel> GetClaimBillNextFundersAndControlNumberAsync(int accountInfoId, int memberId, int claimId)
        {
            var claim = _claimRepository.Query()
                .Include(x => x.ClaimSubmissions)
                .Include(x => x.PaymentClaims)
                .ThenInclude(x => x.Payment)
                .FirstOrDefault(x => x.Id == claimId && x.DateDeleted == null);

            var controlNumber = claim.PaymentClaims.Where(x => x.Payment.PaymentTypeId == (int)PaymentTypes.ERAReceived).Any() ?
                claim.PaymentClaims.Where(x => x.Payment.PaymentTypeId == (int)PaymentTypes.ERAReceived).OrderByDescending(x => x.DateCreated).FirstOrDefault().ControlNumber : "";

            //As per current implementation we only support secondary funder so commented below code
            //var latestSubmission = claim.ClaimSubmissions.OrderByDescending(x => x.DateCreated).FirstOrDefault();
            //var currentFunderSequence = ResponsibilitySequenceTypeHelper.FromString(latestSubmission.ResponsibilitySequence);
            //var nextFunderSequence = ResponsibilitySequenceTypeHelper.FromOrdinal(currentFunderSequence.AsOrdinal() + 1);
            var secondaryFunderDetails = await _claimUpdateService.CheckAndGetSecondaryFunderDetails(accountInfoId, claim);
            if (secondaryFunderDetails == null || !secondaryFunderDetails.funders.Any())
            {
                claim.IsSecondaryPayerAvailable = false;
                _claimRepository.Update(claim);
                await _claimRepository.CommitAsync();
                throw new Exception("Secondary Funder not exists");
            }
            secondaryFunderDetails.controlNumber = controlNumber;
            return secondaryFunderDetails;
        }


        public async Task<List<ServiceLineAppointmentModel>> GetClaimLineAppointmentsAsync(int accountInfoId, int serviceLineId)
        {
            var claimLine = await _claimChargeEntryRepository.Query().Include(x => x.Claim).FirstOrDefaultAsync(x =>
            x.DateDeleted == null && x.Id == serviceLineId);

            var provider = await _rethinkServices.GetProviderBillingCode(claimLine.Claim.AccountInfoId, claimLine.BillingCode);
            var providerBill = provider.FirstOrDefault(x => x.billingCode == claimLine.BillingCode);
            var providerBillingCodes = new BillingCodeData()
            {
                billingCode = providerBill.billingCode,
                billingCode2 = providerBill.billingCode2,
                id = providerBill.id
            };
            var lineProviderBillingCode = providerBillingCodes;


            int? claimLineDiagnosis1Id = null;
            int? claimLineDiagnosis2Id = null;

            if (!String.IsNullOrEmpty(claimLine.DiagnosisCode))
            {
                var diagnosis = await _rethinkServices.GetDiagnosisByCodeAsync(accountInfoId, claimLine.DiagnosisCode);

                claimLineDiagnosis1Id = diagnosis.id;
            }
            if (!String.IsNullOrEmpty(claimLine.DiagnosisCode2))
            {
                var diagnosis = await _rethinkServices.GetDiagnosisByCodeAsync(accountInfoId, claimLine.DiagnosisCode2);

                claimLineDiagnosis2Id = diagnosis.id;
            }

            var claimAppointmentsIds = await _claimAppointmentLinkRepository.Query()
                .Where(x => x.DateDeleted == null && x.ClaimId == claimLine.ClaimId)
                .Select(x => x.AppointmentId)
                .ToListAsync();

            var claimAppointments = await _rethinkServices.GetAppointmentListAsync(claimAppointmentsIds);

            var selectedAppointments = new List<ServiceLineAppointmentModel>();

            foreach (var appointment in claimAppointments)
            {
                appointment.ChildProfile = await _rethinkServices.GetChildProfile(accountInfoId, appointment.clientId ?? 0);
                var fakeData = true;
                if (appointment.providerBillingCodeId == lineProviderBillingCode.id || fakeData)
                {

                    if (fakeData || claimLineDiagnosis1Id == appointment.diagnosisId ||
                        claimLineDiagnosis2Id == appointment.diagnosisId)
                    {
                        var patientName = FullNameExt.GetFullName(appointment.ChildProfile.name.firstName,
                                                      appointment.ChildProfile.name.middleName,
                                                      appointment.ChildProfile.name.lastName);

                        var appointmentModel = new ServiceLineAppointmentModel
                        {
                            Id = appointment.id,
                            AppointmentDescription = appointment.appointmentDescription,
                            StartDate = appointment.startDate,
                            StartTime = DateTime.Today.Add((new TimeSpan((appointment.actualStartTime ?? appointment.startTime) / 60,
                                (appointment.actualStartTime ?? appointment.startTime) % 60, 0))),
                            EndDate = appointment.endDate,
                            EndTime = DateTime.Today.Add((new TimeSpan((appointment.actualEndTime ?? appointment.endTime) / 60,
                                (appointment.actualEndTime ?? appointment.endTime) % 60, 0))),
                            ClientName = patientName,
                            Location = appointment.toLocationId == 1 ?
                                "Staff Home" :
                                    appointment.toLocationId == 2 ? "Client Home" :
                                        String.IsNullOrEmpty(appointment.toLocation) ? "Blank" : appointment.toLocation
                        };

                        selectedAppointments.Add(appointmentModel);
                    }
                }
            }

            return selectedAppointments;
        }

        //public async Task<List<StateEntity>> GetStates()
        //{
        //    return await _bhStateRepository.Query().ToListAsync();
        //}
        //public async Task<List<CountryEntity>> GetCountries()
        //{
        //    return await _bhCountryRepository.Query().ToListAsync();
        //}

        public async Task<bool> IsDiagnosisServiceLineHasActiveClaims(int clienId, int diagnosisCodeId)
        {
            var childProfileClaims = await (await _claimRepository.GetAllAsync(c => c.ChildProfileId == clienId && c.DateDeleted == null))
                .Include(c => c.ClaimDiagnosisCodes).ToListAsync();

            foreach (var claim in childProfileClaims)
            {
                if (claim.AuthorizationId.HasValue)
                {
                    var authorization = await _rethinkServices.GetChildProfileAuthorizationById(claim.AccountInfoId, claim.AuthorizationId.Value);

                    if (authorization != null)
                    {
                        claim.ChildProfileAuthorization = authorization;
                    }
                }
            }

            var authDiagnosisCodes = childProfileClaims.SelectMany(c => c.ChildProfileAuthorization?.ChildProfileAuthorizationDiagnosisCodes ?? new List<ChildProfileAuthorizationDiagnosisCode>());
            var claimDiagnosisCodes = childProfileClaims.SelectMany(c => c.ClaimDiagnosisCodes ?? Enumerable.Empty<ClaimDiagnosisCodeEntity>());

            var hasClaim = authDiagnosisCodes.Any(c => c.diagnosisId == diagnosisCodeId) || claimDiagnosisCodes.Any(c => c.DiagnosisId == diagnosisCodeId);

            return hasClaim;
        }

        public async Task<List<ClientDiagnosisServiceLine>> GetDiagnosisServiceLineUsedByClaims(int clienId, int diagnosisCodeId)
        {
            //var usedServiceLineDiagnosis = _claimRepository.Query()
            //    .Where(c =>
            //            c.ChildProfileId == clienId
            //         && c.DateDeleted == null)
            //    .SelectMany(c => c.ChildProfileAuthorization.ChildProfileAuthorizationDiagnosisCodes)
            //    .Select(auc => new ClientDiagnosisServiceLine
            //    {
            //        ServiceLineId = auc.ChildProfileAuthorization.ProviderServiceId,
            //        DiagnosisCodeId = diagnosisCodeId
            //    });

            //var result = await usedServiceLineDiagnosis.ToListAsync();

            return new List<ClientDiagnosisServiceLine>();
        }

        public async Task<int> ClaimProviderLocationUsageCountAsync(int providerLocationId)
        {
            var usedProviderLocations = await _claimRepository.Query()
                .Where(c => c.DateDeleted == null
                    && (c.ProviderLocationId == providerLocationId || c.ToLocationId == providerLocationId || c.ServiceLocationId == providerLocationId)
                )
                .CountAsync();

            return usedProviderLocations;
        }

        public async Task<int> ClaimReferringProviderUsageCountAsync(int providerId)
        {
            var usedReferringProviders = await _claimRepository.Query()
                .Where(c => c.DateDeleted == null
                    && c.ChildProfileReferringProviderId == providerId
                )
                .CountAsync();

            return usedReferringProviders;
        }

        public async Task<int> ClaimStaffAsRendingProviderUsageCountAsync(int staffMemberId)
        {
            var usedRenderingStaffMembers = await _claimRepository.Query()
                .Where(c => c.DateDeleted == null
                    && c.RenderingStaffMemberId == staffMemberId
                )
                .CountAsync();

            return usedRenderingStaffMembers;
        }

        public async Task<List<int>> GetBilledPreviouslyClaimsIdsAsync(int accountInfoId, int memberId, int[] claimsIds)
        {
            var billedPreviouslyClaims = new List<int>();

            foreach (var claimId in claimsIds)
            {
                var claim = await _claimRepository.Query()
                    .Include(x => x.ClaimHistory)
                    .FirstOrDefaultAsync(x => x.AccountInfoId == accountInfoId && x.DateDeleted == null && x.Id == claimId);

                if (claim != null)
                {
                    var histories = claim.ClaimHistory.OrderByDescending(ch => ch.DateCreated).ToList();

                    if (histories.Any(hi => billedHistoryActions.Contains(hi.ClaimHistoryAction)))
                    {
                        billedPreviouslyClaims.Add(claim.Id);
                    }
                }
            }

            return billedPreviouslyClaims;
        }

        public async Task<bool> HasFunderBilledClaimsAsync(ClientFunderModel model)
        {
            var hasBilledClaims = await _claimRepository.Query()
                .Where(c => c.DateDeleted.HasValue
                        && c.ChildProfileId == model.ClientId
                        && c.ClaimSubmissions.Any(cs => cs.FunderId == model.FunderId))
                .AnyAsync(x => x.ClaimHistory.Any());

            return hasBilledClaims;
        }

        public async Task SetEditAuthWarningAsync(AuthorizationModifiedModel model)
        {
            var claimSubmissions = await _claimSubmissionRepository.Query()
                .Where(c => c.Claim.ClaimStatus != ClaimStatus.PendingReview
                    && c.Claim.ClaimStatus != ClaimStatus.ReadyToBill
                    && c.ChildProfileAuthorizationId == model.AuthorizationId)
                .GroupBy(c => c.ClaimId)
                .Select(c => c.ToList())
                .ToListAsync();
            var message = await _claimErrorMessageRepository.Query().FirstOrDefaultAsync(x => x.ErrorNumber == ClaimErrorNumber.AuthorizationModified);
            foreach (var claimSubmissionGroup in claimSubmissions)
            {
                var lastSubmission = claimSubmissionGroup.OrderByDescending(x => x.DateLastModified).FirstOrDefault();
                var newError = new ClaimValidationErrorEntity()
                {
                    ClaimId = lastSubmission.ClaimId,
                    ClaimErrorMessageId = message.Id,
                    ClaimErrorSource = ClaimErrorSource.Bh,
                    ContextMessage = message.LongDescription,
                    ValidationDate = EstDateTime
                };
                MarkCreated(newError, model.MemberId);
                await _claimValidationErrorRepository.AddAsync(newError);

                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = lastSubmission.ClaimId,
                    MemberId = model.MemberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.Edit,
                    ClaimHistoryAction = ClaimHistoryAction.AuthorizationModified,
                    ActionDate = EstDateTime,
                    OldValue = model.OldValue,
                    NewValue = model.NewValue,
                }, false);
            }

            await _claimValidationErrorRepository.CommitAsync();
        }

        public async Task<List<AuthorizationBuitData>> CheckIsAuthUsedByClaimAsync(int authorizationId)
        {
            var builtSLrecords = await _claimSubmissionRepository.Query()
                .Where(c => c.Claim.ClaimStatus != ClaimStatus.PendingReview
                    && c.Claim.ClaimStatus != ClaimStatus.ReadyToBill
                    && c.ChildProfileAuthorizationId == authorizationId)
                .SelectMany(x => x.ClaimSubmissionServiceLines)
                .ToListAsync();
            var authCharges = builtSLrecords
                .GroupBy(x => x.ClaimChargeEntryId)
                .Select(gr => gr.ToList().OrderByDescending(x => x.DateCreated).Select(sl => new AuthorizationBuitData
                {
                    BillingCode = sl.BillingCode,
                    BillingCodeDescription = sl.BillingCodeDescription,
                    Units = sl.Units,
                    ServiceLineIdentifier = sl.ServiceLineIdentifier,
                    ServiceLineIndex = sl.ServiceLineIndex
                }).FirstOrDefault())
                .ToList();
            return authCharges;
        }

        private Expression<Func<ClaimEntity, bool>> FilterClaimsBySelectedTab(ClaimListingTab tab)
        {
            switch (tab)
            {
                case ClaimListingTab.ReadyToBill:
                    return entity => (entity.ClaimStatus == ClaimStatus.Void ||
                            entity.ClaimStatus == ClaimStatus.ReadyToBill ||
                            entity.ClaimStatus == ClaimStatus.Rebill ||
                            entity.ClaimStatus == ClaimStatus.SubmissionFailed) && !entity.IsFlagged;
                case ClaimListingTab.BillingPending:
                    return entity => (entity.ClaimStatus == ClaimStatus.Billed ||
                            entity.ClaimStatus == ClaimStatus.Pending ||
                            entity.ClaimStatus == ClaimStatus.Paid ||
                            entity.ClaimStatus == ClaimStatus.AcceptedClearingHouse ||
                            entity.ClaimStatus == ClaimStatus.AcceptedFunder ||
                            entity.ClaimStatus == ClaimStatus.ReceivedFunder ||
                            entity.ClaimStatus == ClaimStatus.BillNextFunder) && !entity.IsFlagged;
                case ClaimListingTab.Completed:
                    return entity => (entity.ClaimStatus == ClaimStatus.Closed ||
                            entity.ClaimStatus == ClaimStatus.VoidClosed) && !entity.IsFlagged;
                case ClaimListingTab.Rejected:
                    return entity => (entity.ClaimStatus == ClaimStatus.RejectedClearinghouse ||
                            entity.ClaimStatus == ClaimStatus.RejectedFunder) && !entity.IsFlagged;
                case ClaimListingTab.Denied:
                    return entity => entity.ClaimStatus == ClaimStatus.Denied && !entity.IsFlagged;
                case ClaimListingTab.Flagged:
                    return entity => entity.IsFlagged;
                case ClaimListingTab.PendingReview:
                default:
                    return entity => (entity.ClaimStatus == ClaimStatus.PendingReview
                           || entity.ClaimStatus == ClaimStatus.ApprovalFailed) && !entity.IsFlagged;
            }
        }

        public async Task<bool> PopagateProvidersClaimDataAsync(PropagatingProvidersClaimDataModel model, int accountInfoId)
        {
            var effectedClaims = await _claimRepository.Query()
                .Include(x => x.ClaimSubmissions)
                .Include("ClaimValidationErrors.ClaimErrorMessage")
                .Where(x => x.AuthorizationId == model.AuthorizationId)
                .ToListAsync();
            var auth = await _rethinkServices.GetChildProfileAuthorizationById(accountInfoId, model.AuthorizationId);
            //var auth = await _childProfileAuthorizationRepository.Query().FirstOrDefaultAsync(x => x.Id == model.AuthorizationId);

            var billingProviderChangedMessageId = (await _claimErrorMessageRepository.Query().FirstOrDefaultAsync(x => x.ErrorNumber == ClaimErrorNumber.BillingProviderChanged)).Id;
            var renderingProviderChangedMessageId = (await _claimErrorMessageRepository.Query().FirstOrDefaultAsync(x => x.ErrorNumber == ClaimErrorNumber.RenderingProviderChanged)).Id;
            var serviceFacilityChangedMessageId = (await _claimErrorMessageRepository.Query().FirstOrDefaultAsync(x => x.ErrorNumber == ClaimErrorNumber.FacilityChanged)).Id;

            foreach (var claim in effectedClaims)
            {
                if (claim.ClaimStatus != ClaimStatus.Billed)
                {
                    var oldValue = JsonConvert.SerializeObject(new
                    {
                        claim.RenderingStaffMemberId,
                        claim.ChildProfileReferringProviderId,
                        claim.ProviderLocationId,
                        claim.ServiceLocationId
                    });
                    var newValue = JsonConvert.SerializeObject(new
                    {
                        RenderingStaffMemberId = model.Billing.RenderingProviderId,
                        ReferringProviderId = model.Billing.ReferringProviderId,
                        ProviderLocationId = model.Billing.BillingProviderId,
                        ServiceLocationId = model.Billing.ServiceProviderId,
                    });

                    claim.RenderingStaffMemberId = model.Billing.RenderingProviderId;
                    claim.ChildProfileReferringProviderId = model.Billing.ReferringProviderId;
                    claim.ProviderLocationId = model.Billing.BillingProviderId;
                    claim.ServiceLocationId = model.Billing.ServiceProviderId;

                    MarkUpdated(claim, model.MemberId);
                    _claimRepository.Entry(claim).Context.ChangeTracker.Clear();
                    _claimRepository.Entry(claim).State = EntityState.Modified;
                    _claimRepository.Update(claim);
                    await _claimRepository.CommitAsync();

                    await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                    {
                        ClaimId = claim.Id,
                        MemberId = model.MemberId,
                        Mode = ClaimActionMode.User,
                        ClaimAction = ClaimAction.Edit,
                        ClaimHistoryAction = ClaimHistoryAction.AuthorizationModified,
                        ActionDate = EstDateTime,
                        OldValue = oldValue,
                        NewValue = newValue,
                    }, false);
                }
                else
                {
                    var claimSubmission = claim.ClaimSubmissions.OrderByDescending(x => x.DateCreated).FirstOrDefault();
                    if (model.Billing.RenderingPropagatingData != null)
                    {
                        if (model.Billing.RenderingPropagatingData.TypeId == PropagatingAppointmentData.Type.ApplyToNewAndExistingAppointments
                            || (model.Billing.RenderingPropagatingData.TypeId == PropagatingAppointmentData.Type.ApplyToNewAndExistingAppointmentsAfterDate
                                && claimSubmission?.SubmitDate > model.Billing.ReferringPropagatingData.StartDate))
                        {
                            if (claim.RenderingStaffMemberId != model.Billing.RenderingProviderId)
                            {
                                var date = auth.renderingProviderDateUpdated ?? DateTime.UtcNow;
                                var error = claim.ClaimValidationErrors.FirstOrDefault(x => x.ClaimErrorMessage.ErrorNumber == ClaimErrorNumber.RenderingProviderChanged);
                                if (error != null)
                                {
                                    error.ContextMessage = $"Rendering provider updated on auth as of ${date.ToShortDateString()}";
                                    MarkUpdated(error, model.MemberId);
                                    _claimValidationErrorRepository.Update(error);
                                    await _claimValidationErrorRepository.CommitAsync();
                                }
                                else
                                {
                                    error = new ClaimValidationErrorEntity
                                    {
                                        ClaimId = claim.Id,
                                        ClaimErrorMessageId = renderingProviderChangedMessageId,
                                        ClaimErrorSource = ClaimErrorSource.Bh,
                                        ContextMessage = $"Rendering provider updated on auth as of ${date.ToShortDateString()}",
                                        ValidationDate = EstDateTime
                                    };

                                    MarkCreated(error, model.MemberId);
                                    await _claimValidationErrorRepository.AddAsync(error);
                                    await _claimValidationErrorRepository.CommitAsync();
                                }
                            }
                        }
                    }

                    if (model.Billing.BillingPropagatingData != null)
                    {
                        if (model.Billing.BillingPropagatingData.TypeId == PropagatingAppointmentData.Type.ApplyToNewAndExistingAppointments
                           || (model.Billing.BillingPropagatingData.TypeId == PropagatingAppointmentData.Type.ApplyToNewAndExistingAppointmentsAfterDate
                               && claimSubmission?.SubmitDate > model.Billing.BillingPropagatingData.StartDate))
                        {
                            if (claim.ProviderLocationId != model.Billing.BillingProviderId)
                            {
                                var date = auth.billingProviderDateUpdated ?? DateTime.UtcNow;
                                var error = claim.ClaimValidationErrors.FirstOrDefault(x => x.ClaimErrorMessage.ErrorNumber == ClaimErrorNumber.BillingProviderChanged);
                                if (error != null)
                                {
                                    error.ContextMessage = $"Billing provider updated on auth as of ${date.ToShortDateString()}";
                                    MarkUpdated(error, model.MemberId);
                                    _claimValidationErrorRepository.Update(error);
                                    await _claimValidationErrorRepository.CommitAsync();
                                }
                                else
                                {
                                    error = new ClaimValidationErrorEntity
                                    {
                                        ClaimId = claim.Id,
                                        ClaimErrorMessageId = billingProviderChangedMessageId,
                                        ClaimErrorSource = ClaimErrorSource.Bh,
                                        ContextMessage = $"Billing provider updated on auth as of ${date.ToShortDateString()}",
                                        ValidationDate = EstDateTime
                                    };

                                    MarkCreated(error, model.MemberId);
                                    await _claimValidationErrorRepository.AddAsync(error);
                                    await _claimValidationErrorRepository.CommitAsync();
                                }
                            }
                        }
                    }

                    if (model.Billing.ServicePropagatingData != null)
                    {
                        if (model.Billing.ServicePropagatingData.TypeId == PropagatingAppointmentData.Type.ApplyToNewAndExistingAppointments
                           || (model.Billing.ServicePropagatingData.TypeId == PropagatingAppointmentData.Type.ApplyToNewAndExistingAppointmentsAfterDate
                              && claimSubmission?.SubmitDate > model.Billing.ServicePropagatingData.StartDate))
                        {
                            if (claim.ServiceLocationId != model.Billing.ServiceProviderId)
                            {
                                var date = auth.serviceFacilityLocationDateUpdated ?? DateTime.UtcNow;
                                var error = claim.ClaimValidationErrors.FirstOrDefault(x => x.ClaimErrorMessage.ErrorNumber == ClaimErrorNumber.FacilityChanged);
                                if (error != null)
                                {
                                    error.ContextMessage = $"Service facility updated on auth as of {date.ToShortDateString()}";
                                    MarkUpdated(error, model.MemberId);
                                    _claimValidationErrorRepository.Update(error);
                                    await _claimValidationErrorRepository.CommitAsync();
                                }
                                else
                                {
                                    error = new ClaimValidationErrorEntity
                                    {
                                        ClaimId = claim.Id,
                                        ClaimErrorMessageId = serviceFacilityChangedMessageId,
                                        ClaimErrorSource = ClaimErrorSource.Bh,
                                        ContextMessage = $"Service facility updated on auth as of {date.ToShortDateString()}",
                                        ValidationDate = EstDateTime
                                    };

                                    MarkCreated(error, model.MemberId);
                                    await _claimValidationErrorRepository.AddAsync(error);
                                    await _claimValidationErrorRepository.CommitAsync();
                                }
                            }
                        }
                    }

                    var newValue = JsonConvert.SerializeObject(new
                    {
                        RenderingStaffMemberId = model.Billing.RenderingProviderId,
                        ReferringProviderId = model.Billing.ReferringProviderId,
                        ProviderLocationId = model.Billing.BillingProviderId,
                        ServiceLocationId = model.Billing.ServiceProviderId,
                    });

                    await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                    {
                        ClaimId = claim.Id,
                        MemberId = model.MemberId,
                        Mode = ClaimActionMode.User,
                        ClaimAction = ClaimAction.Edit,
                        ClaimHistoryAction = ClaimHistoryAction.AuthorizationModified,
                        ActionDate = EstDateTime,
                        NewValue = newValue,
                    }, false);
                }
            }

            return true;
        }

        public async Task<List<BasicOption>> GetClaimRenderingProviders(int accountInfoId)
        {
            var result = _rethinkServices.GetStaffMemberList(accountInfoId).Result
                //.Where(x => x.identifiers.All(y => x.identifiers.Any(f => f.identifierType.ToLower() == "npinumber")))
                .Select(x => new BasicOption
                { Id = x.memberId, Name = x.name.firstName + (!String.IsNullOrEmpty(x.name.middleName) ? " " + x.name.middleName + " " : " ") + x.name.lastName })
                .OrderBy(x => x.Name)
                .ToList();

            return result;
        }

        public async Task<List<BasicOption>> GetClaimReferringProviders(int claimId, int accountInfoId)
        {
            var childProfileId = await _claimRepository.Query().Where(x => x.Id == claimId).Select(x => x.ChildProfileId).FirstOrDefaultAsync();

            List<ReferringProviderDropdownModel> response = await _rethinkServices.GetReferringProvidersByClientId(childProfileId, accountInfoId);

            var result = response.Select(x => new BasicOption
            {
                Id = x.Id,
                Name = x.FirstName + " " + x.LastName
            })
                           .OrderBy(x => x.Name)
                           .ToList();

            return result;
        }

        public async Task<List<ProviderLocations>> GetClaimProviderLocations(int accountInfoId)
        {
            var result = await _rethinkServices.GetProviderLocationList(accountInfoId);
            return result.data;
        }

        public async Task<List<ClientFunderWithClaimModel>> IsFunderHasActiveClaimsAsync(IsClientFundersInUseModel model)
        {
            var clientFundersWithClaimsIds = await _claimRepository.Query()
                .Where(c => c.ChildProfileId == model.ClientId
                        && c.ClientFunderId != null
                        && model.ClientFunderIds.Contains(c.ClientFunderId.Value)
                        && c.DateDeleted == null)
                .Select(c => c.ClientFunderId).ToListAsync();

            var clientFundersWithClaims = new List<ClientFunderWithClaimModel>();

            foreach (var clientFunderId in model.ClientFunderIds)
            {
                var clientFunderWithClaim = new ClientFunderWithClaimModel
                {
                    ClientFunderId = clientFunderId,
                    HasClaim = clientFundersWithClaimsIds.Contains(clientFunderId)
                };

                clientFundersWithClaims.Add(clientFunderWithClaim);
            }

            return clientFundersWithClaims;
        }

        public async Task ValidateClaimDataAsync(ClaimValidationModel model)
        {
            var claim = await _claimRepository.GetByIdAsync(model.Id);
            if (claim != null)
            {
                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = model.Id,
                    MemberId = model.MemberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.ScrubbingRules,
                    ClaimHistoryAction = ClaimHistoryAction.ScrubbingRulesInitiatedByUser,
                });

                var respsonibilitySequence = model.IsSecondary ? ResponsibilitySequenceType.Secondary : await GetClaimResponsibilitySequenceAsync(claim.AccountInfoId, claim.ChildProfileId, claim.ClientFunderId, claim.ClientFunderServiceLineId);
                if (model.SecondaryFunderId.HasValue)
                    await _claimValidationService.ValidateClaimData(model.Id, model.MemberId, null, respsonibilitySequence, false, model.SecondaryFunderId);
                else
                    await _claimValidationService.ValidateClaimData(model.Id, model.MemberId, null, respsonibilitySequence);
            }
        }

        public async Task ValidateClaimsOnFunderChangedAsync(int funderId, int clientFunderId, DateTime funderModifiedDate, int memberId)
        {
            var claims = await _claimRepository.Query()
                .Where(x => !x.DateDeleted.HasValue && x.MemberId == memberId && x.DateLastModified < funderModifiedDate && x.ClaimStatus == ClaimStatus.Billed
                            //Funder check
                            && x.ClaimSubmissions.OrderByDescending(cs => cs.Id).FirstOrDefault().FunderId == funderId)
                .ToListAsync();

            foreach (var claim in claims)
            {
                //if we have client funder id - exclude claim that not connected to this client funder
                if (clientFunderId > 0 && !(claim.ClientFunderId == clientFunderId))
                {
                    continue;
                }

                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = claim.Id,
                    MemberId = memberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.ScrubbingRules,
                    ClaimHistoryAction = ClaimHistoryAction.ScrubbingRulesInitiatedByUser,
                });

                await _claimValidationService.ValidateClaimData(claim.Id, memberId, null);
            }
        }

        private async Task<ResponsibilitySequenceType> GetClaimResponsibilitySequenceAsync(int accountInfoId, int childProfileId, int? funderMappingId, int? clientFunderServiceLineId)
        {
            if (clientFunderServiceLineId.HasValue)
            {
                var clientFunderServiceLine = await _rethinkServices.GetChildProfileFunderServiceLineMappingEntity(accountInfoId, childProfileId, funderMappingId ?? 0, clientFunderServiceLineId ?? 0);

                return clientFunderServiceLine.responsibilitySequence;
            }

            return ResponsibilitySequenceType.Primary; // for old claims
        }
        public async void sendMessage(string entityName, List<ClaimTransactionModel> claimTransaction)
        {

            if (claimTransaction.Count != 0)
            {
                //For Updating the statuses in AR report
                await _bus.SendBatchAsync(entityName, claimTransaction);
            }
        }

        public async Task<List<AuthRenderingProviderType>> GetRenderingProviders(int accountInfoId)
        {
            return await _rethinkServices.GetRenderingProvidersAsync(accountInfoId, false);
        }

        public async Task<List<CarcCodeResponseModel>> GetAllCarcCodes()
        {
            var carcCodes = await _cacheService.GetOrSetCacheAsync(
                deniedReasonCodeKey,
                async () => await _carcCodeRepository.Query().Where(x => x.DateDeleted == null).ToListAsync(),
                TimeSpan.FromMinutes(cacheExpiration)
            );

            return _mapper.Map<List<CarcCodeResponseModel>>(carcCodes);
        }

        public async Task<bool> UpdateClaimsStatusAsync(UpdateClaimRequestModel model)
        {
            try
            {
                await UpdateClaimStatusAsync(model.ClaimId, (ClaimStatus)model.ClaimStatusId, model.AccountInfoId);

                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = model.ClaimId,
                    MemberId = model.MemberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = (ClaimAction)(model.ClaimStatusId == 5 ? 7 : 6),
                    ClaimHistoryAction = (ClaimHistoryAction)(model.ClaimStatusId == 5 ? 24 : 21),
                    NewValue = $"{(ClaimStatus)model.ClaimStatusId}",
                });

                await _claimRepository.CommitAsync();

                // Notify AR Report about the status change
                List<ClaimTransactionModel> claimTransactionData = [];
                claimTransactionData.Add(PrepareClaimTransaction(model.ClaimId, ClaimTransactionType.submitClaim));
                if (claimTransactionData.Count != 0)
                {
                    //For Updating the statuses in AR report
                    await _bus.SendBatchAsync(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private async Task UpdateClaimStatusAsync(int claimId, ClaimStatus status, int memberId, bool commitImmediately = true)
        {
            var claimEntity = await _claimRepository.GetByIdAsync(claimId) ?? throw new ArgumentNullException($"Claim with id: {claimId} not found!");
            claimEntity.ClaimStatus = status;

            MarkUpdated(claimEntity, memberId);
            _claimRepository.Update(claimEntity);

            if (commitImmediately) await _claimRepository.CommitAsync();
        }

        public async Task<List<BaseNameOption>> GetStaffLocations(ClaimFilterGetModel model)
        {
            if (model == null || model.AccountInfoId == 0)
                return new List<BaseNameOption>();

            var locationCodes = await _rethinkServices.GetProviderLocationList(model.AccountInfoId);

            if (locationCodes == null || locationCodes.data == null)
                return new List<BaseNameOption>();

            var result = locationCodes.data
                .Where(x => x != null && x.id != 0 && !string.IsNullOrWhiteSpace(x.name))
                .Select(x => new BaseNameOption
                {
                    Id = x.id,
                    Name = x.name
                })
                .ToList();

            if (result.Count == 0)
                return new List<BaseNameOption>();

            if (!string.IsNullOrWhiteSpace(model.SearchValue))
                return result
                    .Where(x => !string.IsNullOrWhiteSpace(x.Name) && x.Name.Contains(model.SearchValue, StringComparison.OrdinalIgnoreCase))
                    .ToList();

            return result;
        }

        /// <summary>
        /// Submits claims to the Service Bus for processing.
        /// </summary>
        /// <remarks>This method processes the claims specified in the <paramref name="requestModel"/> by
        /// creating a batch of claim processing requests and sending them to the Service Bus. Each claim is processed
        /// individually within the batch, and the batch is identified by a unique batch ID.  The method ensures that
        /// secondary funder details are filtered for each claim, if applicable. The claims are sent to the Service Bus
        /// in chunks, as determined by the implementation of the <c>SendBatchAsync</c> method.</remarks>
        /// <param name="requestModel">The model containing the claims submission details, including claim IDs, adjustment level, secondary funder
        /// details, account information, and member information.</param>
        /// <returns></returns>
        public async Task SubmitClaimsToServiceBusAsync(ClaimsSubmitModel requestModel)
        {
            var isSecondary = requestModel.IsSecondary;
            var adjustmentLevel = requestModel?.AdjustmentLevel;
            var secondaryFunderDetails = requestModel?.SecondaryFunderDetails;
            var accountInfoId = requestModel?.AccountInfoId;
            var memberId = requestModel?.MemberId;
            var totalClaims = requestModel?.Ids.Length;
            var batchId = Guid.NewGuid().ToString();
            var impersonationUserName = requestModel.ImpersonationUserName;

            // Creating a list with single item to use SendBatchAsync method
            var requestModels = new List<ClaimProcessRequestModel>();
            foreach (var x in requestModel.Ids)
            {
                requestModels.Add(new ClaimProcessRequestModel
                {
                    BatchId = batchId,
                    RequestModel = new ClaimsSubmitModel
                    {
                        Ids = [x],
                        IsSecondary = isSecondary,
                        AdjustmentLevel = adjustmentLevel,
                        SecondaryFunderDetails = secondaryFunderDetails.Count > 0 ? [.. secondaryFunderDetails.Where(s => s.ClaimId == x)] : secondaryFunderDetails,
                        AccountInfoId = accountInfoId ?? 0,
                        MemberId = memberId ?? 0,
                        ImpersonationUserName = impersonationUserName
                    },
                    TotalClaims = totalClaims ?? 0
                });
            }

            // Sending message to service bus
            if (requestModels.Count > 0)
            {
                await _bus.SendBatchAsync(Topics.RT_Billing_ProcessClaimSubmission, requestModels, chunkSize);
            }
        }


        /// <summary>
        /// Prepares and logs an error related to a claim submission.
        /// </summary>
        /// <remarks>This method retrieves the most recent claim submission for the specified claim, if
        /// available,  and logs the error details for further processing or auditing.</remarks>
        /// <param name="error">A description of the error that occurred.</param>
        /// <param name="errorId">The unique identifier for the error type.</param>
        /// <param name="claimId">The unique identifier of the claim associated with the error.</param>
        /// <param name="memberId">The unique identifier of the member associated with the claim.</param>
        /// <returns></returns>
        private async Task PrepareClaimError(string error, ClaimErrorNumber errorId, int claimId, int memberId)
        {
            // Retrieve the most recent claim submission for the specified claim
            var claimSubmission = await _claimSubmissionRepository.Query()
                    .Where(cs => cs.ClaimId == claimId && cs.DateDeleted == null)
                    .OrderByDescending(cs => cs.Id).FirstOrDefaultAsync();

            if (claimSubmission?.Id == null)
            {
                // We just need the ClaimSubmission Id for logging the error,
                // so if there is no submission for this claim, get the latest one
                // While getting the error we'are not checking any claim submission relation for this.
                claimSubmission = await _claimSubmissionRepository.Query()
                    .Where(cs => cs.DateDeleted == null)
                    .OrderByDescending(cs => cs.Id).FirstOrDefaultAsync();
            }

            // Get the error message ID corresponding to the provided error number
            var errorMessageId = _claimErrorMessageRepository.Query().FirstOrDefault(cem => cem.ErrorNumber == errorId).Id;

            // Save the claim error details to the database
            await SaveClaimErrors(error, errorMessageId, memberId, claimId, claimSubmission?.Id ?? 0);
        }

        /// <summary>
        /// Saves a claim validation error to the database.
        /// </summary>
        /// <remarks>This method creates a new claim validation error record and persists it to the
        /// database. The error is associated with the specified claim and claim submission, and it is marked as
        /// originating from the billing process.</remarks>
        /// <param name="errorMessage">The error message describing the validation issue.</param>
        /// <param name="errorMessageId">The unique identifier of the error message.</param>
        /// <param name="memberId">The identifier of the member associated with the claim.</param>
        /// <param name="claimId">The identifier of the claim associated with the error.</param>
        /// <param name="claimSubmissionId">The identifier of the claim submission associated with the error.</param>
        /// <returns></returns>
        private async Task SaveClaimErrors(string errorMessage, int errorMessageId, int memberId, int claimId, int claimSubmissionId)
        {
            try
            {
                // Check if the error already exists for the given claim and error message
                var existingError = await _claimValidationErrorRepository.Query()
                    .FirstOrDefaultAsync(ce =>
                        ce.ClaimId == claimId &&
                        ce.ClaimErrorMessageId == errorMessageId &&
                        ce.ClaimErrorSource == ClaimErrorSource.Billing &&
                        ce.DateDeleted == null);

                if (existingError != null)
                {
                    return;
                }

                var claimError = new ClaimValidationErrorEntity()
                {
                    ClaimSubmissionId = claimSubmissionId,
                    ClaimId = claimId,
                    ClaimErrorMessageId = errorMessageId, //error.ClaimErrorMessage.Id,
                    ClaimErrorSource = ClaimErrorSource.Billing,
                    ContextMessage = errorMessage,
                    ValidationDate = EstDateTime
                };
                MarkCreated(claimError, memberId);

                await _claimValidationErrorRepository.AddAsync(claimError);
                await _claimValidationErrorRepository.CommitAsync();
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// Publish claims to the Service Bus for Approval process.
        /// </summary>
        /// creating a batch of claim requests and sending them to the Service Bus. Each claim is Approved
        /// individually within the batch, and the batch is identified by a unique batch ID. The claims are sent to the Service Bus
        /// in chunks, as determined by the implementation of the <c>SendBatchAsync</c> method.</remarks>
        /// <returns></returns>
        public async Task SubmitClaimsToServiceBusTopicAsync(IdsWithUserInfo model)
        {
            if (model.Ids == null || model.Ids.Length == 0)
            {
                return;
            }

            var totalClaims = model.Ids.Length;
            var batchId = Guid.NewGuid().ToString();
            var accountInfoId = model.AccountInfoId;
            var memberId = model.MemberId;

            // Creating a list with single item to use SendBatchAsync method
            var requestModels = new List<ClaimApproveRequestModel>();
            foreach (var x in model.Ids)
            {
                requestModels.Add(new ClaimApproveRequestModel
                {
                    BatchId = batchId,
                    RequestModel = new IdsWithUserInfo
                    {
                        Ids = [x],
                        AccountInfoId = accountInfoId,
                        MemberId = memberId
                    },
                    TotalClaims = totalClaims
                });
            }

            // Sending message to service bus
            if (requestModels.Count > 0)
            {
                await _bus.SendBatchAsync(Topics.RT_Billing_ClaimApproval, requestModels, chunkSize);
            }
        }

        /// <summary>
        /// Retrieves claim flag reasons for the specified account.
        /// </summary>
        /// <remarks>
        /// This method returns active claim flag reasons that are either system-level
        /// (AccountInfoId = 0) or specific to the provided account. Soft-deleted
        /// reasons are excluded. Results are returned in a predictable order
        /// for UI consumption.
        /// </remarks>
        /// <param name="accountInfoId">The account identifier.</param>
        /// <returns>List of claim flag reasons.</returns>
        public async Task<List<ClaimFlagReasonModel>> GetClaimFlagReasonsAsync(int accountInfoId)
        {
            try
            {
                var reasons = await _claimFlagReasonMasterRepository.Query()
                    .Where(r =>
                        r.DateDeleted == null &&
                        (r.AccountInfoId == 0 || r.AccountInfoId == accountInfoId))
                    .OrderBy(r => r.ReasonName)
                    .Select(r => new ClaimFlagReasonModel
                    {
                        Id = r.Id,
                        ReasonName = r.ReasonName,
                        ReasonDescription = r.ReasonDescription,
                        AccountInfoId = r.AccountInfoId
                    })
                    .ToListAsync();

                return reasons;
            }
            catch
            {
                // Return empty list to avoid blocking billing flow
                return new List<ClaimFlagReasonModel>();
            }
        }

        public async Task<ClaimBillingProviderOtherDto?> GetBillingProviderDetailsIdAsync(int claimId)
        {
            if (claimId <= 0)
                return null;

            try
            {
                // Select only the required columns instead of loading the full entity
                var dto = await _claimBillingProviderRepository.Query()
                    .AsNoTracking()
                    .Where(x => x.ClaimId == claimId && x.DateDeleted == null)
                    .Select(x => new ClaimBillingProviderOtherDto
                    {
                        ClaimId = x.ClaimId,
                        ProviderType = x.ProviderType,
                        FirstName = x.FirstName ?? string.Empty,
                        LastNameOrFacilityName = x.LastNameOrFacilityName,
                        NPI = x.NPI,
                        TaxId = x.TaxId ?? string.Empty,
                        TaxonomyCode = x.TaxonomyCode ?? string.Empty,
                        AddressLine1 = x.AddressLine1,
                        AddressLine2 = x.AddressLine2 ?? string.Empty,
                        City = x.City,
                        State = x.State,
                        Zip = x.Zip,
                        ZipExt = x.ZipExt
                    })
                    .FirstOrDefaultAsync();

                return dto;
            }
            catch (Exception ex)
            {
                // Log the exception with full context
                _logger.LogError(ex,
                    "{Service}.{Method} - Error retrieving billing provider details. ClaimId={ClaimId}",
                    nameof(ClaimService),
                    nameof(GetBillingProviderDetailsIdAsync),
                    claimId);

                return null;
            }
        }

        public async Task<bool> AssignClaimsAsync(int[] claimIds, int assigneeId, int memberId)
        {
            try
            {
                var claims = await _claimRepository.Query()
                    .Where(x => claimIds.Contains(x.Id) && x.DateDeleted == null)
                    .ToListAsync();

                if (claims.Count == 0)
                    return false;

                foreach (var claim in claims)
                {
                    claim.AssigneeId = assigneeId;

                    MarkUpdated(claim, memberId);
                    _claimRepository.Update(claim);
                }

                await _claimRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }

        }
        public async Task<List<LocationBillingProviderDto>> GetLatestBillingProvidersAsync(int accountInfoId)
        {
            var query =
                from cs in _claimSubmissionRepository.Query()
                join c in _claimRepository.Query()
                    on cs.ClaimId equals c.Id
                where c.AccountInfoId == accountInfoId
                      && cs.LocationBillingProviderNpiNumber != null
                group cs by cs.LocationBillingProviderNpiNumber into g
                select g
                    .OrderByDescending(x => x.DateLastModified)
                    .Select(x => new LocationBillingProviderDto
                    {
                        LocationBillingProviderNpiNumber = x.LocationBillingProviderNpiNumber,
                        LocationBillingProviderName = x.LocationBillingProviderName
                    })
                    .FirstOrDefault();

            return await query.ToListAsync();
        }

        /// <summary>
        /// Retrieves all active states from the database and maps them to <see cref="StateDto"/> objects.
        /// </summary>
        /// <remarks>
        /// Only states that are not soft-deleted (where <c>DateDeleted</c> is null) are returned.
        /// The results are mapped from <see cref="StateEntity"/> to <see cref="StateDto"/> using AutoMapper.
        /// The results are cached for 24 hours to improve performance.
        /// </remarks>
        /// <returns>
        /// A <see cref="List{StateDto}"/> containing all active states. Returns an empty list if no active states exist.
        /// </returns>
        /// <exception cref="Exception">Throws if an unexpected error occurs while retrieving states.</exception>
        public async Task<List<StateDto>> GetStatesAsync()
        {
            try
            {
                // Fetching data from cache, or fetch it from DB if not available
                var data = await _cacheService.GetOrSetCacheAsync(
                    statesCacheKey,
                    async () => await _stateRepository.Query()
                        .AsNoTracking()
                        .Where(x => x.DateDeleted == null)
                        .ToListAsync(),
                    statesCacheDuration
                );

                if (data.Count == 0)
                    return new List<StateDto>();

                var dtos = _mapper.Map<List<StateDto>>(data);
                return dtos;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private void MapBillingProvider(ClaimBillingProviderEntity entity, BillingProvider source)
        {
            entity.ProviderType = source.ProviderType;
            entity.FirstName = source.FirstName;
            entity.LastNameOrFacilityName = source.LastNameOrFacilityName;
            entity.NPI = source.Npi;
            entity.TaxId = source.TaxId;
            entity.TaxonomyCode = source.TaxonomyCode;
            entity.AddressLine1 = source.AddressLine1;
            entity.AddressLine2 = source.AddressLine2;
            entity.City = source.City;
            entity.State = source.State;
            entity.Zip = source.Zip;
            entity.ZipExt = source.ZipExt;
        }

        private async Task<int> AddUpdateBillingProviderAsync(int memberId, BillingProviderRequest request)
        {
            if (request == null || request.BillingProvider == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            var existingProvider = await _claimBillingProviderRepository.Query().FirstOrDefaultAsync(x => x.ClaimId == request.ClaimId && x.DateDeleted == null);

            if (existingProvider != null)
            {
                MapBillingProvider(existingProvider, request.BillingProvider);

                existingProvider.DateLastModified = EstDateTime;
                existingProvider.ModifiedBy = memberId;

                await _claimBillingProviderRepository.CommitAsync();
                return existingProvider.Id;
            }

            var billingProvider = new ClaimBillingProviderEntity();
            MapBillingProvider(billingProvider, request.BillingProvider);
            billingProvider.ClaimId = request.ClaimId;
            billingProvider.DateCreated = EstDateTime;
            billingProvider.CreatedBy = memberId;
            _claimBillingProviderRepository.Add(billingProvider);
            await _claimBillingProviderRepository.CommitAsync();

            return billingProvider.Id;
        }

        /// <summary>
        /// Retrieves all external codes of type <see cref="ExternalCodeType.ClaimStatusCode"/>.
        /// The result is cached for improved performance.
        /// </summary>
        /// <returns>
        /// A list of <see cref="ExternalCodeResponseModel"/> representing the external codes.
        /// Returns an empty list if no records are found.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown when an unexpected error occurs while retrieving external codes.
        /// </exception>
        public async Task<List<ExternalCodeResponseModel>> GetAllExternalCodes()
        {
            _logger.LogInformation("Fetching external codes from cache or database.");

            var externalCodes = await _cacheService.GetOrSetCacheAsync(
                externalCodesCacheKey,
                async () => await _externalCodeRepository.Query()
                    .AsNoTracking()
                    .Where(x => x.CodeTypeId == ExternalCodeType.ClaimStatusCode)
                    .ToListAsync(),
                TimeSpan.FromDays(1)
            );

            if (externalCodes == null || !externalCodes.Any())
            {
                _logger.LogWarning("No external codes found for type ClaimStatusCode.");
                return new List<ExternalCodeResponseModel>();
            }

            _logger.LogInformation("Successfully retrieved {Count} external codes.", externalCodes.Count);
            return _mapper.Map<List<ExternalCodeResponseModel>>(externalCodes);
        }
    }
}