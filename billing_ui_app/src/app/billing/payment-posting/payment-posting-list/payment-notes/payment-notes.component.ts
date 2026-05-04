import { Component, OnDestroy, OnInit } from "@angular/core";
import { FormBuilder, Validators } from "@angular/forms";
import { PaymentNote, PaymentNotesFilter, PaymentPosting, PaymentPostingShortInfo, PaymentSummary } from "@core/models/billing";
import { PaymentNotesService, PaymentPostingService } from "@core/services/billing";
import { Observable, Subject } from "rxjs";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { takeUntil } from "rxjs/operators";
import { Locale } from "@app/locale";
import { AccountPermissions } from "@core/enums/account";
import { PaymentNoteSaveModel } from "@core/models/billing/notes/payment-posting-note";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";

@Component({
    selector: 'payment-notes',
    templateUrl: './payment-notes.html',
    styleUrls: ['./payment-notes.css', '../../status-actions.css']
})
export class PaymentNotesComponent implements OnInit, OnDestroy {
    public unsubscribe$ = new Subject<void>();

    notes: PaymentNote[] = [];
    filter: PaymentNotesFilter;
    addMode = false;
    payment: PaymentPosting;
    paymentInfo$: Observable<PaymentSummary>
    showErrors = false;
    saving = true;
    paymentShortInfo$: Observable<PaymentPostingShortInfo>;
    minDate = new Date();
    canEdit: boolean;

    get dateOfService() {
        return new Date();
        //TODO: from claim
    };

    constructor(
        private paymentNotesService: PaymentNotesService,
        private paymentPostingService: PaymentPostingService,
        private fb: FormBuilder,
        public locale: Locale,
        private accountService: AccountMemberService,
        private notificationService: NotificationHandlerService
    ) {
    }

    ngOnDestroy() {
        this.unsubscribe$.next();
        this.unsubscribe$.complete();
    }

    ngOnInit() {
        this.checkPermission();
    }

    checkPermission() {
        this.accountService
            .accountMemberSettings
            .subscribe((x) => {
                if (x) {
                    this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingPostPayments);
                }
            })
    }

    setData(payment: PaymentPosting) {
        this.payment = payment;
        this.filter = { active: true, paymentId: payment.id };
        this.reload();
        this.paymentInfo$ = this.paymentPostingService.getSummaryById(payment.id);
        this.paymentShortInfo$ = this.paymentPostingService.getPaymentShortInfo(payment.id);
    }

    getActive(notes: PaymentNote[]) {
        const result = notes.filter(n => !n.dateDeleted);
        return result;
    }

    getInactive(notes: PaymentNote[]) {
        const result = notes.filter(n => !!n.dateDeleted);
        return result;
    }

    reload() {
        this.paymentNotesService.getAll(this.payment.id).pipe(takeUntil(this.unsubscribe$))
            .subscribe((notes: PaymentNote[]) => {
                this.notes = notes;
            });
    }

    noteForm = this.fb.group({
        remindDate: this.fb.control(null, [Validators.required]),
        note: this.fb.control(null, [Validators.required])
    });

    checkTextAndDateArea() {
        if (this.noteForm.value.note?.trim() !== '' && this.noteForm.value.remindDate !== null) {
            this.saving = false;
        }
        else {
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
        this.saving = true;
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

        const model = this.noteForm.value as PaymentNoteSaveModel;

        let currentDate = new Date();
        let currentTimeZoneOffset = currentDate.getTimezoneOffset();

        model.remindDate.setTime(model.remindDate.getTime() - currentTimeZoneOffset * 1000 * 60);
        model.paymentId = this.filter.paymentId;
        model.memberId = this.accountService.memberDetails.memberId;
        this.paymentNotesService.addNote(model).pipe(takeUntil(this.unsubscribe$)).subscribe(x => {
            this.reload();
            this.addMode = false;
            this.noteForm.reset();
            this.saving = true;
            this.notificationService.showNotificationSuccess("Note added successfully.")
        }, x => {
            this.saving = false;
        });
    }
}