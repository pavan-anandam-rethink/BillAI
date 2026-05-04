import { Component } from '@angular/core';
import { DialogRef } from '@progress/kendo-angular-dialog';

export interface VoidClaimDialogResult {
    submit: boolean;
    option: boolean;
    note: string;
    claimnote: string;
}

@Component({
    selector: 'void-claim-dialog',
    templateUrl: './void-claim-dialog.component.html',
    styleUrls: ['./void-claim-dialog.component.css']
})

export class VoidClaimDialogComponent {
    note = '';
    claimnote ='';
    option :boolean | null = null;

    constructor(private dialog: DialogRef) {}

    submit() {
        const result: VoidClaimDialogResult = { submit: true, option: this.option, note: this.note, claimnote:this.claimnote };
        this.dialog.close(result);
    }

    close() {
        this.dialog.close();
    }
}