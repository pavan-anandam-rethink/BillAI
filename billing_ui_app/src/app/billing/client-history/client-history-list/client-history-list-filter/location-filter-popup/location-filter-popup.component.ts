import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ReportService } from '@core/services/billing/report.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'location-filter-popup',
  templateUrl: './location-filter-popup.component.html',
  styleUrls: ['./location-filter-popup.component.css'],
})
export class LocationFilterPopupComponent {
  @Output() locationClicked = new EventEmitter<number>();
  @Input() selectedLocation: ClaimFilterOptionModel[];
  // @Output() locationChanged = new EventEmitter<number>();
  @Input() locationList: ClaimFilterOptionModel[] = [];

  locations: ClaimFilterOptionModel[] = [];
  userList: ClaimFilterOptionModel[] = [];
  isLoading: boolean;
  searchTimeout: any;
  isAllSelect: boolean = false;
  private unsubscribeAll$ = new Subject();

  constructor(private reportingService: ReportService) {}

  searchLocations(locationName: string) {
    if (this.searchTimeout)
      clearTimeout(this.searchTimeout);

    // Only load from API if userList is empty and not already loading
    if (this.userList.length === 0 && !this.isLoading) {
      this.isLoading = true;
      this.reportingService
        .getLocationListByIds()
        .pipe(takeUntil(this.unsubscribeAll$))
        .subscribe((x) => {
          this.userList = x;
          this.locations = this.userList.map((p) => {
            const isSelected = this.selectedLocation.some(
              (sel) => sel.id === p.id
            );
            return { ...p, checked: isSelected };
          });
          this.locations.sort(
            (a, b) => Number(b.checked) - Number(a.checked)
          );
          this.isLoading = false;
          this.openPopup();
          // After first load, do search if locationName is not empty
          if (locationName && locationName.trim()) {
            this.setUserList(locationName);
          }
        });
    } else {
      // Use cached userList for search
      this.setUserList(locationName);
    }
  }

  setUserList(locationName: string) {
    let filteredList: ClaimFilterOptionModel[] = [];
    if (locationName && locationName.trim()) {
      filteredList = this.userList.filter(x => x.name != null && x.name.toLowerCase().includes(locationName.toLowerCase()));
    } else {
      filteredList = [...this.userList];
    }
    // Mark checked status
    filteredList = filteredList.map(p => ({
      ...p,
      checked: this.selectedLocation.some(sel => sel.id === p.id)
    }));
    // Sort selected locations to the top
    filteredList.sort((a, b) => Number(b.checked) - Number(a.checked));
    this.locations = filteredList;
    this.isAllSelect = this.locations.length > 0 && this.locations.every(f => f.checked);
  }


   onLocationClicked(location: ClaimFilterOptionModel) {
    const index = this.selectedLocation.findIndex(x => x.id === location.id);
    
    if (index > -1) {
      this.selectedLocation.splice(index, 1);
      location.checked = false;
    }  else {
      this.selectedLocation.push(location);
      location.checked = true;
    }

    this.isAllSelect = this.locations.every(f => f.checked);

    this.locationClicked.emit()
  }

  selectAll(checked: boolean): void {
    this.locations.forEach(location => {
      if (location.checked && !checked) {
        const index = this.selectedLocation.findIndex(x => x.id === location.id);
        if (index > -1) {
          this.selectedLocation.splice(index, 1);
        }
      } else if (!location.checked && checked) {
        this.selectedLocation.push(location);
      }
      location.checked = checked
      this.locationClicked.emit()
    });

    this.isAllSelect = this.locations.every(f => f.checked);
  }

  locationsSearchValueChanged(event: any) {
    this.searchLocations(event.target.value);
  }

  ngOnInit(): void {
    // Only load locations once on init
    if (this.userList.length === 0) {
      this.isLoading = true;
      this.reportingService
        .getLocationListByIds()
        .pipe(takeUntil(this.unsubscribeAll$))
        .subscribe((x) => {
          this.userList = x;
          this.isLoading = false;
          this.setUserList('');
        });
    } else {
      this.setUserList('');
    }
  }

  setLocationList(locationName: string) {
    let filteredList = [];
    this.locations = this.selectedLocation;
    if (locationName != "") 
      filteredList = this.locationList.filter(x => x.name != null && (x.name.toLowerCase().includes(locationName.toLowerCase()) || x.checked));
    else {
      filteredList = this.locationList;
    }
    this.locations = this.locations.concat(filteredList.filter((p: ClaimFilterOptionModel) =>
      !this.selectedLocation.some((s: ClaimFilterOptionModel) => s.id == p.id)));
    this.isAllSelect = this.locations.length > 0 && this.locations.every(f => f.checked);
  }

  openPopup(): void {
    this.userList.forEach(location => {
      location.checked = this.selectedLocation.some(sel => sel.id === location.id);
    });
    this.isAllSelect = this.userList.length > 0 && this.userList.every(loc => loc.checked);
  }
}
