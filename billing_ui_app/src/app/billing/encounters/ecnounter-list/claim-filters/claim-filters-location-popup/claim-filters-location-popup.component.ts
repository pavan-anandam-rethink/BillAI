import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from "@angular/core";
import { ClaimService } from "@core/services/billing";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";
import { ClaimListingTab } from "@core/enums/billing/claim-listing-tab";
import { ClaimFilterOptionModel } from "@core/models/billing/claim-filter-option-model";
import { AccountMemberService } from "@core/services/account/account-member.service";


@Component({
    selector: 'claim-filters-location-popup',
    templateUrl: './claim-filters-location-popup.component.html',
    styleUrls: ['./claim-filters-location-popup.component.css'],
})

export class ClaimFiltersLocationPopupComponent implements OnDestroy, OnInit {
    @Output() locationClicked = new EventEmitter<number>();
    @Input() selectedLocations: ClaimFilterOptionModel[];
    @Input() tab: ClaimListingTab | null;
    private unsubscribeAll$ = new Subject<void>();

    totalCount: number;
    locations: ClaimFilterOptionModel[] = [];
    isLoading: boolean;

    searchTimeout: any;

    constructor(private claimsService: ClaimService, private accountService: AccountMemberService) {
    }

    locationsSearchValueChanged(event: any) {
        this.searchLocations(event.target.value);
    }

    searchLocations(locationName: string) {
        if (this.searchTimeout)
            clearTimeout(this.searchTimeout)

        this.searchTimeout = setTimeout(() => {
            this.isLoading = true;

            this.claimsService.getClaimLocations({ Tab: this.tab, SearchValue: locationName, AccountInfoId: this.accountService.memberDetails.accountInfoId })
                .pipe(takeUntil(this.unsubscribeAll$))
                .subscribe(x => {
                    this.locations = this.selectedLocations;
                     this.locations = this.locations.concat(x.where((p: ClaimFilterOptionModel) =>
                        !this.selectedLocations.any((s: ClaimFilterOptionModel) => s.id == p.id)))

                    this.isLoading = false;
                });
        }, 1000);
    }

    onLocationClicked(location: ClaimFilterOptionModel) {
        if (location.checked) {
            this.selectedLocations.remove(location);
            location.checked = false;
        } else {
            this.selectedLocations.push(location);
            location.checked = true;
        }

        this.locationClicked.emit()
    }

    ngOnDestroy(): void {
        if (this.searchTimeout)
            clearTimeout(this.searchTimeout);

        this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }

    ngOnInit(): void {
        this.locations = [...this.selectedLocations];
        this.searchLocations("");
    }
}