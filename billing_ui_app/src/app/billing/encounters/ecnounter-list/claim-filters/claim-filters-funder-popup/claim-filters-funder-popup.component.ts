import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from "@angular/core";
import { ClaimService } from "@core/services/billing";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";
import { ClaimListingTab } from "@core/enums/billing/claim-listing-tab";
import { ClaimFilterOptionModel } from "@core/models/billing/claim-filter-option-model";
import { AccountMemberService } from "@core/services/account/account-member.service";


@Component({
    selector: 'claim-filters-funder-popup',
    templateUrl: './claim-filters-funder-popup.component.html',
    styleUrls: ['./claim-filters-funder-popup.component.css'],
})

export class ClaimFiltersFunderPopupComponent implements OnDestroy, OnInit {
    @Output() funderClicked = new EventEmitter<number>();
    @Input() selectedFunders: ClaimFilterOptionModel[];
    @Input() tab: ClaimListingTab | null;
    private unsubscribeAll$ = new Subject<void>();

    totalCount: number;
    funders: ClaimFilterOptionModel[] = [];
    isLoading: boolean;

    searchTimeout: any;

    constructor(private claimsService: ClaimService, private accountService: AccountMemberService) {
    }

    fundersSearchValueChanged(event: any) {
        this.searchFunders(event.target.value);
    }

    searchFunders(funderName: string) {
        if (this.searchTimeout)
            clearTimeout(this.searchTimeout)

        this.searchTimeout = setTimeout(() => {
            this.isLoading = true;

            this.claimsService.getClaimFunders({ Tab: this.tab, SearchValue: funderName, AccountInfoId: this.accountService.memberDetails.accountInfoId })
                .pipe(takeUntil(this.unsubscribeAll$))
                .subscribe(x => {
                    this.funders = this.selectedFunders;
                    this.funders = this.funders.concat(x.where((p: ClaimFilterOptionModel) =>
                        !this.selectedFunders.any((s: ClaimFilterOptionModel) => s.id == p.id)))

                    this.isLoading = false;
                });
        }, 1000);
    }

    onFunderClicked(funder: ClaimFilterOptionModel) {
        if (funder.checked) {
            this.selectedFunders.remove(funder);
            funder.checked = false;
        } else {
            this.selectedFunders.push(funder);
            funder.checked = true;
        }

        this.funderClicked.emit()
    }

    ngOnDestroy(): void {
        if (this.searchTimeout)
            clearTimeout(this.searchTimeout);

        this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }

    ngOnInit(): void {
        this.funders = [...this.selectedFunders];
        this.searchFunders("");
    }
}