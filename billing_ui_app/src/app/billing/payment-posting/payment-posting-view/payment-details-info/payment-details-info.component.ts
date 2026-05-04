import {Component, EventEmitter, forwardRef, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild} from '@angular/core';
import {Router} from '@angular/router';
import {PaymentSummary} from '@core/models/billing';
import {PaymentPostingService} from '@core/services/billing';
import {Subject} from 'rxjs';
import {takeUntil} from 'rxjs/operators';
import { AccountPermissions } from '@core/enums/account';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { PaymentDetailsInfoEditDialogComponent } from './payment-details-info-edit-dialog/payment-details-info-edit-dialog.component';
import { NotificationService } from '@progress/kendo-angular-notification';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';


@Component({
    selector: 'payment-details-info',
    templateUrl: './payment-details-info.html',
    styleUrls: ['./payment-details-info.css']
})
export class PaymentDetailsInfoComponent implements OnChanges, OnDestroy {
    @Input() paymentId: number;
    @Input() paymentIdentifier: string;
    @ViewChild(forwardRef(() => PaymentDetailsInfoEditDialogComponent))
    editDialog: PaymentDetailsInfoEditDialogComponent;
    @Input() isManual: boolean;
    @Output() updateStatus = new EventEmitter<number>();
    private unsubscribe = new Subject<void>();
    payment: PaymentSummary;
    currentDate = new Date();
    canEdit: boolean;

    constructor(
        private readonly router: Router,
        private paymentPostingService: PaymentPostingService,
        private notificationService: NotificationHandlerService,
        private accountService: AccountMemberService
    ) {
        this.accountService
            .accountMemberSettings
            .subscribe((x) => {
                if (x) {
                    this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingEditApprovedAppointments);
                }
            });
    }

    loadSummary() {
        this.paymentPostingService.getSummaryById(this.paymentId)
            .pipe(takeUntil(this.unsubscribe))
            .subscribe(payment => {
                if (payment){
                    this.payment = payment;
                    this.payment.id = this.paymentId;
                }
                else {
                    this.router.navigate(["/billing/not-found"]);
                }
            });
    }

    openEdit() {
        this.editDialog.open();
    }

    onEditChangeSave() {
        this.paymentPostingService.getSummaryById(this.payment.id).pipe(takeUntil(this.unsubscribe)).subscribe(payment => {
            this.payment = payment;
            this.updateStatus.emit(this.payment.id);
            this.notificationService.showNotificationSuccess("Payment summary updated successfully.");
        });
    }

    ngOnDestroy() {
        this.unsubscribe.next();
        this.unsubscribe.complete();
    }
    
    ngOnChanges(changes: SimpleChanges) {
        if(this.paymentId > 0){
            this.loadSummary();
        }
    }
}