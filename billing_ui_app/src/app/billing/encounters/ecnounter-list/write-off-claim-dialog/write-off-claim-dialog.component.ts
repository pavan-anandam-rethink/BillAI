import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AmountType } from '@core/enums/billing/amount-type';
import { ClaimOrChargeToWriteOff, WriteOffClaimModelWithUserInfo } from '@core/models/billing/write-off-claim-model';
import { DialogRef } from '@progress/kendo-angular-dialog';


export interface WriteOffClaimDialogResult {
    data: WriteOffClaimModelWithUserInfo[];
}

@Component({
    selector: 'write-off-claim-dialog',
    templateUrl: './write-off-claim-dialog.component.html',
    styleUrls: ['./write-off-claim-dialog.component.css']
})

export class WriteOffClaimDialogComponent {
    formGroup: FormGroup;
    claimsOrChargeToWriteOff: ClaimOrChargeToWriteOff[];
    balance = 0;
    isServiceLine: boolean;

    readonly AmountType = AmountType;

    readonly amountTypeList = [
        { id: 1, name: 'Remaining Amount' },
        { id: 2, name: 'Discount Percentage' },
        { id: 3, name: 'Other Amount' },
    ];
    readonly amountTypeListWithMultiSelect = [
        { id: 1, name: 'Remaining Amount' },
        { id: 2, name: 'Discount Percentage' },
       
    ];

    readonly applicationList = [
        { id: 1, name: 'Newest Procedures First' },
        { id: 2, name: 'Oldest Procedures First' },
        { id: 3, name: 'Highest Balance First' },
        { id: 4, name: 'Lowest Balance First' },
        { id: 5, name: 'Evenly Across Procedures' },
    ];

    readonly writeOffReasonCodeList = [
        { id: 1, name: 'Financial Hardship' },
        { id: 2, name: 'MUE Adjustment' },
        { id: 3, name: 'Exceeding Authorization' },
        { id: 4, name: 'Bad Debt Adjustment' },
        { id: 5, name: 'Small Balance Adjustment' },
        { id: 6, name: 'Out of Network Adjustment' },
        { id: 7, name: 'Prompt Pay Discount' },
        { id: 8, name: 'Other' },
    ];

    constructor(private fb: FormBuilder, private dialog: DialogRef) {
        this.formGroup = this.fb.group({
            amountTypeId: this.fb.control(null, [Validators.required]),
            amount: this.fb.control(null),
            applicationTypeId: this.fb.control(null),
            reasonCodeId: this.fb.control(null, [Validators.required]),
            note: this.fb.control(""),
        });
    }

    cancel(): void {
        this.dialog.close();
    }

    submit(): void {
        if (this.formGroup.valid) {
            const writeOffClaims: WriteOffClaimModelWithUserInfo[] = [];
            const model: WriteOffClaimModelWithUserInfo = this.formGroup.value;
            const amount = model.amount;
            const amountTypeId = model.amountTypeId;
            this.claimsOrChargeToWriteOff.forEach(claimToWriteOff => {                
                model.claimId = claimToWriteOff.claimId;
                model.amount = this.getAmount(claimToWriteOff, amount, amountTypeId);
                model.isServiceLine = this.isServiceLine;
                if(this.isServiceLine)
                    model.serviceLineId = claimToWriteOff.chargeId;
                writeOffClaims.push(Object.assign({}, model));
            });    
           // console.log("Writeoff" ,this.claimsOrChargeToWriteOff);        

            const result: WriteOffClaimDialogResult = { data: writeOffClaims };
            this.dialog.close(result);
        }        
    }

    updateAmountValueValidators() {
        const amountIdControl = this.formGroup.get('amountTypeId');
        if (amountIdControl && amountIdControl.value === AmountType.PercentOfRemaining) {
            this.formGroup.controls['amount'].setValidators([Validators.required, Validators.min(0), Validators.max(100)]);
            this.formGroup.controls['amount'].updateValueAndValidity({ onlySelf: false, emitEvent: false });
            this.formGroup.controls['applicationTypeId'].setValidators([Validators.required]);
            this.formGroup.controls['applicationTypeId'].updateValueAndValidity({ onlySelf: false, emitEvent: false });
        }
        else if (amountIdControl && amountIdControl.value === AmountType.Other) {
            const balances = this.claimsOrChargeToWriteOff.select(claimToWriteOff => claimToWriteOff.balanceAmount);            
            const smallestBalance = Math.min(...balances);
            // balance should be used as max value in the validation, 
            // for several claims the min value of all balances is used
            this.balance=smallestBalance;
            this.formGroup.controls['amount'].setValidators([Validators.required, Validators.max(smallestBalance)]);
            this.formGroup.controls['amount'].updateValueAndValidity({ onlySelf: false, emitEvent: false });
            this.formGroup.controls['applicationTypeId'].setValidators([Validators.required]);
            this.formGroup.controls['applicationTypeId'].updateValueAndValidity({ onlySelf: false, emitEvent: false });
        } 
        else {
            this.formGroup.controls['amount'].clearValidators();
            this.formGroup.controls['amount'].updateValueAndValidity({ onlySelf: false, emitEvent: false });
            this.formGroup.controls['applicationTypeId'].clearValidators();
            this.formGroup.controls['applicationTypeId'].updateValueAndValidity({ onlySelf: false, emitEvent: false });
        }
        if(this.isServiceLine) {
            this.formGroup.controls['applicationTypeId'].clearValidators();
            this.formGroup.controls['applicationTypeId'].updateValueAndValidity({ onlySelf: false, emitEvent: false });
        }
    }

    getAmount(claimOrChargeToWriteOff: ClaimOrChargeToWriteOff, amount: number, amountTypeId: number) {
        switch (amountTypeId) {
            case AmountType.AllRemaining: return claimOrChargeToWriteOff.balanceAmount;
            case AmountType.PercentOfRemaining: return amount * (claimOrChargeToWriteOff.balanceAmount / 100);
            case AmountType.Other:
            default:
                return amount;
        }
    }
}