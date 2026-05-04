import { Component, Input, OnInit, OnDestroy, Output, EventEmitter, OnChanges, SimpleChanges } from "@angular/core";
import { FormGroup, FormBuilder, FormArray, Validators } from "@angular/forms";
import { takeUntil } from "rxjs/operators";
import { Subject } from "rxjs";
import { Locale } from '@app/locale';
import { ClientReferringProviderForDropdown } from "@core/models/clients/referring-provider";
import { CompanyAccountLocation } from "@core/models/company-account";
import { ClientRenderingProvider } from "@core/models/clients/rendering-provider";
import { ClientFunderModel, FunderServiceLine } from "@core/models/company-account/funders/client-funder-model";
import { Authorization } from "@core/models/clients/authorization";
import { BillingProviderOption, FunderType } from "@core/enums/clients";
import { BasicOption } from "@core/models/common";
import { ClaimBillingProviderModel } from "@core/models/billing/claim-billing-provider";
import { StateInformation } from "@core/models/billing";
import { ClaimService } from "@core/services/billing";

@Component({
    selector: 'app-provider-step',
    templateUrl: './provider-step.component.html',
    styleUrls: ['./provider-step.component.css']
})
export class ProviderStepComponent implements OnInit, OnDestroy, OnChanges {
    @Input() parentForm: FormGroup;
    @Input() authDetails: Authorization;
    @Input() renderingProviders: ClientRenderingProvider[];
    @Input() serviceProviders: CompanyAccountLocation[];
    @Input() clientReferringProviders: ClientReferringProviderForDropdown[];
    @Input() selectedFunder: ClientFunderModel;
    @Input() isClaimInfoNextClicked: boolean;
    @Input() clientId: number;
    @Input() facilityId: number;
    @Input() selectedServiceLine: FunderServiceLine;
    @Output() isProviderValid = new EventEmitter<boolean>();
    private unsubscribeAll$ = new Subject<void>();

    stepFormGroup: FormGroup;
    isReferringRequired = false;
    isAuthSelected = false;
    billingProviderOption = BillingProviderOption;
    billingProviders: BasicOption[] = [];

    // Manual Billing Provider fields
    showManualBillingProvider = false;
    manualBillingProviderForm: FormGroup;
    providerType: 'Entity' | 'Person' = 'Entity';
    readonly OTHER_BILLING_PROVIDER_ID = 0;

    // US States from API
    states: StateInformation[] = [];
    private statesLoaded = false;

    minDateStart: Date | undefined;
    maxDateOfService: Date | undefined;
    isRenderingProviderDisabled: boolean = true;
    isServiceFacilityLocationDisabled: boolean = true;
    isReferringProviderDisabled: boolean = true;
    isBillingProviderDisabled: boolean;

    constructor(private fb: FormBuilder, public locale: Locale, private claimService: ClaimService) {
    }

    /**
     * Returns the manual billing provider data if "Other" is selected
     * Returns null if a standard billing provider is selected
     */
    public getBillingProviderOtherData(): ClaimBillingProviderModel | null {
        if (!this.showManualBillingProvider) {
            return null;
        }

        const formValue = this.manualBillingProviderForm.getRawValue();
        const selectedState = this.states.find(s => s.stateId === formValue.state);

        return {
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

        // Subscribe to manual billing provider form changes to trigger validation
        this.manualBillingProviderForm.statusChanges.pipe(takeUntil(this.unsubscribeAll$)).subscribe(() => {
            this.formCheck();
        });
    }

    private addOtherOptionToBillingProviders(): void {
        const otherOption = { id: this.OTHER_BILLING_PROVIDER_ID, name: 'Other' };
        const existingIndex = this.billingProviders.findIndex(p => p.id === this.OTHER_BILLING_PROVIDER_ID);
        if (existingIndex === -1) {
            this.billingProviders.push(otherOption);
        }
    }

    onBillingProviderChange(event: any): void {
        const selectedId = event?.id ?? event;
        this.showManualBillingProvider = selectedId === this.OTHER_BILLING_PROVIDER_ID;
        if (this.showManualBillingProvider) {
            // Load states when "Other" is selected
            this.loadStates();
        } else {
            this.manualBillingProviderForm.reset();
            this.providerType = 'Entity';
            this.updateFirstNameValidation();
        }
        // Re-check validation after billing provider change
        this.formCheck();
    }

    /**
     * Load states from API
     */
    private loadStates(): void {
        if (this.statesLoaded && this.states.length > 0) {
            return;
        }
        this.claimService.getStateInformation().subscribe(
            (data: StateInformation[]) => {
                this.states = data || [];
                this.statesLoaded = true;
            },
            (error) => {
                console.error('Error loading state information', error);
                this.states = [];
            }
        );
    }

    onProviderTypeChange(type: 'Entity' | 'Person'): void {
        this.providerType = type;
        // Clear all form fields when switching between Entity and Person
        this.manualBillingProviderForm.reset();
        this.updateFirstNameValidation();
        this.formCheck();
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

    ngOnChanges(changes: SimpleChanges): void {
        if (changes.authDetails && (
            (changes.authDetails.currentValue && !changes.authDetails.previousValue) ||
            (!changes.authDetails.currentValue && changes.authDetails.previousValue) ||
            (changes.authDetails.currentValue && changes.authDetails.previousValue &&
                changes.authDetails.currentValue.id !== changes.authDetails.previousValue.id))) {
            this.resetProviderFormControls(true);
        }

        if (this.authDetails && this.isClaimInfoNextClicked) {
            this.setFormValue(this.authDetails);
        } else if (changes.renderingProviders && changes.renderingProviders.currentValue &&
            changes.serviceProviders && changes.serviceProviders.currentValue &&
            changes.clientReferringProviders && changes.clientReferringProviders.currentValue && this.isClaimInfoNextClicked) {
            this.setFormValue(null);
        }

        if (changes.serviceProviders && changes.serviceProviders.currentValue) {
            this.billingProviders = this.serviceProviders.filter(x => x.isBillingLocation).map<BasicOption>((x) => ({ name: x.name + ' - ' + x.agencyName, id: x.id }));
            this.addOtherOptionToBillingProviders();
        }

        if (changes.selectedFunder && changes.selectedFunder.currentValue !== undefined) {
            this.isReferringRequired = this.selectedFunder.referringProviderRequiredOnClaim;

            const referringProviderControl = this.stepFormGroup.get('referringProviderId');
            const billingProviderControl = this.stepFormGroup.get('billingProviderId');

            if (referringProviderControl) {
                if (this.isReferringRequired) {
                    referringProviderControl.setValidators([Validators.required]);
                } else {
                    referringProviderControl.clearValidators();
                }

                referringProviderControl.updateValueAndValidity({ onlySelf: false, emitEvent: false });
            }

            if (billingProviderControl) {
                if (this.selectedFunder.funderType === FunderType.Insurance) {
                    billingProviderControl.setValidators([Validators.required]);
                } else {
                    billingProviderControl.clearValidators();
                }

                billingProviderControl.updateValueAndValidity({ onlySelf: false, emitEvent: false });
            }
        }

        if (changes.clientId && changes.clientId.currentValue) {
            this.resetProviderFormControls();
        }

        if (changes.clientId && changes.clientId.currentValue === 0) this.onClientRemove();


        if (this.isClaimInfoNextClicked) {
            this.isAuthSelected = !!this.authDetails;
            this.minDateStart = this.isAuthSelected ? new Date(this.authDetails.startDate) : undefined;
            this.maxDateOfService = this.isAuthSelected ? new Date(this.authDetails.endDate) : undefined;
        }
    }

    formCheck(): void {
        this.isRenderingProviderDisabled = !(this.clientId > 0 && this.selectedServiceLine);
        this.isBillingProviderDisabled = !(this.clientId > 0 && this.selectedServiceLine);
        this.isServiceFacilityLocationDisabled = !(this.clientId > 0 && this.selectedServiceLine);
        this.isReferringProviderDisabled = !(this.clientId > 0 && this.selectedServiceLine);

        // Include manual billing provider form validity when "Other" is selected
        const isManualFormValid = !this.showManualBillingProvider || this.manualBillingProviderForm.valid;
        this.isProviderValid.emit(this.stepFormGroup.valid && isManualFormValid);
    }

    ngOnDestroy(): void {
        this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }

    ngOnInit() {
        this.initManualBillingProviderForm();
        const mainArray = this.parentForm.get("formData") as FormArray;

        if (mainArray.at(1) == undefined) {
            const formValues = this.fb.group({
                renderingProviderId: ['', Validators.required],
                renderingProviderTypeId: [''],
                billingProviderId: [''],
                serviceFacilityLocationId: [''],
                //   dateOfServiceStart: ['', Validators.required],
                //   dateOfServiceEnd: ['', Validators.required],
                referringProviderId: ['']
            });

            const formArray = this.parentForm.get("formData") as FormArray;
            formArray.push(formValues);
        }

        this.stepFormGroup = mainArray.at(1) as FormGroup;
        this.stepFormGroup.disable();

        this.parentForm.valueChanges.pipe(takeUntil(this.unsubscribeAll$)).subscribe(() => {
            this.formCheck();
            //this.disableControls();
        });
    }

    disableControls() {
        this.stepFormGroup.controls['renderingProviderId'].disable();
        this.stepFormGroup.controls['serviceFacilityLocationId'].disable();
        this.stepFormGroup.controls['referringProviderId'].disable();
        this.stepFormGroup.controls['billingProviderId'].disable();
    }

    setFormValue(authDetails: Authorization | null) {
        if (authDetails) {
            this.stepFormGroup.enable();
            this.stepFormGroup.patchValue({
                renderingProviderId: this.getProviderIdOnStepChange('renderingProviderId', authDetails.renderingProviderId),
                renderingProviderTypeId: authDetails.renderingProviderTypeId,
                billingProviderId: this.getBillingPrivderOnStepChange(authDetails),
                serviceFacilityLocationId: this.getProviderIdOnStepChange('serviceFacilityLocationId', authDetails.serviceProviderId),
                referringProviderId: this.getProviderIdOnStepChange('referringProviderId', authDetails.referringProviderId),
            });
        } else {
            let billingLocation = this.serviceProviders.find(x => x.id === this.facilityId);
            if (billingLocation && billingLocation.isBillingLocation) {
                this.stepFormGroup.patchValue({
                    billingProviderId: billingLocation.id,
                });
            } else {
                const isGroupOnly = this.selectedFunder.billingProviderOptionId === BillingProviderOption.GroupOnly;
                const isIndividualAndGroup = this.selectedFunder.billingProviderOptionId === BillingProviderOption.IndividualAndGroup;
                if (isGroupOnly || isIndividualAndGroup) {
                    billingLocation = this.serviceProviders.find(x => x.isBillingLocation && x.isMainLocation);
                    if (billingLocation) {
                        this.stepFormGroup.patchValue({
                            billingProviderId: billingLocation.id,
                        });
                    }
                }
            }

            const serviceFacilityLocation = this.serviceProviders.find((x) => x.id === this.facilityId);
            const serviceFacilityLocationId = this.getProviderIdOnStepChange('serviceFacilityLocationId', serviceFacilityLocation && serviceFacilityLocation.id);
            if (serviceFacilityLocationId) {
                this.stepFormGroup.patchValue({
                    serviceFacilityLocationId: serviceFacilityLocationId,
                });
            }

            const defaultClientReferringProvider = this.clientReferringProviders.find((x) => x.isDefault);
            const clientReferringProviderId = this.getProviderIdOnStepChange('referringProviderId', defaultClientReferringProvider && defaultClientReferringProvider.id);
            if (clientReferringProviderId) {
                this.stepFormGroup.patchValue({
                    referringProviderId: clientReferringProviderId,
                });
            }
        }
    }

    renderingProviderChanged(provider: ClientRenderingProvider) {
        if (this.authDetails || this.renderingProviders) {
            this.stepFormGroup.enable();
            this.stepFormGroup.patchValue({
                renderingProviderTypeId: provider.id
            });

            if (this.selectedServiceLine.billingProviderOptionId === BillingProviderOption.IndividualOnly) {
                const renderingProviderId = provider.staffMemberId;
                const renderingProvider = this.renderingProviders.find((provider) => provider.staffMemberId === renderingProviderId);
                if (renderingProvider) {
                    this.updateBillingProviderIfIndividualOnly(renderingProvider.name);
                }
            }
        }
    }

    getProviderIdOnStepChange(controlName: string, defaultProviderId: number | undefined | null): number | undefined | null {
        const providerControl = this.stepFormGroup.get(controlName);
        if (providerControl && providerControl.value) {
            return providerControl.value;
        }

        return defaultProviderId;
    }

    onClientRemove() {

        this.renderingProviders = [];
        this.billingProviders = [];
        this.serviceProviders = [];
        this.clientReferringProviders = [];

        this.setFormControlsDefaults();

        this.stepFormGroup.controls['renderingProviderId'].clearValidators();
        this.stepFormGroup.controls['renderingProviderId'].updateValueAndValidity();

        this.stepFormGroup.controls['serviceFacilityLocationId'].clearValidators();
        this.stepFormGroup.controls['serviceFacilityLocationId'].updateValueAndValidity();

        this.stepFormGroup.controls['referringProviderId'].clearValidators();
        this.stepFormGroup.controls['referringProviderId'].updateValueAndValidity();

        this.stepFormGroup.controls['billingProviderId'].clearValidators();
        this.stepFormGroup.controls['billingProviderId'].updateValueAndValidity();
    }

    resetProviderFormControls(authChanged = false) {
        const renderingProviderControl = this.stepFormGroup.get('renderingProviderId');
        if (renderingProviderControl) renderingProviderControl.setValue(null);

        const serviceFacilityLocationControl = this.stepFormGroup.get('serviceFacilityLocationId');
        if (serviceFacilityLocationControl) serviceFacilityLocationControl.setValue(null);

        const referringProviderControl = this.stepFormGroup.get('referringProviderId');
        if (referringProviderControl) referringProviderControl.setValue(null);

        if (authChanged) {
            const billingProviderControl = this.stepFormGroup.get('billingProviderId');
            if (billingProviderControl) billingProviderControl.setValue(null);

            const dateOfServiceStartControl = this.stepFormGroup.get('dateOfServiceStart');
            if (dateOfServiceStartControl) dateOfServiceStartControl.reset();

            const dateOfServiceEndControl = this.stepFormGroup.get('dateOfServiceEnd');
            if (dateOfServiceEndControl) dateOfServiceEndControl.reset();
        }
    }

    updateBillingProviderIfIndividualOnly(value: string) {
        const option = { id: -1, name: value };
        const optionIndex = this.billingProviders.findIndex((provider) => provider.id === option.id);

        if (optionIndex !== -1) {
            this.billingProviders[optionIndex] = option;
        } else {
            this.billingProviders.push(option);
        }

        this.stepFormGroup.controls['billingProviderId'].setValue(option.id);
    }

    getBillingPrivderOnStepChange(authDetails: Authorization) {
        if (this.selectedServiceLine.billingProviderOptionId === BillingProviderOption.IndividualOnly) {
            const renderingProviderId = this.authDetails.renderingProviderId;
            const renderingProvider = this.renderingProviders && this.renderingProviders.find((provider) => provider.staffMemberId === renderingProviderId);

            if (renderingProvider) {
                this.updateBillingProviderIfIndividualOnly(renderingProvider.name);
                return -1;
            }
        } else {
            return this.getProviderIdOnStepChange('billingProviderId', authDetails.billingProviderId);
        }
    }

    setFormControlsDefaults() {
        this.stepFormGroup.controls['renderingProviderId'].setValue(null);
        this.stepFormGroup.controls['serviceFacilityLocationId'].setValue(null);
        this.stepFormGroup.controls['referringProviderId'].setValue(null);
        this.stepFormGroup.controls['billingProviderId'].setValue(null);
    }
}