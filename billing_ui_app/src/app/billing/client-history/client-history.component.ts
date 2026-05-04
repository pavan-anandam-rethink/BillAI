import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  forwardRef,
  NgZone,
  OnDestroy,
  OnInit,
  Renderer2,
  ViewChild,
} from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ClaimHistoryListFilterComponent } from '../encounters/encounter-view/claim-history-list-sort-filter/claim-history-list-filter.component';
import { ListFilterSort, PaymentPosting } from '@core/models/billing/';
import { GridFilterOperators, NotificationTypes } from '@core/enums/common';
import { ConfirmDialog, GridFilterModel } from '@core/models/common';
import { PaymentPostingService } from '@core/services/billing/payment-posting.service';
import {
  DialogService,
  DialogCloseResult,
} from '@progress/kendo-angular-dialog';
import {
  GridComponent,
  GridDataResult,
  PageChangeEvent,
  PagerSettings,
  SelectableMode,
  SelectableSettings,
  SelectionEvent,
} from '@progress/kendo-angular-grid';
import { SortDescriptor, State } from '@progress/kendo-data-query';
import { Observable, Subject, Subscription, filter, fromEvent, throttleTime } from 'rxjs';
import  { debounceTime, distinctUntilChanged, map, take, tap } from 'rxjs/operators';
import { AccountPermissions } from '@core/enums/account';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { SidebarService } from '@app/shared/components/sidebar';
import { PaymentPostingListSubject } from '@core/subjects/payment-posting-list.subject';
import {
  AddClaimNotesDialogComponent,
  AddClaimNotesDialogResult,
} from '@app/billing/encounters/ecnounter-list/add-claim-notes-dialog/add-claim-notes-dialog.component';
import { PaymentNotesService } from '@core/services/billing/payment-notes-service';
import { PaymentNoteSaveModel } from '@core/models/billing/notes/payment-posting-note';

import { NotificationService } from '@progress/kendo-angular-notification';
import { PaginationService } from '@core/services/billing/pagination.service';
import { ActivatedRoute, Router } from '@angular/router';
import { PaymentPostingFilterService } from '@app/billing/services/payment-posting-filter.service';
import { DatePipe } from '@angular/common';
import { ClaimsManagementFilterService } from '@app/billing/services/claims-management-filter.service';
import { CreateInvoiceFilterService } from '@app/billing/services/create-invoice-filter.service';
import { PendingCollectionFilterService } from '@app/billing/services/pending-collection-filter.service';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { ClaimService } from '@core/services/billing';
import { ClientHistoryListFilterComponent } from './client-history-list/client-history-list-filter/client-history-list-filter.component';
import { ClientHistoryService } from '@core/services/billing/client-history.service';
import { ClientHistoryListSubject } from '@core/subjects/client-history-list.subject';
import { ClientHistoryChargeComponent } from './client-history-charge/client-history-charge.component';
import { ClientHistoryChargeFilterComponent } from './client-history-list/client-history-list-filter/client-history-charge-filter/client-history-charge-filter.component';
import {
  ClientHistory,
  ClientHistoryGrid,
  ClientHistoryRequestModel,
} from '@core/models/billing/client-history';
import { ClientHistoryFilterService } from '../services/client-history-filter.service';
import { ClientHistorySubject } from '@core/subjects/client-history-subject';
import { Helper } from '../encounters/common/common-helper';
import { Breadcrumb } from '@core/models/billing/bread-crumb';

@Component({
  selector: 'app-client-history',
  templateUrl: './client-history.component.html',
  styleUrls: ['./client-history.component.css'],
})
export class ClientHistoryComponent
  implements OnInit, AfterViewInit, OnDestroy
{
  filterForm: FormGroup;
  @ViewChild(forwardRef(() => ClientHistoryListFilterComponent))
  filtersComponent: ClientHistoryListFilterComponent;
  private unsubscribeAll$ = new Subject<void>();
  public isSubjectLoading$: Observable<boolean>;
  private filterService: ClientHistoryFilterService;
  private unsubscribe = new Subject();
  showFilter = false;
  showActions = false;
  gridPageSizes: any;
  clientName: string;
  clientIds: number[];
  dob: Date;
  location: number[];
  primaryFunder: number[];
  secondaryFunder: string;
  dateOfService: Date;
  placeOfService: string;
  renderingProvider: string;
  claimIdentifier: number;
  billedAmount: number;
  insurancePaidAmount: number;
  patientPaidAmount: number;
  remaningClaimBalance: number;
  gender: string;
  age: number;
  address: string;
  accountInfoId: number;
  memberId: number;
  skip?: number;
  take?: 20;
  sortingModels: SortDescriptor[] = [];
  data: ClientHistory[] | null = null;
  readonly pagingSettings: PagerSettings = {
    buttonCount: 5,
    type: 'numeric',
    pageSizes: true,
    previousNext: true,
  };

  dateOfBirth?: Date | null;
  subscriptions = new Subscription();
  @ViewChild(forwardRef(() => ClaimHistoryListFilterComponent))
  cliHistoryFilter: ClientHistoryListFilterComponent;
  view: Observable<GridDataResult>;
  @ViewChild(GridComponent) paymentPostingGrid: GridComponent;
  public selectedIds: number[] = [];
  clientHistoryListSubject: ClientHistoryListSubject;
  @ViewChild('paymentPostingGrid', { static: true }) grid: any;
  public mode: SelectableMode = 'multiple';
  footerRange: string = '0';

  gridState: State = {
    sort: [{ dir: 'desc', field: 'clientId' }],
    skip: 0,
    take: 20,

    filter: {
      logic: 'and',
      filters: [],
    },
  };

  clientHistorySubject: ClientHistorySubject;

  // =======================
  //  NEW – VIRTUAL SCROLL STATE
  // =======================
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
  private isFilterApplied: boolean = false;
  private readonly UP_SCROLL_THRESHOLD = 150; // px from top to trigger upward load

  constructor(
    private fb: FormBuilder,
    private ngZone: NgZone,
    private paginationServices: PaginationService,
    private claimsService: ClaimService,
    private clientHistoryService: ClientHistoryService,
    private accountService: AccountMemberService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {
    this.getGridPageSizes();

    // Use ListSubject for virtual scroll (like PaymentPosting)
    this.clientHistoryListSubject = new ClientHistoryListSubject(
      this.clientHistoryService
    );
    this.isSubjectLoading$ = this.clientHistoryListSubject.getLoading();

    this.view = this.clientHistoryListSubject.pipe(
      map((data) => ({ data, total: this.clientHistoryListSubject.getCount() }))
    );

    // Update footer only when data buffer changes (append/prepend/remove)
    this.subscriptions.add(
      this.clientHistoryListSubject.subscribe(() => {
        // Avoid updating footer on buffer clear during init
        if ((this.isAllInitializing && this.clientHistoryListSubject.getDataLength() === 0) || this.isBufferOperationInProgress) {
          return;
        }
        this.footerRange = this.computeFooterRange();
      })
    );
  }
  @ViewChild(forwardRef(() => ClientHistoryListFilterComponent))
  clientHistoryListFilterComponent: ClientHistoryListFilterComponent;
  headerText = 'Client History';

  readonly selectableSettings: SelectableSettings = {
    checkboxOnly: true,
    mode: this.mode,
  };
  reporting;

  breadcrumbs: Breadcrumb[] = [
    { label: 'Client History', url: '/billing/clienthistory/list' },
  ];

  ngOnInit(): void {
    // Initialize filter form with default controls
    this.filterForm = this.fb.group({
      location: this.fb.group({
        inputData: [''],
        value: [''],
        operator: [GridFilterOperators.equalAny],
      }),
      clientName: this.fb.group({
        value: [''],
        operator: [GridFilterOperators.rangeDate],
      }),
      dateOfBirth: this.fb.group({
        inputData: [''],
        value: [''],
        operator: [GridFilterOperators.equalAny],
      }),
    });

    // this.loadDataForClientHistory();
  }

  ngAfterViewInit() {
    //this.filtersComponent.OnInitclearFilters();
    this.toggleFilter(true);
  }

  ngOnDestroy() {
    this.unsubscribe.next(void 0);
    this.unsubscribe.complete();
    this.teardownScrollPrefetch();
  }

  toggleFilter(event: boolean) {
    this.showFilter = event;

    this.clientHistoryListFilterComponent.opened = event;
  }

  onFilterChanged() {
    this.location = this.clientHistoryListFilterComponent.isAllLocationSelected  ? []   : this.clientHistoryListFilterComponent.selectedLocation.map((x) => x.id);
    this.clientIds = this.clientHistoryListFilterComponent.isAllClientSelected ? []  : this.clientHistoryListFilterComponent.selectedPatients.map((x) => x.id);
    this.primaryFunder = this.clientHistoryListFilterComponent.isAllFunderSelected ? []  : this.clientHistoryListFilterComponent.selectedfunders.map((x) => x.id);
    const dobVal = this.clientHistoryListFilterComponent.filterForm.get('dateOfBirth')?.value as Date | null | undefined;
    this.dateOfBirth = dobVal ? new Date(Helper.shiftDateToUTC(dobVal) as Date) : null;

    // Reset to first page when filters change
    this.gridState.skip = 0;
    this.isFilterApplied = true;
    if (this.isAllSelected) {
      // Keep pager in ALL mode, reinit virtual window
      this.gridState.take = 9999;
      this.paginationServices.setPageSizes(0);
      this.reinitializeAllModeWithFilters();
      return;
    }
    this.loadDataForClientHistory();
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

  onSortChange(sortParams: SortDescriptor[]): void {
    if (!this.isFilterApplied) {
      return;
    }
    this.gridState.sort = sortParams;
    this.gridState.skip = 0;
    // Sorting in ALL mode must NOT load everything
    if (this.isAllSelected) {
      this.loadedRanges.clear();
      this.windowStart = 0;
      this.windowEnd = 0;
      this.lastLoadedSkip = -1;
      this.isAllInitializing = true;
      this.teardownScrollPrefetch();
      // Keep pager in ALL mode while using virtual scroll
      this.paginationServices.setPageSizes(0);
      this.loadVirtualScrollInitial();
      return;
    }
    // Normal pagination
    this.loadDataForClientHistory();
  }

  loadData(params: State): void {
    //this.applySelectedFilters();
    this.isSubjectLoading$ = this.clientHistoryListSubject.getLoading();
    this.filterService.setFilter2(this.clientHistoryListFilterComponent);
    //this.clientHistoryListSubject.getAll(params);
  }

  public onSelectChange(event: SelectionEvent): void {
    this.selectedIds.addRange(event.selectedRows.select((x) => x.dataItem.id));
    event.deselectedRows.forEach((x) => this.selectedIds.remove(x.dataItem.id));
  }
  public isRowSelected(row: PaymentPosting): boolean {
    return this.selectedIds.indexOf(row.id) >= 0;
  }
  onPageChange(event: PageChangeEvent): void {
    if (!this.isFilterApplied) {
      return;
    }
    // Enter ALL mode when page size is 'All'
    if (event.take === 0) {
      if (!this.isAllSelected) {
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

    // Exit ALL mode when a numeric page size is selected
    if (this.isAllSelected && event.take > 0) {
      this.exitAllModeToPaged(event.take);
      return;
    }

    // Normal paged mode page change
    this.gridState.skip = event.skip;
    this.gridState.take = event.take;
    this.paginationServices.setPageSizes(this.gridState.take);
    this.loadDataForClientHistory();
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
    this.clientHistoryListSubject.clearBuffer();
    this.gridState.skip = 0;
    this.gridState.take = take;
    this.paginationServices.setPageSizes(take);
    this.footerRange = this.computeFooterRange();
    this.loadDataForClientHistory();
  }

  public getPageStart(total: number): number {
    if (!total || total === 0) return 0;
    if (this.isAllSelected) return 1;
    const skip =
      this.gridState && this.gridState.skip ? this.gridState.skip : 0;
    return skip + 1;
  }

  public getPageEnd(total: number): number {
    if (!total || total === 0) return 0;
    if (this.isAllSelected)
      return this.clientHistoryListSubject.getDataLength();
    const skip =
      this.gridState && this.gridState.skip ? this.gridState.skip : 0;
    const take =
      this.gridState && this.gridState.take ? this.gridState.take : 20;
    return Math.min(skip + take, total);
  }

  clickClientChargeHistory(clientId: number) {
    var clientData: any;
    this.view.subscribe((x) => {
      clientData = x.data.find((y) => y.clientId === clientId);
    });
    this.accountService.memberDetails.clientId = clientId;
    this.clientHistoryService.setClientData(clientData);
    this.router.navigate([`/billing/clienthistory/charge/${clientId}`]);
  }

  loadDataForClientHistory() {
    const model = this.buildRequestModel(this.gridState.skip, this.gridState.take);
    // Use ListSubject; virtual mode is controlled via isAllSelected
    this.clientHistoryListSubject.getAll(model, this.isAllSelected);
  }

  private computeFooterRange(): string {
    const total = this.clientHistoryListSubject.getCount();
    const currentLength = this.clientHistoryListSubject.getDataLength();
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

  private buildRequestModel(skip: number, take: number): ClientHistoryRequestModel {
    return {
      ClientRecordFilterModel: {
        locationId: this.location && this.location.length > 0 ? this.location : [],
        clientId: this.clientIds && this.clientIds.length > 0 ? this.clientIds : [],
        funderId: this.primaryFunder && this.primaryFunder.length > 0 ? this.primaryFunder : [],
        dateOfBirth: this.dateOfBirth,
      },
      ClientHistoryRequest: {
        accountInfoId: this.accountService.memberDetails.accountInfoId,
        memberId: this.accountService.memberDetails.memberId,
        skip: skip,
        take: take,
        sortingModels: this.gridState.sort,
      },
    } as ClientHistoryRequestModel;
  }

  private loadVirtualScrollInitial(): void {
    const params = this.buildRequestModel(0, this.virtualScrollPageSize);
    this.loadedRanges.add(0);
    // Reset scroll state and duplicate guard
    this.lastLoadedSkip = -1;
    this.lastScrollDirection = null;
    this.lastScrollTop = 0;
    this.clientHistoryListSubject.getAll(params, true);
    setTimeout(() => {
      this.isAllInitializing = false;
      this.setupScrollPrefetch();
    }, 100);
  }

  private setupScrollPrefetch(): void {
    this.teardownScrollPrefetch();
    const gridEl =
      this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector(
        '.k-grid-content'
      );
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
          // Don't trigger if at absolute top
          if (scrollTop < 20 && this.windowStart === 0) {
            return;
          }
          if (scrollPercentage >= this.prefetchThreshold && direction === 'down') {
            const totalCount = this.clientHistoryListSubject.getCount();
            const currentData = this.clientHistoryListSubject.value || [];
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

    const currentData = this.clientHistoryListSubject.value || [];
    const currentLength = currentData.length;
    const totalCount = this.clientHistoryListSubject.getCount();
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
    const currentLength = (this.clientHistoryListSubject.value || []).length;
    if (currentLength >= this.maxWindowSize) {
      this.loadBatchWithCleanup(params, 'up');
    } else {
      this.loadBatchUpward(params);
    }
  }

  private loadBatch(params: ClientHistoryRequestModel, append: boolean): void {
    this.isLoadingMoreData = true;
    const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    const scrollTopBefore = gridEl?.scrollTop || 0;
    const beforeLength = this.clientHistoryListSubject.getDataLength();
    if (append) {
      this.clientHistoryListSubject.append(params);
    } else {
      this.clientHistoryListSubject.getAll(params, this.isAllSelected);
    }
    this.clientHistoryListSubject
      .pipe(
        filter(arr => (arr?.length || 0) > beforeLength),
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

  private loadBatchUpward(params: ClientHistoryRequestModel): void {
    this.isLoadingMoreData = true;
    const beforeLength = this.clientHistoryListSubject.getDataLength();
    this.clientHistoryListSubject.prependBatch(params, false);
    const amountPrepended = this.virtualScrollPageSize;
    this.windowStart = Math.max(0, this.windowStart - amountPrepended);
    this.clientHistoryListSubject
      .pipe(
        filter(arr => (arr?.length || 0) > beforeLength),
        take(1)
      )
      .subscribe(() => {
        const currentLengthAfterPrepend = this.clientHistoryListSubject.getDataLength();
        if (currentLengthAfterPrepend > this.maxWindowSize) {
          const excessRows = currentLengthAfterPrepend - this.maxWindowSize;
          this.clientHistoryListSubject.removeFromBottom(excessRows);
        }
        // After upward prepend + possible trim, reset scrollbar to safe middle
        const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
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

  private loadBatchWithEndAlignment(params: ClientHistoryRequestModel): void {
    this.isLoadingMoreData = true;
    this.isBufferOperationInProgress = true;
    const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    const beforeLength = this.clientHistoryListSubject.getDataLength();
    const totalCount = this.clientHistoryListSubject.getCount();
    this.clientHistoryListSubject.append(params);
    this.clientHistoryListSubject
      .pipe(
        filter(arr => (arr?.length || 0) > beforeLength),
        take(1)
      )
      .subscribe(() => {
        this.ngZone.run(() => {
          const currentData = this.clientHistoryListSubject.value || [];
          let currentLength = currentData.length;
          if (currentLength > this.maxWindowSize) {
            const maxBatchesInWindow = Math.floor(this.maxWindowSize / this.virtualScrollPageSize);
            const lastBatchStart = Math.floor((totalCount - 1) / this.virtualScrollPageSize) * this.virtualScrollPageSize;
            const desiredWindowStart = Math.max(0, lastBatchStart - (maxBatchesInWindow - 1) * this.virtualScrollPageSize);
            const desiredWindowSize = totalCount - desiredWindowStart;
            const rowsToRemove = currentLength - desiredWindowSize;
            if (rowsToRemove > 0) {
              this.clientHistoryListSubject.removeFromTop(rowsToRemove);
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

  private loadBatchWithCleanup(params: ClientHistoryRequestModel, direction: 'up' | 'down'): void {
    this.isLoadingMoreData = true;
    this.isBufferOperationInProgress = true;
    if (direction === 'down') {
      const gridEl = this.getGridContentEl();
      if (!gridEl) {
        this.isLoadingMoreData = false;
        this.isBufferOperationInProgress = false;
        return;
      }
      
      const actualRowHeight = this.getActualRowHeight(gridEl);
      const scrollTopBefore = gridEl.scrollTop;
      
      const beforeLength = this.clientHistoryListSubject.getDataLength();
      this.clientHistoryListSubject.append(params);
      
      this.clientHistoryListSubject
        .pipe(
          filter(arr => (arr?.length || 0) > beforeLength),
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
      const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
      const beforeLength = this.clientHistoryListSubject.getDataLength();
      this.clientHistoryListSubject.prependBatch(params, false);
      const amountPrepended = this.virtualScrollPageSize;
      this.windowStart = Math.max(0, this.windowStart - amountPrepended);
      this.clientHistoryListSubject
        .pipe(
          filter(arr => (arr?.length || 0) > beforeLength),
          take(1)
        )
        .subscribe(() => {
          this.ngZone.run(() => {
            requestAnimationFrame(() => {
              const currentData = this.clientHistoryListSubject.value || [];
              const currentLength = currentData.length;
              if (currentLength > this.maxWindowSize) {
                const excessRows = currentLength - this.maxWindowSize;
                this.clientHistoryListSubject.removeFromBottom(excessRows);
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
    const currentData = this.clientHistoryListSubject.value || [];
    const currentLength = currentData.length;

    if (currentLength <= this.maxWindowSize) return;

    const excessRows = currentLength - this.maxWindowSize;
    const rowsToRemove = excessRows;

    if (rowsToRemove <= 0) return;

    const gridEl = this.getGridContentEl();
    
    // Remove rows and update window
    this.clientHistoryListSubject.removeFromTop(rowsToRemove);
    this.windowStart += rowsToRemove;
    
    // Reset scrollbar to middle position to prevent triggering upward API
    if (gridEl) {
      requestAnimationFrame(() => {
        const clientHeight = gridEl.clientHeight || 0;
        const scrollHeight = gridEl.scrollHeight || 0;
        if (clientHeight > 0 && scrollHeight > 0) {
          // Position at 40% to give buffer before upward threshold (150px)
          const target = scrollHeight * this.downwardResetPercentage;
          const desiredTop = Math.max(0, Math.min(scrollHeight - clientHeight, target));
          gridEl.scrollTop = desiredTop;
          this.lastScrollTop = desiredTop;
        }
      });
    }
  }

  private applySlidingWindowCleanup(direction: 'up' | 'down'): void {
    const currentData = this.clientHistoryListSubject.value || [];
    const currentLength = currentData.length;
    if (currentLength <= this.maxWindowSize) return;
    const excessRows = currentLength - this.maxWindowSize;
    const rowsToRemove = excessRows;
    if (rowsToRemove <= 0) return;
    const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    const scrollTopBefore = gridEl?.scrollTop || 0;
    let actualRowHeight = 40;
    if (gridEl) {
      const firstRow = gridEl.querySelector('tbody tr');
      if (firstRow) {
        actualRowHeight = firstRow.getBoundingClientRect().height;
      }
    }
    if (direction === 'down') {
      this.clientHistoryListSubject.removeFromTop(rowsToRemove);
      this.windowStart += rowsToRemove;
      if (gridEl) {
        requestAnimationFrame(() => {
          const scrollAdjustment = rowsToRemove * actualRowHeight;
          const newScrollTop = Math.max(0, scrollTopBefore - scrollAdjustment);
          gridEl.scrollTop = newScrollTop;
          this.lastScrollTop = newScrollTop;
        });
      }
    } else {
      this.clientHistoryListSubject.removeFromBottom(rowsToRemove);
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
    }
  }

  getFooterRange(): string {
    if (!this.isAllSelected) {
      return '0';
    }
    const currentData = this.clientHistoryListSubject.value || [];
    const total = this.clientHistoryListSubject.getCount();
    if (currentData.length === 0) {
      return '0';
    }
    const start = this.windowStart + 1;
    const end = Math.min(this.windowStart + currentData.length, total);
    return `${start}–${end} of ${total}`;
  }

  private teardownScrollPrefetch(): void {
    if (this.scrollPrefetchSub) {
      this.scrollPrefetchSub.unsubscribe();
      this.scrollPrefetchSub = null;
    }
  }

  private getGridContentEl(): HTMLElement | null {
    return this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
  }

  private getActualRowHeight(gridEl: HTMLElement): number {
    const firstRow = gridEl.querySelector('tbody tr');
    return firstRow ? firstRow.getBoundingClientRect().height : 40;
  }

  private reinitializeAllModeWithFilters(): void {
    this.isAllInitializing = true;
    this.isLoadingMoreData = false;
    this.loadedRanges.clear();
    this.windowStart = 0;
    this.windowEnd = 0;
    this.lastScrollDirection = null;
    this.lastScrollTop = 0;
    this.lastLoadedSkip = -1;
    this.clientHistoryListSubject.clearBuffer();
    this.gridState.skip = 0;
    this.paginationServices.setPageSizes(0);
    this.loadVirtualScrollInitial();
  }
}
