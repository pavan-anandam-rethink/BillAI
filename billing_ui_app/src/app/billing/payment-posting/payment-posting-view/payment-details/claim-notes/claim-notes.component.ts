import { Component, OnInit } from "@angular/core";
import { ClaimNote, ClaimNotesFilter, PatientDetails } from "@core/models/billing";
import { ClaimNotesService, ClaimPostingService } from "@core/services/billing";
import { Observable, Subject } from "rxjs";
import { FormBuilder, Validators } from "@angular/forms";
import { MatTabChangeEvent } from "@angular/material/tabs";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { ClaimNoteGetAllModel, ClaimNoteGetModel, ClaimNoteSaveModel } from "@core/models/billing/notes/cliam-posting-note";
import { takeUntil } from "rxjs/operators";
import { Locale } from "@app/locale";
import { AccountPermissions } from "@core/enums/account";

import { NotificationService } from "@progress/kendo-angular-notification";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";

@Component({
    selector: 'claim-notes',
    templateUrl: './claim-notes.html',
    styleUrls: ['./claim-notes.css']
})
export class ClaimNotesComponent implements OnInit {
    public unsubscribe$ = new Subject<void>();

    notes: ClaimNote[] = [];
    filter: ClaimNotesFilter;
    addMode = false;
    claim: ClaimNoteGetModel;
    patient$: Observable<PatientDetails>
    showErrors = false;
    saving = true;
    minDate = new Date();
    showButtons = true;
    canEdit: boolean;

    public activeTabChanged(event: MatTabChangeEvent): void {
        this.showButtons = event.tab.textLabel !== 'Deleted';
    }

    constructor(
        private claimNotesService: ClaimNotesService,
        private claimPostingService: ClaimPostingService,
        private fb: FormBuilder,
        private accountService: AccountMemberService,
        public locale: Locale,
        private notificationService: NotificationHandlerService
    ) {

        this.accountService
            .accountMemberSettings
            .subscribe((x) => {
                if (x) {
                    this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingEdit);
                    // this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingPostPayments);
                }
            });
    }

    setData(claim: ClaimNoteGetModel) {
        this.claim = claim;
        this.filter = { active: true, claimId: claim.id };
        this.reload();
        this.patient$ = this.claimPostingService.getPatientDetails(claim.patientId)
    }

    getActive(notes: ClaimNote[]) {
        const result = notes.filter(n => !n.dateDeleted);
        return result;
    }
    getInactive(notes: ClaimNote[]) {
        const result = notes.filter(n => !!n.dateDeleted);
        return result;
    }

    reload() {
        const claimNoteRequest: ClaimNoteGetAllModel = {
            id: this.claim.id,
            accountInfoId: this.accountService.memberDetails.accountInfoId
        }
        this.claimNotesService.getAll(claimNoteRequest).pipe(takeUntil(this.unsubscribe$))
            .subscribe((result) => {
                if (result.success) {
                    this.notes = result.data;
                }
            });
    }

    noteForm = this.fb.group({
        remindDate: this.fb.control(null, [Validators.required]),
        note: this.fb.control(null, [Validators.required])
    });


    checkTextAndDateArea() {
        if (this.noteForm.value.note.trim() !== '' && this.noteForm.value.remindDate !== null) {
            this.saving = false;
        } else {
            this.saving = true;
        }
    }
    
    addNote() {
        this.addMode = true;
    }

    cleanup() {
        this.addMode = false;
        this.noteForm.reset();
        this.showErrors = false;
        this.saving = true
    }

    saveNote() {
        if (this.saving) {
            return;
        }

        if (this.noteForm.invalid) {
            this.showErrors = true;
            this.saving = true;
            return;
        }

        const model = this.noteForm.value as ClaimNoteSaveModel;
        const currentDate = new Date();
        const currentTimeZoneOffset = currentDate.getTimezoneOffset();

        model.remindDate.setTime(model.remindDate.getTime() - currentTimeZoneOffset * 1000 * 60);
        model.claimId = this.filter.claimId;
        model.memberId = this.accountService.memberDetails.memberId;
        //Comment added for testing

        this.claimNotesService.addNote(model).pipe(takeUntil(this.unsubscribe$)).subscribe(x => {
            this.reload();
            this.addMode = false;
            this.noteForm.reset();
            this.saving = true;
            this.notificationService.showNotificationSuccess("Note added successfully.");
        }, x => {
            this.saving = false;
        });

    }

    ngOnInit() {
        const defaultDate = new Date();
        defaultDate.setDate(this.minDate.getDate() + 30);

        this.noteForm.get("remindDate")!.setValue(defaultDate);
    }
}