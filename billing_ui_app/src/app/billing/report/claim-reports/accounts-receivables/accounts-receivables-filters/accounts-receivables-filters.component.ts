import { DatePipe } from "@angular/common";
import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild } from "@angular/core";
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';

@Component({
  selector: 'accounts-receivables-filters',
  templateUrl: './accounts-receivables-filters.component.html',
  styleUrls: ['./accounts-receivables-filters.component.css']
})
export class AccountsReceivablesFiltersComponent {
  @Input() opened: boolean = false;
  @Input() userList: ClaimFilterOptionModel[];
  @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;
  @Output() filterChanged = new EventEmitter();
  @Output() clearFilter = new EventEmitter();

  @ViewChild('dateAnchorEl', { read: ElementRef }) public dateAnchor: ElementRef;
  @Output() requestData = new EventEmitter<{
    funder: number[];
    date: Date;
  }>();

  isFiltersApplied: boolean = false;
  private funderInitialized = false;
  constructor(private datePipe: DatePipe) {}

  @HostListener('document:click', ['$event'])
  public documentClick(event: any): void {
    if (!this.selectedAnchor)
      return;

    if (!this.contains(event.target)) {
      if (this.selectedFilterId != undefined)
        this.selectFilter(undefined);
    }
  }

  selectedFilterId: number | undefined = undefined;
  selectedAnchor: any;
  showDatePopup = false;
  selectedFunder: ClaimFilterOptionModel[] = [];
  filterdate: Date | undefined;
  filterdateString: string | undefined;

  private contains(target: HTMLElement): boolean {
    return this.selectedAnchor.contains(target) ||
      (this.popup ? this.popup.nativeElement.contains(target) : false);
  }

private initializeFilters() {
  if (!this.funderInitialized && this.userList?.length > 0) {
    this.userList.forEach(u => u.checked = true);
    this.selectedFunder = [...this.userList];
    this.funderInitialized = true;
  }

  if (this.filterdate == undefined) {
    this.filterdate = new Date();
    this.filterdateString = this.datePipe.transform(this.filterdate, 'MM/dd/yy');
  }
}

ngOnChanges() {
  this.initializeFilters();
}
  selectFilter(filterId: number | undefined, anchor: any = undefined) {
  this.selectedFilterId = filterId;
  this.selectedAnchor = anchor;

  if (filterId == 1) {
    this.showDatePopup = true;
  }
  if (filterId == 0) {
    this.showDatePopup = false;
  }
}

  clearFilters() {
    this.selectedFunder = [];
    this.filterdate = undefined;
    this.filterdateString = undefined;
    this.userList.forEach(x => x.checked = false);
    this.filterChanged.emit();
    this.clearFilter.emit();
  }

  OnInitClearFilters() {
    this.selectedFunder = [];
    this.filterdate = undefined;
    this.filterdateString = undefined;
  }

  onFilterChange() {
     this.filterChanged.emit();
     this.isFiltersApplied= false;
  }

  applyFilters() {
    if (this.filterdateString != undefined) {
      const data = {
        funder: this.selectedFunder.select(x => x.id),
        date: this.filterdate
      }
      this.requestData.emit(data);
    }
    else {
      this.requestData.emit(undefined);
    }
    this.isFiltersApplied = true;
  }

  setDatePeriod(date: any): void {
    this.filterdateString = this.datePipe.transform(date, 'MM/dd/yy');
    this.filterdate = date;
    this.filterdate.setMinutes(0);
    this.filterdate.setSeconds(0);
    this.filterdate.setMilliseconds(0);
    this.filterChanged.emit();
    this.showDatePopup = false;
    this.isFiltersApplied = false;
  }

  onDateFilterLeave() {
    this.showDatePopup = false;
  }
}
