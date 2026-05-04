import { Component, OnDestroy, OnInit, Input, OnChanges, SimpleChanges } from "@angular/core";
import { AbstractControl, FormArray, FormBuilder, FormControl, FormGroup, Validators } from "@angular/forms";
import { DialogService } from '@progress/kendo-angular-dialog';
import { ClientFunderModel, FunderServiceLine } from "@core/models/company-account/funders/client-funder-model";
import { Subject } from "rxjs";
import { Locale } from '@app/locale';
import { DatePipe } from "@angular/common";
import { BillingCodeAddNoteComponent } from './note/billing-code-note.component';
import { Authorization, AuthorizationBillingCode } from "@core/models/clients/authorization";
import { ClientBillingCode } from "@core/models/billing/billing-code";
import { MatDialog } from "@angular/material/dialog";
import { ModifiersHandler } from "@app/billing/encounters/shared/modifiers/modifiers.handler";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";
import { ClaimFilterOptionModel } from "@core/models/billing/claim-filter-option-model";
import { ClaimPatientGetModel } from "@core/models/billing/claim-patient-get-model";
import { ClaimService } from "@core/services/billing";
import { AccountMemberService } from "@core/services/account/account-member.service";

interface BillingCodeDataItem {
    individualDateOfService: Date | undefined;
    billingCodeId: number | undefined;
    renderingProviderStaffId: number | undefined;
    isSecondaryCode: boolean;
    noOfUnits: number | undefined;
    rate: number | undefined;
    providerBillingCode: { rate: number | undefined | null };
    totalCharges: number | undefined;
    unitTypeId: number | undefined | null;
    modifier1: string;
    modifier2: string;
    modifier3: string;
    modifier4: string;
    note: string | undefined | null;
}


@Component({
    selector: 'billing-codes',
    templateUrl: './billing-codes.component.html',
    styleUrls: ['./billing-codes.component.css'],
    providers: [DatePipe],
})
export class BillingCodesComponent implements OnInit, OnDestroy, OnChanges {
    private unsubscribeAll$ = new Subject<void>();
    @Input() parentForm: FormGroup;
    @Input() clientId: number;
    @Input() public selectedFunder: ClientFunderModel;
    @Input() public billingCodes: ClientBillingCode[];
    @Input() authDetails: Authorization;
    @Input() isProviderNextClicked: boolean;
    @Input() selectedServiceLine: FunderServiceLine;
    modifiersHandler: ModifiersHandler;

    minDateStart: Date | undefined;
    maxDateOfService: Date | undefined;
    stepForm: any;

    createFormGroup(dataItem: BillingCodeDataItem) {
        return this.fb.group({
            'renderingProviderStaffId': new FormControl(dataItem.renderingProviderStaffId, Validators.required),
            'individualDateOfService': new FormControl(dataItem.individualDateOfService, { validators: [Validators.required], updateOn: 'change' }),
            'billingCodeId': new FormControl(dataItem.billingCodeId, Validators.required),
            'unitTypeId': new FormControl(dataItem.unitTypeId, Validators.required),
            'noOfUnits': new FormControl(dataItem.noOfUnits, [Validators.required, this.rateValidator, Validators.min(0)]),
            'rate': new FormControl(dataItem.providerBillingCode.rate, [Validators.required, this.rateValidator, Validators.min(0)]),
            'totalCharges': new FormControl(0, { validators: [Validators.required, Validators.min(0)] }),
            'modifier1': new FormControl('', { validators: [Validators.minLength(2)], updateOn: 'blur' }),
            'modifier2': new FormControl('', { validators: [Validators.minLength(2)], updateOn: 'blur' }),
            'modifier3': new FormControl('', { validators: [Validators.minLength(2)], updateOn: 'blur' }),
            'modifier4': new FormControl('', { validators: [Validators.minLength(2)], updateOn: 'blur' }),
            'note': new FormControl(undefined),
            'isSecondaryCode': new FormControl(dataItem.isSecondaryCode),
        }, { validators: [this.modifiersHandler.modifiersValidator], updateOn: 'blur' })
    }

    billingCodesForm: FormGroup;
    billingCodesArrayForm: FormArray;
    renderingProviders: ClaimFilterOptionModel[] = [];
    isRenderingProviderLoaded = false;  
    totalClaimCharges = 0;
    minDOS: Date;
    maxDOS: Date;

    unitTypes = [
        { id: 1, name: "15 min", minutes: 15 },
        { id: 2, name: "30 min", minutes: 30 },
        { id: 3, name: "60 min", minutes: 60 },
        { id: 4, name: "90 min", minutes: 90 },
        { id: 5, name: "Untimed", minutes: 60 }
    ]

    constructor(private datePipe: DatePipe, private dialogService: DialogService, private fb: FormBuilder,
        public locale: Locale, private notificationHandler: NotificationHandlerService, private dialog: MatDialog,
        private claimService: ClaimService, private accountService: AccountMemberService) {
        this.modifiersHandler = new ModifiersHandler();

    }

    
    loadRenderingProviders() {

    if (this.isRenderingProviderLoaded) return;

    const model = {
        AccountInfoId: this.accountService.memberDetails?.accountInfoId,
        //MemberId: this.accountService.memberDetails?.memberId,
        // SearchValue: 'AAA Test',
        // Tab: 1
    };

    console.log('Rendering Provider Request:', model);

    this.claimService.getRenderingProviders()
        .subscribe({
            next: (res: ClaimFilterOptionModel[]) => {
                console.log('Rendering Provider Response:', res);
                this.renderingProviders = res || [];
                this.isRenderingProviderLoaded = true;
            },
            error: (err) => {
                console.error('Rendering Provider Error:', err);
            }
        });
}

    getUnitValue(index: number) {
        const controlElementValue = this.billingCodesArrayForm.at(index).value;
        if (!controlElementValue.unitTypeId)
            return;

        return controlElementValue.noOfUnits;
    }

    getUnitRateValue(index: number) {
        const controlElementValue = this.billingCodesArrayForm.at(index).value;
        if (!controlElementValue.rate)
            return;

        return controlElementValue.rate;
    }

    onControlChange(event: Event, index: number, controlName: string) {
        const controlElement = this.billingCodesArrayForm.at(index).get(controlName);
        if (controlElement) {
            const rawValue = (event.target as HTMLInputElement).value;

            if (!rawValue) {
                controlElement.setValue(undefined);
            } else {
                controlElement.setValue(rawValue);
            }
            this.calculateTotalCharges(index);
        }
    }

    calculateTotalCharges(index: number) {
        const controlElement = this.billingCodesArrayForm.at(index);
        if (controlElement != undefined) {
            const rateControl = controlElement.get('rate') as FormControl;
            const unitsControl = controlElement.get('noOfUnits') as FormControl;
            const value = rateControl.value * unitsControl.value;

            controlElement.patchValue({ totalCharges: parseFloat(value.toFixed(2)) });
            this.calculateTotalClaimCharges();
        }
    }

    calculateTotalClaimCharges() {
        this.totalClaimCharges = 0;
        this.billingCodesArrayForm.controls.forEach(element => {
            this.totalClaimCharges = parseFloat((this.totalClaimCharges + parseFloat(element.value.totalCharges)).toFixed(2));
        });
        if (isNaN(this.totalClaimCharges)) {
            this.totalClaimCharges = 0;
        }
        // this.totalClaimCharges = this.billingCodesArrayForm.controls.reduce(
            //     (prevValue, currValue) => parseFloat(prevValue + currValue.value.totalCharges, 0))
        }
        
        addHandler() {
            if (this.selectedServiceLine == undefined) {
                this.notificationHandler.showNotificationWarning('Please select the Service Line first');
                return;
            } else if (!this.billingCodes || this.billingCodes.length === 0) {
                this.notificationHandler.showNotificationWarning('Billing Codes are not available');
                return;
            }
            let lastIndividualDateOfService = undefined;
            if (this.billingCodesArrayForm.length > 0) {
                const currentBillingCodeControlValue = this.billingCodesArrayForm.value[this.billingCodesArrayForm.length - 1] as BillingCodeDataItem;
            lastIndividualDateOfService = currentBillingCodeControlValue?.individualDateOfService;
        }
        const emptyBillingCode: BillingCodeDataItem = {
            renderingProviderStaffId: undefined,
            individualDateOfService: undefined,
            billingCodeId: undefined,
            unitTypeId: undefined,
            noOfUnits: undefined,
            providerBillingCode: {
                rate: undefined,
            },
            isSecondaryCode: false,
            modifier1: '',
            modifier2: '',
            modifier3: '',
            modifier4: '',
            note: undefined,
            rate: undefined,
            totalCharges: undefined,
        };

        const group = this.addModifiersHandlers(this.createFormGroup(emptyBillingCode));
        this.billingCodesArrayForm.push(group);
        setTimeout(()=>{
            document.getElementById("IndividualDos"+(this.billingCodesArrayForm.length-1))?.getElementsByTagName("input")[0]?.focus();
        },100);
        
    }

    removeHandler(index: number) {
        const currentBillingCodeControl = this.billingCodesArrayForm.at(index);
        const currentBillingCodeControlValue = currentBillingCodeControl.value;
        const currentBillingCodeHasTwoControls = this.billingCodes.filter((code) => code.billingCodeId == currentBillingCodeControlValue.billingCodeId &&
            !!code.billingCodeName2 && code.billingCodeName2.length > 0);

        if (currentBillingCodeHasTwoControls) {
            if (currentBillingCodeHasTwoControls.length === 0) {
                this.billingCodesArrayForm.removeAt(index);
            } else {
                if (currentBillingCodeControlValue.isSecondaryCode) {
                    this.billingCodesArrayForm.removeAt(index);
                    this.billingCodesArrayForm.removeAt(index - 1);
                }
                else {
                    this.billingCodesArrayForm.removeAt(index + 1);
                    this.billingCodesArrayForm.removeAt(index);
                }
            }
        } else {
            this.billingCodesArrayForm.removeAt(index);
        }

        this.calculateTotalClaimCharges();
    }

    getFormControl(rowIndex: number, controlName: string): FormControl {
        const controlElement = this.billingCodesArrayForm.at(rowIndex);
        if (!controlElement) {
            return new FormControl('');
        }

        return controlElement.get(controlName) as FormControl
    }

    isMarkedAsInvalid(rowIndex: number, controlName: string) {
        const controlElement = this.billingCodesArrayForm.at(rowIndex);
        if (!controlElement) {
            return true;
        }

        const control = controlElement.get(controlName) as FormControl;
        return control.invalid;
    }

    markAsTouched(rowIndex: number, controlName: string) {
        const controlElement = this.billingCodesArrayForm.at(rowIndex);
        const control = controlElement.get(controlName) as FormControl;
        control.markAsTouched();
    }


    getBillingCodeName(billingCode: AuthorizationBillingCode, index: number) {
        const billingCodeControlValue = this.billingCodesArrayForm.at(index).value;

        return billingCodeControlValue.isSecondaryCode ?
            `${billingCode.serviceName}/${billingCode.billingCodeName2}` :
            `${billingCode.serviceName}/${billingCode.billingCodeName}`;
    }

    getBillingCodeFullName(billingCode: AuthorizationBillingCode) {
        return billingCode.serviceName + '/' + billingCode.billingCodeName +
            (billingCode.billingCodeName2 ? ('/' + billingCode.billingCodeName2) : '');
    }

    billingCodeChanged(newBillingCode: AuthorizationBillingCode, i: number) {
        let controlIndexToUpdate = i;
        const currentBillingCodeControl = this.billingCodesArrayForm.at(i);
        const currentBillingCodeControlValue = this.billingCodesArrayForm.at(i).value as BillingCodeDataItem;

        const billingCodeControl = this.getFormControl(controlIndexToUpdate, 'billingCodeId');
        billingCodeControl.patchValue(newBillingCode.billingCodeId);

        currentBillingCodeControlValue.billingCodeId = newBillingCode.billingCodeId;
        const currentBillingCodeHasTwoControls = this.billingCodes.filter((code) => code.billingCodeId == currentBillingCodeControlValue.billingCodeId &&
            !!code.billingCodeName2 && code.billingCodeName2.length > 0);
   
        if (currentBillingCodeHasTwoControls.length > 0) {
            const isSecondary = currentBillingCodeControlValue.isSecondaryCode;

            if (isSecondary && i > 0) {
                this.billingCodesArrayForm.removeAt(i - 1);
                const currentControl = this.billingCodesArrayForm.at(i - 1);
                if (currentControl) {
                    currentControl.patchValue({ isSecondaryCode: false });
                }
            } else if (!isSecondary && i + 1 < this.billingCodesArrayForm.length) {
                
                this.billingCodesArrayForm.removeAt(i + 1);
            }
        }

        const hasBillingCode2 = newBillingCode.billingCodeName2 && newBillingCode.billingCodeName2.length > 0;
        if (hasBillingCode2) {
            const billingCode2: BillingCodeDataItem = {
                renderingProviderStaffId: currentBillingCodeControlValue.renderingProviderStaffId,
                individualDateOfService: currentBillingCodeControlValue.individualDateOfService,
                billingCodeId: newBillingCode.billingCodeId,
                unitTypeId: newBillingCode.unitTypeId2,
                noOfUnits: undefined,
                providerBillingCode: {
                    rate: newBillingCode.rate2,
                },
                totalCharges: undefined,
                modifier1: '',
                modifier2: '',
                modifier3: '',
                modifier4: '',
                note: undefined,
                rate: undefined,
                isSecondaryCode: true,
            };

            this.billingCodesArrayForm.push(this.addModifiersHandlers(this.createFormGroup(billingCode2)));

            const unitTypeIdControl2 = this.getFormControl((controlIndexToUpdate + 1), 'unitTypeId')
            unitTypeIdControl2.patchValue(billingCode2.unitTypeId);
            unitTypeIdControl2.disable();
        }

        const unitTypeIdControl = this.getFormControl(controlIndexToUpdate, 'unitTypeId')
        const unitRateControl = this.getFormControl(controlIndexToUpdate, 'rate')
        const unitControl = this.getFormControl(controlIndexToUpdate, 'noOfUnits')

        unitTypeIdControl.patchValue(newBillingCode.unitTypeId);
        unitRateControl.patchValue(newBillingCode.rate);
        unitControl.patchValue(currentBillingCodeControlValue.noOfUnits);

        unitTypeIdControl.disable();
        //unitRateControl.disable();

        this.calculateTotalCharges(controlIndexToUpdate);
    }

    addModifiersHandlers(formGroup: FormGroup): FormGroup {
        const m1 = formGroup.controls["modifier1"];
        const m2 = formGroup.controls["modifier2"];
        const m3 = formGroup.controls["modifier3"];
        const m4 = formGroup.controls["modifier4"];

        this.modifiersHandler.subscribeModifiers(m1, m2, m3, m4);
        return formGroup;
    }

    isServiceBillingCodeSelected(index: number): boolean {
        const control = this.billingCodesArrayForm.at(index).value;

        return control.billingCodeId;
    }

    openAddNotePopup(index: number) {
        const noteControl = this.billingCodesArrayForm.at(index).get('note');
        const dialogRef = this.dialog.open(BillingCodeAddNoteComponent, {
            width: '540px',
            data: { name: noteControl.value }
        });

        dialogRef.componentInstance.note = noteControl.value;

        dialogRef.afterClosed().subscribe(result => {
            // console.log('The dialog was closed');
            noteControl.setValue(result.note);
        });
    }

    onIndividualDateOfServiceChanged(index: number) {
        const currentBillingCodeControlValue = this.billingCodesArrayForm.at(index).value as BillingCodeDataItem;
        if (
            this.hasSecondaryCode(currentBillingCodeControlValue.billingCodeId) &&
            !currentBillingCodeControlValue.isSecondaryCode
        ) {
            this.syncSecondaryRowDate(index, currentBillingCodeControlValue.individualDateOfService, currentBillingCodeControlValue.billingCodeId);
        }
    }

    private hasSecondaryCode(billingCodeId: number | undefined): boolean {
        return this.billingCodes.some(
            code =>
                code.billingCodeId === billingCodeId &&
                !!code.billingCodeName2 &&
                code.billingCodeName2.length > 0
        );
    }

    private syncSecondaryRowDate(index: number, date: Date | undefined, billingCodeId: number | undefined): void {
        const secondControlIndex = index + 1;
        if (secondControlIndex < this.billingCodesArrayForm.length) {
            const secondControl = this.billingCodesArrayForm.at(secondControlIndex).value as BillingCodeDataItem;
            if (
                secondControl.isSecondaryCode &&
                secondControl.billingCodeId === billingCodeId
            ) {
                const dateControl = this.getFormControl(secondControlIndex, 'individualDateOfService');
                if (dateControl) {
                    dateControl.setValue(date, { emitEvent: false, onlySelf: true });
                }
            }
        }
    }

    setIndividualDateOfServiceValidation(formGroup: FormGroup) {
        const controlDOS = formGroup.get('individualDateOfService');
        if (controlDOS) {
            const controlDOSValue = new Date(controlDOS.value);
            if (controlDOSValue < this.minDOS || controlDOSValue > this.maxDOS) {
                controlDOS.setErrors({ 'individualDateOfService': true });
            }
        }
    }

    setValidators() {
        this.billingCodesArrayForm.controls.forEach((control) => {
            this.setIndividualDateOfServiceValidation(control as FormGroup)
        })
    }

    ngOnInit(): void {
        const mainArray = this.parentForm.get("formData") as FormArray;
        this.stepForm = mainArray.at(2);
        this.billingCodesForm = this.stepForm.get("billingCodes") as FormGroup;
        this.billingCodesArrayForm = this.billingCodesForm.get('billingCodes') as FormArray;
        this.loadRenderingProviders();
    }

    ngOnChanges(changes: SimpleChanges): void {

        if (!changes.clientId || changes.clientId.currentValue === 0) {
            this.billingCodesArrayForm.controls = [];
        }

        if (changes.billingCodes && changes.billingCodes.currentValue) {
            this.billingCodesForm.setControl('billingCodes', new FormArray([], Validators.required));
            this.billingCodesArrayForm = this.billingCodesForm.get('billingCodes') as FormArray;
        }
    }

    rateValidator(control: AbstractControl) {
        let rateValue = parseFloat(control.value);
        let rate = rateValue.toString();
        if (rate === '0' || rate === '0.0' || rate === '0.00' || rate === '-0') {
            return { error: true };
        }
        return null;
    }

    ngOnDestroy(): void {
        this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }
    taxableValue: any;
    formatCurrency_TaxableValue(event) {
        var uy = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(event.target.value);
        this.taxableValue = uy;
    }

    decimalFilter(event: any) {
        const reg = /^\d{0,6}(\.\d{0,2})?$/;
        let input = event.target.value + String.fromCharCode(event.charCode);
        if (!reg.test(input)) {
            event.preventDefault();
        }
    }
}
