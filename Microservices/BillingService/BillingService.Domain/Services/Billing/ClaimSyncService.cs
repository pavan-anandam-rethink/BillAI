using BillingService.Domain.DataObjects.Billing;
using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Interfaces.BillingSettings;
using BillingService.Domain.Interfaces.Payment;
using BillingService.Domain.Models.BillingSettings;
using BillingService.Domain.Models.PaymentPosting;
using BillingService.Domain.Scheduling.Factory;
using BillingService.Domain.Scheduling.Mapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Rethink.Services.Common.Entities.Billing;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Feature;
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
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class ClaimSyncService : BaseService, IClaimSyncService
    {
        private readonly IRepository<BillingDbContext, ClaimChargeEntryEntity> _chargeEntryRepository;
        private readonly IRepository<BillingDbContext, ClaimEntity> _billingClaimRepository;
        private readonly IRepository<BillingDbContext, ClaimDiagnosisCodeEntity> _billingClaimDiagnosisCodeEntityRepository;
        private readonly IRepository<BillingDbContext, ClaimAppointmentLinkEntity> _linkRepository;
        private readonly IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry> _linkChargeRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimEntity> _paymentClaimRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineEntity> _paymentClaimServiceLineRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> _serviceLineAdjustmentRepository;
        private readonly IRepository<BillingDbContext, PaymentEntity> _paymentRepository;
        private readonly IRepository<BillingDbContext, AppointmentClaimProcessingErrorEntity> _appointmentClaimProcessingErrorRepository;
        private readonly IRepository<BillingDbContext, UnProcessedApointmentScheduleEntity> _unProcessedApointmentScheduleRepository;
        private readonly IRepository<BillingDbContext, FunderSettingsEntity> _funderSettingRepo;

        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        private readonly IClaimHistoryService _claimHistoryService;
        private readonly IClaimManagerService _claimManagerService;
        private readonly IClaimValidationService _claimValidationService;
        private readonly IPaymentPostingService _paymentPostingService;
        private readonly IMessageBus _bus;
        private bool existingClaimToUpdate = false;
        private readonly IClaimUpdateService _claimUpdateService;
        private readonly IChargeEntryService _chargeEntryService;
        private readonly IBillingSettingsService _billingSettingsService;
        private readonly IConfiguration _configuration;
        private readonly IRepository<BillingDbContext, TimezonesEntity> _timezonesEntity;

        private const string featureShowBilling = "ShowBilling";
        private const string featureBillingOptionId = "BillingOptionId";
        private const string valRethink = "Rethink";

        public ClaimSyncService(IRepository<BillingDbContext, ClaimAppointmentLinkEntity> linkRepository,
            IRepository<BillingDbContext, ClaimEntity> billingClaimRepository,
            IRepository<BillingDbContext, ClaimDiagnosisCodeEntity> billingClaimDiagnosisCodeEntityRepository,
            IRepository<BillingDbContext, ClaimChargeEntryEntity> chargeEntryRepository,
            IClaimHistoryService claimHistoryService,
            IClaimManagerService claimManagerService,
            IClaimValidationService claimValidationService,
            IMessageBus bus,
            IRethinkMasterDataMicroServices rethinkServices,
            IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry> linkChargeRepository,
            IRepository<BillingDbContext, PaymentClaimEntity> paymentClaimRepository,
            IRepository<BillingDbContext, PaymentClaimServiceLineEntity> paymentClaimServiceLineRepository,
            IClaimUpdateService claimUpdateService,
            IChargeEntryService chargeEntryService,
            IRepository<BillingDbContext, PaymentClaimServiceLineAdjustmentEntity> serviceLineAdjustmentRepository,
            IPaymentPostingService paymentPostingService,
            IRepository<BillingDbContext, PaymentEntity> paymentRepository,
            IRepository<BillingDbContext, AppointmentClaimProcessingErrorEntity> appointmentClaimProcessingErrorRepository,
            IRepository<BillingDbContext, UnProcessedApointmentScheduleEntity> unProcessedApointmentScheduleRepository,
            IConfiguration configuration,
            IBillingSettingsService billingSettingsService,
            IRepository<BillingDbContext, TimezonesEntity> timezonesEntity,
            IRepository<BillingDbContext, FunderSettingsEntity> funderSettingRepo
            )
        {
            _rethinkServices = rethinkServices;
            _linkRepository = linkRepository;
            _billingClaimRepository = billingClaimRepository;
            _billingClaimDiagnosisCodeEntityRepository = billingClaimDiagnosisCodeEntityRepository;
            _chargeEntryRepository = chargeEntryRepository;
            _claimHistoryService = claimHistoryService;
            _claimManagerService = claimManagerService;
            _claimValidationService = claimValidationService;
            _bus = bus;
            _linkChargeRepository = linkChargeRepository;
            _paymentClaimRepository = paymentClaimRepository;
            _paymentClaimServiceLineRepository = paymentClaimServiceLineRepository;
            _claimUpdateService = claimUpdateService;
            _chargeEntryService = chargeEntryService;
            _serviceLineAdjustmentRepository = serviceLineAdjustmentRepository;
            _paymentPostingService = paymentPostingService;
            _paymentRepository = paymentRepository;
            _appointmentClaimProcessingErrorRepository = appointmentClaimProcessingErrorRepository;
            _unProcessedApointmentScheduleRepository = unProcessedApointmentScheduleRepository;
            _configuration = configuration;
            _billingSettingsService = billingSettingsService;
            _timezonesEntity = timezonesEntity;
            _funderSettingRepo = funderSettingRepo;
        }

        public async Task SyncClaimAsync(int appointmentId, int accountInfoId, bool processingSchedule = false)
        {
            var link = new ClaimAppointmentLinkEntity();
            try
            {
                //// Check the account date for the BillingOption is on and Using Rethink as a billing solution
                var accountInfo = await _rethinkServices.GetAccountReturningEntityAsync(accountInfoId, false);
                if(accountInfo == null) {
                    return;
                }

                var features = accountInfo.subscriptionOptions;

                var showBilling = features?.Any(x => x.type == featureShowBilling && (bool)x.value) ?? false;
                var billingOption = features?.Any(x => x.type == featureBillingOptionId && (string)x.value == valRethink) ?? false;

                if (!showBilling || !billingOption)
                    return;


                // Get the appointment details from Rethink
                var appointment = await _rethinkServices.GetAppointmentAsync(appointmentId);

                // First save the data in the link table if not exists
                var newLink = new ClaimAppointmentLinkEntity();
                link = await _linkRepository.Query().FirstOrDefaultAsync(l => l.AppointmentId == appointmentId && l.ClaimId != 0 && !l.DateDeleted.HasValue);
                if (link == null)
                {
                    newLink.AppointmentId = appointmentId;
                    // set AccountInfoId
                    newLink.AccountInfoId = accountInfoId;

                    MarkCreated(newLink, appointment?.StaffMember?.memberId ?? accountInfoId);
                    await _linkRepository.AddAsync(newLink);
                    await _linkRepository.SaveChangesAsync();
                }

                if (appointment == null)
                {
                    const string errorMessage = "Unable to retrieve the Appointment information at the moment. Please try again later.";
                    await LogAppointmentProcessionError(appointmentId, accountInfoId, errorMessage);
                    return;
                }


                appointment.StaffMember = await _rethinkServices.GetStaffMember(appointment.staffAccountInfoId, appointment.staffId);
                appointment.ChildProfileAuthorizationBillingCode = await _rethinkServices.GetChildProfileAuthBillingCodeForAppointment(appointment.staffAccountInfoId, appointment.clientId.Value, appointment.procedureCodeId);
                if (appointment.providerBillingCodeId != null && appointment.ChildProfileAuthorizationBillingCode != null)
                {
                    appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode = await _rethinkServices.GetProviderBillingCode(appointment.StaffMember.accountId, appointment.providerBillingCodeId ?? 0);
                    appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization = await _rethinkServices.GetChildProfileAuthorizationById(appointment.staffAccountInfoId, appointment.ChildProfileAuthorizationBillingCode.childProfileAuthorizationId);
                }

                if (appointment == null || !IsAppointmentVerified(appointment))
                {
                    return;
                }

                // Schedule-aware holding behavior: if payer is configured for non-immediate mode, hold in unbilled and exit
                var funder = await _billingSettingsService.GetBillingFunderIdsSettingAsync(appointment.funderId, accountInfoId);
                if (!processingSchedule && funder != null && funder.ScheduleType != 1)
                {
                    await HandleNonImmediateClaimCreationAsync(appointment, accountInfoId, appointmentId, funder);
                    return;
                }

                if (appointment.appointmentTypeId == 1
                    && appointment.occurrenceTypeId == 1)
                {
                    ClaimEntity claimToUpdate = null;
                    AppointmentClientAuthBillingCodeModel childProfileAuthorizationBillingCode = null;

                    var appointmentHasAuthorization = appointment.ChildProfileAuthorizationBillingCode != null;
                    var funderMappingsMicro = await _rethinkServices.GetChildProfileFunderMappings(accountInfoId, appointment.clientId.Value);
                    if (funderMappingsMicro == null || funderMappingsMicro.data?.Count == 0)
                    {
                        await LogAppointmentProcessionError(appointmentId, appointment.modifiedBy.GetValueOrDefault(), "Missing Funder Mappings Micro");
                        return;
                    }
                    var funderMappingsMicroId = funderMappingsMicro.data.FirstOrDefault(x => x.funderId == appointment.funderId);
                    var serviceLineMappings = await _rethinkServices.GetServiceLineMappingsByFunderId(accountInfoId, appointment.clientId.Value, funderMappingsMicroId.id);
                    if (serviceLineMappings == null || serviceLineMappings.Count == 0)
                    {
                        await LogAppointmentProcessionError(appointmentId, appointment.modifiedBy.GetValueOrDefault(), "Service Line Mapping Not Found");
                        return;
                    }
                    var serviceLineMappingsId = serviceLineMappings.FirstOrDefault(x => x.serviceId == appointment.serviceId);
                    var clientFunderServiceLine = await _rethinkServices.GetChildProfileFunderServiceLineMappingEntity(accountInfoId, appointment.clientId.Value, funderMappingsMicroId.id, serviceLineMappingsId.id);
                    if (clientFunderServiceLine == null || clientFunderServiceLine?.ChildProfileFunderMapping == null)
                    {
                        await LogAppointmentProcessionError(appointmentId, appointment.modifiedBy.GetValueOrDefault(), "Client Funder Service Line Not Found");
                        return;
                    }

                    clientFunderServiceLine.ChildProfileFunderMapping.Funder = await _rethinkServices.GetFunder(accountInfoId, funderMappingsMicroId.funderId);
                    clientFunderServiceLine.ChildProfileFunderMapping.Funder.ServiceFunders = await _rethinkServices.GetServiceFundersEntityListByFunderId(appointment.clientAccountInfoId, appointment.clientId ?? 0, funderMappingsMicroId.funderId);

                    if (link == null)
                    {
                        var facilityId = new ProviderLocationModel();
                        if (appointmentHasAuthorization)
                        {
                            childProfileAuthorizationBillingCode = await _rethinkServices.GetChildProfileAuthBillingCodeForAppointment(accountInfoId, appointment.clientId ?? 0, appointment.procedureCodeId);
                            childProfileAuthorizationBillingCode.AppointmentProviderBillingCode = await _rethinkServices.GetProviderBillingCode(appointment.StaffMember.accountId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId ?? 0);
                            childProfileAuthorizationBillingCode.ChildProfileAuthorization = await _rethinkServices.GetChildProfileAuthorizationByClientId(appointment.staffAccountInfoId, appointment.clientId ?? 0, appointment.ChildProfileAuthorizationBillingCode.childProfileAuthorizationId);

                            if (childProfileAuthorizationBillingCode == null || childProfileAuthorizationBillingCode.ChildProfileAuthorization == null)
                            {
                                await LogAppointmentProcessionError(appointmentId, appointment.modifiedBy.GetValueOrDefault(), "Authorization missing for childProfile.");
                                return;
                            }
                            childProfileAuthorizationBillingCode.ChildProfileAuthorization.ChildProfileAuthorizationDiagnosisCodes = await _rethinkServices.GetChildProfileAuthorizationDiagnosisCodesAsync(appointment.staffAccountInfoId, appointment.clientId.Value, childProfileAuthorizationBillingCode.ChildProfileAuthorization.childProfileDiagnosisId, childProfileAuthorizationBillingCode.ChildProfileAuthorization.id);
                            childProfileAuthorizationBillingCode.ChildProfileAuthorization.Funder = await _rethinkServices.GetFunder(appointment.staffAccountInfoId, appointment.funderId);
                            appointment.ChildProfileAuthorizationBillingCode = childProfileAuthorizationBillingCode;
                            facilityId = await _rethinkServices.GetChildProfileFacility(appointment.staffAccountInfoId, appointment.clientId ?? 0);
                        }

                        var providerBillingCode = await GetProviderBillingCode(appointment, childProfileAuthorizationBillingCode);
                        int? providerMemberId = GetProviderMemberId(appointment, childProfileAuthorizationBillingCode);
                        if (providerBillingCode != null)
                        {
                            var funderSetting = await _funderSettingRepo.Query().FirstOrDefaultAsync(x => x.FunderId == providerBillingCode.funderId
                                && x.AccountInfoId == appointment.clientAccountInfoId && x.DateDeleted == null);

                            if (funderSetting?.CombineChargesForSameClient == true || providerBillingCode.funders.combineChargeTypeId != (int)CombineChargeTypes.DontCombine)
                            {
                                var claimData = await GetExistingClaim(appointment, providerBillingCode, childProfileAuthorizationBillingCode, providerMemberId, funderSetting?.CombineChargesForSameClient ?? false, facilityId.providerLocationId);
                                var isManual = claimData?.ClaimHistory.Any(x => x.ClaimHistoryAction == ClaimHistoryAction.ClaimCreated && x.Mode == ClaimActionMode.User);
                                if (!isManual ?? false)
                                {
                                    claimToUpdate = claimData;
                                }
                            }

                            if (claimToUpdate == null || !(claimToUpdate.ClaimStatus == ClaimStatus.PendingReview || claimToUpdate.ClaimStatus == ClaimStatus.ReadyToBill))
                            {
                                var memberId = providerMemberId ?? appointment.StaffMember.memberId;
                                var childProfileId = appointment.clientId.Value;
                                var primaryFunderId = appointment.funderId;
                                var startDate = appointment.startDateTime.Date;
                                var endDate = appointment.endDateTime.GetValueOrDefault(appointment.startDateTime).Date;

                                var claim = await _claimManagerService.InitializeClaim(memberId, accountInfoId, childProfileId, primaryFunderId, startDate, endDate);
                                await PopulateClaimEntry(claim, appointment, providerBillingCode, childProfileAuthorizationBillingCode, clientFunderServiceLine, facilityId.providerLocationId);

                                await _billingClaimRepository.CommitAsync();
                                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                                {
                                    ClaimId = claim.Id,
                                    MemberId = memberId,
                                    Mode = ClaimActionMode.System,
                                    ClaimAction = ClaimAction.Create,
                                    ClaimHistoryAction = ClaimHistoryAction.ClaimCreated,
                                }, true);
                                claimToUpdate = claim;
                            }
                            else
                            {
                                if (claimToUpdate.ClaimStatus == ClaimStatus.ReadyToBill)
                                {
                                    //claimToUpdate.IsFlagged = true;
                                    claimToUpdate.IsAppointmentDeleted = false;

                                    var funderMappings = await _rethinkServices.GetChildProfileFunderMappingByMappingId(appointment.clientAccountInfoId, appointment.clientId ?? 0, claimToUpdate.ClientFunderId ?? 0);
                                    //var funderMappings = await _bhChildProfileFunderMappingRepository.Query()
                                    //                  .FirstOrDefaultAsync(x => x.ChildProfileId == claimToUpdate.Id
                                    //  && x.DateDeleted == null
                                    //  && x.FunderId == claimToUpdate.LastBilledFunderId);
                                    claimToUpdate.ReleaseOfInformationConfirmationTypeId = funderMappings.releaseOfInformationConfirmationTypeId;
                                    claimToUpdate.AuthorizedPaymentConfirmationTypeId = funderMappings.authorizedPaymentConfirmationTypeId;
                                    claimToUpdate.BenefitAssignmentId = (funderMappings.isAutismCoveredBenefit == true || funderMappings.isAutismCoveredBenefit == null) ? 1 : 2;

                                    if (!claimToUpdate.IsSecondaryPayerAvailable)
                                    {
                                        var secondaryFunderDetails = await _claimUpdateService.CheckAndGetSecondaryFunderDetails(claimToUpdate.AccountInfoId, claimToUpdate);
                                        if (secondaryFunderDetails != null && secondaryFunderDetails.funders.Any())
                                        {
                                            claimToUpdate.IsSecondaryPayerAvailable = true;
                                        }
                                    }

                                    await UpdateClaim(claimToUpdate, appointment.modifiedBy.GetValueOrDefault());
                                    await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                                    {
                                        ClaimId = claimToUpdate.Id,
                                        MemberId = appointment.modifiedBy.GetValueOrDefault(),
                                        Mode = ClaimActionMode.System,
                                        ClaimAction = ClaimAction.Edit,
                                        ClaimHistoryAction = ClaimHistoryAction.Flagged,
                                    });
                                }
                                existingClaimToUpdate = true;
                            }
                        }
                        var claimDiagnosisCode = await AddDiagnosisCodes(claimToUpdate, childProfileAuthorizationBillingCode, appointment.serviceId);

                        newLink.ClaimId = claimToUpdate.Id;
                        _linkRepository.Update(newLink);

                        await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                        {
                            ClaimId = newLink.ClaimId,
                            MemberId = claimToUpdate.CreatedBy,
                            Mode = ClaimActionMode.System,
                            ClaimAction = ClaimAction.Create,
                            ClaimHistoryAction = ClaimHistoryAction.AppointmentLinkCreated,
                            NewValue = $"{newLink.AppointmentId}",
                        });

                        var appointmentHours = new TimeSpan((appointment.actualEndTime ?? appointment.endTime) / 60, (appointment.actualEndTime ?? appointment.endTime) % 60, 0).TotalHours - new TimeSpan((appointment.actualStartTime ?? appointment.startTime) / 60, (appointment.actualStartTime ?? appointment.startTime) % 60, 0).TotalHours;

                        int unit = providerBillingCode.unitTypes.unit ?? 0;

                        bool isUntimed = unit <= 0;

                        if (isUntimed)
                        {
                            unit = 60;
                        }

                        int billingCodeRateTypeId = providerBillingCode.providerBillingCodeRateTypeId ?? 1;
                        int roundingTypeId = providerBillingCode.providerBillingCodeRoundingTypeId ?? 1;

                        decimal numberOfUnits = isUntimed == true ? 1 : (decimal)RoundCacluation(appointmentHours / (unit / 60.0 > 0 ? unit / 60.0 : 1), (RoundingTypes)roundingTypeId);

                        decimal unitRate = 0;
                        decimal? baseRate = providerBillingCode.providerSerivces != null ? providerBillingCode.providerSerivces.baseRate : null;
                        decimal? rate = providerBillingCode.rate;

                        var hcProviderBillingCodeCredential = new ProviderBillingCodeCredentialModel();

                        if (appointment.providerBillingCodeId != null)
                        {
                            hcProviderBillingCodeCredential = appointment?.ProviderBillingCodeCredential ??
                                    await _rethinkServices.GetProviderBillingCodeCredential(appointment.StaffMember.accountId, appointment.providerBillingCodeId ?? 0, appointment.providerBillingCodeCredentialId);
                        }
                        else if (appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId != null)
                        {
                            hcProviderBillingCodeCredential = appointment?.ProviderBillingCodeCredential ??
                                    await _rethinkServices.GetProviderBillingCodeCredential(appointment.StaffMember.accountId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId ?? 0, appointment.providerBillingCodeCredentialId);
                        }


                        if (providerBillingCode.restrictStaffProviderToService == true)
                        {
                            decimal? contactRate = hcProviderBillingCodeCredential != null ? hcProviderBillingCodeCredential.contractRate : null;
                            unitRate = billingCodeRateTypeId == 2 ? contactRate == null ? 0 : contactRate ?? 0 : baseRate ?? 0;
                        }
                        else
                        {
                            unitRate = rate ?? 0;
                        }

                        string billingCode = providerBillingCode.billingCode;
                        string billingCode2 = providerBillingCode.billingCode2;
                        string diagnosisCode = claimDiagnosisCode;

                        string modifier = hcProviderBillingCodeCredential != null ? hcProviderBillingCodeCredential.modifier1 : null;
                        modifier = !string.IsNullOrEmpty(modifier) ? modifier.Trim().Substring(0, Math.Min(2, modifier.Length)) : null;
                        string modifier2 = hcProviderBillingCodeCredential != null ? hcProviderBillingCodeCredential.modifier2 : null;
                        modifier2 = !string.IsNullOrEmpty(modifier2) ? modifier2.Trim().Substring(0, Math.Min(2, modifier2.Length)) : null;
                        string modifier3 = hcProviderBillingCodeCredential != null ? hcProviderBillingCodeCredential.modifier3 : null;
                        modifier3 = !string.IsNullOrEmpty(modifier3) ? modifier3.Trim().Substring(0, Math.Min(2, modifier3.Length)) : null;
                        string modifier4 = hcProviderBillingCodeCredential != null ? hcProviderBillingCodeCredential.modifier4 : null;
                        modifier4 = !string.IsNullOrEmpty(modifier4) ? modifier4.Trim().Substring(0, Math.Min(2, modifier4.Length)) : null;
                        // charge entry merge
                        ClaimChargeEntryEntity chargeEntryToUpdate = null;
                        var billingCodeToUpdate = _chargeEntryRepository.Query()
                            .Where(c => c.DateDeleted == null && c.ClaimId == claimToUpdate.Id && c.BillingCode == billingCode);

                        if (funder?.CombineChargesForSameClient is true)
                        {
                            providerBillingCode.funders.combineChargeTypeId = (int)CombineChargeTypes.SameClient;
                        }
                        if (!string.IsNullOrEmpty(modifier))
                        {
                            billingCodeToUpdate = billingCodeToUpdate.Where(c => c.Modifier1 == modifier);
                        }
                        if (providerBillingCode.funders.combineChargeTypeId == (int)CombineChargeTypes.SameClient)
                        {
                            var staffId = childProfileAuthorizationBillingCode.ChildProfileAuthorization.renderingProviderStaffId;
                            chargeEntryToUpdate = billingCodeToUpdate.FirstOrDefault(x => x.RenderingProviderId == staffId);
                        }
                        else
                        {
                            chargeEntryToUpdate = billingCodeToUpdate.FirstOrDefault();
                        }

                        decimal billingCodeUnits = numberOfUnits;

                        if (!string.IsNullOrEmpty(billingCode2))
                        {
                            if (chargeEntryToUpdate == null || chargeEntryToUpdate.Units < 1)
                            {
                                billingCodeUnits = 1;

                                if (chargeEntryToUpdate != null && chargeEntryToUpdate.Units < 1)
                                {
                                    billingCodeUnits = billingCodeUnits - chargeEntryToUpdate.Units;
                                }

                                numberOfUnits -= billingCodeUnits;
                            }
                        }

                        if (chargeEntryToUpdate == null)
                        {
                            ClaimChargeEntryEntity chargeEntry = new ClaimChargeEntryEntity();

                            chargeEntry.BillingCode = billingCode;
                            chargeEntry.DiagnosisCode = diagnosisCode;
                            chargeEntry.Units = billingCodeUnits;
                            chargeEntry.UnitRate = unitRate;
                            chargeEntry.Charges = billingCodeUnits * unitRate;
                            chargeEntry.UnitTypeId = providerBillingCode.unitTypeId;
                            chargeEntry.Modifier1 = !string.IsNullOrEmpty(modifier) ? modifier.Trim().Substring(0, Math.Min(2, modifier.Length)) : null;
                            chargeEntry.Modifier2 = !string.IsNullOrEmpty(modifier2) ? modifier2.Trim().Substring(0, Math.Min(2, modifier2.Length)) : null;
                            chargeEntry.Modifier3 = !string.IsNullOrEmpty(modifier3) ? modifier3.Trim().Substring(0, Math.Min(2, modifier3.Length)) : null;
                            chargeEntry.Modifier4 = !string.IsNullOrEmpty(modifier4) ? modifier4.Trim().Substring(0, Math.Min(2, modifier4.Length)) : null;
                            chargeEntry.ClaimId = claimToUpdate.Id;
                            chargeEntry.DateOfService = claimToUpdate.StartDate;
                            chargeEntry.BillingCodeId = providerBillingCode.id;
                            chargeEntry.RenderingProviderId = providerBillingCode.funders.combineChargeTypeId == (int)CombineChargeTypes.SameClient
                                ? childProfileAuthorizationBillingCode.ChildProfileAuthorization.renderingProviderStaffId.GetValueOrDefault()
                                : claimToUpdate.RenderingStaffMemberId ?? 0;

                            MarkCreated(chargeEntry, claimToUpdate.CreatedBy);
                            await _chargeEntryRepository.AddAsync(chargeEntry);
                            await _chargeEntryRepository.SaveChangesAsync();


                            var claimAppointmentLinkChargeEntry = new ClaimAppointmentLinkChargeEntry
                            {
                                NpiNumber = appointment.StaffMember.identifiers.FirstOrDefault(x => x.identifierType.ToLower() == "npinumber")?.value,
                                ClaimChargeEntryEntityId = chargeEntry.Id,
                                IsSecondBillingCode = false
                            };
                            MarkCreated(claimAppointmentLinkChargeEntry, claimToUpdate.CreatedBy);
                            _linkChargeRepository.Add(claimAppointmentLinkChargeEntry);
                            await _linkChargeRepository.CommitAsync();

                            newLink = await _linkRepository.Query().Where(x => x.ClaimId == claimToUpdate.Id && x.ClaimChargeEntriesId == null).FirstOrDefaultAsync();
                            newLink.ClaimChargeEntriesId = chargeEntry.Id;
                            newLink.ClaimAppointmentLinkChargeEntryId = claimAppointmentLinkChargeEntry.Id;
                            _linkRepository.Update(newLink);

                            await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                            {
                                ClaimId = chargeEntry.ClaimId,
                                MemberId = claimToUpdate.CreatedBy,
                                Mode = ClaimActionMode.System,
                                ClaimAction = ClaimAction.Create,
                                ClaimHistoryAction = ClaimHistoryAction.ChargeEntryCreated,
                                NewValue = $"{chargeEntry.Id}",
                            });
                        }
                        else if (string.IsNullOrEmpty(billingCode2) || chargeEntryToUpdate.Units < 1)
                        {
                            var previousCharge = chargeEntryToUpdate.Charges;
                            chargeEntryToUpdate.Units += billingCodeUnits;
                            chargeEntryToUpdate.Charges += billingCodeUnits * unitRate;
                            var chargeDifference = chargeEntryToUpdate.Charges - previousCharge;

                            MarkUpdated(chargeEntryToUpdate, claimToUpdate.ModifiedBy.GetValueOrDefault());
                            _chargeEntryRepository.Update(chargeEntryToUpdate);
                            await _chargeEntryRepository.CommitAsync();

                            var claimAppointmentLinkChargeEntry = new ClaimAppointmentLinkChargeEntry
                            {
                                NpiNumber = appointment.StaffMember.identifiers.FirstOrDefault(x => x.identifierType.ToLower() == "npinumber")?.value,
                                ClaimChargeEntryEntityId = chargeEntryToUpdate.Id,
                                IsSecondBillingCode = false
                            };
                            MarkCreated(claimAppointmentLinkChargeEntry, claimToUpdate.CreatedBy);
                            _linkChargeRepository.Add(claimAppointmentLinkChargeEntry);
                            await _linkChargeRepository.CommitAsync();

                            newLink = await _linkRepository.Query().Where(x => x.ClaimId == claimToUpdate.Id && x.ClaimChargeEntriesId == null).FirstOrDefaultAsync();
                            newLink.ClaimChargeEntriesId = chargeEntryToUpdate.Id;
                            newLink.ClaimAppointmentLinkChargeEntryId = claimAppointmentLinkChargeEntry.Id;
                            _linkRepository.Update(newLink);

                            // UPDATE THE PAYMENT CLAIMS AND SERVICE LINES 
                            var paymentClaimServiceLinesToUpdate = await _paymentClaimServiceLineRepository
                                .Query()
                                .Where(x => x.ClaimChargeEntryId == chargeEntryToUpdate.Id && x.DateDeleted == null)
                                .Include(c => c.PaymentClaim)
                                .ToListAsync();

                            if (paymentClaimServiceLinesToUpdate.Any())
                            {
                                foreach (var paymentClaimServiceLine in paymentClaimServiceLinesToUpdate)
                                {
                                    paymentClaimServiceLine.ChargeAmount = chargeEntryToUpdate.Charges;
                                    paymentClaimServiceLine.ChargeAmountOrig = chargeEntryToUpdate.Charges;
                                    _paymentClaimServiceLineRepository.Update(paymentClaimServiceLine);

                                    paymentClaimServiceLine.PaymentClaim.TotalCharge += chargeDifference;
                                    paymentClaimServiceLine.PaymentClaim.TotalChargeOrig += chargeDifference;
                                    _paymentClaimRepository.Update(paymentClaimServiceLine.PaymentClaim);
                                }
                            }

                            await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                            {
                                ClaimId = chargeEntryToUpdate.ClaimId,
                                MemberId = claimToUpdate.ModifiedBy.GetValueOrDefault(),
                                Mode = ClaimActionMode.System,
                                ClaimAction = ClaimAction.Edit,
                                ClaimHistoryAction = ClaimHistoryAction.ChargeEntryUpdated,
                                NewValue = $"{chargeEntryToUpdate.Id}",
                            });
                        }

                        if (!string.IsNullOrEmpty(billingCode2) && numberOfUnits > 0)
                        {
                            int? unit2 = providerBillingCode.unitTypes != null ? providerBillingCode.unitTypes.unit : null;
                            int roundingTypeId2 = providerBillingCode.providerBillingCodeRoundingTypeId2 ?? 1;

                            bool isUntimed2 = !(unit2 > 0);

                            if (isUntimed2)
                            {
                                unit2 = 60;
                            }

                            var numberOfUnits2 = isUntimed2 == true
                                ? 1
                                : unit2 != null
                                    ? (decimal)RoundCacluation(appointmentHours / (unit2.Value / 60.0 > 0 ? unit2.Value / 60.0 : 1), (RoundingTypes)roundingTypeId2)
                                    : 0;
                            if (isUntimed2 != true && numberOfUnits2 > 1)
                            {
                                numberOfUnits2 -= 1;
                            }

                            decimal unitRate2 = 0;
                            decimal? rate2 = providerBillingCode.rate2;

                            if (providerBillingCode.restrictStaffProviderToService == true)
                            {
                                decimal? contactRate = hcProviderBillingCodeCredential != null ? hcProviderBillingCodeCredential.contractRate : null;
                                unitRate2 = billingCodeRateTypeId == 2 ? contactRate == null ? 0 : contactRate ?? 0 : baseRate ?? 0;
                            }
                            else
                            {
                                unitRate2 = rate2 ?? 0;
                            }

                            var billingCode2ToUpdate = _chargeEntryRepository.Query()
                                .Where(c => c.DateDeleted == null && c.ClaimId == claimToUpdate.Id && c.BillingCode == billingCode2);

                            if (!string.IsNullOrEmpty(modifier))
                            {
                                billingCode2ToUpdate = billingCode2ToUpdate.Where(c => c.Modifier1 == modifier);
                            }

                            chargeEntryToUpdate = billingCode2ToUpdate.FirstOrDefault();

                            if (chargeEntryToUpdate == null)
                            {
                                ClaimChargeEntryEntity chargeEntry = new ClaimChargeEntryEntity();
                                chargeEntry.BillingCode = billingCode2;
                                chargeEntry.DiagnosisCode = diagnosisCode;

                                chargeEntry.Units = numberOfUnits2;
                                chargeEntry.UnitRate = unitRate2;
                                chargeEntry.UnitTypeId = providerBillingCode.unitTypeId2 ?? 1;
                                chargeEntry.Charges = numberOfUnits2 * unitRate2;
                                chargeEntry.Modifier1 = !string.IsNullOrEmpty(modifier) ? modifier.Trim().Substring(0, Math.Min(2, modifier.Length)) : null;
                                chargeEntry.Modifier2 = !string.IsNullOrEmpty(modifier2) ? modifier2.Trim().Substring(0, Math.Min(2, modifier2.Length)) : null;
                                chargeEntry.Modifier3 = !string.IsNullOrEmpty(modifier3) ? modifier3.Trim().Substring(0, Math.Min(2, modifier3.Length)) : null;
                                chargeEntry.Modifier4 = !string.IsNullOrEmpty(modifier4) ? modifier4.Trim().Substring(0, Math.Min(2, modifier4.Length)) : null;

                                chargeEntry.ClaimId = claimToUpdate.Id;
                                chargeEntry.DateOfService = claimToUpdate.StartDate;
                                chargeEntry.BillingCodeId = providerBillingCode.id;
                                chargeEntry.RenderingProviderId = claimToUpdate.RenderingStaffMemberId ?? 0;

                                MarkCreated(chargeEntry, claimToUpdate.CreatedBy);
                                await _chargeEntryRepository.AddAsync(chargeEntry);
                                await _chargeEntryRepository.SaveChangesAsync();


                                var claimAppointmentLinkChargeEntry = new ClaimAppointmentLinkChargeEntry
                                {
                                    NpiNumber = appointment.StaffMember.identifiers.FirstOrDefault(x => x.identifierType.ToLower() == "npinumber")?.value,
                                    ClaimChargeEntryEntityId = chargeEntry.Id,
                                    IsSecondBillingCode = false
                                };
                                MarkCreated(claimAppointmentLinkChargeEntry, claimToUpdate.CreatedBy);
                                _linkChargeRepository.Add(claimAppointmentLinkChargeEntry);
                                await _linkChargeRepository.CommitAsync();

                                newLink = await _linkRepository.Query().Where(x => x.ClaimId == claimToUpdate.Id && x.ClaimChargeEntriesId == null).FirstOrDefaultAsync();
                                if (newLink != null)
                                {
                                    newLink.ClaimChargeEntriesId = chargeEntry.Id;
                                    newLink.ClaimAppointmentLinkChargeEntryId = claimAppointmentLinkChargeEntry.Id;
                                    _linkRepository.Update(newLink);
                                }

                                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                                {
                                    ClaimId = chargeEntry.ClaimId,
                                    MemberId = claimToUpdate.CreatedBy,
                                    Mode = ClaimActionMode.System,
                                    ClaimAction = ClaimAction.Create,
                                    ClaimHistoryAction = ClaimHistoryAction.ChargeEntryCreated,
                                    NewValue = $"{chargeEntry.Id}",
                                });
                            }
                            else
                            {
                                chargeEntryToUpdate.Units += numberOfUnits;
                                chargeEntryToUpdate.Charges += numberOfUnits * unitRate2;

                                MarkUpdated(chargeEntryToUpdate, claimToUpdate.ModifiedBy.GetValueOrDefault());
                                _chargeEntryRepository.Update(chargeEntryToUpdate);
                                await _chargeEntryRepository.CommitAsync();

                                var claimAppointmentLinkChargeEntry = new ClaimAppointmentLinkChargeEntry
                                {
                                    NpiNumber = appointment.StaffMember.identifiers.FirstOrDefault(x => x.identifierType.ToLower() == "npinumber")?.value,
                                    ClaimChargeEntryEntityId = chargeEntryToUpdate.Id,
                                    IsSecondBillingCode = false
                                };
                                MarkCreated(claimAppointmentLinkChargeEntry, claimToUpdate.CreatedBy);
                                _linkChargeRepository.Add(claimAppointmentLinkChargeEntry);
                                await _linkChargeRepository.CommitAsync();

                                newLink = await _linkRepository.Query().Where(x => x.ClaimId == claimToUpdate.Id && x.ClaimChargeEntriesId == null).FirstOrDefaultAsync();
                                newLink.ClaimChargeEntriesId = chargeEntryToUpdate.Id;
                                newLink.ClaimAppointmentLinkChargeEntryId = claimAppointmentLinkChargeEntry.Id;
                                _linkRepository.Update(newLink);

                                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                                {
                                    ClaimId = chargeEntryToUpdate.ClaimId,
                                    MemberId = claimToUpdate.ModifiedBy.GetValueOrDefault(),
                                    Mode = ClaimActionMode.System,
                                    ClaimAction = ClaimAction.Edit,
                                    ClaimHistoryAction = ClaimHistoryAction.ChargeEntryUpdated,
                                    NewValue = $"{chargeEntryToUpdate.Id}",
                                });
                            }
                        }
                    }
                    else
                    {
                        claimToUpdate = _billingClaimRepository.Query().Include(c => c.ClaimChargeEntries)
                            .FirstOrDefault(e => e.Id == link.ClaimId);

                        var chargeEntries = _chargeEntryRepository.Query()
                                            .Where(x => x.ClaimId == claimToUpdate.Id)
                                            .ToList();

                        foreach (var chargeEntry in chargeEntries)
                        {
                            chargeEntry.RenderingProviderId = claimToUpdate?.RenderingStaffMemberId ?? 0;
                        }

                        if (chargeEntries.Any())
                        {
                            await _chargeEntryRepository.SaveChangesAsync();
                        }

                        if (claimToUpdate.ClaimStatus == ClaimStatus.ReadyToBill)
                        {
                            //claimToUpdate.IsFlagged = true;

                            await UpdateClaim(claimToUpdate, appointment.modifiedBy.GetValueOrDefault());

                            await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                            {
                                ClaimId = claimToUpdate.Id,
                                MemberId = appointment.modifiedBy.GetValueOrDefault(),
                                Mode = ClaimActionMode.System,
                                ClaimAction = ClaimAction.Edit,
                                ClaimHistoryAction = ClaimHistoryAction.Flagged,
                            });
                        }
                    }

                    await _bus.SendAsync(new ClaimCreateEnd
                    {
                        ClaimId = claimToUpdate.Id,
                        AccountInfoId = claimToUpdate.AccountInfoId,
                        RenderingProviderId = claimToUpdate.RenderingStaffMemberId,
                        RenderingProviderTypeId = claimToUpdate.RenderingProviderTypeId,
                        FunderId = claimToUpdate.PrimaryFunderId,
                        ClientId = claimToUpdate.ChildProfileId,
                        ChildProfileAuthorizationId = claimToUpdate.AuthorizationId
                    }, Queues.RT_Billing_ClaimCreationEnd);

                    await _bus.SendAsync(new AppointmentBillingStatus
                    {
                        AppointmentId = appointmentId,
                        BillingStatus = RethinkBillingStatus.Pending,
                        ModifiedDate = EstDateTime
                    }, Queues.RT_Billing_Queue_AppointmentBillingStatus);

                    List<ClaimTransactionModel> claimTransactionData = [];
                    foreach (var chargeEntryId in claimToUpdate.ClaimChargeEntries.Select(x => x.Id))
                    {
                        claimTransactionData.Add(PrepareClaimTransaction(chargeEntryId, ClaimTransactionType.billedAmount));
                    }
                    await _bus.SendBatchAsync(Topics.RT_Billing_ProcessClaimTxn, claimTransactionData);

                    await _claimValidationService.ValidateClaimData(claimToUpdate.Id, claimToUpdate.CreatedBy, null, clientFunderServiceLine.responsibilitySequence);

                    // --- Code block for Creating Patient Invocie In Case of Private Funder and marking the claim and other entrier as deleted ---
                    if (clientFunderServiceLine.ChildProfileFunderMapping.Funder.funderTypeId == (int)FunderType.PrivatePay)
                    {
                        await HandlePrivatePayClaimAsync(claimToUpdate);
                    }
                    // --- End block ---
                }
            }
            catch (Exception ex)
            {
                var isGenericError = IsGenericTransientError(ex);

                const string genericErrorMessage = "Unable to retrieve the requested information at the moment. Please try again later.";

                link ??= await _linkRepository.Query().FirstOrDefaultAsync(l => l.AppointmentId == appointmentId && !l.DateDeleted.HasValue);

                if (isGenericError)
                {
                    bool hasExistingErrors = link != null
                        && await _appointmentClaimProcessingErrorRepository.Query()
                            .AnyAsync(e => e.ClaimAppointmentLinkId == link.Id && !e.DateDeleted.HasValue);

                    if (link == null || !hasExistingErrors)
                    {
                        await LogAppointmentProcessionError(appointmentId, 0, genericErrorMessage);
                        return;
                    }
                }

                throw;
            }
        }

        private static readonly HashSet<string> _transientErrorMessages = new(StringComparer.OrdinalIgnoreCase)
        {
            "Nullable object must have a value.",
            "Object reference not set to an instance of an object.",
            "The operation was canceled.",
            "Connection timed out",
            "Value cannot be null. (Parameter 'source')",
            "Error converting value {null} to type 'System.Boolean'",
            "Unable to read data from the transport connection: Connection reset by peer.",
            "Network is unreachable",
        };

        private static bool IsGenericTransientError(Exception ex)
        {
            var exceptionMessage = ex.Message;
            var innerMessage = ex.InnerException?.Message;

            return (exceptionMessage != null && _transientErrorMessages.Contains(exceptionMessage))
                || (innerMessage != null && _transientErrorMessages.Contains(innerMessage));
        }

        // Updated: include memberId instead of using appointmentId as CreatedBy
        private async Task LogAppointmentProcessionError(int appointmentId, int memberId, string errorMessage)
        {
            var link = await _linkRepository.Query().FirstOrDefaultAsync(l => l.AppointmentId == appointmentId && !l.DateDeleted.HasValue);
            // Creating the AppointmentClaimProcessingErrorEntity
            var claimSyncError = new AppointmentClaimProcessingErrorEntity()
            {

                ClaimAppointmentLinkId = link.Id,
                ErrorMessage = errorMessage,
                DateCreated = EstDateTime

            };

            // Inactive all previous errors for the same link

            var existingErrors = await _appointmentClaimProcessingErrorRepository.Query()
                .Where(e => e.ClaimAppointmentLinkId == link.Id && !e.DateDeleted.HasValue)
                .ToListAsync();

            if (existingErrors.Count > 0)
            {
                foreach (var error in existingErrors)
                {
                    SoftDelete(error, memberId);
                }
                // update the repository with the soft-deleted errors
                _appointmentClaimProcessingErrorRepository.UpdateRange(existingErrors);
            }

            // Mark the entity as created (audit logic)
            MarkCreated(claimSyncError, memberId);

            // Add entity to the repository
            await _appointmentClaimProcessingErrorRepository.AddAsync(claimSyncError);

            // Commit the transaction
            await _appointmentClaimProcessingErrorRepository.CommitAsync();


        }

        //--- Method for Creating Patient Invocie In Case of Private Funder and marking the claim and other entriers as deleted ---
        private async Task HandlePrivatePayClaimAsync(ClaimEntity claimToUpdate)
        {
            // Mark claim as deleted
            claimToUpdate.DateDeleted = EstDateTime;
            claimToUpdate.DeletedBy = claimToUpdate.CreatedBy;
            claimToUpdate.isPrivatePayClaim = true;
            _billingClaimRepository.Update(claimToUpdate);
            await _billingClaimRepository.CommitAsync();

            // Add payment posting for manual patient payment
            ManualCreatePaymentModel manualCreatePaymentModel = new ManualCreatePaymentModel
            {
                FunderType = "Patient",
                PaymentMethod = "Cash",
                PaymentAmount = 0,
                ReferenceNumber = claimToUpdate.ClaimIdentifier,
                PostDate = EstDateTime,
                DepositDate = EstDateTime,
                AccountInfoId = claimToUpdate.AccountInfoId,
                MemberId = claimToUpdate.CreatedBy,
            };
            var paymentId = await _paymentPostingService.CreateManualPatientPaymentAsync(manualCreatePaymentModel);

            // Get patient info
            var patients = await _rethinkServices.GetChildProfile(claimToUpdate.AccountInfoId, claimToUpdate.ChildProfileId);
            var patient = new ChildProfileEntityModel()
            {
                Id = claimToUpdate.ChildProfileId,
                FirstName = patients != null ? patients.name.firstName : "",
                MiddleName = patients != null ? patients.name.middleName : "",
                LastName = patients != null ? patients.name.lastName : "",
                AccountInfoId = patients != null ? patients.accountId : claimToUpdate.AccountInfoId
            };

            // Get claim with charge entries
            var payment = await _paymentRepository.Query().Where(x => x.Id == paymentId && x.DateDeleted == null).FirstOrDefaultAsync();
            var claims = await _chargeEntryService.GetAllClaimsByIdAsync(payment, new int[] { claimToUpdate.Id });

            // Create payment claim with lines
            await CreatePaymentClaimWithLines(paymentId, claims.FirstOrDefault(), patient, claimToUpdate.CreatedBy);

            // Mark payment as deleted
            SoftDelete(payment, claimToUpdate.CreatedBy);
            _paymentRepository.Update(payment);
            await _paymentRepository.CommitAsync();
        }

        public async Task<int> CreatePaymentClaimWithLines(int paymentId, ClaimChargeItem claim, ChildProfileEntityModel patient, int memberId)
        {
            var paymentClaim = new PaymentClaimEntity
            {
                PaymentId = paymentId,
                ChildProfileId = patient.Id,
                ClaimStatus = claim.ClaimStatus.ToString(),
                ClientFirstName = patient.FirstName,
                ClientLastName = patient.LastName,
                ClientMiddleName = patient.MiddleName,
                ClaimId = claim.ClaimId,
                ClaimIdentifier = _billingClaimRepository.Query().FirstOrDefault(x => x.Id == claim.ClaimId)?.ClaimIdentifier,
                TotalCharge = claim.ChargeEntries.Sum(x => x.Charges),
                TotalChargeOrig = claim.ChargeEntries.Sum(x => x.Charges),
                TotalPayment = 0,
                TotalPaymentOrig = claim.ChargeEntries.Sum(x => x.TotalAmount)
            };

            MarkCreated(paymentClaim, memberId);
            var dbPaymentClaim = await _paymentClaimRepository.AddAndGetAsync(paymentClaim);
            dbPaymentClaim = await _paymentClaimRepository.Query().Include(x => x.Payment).FirstOrDefaultAsync(x => x.Id == dbPaymentClaim.Id);

            foreach (var claimChargeEntry in claim.ChargeEntries)
            {
                var chargeEntry = new PaymentClaimServiceLineEntity
                {
                    PaymentClaimId = dbPaymentClaim.Id,
                    ClaimChargeEntryId = claimChargeEntry.Id,
                    DateOfService = claimChargeEntry.DateOfService,
                    DateOfServiceOrig = claimChargeEntry.DateOfService,
                    ServiceCode = claimChargeEntry.ServiceCode ?? "",
                    ServiceCodeOrig = claimChargeEntry.ServiceCode ?? "",
                    PaymentAmount = 0,
                    PaymentAmountOrig = claimChargeEntry.TotalAmount,
                    ChargeAmount = claimChargeEntry.Charges,
                    ExpectedAmount = claimChargeEntry.Charges,
                    ChargeAmountOrig = claimChargeEntry.Charges,
                    ProcedureModifier1 = claimChargeEntry.Modifier1,
                    ProcedureModifier1Orig = claimChargeEntry.Modifier1,
                    ProcedureModifier2 = claimChargeEntry.Modifier2,
                    ProcedureModifier2Orig = claimChargeEntry.Modifier2,
                    ProcedureModifier3 = claimChargeEntry.Modifier3,
                    ProcedureModifier3Orig = claimChargeEntry.Modifier3,
                    ProcedureModifier4 = claimChargeEntry.Modifier4,
                    ProcedureModifier4Orig = claimChargeEntry.Modifier4,
                    ProcedureUnits = claimChargeEntry.Units.ToString(),
                    ProcedureUnitsOrig = claimChargeEntry.Units.ToString(),
                    ProcedureDesc = claimChargeEntry.Description

                };

                MarkCreated(chargeEntry, memberId);
                var dbServiceLine = await _paymentClaimServiceLineRepository.AddAndGetAsync(chargeEntry);

                var chargeItem = new PaymentClaimServiceLineAdjustmentEntity
                {
                    PaymentClaimServiceLineId = dbServiceLine.Id,
                    AdjustmentAmount = claimChargeEntry.Charges,
                    AdjustmentAmountOrig = claimChargeEntry.Charges,
                    IsAdjustmentPositive = false,
                    AdjustmentGroupCode = "PR",
                    AdjustmentGroupCodeOrig = "PR",
                    AdjustmentReasonCode = "1",
                    AdjustmentReasonCodeOrig = "1",
                    Mode = ClaimActionMode.User
                };

                MarkCreated(chargeItem, 0);

                chargeItem.DateCreated = chargeEntry.DateCreated;
                var sr = await _serviceLineAdjustmentRepository.AddAndGetAsync(chargeItem);
            }
            return 1;
        }

        public async Task SyncClaimDeleteAsync(int appointmentId)
        {
            var appointment = await _rethinkServices.GetAppointmentAsync(appointmentId); //_appointmentRepository.Query().FirstOrDefaultAsync(x => x.Id == appointmentId);

            if (appointment == null) return;

            var link = _linkRepository.Query().FirstOrDefault(l => l.AppointmentId == appointment.id && !l.DateDeleted.HasValue);
            if (link != null)
            {
                SoftDelete(link, appointment.modifiedBy.GetValueOrDefault());
                var claimToUpdate = await _billingClaimRepository.GetByIdAsync(link.ClaimId);

                /*if (!isApproved && claimToUpdate.IsAppointmentDeleted != true)
                {*/
                claimToUpdate.IsAppointmentDeleted = true;
                await UpdateClaim(claimToUpdate, appointment.modifiedBy.GetValueOrDefault());
                /*}*/

                _linkRepository.Update(link);
                await _linkRepository.CommitAsync();

                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = claimToUpdate.Id,
                    MemberId = appointment.modifiedBy.GetValueOrDefault(),
                    Mode = ClaimActionMode.System,
                    ClaimAction = ClaimAction.Delete,
                    ClaimHistoryAction = ClaimHistoryAction.AppointmentRemoved,
                    NewValue = $"{appointment.id}",
                });
            }
        }

        public async Task AutoProcessUnBilledAppointmentScheduleBatchAsync()
        {
            var maxParallelism = _configuration.GetValue<int>("Scheduling:MaxParallelism");
            var batchSize = _configuration.GetValue<int>("Scheduling:BatchSize");
            var maxRetry = _configuration.GetValue<int>("Scheduling:MaxRetry");
            var now = DateTime.UtcNow.TrimToMilliseconds();

            var records = await _unProcessedApointmentScheduleRepository.Query()
                .Where(x =>
                    (
                        x.ProcessingStatus == ProcessingState.Unprocessed.ToString()
                        || (x.ProcessingStatus == ProcessingState.Failed.ToString()
                            && x.Retry <= maxRetry)
                    )
                    && x.UtcExecutionDateTime < now)
                .OrderBy(x => x.UtcExecutionDateTime)
                .Take(batchSize)
                .ToListAsync();

            if (!records.Any())
                return;

            var results = new ConcurrentBag<ProcessingResult>();

            await Parallel.ForEachAsync(
                records,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = maxParallelism
                },
                async (record, cancellationToken) =>
                {
                    try
                    {
                        await PublishUnbilledAppointmentForClaimProcessingAsync(
                            record.AccountInfoId,
                            record.Id,
                            record.AppointmentId);

                        results.Add(new ProcessingResult(record.Id, true));
                    }
                    catch (Exception)
                    {
                        results.Add(new ProcessingResult(record.Id, false));
                    }
                });

            foreach (var result in results)
            {
                var record = records.First(x => x.Id == result.RecordId);

                if (result.IsSuccess)
                {
                    record.ProcessingStatus = ProcessingState.Processed.ToString();
                }
                else
                {
                    record.Retry += 1;
                    record.ProcessingStatus = ProcessingState.Failed.ToString();
                }

                await _unProcessedApointmentScheduleRepository.UpdateAsync(record);
            }
        }

        private DateTime? Convert(DateTime date)
        {
            return date.Date == DateTime.MinValue ? null : date;
        }

        private async Task UpdateClaim(ClaimEntity claim, int memberId)
        {
            MarkUpdated(claim, memberId);

            // Do not change the rebill in any condition except the electronic submission
            // Bug No. 234789
            //if (claim.ClaimStatus == ClaimStatus.Billed || claim.ClaimStatus == ClaimStatus.Rebill || claim.ClaimStatus == ClaimStatus.Pending)
            //{
            //    claim.billedDate = EstDateTime;
            //}
            //else { claim.billedDate = null; }

            _billingClaimRepository.Update(claim);
            await _billingClaimRepository.CommitAsync();
        }

        private bool IsAppointmentVerified(AppointmentRethinkModel a)
        {
            //Need to change it to microservice call
            //var accountInfo = _appointmentRepository.Query().Where(app => app.Id == a.id)
            //    .Select(app => app.StaffMember.Member.AccountInfo)
            //    .FirstOrDefault();
            var accountInfo = _rethinkServices.GetAccountReturningEntityAsync(a.staffAccountInfoId);

            bool isParentVerificationRequired = accountInfo != null ? accountInfo.Result.IsParentVerificationRequired ?? false : false;
            bool isSessionNoteEnteredRequired = accountInfo != null ? accountInfo.Result.IsSessionNoteEnteredRequired ?? false : false;

            return IsAppointmentVerified(a.adminVerificationDate, a.staffVerificationDate, a.clientVerificationDate,
                isParentVerificationRequired, isSessionNoteEnteredRequired,
                a.appointmentTypeId, a.sessionNoteResponseId);
        }

        private bool IsAppointmentVerified(
            DateTime? adminVerificationDate,
            DateTime? staffVerificationDate,
            DateTime? clientVerificationDate,
            bool? isParentVerificationRequired,
            bool? isSessionNoteEnteredRequired,
            int appointmentTypeId,
            int? sessionNoteResponseId
            )
        {
            if (adminVerificationDate != null)
            {
                return true;
            }

            var staffVerification = staffVerificationDate.HasValue
                && Convert(staffVerificationDate.Value) != null;
            if (appointmentTypeId == 1)
            {
                var parentVerification = isParentVerificationRequired == true ? clientVerificationDate != null && Convert(clientVerificationDate.Value) != null : true;
                var sessionNoteEntered = isSessionNoteEnteredRequired == true ? sessionNoteResponseId > 0 : true;

                if (staffVerification == true && parentVerification == true && sessionNoteEntered == true)
                {
                    return true;
                }
            }
            else
            {
                if (staffVerification)
                {
                    return true;
                }
            }

            return false;
        }

        private double RoundCacluation(double input, RoundingTypes roundingType)
        {
            switch (roundingType)
            {
                case RoundingTypes.RoundToNearestUnit:
                    //return Math.Round(input, 0, MidpointRounding.AwayFromZero);
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

        public async Task<BillingCodeData> GetProviderBillingCode(AppointmentRethinkModel appointment, AppointmentClientAuthBillingCodeModel childProfileAuthorizationBillingCode)
        {
            var unitTypes = new List<ClientUnitTypes>();
            if (childProfileAuthorizationBillingCode != null)
            {
                childProfileAuthorizationBillingCode.AppointmentProviderBillingCode.funders = await _rethinkServices.GetFunder(appointment.staffAccountInfoId, childProfileAuthorizationBillingCode.AppointmentProviderBillingCode.funderId);

                unitTypes = await _rethinkServices.GetUnitTypesAsync();

                childProfileAuthorizationBillingCode.AppointmentProviderBillingCode.unitTypes = unitTypes.FirstOrDefault(x => x.id == childProfileAuthorizationBillingCode.AppointmentProviderBillingCode.unitTypeId);

                childProfileAuthorizationBillingCode.AppointmentProviderBillingCode.providerSerivces = await _rethinkServices.GetProviderService(appointment.staffAccountInfoId, childProfileAuthorizationBillingCode.AppointmentProviderBillingCode.serviceId);
                return childProfileAuthorizationBillingCode.AppointmentProviderBillingCode;
            }

            /*var noAuthbillingCode = appointment.ProviderBillingCodeId.HasValue ?
                                    _providerBillingCodeRepository
                                        .Query()
                                        .Include(x => x.UnitType)
                                        .Include(x => x.ProviderService)
                                        .FirstOrDefault(x => x.Id == appointment.ProviderBillingCodeId.Value) :
                                    null;*/
            if (!appointment?.providerBillingCodeId.HasValue ?? true)
            {
                await LogAppointmentProcessionError(appointment.id, appointment.modifiedBy.GetValueOrDefault(), "Authorization missing for the Funder.");
            }
            var providerBillingCode = await _rethinkServices.GetProviderBillingCode(appointment.staffAccountInfoId, appointment.providerBillingCodeId.Value);
            providerBillingCode.funders = await _rethinkServices.GetFunder(appointment.staffAccountInfoId, providerBillingCode.funderId);

            unitTypes = await _rethinkServices.GetUnitTypesAsync();

            providerBillingCode.unitTypes = unitTypes.FirstOrDefault(x => x.id == providerBillingCode.unitTypeId);

            providerBillingCode.providerSerivces = await _rethinkServices.GetProviderService(appointment.staffAccountInfoId, providerBillingCode.serviceId);

            var noAuthbillingCode = appointment.providerBillingCodeId.HasValue ? providerBillingCode :
                                    null;

            return noAuthbillingCode;
        }


        private int? GetProviderMemberId(AppointmentRethinkModel appointment, AppointmentClientAuthBillingCodeModel childProfileAuthorizationBillingCode)
        {
            if (childProfileAuthorizationBillingCode != null)
            {
                int? renderingProviderTypeId = appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization?.authorizationRenderingProviderTypeId;

                if (renderingProviderTypeId != null)
                {
                    if (renderingProviderTypeId == (int)AuthorizationRenderingProviderTypes.ProviderAssignedToAppointment
                        || renderingProviderTypeId == (int)AuthorizationRenderingProviderTypes.Agency)
                    {
                        return appointment?.StaffMember.memberId ?? null;
                    }
                    else if (renderingProviderTypeId == (int)AuthorizationRenderingProviderTypes.StaffMember)
                    {
                        return childProfileAuthorizationBillingCode.ChildProfileAuthorization.renderingProviderStaffId;
                    }
                }
            }

            return appointment.StaffMember.memberId;
        }

        private async Task<ClaimEntity> GetExistingClaim(AppointmentRethinkModel appointment, BillingCodeData providerBillingCode, AppointmentClientAuthBillingCodeModel childProfileAuthorizationBillingCode,
            int? providerMemberId, bool combineChargesForSameClient = false, int facilityProviderLocationId = 0)
        {
            List<ClaimEntity> query;

            if (combineChargesForSameClient)
            {
                var providerLocationId = await GetPorviderLocationId(appointment.clientAccountInfoId, facilityProviderLocationId);

                return await _billingClaimRepository.Query()
                    .Include(x => x.ClaimHistory)
                    .Where(x => x.ChildProfileId == appointment.clientId
                     && x.DateDeleted == null
                     && ((x.ToLocationId != null && x.ToLocationId == appointment.toLocationId) || x.ProviderLocationId == providerLocationId)
                     && x.PrimaryFunderId == providerBillingCode.funderId &&
                     childProfileAuthorizationBillingCode != null && x.AuthorizationId == childProfileAuthorizationBillingCode.childProfileAuthorizationId &&
                    (x.ClaimStatus == ClaimStatus.PendingReview
                    || x.ClaimStatus == ClaimStatus.ReadyToBill
                    || x.ClaimStatus == ClaimStatus.Rebill) &&
                    x.ClaimHistory.Any(x => x.ClaimHistoryAction == ClaimHistoryAction.ClaimCreated
                    && x.Mode == ClaimActionMode.System)).OrderByDescending(x => x.Id).FirstOrDefaultAsync();
            }
            else
            {
                query = await _billingClaimRepository.Query()
                    .Include(x => x.ClaimHistory)
                    .Where(x => x.ChildProfileId == appointment.clientId
                    && x.StartDate.Date == appointment.startDateTime.Date
                    && x.DateDeleted == null
                    && x.LocationCodeId == appointment.locationId).ToListAsync();

                query = query.Where(e => (e.ClaimStatus == ClaimStatus.PendingReview
                    || e.ClaimStatus == ClaimStatus.ReadyToBill
                    || e.ClaimStatus == ClaimStatus.Rebill)
                    && (childProfileAuthorizationBillingCode != null) ?
                    e.AuthorizationId == childProfileAuthorizationBillingCode.childProfileAuthorizationId : e.AuthorizationId == null
                    && e.ClaimHistory.Any(x => x.ClaimHistoryAction == ClaimHistoryAction.ClaimCreated && x.Mode == ClaimActionMode.System)).ToList();


                if (providerBillingCode.funders.combineChargeTypeId == (int)CombineChargeTypes.SameDayClientProcedure)
                {
                    var data = query.SelectMany(x => x.ClaimHistory.Where(c => c.Mode == ClaimActionMode.System && c.ClaimHistoryAction == ClaimHistoryAction.ClaimCreated)).FirstOrDefault();
                    return query.FirstOrDefault(x => x.ClaimHistory.Contains(data));
                }
                else if (providerBillingCode.funders.combineChargeTypeId == (int)CombineChargeTypes.SameDayClientProcedureRenderingProvider)
                {
                    return query.FirstOrDefault(x => x.ProviderLocationId == appointment.toLocationId);
                }
            }
            return query.FirstOrDefault();
        }

        private async Task<int> GetPorviderLocationId(int accountInfoId, int facilityProviderLocationId)
        {
            var providerLocationId = 0;
            var providerLocationData = await _rethinkServices.GetProviderLocation(accountInfoId, facilityProviderLocationId);
            if (providerLocationData != null)
            {
                if (providerLocationData.isBillingLocation)
                {
                    providerLocationId = providerLocationData.id;
                }
                else
                {
                    var mainLocation = await _rethinkServices.GetMainLocation(accountInfoId);
                    providerLocationId = mainLocation?.id ?? 0;
                }
            }

            return providerLocationId;
        }

        public async Task<string> AddDiagnosisCodes(ClaimEntity claim, AppointmentClientAuthBillingCodeModel childProfileAuthorizationBillingCode, int? serviceId)
        {
            var firstDiagnosisCode = string.Empty;
            var appointmentHasAuthorization = childProfileAuthorizationBillingCode != null;

            if (appointmentHasAuthorization && childProfileAuthorizationBillingCode.ChildProfileAuthorization.ChildProfileAuthorizationDiagnosisCodes != null)
            {

                foreach (var diagnosis in childProfileAuthorizationBillingCode.ChildProfileAuthorization.ChildProfileAuthorizationDiagnosisCodes)
                {
                    var claimDiagnosisCode = new ClaimDiagnosisCodeEntity
                    {
                        ClaimId = claim.Id,
                        DiagnosisId = diagnosis.diagnosisId,
                        Order = diagnosis.order,
                        IncludeOnClaims = diagnosis.includeOnClaims,
                    };

                    MarkCreated(claimDiagnosisCode, claim.MemberId);
                    var check = _billingClaimDiagnosisCodeEntityRepository.Query().Where(x => x.ClaimId == claim.Id && x.DiagnosisId == diagnosis.diagnosisId).FirstOrDefault();
                    if (check == null)
                    {
                        await _billingClaimDiagnosisCodeEntityRepository.AddAsync(claimDiagnosisCode);
                    }
                    if (string.IsNullOrEmpty(firstDiagnosisCode))
                    {
                        firstDiagnosisCode = diagnosis.Diagnosis.diagnosisCode;
                    }
                }
            }
            else
            {
                var diagnosisCode = await _rethinkServices.GetClientDiagnosisReturningModelAsync(claim.AccountInfoId, claim.ChildProfileId);

                var diagnosisCodes = diagnosisCode.data.Where(x => x.serviceLineId == serviceId.Value)
                                        .ToList();

                foreach (var diagnosis in diagnosisCodes)
                {
                    if (diagnosis.diagnosisId.HasValue)
                    {
                        var claimDiagnosisCode = new ClaimDiagnosisCodeEntity
                        {
                            ClaimId = claim.Id,
                            DiagnosisId = diagnosis.diagnosisId.Value,
                            Order = diagnosis.order,
                            IncludeOnClaims = diagnosis.order == 1,
                        };

                        MarkCreated(claimDiagnosisCode, claim.MemberId);
                        var check = _billingClaimDiagnosisCodeEntityRepository.Query().Where(x => x.ClaimId == claim.Id && x.DiagnosisId == diagnosis.diagnosisId).FirstOrDefault();
                        if (check == null)
                        {
                            await _billingClaimDiagnosisCodeEntityRepository.AddAsync(claimDiagnosisCode);
                        }
                    }
                }

                var primaryCode = diagnosisCodes.FirstOrDefault(x => x.order == 1);
                firstDiagnosisCode = primaryCode != null ? primaryCode.diagnosis.diagnosisCode : string.Empty;
            }

            await _billingClaimDiagnosisCodeEntityRepository.SaveChangesAsync();

            return firstDiagnosisCode;
        }

        public async Task PublishUnbilledAppointmentForClaimProcessingAsync(int accountInfoId, int memberId, int apptId)
        {
            await _bus.SendAsync(new { AccountId = accountInfoId, AppointmentId = apptId, OptionalParam = true, ProcessingSchedule = true }, Queues.RT_Billing_Queue_AppointmentUpdate);
        }

        private async Task PopulateClaimEntry(ClaimEntity claim,
                                       AppointmentRethinkModel appointment,
                                       BillingCodeData providerBillingCode,
                                       AppointmentClientAuthBillingCodeModel childProfileAuthorizationBillingCode,
                                       ServiceLines childProfileFunderServiceLineMapping,
                                       int facilityId)
        {
            var appointmentHasAuthorization = childProfileAuthorizationBillingCode != null;
            var funderMapping = childProfileFunderServiceLineMapping.ChildProfileFunderMapping;
            var serviceFunder = funderMapping.Funder.ServiceFunders.FirstOrDefault(sf => sf.providerServiceId == appointment.serviceId);

            var serviceLineBillingProviderOption = serviceFunder != null && serviceFunder.billingProviderOptionId.HasValue ?
                (BillingProviderOptionType)serviceFunder.billingProviderOptionId.Value :
                BillingProviderOptionType.Unknown;

            if (appointmentHasAuthorization)
            {
                int limit = 50;
                var authorization = childProfileAuthorizationBillingCode.ChildProfileAuthorization;

                claim.AuthorizationNumber = authorization.authorizationNumber.Length > 50 ? authorization.authorizationNumber.Substring(0, limit) : authorization.authorizationNumber;
                claim.AuthorizationId = childProfileAuthorizationBillingCode.childProfileAuthorizationId;

                claim.ChildProfileReferringProviderId = authorization.childProfileReferringProviderId;
                claim.RenderingStaffMemberId = authorization.renderingProviderStaffId ?? claim.MemberId;
                // #DBMIGRATION
                //claim.ServiceLocationId = authorization.ServiceFacilityLocationId;
                claim.ServiceLocationId = facilityId;
                claim.ToLocationId = appointment.toLocationId;


                //var providerLocationData = await _rethinkServices.GetProviderLocation(claim.AccountInfoId, facilityId);
                //if (providerLocationData != null)
                //{
                //    if (providerLocationData.isBillingLocation)
                //    {
                //        claim.ProviderLocationId = providerLocationData.id;
                //    }
                //    else
                //    {
                //        var mainLocation = await _rethinkServices.GetMainLocation(claim.AccountInfoId);
                //        claim.ProviderLocationId = mainLocation?.id ?? 0;
                //    }
                //}

                claim.ProviderLocationId = await GetPorviderLocationId(claim.AccountInfoId, facilityId);
            }
            else
            {
                var childProfildeReferringProviders = await _rethinkServices.GetReferringProvidersByClientId(appointment.clientId.Value, appointment.clientAccountInfoId);

                var childProfileReferringProvider = childProfildeReferringProviders.FirstOrDefault(x => x.IsDefault == true);

                var billingLocations = await _rethinkServices.GetProviderLocationList(appointment.staffAccountInfoId);

                var serviceLocation = billingLocations.data.FirstOrDefault(x => x.id == appointment.facilityId);

                claim.ProviderLocationId = ResolveBillingProviderId(billingLocations, serviceLineBillingProviderOption, appointment.facilityId);
                claim.ChildProfileReferringProviderId = childProfileReferringProvider?.Id;
                claim.RenderingStaffMemberId = providerBillingCode.renderingProviderStaffId ?? claim.MemberId;
                claim.ServiceLocationId = serviceLocation?.id;
            }

            if (claim.RenderingStaffMemberId.HasValue)
            {
                var renderingProviders = await _rethinkServices.GetRenderingProvidersAsync(claim.AccountInfoId, true);
                claim.RenderingProviderTypeId = renderingProviders.FirstOrDefault(x => x.StaffMemberId == claim.RenderingStaffMemberId).Id;
            }
            claim.ProviderBillingCodeId = providerBillingCode.id;
            claim.LocationCodeId = appointment.locationId.Value;
            claim.PrimaryFunderId = childProfileFunderServiceLineMapping.ChildProfileFunderMapping.funderId;
            claim.LastBilledFunderId = claim.PrimaryFunderId;
            //claim.ClientFunderId = appointment.clientFunderId.HasValue && appointment.clientFunderId.Value > 0 ? appointment.clientFunderId :
            //    childProfileFunderServiceLineMapping.ChildProfileFunderMapping.id;

            // #DBMIGRATION
            //< GET THE MAPPING ID FROM THE MAPPING TABLE
            //claim.ClientFunderId = appointment.ClientFunderId.HasValue && appointment.ClientFunderId.Value > 0 ?
            //    appointment.ClientFunderId :
            //    childProfileFunderServiceLineMapping.ChildProfileFunderMapping.Id;

            var funderMappings = await _rethinkServices.GetChildProfileFunderMappings(claim.AccountInfoId, appointment.clientId ?? 0);
            if (funderMappings != null)
            {
                var clientFunder = funderMappings.data.Where(x => x.funderId == appointment.funderId).FirstOrDefault();
                claim.ClientFunderId = clientFunder != null ? clientFunder.id : 0;
            }
            //>

            claim.ClientFunderServiceLineId = childProfileFunderServiceLineMapping.id;
            claim.ClaimStatus = ClaimStatus.PendingReview;

            claim.ReleaseOfInformationConfirmationTypeId = funderMapping.releaseOfInformationConfirmationTypeId;
            claim.AuthorizedPaymentConfirmationTypeId = funderMapping.authorizedPaymentConfirmationTypeId;
            claim.BenefitAssignmentId = (funderMapping.isAutismCoveredBenefit == true || funderMapping.isAutismCoveredBenefit == null) ? 1 : 2;
            claim.IsSecondaryPayerAvailable = false;

            var secondaryFunderDetails = await _claimUpdateService.CheckAndGetSecondaryFunderDetails(claim.AccountInfoId, claim);
            if (secondaryFunderDetails != null && secondaryFunderDetails.funders.Any())
            {
                claim.IsSecondaryPayerAvailable = true;
            }
        }

        private int? ResolveBillingProviderId(ClientProviderLocationsModel providers, BillingProviderOptionType billingProviderOption, int? facilityId)
        {
            var billingProvider = providers.data.FirstOrDefault(x => x.id == facilityId);
            var isGroupOnly = billingProviderOption == BillingProviderOptionType.Group;
            var isIndividualAndGroup = billingProviderOption == BillingProviderOptionType.GroupAndIndividual;
            if (isGroupOnly || isIndividualAndGroup)
            {
                if (billingProvider == null || !billingProvider.isBillingLocation)
                {
                    billingProvider = providers.data.FirstOrDefault(x => x.isBillingLocation && x.isMainLocation);
                    return billingProvider?.id;
                }
            }

            if (billingProviderOption == BillingProviderOptionType.Individual)
            {
                return null;
            }

            return billingProvider?.id;
        }

        // Handles non-immediate claim creation behavior (hold in unbilled and exit)
        private async Task HandleNonImmediateClaimCreationAsync(AppointmentRethinkModel appointment, int accountInfoId, int appointmentId, BillingFunderIdRequestModel funder)
        {
            // Ensure link exists so appointment appears in unbilled list
            var linkExists = await _linkRepository.Query().AnyAsync(l => l.AppointmentId == appointment.id && l.ClaimId != 0 && !l.DateDeleted.HasValue);
            if (!linkExists)
            {
                var newLink = new ClaimAppointmentLinkEntity
                {
                    AppointmentId = appointment.id,
                    AccountInfoId = accountInfoId
                };
                MarkCreated(newLink, appointment.StaffMember.memberId);
                await _linkRepository.AddAsync(newLink);
                await _linkRepository.SaveChangesAsync();
            }

            await CreateUnBilledAppointmentScheduleIfNotExistsAsync(funder, appointment.id);
            // Send billing status update as pending/held
            await _bus.SendAsync(new AppointmentBillingStatus
            {
                AppointmentId = appointmentId,
                BillingStatus = RethinkBillingStatus.NotBilled,
                ModifiedDate = EstDateTime
            }, Queues.RT_Billing_Queue_AppointmentBillingStatus);
        }

        private async Task CreateUnBilledAppointmentScheduleIfNotExistsAsync(BillingFunderIdRequestModel funder, int appointmentId)
        {
            var exists = _unProcessedApointmentScheduleRepository.Query()
                .Any(x => x.AccountInfoId == funder.AccountInfoId &&
                          x.AppointmentId == appointmentId);

            if (!exists && funder.ScheduleTimeZone is not null && funder.ScheduleTime is not null)
            {
                var timezone = _timezonesEntity.Query().FirstOrDefault(x => x.Id == funder.ScheduleTimeZone);
                var timeZonesDictionary = timezone?.SimpleName;
                var unProcessedApointmentSchedule = funder.ToUnProcessedApointmentSchedule(appointmentId, timeZonesDictionary);
                var strategy = ScheduleFactory.Get(unProcessedApointmentSchedule.ClaimCreationFrequency);
                var nextExecution = strategy.GetNextExecutionUtc(unProcessedApointmentSchedule);
                var entity = unProcessedApointmentSchedule.ToUnProcessedApointmentScheduleEntity(nextExecution);

                try
                {
                    await _unProcessedApointmentScheduleRepository.AddAsync(entity);
                    await _unProcessedApointmentScheduleRepository.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    await LogAppointmentProcessionError(appointmentId, funder.AccountInfoId, $"Failed to create unbilled appointment schedule: {ex.Message}");
                }
            }
        }
    }
}