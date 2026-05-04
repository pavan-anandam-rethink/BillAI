import { FormGroup, FormControl, FormArray, Validators, ValidatorFn, AbstractControl, ValidationErrors } from "@angular/forms";
import { ClaimUpdateDetailsModels, ClaimDetailsModel } from "@core/models/billing";
import { ClaimService } from "@core/services/billing";
import { takeUntil } from "rxjs/operators";
import { Subject } from "rxjs";
import { ClaimUpdateModifiersViewModel } from "@core/models/billing/claim-details-model";
import { CurrencyPipe } from "@angular/common";
import { ModifiersGridColumnUpdateModel } from "../modifiers/modifiers.component";
import { ActivatedRoute, Router } from "@angular/router";
import { EventEmitter } from "@angular/core";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";

const createFormGroup = (dataItem: any) => {
    return new FormGroup({
        'id': new FormControl(dataItem.id),
        'units': new FormControl(dataItem.units),
        'perUnitsCharge': new FormControl(dataItem.perUnitsCharge, Validators.min(0))
    });
}

export class ClaimChargeDetailsShared {
    public unsubscribeAll$ = new Subject<void>();
    public onUpdateEvent = new EventEmitter();

    claimId = 0;
    manualClaim = false;
    claimsToUpdate: number[] = [];

    chargeDetailsData: any[] = [];
    modifiersToUpdate: ClaimUpdateModifiersViewModel[] = [];

    hasInvalidModifiers = false;

    chargeDetailsArrayForm: FormArray = new FormArray([]);
    viewData: any;

    constructor(
        private claimsService: ClaimService, 
        private currencyPipe: CurrencyPipe, 
        private router: Router, 
        private readonly route: ActivatedRoute, 
        private accountService: AccountMemberService,
        private notifyHandler: NotificationHandlerService
    ) { }

    fillArrayForm(requestData: any[]) {
        this.chargeDetailsArrayForm = new FormArray([]);
        requestData.forEach(item => {
            let group = createFormGroup(item);

            const unitsControl = group.get('units');
            unitsControl!.setValidators([this.positiveDecimalValidator(), Validators.required]);
            unitsControl!.updateValueAndValidity({ onlySelf: false, emitEvent: false });

            const perUnitChargeControl = group.get('perUnitsCharge');
            perUnitChargeControl!.setValidators([this.positiveDecimalValidator(), Validators.required]);
            perUnitChargeControl!.updateValueAndValidity({ onlySelf: false, emitEvent: false });

            this.chargeDetailsArrayForm.push(group);
        });
    }

    positiveDecimalValidator(): ValidatorFn {
        return (control: AbstractControl): ValidationErrors | null => {
            const value = control.value;
            if (value > 0) {
                return null;
            }
            return { positiveDecimal: { valid: false } };
        };
    }

    lineUpdated(lineId: number): void {
        let foundedId = this.claimsToUpdate.find(x => x == lineId);

        if (foundedId == undefined) {
            this.claimsToUpdate.push(lineId);
        }
    }
    

    createChargeEntryUpdateModel() {
        let updateModel: ClaimUpdateDetailsModels = {
            billingClaimDetailsModels: [],
            memberId: this.accountService.memberDetails.memberId
        };
        this.claimsToUpdate.forEach(itemId => {
            let itemToUpdate: ClaimDetailsModel = this.chargeDetailsData.find(x => x.id == itemId);
            itemToUpdate.claimId = this.claimId;

            const updatedLineModifiers = this.modifiersToUpdate.find((item) => item.id === itemToUpdate.id)
            if (updatedLineModifiers !== undefined) {
                itemToUpdate.modifier1 = updatedLineModifiers.modifier1;
                itemToUpdate.modifier2 = updatedLineModifiers.modifier2;
                itemToUpdate.modifier3 = updatedLineModifiers.modifier3;
                itemToUpdate.modifier4 = updatedLineModifiers.modifier4;
            }

            updateModel.billingClaimDetailsModels.push(itemToUpdate);
        });
        return updateModel;
    }

    updateLines(): void {
        if (this.chargeDetailsArrayForm.invalid || this.claimsToUpdate.length < 1) {
            this.onUpdateEvent.emit();
            return;
        }      
        var model = this.createChargeEntryUpdateModel();
        this.claimsService.updateBillingClaimDetails(model).pipe(takeUntil(this.unsubscribeAll$))
            .subscribe((result: ClaimDetailsModel[]) => {
                result.forEach(resultItem => {
                    let item = this.chargeDetailsData.find(x => x.id == resultItem.id);
                    let itemIndex = this.chargeDetailsData.indexOf(item);
                    this.chargeDetailsData[itemIndex] = resultItem;
                });

                this.claimsToUpdate = [];
                this.notifyHandler.showNotificationSuccess("Claim details updated successfully");
                this.chargeDetailsArrayForm.markAsUntouched();
                if (this.router.url != '/billing/claims/list')
                    this.navigateToDashboard();
                else
                    this.onUpdateEvent.emit();
            })
    }

    navigateToDashboard(): void {
        this.route.params.subscribe(x => {
            var selectedTab = +x["tab"];
            this.router.navigate(['/billing/claims/list'], { queryParams: { tab: --selectedTab } });
        })
    }

    modifiersUpdated(model: ModifiersGridColumnUpdateModel): void {
        var needUpdateLineModifiers = this.modifiersToUpdate.any((item) => item.id === model.lineId);
        var modifiersUpdateModel: ClaimUpdateModifiersViewModel = {
            id: model.lineId,
            modifier1: model.modifiers.modifier1,
            includeOnClaimMod1: true,
            modifier2: model.modifiers.modifier2,
            includeOnClaimMod2: true,
            modifier3: model.modifiers.modifier3,
            includeOnClaimMod3: true,
            modifier4: model.modifiers.modifier4,
            includeOnClaimMod4: true,
            isValid: model.isValid
        };

        if (needUpdateLineModifiers) {
            const foundIndex = this.modifiersToUpdate.findIndex((item) => item.id === modifiersUpdateModel.id)
            this.modifiersToUpdate[foundIndex] = modifiersUpdateModel;
        } else {
            this.modifiersToUpdate.push(modifiersUpdateModel)
        }

        this.hasInvalidModifiers = this.modifiersToUpdate.some((item) => !item.isValid);
        this.chargeDetailsArrayForm.markAsTouched();
        this.lineUpdated(model.lineId);
    }

    getNumberOfUnitsValue(dataItemId: number, control: string) {
        if (this.chargeDetailsArrayForm.controls.length > 0) {
            for (let i = 0; i < this.chargeDetailsArrayForm.controls.length; i++) {
                if (this.chargeDetailsArrayForm.controls[i].value.id == dataItemId) {
                    const controlElement = this.chargeDetailsArrayForm.at(i);
                    return controlElement.get(control)!.value;
                }
            }
        }
    }

    onNoOfUnitsValueChange(event: any, control: string, dataItemId: number) {
        if (this.chargeDetailsArrayForm.controls.length > 0) {
            const value = (event?.target?.value == null || event?.target?.value == "" || event?.target?.value == undefined) ? null : +event.target.value;
            for (let i = 0; i < this.chargeDetailsArrayForm.controls.length; i++) {
                if (this.chargeDetailsArrayForm.controls[i].value.id == dataItemId) {
                    const controlElement = this.chargeDetailsArrayForm.at(i);
                    controlElement.get(control)!.setValue(value);
                    controlElement.get(control)!.markAsTouched();

                    if (this.viewData !== undefined) {
                        this.calculateTotal(this.viewData);
                    }

                    this.lineUpdated(dataItemId);
                    break;
                }
            }
        }
    }

    isMarkedAsInvalid(dataItemId: number, controlName: string) {
        if (this.chargeDetailsArrayForm.controls.length > 0) {
            for (let i = 0; i < this.chargeDetailsArrayForm.controls.length; i++) {
                if (this.chargeDetailsArrayForm.controls[i].value.id == dataItemId) {
                    const controlElement = this.chargeDetailsArrayForm.at(i);
                    const control = controlElement.get(controlName) as FormControl;
                    return control.invalid;
                }
            }
        }
    }

    currencyInputChange(event: any, lineId: number) {
        if (!(event.key == Number(event.key) || event.key == '.')) {
            event.preventDefault();
        }

        const reg = /^\d{0,6}(\.\d{0,2})?$/;
        let input = event.target.value + String.fromCharCode(event.charCode);
        if (!reg.test(input)) {
            event.preventDefault();
        }
    }

    transformPerUnitChargeAmount(element: any, lineId: number): void {
        let line = this.chargeDetailsData.find(x => x.id == lineId);
        let charge = line?.perUnitsCharge;

        if (charge == null || charge == '' || charge == undefined) {
            return;
        }
        let transformedValue = this.currencyPipe.transform(charge);
        line.perUnitsCharge = transformedValue!.substring(1);
    }

    billedAmountTotal: number;
    paymentAmountTotal: number;
    patientAmountTotal: number;
    balanceAmountTotal: number;
    adjustmentAmountTotal: number;

    calculateTotal(data) {
        this.billedAmountTotal = 0;
        this.adjustmentAmountTotal = 0;
        this.patientAmountTotal = 0;
        this.paymentAmountTotal = 0;
        this.balanceAmountTotal = 0;

        this.viewData = data;
        this.viewData.forEach(ele => {
            this.billedAmountTotal = this.billedAmountTotal + (ele.units * ele.perUnitsCharge);
            this.paymentAmountTotal = this.paymentAmountTotal + ele.paymentAmount;
            this.adjustmentAmountTotal = this.adjustmentAmountTotal + ele.adjustmentAmount;
            this.patientAmountTotal = this.patientAmountTotal + ele.patientAmount;
            //this.balanceAmountTotal = this.balanceAmountTotal + (this.billedAmountTotal - this.paymentAmountTotal + this.patientAmountTotal + this.adjustmentAmountTotal);
            // this.balanceAmountTotal = this.balanceAmountTotal + ((ele.units * ele.perUnitsCharge) + ele.patientAmount - ele.paymentAmount + ele.adjustmentAmount);
        });

        this.balanceAmountTotal = this.billedAmountTotal - this.paymentAmountTotal + this.patientAmountTotal + this.adjustmentAmountTotal;
    }
}
