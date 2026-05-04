import { DatePipe } from '@angular/common';
import { Component, ElementRef, EventEmitter, HostListener, Input, OnInit, Output, SimpleChanges, ViewChild } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ReportService } from '@core/services/billing/report.service';
import { LoaderService } from '@core/services/common';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-unprocessed-appointments-filters',
  templateUrl: './unprocessed-appointments-filters.component.html',
  styleUrls: ['./unprocessed-appointments-filters.component.css']
})
export class UnprocessedAppointmentsFiltersComponent implements OnInit {
  userList: ClaimFilterOptionModel[] = [];
  funderList: ClaimFilterOptionModel[] = [];
  staffList: ClaimFilterOptionModel[] = [];
  locationList: ClaimFilterOptionModel[] = [];
  placeOfServiceList: ClaimFilterOptionModel[] = [];
  @Input() opened: boolean = true;
  @Input() appInfo: any[] = [];
  @Output() filterChanged = new EventEmitter();
  @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;

  selectedFilterId: number | undefined = undefined;
  selectedPatients: ClaimFilterOptionModel[] = [];
  selectedfunders: ClaimFilterOptionModel[] = [];
  selectedStaff: ClaimFilterOptionModel[] = [];
  selectedLocation: ClaimFilterOptionModel[] = [];
  selectedPlaceOfService: ClaimFilterOptionModel[] = [];
  selectedAnchor: any;

  dateFrom: Date | undefined;
  dateTo: Date | undefined;
  dateFromString: string | undefined;
  dateToString: string | undefined;
  dataRangeTypeInput: string | undefined;
  dataRangeTypeNum: number | undefined;
  isFiltersApplied: boolean = true;
  @ViewChild('dateAnchorEl', { read: ElementRef }) public dateAnchor: ElementRef;
  showDatePopup = false;
  isClearFilterDisabled = true;
  isApplyFilterDisabled = true;
  showClearFilter = false;

  @HostListener('document:click', ['$event'])
  public documentClick(event: any): void {
    if (!this.selectedAnchor)
      return;

    if (!this.contains(event.target)) {
      if (this.selectedFilterId != undefined)
        this.selectFilter(undefined);
    }
  }

  private contains(target: HTMLElement): boolean {
    return this.selectedAnchor.contains(target) ||
      (this.popup ? this.popup.nativeElement.contains(target) : false);
  }

  constructor(
    private datePipe: DatePipe,
    private reportingService: ReportService,
    private loaderService: LoaderService
  ) { }

  ngOnInit(): void {
    // Set Date of Service defaults
    const today = new Date();
    const fromDate = new Date();
    fromDate.setDate(today.getDate() - 30);

    this.dateFrom = fromDate;
    this.dateTo = today;
    this.dateFromString = this.datePipe.transform(this.dateFrom, 'MM/dd/yy');
    this.dateToString = this.datePipe.transform(this.dateTo, 'MM/dd/yy');

    this.loaderService.show(); // Show global spinner
    // Fetch all filter data in parallel
    forkJoin({
      clients: this.reportingService.getClientListByIds(),
      funders: this.reportingService.getFunderListByIds(),
      staff: this.reportingService.getStaffListByIds(),
      locations: this.reportingService.getLocationListByIds(),
      pos: this.reportingService.getPoSListByIds()
    }).subscribe(results => {
      this.userList = results.clients.map(patient => ({ ...patient, checked: true }));
      this.selectedPatients = [...this.userList];

      this.funderList = results.funders.map(funder => ({ ...funder, checked: true }));
      this.selectedfunders = [...this.funderList];

      this.staffList = results.staff.map(staff => ({ ...staff, checked: true }));
      this.selectedStaff = [...this.staffList];

      this.locationList = results.locations.map(location => ({ ...location, checked: true }));
      this.selectedLocation = [...this.locationList];

      this.placeOfServiceList = results.pos.map(pos => ({ ...pos, checked: true }));
      this.selectedPlaceOfService = [...this.placeOfServiceList];

      this.loaderService.hide();
    });
  }

  applyFilters() {
    this.showClearFilter = true;
    this.isClearFilterDisabled = false; // Enable Clear Filters button
    this.filterChanged.emit();
  }

  clearFilters() {
    if (this.userList) {
      this.userList.forEach(x => x.checked = false);
    }

    if (this.funderList) {
      this.funderList.forEach(x => x.checked = false);
    }

    if (this.staffList) {
      this.staffList.forEach(x => x.checked = false);
    }

    if (this.locationList) {
      this.locationList.forEach(x => x.checked = false);
    }

    if (this.placeOfServiceList) {
      this.placeOfServiceList.forEach(x => x.checked = false);
    }
    this.selectedFilterId = undefined;
    this.selectedPatients = [];
    this.selectedfunders = [];
    this.selectedStaff = [];
    this.selectedLocation = [];
    this.selectedPlaceOfService = [];
    this.clearDateRange();
    this.showClearFilter = false;
    this.isFiltersApplied = false; // Disable button when all filters are cleared
  }

  selectFilter1(filterId: number | undefined, anchor: any = undefined) {
    this.selectedFilterId = filterId;
    this.selectedAnchor = anchor;
  }

  selectFilter(filterId: number | undefined, anchor: any = undefined) {
    this.selectedFilterId = filterId;
    this.selectedAnchor = anchor;
    if (filterId == 5)
      if (this.dateFrom == undefined || this.dateTo == undefined) {
        this.dateFrom = new Date();
        this.dateTo = new Date();
      }
    this.showDatePopup = true;
  }

  onFilterChange() {
    this.isFiltersApplied = true;
    this.showClearFilter = false;
    this.isClearFilterDisabled = false;
    this.checkFiltersForApply();
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

  clearDateRange(): void {
    this.dateFrom = null;
    this.dateTo = null;
    this.dateFromString = '';
    this.dateToString = '';
    this.showDatePopup = false;
    this.isFiltersApplied = false;
    this.isClearFilterDisabled = true;
  }

  checkFiltersForApply() {
    this.isFiltersApplied = (
      this.selectedPatients.length > 0 ||
      this.selectedfunders.length > 0 ||
      this.selectedStaff.length > 0 ||
      this.selectedLocation.length > 0 ||
      this.selectedPlaceOfService.length > 0 ||
      (!!this.dateFromString && !!this.dateToString)
    );
  }

  onFilterChangeForStaff(staffs?: ClaimFilterOptionModel[]) {
    this.selectedStaff = staffs || [];
    this.showClearFilter = false;
    this.isClearFilterDisabled = false;
    this.checkFiltersForApply();
  }
}
