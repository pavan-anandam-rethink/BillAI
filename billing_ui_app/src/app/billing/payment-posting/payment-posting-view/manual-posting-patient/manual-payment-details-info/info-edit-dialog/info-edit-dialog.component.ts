import {
    Component,
    EventEmitter,
    Input,
    OnChanges,
    OnInit,
    Output,
    SimpleChanges,
    ViewEncapsulation
} from "@angular/core";
import {PaymentPostingMethods, PaymentSummary, UpdateManualPaymentSummary} from "@core/models/billing";
import {PaymentPostingService} from "@core/services/billing";
import {UpdatePaymentSummary} from "@core/models/billing/update-payment-summary";
import {takeUntil} from "rxjs/operators";
import { NotificationService } from "@progress/kendo-angular-notification";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";

@Component({
    selector: 'info-edit-dialog',
    templateUrl: './info-edit-dialog.html',
    styleUrls: ['./info-edit-dialog.css'],
    encapsulation: ViewEncapsulation.None
})

export class InfoEditDialogComponent implements OnInit, OnChanges{
    @Input() paymentSummary: PaymentSummary;
    @Input() paymentMethods: PaymentPostingMethods[];
    @Output() onSaveChanges = new EventEmitter();
    isOpened: boolean = false;

    editedSummary: PaymentSummary;
    //paymentMethods: PaymentPostingMethods[] = [];
    paymentMethodNames: string[] = [];
    paymentMethodValues: string[] = [];

    isUpdating: boolean = false;

    depositDate: Date|undefined;
    datePosted: Date|undefined;
    referenceNumber = '';
    paymentAmount: number;

    paymentMethodTypes: string[] = ['Credit Card', 'Cash', 'Check', 'FSA/HSA'];
    
    constructor(private notificationService: NotificationHandlerService,private paymentPostingService: PaymentPostingService) {}

    open(): void {
        this.isOpened = true;

        this.depositDate = this.paymentSummary.depositDate ? new Date(this.paymentSummary.depositDate) : undefined;
        this.datePosted = this.paymentSummary.postDate ? new Date(this.paymentSummary.postDate) : new Date();
        this.referenceNumber = this.paymentSummary.referenceNumber;
        this.paymentAmount = this.paymentSummary.paymentAmount;

        this.editedSummary = {...this.paymentSummary};
    }

    close(): void {
        this.isOpened = false;
    }

    save(): void {
        let model: UpdateManualPaymentSummary = {
            id: this.paymentSummary.id,
            depositDate: this.depositDate,
            postDate: this.datePosted,
            referenceNumber: this.referenceNumber,
            paymentMethodId: this.editedSummary.paymentMethodId,
            paymentMethod: this.editedSummary.paymentMethod,
            paymentMethodEntity: this.editedSummary.paymentMethodEntity,
            paymentAmount: this.paymentAmount,
        };

        let currentDate = new Date();
        let currentTimeZoneOffset = currentDate.getTimezoneOffset();

        if(model.depositDate != undefined)
          model.depositDate.setTime(model.depositDate.getTime() - currentTimeZoneOffset * 1000 * 60);
        if(model.postDate != undefined)
          model.postDate.setTime(model.postDate.getTime() - currentTimeZoneOffset * 1000 * 60);

        this.isUpdating = true;
        
        this.paymentPostingService.updateManualPaymentSummary(model)
            .subscribe(() => {
                this.isOpened = false;
                this.isUpdating = false;
                this.onSaveChanges.emit();
                this.notificationService.showNotificationSuccess("Payment summary updated successfully.");
            });
    }

    getPaymentName(id: number) {
        if (id != undefined) {
            let method = this.paymentMethods.find(x => x.enumValue == id.toString());
            return method != undefined ? method.displayName : "";
        }

        return ""
    }
    
    ngOnChanges(changes: SimpleChanges) {
        if(this.paymentMethods !== undefined && this.paymentMethods.length > 0){
            this.paymentMethodNames = [];
            this.paymentMethodValues = [];
            this.paymentMethodTypes.forEach(typeName => {
                let method = this.paymentMethods.find((p: PaymentPostingMethods) => p.displayName == typeName);
                if(method != undefined){
                    this.paymentMethodNames.push(method.displayName);
                    this.paymentMethodValues.push(method.enumValue);
                }
            });
        }
    }

    ngOnInit() {
        
    }

}