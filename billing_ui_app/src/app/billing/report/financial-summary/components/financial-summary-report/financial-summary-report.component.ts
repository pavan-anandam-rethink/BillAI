import { ChangeDetectorRef, Component, OnInit, ViewChild, forwardRef } from '@angular/core';
import { Breadcrumb } from '@core/models/billing/bread-crumb';
import { GridDataResult } from '@progress/kendo-angular-grid';
import { Observable, Subscription,fromEvent, throttleTime } from 'rxjs';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ReportService } from '@core/services/billing/report.service';
import { FinancialSummaryFiltersComponent } from '../../financial-summary-filters/financial-summary-filters/financial-summary-filters.component';
import { FinancialSummaryRequestModel } from "@core/models/billing/report-model";
import { Helper } from '../../../../encounters/common/common-helper';
import { FinancialSummaryBaseRequest } from '@core/models/billing/monthly-financial-summary-request';
import { FinancialSummaryResponse, FunderFinancialSummaryResponse } from '../../../../../core/models/billing/monthly-financial-summary-request';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { AccountPermissions } from '@core/enums/account';
import {
  REPORT_TYPE_MONTHLY,
  REPORT_TYPE_FUNDER,
  REPORT_TYPE_OPTIONS,
  DEFAULT_PREFETCH_THRESHOLD,
  SCROLL_DEBOUNCE_MS,
  EXCEL_MIME_TYPE,
  MONTHLY_EXCEL_FILENAME,
  FUNDER_EXCEL_FILENAME,
  LABEL_DATE_RANGE,
  LABEL_LOCATION,
  LABEL_FUNDER,
  LABEL_PREVIOUS_PERIOD,
  LABEL_TOTAL
} from '@core/constants';

@Component({
  selector: 'financial-summary-report',
  templateUrl: './financial-summary-report.component.html',
  styleUrls: ['./financial-summary-report.component.css']
})
export class FinancialSummaryReportComponent implements OnInit {
  financialSummary: FinancialSummaryResponse | null = null;
  funderFinancialSummary: FunderFinancialSummaryResponse | null = null;
  view: Observable<GridDataResult>;
  canEdit = false;
  userList: ClaimFilterOptionModel[] = [];
  public isLoading = true;
  showFilter: boolean;
  @ViewChild('encountersGrid', { static: true }) grid: any;
  private scrollPrefetchSub: Subscription | null = null;
  private permissionSub: Subscription | null = null;
  public isVirtualMode = false;
  gridView!: GridDataResult;
  funderGridView!: GridDataResult;
  reportTypeOptions = REPORT_TYPE_OPTIONS;
  selectedReportType = REPORT_TYPE_MONTHLY;
  private prefetchThreshold = DEFAULT_PREFETCH_THRESHOLD;
  private scrollDebounceMs = SCROLL_DEBOUNCE_MS;
  private hasAppliedMonthly = false;
  private hasAppliedFunder = false;
  @ViewChild(forwardRef(() => FinancialSummaryFiltersComponent))
  filtersComponent: FinancialSummaryFiltersComponent;
  constructor(
    private reportingService: ReportService,
    private cdr: ChangeDetectorRef,
    private notificationService: NotificationHandlerService,
    private accountService: AccountMemberService
  ) {
    this.reportingService.getFunders().subscribe((x) => {
      this.userList =
        this.userList.length == 0
          ? x.map((y) => ({
              id: y.funderId,
              name: y.funderName,
              checked: false,
            }))
          : this.userList;
      this.isLoading = false;
    });
  }

  get checkedGridResultCount(): number {
    if (this.selectedReportType === REPORT_TYPE_FUNDER) {
      return this.funderGridView?.data?.length ?? 0;
    }
    return this.gridView?.data?.length ?? 0;
  }

  onReportTypeChange(reportType: string) {
    this.selectedReportType = reportType;
    // Keep filter component in sync
    if (this.filtersComponent) {
      this.filtersComponent.selectedReportType = reportType as any;
    }
    
    // Clear the previous report type data
    if (reportType === REPORT_TYPE_MONTHLY) {
      this.funderFinancialSummary = null;
      this.funderGridView = { data: [], total: 0 };
    } else if (reportType === REPORT_TYPE_FUNDER) {
      this.financialSummary = null;
      this.gridView = { data: [], total: 0 };
    }

    // Auto-apply only if this report type had filters applied before; else require manual Apply
    const shouldAutoApply =
      reportType === REPORT_TYPE_MONTHLY ? this.hasAppliedMonthly : this.hasAppliedFunder;

    if (shouldAutoApply) {
      this.onFilterChanged();
    } else {
      if (this.filtersComponent) {
        this.filtersComponent.showClearFilter = false;
        this.filtersComponent.isClearFilterDisabled = true;
        this.filtersComponent.isFilterButtonDisabled = false; // allow Apply
        this.filtersComponent.isFiltersApplied = false; // reflect pending state
      }
      this.isLoading = false;
    }
  }
  ngOnInit(): void {
    this.permissionSub = this.accountService.accountMemberSettings.subscribe((x) => {
    if (x) {
      this.canEdit = this.accountService.checkPermissionLevel(
        AccountPermissions.BillingEdit
      );
    }
  });
   
  }
  ngAfterViewInit() {
    this.filtersComponent.clearFilters();
    this.hasAppliedMonthly = false;
    this.hasAppliedFunder = false;
    this.toggleFilter(true);
    setTimeout(() => this.setupScrollListener(), 0);
  }

  ngOnDestroy(): void {
    if (this.scrollPrefetchSub) {
      this.scrollPrefetchSub.unsubscribe();
      this.scrollPrefetchSub = null;
    }
    if (this.permissionSub) {
    this.permissionSub.unsubscribe();
    this.permissionSub = null;
  }
  }
  toggleFilter(event: boolean) {
    this.filtersComponent.opened = event;
    this.showFilter = event;
    this.cdr.detectChanges();
  }
  private setupScrollListener(): void {
    const gridEl =
      this.grid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    if (!gridEl) return;

    this.scrollPrefetchSub = fromEvent(gridEl, 'scroll')
      .pipe(throttleTime(this.scrollDebounceMs))
      .subscribe(() => {
        if (!this.isVirtualMode) return;

        const scrollPercentage =
          (gridEl.scrollTop + gridEl.clientHeight) / gridEl.scrollHeight;

        if (scrollPercentage >= this.prefetchThreshold) {
          // prefetch logic could go here
        }
      });
  }
  exportToExcel(): void {

  if (this.selectedReportType === REPORT_TYPE_FUNDER) {
    this.exportFunderToExcel();
    return;
  }
  
  if (!this.financialSummary || !this.financialSummary.rows?.length) {
    return;
  }

  this.isLoading = true;

  const filter = new FinancialSummaryBaseRequest();

  this.applyCommonFilters(filter);
  filter.renderingProviderIds = this.filtersComponent.isAllRenderingProviderSelected  ? []  : this.filtersComponent.selectedRenderingProviders?.filter(p => p.checked).map(x => x.staffMemberId) || [];
  filter.billingProviderIds = this.filtersComponent.isAllBillingProviderSelected ? [] : this.filtersComponent.selectedbillingProvider?.filter(p => p.checked).map(x => x.id) || [];
  this.applyDateFilters(filter);
  filter.locationNames = this.filtersComponent.selectedLocation.map(x => x.name);
  this.reportingService
    .exportMonthlyFinancialSummaryToExcel(filter)
    .subscribe({
      next: (response: any) => {
        const base64Data = response.data;
        const blob = this.base64ToBlob(base64Data);
        const url = window.URL.createObjectURL(blob);

        const a = document.createElement('a');
        a.href = url;
        a.download = MONTHLY_EXCEL_FILENAME;
        document.body.appendChild(a);
        a.click();

        window.URL.revokeObjectURL(url);
        a.remove();
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        console.error('Failed to export Monthly Financial Summary');
      }
    });
}

exportFunderToExcel(): void {
  if (!this.funderFinancialSummary || !this.funderFinancialSummary.rows?.length) {
    return;
  }

  this.isLoading = true;

  // Use the same request object you already use for Funders
  const filter: any = new FinancialSummaryBaseRequest();

  this.applyCommonFilters(filter);
  filter.renderingProviderIds = this.filtersComponent.isAllRenderingProviderSelected ? [] : this.filtersComponent.selectedRenderingProviders?.filter(p => p.checked).map(x => x.staffMemberId) || [];
  filter.billingProviderIds = this.filtersComponent.isAllBillingProviderSelected ? [] : this.filtersComponent.selectedbillingProvider?.filter(p => p.checked).map(x => x.id) || [];
  this.applyDateFilters(filter);

  // (Optional) used by backend for excel header
  filter.locationNames = this.filtersComponent.selectedLocation.map(x => x.name);
  filter.billingProviderNames =
    this.filtersComponent.selectedbillingProvider?.filter(p => p.checked).map(x => x.name) || [];
  filter.renderingProviderNames =
    this.filtersComponent.selectedRenderingProviders?.filter(p => p.checked).map(x => x.name) || [];

  this.reportingService
    .exportFunderFinancialSummaryToExcel(filter)
    .subscribe({
      next: (response: any) => {
        const base64Data = response.data;
        const blob = this.base64ToBlob(base64Data);
        const url = window.URL.createObjectURL(blob);
        const start = this.formatDateForFileNameMMDDYYYY(this.filtersComponent.dateFrom);
        const end = this.formatDateForFileNameMMDDYYYY(this.filtersComponent.dateTo);
        const a = document.createElement('a');
        a.href = url;
        a.download = FUNDER_EXCEL_FILENAME;
        document.body.appendChild(a);
        a.click();

        window.URL.revokeObjectURL(url);
        a.remove();
        this.isLoading = false;
      },
      error: (error) => {
        this.isLoading = false;
        console.error('Failed to export Funder Financial Summary', error);
      }
    });

  }

private applyCommonFilters(filter: {
  funderIds?: number[];
  locationIds?: number[];
}): void {
  filter.funderIds = this.filtersComponent.isAllFunderSelected  ? []  : this.filtersComponent.selectedfunders.map(x => x.id);
  filter.locationIds = this.filtersComponent.isAllLocationSelected  ? [] : this.filtersComponent.selectedLocation.map(x => x.id);
}

private applyDateFilters(
  filter: FinancialSummaryBaseRequest,
  defaultDateType: string = 'Transaction'
): void {
  filter.startDate = Helper.shiftDateToUTC(this.filtersComponent.dateFrom);
  filter.endDate = Helper.shiftDateToUTC(this.filtersComponent.dateTo);
}

base64ToBlob(base64: string): Blob {
  const byteCharacters = atob(base64);
  const byteArrays = [];

  for (let offset = 0; offset < byteCharacters.length; offset += 1024) {
    const slice = byteCharacters.slice(offset, offset + 1024);
    const byteNumbers = new Array(slice.length);

    for (let i = 0; i < slice.length; i++) {
      byteNumbers[i] = slice.charCodeAt(i);
    }

    byteArrays.push(new Uint8Array(byteNumbers));
  }

  return new Blob(byteArrays, {
    type: EXCEL_MIME_TYPE,
  });
}

private formatDateForFileNameMMDDYYYY(date: Date): string {
  const d = new Date(date);
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  const yyyy = d.getFullYear();
  return `${mm}${dd}${yyyy}`; // mmddyyyy
}

  applyFilter(event: any) {
    if (!event) {
     
      return;
    } 
    
    // Call appropriate API based on selected report type
    if (this.selectedReportType === REPORT_TYPE_MONTHLY) {
      this.loadMonthlyFinancialSummary();
    } else if (this.selectedReportType === REPORT_TYPE_FUNDER) {
      this.loadFunderFinancialSummary();
    }
  }

  clearFilter() {
    // Only clear the active grid based on selected report type
    if (this.selectedReportType === REPORT_TYPE_MONTHLY) {
      this.financialSummary = null;
      this.gridView = { data: [], total: 0 };
      this.hasAppliedMonthly = false;
    } else if (this.selectedReportType === REPORT_TYPE_FUNDER) {
      this.funderFinancialSummary = null;
      this.funderGridView = { data: [], total: 0 };
      this.hasAppliedFunder = false;
    }
  }
  onFilterChanged() {
    const missingFields = [];
    
    // Validate required fields based on report type
    if (this.selectedReportType === REPORT_TYPE_FUNDER) {
      if (!this.filtersComponent.dateFrom || !this.filtersComponent.dateTo) {
        missingFields.push(LABEL_DATE_RANGE);
      }
      
      if (!this.filtersComponent.selectedLocation || this.filtersComponent.selectedLocation.length === 0) {
        missingFields.push(LABEL_LOCATION);
      }
      
      if (!this.filtersComponent.selectedfunders || this.filtersComponent.selectedfunders.length === 0) {
        missingFields.push(LABEL_FUNDER);
      }
    } else if (this.selectedReportType === REPORT_TYPE_MONTHLY) {
      // Validate required fields for Monthly report type
      if (!this.filtersComponent.dateFrom || !this.filtersComponent.dateTo) {
        missingFields.push(LABEL_DATE_RANGE);
      }
    }
    
    if (missingFields.length > 0) {
      this.notificationService.showNotificationError(
        `Please select the following required field${missingFields.length > 1 ? 's' : ''}: ${missingFields.join(', ')}`
      );
      return;
    }

    this.isLoading = true;

    const filter = new FinancialSummaryBaseRequest();
    // Send empty array when all are selected (means "all")
    filter.funderIds = this.filtersComponent.isAllFunderSelected  ? []  : this.filtersComponent.selectedfunders.map((x) => x.id);

    filter.locationIds = this.filtersComponent.isAllLocationSelected ? []  : this.filtersComponent.selectedLocation.map((x) => x.id);
    filter.renderingProviderIds = this.filtersComponent.isAllRenderingProviderSelected ? []  : this.filtersComponent.selectedRenderingProviders?.filter(p => p.checked).map(x => x.staffMemberId ) || [];
    filter.billingProviderIds = this.filtersComponent.isAllBillingProviderSelected ? []  : this.filtersComponent.selectedbillingProvider?.filter(p => p.checked).map(x => x.id) || [];

    filter.startDate = Helper.shiftDateToUTC(this.filtersComponent.dateFrom);
    filter.endDate = Helper.shiftDateToUTC(this.filtersComponent.dateTo);
    
    // Ensure dateType has a valid value (Transaction or Deposit)
    
   
    // Call appropriate API based on selected report type
    if (this.selectedReportType === REPORT_TYPE_MONTHLY) {
      this.loadMonthlyFinancialSummaryWithFilter(filter);
    } else if (this.selectedReportType === REPORT_TYPE_FUNDER) {
      this.loadFunderFinancialSummaryWithFilter(filter);
    }
  }

  private loadMonthlyFinancialSummaryWithFilter(filter: FinancialSummaryBaseRequest): void {
    // Ensure dates are properly formatted
    const startDate = filter.startDate;
    const endDate = filter.endDate;
    filter.startDate = startDate ? new Date(startDate.toISOString().split('T')[0]) : startDate;
    filter.endDate = endDate ? new Date(endDate.toISOString().split('T')[0]) : endDate;
    
    // Date basis is determined server-side; do not force a client-side dateType
    
    this.reportingService.getMonthlyFinancialSummary(filter)
    .subscribe({
      next: (response: FinancialSummaryResponse) => {
        this.financialSummary = response;
        this.loadGrid();
        this.hasAppliedMonthly = true;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Failed to load monthly financial summary', error);
        this.isLoading = false;
      }
     
    });
  }

  private loadFunderFinancialSummaryWithFilter(filter: FinancialSummaryBaseRequest): void {
    // Ensure dates are properly formatted
    const startDate = filter.startDate;
    const endDate = filter.endDate;
    filter.startDate = startDate ? new Date(startDate.toISOString().split('T')[0]) : startDate;
    filter.endDate = endDate ? new Date(endDate.toISOString().split('T')[0]) : endDate;
    
    this.reportingService.getFunderFinancialSummary(filter)
    .subscribe({
      next: (response: FunderFinancialSummaryResponse) => {
        this.funderFinancialSummary = response;
        this.loadFunderGrid();
        this.hasAppliedFunder = true;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Failed to load funder financial summary', error);
        this.isLoading = false;
      }
    });
  }

  private loadMonthlyFinancialSummary(): void {
    const filter = new FinancialSummaryBaseRequest();
    filter.funderIds = this.filtersComponent.isAllFunderSelected  ? []  : this.filtersComponent.selectedfunders.map((x) => x.id);
    filter.locationIds = this.filtersComponent.isAllLocationSelected  ? []  : this.filtersComponent.selectedLocation.map((x) => x.id);
    filter.renderingProviderIds = this.filtersComponent.isAllRenderingProviderSelected  ? []  : this.filtersComponent.selectedRenderingProviders?.filter(p => p.checked).map(x => x.staffMemberId) || [];
    filter.billingProviderIds = this.filtersComponent.isAllBillingProviderSelected  ? [] : this.filtersComponent.selectedbillingProvider?.filter(p => p.checked).map(x => x.id) || [];
    
    // Ensure dates are properly formatted
    const startDate = Helper.shiftDateToUTC(this.filtersComponent.dateFrom);
    const endDate = Helper.shiftDateToUTC(this.filtersComponent.dateTo);
    filter.startDate = startDate ? new Date(startDate.toISOString().split('T')[0]) : startDate;
    filter.endDate = endDate ? new Date(endDate.toISOString().split('T')[0]) : endDate;
    
    // Date basis is determined server-side; do not force a client-side dateType

    this.reportingService.getMonthlyFinancialSummary(filter)
    .subscribe({
      next: (response: FinancialSummaryResponse) => {
        this.financialSummary = response;
        this.loadGrid();
        this.hasAppliedMonthly = true;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Failed to load monthly financial summary', error);
        this.isLoading = false;
      }
    });
  }

  private loadFunderFinancialSummary(): void {
    const filter = new FinancialSummaryBaseRequest();
    filter.funderIds = this.filtersComponent.isAllFunderSelected  ? []  : this.filtersComponent.selectedfunders.map((x) => x.id);
    filter.locationIds = this.filtersComponent.isAllLocationSelected  ? []  : this.filtersComponent.selectedLocation.map((x) => x.id);
    filter.renderingProviderIds = this.filtersComponent.isAllRenderingProviderSelected  ? []  : this.filtersComponent.selectedRenderingProviders?.filter(p => p.checked).map(x => x.staffMemberId) || [];
    filter.billingProviderIds = this.filtersComponent.isAllBillingProviderSelected  ? []  : this.filtersComponent.selectedbillingProvider?.filter(p => p.checked).map(x => x.id) || [];
    filter.billingProviderNames = this.filtersComponent.selectedbillingProvider?.filter(p => p.checked).map(x => x.name) || [];
    filter.renderingProviderNames = this.filtersComponent.selectedRenderingProviders?.filter(p => p.checked).map(x => x.name) || [];
  
    // Ensure dates are properly formatted
    const startDate = Helper.shiftDateToUTC(this.filtersComponent.dateFrom);
    const endDate = Helper.shiftDateToUTC(this.filtersComponent.dateTo);
    filter.startDate = startDate ? new Date(startDate.toISOString().split('T')[0]) : startDate;
    filter.endDate = endDate ? new Date(endDate.toISOString().split('T')[0]) : endDate;
    
    // Date basis is determined server-side; do not force a client-side dateType

    console.log('Funder Financial Summary Request:', JSON.stringify(filter, null, 2));

    this.reportingService.getFunderFinancialSummary(filter)
    .subscribe({
      next: (response: FunderFinancialSummaryResponse) => {
        this.funderFinancialSummary = response;
        this.loadFunderGrid();
        this.hasAppliedFunder = true;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Failed to load funder financial summary', error);
        console.error('Error details:', error.error);
        console.error('Request that failed:', JSON.stringify(filter, null, 2));
        this.isLoading = false;
      }
    });
  }

  patientPayCellClass = (context: any) => {
    return this.isBlank(context.dataItem.patientPay)
      ? 'blank-cell'
      : '';
  };
  

  formatAmount(value: number): string {
    if (value == null) {
      return '';
    }
  
    const absValue = Math.abs(value).toFixed(2);
  
    return value < 0 ? `(${absValue})` : absValue;
  }

  isBlank(value: any): boolean {
  return value === null || value === undefined || value === '';
}
  
  loadGrid(): void {
    if (!this.financialSummary) {
      return;
    }
    const firstkRow = {
      monthYear: LABEL_PREVIOUS_PERIOD,
      charges: null,
      insurancePay: null,
      patientPay: null,
      totalPay: null,
      adjustments: null,
      writeOffs: null,
      periodBalance: null,
      endingAR: this.financialSummary.startingAR
    };
    const blankRow = {
      monthYear: '',
      charges: null,
      insurancePay: null,
      patientPay: null,
      totalPay: null,
      adjustments: null,
      writeOffs: null,
      periodBalance: null,
      endingAR: null
    };
  
    const totalRow = {
      monthYear: this.financialSummary.total.monthYear || LABEL_TOTAL,
      charges: this.financialSummary.total.charges,
      insurancePay: this.financialSummary.total.insurancePay,
      patientPay: this.financialSummary.total.patientPay,
      totalPay: this.financialSummary.total.totalPay,
      adjustments: this.financialSummary.total.adjustments,
      writeOffs: this.financialSummary.total.writeOffs,
      periodBalance: this.financialSummary.total.periodBalance,
      endingAR: this.financialSummary.total.endingAR
    };
    this.gridView = {
      data: [
        firstkRow,
        ...this.financialSummary.rows,
        blankRow,
        totalRow
      ],
      total: this.financialSummary.rows.length + 2
    };
  }

  loadFunderGrid(): void {
    if (!this.funderFinancialSummary) {
      return;
    }

    const blankRow = {
      funderName: '',
      priorPeriodBalance: null,
      charges: null,
      insurancePay: null,
      patientPay: null,
      totalPay: null,
      adjustments: null,
      writeOffs: null,
      periodBalance: null,
      totalBalance: null
    };

    const totalRow = {
      funderName: LABEL_TOTAL,
      priorPeriodBalance: this.funderFinancialSummary.total.priorPeriodBalance,
      charges: this.funderFinancialSummary.total.charges,
      insurancePay: this.funderFinancialSummary.total.insurancePay,
      patientPay: this.funderFinancialSummary.total.patientPay,
      totalPay: this.funderFinancialSummary.total.totalPay,
      adjustments: this.funderFinancialSummary.total.adjustments,
      writeOffs: this.funderFinancialSummary.total.writeOffs,
      periodBalance: this.funderFinancialSummary.total.periodBalance,
      totalBalance: this.funderFinancialSummary.total.totalBalance
    };

    this.funderGridView = {
      data: [
        ...this.funderFinancialSummary.rows,
        blankRow,
        totalRow
      ],
      total: this.funderFinancialSummary.rows.length + 2
    };
  }
  
  breadcrumbs: Breadcrumb[] = [
    {
      label: 'Reporting',
      url: '/billing/reporting/list',
      tabIndex: 0,
      isReportsPage: true,
    },
    {
      label: 'Charge Reports',
      url: '/billing/reporting/list',
      tabIndex: 1,
      isReportsPage: true,
    },
    {
      label: 'Financial Summary Report',
      url: '/billing/reporting/financial-summary-report',
      tabIndex: 1,
      isReportsPage: true,
    },
  ];
}
