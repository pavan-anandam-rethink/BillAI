import { ChangeDetectorRef, Component, ElementRef, forwardRef, NgZone, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { fromEvent, map, Observable, Subscription } from "rxjs";
import { filter, throttleTime } from 'rxjs/operators';
import { ActivatedRoute, Router } from '@angular/router';
import { GridComponent, GridDataResult, PageChangeEvent, PagerSettings, SelectableMode, SelectableSettings } from '@progress/kendo-angular-grid';
import { State } from '@progress/kendo-data-query';
import { Locale } from '@app/locale';
import { MemberViewSettings } from '@core/models/billing/member-view-settings';
import { PaginationService } from '@core/services/billing/pagination.service';
import { environment } from 'src/environments/environment';
import { BillingDetailView, InvoiceDetailsModel, PatientInvoiceHeaderSearch } from '@core/models/billing/patient-invoice';
import { PatientInvoiceService } from '@core/services/billing/patient-invoice.service';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ConfirmDialog } from '@core/models/common';
import { NotificationService } from '@progress/kendo-angular-notification';
import { PendingCollectionListSubject } from '@core/subjects/pending-collection-list.subject';
import { PendingCollectionFiltersComponent } from './pending-collection-filters/pending-collection-filters.component';
import { PrintInvoiceShared } from '../shared/print-invoice-shared';
import { Helper } from '@app/billing/encounters/common/common-helper';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { PendingCollectionFilterService } from '@app/billing/services/pending-collection-filter.service';
import { ClaimService, PaymentPostingService } from '@core/services/billing';
import { DialogAction } from '@progress/kendo-angular-dialog';
import { DialogService } from '@progress/kendo-angular-dialog';
import { ManualPaymentPatientSearch, ManualPaymentPatientSearchBase } from '@core/models/billing/manual-payment-patient-search';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { AccountPermissions } from '@core/enums/account';

export enum InvoiceListingTab {
  CreateInvoice = 1,
  PendingCollection,
  ClientHistory
}

export interface IndexResult {
  index: number;
  anchor: HTMLAnchorElement;
  clientId: number;
}

@Component({
  selector: 'pending-collection',
  templateUrl: './pending-collection.component.html',
  styleUrls: ['./pending-collection.component.css',
    '../status-actions.css']
})
export class PendingCollectionComponent implements OnInit, OnDestroy {
  @ViewChild(forwardRef(() => PendingCollectionFiltersComponent)) filtersComponent: PendingCollectionFiltersComponent;
  @ViewChild("encountersGrid") claimsGrid: GridComponent;
  @ViewChild(GridComponent) encounterGrid: GridComponent;
  @ViewChild('selectorAnchorEl', { read: ElementRef }) public selectorAnchor: ElementRef;
  @ViewChild('printAnchorEl', { read: ElementRef }) public printAnchor: ElementRef;

  subscriptions = new Subscription();
  public mode: SelectableMode = "multiple";
  public invoices: InvoiceDetailsModel[] = [];
  readonly pagingSettings: PagerSettings = {
    buttonCount: 5,
    type: 'numeric',
    pageSizes: true,
    previousNext: true
  };

  readonly selectableSettings: SelectableSettings = {
    enabled: true,
    mode: 'multiple'
  };

  selectedPatientWithLines: BillingDetailView[] = [];


  // Added
    guarantorMap: Record<number,string> = {};
    guarantorLoadError = false;

  filterChangedTimeout: number;

  showSelectorPopup = false;
  flipSelector = false;
  showPrintPopup = false;
  flipPrintPopup = false;
  selectedClients: number[] = [];
  selectAllPatients = false;
  isFromPaymentPostingBackButton = false;
  canEdit: boolean = false;

  IndexList: IndexResult[] = [];

  public mySelection: number[] = [];
  userList: ClaimFilterOptionModel[] | [];

  pendingCollectionListSubject: PendingCollectionListSubject;
  readonly printInvoiceShared: PrintInvoiceShared;
  view: Observable<GridDataResult>
  viewData: InvoiceDetailsModel[] = [];
  totalInvoiceHeaders = 0;
  public dialogForTotalCount: boolean = false;
  public gatClaimsTabData: boolean = false;

  gridState: State = {
    sort: [{ dir: "desc", field: "dateOfServiceStart" }],
    skip: 0,
    take: 20,

    // Initial filter descriptor
    filter: {
      logic: 'and',
      filters: []
    }
  };

  showFilter: boolean;

  rethinkUrl: string;

  gridPageSizes: any;

  // ===== Virtual Scroll (ALL mode) =====
  public isAllSelected = false;
  public isLoadingMoreData = false;
  private isAllInitializing = false;
  private isBufferOperationInProgress = false;

  // Virtual scroll batch size - number of records fetched per API call
  private virtualScrollPageSize = 50;
  
  // Maximum DOM window size - total records kept in DOM at once
  private maxWindowSize = 200;

  private windowStart = 0;
  private windowEnd = 0;

  private lastLoadedSkip = -1;
  private lastScrollTop = 0;
  private lastScrollDirection: 'down' | 'up' | null = null;
  private loadedRanges = new Set<number>();

  private scrollPrefetchSub: Subscription | null = null;
  private scrollDebounceMs = 150;
  private prefetchDownThreshold = 0.8;
  
  // Prefetch threshold for upward scrolling - triggers API call at 30% scroll position
  private prefetchUpThreshold = 0.2;
  private readonly UP_SCROLL_THRESHOLD = 150; // px from top to trigger upward load
  private upwardResetPercentage: number = 0.3;
  private downwardResetPercentage: number = 0.4;
  // ===== End Virtual Scroll State =====

  // Guard flags to prevent duplicate loads and scroll-induced retriggers
  private isAdjustingWindow = false;
  private lastLoadAt = 0;
  private loadCooldownMs = 300;

  showPaymentDialog: boolean = false;
  selectedPaymentData: any = null;
  footerRange: string = '0';

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private patientInvoiceService: PatientInvoiceService,
    private paginationService: PaginationService,
    public locale: Locale,
    private cdr: ChangeDetectorRef,
    private notificationService: NotificationHandlerService,
    private filterService: PendingCollectionFilterService,
    private claimsService: ClaimService,
    private dialogService: DialogService,
    private paymentPostingService: PaymentPostingService,
    private accountService: AccountMemberService,
    private ngZone: NgZone
  ) {
    this.rethinkUrl = environment.rethinkBHUrl;
    this.printInvoiceShared = new PrintInvoiceShared();
    this.userList = [];
    this.pendingCollectionListSubject = new PendingCollectionListSubject(this.patientInvoiceService);
    this.getGridPageSizes();
    this.view = this.pendingCollectionListSubject.pipe(
      map(data => {
        let result = { data: data, total: this.pendingCollectionListSubject.getCount() };
        
        // Update totalInvoiceHeaders for footer display
        this.totalInvoiceHeaders = result.total;
        
        this.userList = this.userList.length == 0 ? this.pendingCollectionListSubject.getUserList() : this.userList;
        this.viewData = data;
        return result;
      })
    )

    // Update footer range whenever the buffer changes
    this.subscriptions.add(
      this.pendingCollectionListSubject.subscribe(() => {
        // Avoid footer flicker while initializing, window aligning, or during in-flight loads
        if (
          (this.isAllInitializing && this.pendingCollectionListSubject.getDataLength() === 0) ||
          this.isBufferOperationInProgress ||
          (this.isAllSelected && this.isLoadingMoreData)
        ) {
          return;
        }
        this.footerRange = this.computeFooterRange();
      })
    );

    this.subscriptions.add(this.accountService.accountMemberSettings.subscribe((x) => {
                if (x) {
                    this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingEdit);
                }
            }));
  }

  ngAfterContentChecked() {
    this.cdr.detectChanges();
  }

  loadPatientHeaders() {

    const filter = new PatientInvoiceHeaderSearch();
    filter.filters.clientIds = this.filtersComponent.selectedPatients.map(x => x.id).join(',');
    filter.filters.dateOfServiceFrom = Helper.shiftDateToUTC(this.filtersComponent.dateOfServiceFrom);
    filter.filters.dateOfServiceTo = Helper.shiftDateToUTC(this.filtersComponent.dateOfServiceTo);
    filter.filters.invoiceFrom = Helper.shiftDateToUTC(this.filtersComponent.invoiceFrom);
    filter.filters.invoiceTo = Helper.shiftDateToUTC(this.filtersComponent.invoiceTo);
    filter.filters.paymentDueFrom = Helper.shiftDateToUTC(this.filtersComponent.paymentDueFrom);
    filter.filters.paymentDueTo = Helper.shiftDateToUTC(this.filtersComponent.paymentDueTo);
    filter.filters.patientResponsibilityFrom = this.filtersComponent.patientResponsibilityFrom;
    filter.filters.patientResponsibilityTo = this.filtersComponent.patientResponsibilityTo;
    filter.take = this.gridState.take || 0;
    filter.skip = this.gridState.skip || 0;

    this.selectedClients = [];
    this.selectedPatientWithLines = [];
    this.mySelection = [];
    this.selectAllPatients = false;
    this.pendingCollectionListSubject.getAll(filter);

    this.filterService.setFilter(this.filtersComponent);
  }

  onPageChange(event: PageChangeEvent): void {
    // Handle 'All' selection - user clicked ALL button
    if (event.take === 0 && !this.isAllSelected) {
      this.enterAllMode();
      return;
    }

    // Already in ALL mode and clicked ALL again - ignore to avoid heavy reload
    if (event.take === 0 && this.isAllSelected) {
      return;
    }

    // User clicked a specific page size while in ALL mode - exit ALL mode
    if (this.isAllSelected && event.take !== 0) {
      this.exitAllMode(event);
      return;
    }

    // Normal pagination
    this.gridState.skip = event.skip;
    this.gridState.take = event.take;

    if (
      this.gridState.take === 0 &&
      this.pendingCollectionListSubject.getCount() > 1000
    ) {
      this.dialogForTotalCount = true;
    } else {
      this.dialogForTotalCount = false;
      this.gatClaimsTabData = true;
      if (this.gridState.take === 0) this.gridState.take = 9999999;
      this.paginationService.setPageSizes(this.gridState.take);
      this.loadPatientHeaders();
      localStorage.setItem('lastPageSize', this.gridState.take.toString());
    }

    if (this.gridState.take === 0) this.gridState.take = 9999999;
    if (this.dialogForTotalCount) {
      this.subscriptions.add(
        this.SubmitPageCount().result.subscribe((result) => {
          if ((result as DialogAction).text === 'Yes') {
            this.loadPatientHeaders();
            this.gatClaimsTabData = true;
          } else {
            const lastPageSize = localStorage.getItem('lastPageSize');
            this.gridState.take = lastPageSize
              ? JSON.parse(lastPageSize)
              : null;
          }

          this.paginationService.setPageSizes(this.gridState.take);
        })
      );
    }
  }

  SubmitPageCount() {
    const confirmDialog = this.dialogService.open({
      title: '⚠️ Please confirm',
      width: 500,
      content: 'There are more than 1000 records to display. This may take more time to process. You can either proceed or apply a filter to narrow down the results',
      actions: [
        { text: "Cancel" },
        { text: "Yes", primary: true }
      ]
    });
    return confirmDialog;
  }

  // ===== Virtual Scroll Methods (ALL mode) =====

  private enterAllMode(): void {
    this.isAllSelected = true;
    this.isAllInitializing = true;
    this.loadedRanges.clear();

    this.windowStart = 0;
    this.windowEnd = 0;
    this.lastLoadedSkip = -1;
    this.lastScrollTop = 0;
    this.loadedRanges.add(0);

    this.gridState.skip = 0;
    this.gridState.take = 99999;

    this.paginationService.setPageSizes(0);
    this.pendingCollectionListSubject.clearBuffer();

    this.cdr.detectChanges();
    this.resetGridScrollTop();
    this.loadPendingCollectionVirtual(true, 0);

    localStorage.setItem('lastPageSize', '0');
    this.gatClaimsTabData = true;
  }

  private exitAllMode(event: PageChangeEvent): void {
    this.teardownScrollPrefetch();

    this.isAllSelected = false;
    this.loadedRanges.clear();
    this.windowStart = 0;
    this.windowEnd = 0;
    this.lastLoadedSkip = -1;

    this.gridState.skip = event.skip;
    this.gridState.take = event.take;
    this.dialogForTotalCount = false;
    this.gatClaimsTabData = true;

    this.paginationService.setPageSizes(this.gridState.take);
    this.loadPatientHeaders();

    localStorage.setItem('lastPageSize', this.gridState.take.toString());
  }

  private buildFilter(skip: number, take: number): PatientInvoiceHeaderSearch {
    const filter = new PatientInvoiceHeaderSearch();
    filter.filters.clientIds = this.filtersComponent.selectedPatients
      .map((x) => x.id)
      .join(',');
    filter.filters.dateOfServiceFrom = Helper.shiftDateToUTC(
      this.filtersComponent.dateOfServiceFrom
    );
    filter.filters.dateOfServiceTo = Helper.shiftDateToUTC(
      this.filtersComponent.dateOfServiceTo
    );
    filter.filters.invoiceFrom = Helper.shiftDateToUTC(
      this.filtersComponent.invoiceFrom
    );
    filter.filters.invoiceTo = Helper.shiftDateToUTC(
      this.filtersComponent.invoiceTo
    );
    filter.filters.paymentDueFrom = Helper.shiftDateToUTC(
      this.filtersComponent.paymentDueFrom
    );
    filter.filters.paymentDueTo = Helper.shiftDateToUTC(
      this.filtersComponent.paymentDueTo
    );
    filter.filters.patientResponsibilityFrom =
      this.filtersComponent.patientResponsibilityFrom;
    filter.filters.patientResponsibilityTo =
      this.filtersComponent.patientResponsibilityTo;
    filter.take = take;
    filter.skip = skip;

    return filter;
  }

  private loadPendingCollectionVirtual(isInitial: boolean, skip: number): void {
    const filter = this.buildFilter(skip, this.virtualScrollPageSize);

    if (isInitial) {
      this.pendingCollectionListSubject.getAll(filter, true);
      setTimeout(() => {
        this.isAllInitializing = false;
        this.setupScrollPrefetch();
        this.isLoadingMoreData = false;
      }, 100);
    } else {
      const beforeLength = this.pendingCollectionListSubject.getDataLength();
      this.pendingCollectionListSubject.append(filter);
      setTimeout(() => {
        const totalInWindow = this.pendingCollectionListSubject.getDataLength();
        const uniqueTotal = this.pendingCollectionListSubject.getCount();
        const itemsAfterThisLoad = Math.max(0, uniqueTotal - (this.windowStart + beforeLength));
        const willLoadLastBatch = itemsAfterThisLoad <= this.virtualScrollPageSize;
        let repositionNeeded = false;
        if (willLoadLastBatch) {
          // Always align window to the end when we reach the last batch,
          // even if currentLength is exactly maxWindowSize, so windowStart is correct.
          this.alignWindowToEnd(uniqueTotal);
          repositionNeeded = true;
        } else {
          const needsTrimForMax = totalInWindow > this.maxWindowSize;
          const needsTrimForSmallTotals = uniqueTotal <= this.maxWindowSize && totalInWindow > (this.virtualScrollPageSize * 2);
          if (needsTrimForMax || needsTrimForSmallTotals) {
            this.applySlidingWindowCleanupDownward();
            repositionNeeded = true;
          }
        }

        // Reposition only when the window was trimmed or aligned
        if (repositionNeeded) {
          const gridEl = this.getGridContentEl();
          if (gridEl) {
            this.isAdjustingWindow = true;
            const clientHeight = gridEl.clientHeight || 0;
            const scrollHeight = gridEl.scrollHeight || 0;
            if (clientHeight > 0 && scrollHeight > 0) {
              const target = scrollHeight * this.downwardResetPercentage;
              const desiredTop = Math.max(0, Math.min(scrollHeight - clientHeight, target));
              gridEl.scrollTop = desiredTop;
              this.lastScrollTop = desiredTop;
            }
            this.isAdjustingWindow = false;
          }
        }

        this.isLoadingMoreData = false;
      }, 100);
    }

    this.mySelection = [];
    this.selectedPatientWithLines = [];
    this.selectedClients = [];
    this.selectAllPatients = false;
  }

  private setupScrollPrefetch(): void {
    if (this.scrollPrefetchSub) {
      this.scrollPrefetchSub.unsubscribe();
      this.scrollPrefetchSub = null;
    }

    const gridEl = this.claimsGrid && this.claimsGrid.wrapper
      ? this.claimsGrid.wrapper.nativeElement.querySelector('.k-grid-content')
      : null;

    if (!gridEl) return;

    this.ngZone.runOutsideAngular(() => {
      this.scrollPrefetchSub = fromEvent(gridEl, 'scroll', { passive: true })
        .pipe(
          throttleTime(this.scrollDebounceMs),
          filter(() => this.isAllSelected && !this.isLoadingMoreData && !this.isAdjustingWindow && (Date.now() - this.lastLoadAt) >= this.loadCooldownMs)
        )
        .subscribe(() => {
          const scrollTop = gridEl.scrollTop;
          const clientHeight = gridEl.clientHeight;
          const scrollHeight = gridEl.scrollHeight;

          const topPercentage = scrollTop / scrollHeight;
          const bottomPercentage = (scrollTop + clientHeight) / scrollHeight;
          const direction = scrollTop > this.lastScrollTop ? 'down' : 'up';
          this.lastScrollTop = scrollTop;
          if (this.lastScrollDirection && direction !== this.lastScrollDirection) {
            this.lastLoadedSkip = -1;
          }
          this.lastScrollDirection = direction;

          if (scrollTop <= 1 && direction === 'up') {
            return;
          }

          if (bottomPercentage >= this.prefetchDownThreshold && direction === 'down') {
            this.ngZone.run(() => this.handleScrollDown());
          } else if ((scrollTop <= this.UP_SCROLL_THRESHOLD || topPercentage <= this.prefetchUpThreshold) && direction === 'up' && this.windowStart > 0) {
            this.ngZone.run(() => this.handleScrollUp());
          }
        });
    });
  }

  private teardownScrollPrefetch(): void {
    if (this.scrollPrefetchSub) {
      this.scrollPrefetchSub.unsubscribe();
      this.scrollPrefetchSub = null;
    }
  }

  private handleScrollDown(): void {
    if (!this.isAllSelected || this.isLoadingMoreData || this.isAllInitializing) return;

    const currentLength = this.pendingCollectionListSubject.getDataLength();
    const totalCount = this.pendingCollectionListSubject.getCount();

    const skipVal = this.windowStart + currentLength;

    if (skipVal >= totalCount || skipVal === this.lastLoadedSkip) return;

    const pageIndex = Math.floor(skipVal / this.virtualScrollPageSize);

    if (!this.loadedRanges.has(pageIndex)) {
      this.loadedRanges.add(pageIndex);
      this.isLoadingMoreData = true;
      this.lastLoadedSkip = skipVal;
      this.lastLoadAt = Date.now();

      this.loadPendingCollectionVirtual(false, skipVal);
      this.windowEnd = skipVal + this.virtualScrollPageSize;

      if (currentLength >= this.maxWindowSize) {
        this.applySlidingWindowCleanupDownward();
      }
    }
  }

  private handleScrollUp(): void {
    if (!this.isAllSelected || this.isLoadingMoreData || this.windowStart <= 0) return;

    // If currently at dataset end, base previous skip on the desired end-aligned window
    const totalCount = this.pendingCollectionListSubject.getCount();
    const currentLength = this.pendingCollectionListSubject.getDataLength();
    const atEndNow = (this.windowStart + currentLength) >= totalCount;
    let baseWindowStart = this.windowStart;
    if (atEndNow) {
      const maxBatchesInWindow = Math.floor(this.maxWindowSize / this.virtualScrollPageSize) || 1;
      const lastBatchStart = Math.floor((totalCount - 1) / this.virtualScrollPageSize) * this.virtualScrollPageSize;
      baseWindowStart = Math.max(0, lastBatchStart - (maxBatchesInWindow - 1) * this.virtualScrollPageSize);
    }
    const previousSkip = Math.max(0, baseWindowStart - this.virtualScrollPageSize);
    // Prevent duplicate fetches for the same page
    if (previousSkip === this.lastLoadedSkip) return;
    const pageIndex = Math.floor(previousSkip / this.virtualScrollPageSize);
    // Upward: allow fetch even if pageIndex was previously loaded, since it
    // may have been trimmed from the current window.
    this.isLoadingMoreData = true;
    this.lastLoadAt = Date.now();

    const gridEl = this.claimsGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    this.loadPatientHeadersBatchUpward(previousSkip, () => {
      this.windowStart = previousSkip;
      this.lastLoadedSkip = previousSkip;
      if (gridEl) {
        this.isAdjustingWindow = true;
        const clientHeight = gridEl.clientHeight;
        const scrollHeightAfter = gridEl.scrollHeight;
        const maxScrollable = Math.max(0, scrollHeightAfter - clientHeight);
        const midTarget = Math.max(0, Math.round(0.3 * maxScrollable));
        const safeTarget = Math.max(this.UP_SCROLL_THRESHOLD + 5, midTarget);
        gridEl.scrollTop = safeTarget;
        this.lastScrollTop = gridEl.scrollTop;
        this.isAdjustingWindow = false;
      }

      const totalInWindow = this.pendingCollectionListSubject.getDataLength();
      const uniqueTotal = this.pendingCollectionListSubject.getCount();
      this.windowEnd = this.windowStart + totalInWindow;

      const needsTrimForMax = totalInWindow > this.maxWindowSize;
      const needsTrimForSmallTotals = uniqueTotal <= this.maxWindowSize && totalInWindow > (this.virtualScrollPageSize * 2);
      if (needsTrimForMax || needsTrimForSmallTotals) {
        this.applySlidingWindowCleanup('up');
      }
    });
  }

  private loadPatientHeadersBatchUpward(skip: number, onComplete?: () => void): void {
    const filter = this.buildFilter(skip, this.virtualScrollPageSize);

    this.pendingCollectionListSubject.prependBatch(filter).subscribe(
      () => {
        this.isLoadingMoreData = false;
        this.isAllInitializing = false;
        if (onComplete) {
          onComplete();
        }
      },
      (error) => {
        this.isLoadingMoreData = false;
        this.isAllInitializing = false;
        console.error('Error loading batch upward:', error);
      }
    );
  }

  private applySlidingWindowCleanup(direction: 'down' | 'up'): void {
    const currentData = this.pendingCollectionListSubject.getDataLength();
    const totalCount = this.pendingCollectionListSubject.getCount();

    // Always remove at most one batch; for upward scroll, also trim for small totals
    let recordsToRemove = 0;
    const excessRows = Math.max(0, currentData - this.maxWindowSize);
    if (direction === 'down') {
      if (excessRows > 0) {
        // Remove just the excess to keep the window exact
        recordsToRemove = excessRows;
      } else if (totalCount <= this.maxWindowSize && currentData > (this.virtualScrollPageSize * 2)) {
        // Cap to two pages for small totals
        recordsToRemove = this.virtualScrollPageSize;
      }
    } else {
      // Upward: trim when exceeding max window OR for small totals beyond two pages
      if (excessRows > 0) {
        // Remove just the excess to keep the window exact (e.g., from 51 to 40)
        recordsToRemove = excessRows;
      } else if (totalCount <= this.maxWindowSize && currentData > (this.virtualScrollPageSize * 2)) {
        recordsToRemove = this.virtualScrollPageSize;
      }
    }

    if (recordsToRemove <= 0) return;
    const gridEl = this.claimsGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    this.isAdjustingWindow = true;

    if (direction === 'down') {
      const oldWindowStart = this.windowStart;
      this.windowStart += recordsToRemove;
      this.pendingCollectionListSubject.removeFromTop(recordsToRemove);

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

      for (let i = 0; i < recordsToRemove; i += this.virtualScrollPageSize) {
        const pageToRemove = Math.floor((oldWindowStart + i) / this.virtualScrollPageSize);
        this.loadedRanges.delete(pageToRemove);
      }
    } else {
      const oldWindowEnd = this.windowEnd;
      this.pendingCollectionListSubject.removeFromBottom(recordsToRemove);
      this.windowEnd = this.windowStart + this.pendingCollectionListSubject.getDataLength();

      if (gridEl) {
        requestAnimationFrame(() => {
          const clientHeight = gridEl.clientHeight || 0;
          const scrollHeight = gridEl.scrollHeight || 0;
          if (clientHeight > 0 && scrollHeight > 0) {
            const target = scrollHeight * this.upwardResetPercentage;
            let desiredTop = Math.max(0, Math.min(scrollHeight - clientHeight, target));
            desiredTop = Math.max(this.UP_SCROLL_THRESHOLD + 5, desiredTop);
            gridEl.scrollTop = desiredTop;
            this.lastScrollTop = desiredTop;
          }
        });
      }

      // Adjust loaded ranges for pages trimmed from the bottom
      const endAfterTrim = this.windowEnd;
      const lastPageKept = Math.floor((endAfterTrim - 1) / this.virtualScrollPageSize);
      const lastPageRemoved = Math.floor((oldWindowEnd - 1) / this.virtualScrollPageSize);
      for (let i = lastPageKept + 1; i <= lastPageRemoved; i++) {
        this.loadedRanges.delete(i);
      }
    }
    this.isAdjustingWindow = false;
  }

  private applySlidingWindowCleanupDownward(): void {
    const currentLength = this.pendingCollectionListSubject.getDataLength();
    if (currentLength <= this.maxWindowSize) return;
    const excessRows = currentLength - this.maxWindowSize;
    const rowsToRemove = excessRows;
    if (rowsToRemove <= 0) return;
    const gridEl = this.getGridContentEl();
    this.isAdjustingWindow = true;
    this.pendingCollectionListSubject.removeFromTop(rowsToRemove);
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
        this.isAdjustingWindow = false;
      });
    } else {
      this.isAdjustingWindow = false;
    }
  }

  private resetGridScrollTop(): void {
    if (this.claimsGrid && this.claimsGrid.wrapper) {
      const scrollContainer = this.claimsGrid.wrapper.nativeElement.querySelector('.k-grid-content');
      if (scrollContainer) {
        scrollContainer.scrollTop = 0;
      }
    }
  }

  private reinitializeAllModeWithFilters(): void {
    this.isAllInitializing = true;
    this.isLoadingMoreData = true;
    this.loadedRanges.clear();
    this.windowStart = 0;
    this.windowEnd = 0;
    this.lastScrollTop = 0;
    this.lastLoadedSkip = -1;
    this.loadedRanges.add(0);

    this.pendingCollectionListSubject.clearBuffer();
    this.resetGridScrollTop();
    this.teardownScrollPrefetch();

    this.loadPendingCollectionVirtual(true, 0);
    this.mySelection = [];
    this.selectedPatientWithLines = [];
    this.selectedClients = [];
    this.selectAllPatients = false;
  }

  private computeFooterRange(): string {
    const total = this.totalInvoiceHeaders || this.pendingCollectionListSubject.getCount();
    const currentLength = this.pendingCollectionListSubject.getDataLength();
    if (!total || total === 0 || currentLength === 0) return '0';
    if (this.isAllSelected) {
      const atEnd = (this.windowStart + currentLength) >= total;
      if (atEnd) {
        const maxBatchesInWindow = Math.floor(this.maxWindowSize / this.virtualScrollPageSize) || 1;
        const lastBatchStart = Math.floor((total - 1) / this.virtualScrollPageSize) * this.virtualScrollPageSize;
        const desiredWindowStart = Math.max(0, lastBatchStart - (maxBatchesInWindow - 1) * this.virtualScrollPageSize);
        const visibleSizeAtEnd = total - desiredWindowStart;
        const start = desiredWindowStart + 1;
        const end = total;
        return `${start}–${end}`;
      }
      // Not at end: if buffer oversize during downward append, offset start to
      // reflect the upcoming cleanup window without flicker.
      const oversize = Math.max(0, currentLength - this.maxWindowSize);
      const startBase = this.windowStart + 1;
      const startOffset = (oversize > 0 && this.lastScrollDirection === 'down') ? oversize : 0;
      const start = startBase + startOffset;
      const visibleSize = Math.min(currentLength, this.maxWindowSize);
      const end = Math.min((start - 1) + visibleSize, total);
      return `${start}–${end}`;
    }
    const skip = this.gridState && this.gridState.skip ? this.gridState.skip : 0;
    const take = this.gridState && this.gridState.take ? this.gridState.take : 20;
    const start = skip + 1;
    const end = Math.min(skip + take, total);
    return `${start}–${end}`;
  }

  private getGridContentEl(): HTMLElement | null {
    return this.claimsGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
  }

  private alignWindowToEnd(totalCount: number): void {
    this.isBufferOperationInProgress = true;
    const gridEl = this.getGridContentEl();
    const currentLength = this.pendingCollectionListSubject.getDataLength();
    const maxBatchesInWindow = Math.floor(this.maxWindowSize / this.virtualScrollPageSize);
    const lastBatchStart = Math.floor((totalCount - 1) / this.virtualScrollPageSize) * this.virtualScrollPageSize;
    const desiredWindowStart = Math.max(0, lastBatchStart - Math.max(0, (maxBatchesInWindow - 1)) * this.virtualScrollPageSize);
    const desiredWindowSize = totalCount - desiredWindowStart;
    const rowsToRemove = Math.max(0, currentLength - desiredWindowSize);
    if (rowsToRemove > 0) {
      this.pendingCollectionListSubject.removeFromTop(rowsToRemove);
    }
    // Always set windowStart to the end-aligned start so upward fetch uses correct skip
    this.windowStart = desiredWindowStart;
    // Keep loadedRanges consistent with the aligned end window
    const startPageIndex = Math.floor(desiredWindowStart / this.virtualScrollPageSize);
    const endPageIndex = Math.floor((totalCount - 1) / this.virtualScrollPageSize);
    const newRanges = new Set<number>();
    for (let i = startPageIndex; i <= endPageIndex; i++) {
      newRanges.add(i);
    }
    this.loadedRanges = newRanges;
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
      }, 100);
    });
  }

  // ===== End Virtual Scroll Methods =====

  toggleFilter(event: boolean) {
    this.filtersComponent.opened = event;
    this.showFilter = event;
    this.view.subscribe(x => {
      this.userList = this.userList.length == 0 ? x.data.map(x =>
      ({
        id: x.id,
        name: x.clientName,
        checked: false
      })) : this.userList;
    });
  }

  columnsSelectorToggle(): void {
    this.showSelectorPopup = !this.showSelectorPopup;
    this.flipSelector = !this.flipSelector;
  }

  onSelectorLeave(state: boolean) {
    this.showSelectorPopup = state;
    this.flipSelector = state;
  }


  changeLineSelections(event: any): void {
    let clientId = event.length ? event[0].clientId : undefined;
    event.forEach(x => {
      if (x.checked && !this.selectedPatientWithLines.includes(x)) {
        this.selectedPatientWithLines.push(x);
      }
      else if (!x.checked && this.selectedPatientWithLines.includes(x)) {
        this.selectedPatientWithLines = this.selectedPatientWithLines.filter(s => s.id != x.id);
        this.selectedClients.remove(x.clientId);
      }
    });
    if (event.billingDetails.length
      != this.selectedPatientWithLines.filter(x => x.clientId == clientId).length) {
      this.view.subscribe(x => {
        x.data.first(s => s.id == clientId).checked = false;
      })

    }
  }

  changePatientSelection(event: any): void {
    this.selectionLinesChanged(event.event, event.patientId);
  }

  selectionLinesChanged(event: any, clientId: number): void {
    let isChecked = event.currentTarget.checked;

    if (clientId == 0) {
      this.view.subscribe(x => {
        if (event.currentTarget != null)
          x.data.forEach(item => {
            item.checked = isChecked;
            this.selectedPatientWithLines.addRange(item.billingDetails);
          });
      });
    } else {
      var item = undefined;
      this.view.subscribe(x => {
        if (event.currentTarget != null)
          item = x.data.find(item => item.id == clientId);
      })
      if (item !== undefined) {
        item.checked = isChecked;
        if (isChecked)
          item.billingDetails.forEach(x => {
            if (!this.selectedPatientWithLines.includes(x)) {
              this.selectedPatientWithLines.push(x);
            }
          })
        else {
          this.selectedPatientWithLines = this.selectedPatientWithLines.filter(x => x.clientId != clientId);
          this.selectedClients.remove(clientId);
        }
      }
    }

    if (isChecked) {
      if (clientId === 0) {
        this.selectedClients = [];
        this.view.subscribe(x => {
          if (event.currentTarget != null)
            x.data.forEach(item => this.selectedClients.push(item.id))
        });
      } else {
        this.selectedClients.push(clientId);
      }
    } else {
      if (clientId === 0) {
        this.selectedClients = [];
        this.selectedPatientWithLines = [];
      } else {
        this.selectedClients.remove(clientId);
      }
    }
    this.view.subscribe(x => {
      if (event.currentTarget != null) {
        if (this.selectedClients.length == x.data.length)
          this.selectAllPatients = true;
        else
          this.selectAllPatients = false;
      }
    })

  }

  selectAllClaimLines(clientId: number): boolean {
    if (this.selectedClients.any((x: number) => x == 0)) {
      return true;
    } else {
      return this.selectedClients.any((x: number) => x == clientId)
    }
  }

  getSelectedPatientLines(clientId: number): any {
    return this.selectedPatientWithLines.filter(x => x.clientId == clientId && x.checked);
  }


  onExpanderClick(index: number, anchor: HTMLAnchorElement, clientId: number) {
    const isExpanded = !anchor.classList.contains('k-plus');

    if (isExpanded) {
      this.IndexList.removeWhere(x => x.clientId == clientId);
      anchor.classList.remove('k-minus');
      anchor.classList.add('k-plus');

      this.claimsGrid.collapseRow(index);
    } else {
      this.IndexList.push({ clientId: clientId, index: index, anchor: anchor });
      anchor.classList.remove('k-plus');
      anchor.classList.add('k-minus');

      this.claimsGrid.expandRow(index);
    }
  }

  onFilterChanged() {
    if (this.isAllSelected) {
      // Reinitialize virtual scroll under current filters
      if (this.filterChangedTimeout) {
        clearTimeout(this.filterChangedTimeout);
      }
      // No artificial latency on filter apply
      this.reinitializeAllModeWithFilters();
      this.gatClaimsTabData = true;
    } else {
      // Normal pagination path - reset window state
      this.windowStart = 0;
      this.windowEnd = 0;
      this.lastLoadedSkip = -1;
      this.gridState.skip = 0;
      if (this.filterChangedTimeout) {
        clearTimeout(this.filterChangedTimeout);
      }
      // No artificial latency on filter apply
      this.loadPatientHeaders();
    }
  }

  onDetailExpand(event: any): void {
    // Update parent breadcrumbs via custom event
    const breadcrumbs = [
      { label: 'Patient Invoicing', url: '/billing/patientinvoicing/list' },
      { label: 'Pending Collection', url: '/billing/patientinvoicing/list' },
      { label: 'Invoice Details', url: '' }
    ];
    window.dispatchEvent(new CustomEvent('updateBreadcrumbs', { detail: breadcrumbs }));
  }

  onDetailCollapse(event: any): void {
    // Update parent breadcrumbs via custom event
    const breadcrumbs = [
      { label: 'Patient Invoicing', url: '/billing/patientinvoicing/list' },
      { label: 'Pending Collection', url: '/billing/patientinvoicing/list' }
    ];
    window.dispatchEvent(new CustomEvent('updateBreadcrumbs', { detail: breadcrumbs }));
  }

  ngOnInit(): void {
    // Listen for grid collapse events from parent
    window.addEventListener('collapseGrids', () => {
      this.collapseAllGridRows();
    });

    this.subscriptions.add(this.route.queryParams.subscribe((params) => {
      if (params.tab) {

        if (params['tab'] === 'pendingCollection') {
          this.isFromPaymentPostingBackButton = true;
        }  

        this.router.navigate([], {
          queryParams: {
            tab: null,
          },
          queryParamsHandling: 'merge',
        });
      }
    }));
  }

  collapseAllGridRows(): void {
    // Collapse all expanded rows in the grid
    if (this.encounterGrid && this.encounterGrid.data) {
      const data = this.encounterGrid.data as any;
      if (data && data.data) {
        data.data.forEach((item: any, index: number) => {
          this.encounterGrid.collapseRow(index);
        });
      }
    }
    // Update breadcrumbs to remove Invoice Details
    const breadcrumbs = [
      { label: 'Patient Invoicing', url: '/billing/patientinvoicing/list' },
      { label: 'Pending Collection', url: '/billing/patientinvoicing/list' }
    ];
    window.dispatchEvent(new CustomEvent('updateBreadcrumbs', { detail: breadcrumbs }));
  }

  ngAfterViewInit(): void {
    
    this.setPaginationSortingState();
    this.setFilterState();
    this.pendingCollectionListSubject.getAllUsers$().subscribe(allUsers => {
        if (!allUsers || allUsers.length === 0) {
          // this.pendingCollectionListSubject.fetchAllUsersFromApi();
          this.userList = [];
          return;
        }
        this.userList = allUsers.map(user => ({
          ...user,
          checked: this.filtersComponent.selectedPatients.some(sel => sel.id === user.id)
        }));
      });

    this.loadPatientHeaders();
    if (this.filterService.isFilterSet) {
      window.setTimeout(res => (<HTMLElement>document.querySelector(".filter-btn .outlined-btn"))!.click(), 1000);
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  public getPageStart(total: number): number {
    if (!total || total === 0) return 0;
    const skip = (this.gridState && this.gridState.skip) ? this.gridState.skip : 0;
    return skip + 1;
  }

  public getPageEnd(total: number): number {
    if (!total || total === 0) return 0;
    const skip = (this.gridState && this.gridState.skip) ? this.gridState.skip : 0;
    const take = (this.gridState && this.gridState.take) ? this.gridState.take : 20;
    return Math.min(skip + take, total);
  }

  getInvoicePrint(invoiceData: any) {
    this.subscriptions.add(this.patientInvoiceService.getInvoicePDFPrint(invoiceData.invoiceNo, invoiceData.clientId).subscribe((result: any) => {
      if (result.errors.length) {
        const clientNotFoundError = 'Error processing client 403934: Client details not found';
        if (result.errors.includes(clientNotFoundError)) {
          this.notificationService.showNotificationError(
            `Failed to generate PDF: Client details not found for ${result.errors.length} client(s).`
          );
        }
        else {
          this.notificationService.showNotificationError("Failed to generate PDF invoices for " + result.errors.length + " client(s).");
        }     
      }
      if (result.pdfBase64 != null) {
        if (result.errors.length) {
          window.setTimeout(() => {
            this.printInvoiceShared.processPDF(result);
          }, 1000);
        }
        else {
          this.printInvoiceShared.processPDF(result);
        }
      }

    },
      error => {
        this.notificationService.showNotificationError("Failed to generate PDF invoices: Client details not found");
      }));
  }

  getGridPageSizes(): void {
    const storedGridPageSizes = localStorage.getItem('gridPageSizes') ? JSON.parse(localStorage.getItem('gridPageSizes') || '') : null;
    if (storedGridPageSizes) {
      this.gridPageSizes = storedGridPageSizes;
    } else {
      this.subscriptions.add(this.claimsService.getGridPageSizes().subscribe((sizes: Array<number | { text: string; value: number }>) => {
        this.gridPageSizes = sizes;
      }));
    }
  }

  openPaymentDialog(dataItem: any) {
    const searchParams: ManualPaymentPatientSearchBase = {
       personName: dataItem.clientName,
       accountInfoId: this.accountService.memberDetails.accountInfoId
      };

    this.paymentPostingService.getPatients(searchParams).subscribe(
      (patients: ManualPaymentPatientSearch[]) => {
        if (patients.length === 0) {
          this.showPaymentDialog = false;
          this.notificationService.showNotificationError('Selected Client Does Not Exist In BH System.');
          return;
        }
        
        if (localStorage.getItem('pendingCollectionPagination')) {
          localStorage.removeItem('pendingCollectionPagination');
        }
        if (localStorage.getItem('pendingCollectionAppliedFilters')) {
          localStorage.removeItem('pendingCollectionAppliedFilters');
        }
        
        const paginationState = {
          skip: this.gridState.skip,
          take: this.gridState.take,
          sort: this.gridState.sort
        };
        localStorage.setItem('pendingCollectionPagination', JSON.stringify(paginationState));

        if(this.filterService.isFilterSet) {
          const appliedFilters = {
            clientIds: this.filtersComponent.selectedPatients.map(x => x.id).join(','),
            dateOfServiceFrom: Helper.shiftDateToUTC(this.filtersComponent.dateOfServiceFrom),
            dateOfServiceTo: Helper.shiftDateToUTC(this.filtersComponent.dateOfServiceTo),
            invoiceFrom: Helper.shiftDateToUTC(this.filtersComponent.invoiceFrom),
            invoiceTo: Helper.shiftDateToUTC(this.filtersComponent.invoiceTo),
            paymentDueFrom: Helper.shiftDateToUTC(this.filtersComponent.paymentDueFrom),
            paymentDueTo: Helper.shiftDateToUTC(this.filtersComponent.paymentDueTo),
            patientResponsibilityFrom: this.filtersComponent.patientResponsibilityFrom,
            patientResponsibilityTo: this.filtersComponent.patientResponsibilityTo
          };
          localStorage.setItem('pendingCollectionAppliedFilters', JSON.stringify(appliedFilters));
        }

        this.selectedPaymentData = {
          patientId: dataItem.id,
          patientBalance: 0
        };
        this.showPaymentDialog = true;
      },
      error => {
        this.showPaymentDialog = false;
        //this.notificationService.showNotificationError('Error checking patient existence.');
      }
    );
}


  closePaymentDialog() {
    this.showPaymentDialog = false;
    this.selectedPaymentData = null;
    if (localStorage.getItem('pendingCollectionPagination')) {
      localStorage.removeItem('pendingCollectionPagination');
    }
  }

  setPaginationSortingState() {
    const savedPagination = localStorage.getItem('pendingCollectionPagination');
    if (savedPagination && this.isFromPaymentPostingBackButton) {
      const pagination = JSON.parse(savedPagination);
      this.gridState.skip = pagination.skip || 0;
      this.gridState.take = pagination.take || 20;
      this.gridState.sort = pagination.sort || [];
      localStorage.removeItem('pendingCollectionPagination');
    }
    else {
      if (localStorage.getItem('pendingCollectionPagination')) {
        localStorage.removeItem('pendingCollectionPagination');
      }
    }
  }

  setFilterState() {
    const savedFilters = localStorage.getItem('pendingCollectionAppliedFilters');

    if (savedFilters && this.isFromPaymentPostingBackButton) {
      const appliedFilters = JSON.parse(savedFilters);
      this.filtersComponent.selectedPatients = appliedFilters.clientIds ? appliedFilters.clientIds.split(',').map((id: string) => ({ id: Number(id), name: '' })) : [];
      this.filtersComponent.dateOfServiceFrom = appliedFilters.dateOfServiceFrom ? new Date(appliedFilters.dateOfServiceFrom) : null;
      this.filtersComponent.dateOfServiceTo = appliedFilters.dateOfServiceTo ? new Date(appliedFilters.dateOfServiceTo) : null;
      this.filtersComponent.invoiceFrom = appliedFilters.invoiceFrom ? new Date(appliedFilters.invoiceFrom) : null;
      this.filtersComponent.invoiceTo = appliedFilters.invoiceTo ? new Date(appliedFilters.invoiceTo) : null;
      this.filtersComponent.paymentDueFrom = appliedFilters.paymentDueFrom ? new Date(appliedFilters.paymentDueFrom) : null;
      this.filtersComponent.paymentDueTo = appliedFilters.paymentDueTo ? new Date(appliedFilters.paymentDueTo) : null;
      this.filtersComponent.patientResponsibilityFrom = appliedFilters.patientResponsibilityFrom || null;
      this.filtersComponent.patientResponsibilityTo = appliedFilters.patientResponsibilityTo || null;
      localStorage.removeItem('pendingCollectionAppliedFilters');
    }
    else {
      if (localStorage.getItem('pendingCollectionAppliedFilters')) {
        localStorage.removeItem('pendingCollectionAppliedFilters');
      }
    }
  }

}
