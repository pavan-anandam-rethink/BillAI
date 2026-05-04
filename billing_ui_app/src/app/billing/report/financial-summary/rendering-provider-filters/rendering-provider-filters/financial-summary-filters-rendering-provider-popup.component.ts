import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from "rxjs/operators";
import { ClaimService } from "@core/services/billing";
import { ClaimFilterOptionModel } from "@core/models/billing/claim-filter-option-model";
import { ClaimListingTab } from "@core/enums/billing/claim-listing-tab";
import { AccountMemberService } from "@core/services/account/account-member.service";
@Component({
  selector: 'financial-summary-filters-rendering-provider-popup',
  templateUrl: './financial-summary-filters-rendering-provider-popup.component.html',
  styleUrls: ['./financial-summary-filters-rendering-provider-popup.component.css']
})
export class FinancialSummaryFiltersRenderingProviderPopupComponent implements OnDestroy, OnInit{
  @Output() renderingProviderClicked = new EventEmitter<number>();
  @Input() selectedRenderingProviders: ClaimFilterOptionModel[];
  @Input() tab: ClaimListingTab | 1;
  private unsubscribeAll$ = new Subject();

  totalCount: number;
  renderingProviders: ClaimFilterOptionModel[] = [];
  isLoading: boolean;
  isAllSelect: boolean = false;

  searchTimeout: any;

  constructor(private claimsService: ClaimService, private accountService: AccountMemberService) {
  }

  renderingProvidersSearchValueChanged(event: any) {
      this.searchRenderingProvidersValueChange(event.target.value);
  }

  searchRenderingProvidersValueChange(renderingProviderName: string) {
    if (this.searchTimeout)
        clearTimeout(this.searchTimeout)

    this.searchTimeout = setTimeout(() => {
        this.isLoading = true;

        const searchText = (renderingProviderName || '').toLowerCase().trim();

        if (!searchText) {
          this.renderingProviders = [...this.selectedRenderingProviders];
        } else {
          this.renderingProviders = this.selectedRenderingProviders.filter(p =>
            p.name?.toLowerCase().includes(searchText)
          );
        }
        
        this.isAllSelect = this.selectedRenderingProviders.length > 0 &&
          this.selectedRenderingProviders.every(p => p.checked);
        
                this.isLoading = false;
           
    }, 10);
}


  searchRenderingProviders(renderingProviderName: string) {
      if (this.searchTimeout)
          clearTimeout(this.searchTimeout)

      this.searchTimeout = setTimeout(() => {
          this.isLoading = true;

          this.claimsService.getClaimRenderingProviders({ Tab: 1, SearchValue: renderingProviderName, AccountInfoId: this.accountService.memberDetails.accountInfoId})
              .pipe(takeUntil(this.unsubscribeAll$))
              .subscribe(x => {
                const newItems = x.where((p: ClaimFilterOptionModel) =>
                  !this.selectedRenderingProviders.any((s: ClaimFilterOptionModel) => s.id == p.id))
                  .map((p: ClaimFilterOptionModel) => ({ ...p, checked: this.isAllSelect }));

                this.selectedRenderingProviders.push(...newItems);
                this.renderingProviders = [...this.selectedRenderingProviders];

                  this.isAllSelect = this.selectedRenderingProviders.length > 0 &&
                    this.selectedRenderingProviders.every(p => p.checked);

                  this.isLoading = false;
              });
      }, 1000);
  }

  onRenderingProviderClicked(renderingProvider: ClaimFilterOptionModel) {
      if (!renderingProvider.checked) {
          renderingProvider.checked = true;
      } else {
          renderingProvider.checked = false;
      }

      this.isAllSelect = this.selectedRenderingProviders.length > 0 &&
        this.selectedRenderingProviders.every(p => p.checked);

      this.renderingProviderClicked.emit()
  }

  selectAll(providers: ClaimFilterOptionModel[]): void {
    const newCheckedState = !this.isAllSelect;   
    this.selectedRenderingProviders.forEach(provider => {
      provider.checked = newCheckedState;
    });
    
    this.isAllSelect = newCheckedState;
    this.renderingProviderClicked.emit();
  }

  ngOnDestroy(): void {
      if (this.searchTimeout)
          clearTimeout(this.searchTimeout)

      //this.unsubscribeAll$.next();
      this.unsubscribeAll$.complete();
  }

  ngOnInit(): void {
      this.renderingProviders = [...this.selectedRenderingProviders];
      this.isAllSelect = this.selectedRenderingProviders.length > 0 &&
        this.selectedRenderingProviders.every(p => p.checked);
      this.searchRenderingProviders("");
  }
}
