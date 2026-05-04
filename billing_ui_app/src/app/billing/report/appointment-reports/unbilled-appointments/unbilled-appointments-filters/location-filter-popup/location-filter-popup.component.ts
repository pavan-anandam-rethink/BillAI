import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ReportService } from '@core/services/billing/report.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'location-filter-popup',
  templateUrl: './location-filter-popup.component.html',
  styleUrls: ['./location-filter-popup.component.css']
})
export class LocationFilterPopupComponent {
@Output() locationClicked = new EventEmitter<number>();
  @Input() selectedLocation: ClaimFilterOptionModel[];
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
      clearTimeout(this.searchTimeout)

    this.searchTimeout = setTimeout(() => {
      this.setLocationList(locationName);
    }, 300);
  }

  setUserList(locationName: string) {
    let filteredList = [];
      this.locations = this.selectedLocation; 
      if (locationName != "") 
        filteredList = this.userList.where(x => x.name != null && (x.name.toLowerCase().includes(locationName.toLowerCase()) || x.checked));
      else {
        filteredList = this.userList;
      }
      this.locations = this.locations.concat(filteredList.where((p: ClaimFilterOptionModel) =>
        !this.selectedLocation.any((s: ClaimFilterOptionModel) => s.id == p.id)))
      this.isAllSelect = this.locations.length > 0 && this.locations.every(f=>f.checked);
  }

  onLocationClicked(location: ClaimFilterOptionModel) {
    if (location.checked) {
      this.selectedLocation.remove(location);
      location.checked = false;
    } else {
      this.selectedLocation.push(location);
      location.checked = true;
    }

    this.isAllSelect = this.locations.every(f => f.checked);

    this.locationClicked.emit()
  }


  selectAll(checked: boolean): void {
    this.locations.forEach(location => {
      if (location.checked && !checked) {
        this.selectedLocation.remove(location);
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
    this.locations = [...this.selectedLocation];
    this.setLocationList("");
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
}
