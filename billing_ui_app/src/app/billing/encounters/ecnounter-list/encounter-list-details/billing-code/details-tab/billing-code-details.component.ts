import { Component, Input, OnChanges, OnDestroy, SimpleChanges } from "@angular/core";
import { Appointment, PaymentClaimServiceLineSmall } from "@core/models/billing";
import { ClaimPostingService, ClaimService } from "@core/services/billing";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";
import { Router } from "@angular/router";
import { SidebarService } from "@app/shared/components/sidebar";

import { ServiceLineIdModel } from "@core/models/billing/billing-code";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { GetChargeDetails } from "@core/models/billing/get-charge-details";
import { error } from "console";

@Component({
    selector: 'billing-code-details',
    templateUrl: './billing-code-details.component.html',
    styleUrls: ['./billing-code-details.component.css',
        '../billing-code.component.css']
})

export class BillingCodeDetailsComponent implements OnDestroy, OnChanges {
    @Input() chargeId: number;
    // @Input() hours: number;

    private unsubscribeAll$ = new Subject<void>();
    isServiceLineEdit = false;
    canEdit: boolean;

    serviceLines: PaymentClaimServiceLineSmall[] = [];
    appointments: Appointment[] = [];
    

    constructor(private claimPostingService: ClaimPostingService, private claimService: ClaimService,
        private sidebarService: SidebarService, private router: Router, private accountService: AccountMemberService) {
    }

    closeSidebarWithRedirectToPayment(paymentId: number) {
        this.sidebarService.closeAll();
        this.router.navigate(['/billing/paymentposting/edit', paymentId])
    }

    closeSidebarWithRedirectToScheduler(appointmentId: number) {
        this.sidebarService.closeAll();
        this.router.navigate(['/scheduler/']);
        //TODO change after appointment view will be implemented
        //this.router.navigate(['/scheduler/paymentposting', appointmentId])
    }

    startEditServiceLineAmounts(index: number) {
        this.isServiceLineEdit = true;

        const item = this.serviceLines[index];
        item.allowedAmountOrig = item.allowedAmount;
        item.paidAmountOrig = item.paidAmount;
    }

    saveEditServiceLineAmounts(serviceLineId: number) {
        const serviceLine = this.serviceLines.find(x => x.id == serviceLineId);
        
        if (serviceLine) {
            this.claimPostingService.updatePaymentClaimServiceLineAmounts(serviceLine.id, serviceLine.allowedAmount,
                serviceLine.paidAmount, false)
                .subscribe(x => {
                    this.isServiceLineEdit = false;
                });
        }
    }

    cancelEditServiceLineAmounts(serviceLineId: number) {
        this.isServiceLineEdit = false;

        const serviceLine = this.serviceLines.find(x => x.id == serviceLineId);
        if (serviceLine) {
            serviceLine.allowedAmount = serviceLine.allowedAmountOrig;
            serviceLine.paidAmount = serviceLine.paidAmountOrig;
        }
    }

    removeLeadingDigits(value: number) {
        return value ? parseFloat(value.toFixed(2)) : value;
    }

    loadLines() {
        let GetChargeDetailsModel : GetChargeDetails = { id : this.chargeId, isServiceLine: false};
        this.claimPostingService.getPaymentClaimServiceLinesSmall(GetChargeDetailsModel).pipe(takeUntil(this.unsubscribeAll$))
            .subscribe(result => {
                this.serviceLines = result;
            },
        error =>{
        });
    }

    loadAppointments() {
        const serviceLineIdRequest: ServiceLineIdModel = {
            accountInfoId: this.accountService.memberDetails.accountInfoId,
            serviceLineId: this.chargeId
        }
        this.claimService.getClaimLineAppointments(serviceLineIdRequest).pipe(takeUntil(this.unsubscribeAll$))
            .subscribe(result => {
                this.appointments = result;
            })
    }

    ngOnDestroy() {
        this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }

    ngOnChanges(_changes: SimpleChanges) {
        if (this.chargeId > 0) {
            this.loadLines();
            this.loadAppointments();
        }
    }
}