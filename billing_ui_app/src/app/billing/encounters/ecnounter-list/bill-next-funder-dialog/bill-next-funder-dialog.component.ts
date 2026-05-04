import { Component } from '@angular/core';
import { FormBuilder, FormControl, Validators } from '@angular/forms';
import { DialogRef } from '@progress/kendo-angular-dialog';
import { ClaimPatientFunderOptionModel } from '@core/models/billing/claim-patient-funder-option-model';
import { ClaimService } from '@core/services/billing';
import { ClaimValidationModel } from '@core/models/billing/claim-validation-model';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { NotificationService } from '@progress/kendo-angular-notification';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { billingMode } from '@core/enums/billing/billingMode';
import { takeUntil } from 'rxjs';
import { TakeUntilDestroyService } from '@core/services/common/takeuntill-destroy.service';

export interface BillNextFunderDialogResult {
    submit: boolean;
    funderId: number;
    controlNumber: string;
    isClaimLevelAdjustment: boolean;
}

@Component({
    selector: 'bill-next-funder-dialog',
    templateUrl: './bill-next-funder-dialog.component.html',
    styleUrls: ['./bill-next-funder-dialog.component.css']
})

export class BillNextFunderDialogComponent {

    funders: ClaimPatientFunderOptionModel[];

    funderId: FormControl;
    controlNumber: FormControl;
    isClaimLevelAdjustment: FormControl;
    claimId: number;
    isLoading: boolean = false;
    mode: billingMode;
    billingMode = billingMode;


    constructor(private fb: FormBuilder, private dialog: DialogRef, private destroy: TakeUntilDestroyService,private claimsService: ClaimService, 
        private accountService: AccountMemberService, private notificationService: NotificationHandlerService
    ) {
        this.funderId = this.fb.control(null, [Validators.required]);
        this.controlNumber = this.fb.control(null, [Validators.required, Validators.minLength(1), Validators.maxLength(50)]);
        this.isClaimLevelAdjustment = this.fb.control(null, [Validators.required]);
    }

    cancel(): void {
        this.dialog.close();
    }

    submit(): void {
        if (this.funderId.valid) {
            const result: BillNextFunderDialogResult = {
                submit: true,
                funderId: this.funderId.value.id,
                controlNumber: this.controlNumber.value,
                isClaimLevelAdjustment: this.isClaimLevelAdjustment.value
            };

            this.dialog.close(result);
        }
    }

    reRunValidation() {
        const validateModel: ClaimValidationModel = {
                                                Id : this.claimId,
                                                isSecondary: true,
                                                secondaryFunderId: this.funderId.value.id,
                                                AccountInfoId: this.accountService.memberDetails.accountInfoId,
                                                MemberId: this.accountService.memberDetails.memberId
                                            };
        
        this.claimsService.ReRunValidation(validateModel).pipe(takeUntil(this.destroy.destroy)).subscribe(x => {
            this.notificationService.showNotificationSuccess('Claim has been validated successfully.');
        });
    }

    ngAfterViewInit(): void {
        if (this.funders.any) {
            this.funderId.setValue(this.funders[0]);
        }
        this.isClaimLevelAdjustment.setValue(true);
    }
}