import {
  Component,
  forwardRef,
  Input,
  ViewChild,
  AfterViewInit,
  ChangeDetectorRef,
} from '@angular/core';
import {
  GridDataResult,
  PageChangeEvent,
  PagerSettings,
  SelectableSettings,
} from '@progress/kendo-angular-grid';
import { map, Observable, Subscription } from 'rxjs';
import { PaymentsAdjustmentsFiltersComponent } from './payments-adjustments-filters/payments-adjustments-filters.component';
import { paymentsAdjustmentsRequestModel } from '@core/models/billing/report-model';
import { ReportService } from '@core/services/billing/report.service';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { PaymentsAdjustmentsListSubject } from '@core/subjects/payments-adjustments.subject';
import { PaginationService } from '@core/services/billing/pagination.service';
import { SortDescriptor, State } from '@progress/kendo-data-query';
import { ListFilterSort } from '@core/models/billing';
import { DatePipe } from '@angular/common';
import { NotificationService } from '@progress/kendo-angular-notification';
import { Helper } from '@app/billing/encounters/common/common-helper';
import { ClaimService } from '@core/services/billing';
import { DialogAction } from '@progress/kendo-angular-dialog';
import { DialogService } from '@progress/kendo-angular-dialog';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { AccountPermissions } from '@core/enums/account/account-permissions';
import { Breadcrumb } from '@core/models/billing/bread-crumb';
import { fromEvent, throttleTime } from 'rxjs';

@Component({
  selector: 'payments-adjustments',
  templateUrl: './payments-adjustments.component.html',
  styleUrls: ['./payments-adjustments.component.css'],
})
export class PaymentsAdjustmentsComponent implements AfterViewInit {
  @ViewChild(forwardRef(() => PaymentsAdjustmentsFiltersComponent))
  filtersComponent: PaymentsAdjustmentsFiltersComponent;
  userList: ClaimFilterOptionModel[] = [];
  @ViewChild('encountersGrid', { static: true }) grid: any;

  subscriptions = new Subscription();
  public isLoading = true;
  public isSubjectLoading$: Observable<boolean>;
  public mySelection: number[] = [];
  public isVirtualMode = false;
   public scrollSkip = 0;
   private windowStart = 0;
  private readonly pageSize = 50;
  private readonly maxWindowSize = 200;

   
    private lastScrollTop: number = 0;
    private isScrollLocked = false;        
    private isProgrammaticScroll = false;
  private virtualScrollPageSize = 50;
  private scrollPrefetchSub: Subscription | null = null;
  private prefetchThreshold = 0.7;
  private scrollDebounceMs = 150;

  filterChangedTimeout: number;
  view: Observable<GridDataResult>;
  isExcelDownloadCalled: boolean = false;
  canEdit = false;
     public isAllSelected: boolean = false;
  paymentsAdjustmentsListSubject: PaymentsAdjustmentsListSubject;
  showFilter: boolean;

  paymentsAdjustmentsRequest: any;
  public dialogForTotalCount: boolean = false;
  public gatClaimsTabData: boolean = false;

  onFilterChanged() {
    if (this.filterChangedTimeout) {
      clearTimeout(this.filterChangedTimeout);
    }

    this.filterChangedTimeout = window.setTimeout(() => 1000);
  }

  clearFilter() {
    if (this.paymentsAdjustmentsListSubject.getCount() > 0) {
      const filter = new paymentsAdjustmentsRequestModel();
      filter.funderId = this.paymentsAdjustmentsRequest.funder ?? null;
      filter.startDate = this.paymentsAdjustmentsRequest.dateFrom;
      filter.endDate = this.paymentsAdjustmentsRequest.dateTo;
      filter.RangeType = this.paymentsAdjustmentsRequest.rangeType;
      filter.take = 0;
      filter.skip = 0;
      this.paymentsAdjustmentsListSubject.totalCount = 0;
      this.paymentsAdjustmentsListSubject.clear();
      //this.paymentsAdjustmentsListSubject.getReport(filter);
    } else {
      this.showNotificationError('No data to clear');
    }
  }

  gridState: ListFilterSort = new ListFilterSort();

  readonly pagingSettings: PagerSettings = {
    buttonCount: 5,
    type: 'numeric',
    pageSizes: true,
    previousNext: true,
  };
  readonly selectableSettings: SelectableSettings = {
    checkboxOnly: true,
    enabled: true,
    mode: 'multiple',
  };

  ngAfterViewInit() {
    this.filtersComponent.OnInitclearFilters();
    this.toggleFilter(true);

    if (this.grid) {
      this.grid.autoFitColumns(this.grid.columns.toArray(), 'header');
    }

    setTimeout(() => this.setupScrollListener(), 0);
  }

  totalCount: number;

  gridPageSizes: any;
  constructor(
    private reportingService: ReportService,
    private datePipe: DatePipe,
    private paginationService: PaginationService,
    private cdr: ChangeDetectorRef,
    private claimsService: ClaimService,
    private notificationService: NotificationService,
    private dialogService: DialogService,
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

    this.paymentsAdjustmentsListSubject = new PaymentsAdjustmentsListSubject(
      this.reportingService
    );
    this.view = this.paymentsAdjustmentsListSubject.pipe(
      map((data) => {
        let result = {
          data: data,
          total: this.paymentsAdjustmentsListSubject.getCount(),
        };
        this.totalCount = result.total;

        this.isLoading = false;
        this.isExcelDownloadCalled = false;
        return result;
      })
    );

    this.getGridPageSizes();
  }

  ngOnInit() {
    this.subscriptions.add(
      this.accountService.accountMemberSettings.subscribe((x) => {
        if (x) {
          this.canEdit = this.accountService.checkPermissionLevel(
            AccountPermissions.BillingEdit
          );
        }
      })
    );
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
  }

  toggleFilter(event: boolean) {
    this.filtersComponent.opened = event;
    this.showFilter = event;
    this.cdr.detectChanges();
  }

  loadPatientHeaders() {
    this.isLoading = true;
    const filter = new paymentsAdjustmentsRequestModel();
    filter.funderId = this.paymentsAdjustmentsRequest.funder ?? null;
    filter.startDate = Helper.shiftDateToUTC(
      this.paymentsAdjustmentsRequest.dateFrom
    );
    filter.endDate = Helper.shiftDateToUTC(
      this.paymentsAdjustmentsRequest.dateTo
    );
    filter.RangeType = this.paymentsAdjustmentsRequest.rangeType;

    filter.take = this.isVirtualMode
      ? this.virtualScrollPageSize
      : this.gridState.take || 0;

    filter.skip = this.isVirtualMode ? 0 : this.gridState.skip || 0;
    filter.sortingModels = this.gridState.sortingModels;
    this.mySelection = [];

    this.paymentsAdjustmentsListSubject.getReport(filter);
  }

  onSortChange(sortParams: SortDescriptor[]): void {
    this.scrollToTop();
    this.gridState.sortingModels = sortParams;
    this.loadData(this.gridState);
  }

  private scrollToTop(): void {
    const gridEl = this.getGridScrollElement();
    if (!gridEl) return;
    gridEl.scrollTo({ top: 0, behavior: 'smooth' });
  }

  loadData(params: ListFilterSort): void {
    if (this.paymentsAdjustmentsRequest != undefined) {
      this.isLoading = true;  
      const filter = new paymentsAdjustmentsRequestModel();
      filter.funderId = this.paymentsAdjustmentsRequest.funder ?? null;
      filter.startDate = Helper.shiftDateToUTC(
        this.paymentsAdjustmentsRequest.dateFrom
      );
      filter.endDate = Helper.shiftDateToUTC(
        this.paymentsAdjustmentsRequest.dateTo
      );
      filter.RangeType = this.paymentsAdjustmentsRequest.rangeType;
      filter.sortingModels = params.sortingModels;
      if (this.isVirtualMode) {
        filter.take = this.virtualScrollPageSize;
        filter.skip = 0;
      } else {
        filter.take = this.gridState.take || 0;
        filter.skip = this.gridState.skip || 0;
      }
      this.paymentsAdjustmentsRequest.sortingModels = filter.sortingModels;

      this.paymentsAdjustmentsListSubject.getReport(filter);
    }
  }

  onPageChange(event: PageChangeEvent): void {
    this.dialogForTotalCount = false;

    // ALL selected → Virtual Mode
    if (event.skip === 0 && event.take === 0) {
        this.isVirtualMode = true;
       
       this.paymentsAdjustmentsListSubject.setVirtualMode(true);
        this.windowStart = 0;
        this.gridState.skip = 0;
        this.gridState.take = 9999999;

      this.paginationService.setPageSizes(0);
      this.resetGridScroll();
      
      this.paymentsAdjustmentsListSubject.clear();
      this.paymentsAdjustmentsListSubject.setCount(0);
       this.isLoading = true;
      this.paymentsAdjustmentsListSubject.getReport(
        this.buildVirtualFilter(0)
      );
      this.isAllSelected= true;
      return;
    }
      this.isAllSelected= false;

    // Normal pagination
    this.isVirtualMode = false;
    this.gridState.skip = event.skip;
    this.gridState.take = event.take;

    this.resetGridScroll();
    this.loadPatientHeaders();
  }

  SubmitPageCount() {
    const confirmDialog = this.dialogService.open({
      title: '⚠️ Please confirm',
      width: 500,
      content:
        'There are more than 1000 records to display. This may take more time to process. You can either proceed or apply a filter to narrow down the results',
      actions: [{ text: 'Cancel' }, { text: 'Yes', primary: true }],
    });
    return confirmDialog;
  }

  applyFilter(event) {
    if (event == undefined) {
      this.showNotificationError('Please select filters');
    } else {
      this.isLoading = true;
      const filter = new paymentsAdjustmentsRequestModel();
      filter.funderId = event.funder;
      filter.startDate = Helper.shiftDateToUTC(event.dateFrom);
      filter.endDate = Helper.shiftDateToUTC(event.dateTo);
      filter.RangeType = event.rangeType;
      filter.take = this.gridState.take || 0;
      filter.skip = event.skip;
      this.paymentsAdjustmentsRequest = event;
      this.paymentsAdjustmentsListSubject.getReport(filter);
    }
  }
  exportToExcel(): void {
    if (this.paymentsAdjustmentsListSubject.getCount() > 0) {
      const filter = new paymentsAdjustmentsRequestModel();
      filter.funderId = this.paymentsAdjustmentsRequest.funder;
      filter.startDate = Helper.shiftDateToUTC(
        this.paymentsAdjustmentsRequest.dateFrom
      );
      filter.endDate = Helper.shiftDateToUTC(
        this.paymentsAdjustmentsRequest.dateTo
      );
      filter.RangeType = this.paymentsAdjustmentsRequest.rangeType;
      filter.sortingModels = this.paymentsAdjustmentsRequest.sortingModels;
      this.reportingService
        .paymentsAdjustmentsExportToExcel(filter)
        .subscribe((response: any) => {
          const base64Data = response.data;
          const byteArray = this.base64ToBlob(base64Data);
          const url = window.URL.createObjectURL(byteArray);

          const a = document.createElement('a');
          a.href = url;
          a.download = 'Payments Adjustments report.xlsx'; // File name for the download
          document.body.appendChild(a);
          a.click();
          window.URL.revokeObjectURL(url);
          a.remove();
        });
    }
  }
  showNotificationError(content: string) {
    this.notificationService.show({
      content,
      animation: { type: 'fade', duration: 500 },
      position: { horizontal: 'center', vertical: 'top' },
      type: { style: 'error', icon: false },
      closable: false,
    });
  }

  base64ToBlob(base64: string): Blob {
    const byteCharacters = atob(base64);
    const byteArrays = [];
    for (let offSet = 0; offSet < byteCharacters.length; offSet += 1024) {
      const slice = byteCharacters.slice(
        offSet,
        Math.min(offSet + 1024, byteCharacters.length)
      );
      const byteNumbers = new Array(slice.length);
      for (let i = 0; i < slice.length; i++) {
        byteNumbers[i] = slice.charCodeAt(i);
      }
      const byteArray = new Uint8Array(byteNumbers);
      byteArrays.push(byteArray);
    }
    return new Blob(byteArrays, {
      type: 'application/vnd.openxlmformats-officedocument.spreadsheetml.sheet',
    });
  }

  getPageStart(total: number): number {
    if (!total) return 0;
    // When virtual/all mode is active, start is based on current window or 1
    if (this.isVirtualMode) {
      const subjAny = this.paymentsAdjustmentsListSubject as any;
      const windowStartFn = subjAny.getWindowStart;
      const windowStart =
        typeof windowStartFn === 'function' ? windowStartFn.call(subjAny) : 0;
      return Math.min(windowStart + 1, total);
    }
    const skip = this.gridState?.skip || 0;
    return Math.min(skip + 1, total);
  }

  getPageEnd(total: number): number {
    if (!total) return 0;
    if (this.isVirtualMode) {
      const subjAny = this.paymentsAdjustmentsListSubject as any;
      const dataLengthFn = subjAny.getDataLength;
      const loaded =
        typeof dataLengthFn === 'function' ? dataLengthFn.call(subjAny) : 0;
      return Math.min(loaded, total);
    }
    const skip = this.gridState?.skip || 0;
    const take = this.gridState?.take || 0;
    if (take === 0) return total;
    return Math.min(skip + take, total);
  }

  getGridPageSizes(): void {
    const storedGridPageSizes = localStorage.getItem('gridPageSizes')
      ? JSON.parse(localStorage.getItem('gridPageSizes') || '')
      : null;
    if (storedGridPageSizes) {
      this.gridPageSizes = storedGridPageSizes;
    } else {
      this.claimsService
        .getGridPageSizes()
        .subscribe((sizes: Array<number | { text: string; value: number }>) => {
          this.gridPageSizes = sizes;
        });
    }
  }

   private setupScrollListener(): void {
    const gridEl =
      this.grid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    if (!gridEl) return;

    this.scrollPrefetchSub = fromEvent(gridEl, 'scroll')
      .pipe(throttleTime(this.scrollDebounceMs))
      .subscribe(() => {
      
      if (this.paymentsAdjustmentsListSubject.getVirtualLoadingValue()) return;

      const gridEl = this.getGridScrollElement();
      if (!gridEl) return;

      const scrollTop = gridEl.scrollTop;
      const clientHeight = gridEl.clientHeight;
      const scrollHeight = gridEl.scrollHeight;

      const direction = scrollTop > this.lastScrollTop ? 'down' : 'up';
      this.lastScrollTop = scrollTop;

      const ratio = (scrollTop + clientHeight) / scrollHeight;

      if (ratio >= 0.8 && direction === 'down') {
        this.handleVirtualScrollLoadDown();
      } else if (ratio <= 0.2 && direction === 'up') {
        this.handleVirtualScrollLoadUp();
      }
    });
  }

     getFooterRange(): string {
        if (!this.isAllSelected) {
            return '0';
        }
        const currentLength = this.paymentsAdjustmentsListSubject.getDataLength();
        const total = this.paymentsAdjustmentsListSubject.getCount();

        if (currentLength === 0) {
            return '0';
        }
      const loadedData=this.paymentsAdjustmentsListSubject.getLoadedPages().size * this.virtualScrollPageSize;
        const _start=loadedData-200;
        const start =(_start< 0 ? 0:_start) + 1;
        const end =  Math.min(loadedData,total);

        return `${start}–${end > 0 ? end : this.pageSize}`;
    }

  private getGridScrollElement(): HTMLElement | null {
    return this.grid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
  }

   private resetGridScroll(): void {
    const gridEl = this.getGridScrollElement();
    if (!gridEl) return;
   
  gridEl.scrollTo({
  top: Math.max(0, (gridEl.scrollHeight - gridEl.clientHeight) / 2),
  behavior: 'smooth'
});
  }

    private handleVirtualScrollLoadDown(): void {
  if (!this.isVirtualMode) return;
  if (this.paymentsAdjustmentsListSubject.getVirtualLoadingValue()) return;

  const _loadedData=this.paymentsAdjustmentsListSubject.getLoadedPages().size * this.virtualScrollPageSize;
  if(_loadedData >=this.paymentsAdjustmentsListSubject.getCount()) return;

  const nextStart = this.windowStart + this.pageSize;
  const total = this.paymentsAdjustmentsListSubject.getCount();
  if (nextStart >= total) return;
  
  const loadedData=this.paymentsAdjustmentsListSubject.getLoadedPages().size * this.virtualScrollPageSize;
  this.windowStart = (loadedData - this.maxWindowSize) >= 0 ? (loadedData - this.maxWindowSize) : 0;

  this.paymentsAdjustmentsListSubject.append(
    this.buildVirtualFilter(loadedData)
  );

  this.recenterAfterLoad();
}

private handleVirtualScrollLoadUp(): void {
  if (!this.isVirtualMode) return;
  if (this.paymentsAdjustmentsListSubject.getVirtualLoadingValue()) return;

  const _loadedData=this.paymentsAdjustmentsListSubject.getLoadedPages().size * this.virtualScrollPageSize;
  if(_loadedData==150){
     return;
  }
  const loadedData=this.paymentsAdjustmentsListSubject.getLoadedPages().size * this.virtualScrollPageSize;
  const prevStart = Math.max(0,loadedData - this.pageSize);
  this.windowStart = prevStart;
  this.paymentsAdjustmentsListSubject.prepend(
    this.buildVirtualFilter(prevStart)
  );

  this.recenterAfterLoad();
}


private recenterAfterLoad(): void {
  const gridEl = this.getGridScrollElement();
  if (!gridEl) return;

  this.isProgrammaticScroll = true;

  requestAnimationFrame(() => {
    const maxScrollTop =
      gridEl.scrollHeight - gridEl.clientHeight;

    if (maxScrollTop > 0) {
      gridEl.scrollTop = Math.floor(maxScrollTop / 2);
    }
    this.isProgrammaticScroll = false;
    this.unlockScroll();
  });
}

  private buildVirtualFilter(skip: number): paymentsAdjustmentsRequestModel {

  const filter = new paymentsAdjustmentsRequestModel();
  filter.funderId = this.paymentsAdjustmentsRequest.funder ?? null;
  filter.endDate = this.paymentsAdjustmentsRequest.dateTo;
  filter.RangeType = this.paymentsAdjustmentsRequest.rangeType;
  filter.startDate = Helper.shiftDateToUTC(this.paymentsAdjustmentsRequest.dateFrom);
  filter.sortingModels = this.paymentsAdjustmentsRequest.sortingModels || [];
  filter.skip =  skip;
  filter.take = this.virtualScrollPageSize;
  this.scrollSkip = filter.skip;
  return filter;
}

private unlockScroll(): void {
  const gridEl = this.getGridScrollElement();
  if (!gridEl) return;
  this.isScrollLocked = false;
  gridEl.style.overflowY = 'auto';
}


  private loadMoreOnScroll(init = false): void {
    const loaded = this.paymentsAdjustmentsListSubject.getDataLength();
    const total = this.paymentsAdjustmentsListSubject.getCount();

    if (loaded >= total) return;

    const filter = new paymentsAdjustmentsRequestModel();
    filter.funderId = this.paymentsAdjustmentsRequest.funder ?? null;
    filter.startDate = Helper.shiftDateToUTC(
      this.paymentsAdjustmentsRequest.dateFrom
    );
    filter.endDate = Helper.shiftDateToUTC(
      this.paymentsAdjustmentsRequest.dateTo
    );
    filter.RangeType = this.paymentsAdjustmentsRequest.rangeType;
    filter.sortingModels = this.paymentsAdjustmentsRequest.sortingModels || [];

    filter.skip = init ? 0 : loaded;
    filter.take = this.virtualScrollPageSize; // 50

    if (init) {
      this.paymentsAdjustmentsListSubject.getReport(filter);
    } else {
      this.paymentsAdjustmentsListSubject.append(filter);
    }
  }



  breadcrumbs: Breadcrumb[] = [
    {
      label: 'Reporting',
      url: '/billing/reporting/list',
      tabIndex: 0,
      isReportsPage: true,
    },
    {
      label: 'Claim Reports',
      url: '/billing/reporting/list',
      tabIndex: 0,
      isReportsPage: true,
    },
    {
      label: 'Payment & Adjustment Activity',
      url: '/billing/reporting/payment-adjustments',
      tabIndex: 0,
      isReportsPage: true,
    },
  ];
}
