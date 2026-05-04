import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from "@angular/core";
import { ClaimService } from "@core/services/billing";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";
import { ClaimListingTab } from "@core/enums/billing/claim-listing-tab";
import { ClaimFilterOptionModel } from "@core/models/billing/claim-filter-option-model";
import { AccountMemberService } from "@core/services/account/account-member.service";


@Component({
  selector: 'app-claim-filters-flaggedreason-popup',
  templateUrl: './claim-filters-flaggedreason-popup.component.html',
  styleUrls: ['./claim-filters-flaggedreason-popup.component.css']
})
export class ClaimFiltersFlaggedreasonPopupComponent implements OnDestroy, OnInit {
  @Output() reasonClicked = new EventEmitter<number>();
  @Input() selectedFlaggedReason: ClaimFilterOptionModel[];
  @Input() tab: ClaimListingTab | null;
  private unsubscribeAll$ = new Subject<void>();

  totalCount: number;
  locations: any[] = [];
  isLoading: boolean;

  searchTimeout: any;

  constructor(private claimsService: ClaimService, private accountService: AccountMemberService) {
  }

  reasonSearchValueChanged(event: any) {
    this.searchLocations(event.target.value);
  }

  searchLocations(locationName: string) {
    if (this.searchTimeout)
      clearTimeout(this.searchTimeout)

    this.searchTimeout = setTimeout(() => {
      this.isLoading = true;
      this.claimsService.getClaimFlagReasons()
        .pipe(takeUntil(this.unsubscribeAll$))
        .subscribe(x => {
          this.locations = this.selectedFlaggedReason;
          this.locations = this.locations.concat(x.filter((p: any) =>
            !this.selectedFlaggedReason.some((s: any) => s.id == p.id)))

          this.isLoading = false;
        });
    }, 1000);
  }

  onReasonClicked(location: ClaimFilterOptionModel) {
    if (location.checked) {
      this.selectedFlaggedReason.remove(location);
      location.checked = false;
    } else {
      this.selectedFlaggedReason.push(location);
      location.checked = true;
    }
    this.reasonClicked.emit()
  }

  ngOnDestroy(): void {
    if (this.searchTimeout)
      clearTimeout(this.searchTimeout);

    this.unsubscribeAll$.next();
    this.unsubscribeAll$.complete();
  }

  ngOnInit(): void {
    this.locations = [...this.selectedFlaggedReason];
    this.searchLocations("");
  }
}
