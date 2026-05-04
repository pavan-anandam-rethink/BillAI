import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from "@angular/core";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";
import { ClaimService } from "@core/services/billing";
import { ClaimFilterOptionModel } from "@core/models/billing/claim-filter-option-model";

@Component({
  selector: 'claim-filters-rendering-provider-popup',
  templateUrl: './claim-filters-rendering-provider-popup.component.html',
  styleUrls: ['./claim-filters-rendering-provider-popup.component.css'],
})
export class ClaimFiltersRenderingProviderPopupComponent implements OnDestroy, OnInit {
  @Output() renderingProviderClicked = new EventEmitter<number>();
  @Input() selectedRenderingProviders: ClaimFilterOptionModel[] = [];

  renderingProviders: ClaimFilterOptionModel[] = [];
  userList: ClaimFilterOptionModel[] = [];
  isLoading = false;
  isAllSelect = false;
  searchTimeout: any;
  private unsubscribeAll$ = new Subject<void>();

  // ✅ Static cache to prevent repeated API calls
  private static cachedRenderingProviders: ClaimFilterOptionModel[] = [];

  constructor(private claimsService: ClaimService) {}

  ngOnInit(): void {
    // ✅ Load from cache or fetch API
    if (ClaimFiltersRenderingProviderPopupComponent.cachedRenderingProviders.length > 0) {
      this.userList = ClaimFiltersRenderingProviderPopupComponent.cachedRenderingProviders;
      this.setUserList('');
    } else {
      this.searchRenderingProviders('');
    }
  }

  ngOnDestroy(): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.unsubscribeAll$.next();
    this.unsubscribeAll$.complete();
  }

  renderingProvidersSearchValueChanged(event: any) {
    this.searchRenderingProviders(event.target.value);
  }

  searchRenderingProviders(name: string) {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);

    // Match Authorization: if cache exists, use it immediately
    if (ClaimFiltersRenderingProviderPopupComponent.cachedRenderingProviders.length > 0) {
      this.userList = ClaimFiltersRenderingProviderPopupComponent.cachedRenderingProviders;
      this.setUserList(name);
      return;
    }

    // Otherwise, debounce then fetch once and cache
    this.searchTimeout = setTimeout(() => {
      this.claimsService.getRenderingProviders()
        .pipe(takeUntil(this.unsubscribeAll$))
        .subscribe((x: ClaimFilterOptionModel[]) => {
          ClaimFiltersRenderingProviderPopupComponent.cachedRenderingProviders = x; // cache
          this.userList = x;
          this.setUserList(name);
        });
    }, 300);
  }

  setUserList(name: string) {
    let filteredList = this.userList;
    if (name && name.trim() !== '') {
      filteredList = this.userList.filter(p =>
        p.name && p.name.toLowerCase().includes(name.toLowerCase())
      );
    }

    const selectedIds = this.selectedRenderingProviders.map(s => s.id);
    this.renderingProviders = filteredList.map(p => ({
      ...p,
      checked: selectedIds.includes(p.id)
    }));

    // ✅ Sort checked items to top
    this.renderingProviders.sort((a, b) => Number(b.checked) - Number(a.checked));

    this.isAllSelect = this.renderingProviders.length > 0 &&
      this.renderingProviders.every(p => p.checked);
  }

  onRenderingProviderClicked(provider: ClaimFilterOptionModel) {
    const index = this.selectedRenderingProviders.findIndex(x => x.id === provider.id);
    if (index > -1) {
      this.selectedRenderingProviders.splice(index, 1);
      provider.checked = false;
    } else {
      this.selectedRenderingProviders.push({ ...provider, checked: true });
      provider.checked = true;
    }

    const selectedIds = this.selectedRenderingProviders.map(s => s.id);
    this.isAllSelect = this.renderingProviders.length > 0 &&
      this.renderingProviders.every(p => selectedIds.includes(p.id));

    this.renderingProviderClicked.emit();
  }

  selectAll(providers: ClaimFilterOptionModel[]): void {
    if (!this.isAllSelect) {
      // Not all selected → select all visible items
      providers.forEach(provider => {
        const index = this.selectedRenderingProviders.findIndex(x => x.id === provider.id);
        if (index === -1) {
          this.selectedRenderingProviders.push({ ...provider, checked: true });
        }
        provider.checked = true;
      });
      this.isAllSelect = true;
    } else {
      // All selected → unselect all visible items
      providers.forEach(provider => {
        const index = this.selectedRenderingProviders.findIndex(x => x.id === provider.id);
        if (index > -1) {
          this.selectedRenderingProviders.splice(index, 1);
        }
        provider.checked = false;
      });
      this.isAllSelect = false;
    }

    this.renderingProviderClicked.emit();
  }

}
