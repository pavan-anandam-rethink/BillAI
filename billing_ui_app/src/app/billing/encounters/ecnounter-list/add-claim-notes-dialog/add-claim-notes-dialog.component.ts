import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { DialogRef } from '@progress/kendo-angular-dialog';
import { ClaimNote } from '@core/models/billing';
import { Locale } from '@app/locale';

export interface AddClaimNotesDialogResult {
    data: ClaimNote[];
}

@Component({
    selector: 'add-claim-notes-dialog',
    templateUrl: './add-claim-notes-dialog.component.html',
    styleUrls: ['./add-claim-notes-dialog.component.css']
})

export class AddClaimNotesDialogComponent {
    formGroup: FormGroup;
    minDate = new Date();
    defaultDate = new Date();
    claimIds: number[] = [];
    showErrors = false;
    addMode = false;
    AddNote = true;

    constructor(private fb: FormBuilder, private dialog: DialogRef,public locale: Locale) {
        
        this.defaultDate.setDate(this.minDate.getDate() + 30);
        this.formGroup = this.fb.group({
            remindDate: this.fb.control(this.defaultDate, [Validators.required]),
            note: this.fb.control(null, [Validators.required])
        });
        
    }
    
    cancel(): void {
        this.dialog.close();
    }

    noteForm = this.fb.group({
        remindDate: this.fb.control(null, [Validators.required]),
        note: this.fb.control(null, [Validators.required])
    });

     checkTextAndDateArea() {
        if (this.formGroup.value.note.trim() !== '' && this.formGroup.value.remindDate !== null) {
            this.AddNote = false;
        } else {
            this.AddNote = true;
        }
    }

    addNote(): void {
        if (this.formGroup.valid) {
            const model = this.formGroup.value as ClaimNote;

            const currentDate = new Date();
            const currentTimeZoneOffset = currentDate.getTimezoneOffset();

            const claimsToAddNote: ClaimNote[] = [];

            this.claimIds.forEach(claimId => {
                model.remindDate.setTime(model.remindDate.getTime() - currentTimeZoneOffset * 1000 * 60);
                model.claimId = claimId;
                claimsToAddNote.push(Object.assign({}, model));
            });

            const result: AddClaimNotesDialogResult = { data: claimsToAddNote };
            this.dialog.close(result);
        }
    }
}
