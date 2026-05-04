import { Component, EventEmitter, Input, OnInit, Output } from "@angular/core";
import { ClaimNote } from "@core/models/billing";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { ClaimNotesService } from "@core/services/billing";
import { DialogService } from "@progress/kendo-angular-dialog";
import { AccountPermissions } from "@core/enums/account";
import { AccountMemberSettings } from "@core/services/account/account-member.service";
import { NotificationService } from "@progress/kendo-angular-notification";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";


@Component({
    selector: 'claim-note-details',
    templateUrl: './claim-note-details.html',
    styleUrls: ['./claim-note-details.css']
})
export class ClaimNoteDetailsComponent implements OnInit {
    @Input() notes: ClaimNote[];
    @Input() hideActions: boolean;
    isAdmin: boolean;
    @Output() onReload = new EventEmitter();
    canEdit: boolean;

    constructor(
        private claimNotesService: ClaimNotesService,
        private accountService: AccountMemberService,
        private dialogService: DialogService,
        private notificationService: NotificationHandlerService
    ) {
        this.accountService
            .accountMemberSettings
            .subscribe((x: AccountMemberSettings|null) => {
                if (x) {
                    this.isAdmin = x.memberRole.toLowerCase().indexOf('admin') > -1;
                    this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingEdit);
                    // this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingPostPayments);
                }
            });
    }

    ngOnInit(): void {
        this.notes.forEach((x: any) => x.clipped = true);
    }

    markCompleted(note: ClaimNote) {
        const dialog = this.dialogService.open({
            title: 'Please confirm',
            width: 500,
            content: 'Mark note as completed?',
            actions: [
                { text: "Cancel" },
                { text: "Save", primary: true }
            ]
        });
        dialog.result.subscribe((result: any) => {
            if (result.text === 'Save') {
                this.claimNotesService.deleteNote({ id: note.id, dateCreated: note.dateCreated, memberId: this.accountService.memberDetails.memberId}).subscribe(result => { 
                    if (result.success) { 
                        this.onReload.emit(true) 
                    } 
            this.notificationService.showNotificationSuccess("Note Completed Successfully.")});
            }
        });
    }

    canDelete() {
        return !this.hideActions && (this.isAdmin || this.canEdit);
    }
}