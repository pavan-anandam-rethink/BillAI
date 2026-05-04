import { Component, Input, OnChanges, OnDestroy, SimpleChanges } from "@angular/core";
import { PaymentClaimServiceLineSmall } from "@core/models/billing";
import { AdjustmentService, ClaimPostingService, ClaimService } from "@core/services/billing";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";
import { Router } from "@angular/router";
import { SidebarService } from "@app/shared/components/sidebar";
import { WriteoffService } from "@core/services/billing/writeoff.service";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { Adjustment } from "@core/models/billing/adjustment";
import { GetChargeDetails } from "@core/models/billing/get-charge-details";
import { WriteOffChargeEntryModel} from "@core/models/billing/write-off-charge-entry-model";
@Component({
    selector: 'payment-history-details',
    templateUrl: './payment-history-details.component.html',
    styleUrls: ['./payment-history-details.component.css']
})

export class PaymentHistoryDetailsComponent implements OnDestroy, OnChanges {
    @Input() serviceLineId: number;
    @Input() patientId: number;
    @Input() writeOffs: WriteOffChargeEntryModel[]; // Receive write-offs from parent to avoid duplicate API call
    private unsubscribeAll$ = new Subject<void>();
    isServiceLineEdit = false;
    canEdit: boolean;
    public isLoading=false;
    private unsubscribe = new Subject<void>();

    serviceLines: PaymentClaimServiceLineSmall[] = [];
    adjustments: Adjustment[];

    constructor(private adjustmentService: AdjustmentService, private claimPostingService: ClaimPostingService, private claimService: ClaimService,
        private sidebarService: SidebarService, private router: Router, private accountService: AccountMemberService,private writeOffService: WriteoffService) {
    }

    closeSidebarWithRedirectToPayment(paymentId: number) {
        this.sidebarService.closeAll();
        this.router.navigate(['/billing/paymentposting/edit', paymentId])
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
                    if (this.patientId) {
                    this.sidebarService.emitAdjustmentChanged(this.patientId);
                }
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
        let GetChargeDetailsModel : GetChargeDetails = { id : this.serviceLineId, isServiceLine: true};
        this.claimPostingService.getPaymentClaimServiceLinesSmall(GetChargeDetailsModel).pipe(takeUntil(this.unsubscribeAll$))
            .subscribe(result => {
                this.serviceLines = result;
            });
    }


    ngOnDestroy() {
        this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }

    ngOnChanges(_changes: SimpleChanges) {
        if (this.serviceLineId > 0) {
            this.loadLines();
            this.loadAdjustments()
            // Write-offs are now passed from parent component via @Input
            // this.loadWriteOffs(this.serviceLineId) // REMOVED: Duplicate API call
        }
    }

    // calculateAdjustmentsTotal() {
    //     return this.adjustments == undefined ?
    //         0 : this.adjustments.where(x => x.groupCode != 'PR').sum((x: Adjustment) => x.amount);
    // }

    calculateAdjustmentsTotal() {
        return this.adjustments == undefined ?
            0 : this.adjustments.where((x: Adjustment) => x.isPositive == true).sum((x: Adjustment) => x.amount) - (this.adjustments.where((x: Adjustment) => x.isPositive == false).sum((x: Adjustment) => x.amount));
    }

    loadAdjustments() {
        let GetChargeDetailsModel : GetChargeDetails = { id : this.serviceLineId, isServiceLine: true};
        this.adjustmentService.getServiceLineAdjustmentsByChargeId(GetChargeDetailsModel)
            .pipe(takeUntil(this.unsubscribe))
            .subscribe(x => {
                this.adjustments = x;
            });
    }
    calculateWriteOffsTotal() {
            return this.writeOffs == undefined ?
                0 : this.writeOffs.sum((x: WriteOffChargeEntryModel) => x.writeOffAmount);
        }
}
