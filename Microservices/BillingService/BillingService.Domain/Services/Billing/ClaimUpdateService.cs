using BillingService.Domain.Interfaces.Billing;
using BillingService.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Entities.Billing.Payment;
using Rethink.Services.Common.Entities.Billing.Scheduling;
using Rethink.Services.Common.Enums.BH;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Infrastructure.Context.Billing;
using Rethink.Services.Common.Infrastructure.Repository;
using Rethink.Services.Common.Interfaces;
using Rethink.Services.Common.Models;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Services;
using Rethink.Services.Domain.Interfaces;
using Rethink.Services.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing
{
    public class ClaimUpdateService : BaseService, IClaimUpdateService
    {

        private readonly IRepository<BillingDbContext, ClaimEntity> _claimRepository;
        private readonly IRethinkMasterDataMicroServices _rethinkServices;
        private readonly IRepository<BillingDbContext, ClaimDiagnosisCodeEntity> _billingClaimDiagnosisCodeEntityRepository;
        private readonly IRepository<BillingDbContext, ClaimChargeEntryEntity> _chargeEntryRepository;
        private readonly IRepository<BillingDbContext, ClaimAppointmentLinkEntity> _linkRepository;
        private readonly IRepository<BillingDbContext, AppointmentClaimProcessingErrorEntity> _appointmentClaimProcessingErrorRepository;
        private readonly IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry> _linkChargeRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimServiceLineEntity> _paymentClaimServiceLineRepository;
        private readonly IRepository<BillingDbContext, PaymentClaimEntity> _paymentClaimRepository;
        private readonly IServiceProvider _serviceProvider;
        public ClaimUpdateService(
            IRepository<BillingDbContext, ClaimEntity> claimRepository,
            IRethinkMasterDataMicroServices rethinkServices,
            IRepository<BillingDbContext, ClaimDiagnosisCodeEntity> billingClaimDiagnosisCodeEntityRepository,
            IRepository<BillingDbContext, ClaimChargeEntryEntity> chargeEntryRepository,
            IRepository<BillingDbContext, ClaimAppointmentLinkEntity> linkRepository,
            IRepository<BillingDbContext, AppointmentClaimProcessingErrorEntity> appointmentClaimProcessingErrorRepository,
            IServiceProvider serviceProvider,
            IRepository<BillingDbContext, ClaimAppointmentLinkChargeEntry> linkChargeRepository,
            IRepository<BillingDbContext, PaymentClaimServiceLineEntity> paymentClaimServiceLineRepository,
            IRepository<BillingDbContext, PaymentClaimEntity> paymentClaimRepository
            )
        {
            _claimRepository = claimRepository;
            _rethinkServices = rethinkServices;
            _billingClaimDiagnosisCodeEntityRepository = billingClaimDiagnosisCodeEntityRepository;
            _chargeEntryRepository = chargeEntryRepository;
            _linkRepository = linkRepository;
            _appointmentClaimProcessingErrorRepository = appointmentClaimProcessingErrorRepository;
            _serviceProvider = serviceProvider;
            _linkChargeRepository = linkChargeRepository;
            _paymentClaimServiceLineRepository = paymentClaimServiceLineRepository;
            _paymentClaimRepository = paymentClaimRepository;
        }

        public async Task<ClaimUpdateResult> UpdateClaimSecondaryFunderOnRefresh(int accountInfoId, int memberId, int claimId)
        {
            var result = new ClaimUpdateResult { Success = false, Message = string.Empty };
            try
            {
                var claim = await _claimRepository.Query().FirstOrDefaultAsync(x => x.Id == claimId && x.DateDeleted == null);
                if (claim.ClaimStatus == ClaimStatus.PendingReview || claim.ClaimStatus == ClaimStatus.ReadyToBill || claim.ClaimStatus == ClaimStatus.Rebill)
                {
                    try
                    {
                        await SyncClaimUpdatePrimaryFunder(accountInfoId, memberId, claimId);
                    }
                    catch (ClaimPrimaryFunderUpdateException ex) // Catch only user-defined exception
                    {
                        // Capture the specific exception message and return it to frontend
                        result.Message = ex.Message;
                        return result;
                    }
                }

                claim.IsSecondaryPayerAvailable = false;
                var secondaryFunderDetails = await CheckAndGetSecondaryFunderDetails(accountInfoId, claim);
                if (secondaryFunderDetails != null && secondaryFunderDetails.funders.Any())
                {
                    claim.IsSecondaryPayerAvailable = true;
                }
                MarkUpdated(claim, memberId);
                _claimRepository.Update(claim);
                await _claimRepository.SaveChangesAsync();

                // Sync Claim Diagnosis Codes
                await SyncClaimDiagnosisCode(accountInfoId, memberId, claimId);

                result.Success = true;
                return result;
            }
            catch (Exception)
            {
                return result;
            }
        }

        public async Task<ClaimNextFundersAndControlNumberModel> CheckAndGetSecondaryFunderDetails(int accountInfoId, ClaimEntity claim)
        {
            try
            {
                var funderMappings = await _rethinkServices.GetChildProfileFunderServiceLineMapping(accountInfoId, claim.ChildProfileId);
                var claimServiceId = funderMappings.First(x => x.id == claim.ClientFunderServiceLineId).serviceId;
                funderMappings = funderMappings.Where(x => x.responsibilitySequence == ResponsibilitySequenceType.Secondary &&
                claimServiceId == x.serviceId &&
                (x.ChildProfileFunderMapping.startDate.HasValue ? claim.StartDate >= x.ChildProfileFunderMapping.startDate : true) &&
                (x.ChildProfileFunderMapping.endDate.HasValue ? claim.EndDate <= x.ChildProfileFunderMapping.endDate : true) &&
                x.ChildProfileFunderMapping.metaData.deletedOn == null).ToList();
                if (!funderMappings.Any())
                    return null;
                return new ClaimNextFundersAndControlNumberModel
                {
                    funders = funderMappings.OrderBy(slm => slm.responsibilitySequence.AsOrdinal())
                    .Select(x => new ClaimPatientFunderModel
                    {
                        Id = x.ChildProfileFunderMappingId,
                        Name = x.ChildProfileFunderMapping.Funder.funderName,
                        ResponsibilitySequence = x.responsibilitySequence.AsOrdinal(),
                    }).DistinctBy(x => x.Id).ToList(),
                    controlNumber = ""//controlNumber
                };
            }
            catch (Exception)
            {
                return null;
            }
        }


        public async Task SyncClaimUpdatePrimaryFunder(int accountInfoId, int memberId, int claimId)
        {
            var appointments = new List<Rethink.Services.Common.Models.AppointmentRethinkModel>();
            var newLink = new ClaimAppointmentLinkEntity();
            AppointmentClientAuthBillingCodeModel childProfileAuthorizationBillingCode = null;

            try
            {
                var _claimHistoryService = (IClaimHistoryService)_serviceProvider.GetRequiredService(typeof(IClaimHistoryService));
                var _claimSyncService = (IClaimSyncService)_serviceProvider.GetRequiredService(typeof(IClaimSyncService));

                var appointmentLinks = await _linkRepository.Query()
                                       .Include(x => x.Claim)
                                       .Where(x => x.ClaimId == claimId && !x.DateDeleted.HasValue && x.ClaimId != 0)
                                       .ToListAsync();

                if (!appointmentLinks.Any())
                    return;

                foreach (var link in appointmentLinks)
                {
                    if (link.AppointmentId <= 0)
                        continue;

                    var appointmentData = await _rethinkServices.GetAppointmentAsync(link.AppointmentId);
                    appointmentData.StaffMember = await _rethinkServices.GetStaffMember(appointmentData.staffAccountInfoId, appointmentData.staffId);
                    appointmentData.ChildProfileAuthorizationBillingCode = await _rethinkServices.GetChildProfileAuthBillingCodeForAppointment(appointmentData.staffAccountInfoId, appointmentData.clientId.Value, appointmentData.procedureCodeId);
                    if (appointmentData.providerBillingCodeId != null && appointmentData.ChildProfileAuthorizationBillingCode != null)
                    {
                        appointmentData.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode = await _rethinkServices.GetProviderBillingCode(appointmentData.StaffMember.accountId, appointmentData.providerBillingCodeId ?? 0);
                        appointmentData.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization = await _rethinkServices.GetChildProfileAuthorizationById(appointmentData.staffAccountInfoId, appointmentData.ChildProfileAuthorizationBillingCode.childProfileAuthorizationId);
                    }


                    if (appointmentData != null)
                        appointments.Add(appointmentData);
                }

                if (!appointments.Any())
                    return;

                if (appointments.Select(a => a.funderId).Distinct().Count() > 1)
                {
                    throw new ClaimPrimaryFunderUpdateException("The claim has multiple funders—can’t refresh. Delete it, split into separate claims per appointment, then refresh.");
                }

                if (appointments
                    .Select(a => a.ChildProfileAuthorizationBillingCode?.childProfileAuthorizationId)
                    .Distinct()
                    .Count() > 1)
                {
                    throw new ClaimPrimaryFunderUpdateException("The Claim has multiple authorizations—can’t refresh. Delete it, split into separate claims per appointment, then refresh to update the funder.");
                }

                var appointment = appointments.First();
                var claimToUpdate = appointmentLinks.Where(x => x.AppointmentId == appointment.id).FirstOrDefault().Claim;
                var appointmentId = appointment.id;

                if (appointment.funderId == claimToUpdate.PrimaryFunderId)
                    return;

                //appointment.ChildProfileAuthorizationBillingCode = await _rethinkServices.GetChildProfileAuthBillingCodeForAppointment(appointment.staffAccountInfoId, appointment.clientId.Value, appointment.procedureCodeId);
                if (appointment.providerBillingCodeId != null && appointment.ChildProfileAuthorizationBillingCode != null)
                {
                    appointment.ChildProfileAuthorizationBillingCode.AppointmentProviderBillingCode = await _rethinkServices.GetProviderBillingCode(appointment.StaffMember.accountId, appointment.providerBillingCodeId ?? 0);
                    appointment.ChildProfileAuthorizationBillingCode.ChildProfileAuthorization = await _rethinkServices.GetChildProfileAuthorizationById(appointment.staffAccountInfoId, appointment.ChildProfileAuthorizationBillingCode.childProfileAuthorizationId);
                }

                var appointmentHasAuthorization = appointment.ChildProfileAuthorizationBillingCode != null;
                var funderMappingsMicro = await _rethinkServices.GetChildProfileFunderMappings(accountInfoId, appointment.clientId.Value);
                var funderMappingsMicroId = funderMappingsMicro.data.FirstOrDefault(x => x.funderId == appointment.funderId);
                var serviceLineMappings = await _rethinkServices.GetServiceLineMappingsByFunderId(accountInfoId, appointment.clientId.Value, funderMappingsMicroId.id);
                var serviceLineMappingsId = serviceLineMappings.FirstOrDefault(x => x.serviceId == appointment.serviceId);

                var clientFunderServiceLine = await _rethinkServices.GetChildProfileFunderServiceLineMappingEntity(accountInfoId, appointment.clientId.Value, funderMappingsMicroId.id, serviceLineMappingsId.id);
                if (clientFunderServiceLine == null || clientFunderServiceLine?.ChildProfileFunderMapping == null)
                {
                    await LogAppointmentProcessionError(appointmentId, appointment.modifiedBy.GetValueOrDefault(), "Client Funder Service Line Not Found");
                }

                clientFunderServiceLine.ChildProfileFunderMapping.Funder = await _rethinkServices.GetFunder(accountInfoId, funderMappingsMicroId.funderId);
                clientFunderServiceLine.ChildProfileFunderMapping.Funder.ServiceFunders = await _rethinkServices.GetServiceFundersEntityListByFunderId(appointment.clientAccountInfoId, appointment.clientId ?? 0, funderMappingsMicroId.funderId);

                var facilityId = new ProviderLocationModel();
                if (appointmentHasAuthorization)
                {
                    childProfileAuthorizationBillingCode = await _rethinkServices.GetChildProfileAuthBillingCodeForAppointment(accountInfoId, appointment.clientId ?? 0, appointment.procedureCodeId);
                    childProfileAuthorizationBillingCode.AppointmentProviderBillingCode = await _rethinkServices.GetProviderBillingCode(appointment.StaffMember.accountId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId ?? 0);
                    childProfileAuthorizationBillingCode.ChildProfileAuthorization = await _rethinkServices.GetChildProfileAuthorizationByClientId(appointment.staffAccountInfoId, appointment.clientId ?? 0, appointment.ChildProfileAuthorizationBillingCode.childProfileAuthorizationId);

                    if (childProfileAuthorizationBillingCode == null || childProfileAuthorizationBillingCode.ChildProfileAuthorization == null)
                    {
                        await LogAppointmentProcessionError(appointmentId, appointment.modifiedBy.GetValueOrDefault(), "Authorization missing for childProfile.");
                    }
                    childProfileAuthorizationBillingCode.ChildProfileAuthorization.ChildProfileAuthorizationDiagnosisCodes = await _rethinkServices.GetChildProfileAuthorizationDiagnosisCodesAsync(appointment.staffAccountInfoId, appointment.clientId.Value, childProfileAuthorizationBillingCode.ChildProfileAuthorization.childProfileDiagnosisId, childProfileAuthorizationBillingCode.ChildProfileAuthorization.id);
                    childProfileAuthorizationBillingCode.ChildProfileAuthorization.Funder = await _rethinkServices.GetFunder(appointment.staffAccountInfoId, appointment.funderId);
                    appointment.ChildProfileAuthorizationBillingCode = childProfileAuthorizationBillingCode;
                    facilityId = await _rethinkServices.GetChildProfileFacility(appointment.staffAccountInfoId, appointment.clientId ?? 0);
                }

                var providerBillingCode = await GetProviderBillingCode(appointment, childProfileAuthorizationBillingCode);

                await PopulateClaimEntry(claimToUpdate, appointment, providerBillingCode, childProfileAuthorizationBillingCode, clientFunderServiceLine, facilityId.providerLocationId);

                await UpdateClaim(claimToUpdate, memberId);

                await _claimHistoryService.AddAsync(new ClaimHistorySaveModel
                {
                    ClaimId = claimToUpdate.Id,
                    MemberId = memberId,
                    Mode = ClaimActionMode.User,
                    ClaimAction = ClaimAction.Edit,
                    ClaimHistoryAction = ClaimHistoryAction.ClaimUpdated,
                }, true);



                //mark existing charge entry as deleted

                var existingChargeEntries = await _chargeEntryRepository.Query().Where(x => x.ClaimId == claimToUpdate.Id && x.DateDeleted == null).ToListAsync();
                if (existingChargeEntries != null)
                {
                    foreach (var chargeEntry in existingChargeEntries)
                    {
                        chargeEntry.ModifiedBy = memberId;
                        chargeEntry.DateLastModified = EstDateTime;
                        chargeEntry.DateDeleted = EstDateTime;
                        chargeEntry.DeletedBy = memberId;
                        _chargeEntryRepository.Update(chargeEntry);


                        var claimAppointmentLinkChargeEntries = await _linkChargeRepository.Query().
                                                                Where(x => x.ClaimChargeEntryEntityId == chargeEntry.Id && x.DateDeleted == null).ToListAsync();
                        foreach (var linkChargeEntry in claimAppointmentLinkChargeEntries)
                        {
                            linkChargeEntry.ModifiedBy = memberId;
                            linkChargeEntry.DateLastModified = EstDateTime;
                            linkChargeEntry.DateDeleted = EstDateTime;
                            linkChargeEntry.DeletedBy = memberId;
                            _linkChargeRepository.Update(linkChargeEntry);
                        }
                    }
                    await _chargeEntryRepository.SaveChangesAsync();
                    await _linkChargeRepository.CommitAsync();
                }



                foreach (var appointmentData in appointments)
                {
                    await PopulateCharges(providerBillingCode, appointmentData, claimToUpdate, childProfileAuthorizationBillingCode, _claimHistoryService, _claimSyncService);
                }
            }
            catch (ClaimPrimaryFunderUpdateException)
            {
                // Bubble up user-defined exception to the frontend
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception("An unexpected error occurred while updating the claim: " + ex.Message, ex);
            }
        }

        private async Task UpdateClaim(ClaimEntity claim, int memberId)
        {
            MarkUpdated(claim, memberId);
            _claimRepository.Update(claim);
            await _claimRepository.CommitAsync();
        }

        private async Task SyncClaimDiagnosisCode(int accountInfoId, int memberId, int claimId)
        {
            var appointmentId = await _claimRepository.Query()
             .Where(c => c.Id == claimId)
             .Include(x => x.ClaimAppointmentLinks)
             .SelectMany(c => c.ClaimAppointmentLinks)
             .Where(link => link.DateDeleted == null)
             .Select(link => link.AppointmentId)
             .FirstOrDefaultAsync();

            if (appointmentId <= 0)
                return;

            var appointment = await _rethinkServices.GetAppointmentAsync(appointmentId);
            appointment.ChildProfileAuthorizationBillingCode = await _rethinkServices.GetChildProfileAuthBillingCodeForAppointment(appointment.staffAccountInfoId, appointment.clientId.Value, appointment.procedureCodeId);

            if (appointment.ChildProfileAuthorizationBillingCode != null)
            {
                var childProfileAuthorizationBillingCode = await _rethinkServices.GetChildProfileAuthBillingCodeForAppointment(accountInfoId, appointment.clientId ?? 0, appointment.procedureCodeId);
                if (childProfileAuthorizationBillingCode != null)
                {
                    childProfileAuthorizationBillingCode.AppointmentProviderBillingCode = await _rethinkServices.GetProviderBillingCode(accountInfoId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId ?? 0);
                    childProfileAuthorizationBillingCode.ChildProfileAuthorization = await _rethinkServices.GetChildProfileAuthorizationByClientId(appointment.staffAccountInfoId, appointment.clientId ?? 0, appointment.ChildProfileAuthorizationBillingCode.childProfileAuthorizationId);
                    childProfileAuthorizationBillingCode.ChildProfileAuthorization.ChildProfileAuthorizationDiagnosisCodes = await _rethinkServices.GetChildProfileAuthorizationDiagnosisCodesAsync(appointment.staffAccountInfoId, appointment.clientId.Value, childProfileAuthorizationBillingCode.ChildProfileAuthorization.childProfileDiagnosisId, childProfileAuthorizationBillingCode.ChildProfileAuthorization.id);

                    if (childProfileAuthorizationBillingCode.ChildProfileAuthorization.ChildProfileAuthorizationDiagnosisCodes.Count > 0)
                    {
                        foreach (var diagnosis in childProfileAuthorizationBillingCode.ChildProfileAuthorization.ChildProfileAuthorizationDiagnosisCodes)
                        {
                            var claimDiagnosisCode = new ClaimDiagnosisCodeEntity
                            {
                                ClaimId = claimId,
                                DiagnosisId = diagnosis.diagnosisId,
                                Order = diagnosis.order,
                                IncludeOnClaims = diagnosis.includeOnClaims,
                            };

                            MarkCreated(claimDiagnosisCode, memberId);
                            var check = _billingClaimDiagnosisCodeEntityRepository.Query().Where(x => x.ClaimId == claimId && x.DiagnosisId == diagnosis.diagnosisId).FirstOrDefault();
                            if (check == null)
                            {
                                await _billingClaimDiagnosisCodeEntityRepository.AddAsync(claimDiagnosisCode);
                                await _billingClaimDiagnosisCodeEntityRepository.SaveChangesAsync();
                            }
                        }
                    }

                    //update billing rate if changed
                    if (childProfileAuthorizationBillingCode.AppointmentProviderBillingCode != null)
                    {
                        var ClaimChargeEntries = await _chargeEntryRepository.Query().FirstOrDefaultAsync(x => x.ClaimId == claimId && x.BillingCodeId == childProfileAuthorizationBillingCode.AppointmentProviderBillingCode.id && x.DateDeleted == null);
                        if (ClaimChargeEntries != null)
                        {
                            var providerBillingCode = await GetProviderBillingCode(appointment, childProfileAuthorizationBillingCode);
                            if (providerBillingCode != null)
                            {
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
                                            await _rethinkServices.GetProviderBillingCodeCredential(accountInfoId, appointment.providerBillingCodeId ?? 0, appointment.providerBillingCodeCredentialId);
                                }
                                else if (appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId != null)
                                {
                                    hcProviderBillingCodeCredential = appointment?.ProviderBillingCodeCredential ??
                                            await _rethinkServices.GetProviderBillingCodeCredential(accountInfoId, appointment.ChildProfileAuthorizationBillingCode.providerBillingCodeId ?? 0, appointment.providerBillingCodeCredentialId);
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

                                ClaimChargeEntries.Charges = unitRate;
                                ClaimChargeEntries.UnitRate = unitRate;
                                MarkUpdated(ClaimChargeEntries, memberId);
                                _chargeEntryRepository.Update(ClaimChargeEntries);
                                await _chargeEntryRepository.SaveChangesAsync();
                            }
                        }
                    }
                }
            }
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
                await LogAppointmentProcessionError(appointment.id, appointment.modifiedBy.GetValueOrDefault(), "Authorization missing for childProfile.");
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

        // Updated: include memberId instead of using appointmentId as CreatedBy
        private async Task LogAppointmentProcessionError(int appointmentId, int memberId, string errorMessage)
        {
            var link = _linkRepository.Query().FirstOrDefault(l => l.AppointmentId == appointmentId && !l.DateDeleted.HasValue);
            // Creating the AppointmentClaimProcessingErrorEntity
            var claimSyncError = new AppointmentClaimProcessingErrorEntity()
            {

                ClaimAppointmentLinkId = link.Id,
                ErrorMessage = errorMessage,
                DateCreated = EstDateTime

            };

            // Inactive all previous errors for the same link

            var existingErrors = _appointmentClaimProcessingErrorRepository.Query()
                .Where(e => e.ClaimAppointmentLinkId == link.Id && !e.DateDeleted.HasValue)
                .ToList();

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

                //#DBMIGRATION
                //<
                var providerLocationData = await _rethinkServices.GetProviderLocation(claim.AccountInfoId, facilityId);
                if (providerLocationData != null)
                {
                    if (providerLocationData.isBillingLocation)
                    {
                        claim.ProviderLocationId = providerLocationData.id;
                    }
                    else
                    {
                        var mainLocation = await _rethinkServices.GetMainLocation(claim.AccountInfoId);
                        claim.ProviderLocationId = mainLocation?.id ?? 0;
                    }
                }
                //claim.ProviderLocationId = authorization.BillingProviderId;
                //>
            }
            else
            {
                claim.AuthorizationNumber = null;
                claim.AuthorizationId = null;
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


            // #DBMIGRATION
            //< GET THE MAPPING ID FROM THE MAPPING TABLE
            var funderMappings = await _rethinkServices.GetChildProfileFunderMappings(claim.AccountInfoId, appointment.clientId ?? 0);
            if (funderMappings != null)
            {
                var clientFunder = funderMappings.data.Where(x => x.funderId == appointment.funderId).FirstOrDefault();
                claim.ClientFunderId = clientFunder != null ? clientFunder.id : 0;
            }
            //>

            claim.ClientFunderServiceLineId = childProfileFunderServiceLineMapping.id;

            claim.ReleaseOfInformationConfirmationTypeId = funderMapping.releaseOfInformationConfirmationTypeId;
            claim.AuthorizedPaymentConfirmationTypeId = funderMapping.authorizedPaymentConfirmationTypeId;
            claim.BenefitAssignmentId = (funderMapping.isAutismCoveredBenefit == true || funderMapping.isAutismCoveredBenefit == null) ? 1 : 2;

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

        private async Task PopulateCharges(BillingCodeData providerBillingCode, Rethink.Services.Common.Models.AppointmentRethinkModel appointment, ClaimEntity claimToUpdate, AppointmentClientAuthBillingCodeModel childProfileAuthorizationBillingCode, IClaimHistoryService claimHistoryService, IClaimSyncService claimSyncService)
        {
            var newLink = new ClaimAppointmentLinkEntity();
            var claimDiagnosisCode = await claimSyncService.AddDiagnosisCodes(claimToUpdate, childProfileAuthorizationBillingCode, appointment.serviceId);

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

                newLink = await _linkRepository.Query().Where(x => x.ClaimId == claimToUpdate.Id && x.AppointmentId == appointment.id).FirstOrDefaultAsync();
                newLink.ClaimChargeEntriesId = chargeEntry.Id;
                newLink.ClaimAppointmentLinkChargeEntryId = claimAppointmentLinkChargeEntry.Id;
                _linkRepository.Update(newLink);

                await claimHistoryService.AddAsync(new ClaimHistorySaveModel
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

                newLink = await _linkRepository.Query().Where(x => x.ClaimId == claimToUpdate.Id && x.AppointmentId == appointment.id).FirstOrDefaultAsync();
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

                await claimHistoryService.AddAsync(new ClaimHistorySaveModel
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

                    newLink = await _linkRepository.Query().Where(x => x.ClaimId == claimToUpdate.Id && x.AppointmentId == appointment.id).FirstOrDefaultAsync();
                    if (newLink != null)
                    {
                        newLink.ClaimChargeEntriesId = chargeEntry.Id;
                        newLink.ClaimAppointmentLinkChargeEntryId = claimAppointmentLinkChargeEntry.Id;
                        _linkRepository.Update(newLink);
                    }

                    await claimHistoryService.AddAsync(new ClaimHistorySaveModel
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

                    newLink = await _linkRepository.Query().Where(x => x.ClaimId == claimToUpdate.Id && x.AppointmentId == appointment.id).FirstOrDefaultAsync();
                    newLink.ClaimChargeEntriesId = chargeEntryToUpdate.Id;
                    newLink.ClaimAppointmentLinkChargeEntryId = claimAppointmentLinkChargeEntry.Id;
                    _linkRepository.Update(newLink);

                    await claimHistoryService.AddAsync(new ClaimHistorySaveModel
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

    }
}
