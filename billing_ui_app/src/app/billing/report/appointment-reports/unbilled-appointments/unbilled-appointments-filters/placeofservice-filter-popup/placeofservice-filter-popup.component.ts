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
  @Input() selectedPlaceOfService: ClaimFilterOptionModel[];
  @Input() userList: ClaimFilterOptionModel[] = [];
  placeOfServices: ClaimFilterOptionModel[] = [];
  isLoading: boolean;
  searchTimeout: any;
  isAllSelect: boolean = false;
  private unsubscribeAll$ = new Subject();

  constructor(private reportingService: ReportService) {}

  searchPOSs(posName: string) {
    if (this.searchTimeout)
      clearTimeout(this.searchTimeout)

    this.searchTimeout = setTimeout(() => {
      this.setUserList(posName);
    }, 300);
  }

  setUserList(posName: string) {
    let filteredList = [];
    this.placeOfServices = this.selectedPlaceOfService;
    if (posName != "") 
      filteredList = this.userList.filter(x => x.name != null && (x.name.toLowerCase().includes(posName.toLowerCase()) || x.checked));
    else {
      filteredList = this.userList;
    }
    this.placeOfServices = this.placeOfServices.concat(filteredList.filter((p: ClaimFilterOptionModel) =>
      !this.selectedPlaceOfService.some((s: ClaimFilterOptionModel) => s.id == p.id)));
    this.isAllSelect = this.placeOfServices.length > 0 && this.placeOfServices.every(f => f.checked);
  }

  onPOSClicked(pos: ClaimFilterOptionModel) {
    if (pos.checked) {
      this.selectedPlaceOfService.remove(pos);
      pos.checked = false;
    } else {
      this.selectedPlaceOfService.push(pos);
      pos.checked = true;
    }

    this.isAllSelect = this.placeOfServices.every(f => f.checked);

    this.posClicked.emit()
  }


  selectAll(checked: boolean): void {
    this.placeOfServices.forEach(pos => {
      if (pos.checked && !checked) {
        this.selectedPlaceOfService.remove(pos);
      } else if (!pos.checked && checked) {
        this.selectedPlaceOfService.push(pos);
      }
      pos.checked = checked
      this.posClicked.emit()
    });

    this.isAllSelect = this.placeOfServices.every(f => f.checked);
  }

  posSearchValueChanged(event: any) {
    this.searchPOSs(event.target.value);
  }

  ngOnInit(): void {
    this.placeOfServices = [...this.selectedPlaceOfService];
    this.setUserList("");
  }
}
