import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from "@angular/core";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";

import { ClaimService } from "@core/services/billing";
import { ClaimFilterOptionModel } from "@core/models/billing/claim-filter-option-model";
import { ClaimListingTab } from "@core/enums/billing/claim-listing-tab";
import { AccountMemberService } from "@core/services/account/account-member.service";


@Component({
    selector: 'claim-filters-rendering-provider-popup',
    templateUrl: './claim-filters-rendering-provider-popup.component.html',
    styleUrls: ['./claim-filters-rendering-provider-popup.component.css'],
})

export class ClaimFiltersRenderingProviderPopupComponent implements OnDestroy, OnInit {
    @Output() renderingProviderClicked = new EventEmitter<number>();
    @Input() selectedRenderingProviders: ClaimFilterOptionModel[];
    @Input() tab: ClaimListingTab | 1;
    private unsubscribeAll$ = new Subject();

    totalCount: number;
    renderingProviders: ClaimFilterOptionModel[] = [];
    isLoading: boolean;

    searchTimeout: any;

    constructor(private claimsService: ClaimService, private accountService: AccountMemberService) {
    }

    renderingProvidersSearchValueChanged(event: any) {
        this.searchRenderingProviders(event.target.value);
    }

    searchRenderingProviders(renderingProviderName: string) {
        if (this.searchTimeout)
            clearTimeout(this.searchTimeout)

        this.searchTimeout = setTimeout(() => {
            this.isLoading = true;

            this.claimsService.getClaimRenderingProviders({ Tab: this.tab, SearchValue: renderingProviderName, AccountInfoId: this.accountService.memberDetails.accountInfoId})
                .pipe(takeUntil(this.unsubscribeAll$))
                .subscribe(x => {
                    this.renderingProviders = this.selectedRenderingProviders;
                    this.renderingProviders = this.renderingProviders.concat(x.where((p: ClaimFilterOptionModel) =>
                        !this.selectedRenderingProviders.any((s: ClaimFilterOptionModel) => s.id == p.id)))

                    this.isLoading = false;
                });
        }, 1000);
    }

    onRenderingProviderClicked(renderingProvider: ClaimFilterOptionModel) {
        if (renderingProvider.checked) {
            this.selectedRenderingProviders.remove(renderingProvider);
            renderingProvider.checked = false;
        } else {
            this.selectedRenderingProviders.push(renderingProvider);
            renderingProvider.checked = true;
        }

        this.renderingProviderClicked.emit()
    }

    ngOnDestroy(): void {
        if (this.searchTimeout)
            clearTimeout(this.searchTimeout)

        //this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }

    ngOnInit(): void {
        this.renderingProviders = [...this.selectedRenderingProviders];
        this.searchRenderingProviders("");
    }
}