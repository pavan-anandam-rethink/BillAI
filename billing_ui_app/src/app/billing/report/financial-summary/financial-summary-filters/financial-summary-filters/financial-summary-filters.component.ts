import { DatePipe } from '@angular/common';
import { Component, ElementRef, EventEmitter, HostListener, Input, OnInit, Output, ViewChild } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { Subject, forkJoin, takeUntil } from 'rxjs';
import { ReportService } from '@core/services/billing/report.service';
import { LoaderService } from '@core/services/common';
import { ClaimService } from '@core/services/billing';
import { AccountMemberService } from "@core/services/account/account-member.service";
import { ClaimListingTab } from "@core/enums/billing/claim-listing-tab";

@Component({
  selector: 'financial-summary-filters',
  templateUrl: './financial-summary-filters.component.html',
  styleUrls: ['./financial-summary-filters.component.css']
})
export class FinancialSummaryFiltersComponent implements OnInit{
  @Input() opened: boolean = false;
  @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;
  @Output() filterChanged = new EventEmitter();
  @Output() clearFilter = new EventEmitter();
  @Input() selectedReportType = 'Monthly';
  @ViewChild('dateAnchorEl', { read: ElementRef }) public dateAnchor: ElementRef;
  @Output() requestData = new EventEmitter<{
    funder: number[];
    date: Date;
  }>();
  isFiltersApplied: boolean = true;
  selectedLocation: ClaimFilterOptionModel[] = [];
  selectedFilterId: number | undefined = undefined;
  selectedAnchor: any;
  dateFrom:Date | undefined;
  dateTo:Date | undefined;
  showDatePopup = false;
  selectedRenderingProviders: ClaimFilterOptionModel[] = [];
  userList: ClaimFilterOptionModel[] = [];
  funderList: ClaimFilterOptionModel[] = [];
  locationList: ClaimFilterOptionModel[] = [];
  selectedfunders: ClaimFilterOptionModel[] = [];
  dateFromString: string | undefined;
  dateToString: string | undefined;
  showClearFilter = false;
  isClearFilterDisabled = true;
  dataRangeTypeNum: number | undefined;
  private unsubscribeAll$ = new Subject();
  tab: ClaimListingTab | 1;
  renderingProviderName: string;
  billingProviderList: ClaimFilterOptionModel[] = [];
  selectedbillingProvider: ClaimFilterOptionModel[] = [];
  isFilterButtonDisabled = true;
  renderingProviders: ClaimFilterOptionModel[] = [];
  isAllFunderSelected: boolean = false;
  isAllLocationSelected: boolean = false;
  isAllRenderingProviderSelected: boolean = false;
  isAllBillingProviderSelected: boolean = false;
  selectFilter(filterId: number | undefined, anchor: any = undefined) {
    this.selectedFilterId = filterId;
    this.selectedAnchor = anchor;
    if(filterId == 5)
        if(this.dateFrom == undefined || this.dateTo == undefined)
        {
        this.dateFrom = new Date();
        this.dateTo = new Date();
      }
      if (filterId === 6) {
        this.loadRenderingProvidersIfNeeded();
      }
      if (filterId === 7) {
        this.loadBillingProvidersIfNeeded();
      }
    this.showDatePopup = true;
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
  private contains(target: HTMLElement): boolean {
    return this.selectedAnchor.contains(target) ||
      (this.popup ? this.popup.nativeElement.contains(target) : false);
  }
  constructor(
    private datePipe: DatePipe,
    private reportingService: ReportService,
    private loaderService: LoaderService,
    private claimsService: ClaimService, 
    private accountService: AccountMemberService
  ) { }
  ngOnInit(): void {
    const today = new Date();
  
    // First date of current month
    const fromDate = new Date(today.getFullYear(), today.getMonth(), 1);
  
    this.dateFrom = fromDate;
    this.dateTo = today;
    this.dateFromString = this.datePipe.transform(this.dateFrom, 'MM/dd/yy');
    this.dateToString = this.datePipe.transform(this.dateTo, 'MM/dd/yy');
   
   forkJoin({
    funders: this.reportingService.getAssignedFunders(),
    locations: this.reportingService.getLocationListByIds(),
    renderingProviders: this.claimsService.getRenderingProviders(),
    billingProviders: this.reportingService.getLatestBillingProviders(),
   
  }).subscribe(results => {

    this.funderList = results.funders.funders.map(funder => ({ id: Number(funder.id), name: funder.funderName, checked: true }));
    this.selectedfunders = [...this.funderList];
    this.isAllFunderSelected = this.funderList.length > 0;

    this.locationList = results.locations.map(location => ({ ...location, checked: true }));
    this.selectedLocation = [...this.locationList];
    this.isAllLocationSelected = this.locationList.length > 0;

    this.userList = results.renderingProviders.map(provider => ({ ...provider, checked: true }));
    this.selectedRenderingProviders = [...this.userList];
    this.isAllRenderingProviderSelected = this.userList.length > 0;

    this.billingProviderList = results.billingProviders.map((billing: any) =>
      ({ id: Number(billing.locationBillingProviderNpiNumber),
        name: billing.locationBillingProviderName,
        checked: true }));
    this.selectedbillingProvider = [...this.billingProviderList];
    this.isAllBillingProviderSelected = this.billingProviderList.length > 0;

    this.loaderService.hide(); 
  });

  }

  private loadBillingProvidersIfNeeded(): void{
    if (this.billingProviderList.length > 0) {
      return;
    }
    this.reportingService.getLatestBillingProviders()
    .subscribe({
      next: (response: any[]) => {
        this.billingProviderList = response.map(billing =>
           ({ id:  Number(billing.locationBillingProviderNpiNumber),
            name: billing.locationBillingProviderName,
            checked: true }));
        this.selectedbillingProvider = [...this.billingProviderList];
        this.isAllBillingProviderSelected = this.billingProviderList.length > 0;
      },
      error: () => {
        console.error('Failed to load financial summary');      }
     
    });  
  }
  private loadRenderingProvidersIfNeeded(): void {
    if (this.userList.length > 0) {
      return;
    }
    forkJoin({
      renderingprovider: this.claimsService.getRenderingProviders(),
    }).subscribe(results => {
    this.userList = results.renderingprovider.map(provider => ({ ...provider, checked: true }));
    this.selectedRenderingProviders = [...this.userList];
    this.isAllRenderingProviderSelected = this.userList.length > 0;
  });    
    
  }
  applyFilters() {
    this.showClearFilter = true;
    this.isClearFilterDisabled = false; 
    this.filterChanged.emit();
  }
  clearFilters() {
    if (this.funderList) {
      this.funderList.forEach(x => x.checked = false);
    }

    if (this.locationList) {
      this.locationList.forEach(x => x.checked = false);
    }
    this.selectedLocation = [];
    this.selectedFilterId = undefined;
    this.selectedfunders = [];
    
    // Clear date range for Funder report type
    if (this.selectedReportType === 'Funder') {
      this.clearDateRange();
      // Also clear Rendering and Billing providers for Funder reports
      if (this.selectedRenderingProviders) {
        this.selectedRenderingProviders.forEach(p => p.checked = false);
      }
      if (this.userList) {
        this.userList.forEach(p => p.checked = false);
      }
      if (this.selectedbillingProvider) {
        this.selectedbillingProvider.forEach(p => p.checked = false);
      }
      if (this.billingProviderList) {
        this.billingProviderList.forEach(p => p.checked = false);
      }
    }
    
    this.showClearFilter = false;
    this.isFiltersApplied = false; 
  }
  clearFiltersByClick() {
    if (this.funderList) {
      this.funderList.forEach(x => x.checked = false);
    }

    if (this.locationList) {
      this.locationList.forEach(x => x.checked = false);
    }
    this.selectedLocation = [];
    this.selectedFilterId = undefined;
    this.selectedfunders = [];
    
    this.clearDateRange();
    // For Funder report type, also clear Rendering and Billing providers
    if (this.selectedReportType === 'Funder') {
      if (this.selectedRenderingProviders) {
        this.selectedRenderingProviders.forEach(p => p.checked = false);
      }
      if (this.userList) {
        this.userList.forEach(p => p.checked = false);
      }
      if (this.selectedbillingProvider) {
        this.selectedbillingProvider.forEach(p => p.checked = false);
      }
      if (this.billingProviderList) {
        this.billingProviderList.forEach(p => p.checked = false);
      }
    }
    this.clearFilter.emit();  
    this.showClearFilter = false;
    this.isFiltersApplied = false; 
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
  onFilterChange() {
    const selectedFunderIds = new Set(this.selectedfunders.map(x => x.id));
    this.isAllFunderSelected = this.funderList.length > 0 && 
      this.funderList.every(funder => selectedFunderIds.has(funder.id));

    const selectedLocationIds = new Set(this.selectedLocation.map(x => x.id));
    this.isAllLocationSelected = this.locationList.length > 0 && 
      this.locationList.every(location => selectedLocationIds.has(location.id));

    // Check rendering providers - use checked property
    this.isAllRenderingProviderSelected = this.userList.length > 0 && 
      this.userList.every(provider => provider.checked);

    // Check billing providers - use checked property
    this.isAllBillingProviderSelected = this.billingProviderList.length > 0 && 
      this.billingProviderList.every(provider => provider.checked);

    this.isFiltersApplied = true;
    this.showClearFilter = false;
    this.isClearFilterDisabled = false;
    this.isFilterButtonDisabled = false;
    this.checkFiltersForApply();
  }
 
  get checkedBillingProviderCount(): number {
    return this.selectedbillingProvider.filter(p => p.checked).length ?? 0;
  }

  get checkedRenderingProviderCount(): number {
    return this.selectedRenderingProviders.filter(p => p.checked).length ?? 0;
  }
  checkFiltersForApply() {
    this.isFiltersApplied = (
      this.selectedfunders.length > 0 ||
      this.selectedLocation.length > 0 ||
      (!!this.dateFromString && !!this.dateToString)
    );
  }
  onDateFilterLeave() {
    this.showDatePopup = false;
  }

  onPrimaryButtonClick(): void {
    if (this.showClearFilter) {
      this.clearFiltersByClick();
    } else {
      this.applyFilters();
    }
  }
}
