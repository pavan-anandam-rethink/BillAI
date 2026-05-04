import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ReportService } from '@core/services/billing/report.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'placeofservice-filter-popup',
  templateUrl: './placeofservice-filter-popup.component.html',
  styleUrls: ['./placeofservice-filter-popup.component.css']
})
export class PlaceofserviceFilterPopupComponent {
  @Output() posClicked = new EventEmitter<number>();
  @Input() selectedPlaceOfService: ClaimFilterOptionModel[] = [];

  placeOfServices: ClaimFilterOptionModel[] = [];
  userList: ClaimFilterOptionModel[] = [];
  isLoading = false;
  searchTimeout: any;
  isAllSelect = false;
  private unsubscribeAll$ = new Subject<void>();

  // ✅ Static cache for reuse
  private static cachedPOSList: ClaimFilterOptionModel[] = [];

  constructor(private reportingService: ReportService) {}

  searchPOSs(posName: string): void {
    if (this.searchTimeout) clearTimeout(this.searchTimeout);

    // ✅ Use cached data if available
    if (PlaceofserviceFilterPopupComponent.cachedPOSList.length > 0) {
      this.userList = PlaceofserviceFilterPopupComponent.cachedPOSList;
      this.setUserList(posName);
      return;
    }

    // ✅ Only call API if no cache
    this.searchTimeout = setTimeout(() => {
      this.isLoading = true;
      this.reportingService.getPoSListByIds()
        .pipe(takeUntil(this.unsubscribeAll$))
        .subscribe(x => {
          PlaceofserviceFilterPopupComponent.cachedPOSList = x; // Cache once
          this.userList = x;
          this.setUserList(posName);
          this.isLoading = false;
        });
    }, 300);
  }

  setUserList(posName: string): void {
    let filteredList = this.userList;

    if (posName && posName.trim() !== '') {
      filteredList = this.userList.filter(x =>
        x.name &&
        x.name.toLowerCase().includes(posName.toLowerCase())
      );
    }

    const selectedIds = this.selectedPlaceOfService.map(s => s.id);
    this.placeOfServices = filteredList.map(p => ({
      ...p,
      checked: selectedIds.includes(p.id)
    }));

    // Sort checked items to the top
    this.placeOfServices.sort((a, b) => Number(b.checked) - Number(a.checked));

    this.isAllSelect = this.placeOfServices.length > 0 &&
      this.placeOfServices.every(f => f.checked);
  }

  onPOSClicked(pos: ClaimFilterOptionModel): void {
    const index = this.selectedPlaceOfService.findIndex(x => x.id === pos.id);
    if (index > -1) {
      this.selectedPlaceOfService.splice(index, 1);
      pos.checked = false;
    } else {
      this.selectedPlaceOfService.push({ ...pos, checked: true });
      pos.checked = true;
    }

    const selectedIds = this.selectedPlaceOfService.map(s => s.id);
    this.isAllSelect = this.placeOfServices.length > 0 &&
      this.placeOfServices.every(f => selectedIds.includes(f.id));

    this.posClicked.emit();
  }

  selectAll(poss: ClaimFilterOptionModel[]): void {
    const newCheckedState = !this.isAllSelect;
    
    if (newCheckedState) {
      poss.forEach(pos => {
        const index = this.selectedPlaceOfService.findIndex(x => x.id === pos.id);
        if (index === -1) {
          this.selectedPlaceOfService.push({ ...pos, checked: true });
        }
        pos.checked = true;
      });
    } else {
    poss.forEach(pos => {
      const index = this.selectedPlaceOfService.findIndex(
        x => x.id === pos.id
      );

      if (index > -1) {
        this.selectedPlaceOfService.splice(index, 1);
        }
        pos.checked = false;
      });
    }

    this.isAllSelect = newCheckedState;
    this.posClicked.emit();
  }

  posSearchValueChanged(event: any): void {
    this.searchPOSs(event.target.value);
  }

  ngOnInit(): void {
    // ✅ Initialize with cache or first API call
    if (PlaceofserviceFilterPopupComponent.cachedPOSList.length > 0) {
      this.userList = PlaceofserviceFilterPopupComponent.cachedPOSList;
      this.setUserList('');
    } else {
      this.searchPOSs('');
    }
  }

  ngOnDestroy(): void {
    this.unsubscribeAll$.next();
    this.unsubscribeAll$.complete();
  }
}
