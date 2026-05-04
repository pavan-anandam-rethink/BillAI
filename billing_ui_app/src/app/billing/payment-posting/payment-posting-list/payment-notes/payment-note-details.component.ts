import { Component, EventEmitter, Input, OnInit, Output } from "@angular/core";
import { PaymentNote } from "@core/models/billing";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { PaymentNotesService } from "@core/services/billing";
import {ConfirmDialog} from "@core/models/common";
import { AccountPermissions } from "@core/enums/account";
import { PaymentNoteDeleteModel } from "@core/models/billing/notes/payment-posting-note";

import { NotificationService } from "@progress/kendo-angular-notification";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";

@Component({
    selector: 'payment-note-details',
    templateUrl: './payment-note-details.html',
    styleUrls: ['./payment-note-details.css']
})
export class PaymentNoteDetailsComponent implements OnInit {
    @Input() notes: PaymentNote[];
    @Input() hideActions: boolean;
    isAdmin: boolean;
    @Output() onReload = new EventEmitter();

    deleteConfirmation = new ConfirmDialog(false, "Confirmation", "Are you sure you want to delete this note?");
    noteToDelete: PaymentNoteDeleteModel = {
        id: 0,
        dateCreated: undefined,
        memberId: 0
    };
    canEdit: boolean;
    
    constructor(
        private paymentNotesService: PaymentNotesService,
        private accountService: AccountMemberService,
        private notificationService: NotificationHandlerService
    ) {
        this.accountService
            .accountMemberSettings
            .subscribe((x) => {
                if (x) {
                    this.isAdmin = x.memberRole.toLowerCase().indexOf('admin') > -1;
                    this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingPostPayments);
                }
            });
    }
    ngOnInit(): void {
        this.notes.forEach((x: any) => x.clipped = true);
    }


    deleteNote(note: PaymentNote) {
        this.noteToDelete.id = note.id;
        this.noteToDelete.dateCreated = note.dateCreated;
        this.noteToDelete.memberId = this.accountService.memberDetails.memberId;
        this.deleteConfirmation.opened = true;
    }

    acceptDeleteNote(isAccepted: boolean) {
        if (isAccepted) {
            this.paymentNotesService.deleteNote(this.noteToDelete).subscribe(x => { if (x) { this.onReload.emit(true) } 
            this.notificationService.showNotificationSuccess("Note deleted successfully.")});
        }
    }

    canDelete() {
        return !this.hideActions && (this.isAdmin || this.canEdit);
    }
}