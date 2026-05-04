import { Component, Input } from '@angular/core';
import { AbstractControl, FormControl, FormGroup, Validators } from '@angular/forms';
import { FunderType } from '@core/enums/clients';
import { ClaimDetailsInfoModel } from '@core/models/billing';
import { BasicOption } from '@core/models/common';

@Component({
  selector: 'app-encounter-details-additional-info',
  templateUrl: './encounter-details-additional-info.component.html',
  styleUrls: ['./encounter-details-additional-info.component.css']
})
export class EncounterDetailsAdditionalInfoComponent {
  @Input() claim: ClaimDetailsInfoModel;
    @Input() claimForm: FormGroup;
    @Input() public placeOfServices: BasicOption[] = [];

    funderIsInsurance: boolean = false;

    ngOnInit(): void {
        this.funderIsInsurance = this.claim.funderTypeId == FunderType.Insurance;
        this.updateValidators();

         // Listen for submissionReason changes
        this.claimForm.get('submissionReason')?.valueChanges.subscribe(value => {
        this.updateSubmissionValidators(value);
});
    }

    benefitsAssignmentIndicatorTypes = [
        { id: 1, description: "Yes" },
        { id: 2, description: "No" }
    ]

    confirmationTypes = [
        { id: 1, description: "Signature On File" },
        { id: 2, description: "No Signature On File" }
    ]

    submissionReasonTypes = [
        { id: 1, description: "1 - Original claim" },
        { id: 7, description: "7 - Replacement of Prior Claim" },
        { id: 8, description: "8 - Void/Cancel Prior Claim" }
    ]

    private updateValidators(): void {
        if (this.funderIsInsurance) {
            this.claimForm.controls['patientReleaseAgreement'].setValidators([Validators.required]);
            this.claimForm.controls['authorizePayment'].setValidators([Validators.required]);
        } else {
            this.claimForm.controls['patientReleaseAgreement'].setValidators([]);
            this.claimForm.controls['authorizePayment'].setValidators([]);
        }

        this.claimForm.controls['patientReleaseAgreement'].updateValueAndValidity({ emitEvent: false, onlySelf: true });
        this.claimForm.controls['authorizePayment'].updateValueAndValidity({ emitEvent: false, onlySelf: true });
      
        // Initialize submission reason validators
        this.updateSubmissionValidators(this.claimForm.get('submissionReason')?.value);
    }

    private updateSubmissionValidators(submissionReason: number): void {
        if (submissionReason === 7 || submissionReason === 8) {
            this.claimForm.controls['originalClaim'].setValidators([Validators.required]);
            this.claimForm.controls['note'].setValidators([Validators.required]);
        } else {
            this.claimForm.controls['originalClaim'].clearValidators();
            this.claimForm.controls['note'].clearValidators();
        }
        this.claimForm.controls['originalClaim'].updateValueAndValidity();
        this.claimForm.controls['note'].updateValueAndValidity();
     }
     

        hasRequiredValidator(field: string): boolean {
        const control = this.claimForm.get(field);
        if (!control || !control.validator) {
            return false;
        }
        const validatorFn = control.validator({} as AbstractControl);
        return !!(validatorFn && validatorFn['required']);
        }
      

    

}
