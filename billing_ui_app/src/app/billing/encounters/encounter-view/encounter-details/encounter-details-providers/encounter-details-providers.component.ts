import { Component, Input, OnInit, OnChanges, SimpleChanges, Output, EventEmitter, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BillingProviderOption, FunderType } from '@core/enums/clients';
import { ClaimDetailsInfoModel, StateInformation } from '@core/models/billing';
import { BasicOption } from '@core/models/common';
import { ClaimService } from '@core/services/billing';
import { ClaimBillingProviderDto, ClaimBillingProviderModel } from '@core/models/billing/claim-billing-provider';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-encounter-details-providers',
  templateUrl: './encounter-details-providers.component.html',
  styleUrls: ['./encounter-details-providers.component.css']
})
export class EncounterDetailsProvidersComponent implements OnInit, OnChanges, OnDestroy {
  @Input() claim: ClaimDetailsInfoModel;
  @Input() claimForm: FormGroup;
  @Input() public serviceProviders: BasicOption[] = [];
  @Input() public referringProviders: BasicOption[] = [];
  @Input() public billingProviders: BasicOption[] = [];
  @Input() public renderingProviders: BasicOption[] = [];

  // Output event when manual billing provider form changes
  @Output() manualBillingProviderFormChanged = new EventEmitter<{ dirty: boolean; valid: boolean }>();

  // Manual Billing Provider fields
  public showManualBillingProvider = false;
  public manualBillingProviderForm: FormGroup;
  public providerType: 'Entity' | 'Person' = 'Entity';
  public readonly OTHER_BILLING_PROVIDER_ID = 0;

  // US States from API
  public states: StateInformation[] = [];
  private statesLoaded = false;
  private destroy$ = new Subject<void>();

  public funderType = FunderType;
  public billingProviderOption = BillingProviderOption;
//   public emptyOption: { id: number; name: string } = { id: null, name: 'N/A' };
  public showReferringProviderRequiredTooltip = false;

  public renderingProviderTooltipBaseMessage = "To update the rendering provider on the claim, please update the rendering provider connected to the";
  public renderingProviderAuthTooltipMessage = `${this.renderingProviderTooltipBaseMessage} authorization in the client chart.`;
  public renderingProviderNoAuthTooltipMessage = `${this.renderingProviderTooltipBaseMessage} funder in your Company Account settings.`;
  public billingProviderAuthTooltipMessage =
      "To update the billing provider on the claim, please update the billing provider connected to the authorization in the client chart.";
  public billingProviderNoAuthTooltipMessage = 
      `To update the billing provider on the claim, please review the funder settings
      for billing provider and update accordingly (billing code, location/facility assigned to client or main location
      if no location assigned to client).`;

  public referringProviderTooltipBaseMessage = "To update the referring provider on the claim, please update the referring provider in the";
  public referringProviderAuthTooltipMessage = `${this.referringProviderTooltipBaseMessage} connected authorization in the client chart.`;
  public referringProviderNoAuthTooltipMessage = `${this.referringProviderTooltipBaseMessage} client chart on the Referring Providers tab.`;

  public referringProviderRequiredAuthTooltipMessage = 
      "To add a referring provider to the claim, please add a referring provider to the connected authorization in the client chart."
  public referringProviderRequiredNoAuthTooltipMessage =
      "To add a referring provider to the claim, please add a referring provider to the client chart on the Referring Providers tab."
  

  public serviceFacilityAuthTooltipMessage =
      "To update the service facility on the claim, please update the service facility connected to the authorization on the client chart.";
  public serviceFacilityNoAuthTooltipMessage = 
      "To update the service facility on the claim, please update the location/facility in the client chart on the Demographics tab";

  constructor(private fb: FormBuilder, private claimService: ClaimService) {
      this.initManualBillingProviderForm();
  }

  ngOnChanges(changes: SimpleChanges): void {
      // Add "Other" option whenever billingProviders input changes
      if (changes['billingProviders'] && this.billingProviders?.length > 0) {
          this.addOtherOptionToBillingProviders();
      }
  }
  
  ngOnInit(): void {
      this.showReferringProviderRequiredTooltip = this.claim &&
          !this.claim.referringProviderId &&
          this.claim.referringProviderRequiredOnClaim;

      const referringProviderControl = this.claimForm.get('referringProviderId');
      if (referringProviderControl)
      {
          if (this.claim.referringProviderRequiredOnClaim) {
              referringProviderControl.setValidators([Validators.required]);
          } else {
              referringProviderControl.clearValidators();
          }

          referringProviderControl.updateValueAndValidity({ onlySelf: false, emitEvent: false });
      }
      this.resolveBillingProvider();
      this.addOtherOptionToBillingProviders();
      this.loadBillingProviderOther();
  }

  /**
   * Load billing provider data if "Other" was previously selected (billingProviderId === 0)
   */
  private loadBillingProviderOther(): void {
      const claimId = this.claim?.id || this.claim?.claimId;
      // Only call GetBillingProviderDetails when billingProviderId is 0
      if (claimId && this.claim?.billingProviderId === 0) {
          this.claimService.getBillingProviderOther(claimId).subscribe(
              (data: ClaimBillingProviderDto | null) => {
                  if (data) {
                      // Load states first, then populate the form
                      this.loadStates().then(() => {
                          this.populateBillingProviderForm(data);
                      });
                  }
              },
              (error) => {
                  // No billing provider other data found - this is expected for standard providers
                  console.log('No other billing provider data found for claim');
              }
          );
      }
  }

  /**
   * Load states from API
   */
  private loadStates(): Promise<void> {
      return new Promise((resolve) => {
          if (this.statesLoaded && this.states.length > 0) {
              resolve();
              return;
          }
          this.claimService.getStateInformation().subscribe(
              (data: StateInformation[]) => {
                  this.states = data || [];
                  this.statesLoaded = true;
                  resolve();
              },
              (error) => {
                  console.error('Error loading state information', error);
                  this.states = [];
                  resolve();
              }
          );
      });
  }

  /**
   * Populate the manual billing provider form with loaded data
   */
  private populateBillingProviderForm(data: ClaimBillingProviderDto): void {
      // Set billing provider dropdown to "Other"
      this.claimForm.controls['billingProviderId']?.setValue(this.OTHER_BILLING_PROVIDER_ID);
      this.showManualBillingProvider = true;

      // Set provider type
      this.providerType = data.providerType === 'Person' ? 'Person' : 'Entity';
      this.updateFirstNameValidation();

      // Find state ID by stateCode
      const stateOption = this.states.find(s => s.stateCode === data.state);

      // Populate form fields
      this.manualBillingProviderForm.patchValue({
          firstName: data.firstName || '',
          facilityLastName: data.lastNameOrFacilityName || '',
          npi: data.npi || '',
          taxId: data.taxId || '',
          taxonomyCode: data.taxonomyCode || '',
          addressLine1: data.addressLine1 || '',
          addressLine2: data.addressLine2 || '',
          city: data.city || '',
          state: stateOption?.stateId || null,
          zip: data.zip || '',
          zipExt: data.zipExt || ''
      });
  }

  /**
   * Returns the manual billing provider data for saving
   */
  public getBillingProviderOtherData(): ClaimBillingProviderModel | null {
      if (!this.showManualBillingProvider) {
          return null;
      }

      const formValue = this.manualBillingProviderForm.getRawValue();
      const selectedState = this.states.find(s => s.stateId === formValue.state);

      return {
          claimId: this.claim?.id || this.claim?.claimId,
          providerType: this.providerType,
          firstName: formValue.firstName || '',
          lastNameOrFacilityName: formValue.facilityLastName || '',
          npi: formValue.npi || '',
          taxId: formValue.taxId || '',
          taxonomyCode: formValue.taxonomyCode || '',
          addressLine1: formValue.addressLine1 || '',
          addressLine2: formValue.addressLine2 || '',
          city: formValue.city || '',
          state: selectedState?.stateCode || '',
          zip: formValue.zip || '',
          zipExt: formValue.zipExt || ''
      };
  }

  private initManualBillingProviderForm(): void {
      this.manualBillingProviderForm = this.fb.group({
          firstName: [''],
          facilityLastName: ['', Validators.required],
          npi: ['', [Validators.required, Validators.pattern(/^\d{10}$/)]],
          taxId: [''],
          taxonomyCode: [''],
          addressLine1: ['', Validators.required],
          addressLine2: [''],
          city: ['', Validators.required],
          state: [null, Validators.required],
          zip: ['', [Validators.required, Validators.pattern(/^\d{5}$/)]],
          zipExt: ['', [Validators.required, Validators.pattern(/^\d{4}$/)]]
      });
      this.updateFirstNameValidation();
      this.subscribeToManualFormChanges();
  }

  private subscribeToManualFormChanges(): void {
      this.manualBillingProviderForm.valueChanges.pipe(
          takeUntil(this.destroy$)
      ).subscribe(() => {
          if (this.showManualBillingProvider) {
              this.manualBillingProviderFormChanged.emit({
                  dirty: this.manualBillingProviderForm.dirty,
                  valid: this.manualBillingProviderForm.valid
              });
          }
      });
  }

  ngOnDestroy(): void {
      this.destroy$.next();
      this.destroy$.complete();
  }

  private addOtherOptionToBillingProviders(): void {
      const otherOption = { id: this.OTHER_BILLING_PROVIDER_ID, name: 'Other' };
      const existingIndex = this.billingProviders.findIndex(p => p.id === this.OTHER_BILLING_PROVIDER_ID);
      if (existingIndex === -1) {
          this.billingProviders.push(otherOption);
      }
  }

  public onBillingProviderChange(event: any): void {
      const selectedId = event?.id ?? event;
      this.showManualBillingProvider = selectedId === this.OTHER_BILLING_PROVIDER_ID;
      if (this.showManualBillingProvider) {
          // Load states when "Other" is selected
          this.loadStates();
          // Emit validity status immediately when "Other" is selected
          this.manualBillingProviderFormChanged.emit({
              dirty: this.manualBillingProviderForm.dirty,
              valid: this.manualBillingProviderForm.valid
          });
      } else {
          this.manualBillingProviderForm.reset();
          this.providerType = 'Entity';
          this.updateFirstNameValidation();
          // Emit that the manual form is now valid (not shown) when switching away from "Other"
          this.manualBillingProviderFormChanged.emit({
              dirty: false,
              valid: true
          });
      }
  }

  public onProviderTypeChange(type: 'Entity' | 'Person'): void {
      this.providerType = type;
      // Clear all form fields when switching between Entity and Person
      this.manualBillingProviderForm.reset();
      this.updateFirstNameValidation();
      // Emit form state change after reset
      this.manualBillingProviderFormChanged.emit({
          dirty: true,
          valid: this.manualBillingProviderForm.valid
      });
  }

  private updateFirstNameValidation(): void {
      const firstNameControl = this.manualBillingProviderForm.get('firstName');
      const facilityLastNameControl = this.manualBillingProviderForm.get('facilityLastName');
      if (this.providerType === 'Person') {
          firstNameControl?.setValidators([Validators.required]);
          firstNameControl?.enable();
          facilityLastNameControl?.setValidators([Validators.required]);
      } else {
          firstNameControl?.clearValidators();
          firstNameControl?.enable();
          firstNameControl?.setValue('');
          facilityLastNameControl?.setValidators([Validators.required]);
      }
      firstNameControl?.updateValueAndValidity();
      facilityLastNameControl?.updateValueAndValidity();
  }


  resolveBillingProvider() {
      if (this.claim.billingProviderOptionId == BillingProviderOption.IndividualOnly) {
          const renderingProviderId = this.claim.authorizationNumber ? this.claim.authorizationDetails.renderingProviderId : this.claim.renderingProviderId;
          const renderingProvider = this.renderingProviders.find((provider) => provider.id === renderingProviderId);
          if (renderingProvider) {
              this.updateBillingProviderIfIndividualOnly(renderingProvider.name);
          }
      }
  }

  updateBillingProviderIfIndividualOnly(value: string) {
      const option = {id: -1, name: value};
      const optionIndex = this.billingProviders.findIndex((provider) => provider.id === option.id);

      if (optionIndex !== -1) {
          this.billingProviders[optionIndex] = option;
      } else {
          this.billingProviders.push(option);
      }

      this.claimForm.controls['billingProviderId'].setValue(option.id);
  }

  renderingProviderChanged(provider: BasicOption) {
      if (this.claim.billingProviderOptionId == BillingProviderOption.IndividualOnly) {
          this.updateBillingProviderIfIndividualOnly(provider.name);
      }
  }
}