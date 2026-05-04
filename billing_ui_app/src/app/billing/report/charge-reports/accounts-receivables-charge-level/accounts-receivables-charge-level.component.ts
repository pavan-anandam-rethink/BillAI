import {
  Component,
  forwardRef,
  Input,
  ViewChild,
  OnInit,
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
import { AccountsReceivablesChargeLevelRequestModel } from '@core/models/billing/report-model';
import { ReportService } from '@core/services/billing/report.service';
import { SortDescriptor, State } from '@progress/kendo-data-query';
import { PaginationService } from '@core/services/billing/pagination.service';
import { NotificationService } from '@progress/kendo-angular-notification';
import { Helper } from '@app/billing/encounters/common/common-helper';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ListFilterSort } from '@core/models/billing';
import { DatePipe } from '@angular/common';
import { AccountsReceivablesChargeLevelSubject } from '@core/subjects/accounts-receivables-charge-level.subject';
import { AccountsReceivablesFiltersComponent } from '../../claim-reports/accounts-receivables/accounts-receivables-filters/accounts-receivables-filters.component';
import { ClaimService } from '@core/services/billing';
import { DialogAction } from '@progress/kendo-angular-dialog';
import { DialogService } from '@progress/kendo-angular-dialog';
import { AccountPermissions } from '@core/enums/account/account-permissions';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { Breadcrumb } from '@core/models/billing/bread-crumb';
import { fromEvent, throttleTime } from 'rxjs';

export enum InvoiceListingTab {
  CreateInvoice = 1,
  PendingCollection,
  ClientHistory,
}

export interface IndexResult {
  index: number;
  anchor: HTMLAnchorElement;
  clientId: number;
}

@Component({
  selector: 'accounts-receivables-charge-level',
  templateUrl: './accounts-receivables-charge-level.component.html',
  styleUrls: ['./accounts-receivables-charge-level.component.css'],
})
export class AccountsReceivablesChargeLevelComponent {
  @ViewChild(forwardRef(() => AccountsReceivablesFiltersComponent))
  filtersComponent: AccountsReceivablesFiltersComponent;
  @ViewChild('encountersGrid', { static: true }) grid: any;
  userList: ClaimFilterOptionModel[] = [];
  view: Observable<GridDataResult>;
  accountsReceivablesChargeLevelSubject: AccountsReceivablesChargeLevelSubject;

  subscriptions = new Subscription();
  accountsReceivablesRequest: any;
  gridState: ListFilterSort = new ListFilterSort();
  filterChangedTimeout: number;
  public mySelection: number[] = [];
  showFilter: boolean;
  canEdit = false;
  public isLoading = false;
   public isAllSelected: boolean = false;
  gridPageSizes: any;
  public dialogForTotalCount: boolean = false;
  public gatClaimsTabData: boolean = false;
  public isVirtualMode = false;
   public scrollSkip = 0;

   // Sliding window state
   private windowStart = 0;
  private readonly pageSize = 50;
  private readonly maxWindowSize = 200;

   
    private lastScrollTop: number = 0;
    private isScrollLocked = false;        // hard lock
    private isProgrammaticScroll = false;
  private virtualScrollPageSize = 50;
  private prefetchThreshold = 0.7;
  private scrollDebounceMs = 150;
  private scrollPrefetchSub: Subscription | null = null;

  constructor(
    private datePipe: DatePipe,
    private reportingService: ReportService,
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

    this.accountsReceivablesChargeLevelSubject =
      new AccountsReceivablesChargeLevelSubject(this.reportingService);
   

    this.view = this.accountsReceivablesChargeLevelSubject.pipe(
      map((data) => ({
        data,
        total: this.accountsReceivablesChargeLevelSubject.getCount(),
      }))
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

  applyFilter(event: any) {
    if (!event) {
      this.showNotificationError('Please select filters');
      return;
    } else {
      const filter = new AccountsReceivablesChargeLevelRequestModel();
      filter.closingDate = Helper.shiftDateToUTC(event.date);
      filter.payerOrFunder = event.funder;
      filter.take = this.gridState.take || 0;
      filter.skip = this.gridState.skip || 0;
      this.accountsReceivablesRequest = filter;
      this.accountsReceivablesChargeLevelSubject.getReport(filter);
    }
  }

  onFilterChanged() {
    if (this.filterChangedTimeout) {
      clearTimeout(this.filterChangedTimeout);
    }

    this.filterChangedTimeout = window.setTimeout(() => 1000);
  }

  clearFilter() {
    this.isLoading = false;
    if (this.accountsReceivablesChargeLevelSubject.getCount()) {
      this.accountsReceivablesChargeLevelSubject.setCount(0);

      this.accountsReceivablesChargeLevelSubject.clear();
    } else {
      this.showNotificationError('No data to clear');
    }
  }

  onSortChange(sortParams: SortDescriptor[]): void {
    this.scrollToTop();
    this.gridState.sortingModels = sortParams;

  if (this.accountsReceivablesRequest) {
    this.accountsReceivablesRequest.sortingModels = sortParams;
  }
   
    this.loadPatientHeaders();
  }

  private scrollToTop(): void {
    const gridEl = this.getGridScrollElement();
    if (!gridEl) return;
    gridEl.scrollTo({ top: 0, behavior: 'smooth' });
  }

  ngAfterViewInit() {
    this.filtersComponent.OnInitClearFilters();
    this.toggleFilter(true);
    setTimeout(() => this.setupScrollListener(), 0);
  }

  private setupScrollListener(): void {
    const gridEl =
      this.grid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    if (!gridEl) return;

    this.scrollPrefetchSub = fromEvent(gridEl, 'scroll')
      .pipe(throttleTime(this.scrollDebounceMs))
      .subscribe(() => {
      if (this.isScrollLocked || this.isProgrammaticScroll) return;
      if (!this.isVirtualMode) return;
      if (this.accountsReceivablesChargeLevelSubject.getVirtualLoadingValue()) return;

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
        const currentLength = this.accountsReceivablesChargeLevelSubject.getDataLength();
        const total = this.accountsReceivablesChargeLevelSubject.getCount();

        if (currentLength === 0) {
            return '0';
        }
      const loadedData=this.accountsReceivablesChargeLevelSubject.getLoadedPages().size * this.virtualScrollPageSize;
        const _start=loadedData-200;
        const start =(_start< 0 ? 0:_start) + 1;
        const end =  Math.min(loadedData,total);
        return `${start}–${end > 0 ? end : this.pageSize}`;
      
  }

  

  onPageChange(event: PageChangeEvent): void {
    if (event.skip === 0 && event.take === 0) {
      this.isVirtualMode = true;
      this.accountsReceivablesChargeLevelSubject.setVirtualMode(true);
      this.windowStart = 0;
      this.gridState.skip = 0;
      this.gridState.take = 9999999;
      this.paginationService.setPageSizes(0);
      this.resetGridScroll();
      this.accountsReceivablesChargeLevelSubject.clear();
      this.accountsReceivablesChargeLevelSubject.setCount(0);

        this.accountsReceivablesChargeLevelSubject.getReport(
          this.buildVirtualFilter(0)
    );
      this.isAllSelected= true;
      return;
    }
      
      this.isAllSelected= false; 
    // normal pagination
    this.isVirtualMode = false;
    this.gridState.skip = event.skip;
    this.gridState.take = event.take;
    this.loadPatientHeaders();
  }

  private loadMoreOnScroll(init = false): void {
    const loaded = this.accountsReceivablesChargeLevelSubject.getDataLength();
    const total = this.accountsReceivablesChargeLevelSubject.getCount();
    if (loaded >= total) return;

    const filter = new AccountsReceivablesChargeLevelRequestModel();
    filter.payerOrFunder = this.accountsReceivablesRequest.payerOrFunder;
    filter.closingDate = this.accountsReceivablesRequest.closingDate;
    filter.sortingModels = this.accountsReceivablesRequest.sortingModels || [];
    filter.skip = init ? 0 : loaded;
    filter.take = this.virtualScrollPageSize;

    if (init) {
      this.accountsReceivablesChargeLevelSubject.getReport(filter);
    } else {
      this.accountsReceivablesChargeLevelSubject.append(filter);
    }
  }

  
    private handleVirtualScrollLoadDown(): void {
           if (!this.isVirtualMode) return;
          if (this.accountsReceivablesChargeLevelSubject.getVirtualLoadingValue()) return;
  
    const _loadedData=this.accountsReceivablesChargeLevelSubject.getLoadedPages().size * this.virtualScrollPageSize;
    if(_loadedData >=this.accountsReceivablesChargeLevelSubject.getCount()) return;
  
    const nextStart = this.windowStart + this.pageSize;
    const total = this.accountsReceivablesChargeLevelSubject.getCount();
    if (nextStart >= total) return;
    const loadedData=this.accountsReceivablesChargeLevelSubject.getLoadedPages().size * this.virtualScrollPageSize;
    this.windowStart = (loadedData - this.maxWindowSize) >= 0 ? (loadedData - this.maxWindowSize) : 0;
  
    this.accountsReceivablesChargeLevelSubject.append(
      this.buildVirtualFilter(loadedData)
    );
  
    this.recenterAfterLoad();
  }
  
  
  
  
  
  private handleVirtualScrollLoadUp(): void {
    if (!this.isVirtualMode) return;
  if (this.accountsReceivablesChargeLevelSubject.getVirtualLoadingValue()) return;

  const loadedData =
    this.accountsReceivablesChargeLevelSubject.getLoadedPages().size *
    this.virtualScrollPageSize;
  if(loadedData==150){
     return;
  }
    const prevStart = Math.max(0, loadedData - this.pageSize);
  this.windowStart = prevStart;

  this.accountsReceivablesChargeLevelSubject.prepend(
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
  
      // Allow scroll again
      this.isProgrammaticScroll = false;
      this.unlockScroll();
    });
  }
  
  private buildVirtualFilter(skip: number): AccountsReceivablesChargeLevelRequestModel {
  
    const filter = new AccountsReceivablesChargeLevelRequestModel();
    filter.payerOrFunder = this.accountsReceivablesRequest.payerOrFunder;
    filter.closingDate = Helper.shiftDateToUTC(this.accountsReceivablesRequest.closingDate);
    filter.sortingModels = this.accountsReceivablesRequest.sortingModels || [];
    filter.skip =  skip;
    filter.take = this.pageSize;

   this.scrollSkip = filter.skip;
    return filter;
  }
  
  private unlockScroll(): void {
    const gridEl = this.getGridScrollElement();
    if (!gridEl) return;
  
    this.isScrollLocked = false;
    gridEl.style.overflowY = 'auto';
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

    loadPatientHeaders() {
    const filter = new AccountsReceivablesChargeLevelRequestModel();
    filter.payerOrFunder = this.accountsReceivablesRequest.payerOrFunder;
    filter.closingDate = Helper.shiftDateToUTC(
      this.accountsReceivablesRequest.closingDate
    );
    filter.sortingModels = this.accountsReceivablesRequest.sortingModels;
    filter.take = this.gridState.take || 0;
    filter.skip = this.gridState.skip || 0;

    this.mySelection = [];
    this.accountsReceivablesChargeLevelSubject.getReport(filter);
  }

  loadData(params: ListFilterSort): void {
    if (this.accountsReceivablesRequest != undefined) {
      const filter = new AccountsReceivablesChargeLevelRequestModel();
      filter.payerOrFunder = this.accountsReceivablesRequest.payerOrFunder;
      filter.closingDate = Helper.shiftDateToUTC(
        this.accountsReceivablesRequest.closingDate
      );
      filter.sortingModels = params.sortingModels;
      filter.take = this.gridState.take || 0;
      filter.skip = this.gridState.skip || 0;
      this.accountsReceivablesRequest.sortingModels = params.sortingModels;
    }
  }

  toggleFilter(event: boolean) {
    this.filtersComponent.opened = event;
    this.showFilter = event;
    this.cdr.detectChanges();
  }

  exportToExcel(): void {
    if (this.accountsReceivablesChargeLevelSubject.getCount() > 0) {
      const filter = new AccountsReceivablesChargeLevelRequestModel();
      filter.closingDate = Helper.shiftDateToUTC(
        this.accountsReceivablesRequest.closingDate
      );
      filter.payerOrFunder = this.accountsReceivablesRequest.payerOrFunder;
      filter.sortingModels = this.accountsReceivablesRequest.sortingModels;
      filter.take = this.gridState.take || 0;
      filter.skip = this.gridState.skip || 0;

      this.reportingService
        .accountsReceivablesChargeLevelExportToExcel(filter)
        .subscribe((response: any) => {
          const byteArray = this.base64ToBlob(response.data);
          const url = window.URL.createObjectURL(byteArray);
          const a = document.createElement('a');
          a.href = url;
          a.download = 'Account Receivables Charge Level.xlsx';
          document.body.appendChild(a);
          a.click();
          window.URL.revokeObjectURL(url);
          a.remove();
          this.isLoading = false;
        });
    }
  }

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
    if (this.isVirtualMode) {
      const subjAny = this.accountsReceivablesChargeLevelSubject as any;
      const windowStartFn = subjAny.getWindowStart;
      const windowStart = typeof windowStartFn === 'function' ? windowStartFn.call(subjAny) : 0;
      return Math.min(windowStart + 1, total);
    }
    const skip = this.gridState?.skip || 0;
    return Math.min(skip + 1, total);
  }

  getPageEnd(total: number): number {
    if (!total) return 0;
    if (this.isVirtualMode) {
      const subjAny = this.accountsReceivablesChargeLevelSubject as any;
      const dataLengthFn = subjAny.getDataLength;
      const loaded = typeof dataLengthFn === 'function' ? dataLengthFn.call(subjAny) : 0;
      return Math.min(loaded, total);
    }
    const skip = this.gridState?.skip || 0;
    const take = this.gridState?.take || 0;
    if (take === 0) return total;
    return Math.min(skip + take, total);
  }

  private showNotificationError(content: string) {
    this.notificationService.show({
      content,
      animation: { type: 'fade', duration: 500 },
      position: { horizontal: 'center', vertical: 'top' },
      type: { style: 'error', icon: false },
      closable: false,
    });
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
      label: 'Accounts Receivable - Charge Level',
      url: '/billing/reporting/accounts-receivables-charge-level',
      tabIndex: 1,
      isReportsPage: true,
    },
  ];
}
