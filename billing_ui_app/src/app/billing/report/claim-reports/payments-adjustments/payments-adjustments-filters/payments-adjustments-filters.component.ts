import { DatePipe } from '@angular/common';
import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';

@Component({
  selector: 'payments-adjustments-filters',
  templateUrl: './payments-adjustments-filters.component.html',
  styleUrls: ['./payments-adjustments-filters.component.css']
})
export class PaymentsAdjustmentsFiltersComponent {

  @Input() opened: boolean = false;
  @Input() userList: ClaimFilterOptionModel[];
  @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;
  @ViewChild("dataRangeType") public dropdownlist: any;
  @Output() filterChanged = new EventEmitter();
  @Output() clearFilter = new EventEmitter();

  @ViewChild('dateAnchorEl', { read: ElementRef }) public dateAnchor: ElementRef;
  @Output() requestData = new EventEmitter<{
    funder: number[] | null;
    dateFrom: Date;
    dateTo: Date;
    rangeType: number;
  }>();


  dataRangeOption: string[] = ['Transaction Date', 'Deposit Date'];
  isFiltersApplied: boolean = false;
    private funderInitialized = false;

  constructor(private datePipe: DatePipe) {
  }
  isDefaultDateSelected = true;
  public defaultItem = "Data Range Type";


  excludeDefault(): void {
    this.dropdownlist.defaultItem = null;
  }
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
  dateFrom: Date | undefined;
  dateTo: Date | undefined;
  dateFromString: string | undefined;
  dateToString: string | undefined;
  dataRangeTypeInput: string | undefined;
  dataRangeTypeNum: number | undefined;


  private contains(target: HTMLElement): boolean {
    return this.selectedAnchor.contains(target) ||
      (this.popup ? this.popup.nativeElement.contains(target) : false);
  }

ngOnChanges() {
  this.initializeFilters();
}
private initializeFilters() {
  if (!this.funderInitialized && this.userList?.length > 0) {
    this.userList.forEach(u => u.checked = true);
    this.selectedFunder = [...this.userList];
    this.funderInitialized = true;
  }

  if (this.dateFrom == undefined) {
    const today = new Date();
    this.dateFrom = new Date(today.getFullYear(), today.getMonth(), 1); 
    this.dateFromString = this.datePipe.transform(this.dateFrom, 'MM/dd/yy');
  }
  if (this.dateTo == undefined) {
    this.dateTo = new Date();
    this.dateToString = this.datePipe.transform(this.dateTo, 'MM/dd/yy');
  }
  if (this.dataRangeTypeInput == undefined) {
     this.dataRangeTypeInput = "Transaction Date";  // "Transaction Date"
     this.dataRangeTypeNum = 1;                   // 1 = Transaction Date
     this.isDefaultDateSelected = false;   
  }
}

  selectFilter(filterId: number | undefined, anchor: any = undefined) {
   
    if (filterId === this.selectedFilterId) {
    this.selectedFilterId = undefined;        
  } else {
    this.selectedFilterId = filterId;
  }
    this.selectedAnchor = anchor;
    if (filterId === 1 && (!this.dateFrom || !this.dateTo)) {
    const today = new Date();
    this.dateFrom = new Date(today.getFullYear(), today.getMonth(), 1);
    this.dateTo = today;
    this.dateFromString = this.datePipe.transform(this.dateFrom, 'MM/dd/yy');
    this.dateToString = this.datePipe.transform(this.dateTo, 'MM/dd/yy');   
  }

    this.showDatePopup = true;
  }

  clearFilters() {
    this.selectedFunder = [];
    this.dateFrom = undefined;
    this.dateTo = undefined;
    this.dateFromString = undefined;
    this.dateToString = undefined;
    this.dataRangeTypeInput = undefined;
    this.dataRangeTypeNum = undefined;
    this.userList.forEach(x => x.checked = false);
    this.filterChanged.emit();
    this.clearFilter.emit();
    this.isFiltersApplied = false;
  }
  OnInitclearFilters() {
    this.selectedFunder = [];
    this.dateFrom = undefined;
    this.dateTo = undefined;
    this.dateFromString = undefined;
    this.dateToString = undefined;
    this.dataRangeTypeInput = undefined;
    this.dataRangeTypeNum = undefined;
    this.userList.forEach(x => x.checked = false);

  }

  setDatePeriod(dateRange: any): void {
    this.dateFromString = this.datePipe.transform(dateRange.start, 'MM/dd/yy');
    this.dateToString = this.datePipe.transform(dateRange.end, 'MM/dd/yy');
    this.dateFrom = dateRange.start;
    this.dateFrom.setMinutes(0);
    this.dateFrom.setSeconds(0);
    this.dateFrom.setMilliseconds(0);
    this.dateTo = dateRange.end;
    this.dateTo.setMinutes(0);
    this.dateTo.setSeconds(0);
    this.dateTo.setMilliseconds(0);
    this.onFilterChange();
    this.showDatePopup = false;
     this.isFiltersApplied = false;
  }
  onDateFilterLeave() {
    this.showDatePopup = false;
  }

  rangeTypeChanged(value: string): void {

    switch (value) {
      case 'Transaction Date':
        this.dataRangeTypeNum = 1;
        break;
      case 'Deposit Date':
        this.dataRangeTypeNum = 2;
        break;
      default:
        this.dataRangeTypeNum = 1;
        break;
    }
    this.isDefaultDateSelected = value == this.defaultItem;
    this.onFilterChange();
  }

  applyFilters() {
    
    if (this.dateFromString != undefined && this.dateToString != undefined) {
      const isAllSelected = this.userList?.length > 0 && this.selectedFunder.length === this.userList.length;
      const data = {
        funder: isAllSelected ? [] : this.selectedFunder.select(x => x.id),
        dateFrom: this.dateFrom,
        dateTo: this.dateTo,
        rangeType: this.dataRangeTypeNum
      }
      this.requestData.emit(data);
    }
    else {
      this.requestData.emit(undefined);
    }
    this.isFiltersApplied = true;
  }

  onFilterChange() {
    this.filterChanged.emit();
     this.isFiltersApplied= false;
  }
}
