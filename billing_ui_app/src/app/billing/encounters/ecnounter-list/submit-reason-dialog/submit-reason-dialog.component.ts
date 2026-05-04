import { Component } from '@angular/core';
import { FormBuilder, FormControl, Validators } from '@angular/forms';

import { DialogRef } from '@progress/kendo-angular-dialog';

import { BasicOption } from '@core/models/common';


@Component({
    selector: 'submit-reason-dialog',
    templateUrl: './submit-reason-dialog.component.html',
    styleUrls: ['./submit-reason-dialog.component.css']
})
export class SubmitReasonDialogComponent {
    readonly reasons: BasicOption[] = [
        { id: 1, name: '1 - Original Submission' },
        { id: 6, name: '6 - Corrected Claim' },
        { id: 7, name: '7 - Replacement of Prior Claim' },
        { id: 8, name: '8 - Void/Cancel Prior Claim' }
    ];

    formControl: FormControl;

    constructor(private fb: FormBuilder, private dialog: DialogRef) {
        this.formControl = this.fb.control('', [Validators.required]);
    }

    cancel(): void {
        this.dialog.close();
    }

    process(): void {
        if (this.formControl.valid) {
            this.dialog.close({ reasonId: this.formControl.value });
        }
    }
}