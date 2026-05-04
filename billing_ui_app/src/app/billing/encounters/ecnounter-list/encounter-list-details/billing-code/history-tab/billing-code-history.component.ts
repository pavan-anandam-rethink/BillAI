import { Component, Input, OnChanges, OnDestroy, SimpleChanges } from "@angular/core";
import { ClaimHistory } from "@core/models/billing/claim-history";
import { Subject } from "rxjs";
import { ClaimService } from "@core/services/billing";
import { takeUntil } from "rxjs/operators";
import { ClaimAction } from '@core/enums/billing/claim-action';

@Component({
    selector: 'billing-code-history',
    templateUrl: './billing-code-history.component.html',
    styleUrls: ['./billing-code-history.component.css',
        '../billing-code.component.css']
})

export class BillingCodeHistoryComponent implements OnDestroy, OnChanges {
    @Input() claimId: number;

    public claimHistoryModels: ClaimHistory[] = []
    public claimAction = ClaimAction;
    private unsubscribeAll$ = new Subject<void>();

    constructor(private claimService: ClaimService) {
    }

    loadHistory() {
        this.claimService.getClaimHistory(this.claimId)
            .pipe(takeUntil(this.unsubscribeAll$))
            .subscribe(x => {
                this.claimHistoryModels = x;
            } );

    }

    ngOnDestroy() {
        this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }

    ngOnChanges(_changes: SimpleChanges) {
        if (this.claimId > 0) {
            this.loadHistory();
        }
    }
}