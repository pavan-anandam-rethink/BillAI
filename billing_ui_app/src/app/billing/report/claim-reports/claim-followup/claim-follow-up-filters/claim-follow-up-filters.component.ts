import { DatePipe } from '@angular/common';
import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';

@Component({
  selector: 'app-claim-follow-up-filters',
  templateUrl: './claim-follow-up-filters.component.html',
  styleUrls: ['./claim-follow-up-filters.component.css']
})
export class ClaimFollowUpFiltersComponent {
  @Input() opened: boolean = false;
  @Input() userList: ClaimFilterOptionModel[];
  @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;
  @ViewChild("dataRangeType") public dropdownlist: any;
  @Output() filterChanged = new EventEmitter();
  @Output() clearFilter = new EventEmitter();

  @ViewChild('dateAnchorEl', { read: ElementRef }) public dateAnchor: ElementRef;
  @Output() requestData = new EventEmitter<{
    funder: number[];
    dateFrom: Date;
    dateTo: Date;
    rangeType: number;
  }>();


  dataRangeOption: string[] = ['Active', 'Completed'];
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
      const ninetyDaysFromToday = new Date();
      ninetyDaysFromToday.setDate(today.getDate() - 90);
      this.dateFrom = ninetyDaysFromToday;
      this.dateFromString = this.datePipe.transform(this.dateFrom, 'MM/dd/yy');
    }
    if (this.dateTo == undefined) {
      this.dateTo = new Date();
      this.dateTo.setDate(this.dateTo.getDate() + 7);
      this.dateToString = this.datePipe.transform(this.dateTo, 'MM/dd/yy');
    }
    if (this.dataRangeTypeInput == undefined) {
      this.dataRangeTypeInput = "Active";  
      this.dataRangeTypeNum = 0;                   
      this.isDefaultDateSelected = false;
    }
  }
  selectFilter(filterId: number | undefined, anchor: any = undefined) {

    if (filterId == this.selectedFilterId) {
      this.selectedFilterId = undefined;
    } else {
      this.selectedFilterId = filterId;
    }
    this.selectedAnchor = anchor;
    // if (filterId == 1)
    //   if (this.dateFrom == undefined || this.dateTo == undefined) {
    //     this.dateFrom = new Date();
    //     this.dateTo = new Date();
    //   }

    if (filterId === 1 && (!this.dateFrom || !this.dateTo)) {
      const today = new Date();
      const ninetyDaysFromToday = new Date();
      ninetyDaysFromToday.setDate(today.getDate() - 90);
      this.dateFrom = ninetyDaysFromToday
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
  }
  onDateFilterLeave() {
    this.showDatePopup = false;
  }

  rangeTypeChanged(value: string): void {

    switch (value) {
      case 'Active':
        this.dataRangeTypeNum = 0;
        break;
      case 'Completed':
        this.dataRangeTypeNum = 1;
        break;
      default:
        this.dataRangeTypeNum = 0;
        break;
    }
    this.isDefaultDateSelected = value == this.defaultItem;
    this.onFilterChange();
  }

  applyFilters() {
    if (this.dateFromString != undefined && this.dateToString != undefined) {
      const data = {
        funder: this.selectedFunder.select(x => x.id),
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
    this.isFiltersApplied = false;
  }
}
