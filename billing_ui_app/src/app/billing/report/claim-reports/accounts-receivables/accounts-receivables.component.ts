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
import {
  map,
  Observable,
  of,
  Subscription,
  filter,
  fromEvent,
  throttleTime,
} from 'rxjs';
import { AccountsReceivablesFiltersComponent } from './accounts-receivables-filters/accounts-receivables-filters.component';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { AccountsReceivablesRequestModel } from '@core/models/billing/report-model';
import { ReportService } from '@core/services/billing/report.service';
import { AccountsReceivablesListSubject } from '@core/subjects/accounts-receivables.subject';
import { SortDescriptor, State } from '@progress/kendo-data-query';
import { PaginationService } from '@core/services/billing/pagination.service';
import { ListFilterSort } from '@core/models/billing';
import { DatePipe } from '@angular/common';
import { NotificationService } from '@progress/kendo-angular-notification';
import { Helper } from '@app/billing/encounters/common/common-helper';
import { ClaimService } from '@core/services/billing';
import { DialogService } from '@progress/kendo-angular-dialog';
import { AccountPermissions } from '@core/enums/account/account-permissions';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { Breadcrumb } from '@core/models/billing/bread-crumb';

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
  selector: 'accounts-receivables',
  templateUrl: './accounts-receivables.component.html',
  styleUrls: ['./accounts-receivables.component.css'],
})
export class AccountsReceivablesComponent {
  @ViewChild(forwardRef(() => AccountsReceivablesFiltersComponent))
  filtersComponent: AccountsReceivablesFiltersComponent;
  @ViewChild('encountersGrid', { static: true }) grid: any;
  userList: ClaimFilterOptionModel[] = [];
  view: Observable<GridDataResult>;
  accountsReceivablesListSubject: AccountsReceivablesListSubject;

  subscriptions = new Subscription();
  accountsReceivablesRequest: any;
  gridState: ListFilterSort = new ListFilterSort();
  gridPageSizes: any;
  canEdit = false;
  public isAllSelected: boolean = false;
  public prefetchThresholdprefetchThreshold: boolean = false;
  private virtualScrollPageSize: number = 50;
  private scrollPrefetchSub: Subscription | null = null;
  private prefetchThreshold = 0.7; 
  private scrollDebounceMs = 150;
  public isVirtualMode = false;
  public scrollSkip = 0;
   private windowStart = 0;
  private readonly pageSize = 50;
  private readonly maxWindowSize = 200;
    private lastScrollTop: number = 0;
    private isScrollLocked = false;
    private isProgrammaticScroll = false; 
  constructor(private datePipe: DatePipe,
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

    this.accountsReceivablesListSubject = new AccountsReceivablesListSubject(
      this.reportingService
    );
    this.isLoading = true;
    this.isSubjectLoading$ = this.accountsReceivablesListSubject.getLoading();
    this.view = this.accountsReceivablesListSubject.asObservable().pipe(
      map((data) => ({
        data,
        total: this.accountsReceivablesListSubject.getCount(),
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

  clearFilter() {
    this.isLoading = false;
    if (this.accountsReceivablesListSubject.getCount()) {
      this.accountsReceivablesListSubject.setCount(0);
      this.accountsReceivablesListSubject.clear();
    } else {
      this.showNotificationError('No data to clear');
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

  filterChangedTimeout: number;
  public isLoading = true;
  public isSubjectLoading$: Observable<boolean>;
  public mySelection: number[] = [];
  showFilter: boolean;
  public dialogForTotalCount: boolean = false;
  public gatClaimsTabData: boolean = false;

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

  ngAfterViewInit() {
    this.filtersComponent.OnInitClearFilters();
    this.toggleFilter(true);
    if (this.grid) {
      this.grid.autoFitColumns(this.grid.columns.toArray(), 'header');
    }
    setTimeout(() => this.setupScrollListener(), 0);
  }
  private setupScrollListener(): void {
    const gridEl =
      this.grid?.wrapper?.nativeElement?.querySelector('.k-grid-content');

    if (!gridEl) return;

    this.scrollPrefetchSub = fromEvent(gridEl, 'scroll')
      .pipe(throttleTime(this.scrollDebounceMs))
      .subscribe(() => {
      if (this.accountsReceivablesListSubject.getVirtualLoadingValue()) return;

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
        const currentLength = this.accountsReceivablesListSubject.getDataLength();
        const total = this.accountsReceivablesListSubject.getCount();

        if (currentLength === 0) {
            return '0';
        }
      const loadedData=this.accountsReceivablesListSubject.getLoadedPages().size * this.virtualScrollPageSize;
        const _start=loadedData-200;
        const start =(_start< 0 ? 0:_start) + 1;
        const end =  Math.min(loadedData,total);

        return `${start}–${end > 0 ? end : this.pageSize}`;
    }


  loadData(params: ListFilterSort): void {
    if (!this.accountsReceivablesRequest) {
      return;
    }

    const filter = new AccountsReceivablesRequestModel();
    filter.payerOrFunder = this.accountsReceivablesRequest.funder;
    filter.closingDate = Helper.shiftDateToUTC(
      this.accountsReceivablesRequest.date
    );
    filter.sortingModels = params.sortingModels;
    if (this.isVirtualMode) {
      filter.take = this.virtualScrollPageSize;
      filter.skip = 0;
    } else {
      filter.take = this.gridState.take || 0;
      filter.skip = this.gridState.skip || 0;
    }
    // Keep request state in sync
    this.accountsReceivablesRequest.sortingModels = params.sortingModels;

    this.accountsReceivablesListSubject.getReport(filter);
  }

  toggleFilter(event: boolean) {
    this.filtersComponent.opened = event;
    this.showFilter = event;
    this.cdr.detectChanges();
  }
  onFilterChanged() {
    if (this.filterChangedTimeout) {
      clearTimeout(this.filterChangedTimeout);
    }

    this.filterChangedTimeout = window.setTimeout(() => 1000);
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

  onPageChange(event: PageChangeEvent): void {
    this.dialogForTotalCount = false;
     if (event.skip === 0 && event.take === 0) {
        this.isVirtualMode = true;
       
       this.accountsReceivablesListSubject.setVirtualMode(true);
        this.windowStart = 0;
        this.gridState.skip = 0;
        this.gridState.take = 9999999;

      this.paginationService.setPageSizes(0);
      this.resetGridScroll();
      
      this.accountsReceivablesListSubject.clear();
      this.accountsReceivablesListSubject.setCount(0);
       this.isLoading = true;
      this.accountsReceivablesListSubject.getReport(
        this.buildVirtualFilter(0)
      );
      this.isLoading = false;
      this.isAllSelected= true;
      return;
    }
     this.isAllSelected= false;
    this.isVirtualMode = false;
    this.gridState.skip = event.skip;
    this.gridState.take = event.take;

    this.resetGridScroll();
    this.loadPatientHeaders();
  }

  public getPageStart(total: number): number {
    if (!total || total === 0) return 0;
    if (this.isVirtualMode) return 1;
    const skip =
      this.gridState && this.gridState.skip ? this.gridState.skip : 0;
    return skip + 1;
  }

  public getPageEnd(total: number): number {
    if (!total || total === 0) return 0;
    if (this.isVirtualMode)
      return this.accountsReceivablesListSubject.getDataLength();
    const skip =
      this.gridState && this.gridState.skip ? this.gridState.skip : 0;
    const take =
      this.gridState && this.gridState.take ? this.gridState.take : 20;
    return Math.min(skip + take, total);
  }

  loadPatientHeaders() {
    const filter = new AccountsReceivablesRequestModel();
    filter.payerOrFunder = this.accountsReceivablesRequest.funder;
    filter.closingDate = Helper.shiftDateToUTC(
      this.accountsReceivablesRequest.date
    );
    filter.sortingModels = this.accountsReceivablesRequest.sortingModels;
    filter.take = this.isVirtualMode
      ? this.virtualScrollPageSize
      : this.gridState.take || 0;
    filter.skip = this.gridState.skip || 0;

    this.mySelection = [];

    this.accountsReceivablesListSubject.getReport(filter);
  }
  applyFilter(event) {
    this.resetGridScroll();
    if (event == undefined) {
      this.showNotificationError('Please select filters');
    } else {
      const filter = new AccountsReceivablesRequestModel();
      filter.closingDate = Helper.shiftDateToUTC(event.date);
      filter.payerOrFunder = event.funder;
      if (this.isVirtualMode) {
        filter.take = this.virtualScrollPageSize;
      } else {
        filter.take = this.gridState.take || 0;
        this.isLoading = false;
      }

      this.accountsReceivablesRequest = event;
      this.accountsReceivablesListSubject.getReport(filter);
    }
  }
  exportToExcel(): void {
    if (this.accountsReceivablesListSubject.getCount() > 0) {
      this.isLoading = true;
      const filter = new AccountsReceivablesRequestModel();
      filter.closingDate = Helper.shiftDateToUTC(
        this.accountsReceivablesRequest.date
      );
      filter.payerOrFunder = this.accountsReceivablesRequest.funder;
      filter.sortingModels = this.accountsReceivablesRequest.sortingModels;
      this.reportingService
        .accountsReceivablesExportToExcel(filter)
        .subscribe((response: any) => {
          const base64Data = response.data;
          const byteArray = this.base64ToBlob(base64Data);
          const url = window.URL.createObjectURL(byteArray);

          const a = document.createElement('a');
          a.href = url;
          a.download = 'Account Receivables report.xlsx'; // File name for the download
          document.body.appendChild(a);
          a.click();
          window.URL.revokeObjectURL(url);
          a.remove();
          this.isLoading = false;
        });
    }
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
  if (this.accountsReceivablesListSubject.getVirtualLoadingValue()) return;

  const _loadedData=this.accountsReceivablesListSubject.getLoadedPages().size * this.virtualScrollPageSize;
  if(_loadedData >=this.accountsReceivablesListSubject.getCount()) return;

  const nextStart = this.windowStart + this.pageSize;
  const total = this.accountsReceivablesListSubject.getCount();
  if (nextStart >= total) return;
  
  const loadedData=this.accountsReceivablesListSubject.getLoadedPages().size * this.virtualScrollPageSize;
  this.windowStart = (loadedData - this.maxWindowSize) >= 0 ? (loadedData - this.maxWindowSize) : 0;

  this.accountsReceivablesListSubject.append(
    this.buildVirtualFilter(loadedData)
  );

  this.recenterAfterLoad();
}
private handleVirtualScrollLoadUp(): void {
  if (!this.isVirtualMode) return;
  if (this.accountsReceivablesListSubject.getVirtualLoadingValue()) return;

  const _loadedData=this.accountsReceivablesListSubject.getLoadedPages().size * this.virtualScrollPageSize;
  if(_loadedData==150){
     return;
  }
  const loadedData=this.accountsReceivablesListSubject.getLoadedPages().size * this.virtualScrollPageSize;
  const prevStart = Math.max(0,loadedData - this.pageSize);
  this.windowStart = prevStart;
  this.accountsReceivablesListSubject.prepend(
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
      gridEl.scrollTop = Math.floor(maxScrollTop / 2);}
    this.isProgrammaticScroll = false;
    this.unlockScroll();
  });
}
private buildVirtualFilter(skip: number): AccountsReceivablesRequestModel {
  const filter = new AccountsReceivablesRequestModel();
  filter.payerOrFunder = this.accountsReceivablesRequest.funder;
  filter.closingDate = Helper.shiftDateToUTC(this.accountsReceivablesRequest.date);
  filter.sortingModels = this.accountsReceivablesRequest.sortingModels || [];
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
      label: 'Account Receivables-Claim Level',
      url: '/billing/reporting/account-receivables',
      tabIndex: 0,
      isReportsPage: true,
    },
  ];
}