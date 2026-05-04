import { Component, EventEmitter, Input, Output, OnInit, OnDestroy } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ReportService } from '@core/services/billing/report.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'authorization-filter-popup',
  templateUrl: './authorization-filter-popup.component.html',
  styleUrls: ['./authorization-filter-popup.component.css'],
})
export class AuthorizationFilterPopupComponent implements OnInit, OnDestroy {
  @Output() authorizationClicked = new EventEmitter<number>();
  @Input() selectedAuthNumber: ClaimFilterOptionModel[] = [];

  authorizationNumbers: ClaimFilterOptionModel[] = [];
  userList: ClaimFilterOptionModel[] = [];
  isLoading = false;
  searchTimeout: any;
  isAllSelect = false;
  private unsubscribeAll$ = new Subject<void>();

  // ✅ Static cache to prevent repeated API calls
  private static cachedAuthorizationNumbers: ClaimFilterOptionModel[] = [];

  constructor(private reportingService: ReportService) {}

  ngOnInit(): void {
    // ✅ Load from cache if available (mirror POS behavior)
    if (AuthorizationFilterPopupComponent.cachedAuthorizationNumbers.length > 0) {
      this.userList = AuthorizationFilterPopupComponent.cachedAuthorizationNumbers;
      this.setUserList('');
    } else {
      this.searchAuthorizationNumber('');
    }
  }

  ngOnDestroy(): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.unsubscribeAll$.next();
    this.unsubscribeAll$.complete();
  }

  AuthorizationSearchValueChanged(event: any) {
    this.searchAuthorizationNumber(event.target.value);
  }

  searchAuthorizationNumber(name: string) {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);

    // ✅ Use cached data if available
    if (AuthorizationFilterPopupComponent.cachedAuthorizationNumbers.length > 0) {
      this.userList = AuthorizationFilterPopupComponent.cachedAuthorizationNumbers;
      this.setUserList(name);
      return;
    }

    // ✅ Call API only if cache is empty
    this.searchTimeout = setTimeout(() => {
      this.isLoading = true;
      this.reportingService
        .getAllAuthorizationNumbers()
        .pipe(takeUntil(this.unsubscribeAll$))
        .subscribe((x: ClaimFilterOptionModel[]) => {
          AuthorizationFilterPopupComponent.cachedAuthorizationNumbers = x; // cache
          this.userList = x;
          this.setUserList(name);
          this.isLoading = false;
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

    // ✅ Only mark items as checked if they are in selectedAuthNumber
    const selectedIds = this.selectedAuthNumber.map(s => s.id);
    this.authorizationNumbers = filteredList.map(p => ({
      ...p,
      checked: selectedIds.includes(p.id)
    }));

    // ✅ Sort checked items to top
    this.authorizationNumbers.sort((a, b) => Number(b.checked) - Number(a.checked));

    // ✅ Do NOT auto-select all; reflect only what user has selected
    this.isAllSelect = this.authorizationNumbers.length > 0 &&
      this.authorizationNumbers.every(p => p.checked);
  }

  onAuthorizationClicked(auth: ClaimFilterOptionModel) {
    const index = this.selectedAuthNumber.findIndex(x => x.id === auth.id);
    if (index > -1) {
      this.selectedAuthNumber.splice(index, 1);
      auth.checked = false;
    } else {
      this.selectedAuthNumber.push({ ...auth, checked: true });
      auth.checked = true;
    }

    const selectedIds = this.selectedAuthNumber.map(s => s.id);
    this.isAllSelect = this.authorizationNumbers.length > 0 &&
      this.authorizationNumbers.every(p => selectedIds.includes(p.id));

    this.authorizationClicked.emit();
  }

  selectAll(auths: ClaimFilterOptionModel[]): void {
    if (!this.isAllSelect) {
      // Not all selected → select all visible items
      auths.forEach(auth => {
        const index = this.selectedAuthNumber.findIndex(x => x.id === auth.id);
        if (index === -1) {
          this.selectedAuthNumber.push({ ...auth, checked: true });
        }
        auth.checked = true;
      });
      this.isAllSelect = true;
    } else {
      // All selected → unselect all visible items
      auths.forEach(auth => {
        const index = this.selectedAuthNumber.findIndex(x => x.id === auth.id);
        if (index > -1) {
          this.selectedAuthNumber.splice(index, 1);
        }
        auth.checked = false;
      });
      this.isAllSelect = false;
    }

    this.authorizationClicked.emit();
  }
}
