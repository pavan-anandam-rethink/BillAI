import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { DialogRef } from '@progress/kendo-angular-dialog';

export interface BillingCodeAddNoteResult {
    save: boolean;
    note: string;
}

@Component({
    selector: 'billing-code-add-note',
    templateUrl: './billing-code-note.component.html',
    styleUrls: ['./billing-code-note.component.css']
})

export class BillingCodeAddNoteComponent {
    note = '';

    constructor(public dialogRef: MatDialogRef<BillingCodeAddNoteComponent>,
        @Inject(MAT_DIALOG_DATA) public data: any) {}

    save() {
        const result: BillingCodeAddNoteResult = { save: true, note: this.note };
        this.dialogRef.close(result);
    }

    close() {
        const result: BillingCodeAddNoteResult = { save: true, note: this.dialogRef.componentInstance.data.name };
        this.dialogRef.close(result);
    }
}