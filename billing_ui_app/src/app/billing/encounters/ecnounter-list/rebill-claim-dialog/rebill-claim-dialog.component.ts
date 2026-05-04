import { Component } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { DialogRef } from '@progress/kendo-angular-dialog';

export interface RebillClaimDialogResult {
    submit: boolean;
    rebillReason: string;
    submissionReasonId: number;
    note: string;
    claimnote: string;
}

@Component({
    selector: 'rebill-claim-dialog',
    templateUrl: './rebill-claim-dialog.component.html',
    styleUrls: ['./rebill-claim-dialog.component.css']
})

export class RebillClaimDialogComponent {
    readonly rebillReasonList = [
        { id: 1, name: 'Corrected Claim' },
        { id: 2, name: 'Pricing Adjustment' },
        { id: 3, name: 'Provider Credentialing' },
        { id: 4, name: 'Authorization' },
        { id: 5, name: 'Network Status' },
    ];

    readonly submissionReasonList = [
        { id: 1, name: '1: Original Submission' },
        { id: 7, name: '7: Replacement of Prior Claim' },
    ]
    formGroup:FormGroup;

    constructor(private fb: FormBuilder, private dialog: DialogRef) {
        this.formGroup=this.fb.group({
            rebillReason : this.fb.control(null, [Validators.required]),
            submissionReasonId : this.fb.control(null, [Validators.required]),
            note : this.fb.control(""),
            claimnote : this.fb.control(""),
        })
    }

    cancel(): void {
        this.dialog.close();
    }

    submit(): void {
        if (this.formGroup.valid) {
            const result: RebillClaimDialogResult = {
                submit: true,
                rebillReason: this.formGroup.value.rebillReason,
                submissionReasonId: this.formGroup.value.submissionReasonId,
                note: this.formGroup.value.note,
                claimnote: this.formGroup.value.claimnote
            };

            this.dialog.close(result);

        }
    }
}