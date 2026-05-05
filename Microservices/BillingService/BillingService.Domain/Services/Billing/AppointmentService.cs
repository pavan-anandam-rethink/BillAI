using AutoMapper;
using BillingService.Domain.Extensions;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.Common;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.VariantTypes;
using Microsoft.EntityFrameworkCore;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Claim.History;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Extensions;
using Rethink.Services.Common.Handlers;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Messaging;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Services;
using Rethink.Services.Common.Utils;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public sealed class AppointmentService : BaseService, IAppointmentService
    {
        private const string claimsAssigneeKey = "claimsAssignees_{0}";
        private const int cacheExpiration = 15; // 15 minutes for claims assignees
        private const string BillingView = "BillingView";
        public static readonly int[] TabReadyToBill = { 2, 7, 10, 11, 14 };
        public static readonly int[] TabBillingPending = { 3, 4, 12, 15, 16, 17 };
        public static readonly int[] TabCompleted = { 18, 6, 13 };
        public static readonly int[] TabRejected = { 8, 9 };

        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private readonly IRepository<BillingDbContext, ClaimAppointmentLinkEntity> _linkRepository;
        private readonly IRepository<BillingDbContext, ClaimChargeEntryEntity> _chargeEntryRepository;
        private readonly IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry> _linkChargeRepository;
        private readonly IRepository<BillingDbContext, ClaimHistoryEntity> _claimHistoryRepository;
        private readonly IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity> _claimSubmissionServiceLineRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimEntity> _paymentClaimRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineEntity> _paymentClaimServiceLineRepository;
        private readonly IRepository<BillingDbContext, ClaimSearchFunderEntity> _claimSearchFunderRepository;
        private readonly IClaimHistoryService _claimHistoryService;
        private readonly IClaimSyncService _claimSyncService;
        private readonly IMessageBus _bus;
        private readonly IClaimManagerService _claimManagerService;
        private readonly IClaimValidationService _claimValidationService;
        private readonly IRethinkMasterDataMicroServices _rethinkService;
        private readonly IMapper _mapper;
        private ClaimTransactionType transactionType;
        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        private readonly ICacheService _cacheService;

        public AppointmentService(IRepository<BillingDbContext, ClaimAppointmentLinkEntity> linkRepository,
            IRepository<BillingDbContext, ClaimHistoryEntity> claimHistoryRepository,
            IRepository<BillingDbContext, ClaimChargeEntryEntity> chargeEntryRepository,
            IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry> linkChargeRepository,
            IRepository<BillingDbContext, ClaimSubmissionServiceLineEntity> claimSubmissionServiceLineRepository,
            IRepository<BillingDbContext, PaymentClaimEntity> paymentClaimRepository,
            IRepository<BillingDbContext, PaymentClaimServiceLineEntity> paymentClaimServiceLineRepository,
            IRepository<BillingDbContext, ClaimSearchFunderEntity> claimSearchFunderRepository,
            IClaimManagerService claimManagerService,
            IClaimValidationService claimValidationService,
            IRethinkMasterDataMicroServices rethinkService,
            IMapper mapper,
            IClaimHistoryService claimHistoryService,
            IClaimSyncService claimSyncService,
            IMessageBus bus,
            IRethinkMasterDataMicroServices rethinkServices,
            ICacheService cacheService)
        {
            _claimSyncService = claimSyncService;
            _linkRepository = linkRepository;
            _claimHistoryRepository = claimHistoryRepository;
            _chargeEntryRepository = chargeEntryRepository;
            _mapper = mapper;
            _claimRepository = claimRepository;
            _claimManagerService = claimManagerService;
            _claimValidationService = claimValidationService;
            _linkChargeRepository = linkChargeRepository;
            _rethinkService = rethinkService;
            _claimHistoryService = claimHistoryService;
            _claimSubmissionServiceLineRepository = claimSubmissionServiceLineRepository;
            _paymentClaimRepository = paymentClaimRepository;
            _paymentClaimServiceLineRepository = paymentClaimServiceLineRepository;
            _claimSearchFunderRepository = claimSearchFunderRepository;
            _bus = bus;
            _rethinkServices = rethinkServices;
            _cacheService = cacheService;
        }

        public async Task<List<ClaimsAssigneeResponse>> GetClaimsAssignees(ClaimFilterGetModel model)
        {
            var cacheKey = string.Format(claimsAssigneeKey, model.AccountInfoId);

            var staffMembers = await _cacheService.GetOrSetCacheAsync(
                cacheKey,
                async () => await _rethinkService.GetStaffMemberListByPermission(model.AccountInfoId, new List<string> { BillingView }, "OR"),
                TimeSpan.FromMinutes(cacheExpiration)
            );

            if (staffMembers == null)
                return null;


            var staffResponses = staffMembers
                    .Select(staff => new ClaimsAssigneeResponse
                    {
                        MemberId = staff.memberId,
                        Name = FullNameExt.GetFullName(staff.firstName, null, staff.lastName)
                    })
                    .OrderBy(s => s.Name)
                    .ToList();

            if (model.Tab != 0)
            {
                var assigneeIds = _claimRepository.Query()
                    .AsNoTracking()
                    .Where(c =>
                        c.AccountInfoId == model.AccountInfoId &&
                        c.DateDeleted == null &&
                        (
                            (model.Tab == ClaimListingTab.PendingReview && (int)c.ClaimStatus == 1 && c.IsFlagged == false) ||
                            (model.Tab == ClaimListingTab.ReadyToBill && TabReadyToBill.Contains((int)c.ClaimStatus) && c.IsFlagged == false) ||
                            (model.Tab == ClaimListingTab.BillingPending && TabBillingPending.Contains((int)c.ClaimStatus) && c.IsFlagged == false) ||
                            (model.Tab == ClaimListingTab.Completed && TabCompleted.Contains((int)c.ClaimStatus) && c.IsFlagged == false) ||
                            (model.Tab == ClaimListingTab.Rejected && TabRejected.Contains((int)c.ClaimStatus) && c.IsFlagged == false) ||
                            (model.Tab == ClaimListingTab.Denied && (int)c.ClaimStatus == 5 && c.IsFlagged == false) ||
                            (model.Tab == ClaimListingTab.Flagged && c.IsFlagged == true)
                        ))
                    .Select(s => s.AssigneeId)
                    .Distinct()
                    .ToList();

                var assigneeSet = new HashSet<int>(assigneeIds);

                staffResponses = staffResponses
                    .Where(s => assigneeSet.Contains(s.MemberId))
                    .OrderBy(s => s.Name)
                    .ToList();
            }

            staffResponses.Insert(0, new ClaimsAssigneeResponse
            {
                MemberId = 0,
                Name = "Unassigned"
            });

            if (model.SearchValue != null)
                return staffResponses.Where(x => x.Name != null && x.Name.ToLower().Contains(model.SearchValue.ToLower())).ToList();

            return staffResponses;
        }

        public async Task<List<AppointmentModel>> GetFor(int accountInfoId, int currentMemberId, int claimId,
            int? clientId, int? memberId, DateTime? startDate, DateTime? endDate, int? locationId)
        {
            var appointments = new List<AppointmentRethinkModel>();
            var claim = await _claimRepository.Query().FirstOrDefaultAsync(x => x.Id == claimId);
            var placeOfService = claim.LocationCodeId;
            var renderingProviderId = claim.RenderingStaffMemberId;
            var childFunderId = claim.ClientFunderId;
            var authNumber = claim.AuthorizationNumber;

            var funder = await _rethinkService.GetChildProfileFunderMappingByMappingId(accountInfoId, clientId ?? 0, childFunderId ?? 0);

            var completedApptList = await _rethinkService.GetCompletedAppointmentListAsync(accountInfoId, clientId ?? 0, (DateTime)startDate);

            completedApptList = completedApptList.Where(x =>
                                x.appointmentTypeId == 1 &&
                                x.occurrenceTypeId == 1 &&
                                x.locationId == locationId &&
                                x.funderId == funder.funderId &&
                                x.startDate <= endDate).ToList();

            if (!completedApptList.Any())
            {
                return new List<AppointmentModel>();
            }

            var locationCodes = await _rethinkService.GetLocationCodes();

            foreach (var appointment in completedApptList)
            {
                appointment.PlaceOfService = locationCodes.FirstOrDefault(x => x.id == appointment.locationId);
                if (appointment.PlaceOfService.id != placeOfService) continue;

                appointment.StaffMember = await _rethinkService.GetStaffMember(accountInfoId, appointment.staffId);
                appointment.StaffMember.Member = await _rethinkService.GetMemberAsync(accountInfoId, appointment.StaffMember.memberId);
                appointment.StaffMember.Member.AccountInfo = await _rethinkService.GetAccountReturningEntityAsync(appointment.StaffMember.Member.accountId);
                if (appointment.StaffMember.Member.accountId != accountInfoId) continue;

                appointment.ChildProfileAuthorizationBillingCode = await _rethinkService.GetChildProfileAuthBillingCodeForAppointment(accountInfoId, clientId ?? 0, appointment.procedureCodeId);
                appointment.ProviderBillingCode = await _rethinkService.GetProviderBillingCode(accountInfoId, appointment.providerBillingCodeId ?? 0);
                if (claim.AuthorizationId != null)
                {
                    if (appointment.ChildProfileAuthorizationBillingCode != null)
                    {
                        appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization = await _rethinkService.GetChildProfileAuthorizationByClientId(accountInfoId, appointment.clientId ?? 0, appointment.ChildProfileAuthorizationBillingCode.childProfileAuthorizationId);

                        if (appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization != null)
                        {
                            appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization.RenderingProvider = await _rethinkService.GetMemberAsync(accountInfoId, appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization.renderingProviderStaffId ?? 0);

                            if (appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization.RenderingProvider != null)
                            {
                                if (appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization.RenderingProvider.id != renderingProviderId) continue;
                            }
                            appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode = await _rethinkService.GetProviderBillingCode(accountInfoId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId ?? 0);
                            appointments.Add(appointment);
                        }
                    }
                }
                else
                {
                    if (appointment.ChildProfileAuthorizationBillingCode == null)
                    {
                        appointments.Add(appointment);
                    }
                }
            }

            appointments = appointments.OrderBy(x => x.startDate).ThenBy(y => y.startTime).ToList();

            var appointmentIds = appointments.Select(x => x.id).ToList();

            var appointmentIdWithLinks = await _linkRepository.Query().Where(l =>
                appointmentIds.Contains(l.AppointmentId) && !l.DateDeleted.HasValue).Select(x => x.AppointmentId)
                .ToListAsync();

            if (appointmentIdWithLinks.Any())
            {
                appointments = appointments.Where(app => !appointmentIdWithLinks.Contains(app.id)).ToList();
            }

            return await ToAppointmentItems(accountInfoId, appointments, currentMemberId);
        }

        public async Task<List<AppointmentModel>> GetForClaim(int accountInfoId, int memberId, int claimId)
        {
            var appointmentsLinks = await _linkRepository.Query()
                    .Include(x => x.Claim)
                    .Where(x => x.DateDeleted == null && x.Claim.AccountInfoId == accountInfoId &&
                                x.ClaimId == claimId).ToListAsync();

            if (!appointmentsLinks.Any())
            {
                return new List<AppointmentModel>();
            }

            var appointmentIds = appointmentsLinks.Select(x => x.AppointmentId).ToList();

            var appointmentList = await _rethinkService.GetAppointmentListAsync(appointmentIds);

            appointmentList = await SetupRethinkDataForAppointments(appointmentList);

            return await ToAppointmentItems(accountInfoId, appointmentList, memberId);
        }


        public async Task<List<AppointmentModel>> ToAppointmentItems(int accountInfoId, List<AppointmentRethinkModel> appointments, int memberId)
        {
            // Get all client for Account.
            var clientUsersList = await _rethinkServices.GetChildProfilesForAccount(accountInfoId);

            var appItems = _mapper.Map<List<DataObjects.Billing.AppointmentItem>>(appointments);
            appItems.ForEach(a =>
            {
                var currentApp = appointments.FirstOrDefault(x => x.id == a.Id);
                var origAppBC = currentApp != null && currentApp.ChildProfileAuthorizationBillingCode == null ?
                    currentApp.ProviderBillingCode
                    : currentApp.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode;

                a.BillingCode = origAppBC != null ? origAppBC.billingCode : string.Empty;
                a.BillingCode2 = origAppBC != null ? origAppBC.billingCode2 : string.Empty;

                var funder = _claimSearchFunderRepository.Query().FirstOrDefault(x => x.Id == a.FunderId);
                a.FunderName = funder != null ? funder.Name : "";
                var client = clientUsersList.FirstOrDefault(x => x.Id == a.ClientId);
                a.ClientName = client != null ? client.FirstName + " " + client.MiddleName + " " + client.LastName : "";

               });

            return _mapper.Map<List<AppointmentModel>>(appItems);
        }

        public async Task<(bool, DateTime?, DateTime?)> LinkAppointments(int accountInfoId, int memberId, int claimId,
            List<int> appointmentIds)
        {
            List<ClaimTransactionModel> chargeEntriesToSend = [];

            var claim = await _claimRepository.Query()
                .Include(x => x.ClaimSubmissions)
                .FirstAsync(x => x.Id == claimId);

            var queryAppointmentList = await _linkRepository.Query()
                .Include(x => x.Claim)
                .Include(x => x.ClaimAppointmentLinkChargeEntry)
                .Where(x => x.DateDeleted == null && x.Claim.AccountInfoId == accountInfoId &&
                            x.ClaimId == claimId).ToListAsync();
            var queryAppointmentId = queryAppointmentList.Select(x => x.AppointmentId);

            var usedAppointmentIds = queryAppointmentId.ToList();

            var queryAppointments = await AppointmentsQuery(appointmentIds);

            var appointmentLookup = queryAppointments.ToDictionary(x => x.id, x => x);

            var appForCheck = appointmentLookup.FirstOrDefault().Value;
            if (claim.ChildProfileId != appForCheck.clientId)
            {
                return (false, claim.StartDate, claim.EndDate);
            }

            var appointmentLinksToUpdate = queryAppointmentList.Where(x => x.ClaimAppointmentLinkChargeEntry == null);
            if (appointmentLinksToUpdate.Any()) // restore ClaimAppointmentLinkChargeEntry for previously submitted links
            {
                var updateAppointmentIds = appointmentLinksToUpdate.Select(x => x.AppointmentId);

                var updateAppointments = await AppointmentsQuery(updateAppointmentIds.ToList());
                //var updateAppointments = await queryAllAppointments.Where(x => updateAppointmentIds.Contains(x.Id)).ToListAsync();


                var chargeEntriesToCheck = await _chargeEntryRepository.Query()
                    .Where(x => x.ClaimId == claimId && x.DateDeleted == null).ToListAsync();

                foreach (var link in appointmentLinksToUpdate)
                {
                    var appointment = updateAppointments.FirstOrDefault(x => x.id == link.AppointmentId);
                    string diagnosisCode = string.Empty;

                    appointment.ChildProfileAuthorizationBillingCode = await _rethinkService.GetChildProfileAuthBillingCodeForAppointment(accountInfoId, appointment.clientId ?? 0, appointment.procedureCodeId);
                    if (appointment.ChildProfileAuthorizationBillingCode != null)
                    {
                        appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization = await _rethinkService.GetChildProfileAuthorizationByClientId(accountInfoId, appointment.clientId ?? 0, appointment.ChildProfileAuthorizationBillingCode.childProfileAuthorizationId);
                        appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization.ChildProfileDiagnosis =
                                    await _rethinkService.GetClientDiagnosisById(accountInfoId, appointment.clientId ?? 0, appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization.childProfileDiagnosisId);
                        appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode = await _rethinkService.GetProviderBillingCode(accountInfoId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId.Value);
                        appointment.ProviderBillingCodeCredential = await _rethinkService.GetProviderBillingCodeCredential(accountInfoId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId ?? 0, appointment.providerBillingCodeCredentialId);
                        diagnosisCode =
                            appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization != null &&
                            appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization
                                .ChildProfileDiagnosis != null &&
                            appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization
                                .ChildProfileDiagnosis.diagnosis != null
                                ? appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization
                                    .ChildProfileDiagnosis.diagnosis.diagnosisCode
                            : null;
                    }
                    else
                    {
                        appointment.ProviderBillingCodeCredential = await _rethinkService.GetProviderBillingCodeCredential(accountInfoId, appointment.providerBillingCodeId ?? 0, appointment.providerBillingCodeCredentialId);
                        appointment.ProviderBillingCode = await _rethinkService.GetProviderBillingCode(accountInfoId, appointment.providerBillingCodeId.Value);
                        diagnosisCode = await _claimSyncService.AddDiagnosisCodes(claim, appointment.ChildProfileAuthorizationBillingCode, appointment.serviceId);
                    }



                    appointment.StaffMember = await _rethinkService.GetStaffMember(appointment.staffAccountInfoId, appointment.staffId);

                    var billingCode = appointment.ChildProfileAuthorizationBillingCode != null ? appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode.billingCode : appointment.ProviderBillingCode.billingCode;
                    var billingCode2 = appointment.ChildProfileAuthorizationBillingCode != null ? appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode.billingCode2 : appointment.ProviderBillingCode.billingCode2;
                    string modifier1 = appointment.ProviderBillingCodeCredential?.modifier1;
                    string modifier2 = appointment.ProviderBillingCodeCredential?.modifier2;

                    var entriesToConnect = chargeEntriesToCheck.Where(x => x.Modifier1 == modifier1 &&
                        x.Modifier2 == modifier2 &&
                        x.DiagnosisCode == diagnosisCode &&
                        (x.BillingCode == billingCode || x.BillingCode == billingCode2));

                    foreach (var entryToConnect in entriesToConnect)
                    {
                        var secondBillingCode = entryToConnect.BillingCode == billingCode2;

                        var claimAppointmentLinkChargeEntry = new ClaimAppointmentLinkChargeEntry
                        {
                            NpiNumber = appointment.StaffMember.identifiers.FirstOrDefault(x => x.identifierType.ToLower() == "npinumber")?.value,
                            ClaimChargeEntryEntityId = entryToConnect.Id,
                            IsSecondBillingCode = secondBillingCode
                        };
                        MarkCreated(claimAppointmentLinkChargeEntry, memberId);
                        link.ClaimAppointmentLinkChargeEntry = claimAppointmentLinkChargeEntry;

                        await _linkChargeRepository.AddAsync(claimAppointmentLinkChargeEntry);
                    }
                }

                await _linkChargeRepository.CommitAsync();
            }

            foreach (int appointmentId in appointmentIds)
            {
                if (!usedAppointmentIds.Contains(appointmentId))
                {
                    // add
                    var entity = new ClaimAppointmentLinkEntity();
                    entity.ClaimId = claimId;
                    entity.AppointmentId = appointmentId;

                    MarkCreated(entity, memberId);
                    _linkRepository.Add(entity);

                    await _bus.SendAsync(new AppointmentBillingStatus
                    {
                        AppointmentId = appointmentId,
                        BillingStatus = claim.billedDate.HasValue ? RethinkBillingStatus.Billed : RethinkBillingStatus.Pending,
                        ModifiedDate = EstDateTime
                    }, Queues.RT_Billing_Queue_AppointmentBillingStatus);

                    // set charges
                    var appointment = appointmentLookup[appointmentId];

                    appointment.ChildProfileAuthorizationBillingCode = await _rethinkService.GetChildProfileAuthBillingCodeForAppointment(accountInfoId, appointment.clientId ?? 0, appointment.procedureCodeId);

                    if (appointment.ChildProfileAuthorizationBillingCode != null)
                    {
                        appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode = await _rethinkService.GetProviderBillingCode(accountInfoId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId ?? 0);
                        appointment.ProviderBillingCodeCredential = await _rethinkService.GetProviderBillingCodeCredential(accountInfoId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId ?? 0, appointment.providerBillingCodeCredentialId);

                        var providerBillingCode = appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode;
                        string billingCode = providerBillingCode.billingCode;
                        string billingCode2 = providerBillingCode.billingCode2;

                        entity.ClaimAppointmentLinkChargeEntry = await AddLink(appointment, queryAppointmentList, claimId, accountInfoId, memberId, billingCode, claim.StartDate, true);

                        if (!string.IsNullOrEmpty(billingCode2))
                        {
                            entity.ClaimAppointmentLinkChargeEntry = await AddLink(appointment, queryAppointmentList, claimId, accountInfoId, memberId, billingCode2, claim.StartDate, false);
                        }
                        chargeEntriesToSend.Add(PrepareClaimTransaction(entity.ClaimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId, ClaimTransactionType.billedAmount));
                    }
                    else
                    {
                        appointment.ProviderBillingCode = await _rethinkService.GetProviderBillingCode(accountInfoId, appointment.providerBillingCodeId ?? 0);
                        appointment.ProviderBillingCodeCredential = await _rethinkService.GetProviderBillingCodeCredential(accountInfoId, appointment.providerBillingCodeId ?? 0, appointment.providerBillingCodeCredentialId);

                        var providerBillingCode = appointment.ProviderBillingCode;
                        string billingCode = providerBillingCode.billingCode;
                        string billingCode2 = providerBillingCode.billingCode2;

                        entity.ClaimAppointmentLinkChargeEntry = await AddLink(appointment, queryAppointmentList, claimId, accountInfoId, memberId, billingCode, claim.StartDate, true);

                        if (!string.IsNullOrEmpty(billingCode2))
                        {
                            entity.ClaimAppointmentLinkChargeEntry = await AddLink(appointment, queryAppointmentList, claimId, accountInfoId, memberId, billingCode2, claim.StartDate, false);
                        }
                        chargeEntriesToSend.Add(PrepareClaimTransaction(entity.ClaimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId, ClaimTransactionType.billedAmount));
                    }
                }
            }

            //#119539
            if (claim.ClaimStatus == ClaimStatus.PendingReview && claim.ClaimSubmissions.Count > 1 && !claim.IsFlagged)
            {
                //claim.IsFlagged = true;

                var claimHistoryEntity = new ClaimHistoryEntity
                {
                    ClaimId = claimId,
                    Mode = ClaimActionMode.System,
                    ClaimAction = ClaimAction.Flag,
                    ClaimHistoryAction = ClaimHistoryAction.Flagged,
                    ActionDate = EstDateTime
                };

                MarkCreated(claimHistoryEntity, memberId);
                await _claimHistoryRepository.AddAsync(claimHistoryEntity);
            }

            var (startDate, endDate) = GetClaimStartDateEndDate(claim.Id);
            claim.StartDate = startDate;
            claim.EndDate = endDate;

            MarkUpdated(claim, memberId);
            _claimRepository.Update(claim);


            await _chargeEntryRepository.CommitAsync();
            await _claimValidationService.ValidateClaimData(claimId, memberId, null);

            await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
            {
                ClaimId = claim.Id,
                MemberId = claim.MemberId,
                Mode = ClaimActionMode.User,
                ClaimAction = ClaimAction.Added,
                ClaimHistoryAction = ClaimHistoryAction.AppointmentAdded,
                NewValue = appointmentIds.First().ToString(),
            });

            await _bus.SendBatchAsync(Topics.RT_Billing_ProcessClaimTxn, chargeEntriesToSend);

            return (true, claim.StartDate, claim.EndDate);
        }

        public async Task<(bool, DateTime?, DateTime?)> UnLinkAppointments(int accountInfoId, int memberId, int claimId,
            List<int> appointmentIds)
        {
            List<ClaimTransactionModel> chargeEntriesToSend = [];

            var claim = await _claimRepository.Query().FirstAsync(x => x.Id == claimId);

            var claimAppointmentLinks = await _linkRepository.Query()
                .Include(x => x.Claim)
                .Include(x => x.ClaimAppointmentLinkChargeEntry)
                //.Include("Appointment.ChildProfileAuthorizationBillingCode.ProviderBillingCode")
                .Where(x => x.DateDeleted == null && x.Claim.AccountInfoId == accountInfoId &&
                            x.ClaimId == claimId).ToListAsync();

            var appointmentsToDelete = claimAppointmentLinks.Where(x => appointmentIds.Contains(x.AppointmentId));

            var queryAppointments = await AppointmentsQuery(appointmentIds);

            var appointmentLookup = queryAppointments.ToDictionary(x => x.id, x => x);

            var appForCheck = appointmentLookup.FirstOrDefault().Value;
            if (claim.ChildProfileId != appForCheck.clientId)
            {
                return (false, claim.StartDate, claim.EndDate);
            }

            foreach (int appointmentId in appointmentIds)
            {
                // unset charges
                var appointmentTemp = appointmentLookup[appointmentId];

                if (appointmentTemp.ChildProfileAuthorizationBillingCode != null)
                {
                    var claimAppointmentLinkChargeEntries = claimAppointmentLinks.Where(x => x.AppointmentId == appointmentTemp.id).Select(x => x.ClaimAppointmentLinkChargeEntry);
                    if (claimAppointmentLinkChargeEntries.FirstOrDefault() != null && claimAppointmentLinkChargeEntries.Any())
                    {
                        foreach (var claimAppointmentLinkChargeEntry in claimAppointmentLinkChargeEntries)
                        {
                            var chargeEntriesToSameAppointments = await _linkChargeRepository.Query()
                                .Where(x => x.ClaimChargeEntryEntityId == claimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId &&
                                x.IsSecondBillingCode == claimAppointmentLinkChargeEntry.IsSecondBillingCode).ToListAsync();

                            if (chargeEntriesToSameAppointments.Any())
                            {
                                var leftChargeEntriesIds = chargeEntriesToSameAppointments.Where(x => x.Id == claimAppointmentLinkChargeEntry.Id).Select(x => x.Id).ToList();
                                var leftAppointmentsIds = claimAppointmentLinks.Where(x => x.ClaimAppointmentLinkChargeEntryId != null && leftChargeEntriesIds.Contains(x.ClaimAppointmentLinkChargeEntryId ?? 0)).Select(x => x.AppointmentId).ToList();
                                var leftAppointments = new List<AppointmentRethinkModel>();
                                if (leftAppointmentsIds.Any())
                                {
                                    var leftAppointmentsQuery = await AppointmentsQuery(leftAppointmentsIds);
                                    leftAppointments = leftAppointmentsQuery.ToList();
                                }

                                claimAppointmentLinkChargeEntry.ClaimChargeEntry = await _chargeEntryRepository.Query().FirstOrDefaultAsync(x => x.Id == claimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId);
                                var chargeEntryToUpdate = await _chargeEntryRepository.Query().FirstOrDefaultAsync(x => x.Id == claimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId);

                                await UpdateChargeEntity(leftAppointments, chargeEntryToUpdate, !(bool)claimAppointmentLinkChargeEntry.IsSecondBillingCode, false);
                                await _chargeEntryRepository.CommitAsync();
                            }
                            else
                            {
                                var chargeEntryToDelete = await _chargeEntryRepository.Query().FirstOrDefaultAsync(x => x.Id == chargeEntriesToSameAppointments.First().ClaimChargeEntryEntityId);
                                if (chargeEntryToDelete != null)
                                {
                                    SoftDelete(chargeEntryToDelete, memberId);
                                    _chargeEntryRepository.Update(chargeEntryToDelete);
                                }
                            }

                            SoftDelete(claimAppointmentLinkChargeEntry, memberId);
                            _linkChargeRepository.Update(claimAppointmentLinkChargeEntry);
                            if (!chargeEntriesToSend.Any(x => x.TransactionTypeId == claimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId))
                            {
                                chargeEntriesToSend.Add(PrepareClaimTransaction(claimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId, transactionType));
                            }
                        }
                    }
                }
                else
                {
                    var claimAppointmentLinkChargeEntries = claimAppointmentLinks.Where(x => x.AppointmentId == appointmentTemp.id).Select(x => x.ClaimAppointmentLinkChargeEntry);
                    if (claimAppointmentLinkChargeEntries.FirstOrDefault() != null && claimAppointmentLinkChargeEntries.Any())
                    {
                        foreach (var claimAppointmentLinkChargeEntry in claimAppointmentLinkChargeEntries)
                        {
                            var chargeEntriesToSameAppointments = await _linkChargeRepository.Query()
                                .Where(x => x.ClaimChargeEntryEntityId == claimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId &&
                                x.IsSecondBillingCode == claimAppointmentLinkChargeEntry.IsSecondBillingCode).ToListAsync();

                            if (chargeEntriesToSameAppointments.Any())
                            {
                                var leftChargeEntriesIds = chargeEntriesToSameAppointments.Where(x => x.Id == claimAppointmentLinkChargeEntry.Id).Select(x => x.Id).ToList();
                                var leftAppointmentsIds = claimAppointmentLinks.Where(x => x.ClaimAppointmentLinkChargeEntryId != null && leftChargeEntriesIds.Contains(x.ClaimAppointmentLinkChargeEntryId ?? 0)).Select(x => x.AppointmentId).ToList();
                                var leftAppointments = new List<AppointmentRethinkModel>();
                                if (leftAppointmentsIds.Any())
                                {
                                    var leftAppointmentsQuery = await AppointmentsQuery(leftAppointmentsIds);
                                    leftAppointments = leftAppointmentsQuery.ToList();
                                }

                                claimAppointmentLinkChargeEntry.ClaimChargeEntry = await _chargeEntryRepository.Query().FirstOrDefaultAsync(x => x.Id == claimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId);
                                //var chargeEntryToUpdate = await _chargeEntryRepository.Query().FirstOrDefaultAsync(x => x.Id == claimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId);

                                await UpdateChargeEntityWithoutAuth(leftAppointments, claimAppointmentLinkChargeEntry.ClaimChargeEntry, !(bool)claimAppointmentLinkChargeEntry.IsSecondBillingCode, false);
                                await _chargeEntryRepository.CommitAsync();

                            }
                            else
                            {
                                var chargeEntryToDelete = await _chargeEntryRepository.Query().FirstOrDefaultAsync(x => x.Id == chargeEntriesToSameAppointments.First().ClaimChargeEntryEntityId);
                                if (chargeEntryToDelete != null)
                                {
                                    SoftDelete(chargeEntryToDelete, memberId);
                                    _chargeEntryRepository.Update(chargeEntryToDelete);
                                }
                            }

                            SoftDelete(claimAppointmentLinkChargeEntry, memberId);
                            _linkChargeRepository.Update(claimAppointmentLinkChargeEntry);
                            if (!chargeEntriesToSend.Any(x => x.TransactionTypeId == claimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId))
                            {
                                chargeEntriesToSend.Add(PrepareClaimTransaction(claimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId, transactionType));
                            }
                        }
                    }
                }

                var toDelete = appointmentsToDelete.First(a => a.AppointmentId == appointmentId);

                SoftDelete(toDelete, memberId);
                _linkRepository.Update(toDelete);

                await _bus.SendAsync(new AppointmentBillingStatus
                {
                    AppointmentId = appointmentId,
                    BillingStatus = RethinkBillingStatus.NotBilled,
                    ModifiedDate = EstDateTime
                }, Queues.RT_Billing_Queue_AppointmentBillingStatus);
            }

            var (startDate, endDate) = GetClaimStartDateEndDate(claim.Id);
            claim.StartDate = startDate;
            claim.EndDate = endDate;

            MarkUpdated(claim, memberId);
            _claimRepository.Update(claim);

            await _chargeEntryRepository.CommitAsync();
            await _claimValidationService.ValidateClaimData(claimId, memberId, null);

            await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
            {
                ClaimId = claim.Id,
                MemberId = claim.MemberId,
                Mode = ClaimActionMode.User,
                ClaimAction = ClaimAction.Delete,
                ClaimHistoryAction = ClaimHistoryAction.AppointmentRemoved,
                NewValue = appointmentIds.First().ToString(),
            });

            await _bus.SendBatchAsync(Topics.RT_Billing_ProcessClaimTxn, chargeEntriesToSend);

            return (true, claim.StartDate, claim.EndDate);
        }

        private (DateTime, DateTime) GetClaimStartDateEndDate(int claimId)
        {
            var claimData = _claimRepository.Query().Include(c => c.ClaimChargeEntries).FirstOrDefault(x => x.Id == claimId);
            var chargeEntries = claimData.ClaimChargeEntries.Where(x => x.DateDeleted == null).ToList();
            claimData.StartDate = chargeEntries.OrderBy(x => x.DateOfService).FirstOrDefault()?.DateOfService ?? DateTime.MinValue;
            claimData.EndDate = chargeEntries.OrderByDescending(x => x.DateOfService).FirstOrDefault()?.DateOfService ?? DateTime.MinValue;
            return (claimData.StartDate, claimData.EndDate);
        }

        private async Task<ClaimAppointmentLinkChargeEntry> AddLink(AppointmentRethinkModel appointment,
            List<ClaimAppointmentLinkEntity> queryAppointmentList,
            int claimId,
            int accountInfoId,
            int memberId,
            string billingCode,
            DateTime startDate,
            bool firstCodeProcessing)
        {
            var claimAppointmentLinkChargeEntry = new ClaimAppointmentLinkChargeEntry();
            var appointmentsIdsLinkedToSameChargeEntry = new List<int>();
            if (appointment.providerBillingCodeId != null)
            {
                appointment.ProviderBillingCode = await _rethinkService.GetProviderBillingCode(appointment.staffAccountInfoId, appointment.providerBillingCodeId.Value);
            }
            else if (appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId != null)
            {
                appointment.ProviderBillingCode = await _rethinkService.GetProviderBillingCode(appointment.staffAccountInfoId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId.Value);
            }
            appointment.StaffMember = await _rethinkService.GetStaffMember(appointment.clientAccountInfoId, appointment.staffId);

            var chargeEntryToUpdate = await GetExistingClaimChargeEntity(appointment, claimId, accountInfoId, billingCode, startDate);
            if (chargeEntryToUpdate == null)
            {
                chargeEntryToUpdate = await CreateClaimChargeEntity(appointment, billingCode, claimId, memberId, startDate, firstCodeProcessing, appointment.ProviderBillingCode.id);
                _chargeEntryRepository.Add(chargeEntryToUpdate);
                await _chargeEntryRepository.CommitAsync();

                claimAppointmentLinkChargeEntry = new ClaimAppointmentLinkChargeEntry
                {
                    NpiNumber = appointment.StaffMember.identifiers.FirstOrDefault(x => x.identifierType.ToLower() == "npinumber")?.value,
                    ClaimChargeEntryEntityId = chargeEntryToUpdate.Id,
                    IsSecondBillingCode = false
                };
                MarkCreated(claimAppointmentLinkChargeEntry, memberId);
                _linkChargeRepository.Add(claimAppointmentLinkChargeEntry);
                await _linkChargeRepository.CommitAsync();

                var newLink = await _linkRepository.Query().FirstOrDefaultAsync(x => x.AppointmentId == appointment.id && x.ClaimId == claimId && x.DateDeleted == null);
                newLink.ClaimChargeEntriesId = claimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId;
                newLink.ClaimAppointmentLinkChargeEntryId = claimAppointmentLinkChargeEntry.Id;
                MarkCreated(newLink, memberId);
                _linkRepository.Update(newLink);
                await _linkRepository.SaveChangesAsync();

                queryAppointmentList.Clear();
                queryAppointmentList = await _linkRepository.Query()
                .Include(x => x.Claim)
                .Include(x => x.ClaimAppointmentLinkChargeEntry)
                .Where(x => x.DateDeleted == null && x.Claim.AccountInfoId == accountInfoId &&
                            x.ClaimId == claimId).ToListAsync();

                appointmentsIdsLinkedToSameChargeEntry = queryAppointmentList
                    .Where(ll => ll.ClaimAppointmentLinkChargeEntry != null &&
                        ll.ClaimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId == chargeEntryToUpdate.Id)
                    .Select(x => x.AppointmentId).ToList();
            }
            else
            {
                claimAppointmentLinkChargeEntry = new ClaimAppointmentLinkChargeEntry
                {
                    NpiNumber = appointment.StaffMember.identifiers.FirstOrDefault(x => x.identifierType.ToLower() == "npinumber")?.value,
                    ClaimChargeEntryEntityId = chargeEntryToUpdate.Id,
                    IsSecondBillingCode = false
                };
                MarkCreated(claimAppointmentLinkChargeEntry, memberId);
                _linkChargeRepository.Add(claimAppointmentLinkChargeEntry);
                await _linkChargeRepository.CommitAsync();

                var newLink = await _linkRepository.Query().FirstOrDefaultAsync(x => x.AppointmentId == appointment.id && x.ClaimId == claimId && x.DateDeleted == null);
                newLink.ClaimChargeEntriesId = claimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId;
                newLink.ClaimAppointmentLinkChargeEntryId = claimAppointmentLinkChargeEntry.Id;
                MarkCreated(newLink, memberId);
                _linkRepository.Update(newLink);
                await _linkRepository.SaveChangesAsync();

                appointmentsIdsLinkedToSameChargeEntry = queryAppointmentList
                .Where(ll => ll.ClaimAppointmentLinkChargeEntry != null &&
                    ll.ClaimAppointmentLinkChargeEntry.ClaimChargeEntryEntityId == chargeEntryToUpdate.Id)
                .Select(x => x.AppointmentId).ToList();

            }

            //if (appointmentsIdsLinkedToSameChargeEntry.Count() > 0)
            //{
            var appointments = new List<AppointmentRethinkModel>();
            appointments.Add(appointment);
            await UpdateChargeEntity(appointments, chargeEntryToUpdate, firstCodeProcessing, true);
            //}


            return claimAppointmentLinkChargeEntry;
        }


        private async Task UpdateChargeEntity(List<AppointmentRethinkModel> appointments, ClaimChargeEntryEntity chargeEntryToUpdate, bool firstCodeProcessing, bool chargesAddFlag = false)
        {
            int number = 1;
            decimal units = 0;
            decimal charges = 0;
            foreach (var appointment in appointments)
            {
                var appointmentHours =
                    (new TimeSpan((appointment.actualEndTime ?? appointment.endTime) / 60, (appointment.actualEndTime ?? appointment.endTime) % 60, 0)).TotalHours -
                    (new TimeSpan((appointment.actualStartTime ?? appointment.startTime) / 60,
                        (appointment.actualStartTime ?? appointment.startTime) % 60, 0)).TotalHours;
                if (appointment.ChildProfileAuthorizationBillingCode != null)
                {
                    appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode = await _rethinkService.GetProviderBillingCode(appointment.staffAccountInfoId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId.Value);

                    var unitTypes = await _rethinkService.GetUnitTypesAsync();
                    appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode.unitTypes2 = unitTypes.FirstOrDefault(x => x.id == appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode.unitTypeId2);
                    appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode.unitTypes = unitTypes.FirstOrDefault(x => x.id == appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode.unitTypeId);
                }
                else
                {
                    var unitTypes = await _rethinkService.GetUnitTypesAsync();
                    appointment.ProviderBillingCode.unitTypes = unitTypes.FirstOrDefault(x => x.id == appointment.ProviderBillingCode.unitTypeId);
                    appointment.ProviderBillingCode.unitTypes2 = unitTypes.FirstOrDefault(x => x.id == appointment.ProviderBillingCode.unitTypeId2);
                }

                var providerBillingCode = appointment.ChildProfileAuthorizationBillingCode != null ? appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode : appointment.ProviderBillingCode;

                int? unit = firstCodeProcessing ? (providerBillingCode.unitTypes.unit ?? 0) : (providerBillingCode.unitTypes2?.unit);

                bool isUntimed = unit > 0 ? false : true;

                if (isUntimed == true)
                {
                    unit = 60;
                }

                int billingCodeRateTypeId = providerBillingCode.providerBillingCodeRateTypeId ?? 1;
                int roundingTypeId = firstCodeProcessing ? (providerBillingCode.providerBillingCodeRoundingTypeId ?? 1) : (providerBillingCode.providerBillingCodeRoundingTypeId2 ?? 1);

                decimal numberOfUnits = isUntimed == true
                    ? 1
                    : (decimal)(RoundCacluation(appointmentHours / ((unit.Value / 60.0) > 0 ? (unit.Value / 60.0) : 1),
                        (RoundingTypes)roundingTypeId));

                if (appointment.ChildProfileAuthorizationBillingCode != null) { appointment.ProviderBillingCodeCredential = await _rethinkService.GetProviderBillingCodeCredential(appointment.staffAccountInfoId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId.Value, appointment.providerBillingCodeCredentialId); }
                else
                {
                    appointment.ProviderBillingCodeCredential = await _rethinkService.GetProviderBillingCodeCredential(appointment.staffAccountInfoId, appointment.providerBillingCodeId.Value, appointment.providerBillingCodeCredentialId);
                }


                decimal unitRate = 0;
                decimal? baseRate = providerBillingCode.providerSerivces != null
                    ? providerBillingCode.providerSerivces.baseRate
                    : null;
                decimal? rate = firstCodeProcessing ? providerBillingCode.rate : providerBillingCode.rate2;

                if (providerBillingCode.restrictStaffProviderToService == true)
                {
                    decimal? contactRate = appointment.ProviderBillingCodeCredential != null
                        ? appointment.ProviderBillingCodeCredential.contractRate
                        : null;
                    unitRate = billingCodeRateTypeId == 2
                        ? (contactRate == null ? 0 : contactRate ?? 0)
                        : (baseRate ?? 0);
                }
                else
                {
                    unitRate = (rate ?? 0);
                }

                string billingCode2 = firstCodeProcessing ? providerBillingCode.billingCode2 : null;

                decimal billingCodeUnits = numberOfUnits;

                if (!string.IsNullOrEmpty(billingCode2))
                {
                    if (number == 1 || units < 1)
                    {
                        billingCodeUnits = 1;

                        if (number > 1 && units < 1)
                        {
                            billingCodeUnits = billingCodeUnits - units;
                        }

                        numberOfUnits -= billingCodeUnits;
                    }
                }

                units += billingCodeUnits;
                charges += billingCodeUnits * unitRate;

                number++;
            }
            var previousCharge = chargeEntryToUpdate.Charges;
            if (chargesAddFlag)
            {
                chargeEntryToUpdate.Units += units;
                chargeEntryToUpdate.Charges += charges;
            }
            else
            {
                chargeEntryToUpdate.Units -= units;
                chargeEntryToUpdate.Charges -= charges;
            }

            var chargeDifference = chargeEntryToUpdate.Charges - previousCharge;

            _chargeEntryRepository.Update(chargeEntryToUpdate);

            transactionType = ClaimTransactionType.billedAmount;
            if (chargeEntryToUpdate.Units == 0)
            {
                SoftDelete(chargeEntryToUpdate, chargeEntryToUpdate.CreatedBy);
                _chargeEntryRepository.Update(chargeEntryToUpdate);
                transactionType = ClaimTransactionType.deleteCharge;
            }

            // UPDATE THE CLAIM SUBMISSION SERVICE LINES WHEN APPOINTMENT IS ADDED/DELETED
            var submissionChargeEntry = _claimSubmissionServiceLineRepository.Query().FirstOrDefault(x => x.ClaimChargeEntryId == chargeEntryToUpdate.Id);
            if (submissionChargeEntry != null)
            {
                submissionChargeEntry.Units = chargeEntryToUpdate.Units;
                submissionChargeEntry.Charges = chargeEntryToUpdate.Charges;
                if (chargeEntryToUpdate.Units == 0) SoftDelete(submissionChargeEntry, chargeEntryToUpdate.CreatedBy);
                _claimSubmissionServiceLineRepository.Update(submissionChargeEntry);
                await _claimSubmissionServiceLineRepository.SaveChangesAsync();
            }

            // UPDATE THE PAYMENT CLAIMS AND SERVICE LINES 
            var paymentClaimServiceLinesToUpdate = await _paymentClaimServiceLineRepository
                .Query()
                .Where(x => x.ClaimChargeEntryId == chargeEntryToUpdate.Id)
                .Include(c => c.PaymentClaim)
                .ToListAsync();

            if (paymentClaimServiceLinesToUpdate.Any())
            {
                foreach (var paymentClaimServiceLine in paymentClaimServiceLinesToUpdate)
                {
                    if (chargeEntryToUpdate.Units == 0)
                    {
                        var slCount = await _paymentClaimServiceLineRepository.Query().Where(x => x.PaymentClaimId == paymentClaimServiceLine.PaymentClaim.Id && x.DateDeleted == null).CountAsync();

                        SoftDelete(paymentClaimServiceLine, chargeEntryToUpdate.CreatedBy);
                        _paymentClaimServiceLineRepository.Update(paymentClaimServiceLine);

                        if (slCount == 1)
                        {
                            SoftDelete(paymentClaimServiceLine.PaymentClaim, chargeEntryToUpdate.CreatedBy);
                            _paymentClaimRepository.Update(paymentClaimServiceLine.PaymentClaim);
                        }
                        else
                        {
                            paymentClaimServiceLine.PaymentClaim.TotalCharge += chargeDifference;
                            paymentClaimServiceLine.PaymentClaim.TotalChargeOrig += chargeDifference;
                            _paymentClaimRepository.Update(paymentClaimServiceLine.PaymentClaim);
                        }
                    }
                    else
                    {
                        paymentClaimServiceLine.ChargeAmount = chargeEntryToUpdate.Charges;
                        paymentClaimServiceLine.ChargeAmountOrig = chargeEntryToUpdate.Charges;
                        _paymentClaimServiceLineRepository.Update(paymentClaimServiceLine);

                        paymentClaimServiceLine.PaymentClaim.TotalCharge += chargeDifference;
                        paymentClaimServiceLine.PaymentClaim.TotalChargeOrig += chargeDifference;
                        _paymentClaimRepository.Update(paymentClaimServiceLine.PaymentClaim);
                    }
                }
            }
        }

        public async Task<BillingCodeData> GetProviderBillingCodeWithoutAuth(AppointmentRethinkModel appointment)
        {
            var unitTypes = new List<ClientUnitTypes>();
            var providerBillingCode = await _rethinkService.GetProviderBillingCode(appointment.staffAccountInfoId, appointment.providerBillingCodeId.Value);
            providerBillingCode.funders = await _rethinkService.GetFunder(appointment.staffAccountInfoId, providerBillingCode.funderId);

            unitTypes = await _rethinkService.GetUnitTypesAsync();

            providerBillingCode.unitTypes = unitTypes.FirstOrDefault(x => x.id == providerBillingCode.unitTypeId);

            providerBillingCode.providerSerivces = await _rethinkService.GetProviderService(appointment.staffAccountInfoId, providerBillingCode.serviceId);

            var noAuthbillingCode = appointment.providerBillingCodeId.HasValue ? providerBillingCode :
                                    null;

            return noAuthbillingCode;
        }

        private async Task UpdateChargeEntityWithoutAuth(List<AppointmentRethinkModel> appointments, ClaimChargeEntryEntity chargeEntryToUpdate, bool firstCodeProcessing, bool chargeAdd)
        {
            int number = 1;
            decimal units = 0;
            decimal charges = 0;
            foreach (var appointment in appointments)
            {
                var appointmentHours =
                    (new TimeSpan((appointment.actualEndTime ?? appointment.endTime) / 60, (appointment.actualEndTime ?? appointment.endTime) % 60, 0)).TotalHours -
                    (new TimeSpan((appointment.actualStartTime ?? appointment.startTime) / 60,
                        (appointment.actualStartTime ?? appointment.startTime) % 60, 0)).TotalHours;

                appointment.ProviderBillingCode = await GetProviderBillingCodeWithoutAuth(appointment);

                if (appointment.providerBillingCodeId != null) { appointment.ProviderBillingCodeCredential = await _rethinkService.GetProviderBillingCodeCredential(appointment.staffAccountInfoId, appointment.providerBillingCodeId.Value, appointment.providerBillingCodeCredentialId); }

                var providerBillingCode = appointment.ProviderBillingCode;

                int? unit = firstCodeProcessing ? (providerBillingCode.unitTypes.unit ?? 0) : (providerBillingCode.unitTypes2?.unit);

                bool isUntimed = unit > 0 ? false : true;

                if (isUntimed == true)
                {
                    unit = 60;
                }

                int billingCodeRateTypeId = providerBillingCode.providerBillingCodeRateTypeId ?? 1;
                int roundingTypeId = firstCodeProcessing ? (providerBillingCode.providerBillingCodeRoundingTypeId ?? 1) : (providerBillingCode.providerBillingCodeRoundingTypeId2 ?? 1);

                decimal numberOfUnits = isUntimed == true
                    ? 1
                    : (decimal)(RoundCacluation(appointmentHours / ((unit.Value / 60.0) > 0 ? (unit.Value / 60.0) : 1),
                        (RoundingTypes)roundingTypeId));

                decimal unitRate = 0;
                decimal? baseRate = providerBillingCode.providerSerivces != null
                    ? providerBillingCode.providerSerivces.baseRate
                    : null;
                decimal? rate = firstCodeProcessing ? providerBillingCode.rate : providerBillingCode.rate2;

                if (providerBillingCode.restrictStaffProviderToService == true)
                {
                    decimal? contactRate = appointment.ProviderBillingCodeCredential != null
                        ? appointment.ProviderBillingCodeCredential.contractRate
                        : null;
                    unitRate = billingCodeRateTypeId == 2
                        ? (contactRate == null ? 0 : contactRate ?? 0)
                        : (baseRate ?? 0);
                }
                else
                {
                    unitRate = (rate ?? 0);
                }

                string billingCode2 = firstCodeProcessing ? providerBillingCode.billingCode2 : null;

                decimal billingCodeUnits = numberOfUnits;

                if (!string.IsNullOrEmpty(billingCode2))
                {
                    if (number == 1 || units < 1)
                    {
                        billingCodeUnits = 1;

                        if (number > 1 && units < 1)
                        {
                            billingCodeUnits = billingCodeUnits - units;
                        }

                        numberOfUnits -= billingCodeUnits;
                    }
                }

                units += billingCodeUnits;
                charges += billingCodeUnits * unitRate;

                number++;
            }
            if (chargeAdd)
            {
                chargeEntryToUpdate.Units += units;
                chargeEntryToUpdate.Charges += charges;
            }
            else
            {
                chargeEntryToUpdate.Units -= units;
                chargeEntryToUpdate.Charges -= charges;
            }

            _chargeEntryRepository.Update(chargeEntryToUpdate);
            transactionType = ClaimTransactionType.billedAmount;
            if (chargeEntryToUpdate.Units == 0)
            {
                SoftDelete(chargeEntryToUpdate, chargeEntryToUpdate.CreatedBy);
                _chargeEntryRepository.Update(chargeEntryToUpdate);
                transactionType = ClaimTransactionType.deleteCharge;
            }
        }

        private async Task<ClaimChargeEntryEntity> GetExistingClaimChargeEntity(AppointmentRethinkModel appointment, int claimId, int accountInfoId, string billingCode, DateTime startDate)
        {
            var billingCodeToUpdate = _chargeEntryRepository.Query().Where(c =>
                c.DateDeleted == null && c.ClaimId == claimId && c.BillingCode == billingCode);

            var providerBillingCode = appointment.ChildProfileAuthorizationBillingCode != null ? appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode : appointment.ProviderBillingCode;
            providerBillingCode.funders = await _rethinkService.GetFunder(accountInfoId, providerBillingCode.funderId);

            var chargeEntriesToUpdate = await billingCodeToUpdate.ToListAsync();

            var combineChargeType = providerBillingCode.funders.combineChargeTypeId;

            if (combineChargeType > 0)
            {
                if (combineChargeType == (int)CombineChargeTypes.DontCombine)
                {
                    return null;
                }
                else
                {
                    // code changes added by Chetan - To get same day charge as appointment date
                    chargeEntriesToUpdate = chargeEntriesToUpdate.Where(c => c.DateOfService == appointment.startDate).ToList();
                    if (!chargeEntriesToUpdate.Any()) return null;

                    var start = startDate.Date;
                    var end = start.AddDays(1);

                    string modifier1 = appointment.ProviderBillingCodeCredential?.modifier1;
                    string modifier2 = appointment.ProviderBillingCodeCredential?.modifier2;

                    if (!string.IsNullOrEmpty(modifier1))
                    {
                        modifier1 = modifier1.Trim().Substring(0, Math.Min(2, modifier1.Length));
                        chargeEntriesToUpdate = chargeEntriesToUpdate.Where(c => c.Modifier1 == modifier1).ToList();
                    }
                    if (!chargeEntriesToUpdate.Any()) return null;

                    if (!string.IsNullOrEmpty(modifier2))
                    {
                        modifier2 = modifier2.Trim().Substring(0, Math.Min(2, modifier2.Length));
                        chargeEntriesToUpdate = chargeEntriesToUpdate.Where(c => c.Modifier2 == modifier2).ToList();
                    }
                    if (!chargeEntriesToUpdate.Any()) return null;

                    chargeEntriesToUpdate = chargeEntriesToUpdate.Where(c => c.DateOfService >= start || c.DateOfService <= end).ToList();

                    if (combineChargeType == (int)CombineChargeTypes.SameDayClientProcedureRenderingProvider)
                    {
                        appointment.StaffMember = await _rethinkService.GetStaffMember(accountInfoId, appointment.staffId);
                        var npinumber = appointment.StaffMember.identifiers.FirstOrDefault(x => x.identifierType.ToLower() == "npinumber")?.value;
                        var npiNumbersSelected = await _linkChargeRepository.Query().Where(x => x.NpiNumber == npinumber).Select(x => x.ClaimChargeEntryEntityId).ToListAsync();
                        chargeEntriesToUpdate = chargeEntriesToUpdate.Where(c => npiNumbersSelected.Contains(c.Id)).ToList();
                    }
                }
            }

            return chargeEntriesToUpdate.FirstOrDefault();
        }

        private async Task<ClaimChargeEntryEntity> CreateClaimChargeEntity(AppointmentRethinkModel appointment, string billingCode, int claimId, int memberId, DateTime startDate, bool firstCodeProcessing, int providerBillingCodeId)
        {
            var diagnosisCodeWithoutAuth = string.Empty;
            if (appointment.ChildProfileAuthorizationBillingCode != null)
            {
                appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization = await _rethinkService.GetChildProfileAuthorizationByClientId(appointment.staffAccountInfoId, appointment.clientId.Value, appointment.ChildProfileAuthorizationBillingCode.childProfileAuthorizationId);
                appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization.ChildProfileDiagnosis = await _rethinkService.GetClientDiagnosisById(appointment.staffAccountInfoId, appointment.clientId ?? 0, appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization.childProfileDiagnosisId);
            }
            else
            {
                var claim = await _claimRepository.Query().Where(c => c.Id == claimId).FirstOrDefaultAsync();
                diagnosisCodeWithoutAuth = await _claimSyncService.AddDiagnosisCodes(claim, appointment.ChildProfileAuthorizationBillingCode, appointment.serviceId);
                if (appointment.ProviderBillingCodeCredential != null) appointment.ProviderBillingCodeCredential.ProviderBillingCode = appointment.ProviderBillingCode;
            }
            string diagnosisCode =
                appointment.ChildProfileAuthorizationBillingCode != null &&
                appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization != null &&
                appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization
                    .ChildProfileDiagnosis != null &&
                appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization
                    .ChildProfileDiagnosis.diagnosis != null
                    ? appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization
                        .ChildProfileDiagnosis.diagnosis.diagnosisCode
                    : diagnosisCodeWithoutAuth;

            string billingCodeDescription =
            appointment.ProviderBillingCodeCredential?.ProviderBillingCode?.description;
            var providerBillingCode = appointment.ChildProfileAuthorizationBillingCode != null ? appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode : appointment.ProviderBillingCode;

            string modifier1 = appointment.ProviderBillingCodeCredential?.modifier1;
            string modifier2 = appointment.ProviderBillingCodeCredential?.modifier2;
            string modifier3 = appointment.ProviderBillingCodeCredential?.modifier3;
            string modifier4 = appointment.ProviderBillingCodeCredential?.modifier4;

            int billingCodeRateTypeId = providerBillingCode.providerBillingCodeRateTypeId ?? 1;


            decimal unitRate = 0;
            decimal? baseRate = providerBillingCode.providerSerivces != null
                ? providerBillingCode.providerSerivces.baseRate
                : null;
            decimal? rate = firstCodeProcessing ? providerBillingCode.rate : providerBillingCode.rate2;

            if (providerBillingCode.restrictStaffProviderToService == true)
            {
                decimal? contactRate = appointment.ProviderBillingCodeCredential != null
                    ? appointment.ProviderBillingCodeCredential.contractRate
                    : null;
                unitRate = billingCodeRateTypeId == 2
                    ? (contactRate == null ? 0 : contactRate ?? 0)
                    : (baseRate ?? 0);
            }
            else
            {
                unitRate = (rate ?? 0);
            }

            ClaimChargeEntryEntity chargeEntry = new ClaimChargeEntryEntity();

            chargeEntry.BillingCode = billingCode;
            chargeEntry.BillingCodeId = providerBillingCodeId;
            chargeEntry.DiagnosisCode = diagnosisCode;
            chargeEntry.UnitTypeId = providerBillingCode.unitTypeId;
            chargeEntry.UnitRate = unitRate;
            chargeEntry.Modifier1 = !string.IsNullOrEmpty(modifier1) ? modifier1.Trim().Substring(0, Math.Min(2, modifier1.Length)) : null;
            chargeEntry.Modifier2 = !string.IsNullOrEmpty(modifier2) ? modifier2.Trim().Substring(0, Math.Min(2, modifier2.Length)) : null;
            chargeEntry.Modifier3 = !string.IsNullOrEmpty(modifier3) ? modifier3.Trim().Substring(0, Math.Min(2, modifier3.Length)) : null;
            chargeEntry.Modifier4 = !string.IsNullOrEmpty(modifier4) ? modifier4.Trim().Substring(0, Math.Min(2, modifier4.Length)) : null;
            chargeEntry.BillingCodeDescription = billingCodeDescription;
            chargeEntry.ClaimId = claimId;
            chargeEntry.DateOfService = appointment.startDate;

            chargeEntry.CreatedBy = memberId;
            chargeEntry.ModifiedBy = memberId;
            chargeEntry.DateCreated = EstDateTime;
            chargeEntry.DateLastModified = EstDateTime;

            chargeEntry.TypeId = (int)TransactionTypes.System;

            return chargeEntry;
        }

        private async Task<IQueryable<AppointmentRethinkModel>> AppointmentsQuery(List<int> appointmentsIds)
        {
            var appointmentList = await _rethinkService.GetAppointmentListAsync(appointmentsIds);
            foreach (var appointment in appointmentList)
            {
                appointment.ChildProfileAuthorizationBillingCode = await _rethinkService.GetChildProfileAuthBillingCodeForAppointment(appointment.staffAccountInfoId, appointment.clientId ?? 0, appointment.procedureCodeId);
                if (appointment.ChildProfileAuthorizationBillingCode != null)
                {
                    appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode = await _rethinkService.GetProviderBillingCode(appointment.staffAccountInfoId, appointment.providerBillingCodeId ?? 0);
                }
            }
            return appointmentList.AsQueryable();
        }
        private double RoundCacluation(double input, RoundingTypes roundingType)
        {
            switch (roundingType)
            {
                case RoundingTypes.RoundToNearestUnit:
                    // round down when .5 otherwise round up
                    // Math.Round works toward even numbers and instead using ceiling and accounting for .5
                    return Math.Ceiling(input - 0.5);
                case RoundingTypes.RoundUp:
                    return Math.Ceiling(input);
                case RoundingTypes.RoundDown:
                    return Math.Floor(input);
                case RoundingTypes.NoRounding:
                default:
                    return input;
            }
        }

        [Obsolete]
        public async Task<List<AppointmentRethinkModel>> SetupRethinkDataForAppointmentsOld(List<AppointmentRethinkModel> appointmentList)
        {
            await Parallel.ForEachAsync(appointmentList, new ParallelOptions { MaxDegreeOfParallelism = 10 },
                async (appointment, token) =>
                {
                    appointment.ProviderBillingCode = await _rethinkService.GetProviderBillingCode(appointment.clientAccountInfoId, appointment.providerBillingCodeId ?? 0);

                    appointment.ChildProfileAuthorizationBillingCode = await _rethinkService.GetChildProfileAuthBillingCodeForAppointment(appointment.clientAccountInfoId, appointment.clientId ?? 0, appointment.procedureCodeId);
                    if (appointment.ChildProfileAuthorizationBillingCode != null)
                    {
                        if (appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId != null)
                        {
                            appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode = await _rethinkService.GetProviderBillingCode(appointment.clientAccountInfoId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId ?? 0);
                        }
                    }

                    appointment.WorkFlowHistory = await _rethinkService.GetWorkFlowHistoyDetailsById(appointment.workflowHistoryId);
                    int? authorizationId = appointment.ChildProfileAuthorizationBillingCode?.childProfileAuthorizationId;
                    var authorization = appointment.clientId.HasValue  && authorizationId.HasValue ? await _rethinkServices.GetChildProfileAuthorizationByClientId(appointment.clientAccountInfoId, appointment.clientId.Value, authorizationId.Value) : null;
                    var isOverrideProvider = authorization?.renderingProviderStaffId;

                    appointment.StaffMember = await _rethinkService.GetStaffMember(appointment.clientAccountInfoId, appointment.staffId);
                    var staffId = isOverrideProvider ?? appointment.StaffMember?.memberId ?? 0;

                    if (staffId>0)
                    {            
                        appointment.StaffMember.Member = await _rethinkService.GetMemberAsync(appointment.clientAccountInfoId, staffId);
                        
                        if (appointment.StaffMember.Member is not null)
                            appointment.StaffMember.Member.AccountInfo = await _rethinkService.GetAccountReturningEntityAsync(appointment.StaffMember.Member.accountId);
                    }

                    appointment.Location = await _rethinkService.GetProviderLocation(appointment.clientAccountInfoId, appointment.toLocationId);
                    appointment.ProviderService = await _rethinkService.GetProviderService(appointment.clientAccountInfoId, appointment.providerServiceId);
                    var locationCodes = await _rethinkService.GetLocationCodes();
                    appointment.PlaceOfService = locationCodes.FirstOrDefault(x => x.id == appointment.locationId);
                    appointment.ProviderServiceLine = await _rethinkService.GetServiceLine(appointment.clientAccountInfoId, appointment.serviceId);
                });

            return appointmentList;
        }

        public async Task<List<AppointmentRethinkModel>> SetupRethinkDataForAppointments(List<AppointmentRethinkModel> appointmentList)
        {
            await Parallel.ForEachAsync(appointmentList.Where(a => a.clientAccountInfoId > 0), new ParallelOptions { MaxDegreeOfParallelism = 10 },
                async (appointment, token) =>
                {
                    try
                    {
                        // Phase 1: Fire all independent calls concurrently
                        var providerBillingCodeTask = _rethinkService.GetProviderBillingCode(appointment.clientAccountInfoId, appointment.providerBillingCodeId ?? 0);
                        var authBillingCodeTask = _rethinkService.GetChildProfileAuthBillingCodeForAppointment(appointment.clientAccountInfoId, appointment.clientId ?? 0, appointment.procedureCodeId);
                        var workFlowHistoryTask = _rethinkService.GetWorkFlowHistoyDetailsById(appointment.workflowHistoryId);
                        var staffMemberTask = _rethinkService.GetStaffMember(appointment.clientAccountInfoId, appointment.staffId);
                        var locationTask = _rethinkService.GetProviderLocation(appointment.clientAccountInfoId, appointment.toLocationId);
                        var providerServiceTask = _rethinkService.GetProviderService(appointment.clientAccountInfoId, appointment.providerServiceId);
                        var locationCodesTask = _rethinkService.GetLocationCodes();
                        var serviceLineTask = _rethinkService.GetServiceLine(appointment.clientAccountInfoId, appointment.serviceId);

                        await Task.WhenAll(providerBillingCodeTask, authBillingCodeTask, workFlowHistoryTask, staffMemberTask, locationTask, providerServiceTask, locationCodesTask, serviceLineTask);

                        appointment.ProviderBillingCode = await providerBillingCodeTask;
                        appointment.ChildProfileAuthorizationBillingCode = await authBillingCodeTask;
                        appointment.WorkFlowHistory = await workFlowHistoryTask;
                        appointment.StaffMember = await staffMemberTask;
                        appointment.Location = await locationTask;
                        appointment.ProviderService = await providerServiceTask;
                        var locationCodes = await locationCodesTask;
                        appointment.PlaceOfService = locationCodes.FirstOrDefault(x => x.id == appointment.locationId);
                        appointment.ProviderServiceLine = await serviceLineTask;

                        // Phase 2: Dependent calls that need phase 1 results, run concurrently where possible
                        var phase2Tasks = new List<Task>();

                        Task authorizationTask = Task.CompletedTask;
                        int? authorizationId = appointment.ChildProfileAuthorizationBillingCode?.childProfileAuthorizationId;
                        var hasAuthorization = appointment.clientId.HasValue && authorizationId.HasValue;

                        if (appointment.ChildProfileAuthorizationBillingCode is { providerBillingCodeId: not null })
                        {
                            var appointmentBillingCodeTask = _rethinkService.GetProviderBillingCode(appointment.clientAccountInfoId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId ?? 0);
                            phase2Tasks.Add(appointmentBillingCodeTask.ContinueWith(t =>
                                appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode = t.Result,
                                TaskContinuationOptions.OnlyOnRanToCompletion));
                        }

                        dynamic authorization = null;
                        if (hasAuthorization)
                        {
                            var authTask = _rethinkServices.GetChildProfileAuthorizationByClientId(appointment.clientAccountInfoId, appointment.clientId!.Value, authorizationId!.Value);
                            authorizationTask = authTask;
                            phase2Tasks.Add(authorizationTask);
                        }

                        if (phase2Tasks.Count > 0)
                            await Task.WhenAll(phase2Tasks);

                        var authResult = hasAuthorization ? await (dynamic)authorizationTask : null;
                        var isOverrideProvider = authResult?.renderingProviderStaffId;
                        var staffId = (int)(isOverrideProvider ?? appointment.StaffMember?.memberId ?? 0);

                        // Phase 3: Sequential chain — Member depends on staffId, AccountInfo depends on Member
                        if (staffId > 0)
                        {
                            appointment.StaffMember.Member = await _rethinkService.GetMemberAsync(appointment.clientAccountInfoId, staffId);

                            if (appointment.StaffMember.Member is not null)
                                appointment.StaffMember.Member.AccountInfo = await _rethinkService.GetAccountReturningEntityAsync(appointment.StaffMember.Member.accountId);
                        }
                    }
                    catch
                    {
                        // Depending on requirements, you might want to set a flag on the appointment to indicate partial failure
                    }
                });

            return appointmentList;
        }
    }
}