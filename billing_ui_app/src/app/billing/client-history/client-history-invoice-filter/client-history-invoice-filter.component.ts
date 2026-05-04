import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild } from '@angular/core';
import { FormGroup } from '@angular/forms';
import { Subject } from 'rxjs';
import { DatePipe } from '@angular/common';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { PatientInvoiceStatus, PatientInvoiceStatusLabel } from '@core/enums/billing/patient-invoice-status';
import { ClientHistoryInvoiceFilterService } from '../../services/client-history-invoice-filter.service';

@Component({
  selector: 'app-client-history-invoice-filter',
  templateUrl: './client-history-invoice-filter.component.html',
  styleUrls: ['./client-history-invoice-filter.component.css']
})
export class ClientHistoryInvoiceFilterComponent {
  @Input() opened: boolean = false;
  @Input() statusList: ClaimFilterOptionModel[];
  @ViewChild('popup', { read: ElementRef }) popupRef!: ElementRef;
  @ViewChild('dosAnchorEl', { read: ElementRef }) public dosAnchorEl: ElementRef;
  @Output() filterChanged = new EventEmitter<any>();

  @Input() filterForm: FormGroup;
  public viewVoidedFromStatusPopup: boolean = false;
  isFiltersApplied: boolean = false;
  isFilterButtonDisabled: boolean = false;

  /** POPUP CONTROL */
  selectedFilterId: number | undefined = undefined;
  selectedAnchor: any;
  showDatePopup = false;
  
  private destroy$ = new Subject<void>();

  /** STATUS OPTIONS */
  selectedStatus: ClaimFilterOptionModel[] = [];
  isClosedFilterSet: boolean = false;
  isClosedFilterReset: boolean = false;
  userTouchedStatuses: boolean = false;

  /** Patient Responsibility Range */
  patientResponsibilityFrom: number | undefined;
  patientResponsibilityTo: number | undefined;

  /** Patient Balance Range (NEW) */
  patientBalanceFrom: number | undefined;
  patientBalanceTo: number | undefined;

  /** DATE RANGE (Date of Service) */
  dateOfServiceFrom!: Date | undefined;
  dateOfServiceTo!: Date | undefined;
  dateOfServiceFromString!: string;
  dateOfServiceToString!: string;

  /** INVOICE DATE RANGE */
  invoiceDateFrom!: Date | undefined;
  invoiceDateTo!: Date | undefined;
  invoiceDateFromString!: string | undefined;
  invoiceDateToString!: string | undefined;

  /** INVOICE DUE DATE RANGE */
  invoiceDueDateFrom!: Date | undefined;
  invoiceDueDateTo!: Date | undefined;
  invoiceDueDateFromString!: string | undefined;
  invoiceDueDateToString!: string | undefined;

  constructor(private datePipe: DatePipe, private clientHistoryInvoiceFilterService: ClientHistoryInvoiceFilterService) {
  }

  ngOnInit(): void {
    this.loadStatuses();
    /** Default 180 days or 6 months */
    const today = new Date();
    const fromDate = new Date();
    fromDate.setDate(today.getDate() - 180);

    this.dateOfServiceFrom = fromDate;
    this.dateOfServiceTo = today;

    this.dateOfServiceFromString = this.datePipe.transform(this.dateOfServiceFrom, 'MM/dd/yy');
    this.dateOfServiceToString = this.datePipe.transform(this.dateOfServiceTo, 'MM/dd/yy');
  }

    private loadStatuses() {
    this.statusList = Object.keys(PatientInvoiceStatus)
      .filter(key => !isNaN(Number(key)))
      .map(key => ({
        id: Number(key),
        name: PatientInvoiceStatusLabel[Number(key)],
        checked: true
      })) as ClaimFilterOptionModel[];

      this.selectedStatus = [...this.statusList];
  }

  private contains(target: HTMLElement): boolean {
      return this.selectedAnchor.contains(target) ||
          (this.popupRef ? this.popupRef.nativeElement.contains(target) : false);
  }

  //selectFilter
  selectFilter(id: number | undefined, anchor: any = null) {
    this.selectedAnchor = anchor;

    this.selectedFilterId = this.selectedFilterId === id ? undefined : id;

    if (id === 3 || id === 4 || id === 5) {
      this.showDatePopup = true;
    }
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

@HostListener('keydown', ['$event'])
  public keydown(event: any): void {
      if (event.keyCode === 27) {
          this.selectFilter(undefined);
      }
  }   

  //setDatePeriod
  setDatePeriod(range: any) {
    this.dateOfServiceFrom = range.start ? new Date(range.start) : undefined;
    this.dateOfServiceTo = range.end ? new Date(range.end) : undefined; 
    this.dateOfServiceFromString = this.datePipe.transform(this.dateOfServiceFrom, 'MM/dd/yy')!;
    this.dateOfServiceToString = this.datePipe.transform(this.dateOfServiceTo, 'MM/dd/yy')!;
    this.onFilterChange();
    this.showDatePopup = false;
  }

  //setPatientResponsibilityRange
  setPatientResponsibilityRange(range: any): void {
    this.patientResponsibilityFrom = range.From;
    this.patientResponsibilityTo = range.To;
    this.onFilterChange();
  }

  //setPatientBalanceRange
  setPatientBalanceRange(range: any): void {
    this.patientBalanceFrom = range.From;
    this.patientBalanceTo = range.To;
    this.onFilterChange();
  }

  //setInvoiceDatePeriod
  setInvoiceDatePeriod(range: any) {
  this.invoiceDateFrom = range.start ? new Date(range.start) : undefined;
  this.invoiceDateTo = range.end ? new Date(range.end) : undefined; 
  this.invoiceDateFromString = this.datePipe.transform(this.invoiceDateFrom, 'MM/dd/yy')!;
  this.invoiceDateToString = this.datePipe.transform(this.invoiceDateTo, 'MM/dd/yy')!; 
  this.onFilterChange();
  this.showDatePopup = false;
}

  //setInvoiceDueDatePeriod
  setInvoiceDueDatePeriod(range: any) {
    this.invoiceDueDateFrom = range.start ? new Date(range.start) : undefined;
    this.invoiceDueDateTo = range.end ? new Date(range.end) : undefined;
    this.invoiceDueDateFromString = this.datePipe.transform(range.start, 'MM/dd/yy')!;
    this.invoiceDueDateToString = this.datePipe.transform(range.end, 'MM/dd/yy')!;
    this.onFilterChange();
    this.showDatePopup = false;
  }

  //onDateFilterLeave
  onDateFilterLeave() {
    this.showDatePopup = false;
  }
  //applyFilters
  applyFilters() {
    this.filterChanged.emit();
    this.isFiltersApplied = true;
    this.isFilterButtonDisabled = false;
    }

  //clearFilters
  clearFilters() {
    // Reset all statuses to checked
    this.selectedStatus = [];   

    this.dateOfServiceFrom = this.dateOfServiceTo = undefined;
    this.dateOfServiceFromString =  this.dateOfServiceToString = undefined;

    this.invoiceDateFrom = this.invoiceDateTo = undefined;
    this.invoiceDateFromString = this.invoiceDateToString = undefined;

    this.invoiceDueDateFrom = this.invoiceDueDateTo = undefined;
    this.invoiceDueDateFromString = this.invoiceDueDateToString = undefined;

    this.patientResponsibilityFrom = undefined;
    this.patientResponsibilityTo = undefined;
    this.patientBalanceFrom = undefined;
    this.patientBalanceTo = undefined;
    this.clientHistoryInvoiceFilterService.isFilterSet = false;
    this.statusList.forEach(s => s.checked = false);
    this.filterChanged.emit();  
    this.isFilterButtonDisabled = true;
    this.isFiltersApplied = false;
  }

  onFilterChanged() {
     this.filterChanged.emit();  
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onFilterChange() {
      this.isFiltersApplied = false;
      this.isFilterButtonDisabled = false;
  }
}