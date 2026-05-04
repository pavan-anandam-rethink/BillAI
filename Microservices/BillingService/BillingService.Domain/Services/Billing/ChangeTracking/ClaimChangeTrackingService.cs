using BillingService.Domain.Interfaces.Billing.ChangeTracking;
using BillingService.Domain.Models;
using BillingService.Domain.Models.Claims;
using BillingService.Domain.Models.PaymentPosting;
using Rethink.Services.Common.Entities.Billing.Claim;
using Rethink.Services.Common.Enums.Billing;
using Rethink.Services.Common.Models.Claim.History;
using Rethink.Services.Common.Utils;
using Rethink.Services.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BillingService.Domain.Services.Billing.ChangeTracking
{
    public class ClaimChangeTrackingService : IClaimChangeTrackingService
    {
        private const string ValuesSeparator = ", ";
        private readonly List<ClaimChangeTrackingModel> changes;
        private string? _impersonationUserName;

        private readonly IClaimHistoryService _claimHistoryService;

        public ClaimChangeTrackingService(IClaimHistoryService claimHistoryService)
        {
            _claimHistoryService = claimHistoryService;
            changes = new List<ClaimChangeTrackingModel>();
        }

        private DateTime ActionDate { get; set; }

        private IdWithUserInfo ClaimUserInfo { get; set; }

        private ClaimAction ClaimAction { get; set; }

        private ClaimHistoryAction ClaimHistoryAction { get; set; }

        public void Initialize(IdWithUserInfo claimUserInfo, ClaimAction action, ClaimHistoryAction historyAction, DateTime actionDate)
        {
            ClaimAction = action;
            ClaimHistoryAction = historyAction;
            ActionDate = actionDate;
            ClaimUserInfo = claimUserInfo;
        }
        public void TrackAttachmentsChanges(ClaimAttachmentEntity attachment, RenameAttachmentModelWithUserInfo saveModel)
        {
            TrackAttachementsChangesForClaim(attachment, saveModel);
        }
        public void TrackChangesForCharges(ClaimChargeEntryEntity charge, UpdateBillingClaimDetailsModel saveModel)
        {
            TrackChargeEntries(charge, saveModel);
        }

        public void TrackAttachementsChangesForClaim(ClaimAttachmentEntity attachment, RenameAttachmentModelWithUserInfo saveModel)
        {
            var oldAttachmentFileName = attachment.FileName;
            TrackValue(ClaimHistoryField.Attachments, oldAttachmentFileName, saveModel.FileName);
        }

        public void TrackChanges(ClaimEntity claim, UpdateClaimDetailsModel saveModel)
        {
            // Claim Details Tab
            TrackChargeDetailSummary(claim, saveModel);
            TrackProviders(claim, saveModel);
            TrackAdditinalInfo(claim, saveModel);
            //if (claim.ChildProfileAuthorization != null)
            //{
            TrackClientInfo(claim, saveModel);
            //}
        }

        public void TrackChargeEntries(ClaimChargeEntryEntity charge, UpdateBillingClaimDetailsModel saveModel)
        {
            //Units
            var oldUnitsValue = charge.Units;
            if (((int)((oldUnitsValue % 1) * 100)) != ((int)((saveModel.Units % 1) * 100)) || ((int)oldUnitsValue) != ((int)saveModel.Units))
            {
                // added by Saurabh - commented by Chetan - To Resolve bug 233604
                //if (((int)((saveModel.Units % 1) * 100)) == 0)
                //{
                //    saveModel.Units += (oldUnitsValue % 1);
                //}
                TrackValue(ClaimHistoryField.Units, oldUnitsValue.ToString(), saveModel.Units.ToString());
            }

            //Per Units Charge
            var oldPerUnitsCharge = charge.UnitRate;
            if (((int)((oldPerUnitsCharge % 1) * 100)) != ((int)((saveModel.PerUnitsCharge % 1) * 100)) || ((int)oldPerUnitsCharge) != ((int)saveModel.PerUnitsCharge))
            {
                TrackValue(ClaimHistoryField.PerUnitsCharge, oldPerUnitsCharge.ToString(), saveModel.PerUnitsCharge.ToString());
            }

            //Modifier 1
            var oldModifier1 = charge.Modifier1 ?? ""; if (saveModel.Modifier1 != charge.Modifier1) { TrackValue(ClaimHistoryField.Modifier1, oldModifier1, saveModel?.Modifier1); }
            //Modifier 2
            var oldModifier2 = charge.Modifier2 ?? ""; if (saveModel.Modifier2 != charge.Modifier2) { TrackValue(ClaimHistoryField.Modifier2, oldModifier2, saveModel?.Modifier2); }
            //Modifier 3 
            var oldModifier3 = charge.Modifier3 ?? ""; if (saveModel.Modifier3 != charge.Modifier3) { TrackValue(ClaimHistoryField.Modifier3, oldModifier3, saveModel.Modifier3); }
            //Modifier 4
            var oldModifier4 = charge.Modifier4 ?? ""; if (saveModel.Modifier4 != charge.Modifier4) { TrackValue(ClaimHistoryField.Modifier4, oldModifier4, saveModel?.Modifier4); }
        }

        public void TrackChangesForModifiers(ClaimChargeEntryEntity charge, UpdateChargeModifiersModel saveModel)
        {
            //Modifier 1
            var oldModifier1 = charge.Modifier1 ?? ""; if (saveModel.Modifier1 != charge.Modifier1) { TrackValue(ClaimHistoryField.Modifier1, oldModifier1, saveModel?.Modifier1); }
            //Modifier 2
            var oldModifier2 = charge.Modifier2 ?? ""; if (saveModel.Modifier2 != charge.Modifier2) { TrackValue(ClaimHistoryField.Modifier2, oldModifier2, saveModel?.Modifier2); }
            //Modifier 3 
            var oldModifier3 = charge.Modifier3 ?? ""; if (saveModel.Modifier3 != charge.Modifier3) { TrackValue(ClaimHistoryField.Modifier3, oldModifier3, saveModel.Modifier3); }
            //Modifier 4
            var oldModifier4 = charge.Modifier4 ?? ""; if (saveModel.Modifier4 != charge.Modifier4) { TrackValue(ClaimHistoryField.Modifier4, oldModifier4, saveModel?.Modifier4); }
        }

        public async Task SaveChangesAsync( string? ImpersonationUserName)
        {
            _impersonationUserName = ImpersonationUserName;
            await SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        { 
            foreach (var field in changes)
            if ((field.NewValue != null || field.NewValue != "") && (field.OldValue != null || field.OldValue != "")) await AddClaimHistory(field);
        }

        private void TrackValue(ClaimHistoryField field, string oldValue = null, string newValue = null, ClaimHistoryAction claimHistoryAction = 0)
        {
            if (!(string.IsNullOrEmpty(oldValue) && string.IsNullOrEmpty(newValue)))
            {
                if (!Equals(oldValue, newValue))
                {
                    Add(new ClaimChangeTrackingModel(ClaimAction, claimHistoryAction > 0 ? claimHistoryAction : ClaimHistoryAction, field)
                    {
                        OldValue = oldValue,
                        NewValue = newValue,
                    });
                }
            }
        }

        private void TrackSubReasonValue(ClaimHistoryField field, string oldValue = null, string newValue = null)
        {
            if (!(string.IsNullOrEmpty(oldValue) || string.IsNullOrEmpty(newValue)))
            {
                if (!Equals(oldValue, newValue))
                {
                    Add(new ClaimChangeTrackingModel(ClaimAction, ClaimHistoryAction.ClaimUpdated, field)
                    {
                        OldValue = oldValue,
                        NewValue = newValue,
                    });
                }
            }
        }


        private void TrackBillingProviderValue(ClaimHistoryField field, string oldId = null, string newId = null, string oldValue = null, string newValue = null)
        {
            if (!(string.IsNullOrEmpty(oldId) && string.IsNullOrEmpty(newId)))
            {
                if (!Equals(oldId, newId))
                {
                    Add(new ClaimChangeTrackingModel(ClaimAction, ClaimHistoryAction.ClaimUpdated, field)
                    {
                        OldValue = oldValue,
                        NewValue = newValue,
                    });
                }
            }
        }


        private void TrackCollectionValues(ClaimHistoryField field, List<string> oldValues, List<string> newValues)
        {
            TrackValue(field, string.Join(ValuesSeparator, oldValues), string.Join(ValuesSeparator, newValues));
        }

        private void Add(ClaimChangeTrackingModel historyEntity)
        {
            changes.Add(historyEntity);
        }

        private void TrackProviders(ClaimEntity claim, UpdateClaimDetailsModel saveModel)
        {
            // BillingProvider            
            TrackBillingProviderValue(ClaimHistoryField.BillingProvider, claim.ProviderLocation?.id.ToString(), saveModel.BillingProviderId.ToString(), claim.ProviderLocation?.name.ToString(), saveModel.BillingProvider.ToString());

            // ReferringProvider
            var oldReferringProviderEntity = claim.ReferringProvider;
            var oldReferringProviderName = oldReferringProviderEntity?.ReferringProvider.name.firstName != null ? oldReferringProviderEntity?.ReferringProvider.name.firstName + ' ' + oldReferringProviderEntity?.ReferringProvider.name.lastName : oldReferringProviderEntity?.ReferringProvider.name.lastName;
            TrackValue(ClaimHistoryField.ReferringProvider, oldReferringProviderName, saveModel.ReferringProvider, ClaimHistoryAction.ClaimUpdated);

            // RenderingProvider
            var oldRenderingProviderEntity = claim.RenderingStaffMember;
            var oldRenderingProviderName = oldRenderingProviderEntity?.firstName != null ? oldRenderingProviderEntity?.firstName + (!String.IsNullOrEmpty(oldRenderingProviderEntity.middleName) ? " " + oldRenderingProviderEntity.middleName + " " : " ") + oldRenderingProviderEntity?.lastName : oldRenderingProviderEntity?.lastName;
            TrackValue(ClaimHistoryField.RenderingProvider, oldRenderingProviderName, saveModel.RenderingProvider, ClaimHistoryAction.ClaimUpdated);

            // ServiceProvider
            TrackValue(ClaimHistoryField.ServiceFacility, claim.ServiceLocation?.name, saveModel.ServiceFacility, ClaimHistoryAction.ClaimUpdated);
        }

        private void TrackChargeDetailSummary(ClaimEntity claim, UpdateClaimDetailsModel saveModel)
        {
            // PlaceOfService
            var currentPosId = claim.LocationCode.id;
            var selectedPosId = saveModel.PlaceOfServiceId;
            //TrackValue(ClaimHistoryField.PlaceOfService, claim.LocationCode.Description, saveModel.PlaceOfService);

            //modifiers
            //var oldModifiers = claim.ClaimChargeEntries.
            //units

            //perunitsCharge

        }

        private void TrackAdditinalInfo(ClaimEntity claim, UpdateClaimDetailsModel saveModel)
        {

            // BenefitsAssignmentCertificationIndicator
            var oldBenefitsAssignment = GetConfirmationType(claim.BenefitAssignmentId);
            var newBenefitsAssignment = GetConfirmationType(saveModel.BenefitAssignmentId);
            TrackValue(ClaimHistoryField.BenefitsAssignmentCertificationIndicator, oldBenefitsAssignment, newBenefitsAssignment, ClaimHistoryAction.ClaimUpdated);

            // AuthorizedReleaseInfo            
            var oldPatientReleaseAgreement = GetConfirmationType(saveModel.PatientReleaseAgreementId);
            var newPatientReleaseAgreement = GetConfirmationType(claim.ReleaseOfInformationConfirmationTypeId);
            TrackValue(ClaimHistoryField.AuthorizedReleaseOfInfo, newPatientReleaseAgreement, oldPatientReleaseAgreement, ClaimHistoryAction.ClaimUpdated);

            // AuthorizedPayment
            var oldPatientSignature = GetConfirmationType(claim.AuthorizedPaymentConfirmationTypeId);
            var newPatientSignature = GetConfirmationType(saveModel.AuthorizePaymentId);
            TrackValue(ClaimHistoryField.AuthorizePayment, oldPatientSignature, newPatientSignature, ClaimHistoryAction.ClaimUpdated);


            // SubmissionReason
            var newSubmissionReason = (ClaimFrequencyType)saveModel.SubmissionReasonId;
            TrackValue(ClaimHistoryField.SubmissionReason, claim.FrequencyTypeId?.GetDescription(), newSubmissionReason.GetDescription(), ClaimHistoryAction.ClaimUpdated);

            TrackValue(ClaimHistoryField.OriginalClaim, claim.OriginalClaim, saveModel.OriginalClaim, ClaimHistoryAction.ClaimUpdated);
            TrackValue(ClaimHistoryField.Note, claim.Note, saveModel.Note, ClaimHistoryAction.ClaimUpdated);
        }

        private void TrackClientInfo(ClaimEntity claim, UpdateClaimDetailsModel saveModel)
        {
            //CHANGED THE LOGIC TO TRACK THE CHANGES IN DIAGNOSIS CODE 
            //    CHECKED BY CHECKING THE PROVIOUS SAVED VALUE AND NEW INCOMING VALUE
            //var childProfileAuthEntity = claim.ChildProfileAuthorization;
            //var currentDiagnosis = childProfileAuthEntity.ChildProfileAuthorizationDiagnosisCodes.Where(x => x.DateDeleted == null).ToList();


            var oldDiagnosisCodes = claim.ClaimDiagnosisCodes.OrderBy(x => x.Order).Select(x => x.Diagnosis.DiagnosisCode).ToList();
            var newDiagnosisCodes = saveModel.DiagnosisCodes.OrderBy(x => x.Order).Select(x => x.DiagnosisCode).ToList();

            TrackCollectionValues(ClaimHistoryField.DiagnosisCodes, oldDiagnosisCodes, newDiagnosisCodes);
        }

        private async Task AddClaimHistory(ClaimChangeTrackingModel trackingModel)
        {
            var saveModel = new ClaimHistoryFieldSaveModel
            {
                ClaimId = ClaimUserInfo.Id,
                MemberId = ClaimUserInfo.MemberId,
                Mode = ClaimActionMode.User,
                ClaimAction = trackingModel.ClaimAction,
                ClaimHistoryAction = trackingModel.ClaimHistoryAction,
                ClaimHistoryField = trackingModel.TrackingField,
                OldValue = trackingModel.OldValue,
                NewValue = trackingModel.NewValue,
                ActionDate = ActionDate,
                ImpersonationUserName = _impersonationUserName ?? ""
            };
            await _claimHistoryService.AddAsync(saveModel);


        }

        private string GetConfirmationType(int? typeId)
        {
            switch (typeId)
            {
                case 1: return "Yes";
                case 2: return "No";
                case 3: return "Not applicable";
                default: return null;
            }
        }
    }
}
