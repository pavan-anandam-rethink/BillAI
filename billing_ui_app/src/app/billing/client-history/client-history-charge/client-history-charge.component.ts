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
import { ClaimHistoryListFilterComponent } from '@app/billing/encounters/encounter-view/claim-history-list-sort-filter/claim-history-list-filter.component';
import { ListFilterSort, PaymentPosting } from '@core/models/billing/';
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
import { filter, forkJoin, map, Observable, Subject, Subscription, take, fromEvent, throttleTime } from 'rxjs';
import { PaginationService } from '@core/services/billing/pagination.service';
import { ClaimService } from '@core/services/billing';
import { ClientHistoryService } from '@core/services/billing/client-history.service';
import { ClientHistoryListSubject } from '@core/subjects/client-history-list.subject';
import { AccountPermissions } from '@core/enums/account/account-permissions';
import {
  ClientHistory,
  ClientHistoryChargeDetails,
  ClientHistoryChargeDetailsRequest,
  ClientHistoryChargeDetailsRequestModel,
  ClientHistoryChargeDetailsResponse,
} from '@core/models/billing/client-history';
import { ClientHistoryChargeFilterComponent } from '../client-history-list/client-history-list-filter/client-history-charge-filter/client-history-charge-filter.component';
import { ActivatedRoute } from '@angular/router';
import { GridFilterOperators } from '@core/enums/common';
import { GridFilterModel } from '@core/models/common';
import { PaymentPostingListFilterComponent } from '@app/billing/payment-posting';
import { ClientHistoryFilterService } from '@app/billing/services/client-history-filter.service';
import { ClientChargeHistorySubject } from '@core/subjects/client-charge-history-subject';
import { debug } from 'console';
import { DatePipe } from '@angular/common';
import { LoaderService } from '@core/services/common/loader.service';
import { ReportService } from '@core/services/billing/report.service';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { Breadcrumb } from '@core/models/billing/bread-crumb';
import { AccountMemberService } from '@core/services/account/account-member.service';

@Component({
  selector: 'app-client-history-charge',
  templateUrl: './client-history-charge.component.html',
  styleUrls: ['./client-history-charge.component.css'],
})
export class ClientHistoryChargeComponent
  implements OnInit, AfterViewInit, OnDestroy
{
  headerText = 'Client History';
  public mode: SelectableMode = 'multiple';
  readonly selectableSettings: SelectableSettings = {
    checkboxOnly: true,
    mode: this.mode,
  };
  private unsubscribeAll$ = new Subject<void>();
  public isSubjectLoading$: Observable<boolean>;
  filterForm: FormGroup;
  showFilter = false;
  showActions = false;
  gridPageSizes: any;
  fromDOS: string;
  toDOS: string;
  placeOfService?: number[];
  renderingProvider?: number[];
  authorizationNumber?: number[];
  modifiers: string[];
  diagnosis: string;
  primaryFunder?: number[];
  primaryClaimId: string;
  claimStatus: string;
  hours: number;
  units: number;
  perUnitCharge: number;
  billedAmount: number;
  insurancePayments: number;
  adjustments: number;
  patientResponsibilityAdjustments: number;
  claimBalance: number;
  invoiceNumber: string;
  invoiceStatus: string;
  patientResponsibility: number;
  patientPayments: number;
  patientBalance: number;
  selectedClientDetails: any;
  fromDate: Date;
  throughDate: Date;
  selectedTabIndex = 0; // 0 = Charges, 1 = Invoices
  canEdit: boolean = false;
  gridState: State = {
    sort: [{ dir: 'desc', field: 'dateOfService' }],
    skip: 0,
    take: 20,

    filter: {
      logic: 'and',
      filters: [],
    },
  };

  readonly pagingSettings: PagerSettings = {
    buttonCount: 5,
    type: 'numeric',
    pageSizes: true,
    previousNext: true,
  };
  @ViewChild('paymentPostingGrid') grid: GridComponent;
  @ViewChild(GridComponent) paymentPostingGrid: GridComponent;
  @ViewChild(forwardRef(() => PaymentPostingListFilterComponent))
  paymentPostingListFilterComponent: PaymentPostingListFilterComponent;
  data: ClientHistoryChargeDetails[] = [];

  gridData$: Observable<GridDataResult>;
  gridView: GridDataResult;
  clientHistoryListSubject: ClientHistoryListSubject;
  subscription = new Subscription();
  totalRecords = 0;
  breadcrumbs: Breadcrumb[];
  pageSize = 20;
  sort: SortDescriptor[] = [];

  isLoading = false;
  canViewPatientInvoicing = false;

  selectedRows: PaymentPosting[] = [];
  private searchTerms = new Subject<string>();
  accountPermissions = AccountPermissions;
  public selectedIds: number[] = [];
  client: ClientHistoryChargeDetails | null = null;
  clientId: ClientHistoryChargeDetailsRequest | null = null;
  view: Observable<GridDataResult>;
  subscriptions = new Subscription();
  clientChargeHistorySubject: ClientChargeHistorySubject;
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

  constructor(
    private fb: FormBuilder,
    private ngZone: NgZone,
    private paginationServices: PaginationService,
    private claimsService: ClaimService,
    private clientHistoryService: ClientHistoryService,
    private route: ActivatedRoute,
    private filterService: ClientHistoryFilterService,
    private datePipe: DatePipe,
    private reportingService: ReportService,
    private loaderService: LoaderService,
    private accountMemberService: AccountMemberService,
    private accountService: AccountMemberService,
    private cdr: ChangeDetectorRef
  ) {
    this.subscriptions.add(
      this.accountMemberService.accountMemberSettings.subscribe((x) => {
        if (x) {
          this.canViewPatientInvoicing =
            this.accountMemberService.checkPermissionLevel(
              AccountPermissions.BillingReopenEncounter
            );
        }
      })
    );

    this.selectedClientDetails = this.clientHistoryService.getClientData();
    this.breadcrumbs = [
      {
        label: 'Client History',
        url: '/billing/clienthistory/list',
        isReportsPage: false,
      },
      {
        label: 'Charges',
        url: `/billing/clienthistory/charge/${this.selectedClientDetails.clientId}`,
        isReportsPage: false,
      },
      {
        label: 'Invoices',
        url: `/billing/clienthistory/invoice/${this.selectedClientDetails.clientId}`,
        isReportsPage: false,
      },
    ];

    this.getGridPageSizes();
    this.clientChargeHistorySubject = new ClientChargeHistorySubject(
      this.clientHistoryService
    );
    this.isSubjectLoading$ = this.clientChargeHistorySubject.getLoading();

    this.view = this.clientChargeHistorySubject.pipe(
      map((data) => ({
        data,
        total: this.clientChargeHistorySubject.getCount(),
      }))
    );
    // Footer updates only on buffer changes
    this.subscriptions.add(
      this.clientChargeHistorySubject.subscribe(() => {
        if ((this.isAllInitializing && this.clientChargeHistorySubject.getDataLength() === 0) || this.isBufferOperationInProgress) {
          return;
        }
        this.footerRange = this.computeFooterRange();
      })
    );
  }

  updateBreadcrumbs(): void {
    if (!this.selectedClientDetails) {
      return;
    }
    if (this.selectedTabIndex === 0) {
      this.breadcrumbs = [
        {
          label: 'Client History',
          url: '/billing/clienthistory/list',
          isReportsPage: false,
        },
        {
          label: 'Charges',
          url: `/billing/clienthistory/charge/${this.selectedClientDetails.clientId}`,
          isReportsPage: false,
        },
      ];
    } else {
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
    }
  }

  @ViewChild(forwardRef(() => ClientHistoryChargeFilterComponent))
  clientHistoryChargeFilterComponent: ClientHistoryChargeFilterComponent;
  clientIds: number | null = null;
  ngOnInit(): void {
    this.filterForm = this.fb.group({
      dos: this.fb.group({
        inputData: [''],
        value: [''],
        operator: [GridFilterOperators.equalAny],
      }),
      pos: this.fb.group({
        value: [''],
        operator: [GridFilterOperators.rangeDate],
      }),
      renderingProvider: this.fb.group({
        inputData: [''],
        value: [''],
        operator: [GridFilterOperators.equalAny],
      }),
      authorizationNumber: this.fb.group({
        inputData: [''],
        value: [''],
        operator: [GridFilterOperators.equalAny],
      }),

      primaryFunder: this.fb.group({
        value: [''],
        operator: [GridFilterOperators.equalAny],
      }),
    });
    this.route.url.subscribe((segments) => {
      const lastSegment =
        segments[segments.length - 2]?.path ||
        segments[segments.length - 1]?.path;
      if (lastSegment === 'invoice') {
        this.selectedTabIndex = 1;
      } else {
        this.selectedTabIndex = 0;
      }
      this.clientIds = +segments[segments.length - 1].path;

      this.updateBreadcrumbs();
    });

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
  ngAfterViewInit(): void {}
  ngOnDestroy(): void {
    this.subscription.unsubscribe();
    this.teardownScrollPrefetch();
  }

  toggleFilter(event: boolean) {
    this.showFilter = event;
    this.clientHistoryChargeFilterComponent.opened = event;
  }

  onPageChange(event: PageChangeEvent): void {
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

  getPageStart(total: number): number {
    if (!total) { return 0; }

    // ALL mode or virtual: when page size is 0 (All) or a sentinel large value
    if (!this.gridState || this.gridState.take === 0 || this.gridState.take === 9999999) {
      // If subject exposes virtual window info, prefer that
      const getWindowStart = (this.clientChargeHistorySubject as any)?.getWindowStart;
      const getDataLength = (this.clientChargeHistorySubject as any)?.getDataLength;
      if (typeof getWindowStart === 'function' && typeof getDataLength === 'function') {
        const ws = (this.clientChargeHistorySubject as any).getWindowStart();
        const len = (this.clientChargeHistorySubject as any).getDataLength();
        return (len && len > 0) ? (ws || 0) + 1 : 0;
      }

      // Fallback for ALL mode: use the total provided by the pager template
      return total > 0 ? 1 : 0;
    }

    const start = (this.gridState.skip || 0) + 1;
    return Math.min(start, total);
  }

  getPageEnd(total: number): number {
    if (!total) { return 0; }

    if (!this.gridState || this.gridState.take === 0 || this.gridState.take === 9999999) {
      const getWindowStart = (this.clientChargeHistorySubject as any)?.getWindowStart;
      const getDataLength = (this.clientChargeHistorySubject as any)?.getDataLength;
      if (typeof getWindowStart === 'function' && typeof getDataLength === 'function') {
        const ws = (this.clientChargeHistorySubject as any).getWindowStart();
        const len = (this.clientChargeHistorySubject as any).getDataLength();
        return Math.min((ws || 0) + (len || 0), total);
      }

      // ALL mode: the pager's `total` already represents all items
      return total;
    }

    const end = Math.min((this.gridState.skip || 0) + (this.gridState.take || 0), total);
    return end;
  }

  onFilterChanged() {
    this.gridState.skip = 0;
    this.gridState.take = this.pageSize;
    this.placeOfService = this.clientHistoryChargeFilterComponent.isAllPlaceOfServiceSelected ? [] : this.clientHistoryChargeFilterComponent.selectedPlaceOfService.map((x) => x.id);
    this.fromDOS = this.clientHistoryChargeFilterComponent.dateFromString;
    this.toDOS = this.clientHistoryChargeFilterComponent.dateToString;
    //this.renderingProvider = this.clientHistoryChargeFilterComponent.selectedRenderingProviders.map(x => x.id);
    const selectedData: any[] =
      this.clientHistoryChargeFilterComponent.selectedRenderingProviders;
    this.renderingProvider = this.clientHistoryChargeFilterComponent.isAllRenderingProviderSelected  ? []  : selectedData.map((x) => x.staffMemberId);
    this.primaryFunder = this.clientHistoryChargeFilterComponent.isAllFunderSelected ? []  : this.clientHistoryChargeFilterComponent.selectedFunders.map((x) => x.id);
    this.authorizationNumber = this.clientHistoryChargeFilterComponent.isAllAuthNumberSelected ? []  : this.clientHistoryChargeFilterComponent.selectedAuthNumber.map((x) => x.id);
    if (this.isAllSelected) {
      this.gridState.take = 9999;
      this.paginationServices.setPageSizes(0);
      this.reinitializeAllModeWithFilters();
      return;
    }
    this.loadDataForClientHistory();
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
    if (this.clientChargeHistorySubject.getCount()) {
      this.loadDataForClientHistory();
    }
  }
  public onSelectChange(event: SelectionEvent): void {
    this.selectedIds.addRange(event.selectedRows.select((x) => x.dataItem.id));
    event.deselectedRows.forEach((x) => this.selectedIds.remove(x.dataItem.id));
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

  loadDataForClientHistory() {
    const fromDate = this.fromDOS
      ? new Date(
          new Date(this.fromDOS).setDate(new Date(this.fromDOS).getDate() + 1)
        )
      : new Date(new Date().setDate(new Date().getDate() - 90 + 1));

    const toDate = this.toDOS
      ? new Date(
          new Date(this.toDOS).setDate(new Date(this.toDOS).getDate() + 1)
        )
      : new Date(new Date().setDate(new Date().getDate() + 1));

    const model: ClientHistoryChargeDetailsRequestModel = {
      // Pass the mapping parameters here
      ClientHistoryChargeDetailsRequest: {
        take: this.gridState.take || 0,
        skip: this.gridState.skip || 0,
        clientId: this.clientIds || 0,
        sortingModels: this.gridState.sort || [],
      },
      ClientHistoryChargeFilterModel: {
        fromDate: fromDate,
        throughDate: toDate,

        placeOfService: this.placeOfService || [],
        renderingProvider: this.renderingProvider || [],
        authorizationNumber: this.authorizationNumber || [],
        primaryFunder: this.primaryFunder || [],
      },
    };

    this.clientChargeHistorySubject.getAll(model, this.isAllSelected);
  }

  private buildRequestModel(skip: number, take: number): ClientHistoryChargeDetailsRequestModel {
    const fromDate = this.fromDOS
      ? new Date(new Date(this.fromDOS).setDate(new Date(this.fromDOS).getDate() + 1))
      : new Date(new Date().setDate(new Date().getDate() - 90 + 1));
    const toDate = this.toDOS
      ? new Date(new Date(this.toDOS).setDate(new Date(this.toDOS).getDate() + 1))
      : new Date(new Date().setDate(new Date().getDate() + 1));
    return {
      ClientHistoryChargeDetailsRequest: {
        take: take || 0,
        skip: skip || 0,
        clientId: this.clientIds || 0,
        sortingModels: this.gridState.sort || [],
      },
      ClientHistoryChargeFilterModel: {
        fromDate: fromDate,
        throughDate: toDate,
        placeOfService: this.placeOfService || [],
        renderingProvider: this.renderingProvider || [],
        authorizationNumber: this.authorizationNumber || [],
        primaryFunder: this.primaryFunder || [],
      },
    };
  }

  private loadVirtualScrollInitial(): void {
    const params = this.buildRequestModel(0, this.virtualScrollPageSize);
    this.loadedRanges.add(0);
    this.lastLoadedSkip = -1;
    this.lastScrollDirection = null;
    this.lastScrollTop = 0;
    this.clientChargeHistorySubject.getAll(params, true);
    setTimeout(() => {
      this.isAllInitializing = false;
      this.setupScrollPrefetch();
    }, 100);
  }

  private setupScrollPrefetch(): void {
    this.teardownScrollPrefetch();
    const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
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
            const totalCount = this.clientChargeHistorySubject.getCount();
            const currentData = this.clientChargeHistorySubject.value || [];
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
    const currentData = this.clientChargeHistorySubject.value || [];
    const currentLength = currentData.length;
    const totalCount = this.clientChargeHistorySubject.getCount();
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
    const currentLength = (this.clientChargeHistorySubject.value || []).length;
    if (currentLength >= this.maxWindowSize) {
      this.loadBatchWithCleanup(params, 'up');
    } else {
      this.loadBatchUpward(params);
    }
  }

  private loadBatch(params: ClientHistoryChargeDetailsRequestModel, append: boolean): void {
    this.isLoadingMoreData = true;
    const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    const scrollTopBefore = gridEl?.scrollTop || 0;
    const beforeLength = this.clientChargeHistorySubject.getDataLength();
    if (append) {
      this.clientChargeHistorySubject.append(params);
    } else {
      this.clientChargeHistorySubject.getAll(params, this.isAllSelected);
    }
    this.clientChargeHistorySubject
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

  private loadBatchUpward(params: ClientHistoryChargeDetailsRequestModel): void {
    this.isLoadingMoreData = true;
    const beforeLength = this.clientChargeHistorySubject.getDataLength();
    this.clientChargeHistorySubject.prependBatch(params, false);
    const amountPrepended = this.virtualScrollPageSize;
    this.windowStart = Math.max(0, this.windowStart - amountPrepended);
    this.clientChargeHistorySubject
      .pipe(
        filter((arr: any[]) => (arr?.length || 0) > beforeLength),
        take(1)
      )
      .subscribe(() => {
        const currentLengthAfterPrepend = this.clientChargeHistorySubject.getDataLength();
        if (currentLengthAfterPrepend > this.maxWindowSize) {
          const excessRows = currentLengthAfterPrepend - this.maxWindowSize;
          this.clientChargeHistorySubject.removeFromBottom(excessRows);
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

  private loadBatchWithEndAlignment(params: ClientHistoryChargeDetailsRequestModel): void {
    this.isLoadingMoreData = true;
    this.isBufferOperationInProgress = true;
    const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    const beforeLength = this.clientChargeHistorySubject.getDataLength();
    const totalCount = this.clientChargeHistorySubject.getCount();
    this.clientChargeHistorySubject.append(params);
    this.clientChargeHistorySubject
      .pipe(
        filter((arr: any[]) => (arr?.length || 0) > beforeLength),
        take(1)
      )
      .subscribe(() => {
        this.ngZone.run(() => {
          const currentData = this.clientChargeHistorySubject.value || [];
          let currentLength = currentData.length;
          if (currentLength > this.maxWindowSize) {
            const maxBatchesInWindow = Math.floor(this.maxWindowSize / this.virtualScrollPageSize);
            const lastBatchStart = Math.floor((totalCount - 1) / this.virtualScrollPageSize) * this.virtualScrollPageSize;
            const desiredWindowStart = Math.max(0, lastBatchStart - (maxBatchesInWindow - 1) * this.virtualScrollPageSize);
            const desiredWindowSize = totalCount - desiredWindowStart;
            const rowsToRemove = currentLength - desiredWindowSize;
            if (rowsToRemove > 0) {
              this.clientChargeHistorySubject.removeFromTop(rowsToRemove);
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

  private loadBatchWithCleanup(params: ClientHistoryChargeDetailsRequestModel, direction: 'up' | 'down'): void {
    this.isLoadingMoreData = true;
    this.isBufferOperationInProgress = true;
    if (direction === 'down') {
      const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
      let actualRowHeight = 40;
      const scrollTopBefore = gridEl?.scrollTop || 0;
      if (gridEl) {
        const firstRow = gridEl.querySelector('tbody tr');
        if (firstRow) {
          actualRowHeight = firstRow.getBoundingClientRect().height;
        }
      }
      const beforeLength = this.clientChargeHistorySubject.getDataLength();
      this.clientChargeHistorySubject.append(params);
      this.clientChargeHistorySubject
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
      const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
      const beforeLength = this.clientChargeHistorySubject.getDataLength();
      this.clientChargeHistorySubject.prependBatch(params, false);
      const amountPrepended = this.virtualScrollPageSize;
      this.windowStart = Math.max(0, this.windowStart - amountPrepended);
      this.clientChargeHistorySubject
        .pipe(
          filter((arr: any[]) => (arr?.length || 0) > beforeLength),
          take(1)
        )
        .subscribe(() => {
          this.ngZone.run(() => {
            requestAnimationFrame(() => {
              const currentData = this.clientChargeHistorySubject.value || [];
              const currentLength = currentData.length;
              if (currentLength > this.maxWindowSize) {
                const excessRows = currentLength - this.maxWindowSize;
                this.clientChargeHistorySubject.removeFromBottom(excessRows);
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
    const currentData = this.clientChargeHistorySubject.value || [];
    const currentLength = currentData.length;
    if (currentLength <= this.maxWindowSize) return;
    const excessRows = currentLength - this.maxWindowSize;
    const rowsToRemove = excessRows;
    if (rowsToRemove <= 0) return;
    const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    this.clientChargeHistorySubject.removeFromTop(rowsToRemove);
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
    this.clientChargeHistorySubject.clearBuffer();
    this.teardownScrollPrefetch();
    this.loadVirtualScrollInitial();
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
    this.clientChargeHistorySubject.clearBuffer();
    this.gridState.skip = 0;
    this.gridState.take = take;
    this.paginationServices.setPageSizes(take);
    this.footerRange = this.computeFooterRange();
    this.loadDataForClientHistory();
  }

  private computeFooterRange(): string {
    const total = this.clientChargeHistorySubject.getCount();
    const currentLength = this.clientChargeHistorySubject.getDataLength();
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
