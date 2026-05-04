import {Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges, ViewChild} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import {PaymentPostingMethods, PaymentSummary, UpdateManualPaymentSummary} from '@core/models/billing';
import { PaymentPostingService } from '@core/services/billing';
import {Observable, Subject} from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import {FormBuilder} from "@angular/forms";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { AccountPermissions } from '@core/enums/account';


@Component({
    selector: 'manual-payment-details-info',
    templateUrl: './manual-payment-details-info.component.html',
    styleUrls: ['./manual-payment-details-info.component.css']
})
export class ManualPaymentDetailsInfoComponent implements OnInit, OnDestroy {
    @Input() paymentId: number
    @Input() paymentIdentifier: string;
    private unsubscribe$ = new Subject<void>();
    payment: PaymentSummary;
    currentDate = new Date();
    isEditMode = false;

    @ViewChild("editInfoDialog")editDialog: any;
    @Output() onPaymentSummaryUpdate = new EventEmitter();
    @Output() updateStatus = new EventEmitter<number>();
    paymentMethodsLoaded: PaymentPostingMethods[];
    isRevSpringPayment: boolean | undefined = undefined;
    
    updateModel: UpdateManualPaymentSummary = {
        id: 0,
        depositDate: new Date,
        postDate: new Date,
        referenceNumber: '',
        paymentMethodId: 0,
        paymentMethod: '',
        paymentMethodEntity: {
            displayName: 'NON',
            enumValue: '0'
        }
    };
    canEdit: boolean;

    constructor(private fb: FormBuilder,
        private route: ActivatedRoute,
        private paymentPostingService: PaymentPostingService,
        private accountService: AccountMemberService
    ) {
        this.loadPaymentMethods();
    }

    openEdit() {
        this.editDialog.open();
    }
  
    restoreInitialValues(): void {
        this.updateModel.id = this.payment.id;
        this.updateModel.postDate = this.payment.postDate == undefined ? new Date() : new Date(this.payment.postDate);
        this.updateModel.depositDate = this.payment.depositDate == undefined ? undefined : new Date(this.payment.depositDate);
        this.updateModel.referenceNumber = this.payment.referenceNumber;
    }

    updatePayment() {
        //TODO
    }
    
    getPaymentMethodInitialValue(): void {
        if(this.paymentMethodsLoaded == undefined){
            this.loadPaymentMethods();
            
            window.setTimeout(() => this.getPaymentMethodInitialValue(), 1000);
            return;
        }
        
        let paymentMethodName = this.payment ? this.payment.paymentMethod : "NON";
        if(paymentMethodName == "NON"){
            paymentMethodName = "Non-Payment"
            
            let result = this.paymentMethodsLoaded.find(item => item.displayName == paymentMethodName)
            if(result !== undefined){
                this.updateModel.paymentMethodEntity = result;
            }
        }else{
            let result = this.paymentMethodsLoaded.find(item => item.enumValue == this.payment.paymentMethodId.toString())
            if(result !== undefined){
                this.updateModel.paymentMethodEntity = result;
            }
        }
    }
    
    restoreDefault(): void {
        this.isEditMode = false;
        
        this.restoreInitialValues();
        this.getPaymentMethodInitialValue();
    }
    
    loadPaymentSummary(id: number = this.paymentId, isOnInit = false): void {
        this.paymentPostingService.getSummaryById(id).pipe(takeUntil(this.unsubscribe$))
            .subscribe(payment => {
                this.payment = payment;
                const method = (payment?.paymentMethod || '').toLowerCase();
                this.isRevSpringPayment = method === 'revspring';
                this.restoreDefault();
                this.updateStatus.emit(this.paymentId);
                if (isOnInit) this.onPaymentSummaryUpdate.emit();
                //this.getPaymentMethodInitialValue();
            });
    }
    
    loadPaymentMethods(){
        this.paymentPostingService.getPaymentMethods().pipe(takeUntil(this.unsubscribe$))
            .subscribe(result => {
                this.paymentMethodsLoaded = result
            });
    }

    ngOnInit(): void {
        if(this.paymentId > 0){
            if(this.paymentMethodsLoaded == undefined){
                this.loadPaymentMethods();
            }

            this.accountService
                .accountMemberSettings
                .subscribe((x) => {
                    if (x) {
                        this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingEditApprovedAppointments);
                    }
                });
            
            this.loadPaymentSummary(this.paymentId);
        }
    }

    ngOnDestroy() {
        this.unsubscribe$.next();
        this.unsubscribe$.complete();
    }
}