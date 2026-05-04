import { Component, forwardRef, OnInit, ViewChild, NgZone, ChangeDetectorRef } from '@angular/core';
import {
  GridComponent,
  GridDataResult,
  PageChangeEvent,
  PagerSettings,
  SelectableMode,
  SelectableSettings,
  SelectionEvent,
} from '@progress/kendo-angular-grid';
import { map, Observable, Subscription, filter, fromEvent, throttleTime, take } from 'rxjs';
import { SortDescriptor, State } from '@progress/kendo-data-query';
import { PaginationService } from '@core/services/billing/pagination.service';
import { ClaimService } from '@core/services/billing/claim.service';
import { ClientInvoiceHistorySubject } from '@core/subjects/client-invoice-history-subject';
import { ClientHistoryService } from '@core/services/billing/client-history.service';
import { ClientInvoiceHistoryRequestModel } from '@core/models/billing/client-history';
import { ActivatedRoute } from '@angular/router';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { FormGroup } from '@angular/forms';
import { ClientHistoryInvoiceFilterComponent } from '../client-history-invoice-filter/client-history-invoice-filter.component';
import { Breadcrumb } from '@core/models/billing/bread-crumb';
import { Helper } from '@app/billing/encounters/common/common-helper';

@Component({
  selector: 'app-client-history-invoice',
  templateUrl: './client-history-invoice.component.html',
  styleUrls: ['./client-history-invoice.component.css'],
})
export class ClientHistoryInvoiceComponent implements OnInit {
  view!: Observable<GridDataResult>;
  mode: SelectableMode = 'multiple';
  public selectedIds: number[] = [];
  gridPageSizes: any;
  showFilter = false;
  subscriptions = new Subscription();
  clientInvoiceHistorySubject: ClientInvoiceHistorySubject;
  public isSubjectLoading$: Observable<boolean>;
  selectedClientInvoiceDetails: any;
  clientId!: number;
  accountInfoId!: number;
  filterForm!: FormGroup;
  breadcrumbs: Breadcrumb[];
  selectedClientDetails: any;
  // Virtual scroll
  public isAllSelected = false;
  public isLoadingMoreData = false;
  private virtualScrollPageSize = 50;
  private loadedRanges = new Set<number>();
  private scrollPrefetchSub: Subscription | null = null;
  private prefetchThreshold = 0.8;
  private scrollDebounceMs = 80;
  private lastBatchLoadTime: number = 0;
  private batchLoadCooldown: number = 150;
  private upwardResetPercentage: number = 0.3;
  private downwardResetPercentage: number = 0.4;
  private windowStart: number = 0;
  private windowEnd: number = 0;
  private maxWindowSize: number = 200;
  private lastScrollDirection: 'down' | 'up' | null = null;
  private lastScrollTop: number = 0;
  private lastLoadedSkip: number = -1;
  private isAllInitializing: boolean = false;
  private isBufferOperationInProgress: boolean = false;
  private readonly UP_SCROLL_THRESHOLD = 150; // px from top to trigger upward load
  footerRange: string = '0';

  readonly pagingSettings: PagerSettings = {
    buttonCount: 5,
    type: 'numeric',
    pageSizes: true,
    previousNext: true,
  };

  readonly selectableSettings: SelectableSettings = {
    checkboxOnly: true,
    mode: this.mode,
  };

  gridState: State = {
    sort: [{ dir: 'desc', field: 'dateOfService' }],
    skip: 0,
    take: 20,

    filter: {
      logic: 'and',
      filters: [],
    },
  };

  constructor(
    private paginationServices: PaginationService,
    private claimsService: ClaimService,
    private clientHistoryService: ClientHistoryService,
    private route: ActivatedRoute,
    private accountService: AccountMemberService,
    private ngZone: NgZone,
    private cdr: ChangeDetectorRef
  ) {
    this.selectedClientDetails = this.clientHistoryService.getClientData();

    // Breadcrumb for invoice page: Client History > Invoices
    this.breadcrumbs = [
      {
        label: 'Client History',
        url: '/billing/clienthistory/list',
        isReportsPage: false,
      },
      {
        label: 'Invoices',
        url: `/billing/clienthistory/invoice/${this.selectedClientDetails.clientId}`,
        isReportsPage: false,
      },
    ];

    this.getGridPageSizes();
    this.clientInvoiceHistorySubject = new ClientInvoiceHistorySubject(
      this.clientHistoryService
    );

    this.isSubjectLoading$ = this.clientInvoiceHistorySubject.getLoading();

    this.view = this.clientInvoiceHistorySubject.pipe(
      map((data) => ({ data, total: this.clientInvoiceHistorySubject.getCount() }))
    );
    // Footer updates only on buffer changes
    this.subscriptions.add(
      this.clientInvoiceHistorySubject.subscribe(() => {
        if ((this.isAllInitializing && this.clientInvoiceHistorySubject.getDataLength() === 0) || this.isBufferOperationInProgress) {
          return;
        }
        this.footerRange = this.computeFooterRange();
      })
    );
  }
  @ViewChild('clientHistoryInvoiceGrid') grid!: GridComponent;
  @ViewChild(GridComponent) clientHistoryInvoiceGrid!: GridComponent;
  @ViewChild(forwardRef(() => ClientHistoryInvoiceFilterComponent))
  clientHistoryInvoiceFilterComponent!: ClientHistoryInvoiceFilterComponent;
  ngOnInit(): void {
    this.accountInfoId = this.accountService.memberDetails.accountInfoId;
    this.route.url.subscribe((segments) => {
      this.clientId = +segments[segments.length - 1].path;
    });
  }
  onPageChange(event: PageChangeEvent): void {
    // Handle toggling between ALL and paged modes via pageSizes
    if (event.take === 0) {
      if (!this.isAllSelected) {
        // Enter ALL mode
        this.isAllSelected = true;
        this.loadedRanges.clear();
        this.windowStart = 0;
        this.windowEnd = 0;
        this.lastScrollDirection = null;
        this.lastScrollTop = 0;
        this.lastLoadedSkip = -1;
        this.isAllInitializing = true;
        this.gridState.skip = 0;
        this.gridState.take = 9999;
        this.paginationServices.setPageSizes(0);
        this.loadVirtualScrollInitial();
      }
      return;
    }

    // If currently ALL, selecting a numeric pageSize should exit ALL mode
    if (this.isAllSelected && event.take > 0) {
      this.exitAllModeToPaged(event.take);
      return;
    }

    // Normal paged mode page change
    this.gridState.skip = event.skip;
    this.gridState.take = event.take;
    this.paginationServices.setPageSizes(this.gridState.take);
    this.ClientInvoicesHistory();
  }

  private exitAllModeToPaged(take: number): void {
    this.isAllSelected = false;
    this.isAllInitializing = false;
    this.isLoadingMoreData = false;
    this.isBufferOperationInProgress = false;
    this.loadedRanges.clear();
    this.windowStart = 0;
    this.windowEnd = 0;
    this.lastLoadedSkip = -1;
    this.lastScrollDirection = null;
    this.lastScrollTop = 0;
    this.teardownScrollPrefetch();
    this.clientInvoiceHistorySubject.clearBuffer();
    this.gridState.skip = 0;
    this.gridState.take = take;
    this.paginationServices.setPageSizes(take);
    this.footerRange = this.computeFooterRange();
    this.ClientInvoicesHistory();
  }

  getPageStart(total: number): number { return 0; }
  getPageEnd(total: number): number { return 0; }

  getStatusClass(status: string): string {
    return status ? status.toLowerCase().replace(/\s+/g, '-') : '';
  }

  onSortChange(sortParams: SortDescriptor[]): void {
    this.gridState.sort = sortParams;
    this.gridState.skip = 0;
    if (this.isAllSelected) {
      this.loadedRanges.clear();
      this.windowStart = 0;
      this.windowEnd = 0;
      this.lastLoadedSkip = -1;
      this.isAllInitializing = true;
      this.paginationServices.setPageSizes(0);
      this.loadVirtualScrollInitial();
      return;
    }
    this.ClientInvoicesHistory();
  }

  public onSelectChange(event: SelectionEvent): void {
    (this.selectedIds as any).addRange((event.selectedRows as any)?.select((x: any) => x.dataItem.id));
    (event.deselectedRows || []).forEach((x: any) => (this.selectedIds as any).remove(x.dataItem.id));
    this.ClientInvoicesHistory();
  }

  getGridPageSizes(): void {
    const storedGridPageSizes = localStorage.getItem('gridPageSizes')
      ? JSON.parse(localStorage.getItem('gridPageSizes') || '')
      : null;
    if (storedGridPageSizes) {
      this.gridPageSizes = storedGridPageSizes;
    } else {
      this.subscriptions.add(
        this.claimsService
          .getGridPageSizes()
          .subscribe(
            (sizes: Array<number | { text: string; value: number }>) => {
              this.gridPageSizes = sizes;
            }
          )
      );
    }
  }

  onFilterChanged() {
    this.gridState.skip = 0;
    if (this.isAllSelected) {
      this.gridState.take = 9999;
      this.paginationServices.setPageSizes(0);
      this.reinitializeAllModeWithFilters();
      return;
    }
    this.ClientInvoicesHistory();
  }

  ClientInvoicesHistory() {
    const model: ClientInvoiceHistoryRequestModel = {
      InvoiceHistoryRequest: {
        take: this.gridState.take || 0,
        skip: this.gridState.skip || 0,
        clientId: this.clientId || 0,
        sortingModels: this.gridState.sort || [],
      },
      InvoiceHistoryRequestFilterModel: {
        accountInfoId: this.accountInfoId,
        status: this.clientHistoryInvoiceFilterComponent.selectedStatus
          .filter((s) => s.checked)
          .map((s) => s.id),
        patientResponsibilityFrom:
          this.clientHistoryInvoiceFilterComponent.patientResponsibilityFrom,
        patientResponsibilityTo:
          this.clientHistoryInvoiceFilterComponent.patientResponsibilityTo,
        dateOfServiceFrom: Helper.shiftDateToUTC(
          this.clientHistoryInvoiceFilterComponent.dateOfServiceFrom
        ),
        dateOfServiceTo: Helper.shiftDateToUTC(
          this.clientHistoryInvoiceFilterComponent.dateOfServiceTo
        ),
        invoiceDateFrom: Helper.shiftDateToUTC(
          this.clientHistoryInvoiceFilterComponent.invoiceDateFrom
        ),
        invoiceDateTo: Helper.shiftDateToUTC(
          this.clientHistoryInvoiceFilterComponent.invoiceDateTo
        ),
        invoiceDueDateFrom: Helper.shiftDateToUTC(
          this.clientHistoryInvoiceFilterComponent.invoiceDueDateFrom
        ),
        invoiceDueDateTo: Helper.shiftDateToUTC(
          this.clientHistoryInvoiceFilterComponent.invoiceDueDateTo
        ),
        patientBalanceFrom:
          this.clientHistoryInvoiceFilterComponent.patientBalanceFrom,
        patientBalanceTo:
          this.clientHistoryInvoiceFilterComponent.patientBalanceTo,
      },
    };
    this.clientInvoiceHistorySubject.getAll(model, this.isAllSelected);
  }

  private buildRequestModel(skip: number, take: number): ClientInvoiceHistoryRequestModel {
    return {
      InvoiceHistoryRequest: {
        take: take || 0,
        skip: skip || 0,
        clientId: this.clientId || 0,
        sortingModels: this.gridState.sort || [],
      },
      InvoiceHistoryRequestFilterModel: {
        accountInfoId: this.accountInfoId,
        status: this.clientHistoryInvoiceFilterComponent.selectedStatus
          .filter((s) => s.checked)
          .map((s) => s.id),
        patientResponsibilityFrom: this.clientHistoryInvoiceFilterComponent.patientResponsibilityFrom,
        patientResponsibilityTo: this.clientHistoryInvoiceFilterComponent.patientResponsibilityTo,
        dateOfServiceFrom: Helper.shiftDateToUTC(this.clientHistoryInvoiceFilterComponent.dateOfServiceFrom),
        dateOfServiceTo: Helper.shiftDateToUTC(this.clientHistoryInvoiceFilterComponent.dateOfServiceTo),
        invoiceDateFrom: Helper.shiftDateToUTC(this.clientHistoryInvoiceFilterComponent.invoiceDateFrom),
        invoiceDateTo: Helper.shiftDateToUTC(this.clientHistoryInvoiceFilterComponent.invoiceDateTo),
        invoiceDueDateFrom: Helper.shiftDateToUTC(this.clientHistoryInvoiceFilterComponent.invoiceDueDateFrom),
        invoiceDueDateTo: Helper.shiftDateToUTC(this.clientHistoryInvoiceFilterComponent.invoiceDueDateTo),
        patientBalanceFrom: this.clientHistoryInvoiceFilterComponent.patientBalanceFrom,
        patientBalanceTo: this.clientHistoryInvoiceFilterComponent.patientBalanceTo,
      },
    };
  }

  private loadVirtualScrollInitial(): void {
    const params = this.buildRequestModel(0, this.virtualScrollPageSize);
    this.loadedRanges.add(0);
    this.lastLoadedSkip = -1;
    this.lastScrollDirection = null;
    this.lastScrollTop = 0;
    this.clientInvoiceHistorySubject.getAll(params, true);
    setTimeout(() => {
      this.isAllInitializing = false;
      this.setupScrollPrefetch();
    }, 100);
  }

  private setupScrollPrefetch(): void {
    this.teardownScrollPrefetch();
    const gridEl = this.clientHistoryInvoiceGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    if (!gridEl) return;
    this.ngZone.runOutsideAngular(() => {
      this.scrollPrefetchSub = fromEvent(gridEl, 'scroll', { passive: true })
        .pipe(
          throttleTime(this.scrollDebounceMs, undefined, { trailing: true }),
          filter(() => this.isAllSelected && !this.isLoadingMoreData && !this.isAllInitializing && !this.isBufferOperationInProgress)
        )
        .subscribe(() => {
          const scrollTop = gridEl.scrollTop;
          const clientHeight = gridEl.clientHeight;
          const scrollHeight = gridEl.scrollHeight;
          const scrollPercentage = (scrollTop + clientHeight) / scrollHeight;
          const direction = scrollTop > this.lastScrollTop ? 'down' : 'up';
          this.lastScrollTop = scrollTop;
          if (this.lastScrollDirection && direction !== this.lastScrollDirection) {
            this.lastLoadedSkip = -1;
          }
          if (scrollTop < 20 && direction === 'up' && this.windowStart === 0) {
            return;
          }
          if (scrollPercentage >= this.prefetchThreshold && direction === 'down') {
            const totalCount = this.clientInvoiceHistorySubject.getCount();
            const currentData = this.clientInvoiceHistorySubject.value || [];
            const currentEnd = this.windowStart + currentData.length;
            if (currentEnd < totalCount) {
              this.lastScrollDirection = 'down';
              this.ngZone.run(() => this.loadNextVirtualBatch());
            }
          } else if (scrollTop < this.UP_SCROLL_THRESHOLD && direction === 'up') {
            if (this.windowStart > 0) {
              this.lastScrollDirection = 'up';
              this.ngZone.run(() => this.loadPreviousVirtualBatch());
            }
          }
        });
    });
  }

  private loadNextVirtualBatch(): void {
    if (this.isLoadingMoreData) return;
    const now = Date.now();
    if (now - this.lastBatchLoadTime < this.batchLoadCooldown) return;
    this.lastBatchLoadTime = now;
    const currentData = this.clientInvoiceHistorySubject.value || [];
    const currentLength = currentData.length;
    const totalCount = this.clientInvoiceHistorySubject.getCount();
    const skipVal = this.windowStart + currentLength;
    if (skipVal >= totalCount) return;
    if (this.lastLoadedSkip === skipVal) return;
    this.lastLoadedSkip = skipVal;
    const params = this.buildRequestModel(skipVal, this.virtualScrollPageSize);
    const itemsAfterThisLoad = totalCount - skipVal;
    const willLoadLastBatch = itemsAfterThisLoad <= this.virtualScrollPageSize;
    if (willLoadLastBatch && (currentLength + itemsAfterThisLoad) > this.maxWindowSize) {
      this.loadBatchWithEndAlignment(params);
    } else if (currentLength >= this.maxWindowSize) {
      this.loadBatchWithCleanup(params, 'down');
    } else {
      this.loadBatch(params, true);
    }
  }

  private loadPreviousVirtualBatch(): void {
    if (this.isLoadingMoreData) return;
    if (this.windowStart === 0) return;
    const now = Date.now();
    if (now - this.lastBatchLoadTime < this.batchLoadCooldown) return;
    this.lastBatchLoadTime = now;
    const skipVal = Math.max(0, this.windowStart - this.virtualScrollPageSize);
    if (this.lastLoadedSkip === skipVal) return;
    this.lastLoadedSkip = skipVal;
    const params = this.buildRequestModel(skipVal, this.virtualScrollPageSize);
    const currentLength = (this.clientInvoiceHistorySubject.value || []).length;
    if (currentLength >= this.maxWindowSize) {
      this.loadBatchWithCleanup(params, 'up');
    } else {
      this.loadBatchUpward(params);
    }
  }

  private loadBatch(params: ClientInvoiceHistoryRequestModel, append: boolean): void {
    this.isLoadingMoreData = true;
    const gridEl = this.clientHistoryInvoiceGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    const scrollTopBefore = gridEl?.scrollTop || 0;
    const beforeLength = this.clientInvoiceHistorySubject.getDataLength();
    if (append) {
      this.clientInvoiceHistorySubject.append(params);
    } else {
      this.clientInvoiceHistorySubject.getAll(params, this.isAllSelected);
    }
    this.clientInvoiceHistorySubject
      .pipe(
        filter((arr: any[]) => (arr?.length || 0) > beforeLength),
        take(1)
      )
      .subscribe(() => {
        if (append && gridEl) {
          requestAnimationFrame(() => {
            gridEl.scrollTop = scrollTopBefore;
            this.lastScrollTop = scrollTopBefore;
          });
        }
        this.isLoadingMoreData = false;
        this.cdr.detectChanges();
      });
  }

  private loadBatchUpward(params: ClientInvoiceHistoryRequestModel): void {
    this.isLoadingMoreData = true;
    const beforeLength = this.clientInvoiceHistorySubject.getDataLength();
    this.clientInvoiceHistorySubject.prependBatch(params, false);
    const amountPrepended = this.virtualScrollPageSize;
    this.windowStart = Math.max(0, this.windowStart - amountPrepended);
    this.clientInvoiceHistorySubject
      .pipe(
        filter((arr: any[]) => (arr?.length || 0) > beforeLength),
        take(1)
      )
      .subscribe(() => {
        const currentLengthAfterPrepend = this.clientInvoiceHistorySubject.getDataLength();
        if (currentLengthAfterPrepend > this.maxWindowSize) {
          const excessRows = currentLengthAfterPrepend - this.maxWindowSize;
          this.clientInvoiceHistorySubject.removeFromBottom(excessRows);
        }
        // After upward prepend + possible trim, reset scrollbar to safe middle
        const gridEl = this.clientHistoryInvoiceGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
        if (gridEl) {
          requestAnimationFrame(() => {
            const clientHeight = gridEl.clientHeight || 0;
            const scrollHeight = gridEl.scrollHeight || 0;
            if (clientHeight > 0 && scrollHeight > 0) {
              const target = scrollHeight * this.upwardResetPercentage;
              const desiredTop = Math.max(0, Math.min(scrollHeight - clientHeight, target));
              gridEl.scrollTop = desiredTop;
              this.lastScrollTop = desiredTop;
            }
          });
        }
        this.isLoadingMoreData = false;
        this.cdr.detectChanges();
      });
  }

  private loadBatchWithEndAlignment(params: ClientInvoiceHistoryRequestModel): void {
    this.isLoadingMoreData = true;
    this.isBufferOperationInProgress = true;
    const gridEl = this.clientHistoryInvoiceGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    const beforeLength = this.clientInvoiceHistorySubject.getDataLength();
    const totalCount = this.clientInvoiceHistorySubject.getCount();
    this.clientInvoiceHistorySubject.append(params);
    this.clientInvoiceHistorySubject
      .pipe(
        filter((arr: any[]) => (arr?.length || 0) > beforeLength),
        take(1)
      )
      .subscribe(() => {
        this.ngZone.run(() => {
          const currentData = this.clientInvoiceHistorySubject.value || [];
          let currentLength = currentData.length;
          if (currentLength > this.maxWindowSize) {
            const maxBatchesInWindow = Math.floor(this.maxWindowSize / this.virtualScrollPageSize);
            const lastBatchStart = Math.floor((totalCount - 1) / this.virtualScrollPageSize) * this.virtualScrollPageSize;
            const desiredWindowStart = Math.max(0, lastBatchStart - (maxBatchesInWindow - 1) * this.virtualScrollPageSize);
            const desiredWindowSize = totalCount - desiredWindowStart;
            const rowsToRemove = currentLength - desiredWindowSize;
            if (rowsToRemove > 0) {
              this.clientInvoiceHistorySubject.removeFromTop(rowsToRemove);
              this.windowStart = desiredWindowStart;
              currentLength = desiredWindowSize;
            }
          }
          this.cdr.detectChanges();
          requestAnimationFrame(() => {
            if (gridEl) {
              const scrollHeight = gridEl.scrollHeight || 0;
              const clientHeight = gridEl.clientHeight || 0;
              gridEl.scrollTop = Math.max(0, scrollHeight - clientHeight);
              this.lastScrollTop = gridEl.scrollTop;
            }
            setTimeout(() => {
              this.footerRange = this.computeFooterRange();
              this.isBufferOperationInProgress = false;
              this.isLoadingMoreData = false;
            }, 100);
          });
        });
      });
  }

  private loadBatchWithCleanup(params: ClientInvoiceHistoryRequestModel, direction: 'up' | 'down'): void {
    this.isLoadingMoreData = true;
    this.isBufferOperationInProgress = true;
    if (direction === 'down') {
      const gridEl = this.clientHistoryInvoiceGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
      let actualRowHeight = 40;
      const scrollTopBefore = gridEl?.scrollTop || 0;
      if (gridEl) {
        const firstRow = gridEl.querySelector('tbody tr');
        if (firstRow) {
          actualRowHeight = firstRow.getBoundingClientRect().height;
        }
      }
      const beforeLength = this.clientInvoiceHistorySubject.getDataLength();
      this.clientInvoiceHistorySubject.append(params);
      this.clientInvoiceHistorySubject
        .pipe(
          filter((arr: any[]) => (arr?.length || 0) > beforeLength),
          take(1)
        )
        .subscribe(() => {
          requestAnimationFrame(() => {
            this.applySlidingWindowCleanupDownward(actualRowHeight, scrollTopBefore);
            this.ngZone.run(() => this.cdr.detectChanges());
            this.footerRange = this.computeFooterRange();
            this.isLoadingMoreData = false;
            this.isBufferOperationInProgress = false;
          });
        });
    } else {
      const gridEl = this.clientHistoryInvoiceGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
      const beforeLength = this.clientInvoiceHistorySubject.getDataLength();
      this.clientInvoiceHistorySubject.prependBatch(params, false);
      const amountPrepended = this.virtualScrollPageSize;
      this.windowStart = Math.max(0, this.windowStart - amountPrepended);
      this.clientInvoiceHistorySubject
        .pipe(
          filter((arr: any[]) => (arr?.length || 0) > beforeLength),
          // @ts-ignore
          take(1)
        )
        .subscribe(() => {
          this.ngZone.run(() => {
            requestAnimationFrame(() => {
              const currentData = this.clientInvoiceHistorySubject.value || [];
              const currentLength = currentData.length;
              if (currentLength > this.maxWindowSize) {
                const excessRows = currentLength - this.maxWindowSize;
                this.clientInvoiceHistorySubject.removeFromBottom(excessRows);
              }
              if (gridEl) {
                const clientHeight = gridEl.clientHeight || 0;
                const scrollHeight = gridEl.scrollHeight || 0;
                if (clientHeight > 0 && scrollHeight > 0) {
                  const target = scrollHeight * this.upwardResetPercentage;
                  const desiredTop = Math.max(0, Math.min(scrollHeight - clientHeight, target));
                  gridEl.scrollTop = desiredTop;
                  this.lastScrollTop = desiredTop;
                }
              }
              this.cdr.detectChanges();
              this.footerRange = this.computeFooterRange();
              this.isLoadingMoreData = false;
              this.isBufferOperationInProgress = false;
            });
          });
        });
    }
  }

  private applySlidingWindowCleanupDownward(actualRowHeight: number, scrollTopBefore: number): void {
    const currentData = this.clientInvoiceHistorySubject.value || [];
    const currentLength = currentData.length;
    if (currentLength <= this.maxWindowSize) return;
    const excessRows = currentLength - this.maxWindowSize;
    const rowsToRemove = excessRows;
    if (rowsToRemove <= 0) return;
    const gridEl = this.clientHistoryInvoiceGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    this.clientInvoiceHistorySubject.removeFromTop(rowsToRemove);
    this.windowStart += rowsToRemove;
    if (gridEl) {
      requestAnimationFrame(() => {
        const clientHeight = gridEl.clientHeight || 0;
        const scrollHeight = gridEl.scrollHeight || 0;
        if (clientHeight > 0 && scrollHeight > 0) {
          const target = scrollHeight * this.downwardResetPercentage;
          const desiredTop = Math.max(0, Math.min(scrollHeight - clientHeight, target));
          gridEl.scrollTop = desiredTop;
          this.lastScrollTop = desiredTop;
        }
      });
    }
  }

  private teardownScrollPrefetch(): void {
    if (this.scrollPrefetchSub) {
      this.scrollPrefetchSub.unsubscribe();
      this.scrollPrefetchSub = null;
    }
  }

  private reinitializeAllModeWithFilters(): void {
    this.isAllInitializing = true;
    this.isLoadingMoreData = false;
    this.isBufferOperationInProgress = false;
    this.loadedRanges.clear();
    this.windowStart = 0;
    this.windowEnd = 0;
    this.lastLoadedSkip = -1;
    this.lastScrollDirection = null;
    this.lastScrollTop = 0;
    this.clientInvoiceHistorySubject.clearBuffer();
    this.teardownScrollPrefetch();
    this.loadVirtualScrollInitial();
  }

  ngOnDestroy(): void {
    this.teardownScrollPrefetch();
    if (this.subscriptions) {
      this.subscriptions.unsubscribe();
    }
  }

  private computeFooterRange(): string {
    const total = this.clientInvoiceHistorySubject.getCount();
    const currentLength = this.clientInvoiceHistorySubject.getDataLength();
    if (!total || total === 0 || currentLength === 0) return '0';
    if (this.isAllSelected) {
      const start = this.windowStart + 1;
      const end = Math.min(this.windowStart + currentLength, total);
      return `${start}–${end} of ${total}`;
    }
    const skip = this.gridState && this.gridState.skip ? this.gridState.skip : 0;
    const take = this.gridState && this.gridState.take ? this.gridState.take : 20;
    const start = skip + 1;
    const end = Math.min(skip + take, total);
    return `${start}–${end} of ${total}`;
  }
}
