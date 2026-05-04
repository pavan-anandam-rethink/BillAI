import { Component, Input, Output, EventEmitter, SimpleChanges, ChangeDetectorRef, ChangeDetectionStrategy } from "@angular/core";
import { FormGroup, FormBuilder, FormArray, Validators, ValidationErrors, FormControl } from "@angular/forms";
import { takeUntil } from "rxjs/operators";
import { Observable, Subject } from "rxjs";
import { ClientsService } from '@core/services/clients/clients.service';
import { PlaceOfServiceOptionModel, PlaceOfServiceServerModel } from "@core/models/billing";
import { ClientFunderModel, FunderServiceLine } from "@core/models/company-account/funders/client-funder-model";
import { BasicOption } from "@core/models/common";
import { ClientOptionModel } from "@core/models/clients";
import { PreventableEvent } from "@progress/kendo-angular-dropdowns";
import { ClientFundersSmallModel } from "@core/models/clients/Client-Funders-Small-model";
import { AccountMemberService } from "@core/services/account/account-member.service";


@Component({
    selector: 'app-claim-info-step',
    templateUrl: './claim-info-step.component.html',
    styleUrls: ['./claim-info-step.component.css'],
    changeDetection: ChangeDetectionStrategy.Default
})
export class ClaimInfoStepComponent {
    @Input() parentForm: FormGroup;
    @Input() clients: ClientOptionModel[];
    @Output() isClaimInfoValid = new EventEmitter<boolean>();
    @Output() clientIdentifier = new EventEmitter<number>();
    @Output() clientRemove = new EventEmitter<number>();
    @Output() authId = new EventEmitter<number | string>();
    @Output() selectedFunder = new EventEmitter<ClientFunderModel>();
    @Output() selectedServiceLine = new EventEmitter<FunderServiceLine>();
    @Output() facilityId = new EventEmitter<number>();
    @Output() changeEvent = new EventEmitter<number>();
    @Output() onAuthNoChange = new EventEmitter<number | string>();

    public allowManualAuth = false;
    private unsubscribeAll$ = new Subject<void>();

    clientId: number;
    funderId: number;
    relationshipToInsured: number;

    funders: ClientFunderModel[];
    responsibleParties: BasicOption[];
    serviceLines: FunderServiceLine[];
    authorizations: BasicOption[];
    placesOfService: PlaceOfServiceOptionModel[];

    isFunderDisabled = true;
    isServiceLineDisabled = true;
    isAuthNumberDisabled = true;

    stepFormGroup: FormGroup;
    filteredClients: ClientOptionModel[];
    filteredOptions: Observable<ClientOptionModel[]>;

    constructor(private fb: FormBuilder, private clientService: ClientsService, 
        private cd: ChangeDetectorRef, private accountService: AccountMemberService) {
    }

   

    ngOnChanges(changes: SimpleChanges): void {
        const clients = changes['clients'];
        if (clients.currentValue) {
            this.filteredClients = this.clients;
        }
    }
   
    formCheck(): void {
        const clientControlValue = this.stepFormGroup.controls['clientId'].value;
        const isClientSelected = !!(clientControlValue && clientControlValue !== '');
        this.isFunderDisabled = !isClientSelected;

        const funderControlValue = this.stepFormGroup.controls['clientFunderId'].value;
        const isFunderSelected = funderControlValue !== null && funderControlValue !== '';
        this.isServiceLineDisabled = !(isClientSelected && isFunderSelected);

        const serviceLineControlValue = this.stepFormGroup.controls['serviceLineId'].value;
        const isServiceLineSelected = serviceLineControlValue !== null && serviceLineControlValue !== '';
        this.isAuthNumberDisabled = !(isClientSelected && isFunderSelected && isServiceLineSelected);

        this.isClaimInfoValid.emit(this.stepFormGroup.valid);
    }

    ngOnDestroy(): void {
        this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }


    ngOnInit() {

        const mainArray = this.parentForm.get("formData") as FormArray;

        if (mainArray.at(0) == undefined) {
            const formValues = this.fb.group({
                clientId: ['', Validators.required],
                clientFunderId: [{ value: '', disabled: this.isFunderDisabled }, Validators.required],
                responsiblePartyId: [{ value: '', disabled: true }],
                serviceLineId: [{ value: '', disabled: this.isServiceLineDisabled }, Validators.required],
                serviceId: [''],
                authorizationNumberId: [{ value: '', disabled: this.isAuthNumberDisabled }, Validators.maxLength(50)],
                placeOfServiceCodeId: ['', Validators.required],
                allowManualAuthorization: false
            }, { validators: [this.authorizationNumberRequired] });

            const formArray = this.parentForm.get("formData") as FormArray;
            formArray.push(formValues);
        }

        this.stepFormGroup = mainArray.at(0) as FormGroup;

        this.clientService.getPlacesOfService(this.accountService.memberDetails.accountInfoId).pipe(takeUntil(this.unsubscribeAll$)).subscribe(
            places => {
                this.placesOfService = this.mapPlaceOfServices(places);
            }
        );

        this.parentForm.valueChanges.pipe(takeUntil(this.unsubscribeAll$)).subscribe(() => {
            this.formCheck();
        });
    }

    disableControls() {

        // this.stepFormGroup.controls['clientFunderId'].updateValueAndValidity();
        // this.stepFormGroup.controls['responsiblePartyId'].disable();
        // this.stepFormGroup.controls['serviceLineId'].disable();
        // this.stepFormGroup.controls['allowManualAuthorization'].disable();
    }

    onClientChange(clientId: number | null | undefined) {
        const getClientFunderSmall:ClientFundersSmallModel={
            childProfileId:clientId,
            accountInfoId:this.accountService.memberDetails.accountInfoId
        }
        if (getClientFunderSmall.childProfileId&&getClientFunderSmall.accountInfoId) {
            
            const selectedClientOption = this.clients.find((client) => client.id === getClientFunderSmall.childProfileId);
            if (selectedClientOption) {
                getClientFunderSmall.childProfileId = selectedClientOption.id;
                this.clientIdentifier.emit(getClientFunderSmall.childProfileId);
                

                //get facility id on selection of client
                this.clientService.getClientFacilityId(getClientFunderSmall.childProfileId).pipe(takeUntil(this.unsubscribeAll$)).subscribe(
                    res => {
                        this.facilityId.emit(res);
                        this.clientService.getClientFundersSmall(getClientFunderSmall).pipe(takeUntil(this.unsubscribeAll$)).subscribe(
                            funders => {
                                this.funders = funders;
                                
                            }
                        );
                    }
                )
                

                this.stepFormGroup.controls['clientFunderId'].setValidators([Validators.required]);
                this.stepFormGroup.controls['serviceLineId'].setValidators([Validators.required]);
                this.setFormControlsDefaults();
                this.stepFormGroup.controls['serviceLineId'].markAsUntouched();
            }
        } else {
            this.clientRemove.emit(null);
            this.setFormControlsDefaults();
            this.stepFormGroup.controls['clientFunderId'].clearValidators();
            this.stepFormGroup.controls['clientFunderId'].updateValueAndValidity();
            this.stepFormGroup.controls['serviceLineId'].clearValidators();
            this.stepFormGroup.controls['serviceLineId'].updateValueAndValidity();
            this.funders = [];
            this.responsibleParties = [];
            this.serviceLines = [];
        }

        this.formCheck();
        this.clientId=getClientFunderSmall.childProfileId
    }

    isResponsiblePartiesLoaded: boolean = false;
    isServiceLinesLoaded: boolean = false;

    onFunderChange(clientFunderId: number) {
        if (clientFunderId === null) {
            return;
        }
        
        const funder = this.funders.find((f: ClientFunderModel) => f.id === clientFunderId);
        if (funder) {
            this.funderId = funder.funderId || 0;
            this.selectedFunder.emit(funder);
            this.isResponsiblePartiesLoaded = false;
            
            this.getFundersServiceLines(funder, clientFunderId);

            this.clientService.getClientFunderResponsibleParties(this.clientId, funder.funderId).pipe(takeUntil(this.unsubscribeAll$)).subscribe(
                responsibleParties => {
                    this.responsibleParties = [
                        { id: responsibleParties.clientDemographics.id, name: responsibleParties.clientDemographics.fullName },
                        { id: responsibleParties.insuranceContact.id, name: responsibleParties.insuranceContact.firstName + ' ' + responsibleParties.insuranceContact.lastName }
                    ];
                    this.relationshipToInsured = responsibleParties.insuranceContact.relationshipToInsured;
                    if (this.relationshipToInsured === 1) {
                        this.stepFormGroup.controls['responsiblePartyId'].setValue(this.responsibleParties[0].id);
                    } else {
                        this.stepFormGroup.controls['responsiblePartyId'].setValue(this.responsibleParties[1].id);
                    }
                    this.isResponsiblePartiesLoaded = true;
                }
            );

            //this.serviceLines = funder.serviceLines;
            this.stepFormGroup.controls['serviceLineId'].setValue(null);
            this.stepFormGroup.controls['authorizationNumberId'].setValue(null);

            this.formCheck();
        }
    }

    getFundersServiceLines(funder: ClientFunderModel, clientFunderId: number) {
        
        this.isServiceLinesLoaded = false;
        this.clientService.getFundersServiceLines(this.clientId, funder.funderId, clientFunderId).pipe(takeUntil(this.unsubscribeAll$)).subscribe(
            serviceLines => {
                this.serviceLines = serviceLines;
                this.isServiceLinesLoaded = true;
        });
    }

    onServiceLineChange(value: FunderServiceLine) {
        if (value.mappingId === null) {
            return;
        }

        const serviceLine = this.serviceLines.find((sl) => sl.mappingId === value.mappingId);
        this.selectedServiceLine.emit(serviceLine);

        this.stepFormGroup.patchValue({
            serviceId: value.serviceId
        });

        this.clientService.getClientAuthorizationsForClaim(this.clientId, this.funderId, value.serviceId, this.accountService.memberDetails.accountInfoId).pipe(takeUntil(this.unsubscribeAll$)).subscribe(
            authorizations => {
                this.authorizations = authorizations;
            }
        );

        this.changeEvent.emit(value.mappingId);

        this.stepFormGroup.controls['authorizationNumberId'].setValue(null);
        this.formCheck();
    }

    onAuthNumberChange(authId: number | string) {
        if (typeof authId === 'number') {
            this.stepFormGroup.controls['allowManualAuthorization'].setValue(false);
        }
        else if(typeof authId === 'string')
        {
            if(authId.trim() === '')
            {
                this.stepFormGroup.controls['authorizationNumberId'].setValue(null);
            } 
        }

        this.authId.emit(authId);
        this.onAuthNoChange.emit(authId);
    }

    onAllowManualAuthChange() {
       
        this.stepFormGroup.controls['authorizationNumberId'].setValue(null);
        if(this.stepFormGroup.get('allowManualAuthorization').value == true)
        {
            this.stepFormGroup.get('authorizationNumberId').enable();
        }

    }

    clientSearchValueChanged(newVal: string): void {
        this.filteredClients = this.clients.filter((x) => x.name.toLowerCase().indexOf(newVal.toLowerCase()) >= 0);
    }

    onAuthNumberOpen(event: PreventableEvent) {
        if (this.allowManualAuth) {
            event.preventDefault();
        }
    }

    mapPlaceOfServices(placeOfServices: PlaceOfServiceServerModel[]): PlaceOfServiceOptionModel[] {
        return placeOfServices ? placeOfServices.filter((pos) => pos.isActive).map<PlaceOfServiceOptionModel>((option) => ({
            id: option.id,
            value: `${option.code} - ${option.description}`,
        })) :
            [];
    }

    authorizationNumberRequired(formGroup: FormGroup): ValidationErrors | null {
        const authorizationNumberControl = formGroup.get("authorizationNumberId") as FormControl;
        const allowManualAuthControl = formGroup.get("allowManualAuthorization");
        const clientControl = formGroup.get("clientId");
        const isClientSelected = clientControl && clientControl.value;
        const isManualAuth = allowManualAuthControl && allowManualAuthControl.value;
        const isAuthHasValue = authorizationNumberControl && authorizationNumberControl.value ||
            (authorizationNumberControl.value && authorizationNumberControl.value.length);

        if (isManualAuth && !isAuthHasValue && isClientSelected) {
            authorizationNumberControl.setErrors({ 'required': true });
            authorizationNumberControl.markAsTouched();

            return { authorizationNumberControl };
        }

        return null;
    }

    setFormControlsDefaults() {
        this.stepFormGroup.controls['clientFunderId'].setValue(null);
        this.stepFormGroup.controls['responsiblePartyId'].setValue(null);
        this.stepFormGroup.controls['serviceLineId'].setValue(null);
        this.stepFormGroup.controls['authorizationNumberId'].setValue(null);
        this.stepFormGroup.controls['allowManualAuthorization'].setValue(false);
    }

    clear() {
        this.clientId = 0;
        this.stepFormGroup.get('clientId').setValue('');
    }
}
