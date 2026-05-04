import { Component, forwardRef, Input, Renderer2, ViewChild } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { BackCancelService } from '@app/billing/services/back-cancel.service';
import { makeAllControlsTouched } from '@core/common/mark-every-controls-as-touched';
import { AccountPermissions } from '@core/enums/account';
import { ClaimDetailsInfoModel, ClaimDetailsInfoUpdateModel, ClaimDetailsModel, ClaimOptions, ClaimUpdateDetailsModels, ClaimCreateInfoGetModel } from '@core/models/billing';
import { ClientBillingCode } from '@core/models/billing/billing-code';
import { BasicOption, ConfirmDialog, NotifyDialog } from '@core/models/common';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { AppointmentService, ClaimService } from '@core/services/billing';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { CurrencyPipe, Location } from '@angular/common';
import { SidebarService } from '@app/shared/components/sidebar';
import { EncounterAttachmentsComponent } from '../encounter-attachments/encounter-attachments.component';

import { EncounterDetailsChargeDetailSummaryComponent } from './encounter-details-charge-detail-summary/encounter-details-charge-detail-summary.component';
import { EncounterDetailsProvidersComponent } from './encounter-details-providers/encounter-details-providers.component';

import { EncounterErrorsAlertsComponent } from '../encounter-errors-alerts/encounter-errors-alerts.component';
import { ActivatedRoute, Router } from "@angular/router";
import { ConfirmationDialogComponent } from '@app/shared/components/confirmation-dialog/confirmation-dialog.component';
import { ClaimChargeDetailsShared } from '../../shared/claim-charge-details/claim-charge-details-shared';
import { ClaimUpdateModel } from '@core/models/billing/claim-details-model';
import { ClaimUpdateService } from '@core/services/billing/claim-update.service';
import { NotificationService } from '@progress/kendo-angular-notification';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { ClientsService } from '@core/services/clients/clients.service';
@Component({
  selector: 'app-encounter-details',
  templateUrl: './encounter-details.component.html',
  styleUrls: ['./encounter-details.component.css']
})
export class EncounterDetailsComponent {

    @Input() claimId: number;
    @Input() childProfileId: number;
    @Input() isManualClaim: boolean;
    @Input() claimIdentifier: string;
    @ViewChild(EncounterDetailsChargeDetailSummaryComponent) chargeDetailsSummary: EncounterDetailsChargeDetailSummaryComponent;
    @ViewChild(EncounterDetailsProvidersComponent) providersComponent: EncounterDetailsProvidersComponent;
    @ViewChild("cancelConfirmationAlert") cancelConfirmationAlert: ConfirmationDialogComponent;

  isChargeEntryValid: boolean = true;
  isChargeEntryDirty: boolean = false;

  cancelConfirmation = new ConfirmDialog(false, "Confirmation",
    "Any changes you've made will not be saved, are you sure you want to Cancel?", "Yes", "No");

  private unsubscribe = new Subject<void>();
  private options: ClaimOptions | null = null;

    notifyDialog: NotifyDialog = new NotifyDialog(false, '', '');
    isDisabledSaveBtn = true;
    private cacheFormValue: any = null;
    private manualBillingProviderDirty = false;
    private manualBillingProviderValid = true;
    private backCancelUpdate$ = new Subject<boolean>();

  // permission
  public canEdit: boolean = false;
  public canApprove: boolean = false;
  public canClose: boolean = false;
  public canReopen: boolean = false;

  public claimForm = new FormGroup({
    renderingProviderId: new FormControl(1, Validators.required),
    referringProviderId: new FormControl(1),
    serviceFacilityId: new FormControl(1),
    billingProviderId: new FormControl(1),
    benefitsAssignmentId: new FormControl(1, Validators.required),
    submissionReason: new FormControl(1, Validators.required),
    placeOfService: new FormControl("", Validators.required),
    patientReleaseAgreement: new FormControl(1),
    authorizePayment: new FormControl(1),
    submissionCode: new FormControl(""),
    originalClaim: new FormControl(""),
    note: new FormControl(""),

    diagnosisCodes: new FormControl([], Validators.minLength(1)),
    locationCodeId: new FormControl(""),
    claimId: new FormControl(""),
  })

  public claim: ClaimDetailsInfoModel;
  billingCodes: ClientBillingCode[] = [];

  serviceProviders: BasicOption[] = [];
  referringProviders: BasicOption[] = [];
  billingProviders: BasicOption[] = [];
  renderingProviders: BasicOption[] = [];
  locationCodes: BasicOption[] = [];

  claimChargeDetailsShared: ClaimChargeDetailsShared;
  diagnosisCode: any;

  private onLinkSubscription: Subscription | null = null;

  constructor(
    private accountService: AccountMemberService,
    private claimsService: ClaimService,
    private appointmentService: AppointmentService,
    private claimUpdateService: ClaimUpdateService,
    private backCancelService: BackCancelService,
    private currencyPipe: CurrencyPipe,
    private sidebarService: SidebarService,
    private router: Router,
    private readonly route: ActivatedRoute,
    private renderer: Renderer2,
    private notificationHandler: NotificationHandlerService,
    private clientsService: ClientsService

    ) {
        this.accountService
            .accountMemberSettings
            .subscribe((x) => {
                if (x) {
                    this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingEdit);
                    // this.canApprove = this.accountService.checkPermissionLevel(AccountPermissions.BillingAddEditApproveSubmitClaims);
                    // this.canClose = this.accountService.checkPermissionLevel(AccountPermissions.BillingAddEditApproveSubmitClaims);
                    // this.canReopen = this.accountService.checkPermissionLevel(AccountPermissions.BillingAddEditApproveSubmitClaims);
                }
            });
        this.claimChargeDetailsShared = new ClaimChargeDetailsShared(claimsService, currencyPipe, router, route, accountService, notificationHandler);
        this.addFieldListener();
        this.setupBackCancelDebounce();
    }

    private setupBackCancelDebounce(): void {
        this.backCancelUpdate$.pipe(
            takeUntil(this.unsubscribe),
            debounceTime(300),
            distinctUntilChanged()
        ).subscribe((saveBtnEnabled) => {
            this.backCancelService.onClaimFormChange(saveBtnEnabled);
        });
    }

    ngOnInit(): void {

    if (this.claimId) {
      this.claimsService.GetOptions(this.claimId).subscribe((options: ClaimOptions) => {
        this.options = options;
        if (this.options != null) {
          this.renderingProviders = this.options.renderingProviders;
          this.referringProviders = this.options.referringProviders;
          this.serviceProviders = this.options.serviceFacilities;
          this.billingProviders = this.options.billingProviders;
          this.locationCodes = this.options.locationCodes;
        }
      }, error => {
        this.router.navigate(['/billing/claims/list']);
      });

      this.loadClaimInfo();

      this.onLinkSubscription = this.appointmentService.onLinkClaimInfo.subscribe((result) => {
        this.claim.dateOfServiceEnd = new Date(result.endDate);
        this.claim.dateOfServiceStart = new Date(result.startDate);
      });
    }
  }

  addFieldListener(): void {
    this.claimForm.valueChanges.pipe(takeUntil(this.unsubscribe)).subscribe(value => {
      let formChanged = false;
      Object.keys(this.cacheFormValue).forEach(k => {
        if (this.cacheFormValue[k] || undefined !== value[k] || undefined) {
          formChanged = true;
        }
      });

      if (formChanged) {
        this.updateSaveButtonState();
      }
    });
  }

    onManualBillingProviderFormChanged(event: { dirty: boolean; valid: boolean }): void {
        this.manualBillingProviderDirty = event.dirty;
        this.manualBillingProviderValid = event.valid;
        this.updateSaveButtonState();
    }

    private updateSaveButtonState(): void {
        const isClaimFormDirty = this.claimForm.dirty || this.cacheFormValue['diagnosisCodes'] != this.claimForm.controls['diagnosisCodes'].value;
        const isAnyFormDirty = isClaimFormDirty || this.manualBillingProviderDirty;
        
        const isAllFormsValid = this.claimForm.valid && this.isManualBillingProviderFormValid();
        
        const newDisabledState = !(isAllFormsValid && isAnyFormDirty);
        
        // Update save button state immediately
        this.isDisabledSaveBtn = newDisabledState;
        // Debounce the backCancelService call to prevent multiple API calls
        this.backCancelUpdate$.next(!this.isDisabledSaveBtn);
    }

    private isManualBillingProviderFormValid(): boolean {
        // Check if "Other" billing provider is selected (id = 0)
        const billingProviderId = this.claimForm.get('billingProviderId')?.value;
        const isOtherSelected = billingProviderId === 0;
        
        if (!isOtherSelected) {
            return true;
        }
        
        // When "Other" is selected, check form validity directly
        if (this.providersComponent?.showManualBillingProvider) {
            return this.providersComponent.manualBillingProviderForm?.valid === true;
        }
        
        // Fallback to cached value
        return this.manualBillingProviderValid;
    }

    ngOnDestroy(): void {
        this.unsubscribe.next();
        this.unsubscribe.complete();
        Array.from(document.getElementsByClassName('k-popup')).forEach(
            function (element) {
                if (element && element.parentElement) {
                    element.parentElement.remove()
                };
            }
        );
        this.backCancelService.onClaimFormChange(false);
        if (this.onLinkSubscription !== null) {
            this.onLinkSubscription.unsubscribe();
        }
    }

  isClaimUpdated = false;
  isChargeEntryUpdated = false;

  @ViewChild(forwardRef(() => EncounterDetailsChargeDetailSummaryComponent)) encounterDetailsChargeDetailSummaryComponent: EncounterDetailsChargeDetailSummaryComponent;
  updateClaimDetails() {
    // Always attempt to send charge entry data (saving all grid rows) if grid has any rows
    if (this.chargeDetailsSummary?.getIsChargeEntryUpdated()) {
      this.isChargeEntryUpdated = true;
    }
    // Preserve claim header update behavior (only if form changed & button enabled)
    if (!this.isDisabledSaveBtn) {
      this.isClaimUpdated = true;
    }
    this.updateClaimInfo();
  }

  updateClaimInfo() {
    // Allow saving charge entry lines even if claim header form untouched / invalid; only block if both invalid and no charge lines.
    if (!this.claimForm.valid && !this.isChargeEntryUpdated) {
      makeAllControlsTouched(this.claimForm);
      return;
    }

    // Validate manual billing provider form when "Other" is selected
    const billingProviderId = this.claimForm.get('billingProviderId')?.value;
    if (billingProviderId === 0 && this.providersComponent && !this.providersComponent.manualBillingProviderForm.valid) {
      makeAllControlsTouched(this.providersComponent.manualBillingProviderForm);
      return;
    }

    this.isDisabledSaveBtn = true;

    var claimUpdateModel: ClaimUpdateModel = {
      isClaimUpdated: false,
      isChargeEntryUpdated: false,
      claimModel: null,
      chargeEntryModel: null,
      impersonationUserName: null
    }

    if (this.isClaimUpdated) {
      const referringProviderId = this.claimForm.get("referringProviderId")!.value;
      const renderingProviderId = this.claimForm.get("renderingProviderId")!.value;
      const billingProviderId = this.claimForm.get("billingProviderId")!.value;
      const serviceFacilityId = this.claimForm.get("serviceFacilityId")!.value;
      const billingProvider = this.billingProviders.find((x) => x.id == billingProviderId);
      const placeOfService = this.claimForm.get("placeOfService");
      const referringProvider = this.referringProviders.find((x) => x.id == referringProviderId);
      const renderingProvider = this.renderingProviders.find((x) => x.id == renderingProviderId);
      const serviceFacility = this.serviceProviders.find((x) => x.id == serviceFacilityId);
      let original_claim = this.claimForm.get("originalClaim")!.value == undefined ? this.claimForm.get("originalClaim")!.value : this.claimForm.get("originalClaim")!.value.trim();
      let note = this.claimForm.get("note")!.value == undefined ? this.claimForm.get("note")!.value : this.claimForm.get("note")!.value.trim();

            // Check if "Other" is selected (id = 0)
            const isOtherBillingProvider = billingProviderId === 0;

            const model: ClaimDetailsInfoUpdateModel = {
                claimId: this.claimId,
                diagnosisCodes: this.claimForm.get("diagnosisCodes")!.value,
                placeOfServiceId: this.claim.placeOfServiceId,
                referringProviderId: referringProviderId,
                renderingProviderId: renderingProviderId,
                billingProviderId: isOtherBillingProvider ? 0 : this.mapBillingProviderId(billingProviderId),
                serviceFacilityId: serviceFacilityId,

                placeOfService: placeOfService ? placeOfService.value : '',
                billingProvider: isOtherBillingProvider ? 'Other' : (billingProvider ? billingProvider.name : ''),
                referringProvider: referringProvider ? referringProvider.name : '',
                renderingProvider: renderingProvider ? renderingProvider.name : '',
                serviceFacility: serviceFacility ? serviceFacility.name : '',

        submissionReasonId: this.claimForm.get("submissionReason")!.value,
        patientReleaseAgreementId: this.claimForm.get("patientReleaseAgreement")!.value,
        authorizePaymentId: this.claimForm.get("authorizePayment")!.value,
        benefitAssignmentId: this.claimForm.get("benefitsAssignmentId")!.value,
        originalClaim: original_claim,
        note: note
      };

            claimUpdateModel.claimModel = model;
            claimUpdateModel.isClaimUpdated = true;

            // Add billing provider request if "Other" is selected
            if (isOtherBillingProvider && this.providersComponent) {
                const billingProviderData = this.providersComponent.getBillingProviderOtherData();
                if (billingProviderData) {
                    claimUpdateModel.billingProviderRequest = {
                        claimId: this.claimId,
                        billingProvider: billingProviderData
                    };
                }
            }
        }

    const performUpdate = () => {
      if (this.isChargeEntryUpdated) {
        claimUpdateModel.chargeEntryModel = this.chargeDetailsSummary.getChargeEntryUpdateModel();
        claimUpdateModel.isChargeEntryUpdated = true;
      }

      this.claimsService.updateClaimInfo(claimUpdateModel).pipe(takeUntil(this.unsubscribe))
        .subscribe((result) => {
          let { dateOfServiceEnd, dateOfServiceStart } = result;
          dateOfServiceStart = new Date(dateOfServiceStart);
          dateOfServiceEnd = new Date(dateOfServiceEnd);
          this.notificationHandler.showNotificationSuccess("Claim details updated successfully");
          this.claimChargeDetailsShared.navigateToDashboard();
          this.cacheFormValue = Object.assign(this.claimForm.value);
        }, () => { }, () => {
          this.isDisabledSaveBtn = false;
        });
    };

    // Single pass: backend handles both create (id=0) and update (>0) lines
    performUpdate();

  }

  updateChargeLines(): void {
    if (this.claimChargeDetailsShared.chargeDetailsArrayForm.invalid || this.claimChargeDetailsShared.claimsToUpdate.length < 1) {
      return;
    }

    let updateModel: ClaimUpdateDetailsModels = {
      billingClaimDetailsModels: [],
      memberId: this.accountService.memberDetails.memberId
    };
    this.claimChargeDetailsShared.claimsToUpdate.forEach(itemId => {
      let itemToUpdate: ClaimDetailsModel = this.claimChargeDetailsShared.chargeDetailsData.find(x => x.id == itemId);
      itemToUpdate.claimId = this.claimId;

      const updatedLineModifiers = this.claimChargeDetailsShared.modifiersToUpdate.find((item) => item.id === itemToUpdate.id)
      if (updatedLineModifiers !== undefined) {
        itemToUpdate.modifier1 = updatedLineModifiers.modifier1;
        itemToUpdate.modifier2 = updatedLineModifiers.modifier2;
        itemToUpdate.modifier3 = updatedLineModifiers.modifier3;
        itemToUpdate.modifier4 = updatedLineModifiers.modifier4;
      }

      updateModel.billingClaimDetailsModels.push(itemToUpdate);
    })

    this.claimsService.updateBillingClaimDetails(updateModel).pipe(takeUntil(this.claimChargeDetailsShared.unsubscribeAll$))
      .subscribe((result: ClaimDetailsModel[]) => {
        result.forEach(resultItem => {
          let item = this.claimChargeDetailsShared.chargeDetailsData.find(x => x.id == resultItem.id);
          let itemIndex = this.claimChargeDetailsShared.chargeDetailsData.indexOf(item);
          this.claimChargeDetailsShared.chargeDetailsData[itemIndex] = resultItem;
        });
        this.claimChargeDetailsShared.claimsToUpdate = [];
      })
  }

  loadClaimInfo() {
    this.claimsService.getClaimInfo(this.claimId).subscribe(x => {
      this.claim = x;

      const patchValue = {
        renderingProviderId: x.renderingProviderId,
        referringProviderId: x.referringProviderId,
        serviceFacilityId: x.serviceFacilityId,
        billingProviderId: x.billingProviderId,
        benefitsAssignmentId: 1,

        submissionReason: x.submissionReason,
        placeOfService: x.placeOfService,
        patientReleaseAgreement: x.patientReleaseAgreement,
        authorizePayment: x.authorizePayment,
        diagnosisCodes: x.diagnosisCodes,
        originalClaim: x.originalClaim,
        note: x.note,
      }

      this.cacheFormValue = patchValue;

      this.claimForm.get('renderingProviderId').patchValue(x.renderingProviderId);
      this.claimForm.get('referringProviderId').patchValue(x.referringProviderId);
      this.claimForm.get('serviceFacilityId').patchValue(x.serviceFacilityId);
      this.claimForm.get('billingProviderId').patchValue(x.billingProviderId);
      this.claimForm.get('benefitsAssignmentId').patchValue(x.benefitsAssignment);
      this.claimForm.get('submissionReason').patchValue(parseInt(x.submissionReason));
      this.claimForm.get('placeOfService').patchValue(x.placeOfService.toString());
      this.claimForm.get('patientReleaseAgreement').patchValue(x.patientReleaseAgreement ? parseInt(x.patientReleaseAgreement) : null);
      this.claimForm.get('authorizePayment').patchValue(parseInt(x.authorizePayment));
      this.claimForm.get('originalClaim').patchValue(x.originalClaim?.toString());
      this.claimForm.get('note').patchValue(x.note?.toString());
      this.claimForm.get('diagnosisCodes').patchValue(x.diagnosisCodes);

      // this.claimForm.patchValue(patchValue);
      if (x.renderingProviderId === -1) {
        this.renderingProviders.push({
          id: x.renderingProviderId,
          name: x.renderingProvider
        });
      }
      // Fetch billing codes related to this claim's funder using legacy hardcoded serviceId (889).
      // (Reverted from dynamic resolvedServiceId mapping per user request.)
      if (x.funderId) {
        const model: ClaimCreateInfoGetModel = {
          clientId: x.patientId,
          funderId: x.funderId,
          serviceId: x.serviceId,
          accountInfoId: this.accountService.memberDetails.accountInfoId
        };
        this.clientsService.getClaimCreateInfo(model).subscribe((info: any) => {
          this.billingCodes = info?.billingCodes || [];
        });
      }
    },
      error => {
        this.router.navigate(["/billing/claims"]);
      })
  }

  mapBillingProviderId(billingProviderId: number) {
    if (billingProviderId === -1) {
      return undefined
    }

    return billingProviderId;
  }

  public cancel(): void {
    if (this.isChargeEntryDirty || !this.isDisabledSaveBtn) {
      this.cancelConfirmationAlert.opened = true;
    }
    else {
      this.backCancelService.emitBackCancel();
    }
  }

  updateLines() {
    this.chargeDetailsSummary.updateLines();
  }

  attachmentsClick() {
    this.sidebarService.openRight(EncounterAttachmentsComponent, true, "md").subscribe(sidebar => {
      let instance: EncounterAttachmentsComponent = sidebar.instance;

      instance.loadData({ claimIdentifier: this.claimIdentifier, claimId: this.claimId });
    });
  }

  errosAndAlertsClick() {
    this.sidebarService.openRight(EncounterErrorsAlertsComponent, true, "md").subscribe(sidebar => {
      let instance: EncounterErrorsAlertsComponent = sidebar.instance;

      instance.setData({ claimIdentifier: this.claimIdentifier, claimId: this.claimId }, 'Error');
    });
  }

  onChangeDiagnosicCode(event) {
    this.diagnosisCode = '';
    let diagnosisCodelist = []
    event.forEach(element => {
      diagnosisCodelist.push(element.diagnosisCode)
    });
    this.diagnosisCode = diagnosisCodelist.join(', ');
  }

  RefreshClaimData() {
  this.claimUpdateService.getSecondaryFunderDetails(this.claimId).subscribe({
    next: (result) => {
      if (result?.success) {
        this.notificationHandler.showNotificationSuccess(result.message || "Claim details have been updated.");
        setTimeout(() => {
          this.loadClaimInfo();
          this.encounterDetailsChargeDetailSummaryComponent.loadData();
        }, 2000);
      } else {
        this.notificationHandler.showNotificationError(result?.message || "Claim details could not be updated.");
      }
    },
    error: (err) => {
      // handle any unexpected errors from the API
      console.error('Error updating claim:', err);
      this.notificationHandler.showNotificationError("An unexpected error occurred while updating the claim.");
    }
  });
}

  checkIfChargeEntryValid(event: any) {
    this.isChargeEntryValid = event;
    if (!this.claimForm.dirty && this.isManualBillingProviderFormValid()) {
      this.isDisabledSaveBtn = false;
    }
  }

  checkIfChargeEntryDirty(event: any) {
    this.isChargeEntryDirty = event;
  }
}
