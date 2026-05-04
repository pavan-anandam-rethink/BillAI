import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  NgZone,
  OnDestroy,
  OnInit,
  Output,
  TemplateRef,
  ViewChild,
} from '@angular/core';
import { ActivatedRoute, Router, Routes } from '@angular/router';
import { GridFilterOperators } from '@core/enums/common';
import {
  ClaimListFilterSort,
  ClaimPosting,
  ClientPrintData,
  PaymentPostingShortInfo,
  PaymentSummary,
} from '@core/models/billing';
import { NotifyDialog } from '@core/models/common';
import {
  ClaimNotesService,
  ClaimPostingService,
  ClaimService,
  PaymentPostingService,
} from '@core/services/billing';
import {
  DialogService,
  DialogCloseResult,
} from '@progress/kendo-angular-dialog';
import {
  GridComponent,
  GridDataResult,
  PagerSettings,
  PageChangeEvent,
  SelectableMode,
  SelectableSettings,
  SelectionEvent,
} from '@progress/kendo-angular-grid';
import { SortDescriptor } from '@progress/kendo-data-query';
import { SidebarService } from '@app/shared/components/sidebar';
import {
  combineLatest,
  forkJoin,
  Observable,
  of,
  Subject,
  Subscription,
  fromEvent,
} from 'rxjs';
import { catchError, map, take, takeUntil, tap, throttleTime, filter } from 'rxjs/operators';
import { ClaimDetailsComponent } from './claim-details';
import { ClaimNotesComponent } from './claim-notes';
import { PatientDetailsComponent } from './patient-details';
import { PaymentDetailsInfoComponent } from '@app/billing/payment-posting/payment-posting-view/payment-details-info/payment-details-info.component';
import { AccountMemberService } from '@core/services/account/account-member.service';
import {
  ClaimNoteGetModel,
  ClaimNoteModel,
  ClaimNotesSaveModel,
} from '@core/models/billing/notes/cliam-posting-note';
import { AccountPermissions } from '@core/enums/account';
import { ClaimPostingListSubject } from '@core/subjects/claim-posting-list.subject';
import { HFCAprintComponent } from '@app/shared/components/HFCA';
import { PrintModalService } from '@app/shared/components/print-modal';
import {
  WriteOffClaimDialogComponent,
  WriteOffClaimDialogResult,
} from '@app/billing/encounters/ecnounter-list/write-off-claim-dialog/write-off-claim-dialog.component';
import { VoidClaimDialogComponent } from '@app/billing/encounters/ecnounter-list/void-claim-dialog/void-claim-dialog.component';
import { RebillClaimDialogComponent } from '@app/billing/encounters/ecnounter-list/rebill-claim-dialog/rebill-claim-dialog.component';
import {
  AddClaimNotesDialogComponent,
  AddClaimNotesDialogResult,
} from '@app/billing/encounters/ecnounter-list/add-claim-notes-dialog/add-claim-notes-dialog.component';
import { NotificationService } from '@progress/kendo-angular-notification';
import { IdsWithUserInfo } from '@core/models/billing/get-claim-by-identifier';
import {
  PaymentPostingPrintModel,
  PaymentPostingBulkModel,
  PaymentClaims,
} from '@core/models/billing/cliam-posting';
import { ClaimOrChargeToWriteOff } from '@core/models/billing/write-off-claim-model';
import { ClaimToPrint } from '@app/shared/components/HFCA/HFCAprint.component';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { WriteoffService } from '@core/services/billing/writeoff.service';
import { DialogAction } from '@progress/kendo-angular-dialog';
import { PaymentDetailsFilterComponent } from './payment-details-filter/payment-details-filter.component';
import {
  GridFilterModel,
  InsurancePaymentGridFilterModel,
} from '@core/models/common/grid-filter-model';
import { InsuranceClaimListFilterSort } from '@core/models/billing/claim-posting-filter-sort';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ClaimSubmissionType } from '@core/enums/billing/claim-submission-type';
import { billingMode } from '@core/enums/billing/billingMode';
import {
  BillNextFunderDialogComponent,
  BillNextFunderDialogResult,
} from '@app/billing/encounters/ecnounter-list/bill-next-funder-dialog/bill-next-funder-dialog.component';
import { ClaimNextFundersAndControlNumberModel } from '@core/models/billing/claim-patient-funder-option-model';
import {
  SecondaryFunderDetailsModel,
  ClaimsSubmitModel,
} from '@core/models/billing/claims-submit-model';

@Component({
  selector: 'payment-details',
  templateUrl: './payment-details.html',
  styleUrls: ['./payment-details.css', '../../status-actions.css'],
})
export class PaymentDetailsComponent implements OnInit, OnDestroy {
  private unsubscribe = new Subject<void>();
  @Input() payment: PaymentPostingShortInfo;
  //@Output() zeroOpenClaimsNotify = new EventEmitter();
  @ViewChild(GridComponent) encounterGrid: GridComponent;

  @ViewChild(GridComponent) claimsGrid: GridComponent;
  @ViewChild(PaymentDetailsInfoComponent)
  paymentDetailsInfo: PaymentDetailsInfoComponent;
  @ViewChild(PaymentDetailsFilterComponent)
  filterComponent: PaymentDetailsFilterComponent;
  private idsWithUserInfoReq: IdsWithUserInfo = {
    AccountInfoId: this.accountService.memberDetails.accountInfoId,
    MemberId: this.accountService.memberDetails.memberId,
    Ids: [],
  };

  subscriptions = new Subscription();
  public notifyDialog = new NotifyDialog(false);
  writeOffEventSubject: Subject<void> = new Subject<void>();
  @Output() updateStatus = new EventEmitter<number>();
  ClaimPostingListSubject: ClaimPostingListSubject;
  view: Observable<GridDataResult>;
  public mode: SelectableMode = 'multiple';
  public dialogForTotalCount: boolean = false;
  public gatClaimsTabData: boolean = false;
  redraw = false;
  fullGridData: ClaimPosting[] = [];

  gridState: InsuranceClaimListFilterSort = new InsuranceClaimListFilterSort();
  defaultGridState: InsuranceClaimListFilterSort =
    new InsuranceClaimListFilterSort();
  readonly pagingSettings: PagerSettings = {
    buttonCount: 5,
    type: 'numeric',
    pageSizes: true,
    previousNext: true,
  };

  gridData: ClaimPosting[] = [];
  allClients: ClaimFilterOptionModel[] = [];
  paymentId: number;

  readonly selectableSettings: SelectableSettings = {
    checkboxOnly: true,
    mode: this.mode,
  };

  serviceBreakdown = false;
  currentDay = new Date();

  testData = [
    {
      id: 1,
      clientName: 'clientName',
      expected: 1,
      allowed: 1,
      balance: 1,
      status: 'processed',
    },
  ];
  showActions = false;

  showAddClaimDialog = false;
  canEdit: boolean;
  canEditClaim: boolean;
  public claimsToPrintIds: ClaimToPrint[] = [];
  public unsubscribeAll$ = new Subject<void>();
  mySelectionitems: any[] = []; // stores selected keys (ids)
  selectedRowsData: any[] = []; // store full data items for selected rows
  deniedStatus = 'Denied';

  gridPageSizes: any;
  showStatusDropdown = 0;
  hoveredItemId: number | null = null;
  showHeaderActions = false;
  activeRow: number | null = null;
  footerRange: string = '0';
  impersonationUserName: string | null = null;

  // =======================
  //  VIRTUAL SCROLL STATE (ALL mode)
  // =======================
  public isAllSelected = false;
  public isLoadingMoreData = false;
  private isAllInitializing = false;
  private isBufferOperationInProgress = false;

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
  private readonly UP_SCROLL_THRESHOLD = 150; // px from top to trigger upward load

  constructor(
    private ngZone: NgZone,
    private route: ActivatedRoute,
    private printService: PrintModalService,
    private notificationService: NotificationHandlerService,
    private claimPostingService: ClaimPostingService,
    private paymentPostingService: PaymentPostingService,
    private dialogService: DialogService,
    private sidebarService: SidebarService,
    private accountService: AccountMemberService,
    private claimService: ClaimService,
    private claimNotesService: ClaimNotesService,
    private writeOffService: WriteoffService,
    private router: Router
  ) {
    this.impersonationUserName = this.accountService.memberDetails.impersonationUserName;
    this.getGridPageSizes();
  }

  onPageChange(event: PageChangeEvent) {
    this.gridState.skip = event.skip;
    this.gridState.take = event.take;

    // Enter ALL mode when page size is 'All'
    if (event.take === 0) {
      // Confirmation when very large
      if (this.ClaimPostingListSubject.totalCount > 1000) {
        this.dialogForTotalCount = true;
        this.subscriptions.add(
          this.SubmitPageCount().result.subscribe((result) => {
            if ((result as DialogAction).text === 'Yes') {
              this.enableAllMode();
            } else {
              const lastPageSize = localStorage.getItem('lastPageSize');
              this.gridState.take = lastPageSize
                ? JSON.parse(lastPageSize)
                : 20;
            }
          })
        );
        return;
      }
      this.enableAllMode();
      return;
    }

    // Exit ALL mode when a numeric page size is selected
    if (this.isAllSelected && event.take > 0) {
      this.exitAllModeToPaged(event.take);
      return;
    }

    // Normal paged mode page change
    this.gatClaimsTabData = true;
    localStorage.setItem('lastPageSize', this.gridState.take.toString());
    this.loadData(this.gridState);
  }

  getPageStart(total: number): number {
    if (!total) {
      return 0;
    }
    const isAll =
      !this.gridState ||
      this.gridState.take === 0 ||
      this.gridState.take === 9999999;

    if (isAll) {
      const getWindowStart = (this.ClaimPostingListSubject as any)
        ?.getWindowStart;
      if (typeof getWindowStart === 'function') {
        const ws = (this.ClaimPostingListSubject as any).getWindowStart();
        return (ws || 0) + 1;
      }

      // No window info — show the full range for ALL
      return total > 0 ? 1 : 0;
    }

    const start = (this.gridState.skip || 0) + 1;
    return Math.min(start, total);
  }

  getPageEnd(total: number): number {
    if (!total) {
      return 0;
    }
    const isAll =
      !this.gridState ||
      this.gridState.take === 0 ||
      this.gridState.take === 9999999;

    if (isAll) {
      const getWindowStart = (this.ClaimPostingListSubject as any)
        ?.getWindowStart;
      const getDataLength = (this.ClaimPostingListSubject as any)
        ?.getDataLength;

      if (typeof getWindowStart === 'function' && typeof getDataLength === 'function') {
        const ws = (this.ClaimPostingListSubject as any).getWindowStart();
        const len = (this.ClaimPostingListSubject as any).getDataLength() || 0;
        return Math.min((ws || 0) + len, total);
      }

      if (typeof getDataLength === 'function') {
        const len = (this.ClaimPostingListSubject as any).getDataLength() || 0;
        return Math.min(len, total);
      }

      // No window info — show full total for ALL
      return total;
    }

    const end = Math.min(
      (this.gridState.skip || 0) + (this.gridState.take || 0),
      total
    );
    return end;
  }
  onDetailExpand(event: any): void {
    // Update parent breadcrumbs via custom event
    const routeParams = this.route.snapshot.params;
    const breadcrumbs = [
      { label: 'Payment Posting', url: '/billing/paymentposting/list' },
      {
        label: 'Payment Details',
        url: '/billing/paymentposting/edit/' + routeParams['id'],
      },
      { label: 'Service Line Details', url: '' },
    ];
    window.dispatchEvent(
      new CustomEvent('updateBreadcrumbs', { detail: breadcrumbs })
    );
  }

  onDetailCollapse(event: any): void {
    const routeParams = this.route.snapshot.params;
    // Update parent breadcrumbs via custom event
    const breadcrumbs = [
      { label: 'Payment Posting', url: '/billing/paymentposting/list' },
      {
        label: 'Payment Details',
        url: '/billing/paymentposting/edit/' + routeParams['id'],
      },
    ];
    window.dispatchEvent(
      new CustomEvent('updateBreadcrumbs', { detail: breadcrumbs })
    );
  }

  collapseAllGridRows(): void {
    // Collapse all expanded rows in the grid For Payment Details
    if (this.claimsGrid && this.claimsGrid.data) {
      const data = this.claimsGrid.data as any;
      if (data && data.data) {
        data.data.forEach((item: any, index: number) => {
          this.claimsGrid.collapseRow(index);
        });
      }
    }
    // Removed The Code of breadcrumbs For Invoice Details
    // Note: Parent component handles breadcrumb update when it initiates collapse via breadcrumb click
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

  onSortChange(sortParams: SortDescriptor[]) {
    this.gridState.sortingModels = sortParams;
    if (this.isAllSelected) {
      this.reinitializeAllModeWithFilters();
      return;
    }
    this.loadData(this.gridState);
  }

  public onSelectChange(event: SelectionEvent): void {
    this.mySelection.addRange(
      event.selectedRows.select((x) => x.dataItem.claimId)
    );
    event.deselectedRows.forEach((x) =>
      this.mySelection.remove(x.dataItem.claimId)
    );
    let claimToWriteOffModel: ClaimOrChargeToWriteOff = {
      balanceAmount: 0,
      claimId: 0,
      chargeId: 0,
    };
    let claimToPrintModel: ClaimToPrint = {
      claimId: 0,
      cmsPages: 0,
    };
    event.selectedRows.forEach((x) => {
      claimToWriteOffModel.claimId = x.dataItem.claimId;
      claimToWriteOffModel.balanceAmount = x.dataItem.balance;
      this.claimsOrChargeToWriteOff.push(
        Object.assign({}, claimToWriteOffModel)
      );

      this.mySelectionitems.push(x.dataItem.id);
      claimToPrintModel.claimId = x.dataItem.claimId;
      claimToPrintModel.cmsPages = x.dataItem.cmsPageCount;
      this.claimsToPrintIds.push(Object.assign({}, claimToPrintModel));
    });
    event.deselectedRows.forEach((x) => {
      claimToWriteOffModel.claimId = x.dataItem.claimId;
      claimToWriteOffModel.balanceAmount = x.dataItem.balance;
      this.claimsOrChargeToWriteOff.removeWhere(
        (claim) => claim.id == x.dataItem.claimIdentifier
      );

      this.mySelectionitems.remove(x.dataItem.id);
      claimToPrintModel.claimId = x.dataItem.claimId;
      claimToPrintModel.cmsPages = x.dataItem.cmsPageCount;
      this.claimsToPrintIds.removeWhere((claim) => claim.id == x.dataItem.id);
    });
  }

  async loadData(params: InsuranceClaimListFilterSort) {
    this.ClaimPostingListSubject.getAll(params);
  }

  fitColumns(): void {
    this.ngZone.onStable
      .asObservable()
      .pipe(take(1))
      .subscribe(() => {
        this.claimsGrid.autoFitColumns(this.claimsGrid.columnList.toArray());
        this.claimsGrid.autoFitColumns();
      });
  }

  getGridPageSizes(): void {
    const storedGridPageSizes = localStorage.getItem('gridPageSizes')
      ? JSON.parse(localStorage.getItem('gridPageSizes') || '')
      : null;
    if (storedGridPageSizes) {
      this.gridPageSizes = storedGridPageSizes;
    } else {
      this.claimService
        .getGridPageSizes()
        .subscribe((sizes: Array<number | { text: string; value: number }>) => {
          this.gridPageSizes = sizes;
        });
    }
  }

  ngOnDestroy() {
    //unsubscribe from all here!
    this.ClaimPostingListSubject.unsubscribe();
    this.unsubscribe.next();
    this.unsubscribe.complete();
    this.sidebarService.closeAll();
    document.removeEventListener('click', this.handleOutsideClick);
    this.teardownScrollPrefetch();
  }

  ngAfterViewInit() {
    this.fitColumns();
  }

  ngOnInit() {
    window.addEventListener('collapseGrids', () => {
      this.collapseAllGridRows();
    });

    this.route.params.pipe(takeUntil(this.unsubscribe)).subscribe((x) => {
      if (x['id']) {
        const id = +x['id'];
        id && (this.gridState.paymentId = id);

        this.ClaimPostingListSubject = new ClaimPostingListSubject(
          this.claimPostingService
        );
        this.paymentId = this.gridState.paymentId;
        this.view = this.ClaimPostingListSubject.pipe(
          map(
            (data) => {
              let result = {
                data: data,
                total: this.ClaimPostingListSubject.totalCount,
              };
              this.gridData = result.data;
              return result;
            },
            tap(() => setTimeout(() => this.fitColumns(), 250))
          )
        );

        this.loadData(this.gridState);

        // Update footer when buffer changes (append/prepend/remove)
        this.subscriptions.add(
          (this.ClaimPostingListSubject as any).subscribe(() => {
            if (
              (this.isAllInitializing && this.ClaimPostingListSubject.getDataLength() === 0) ||
              this.isBufferOperationInProgress
            ) {
              return;
            }
            this.footerRange = this.computeFooterRange();
          })
        );
      }
    });

    this.accountService.accountMemberSettings.subscribe((x) => {
      if (x) {
        this.canEdit = this.accountService.checkPermissionLevel(
          AccountPermissions.BillingEditApprovedAppointments
        );
        this.canEditClaim = this.accountService.checkPermissionLevel(
          AccountPermissions.BillingEdit
        );
      }
    });

    this.sidebarService.adjustmentChanged$
      .pipe(takeUntil(this.unsubscribe))
      .subscribe((data: number) => {
        this.updateSummary();
        // Removed: this.loadData() - already called via onUpdate -> updateClaim chain
        // Removed: this.sidebarService.closeAll() - already handled in payment-posting-adjustments.component.ts
      });
  }

  // =======================
  //  ALL mode: virtual scroll helpers
  // =======================
  private enableAllMode(): void {
    if (!this.isAllSelected) {
      this.isAllSelected = true;
      this.loadedRanges.clear();
      this.windowStart = 0;
      this.windowEnd = 0;
      this.lastScrollDirection = null;
      this.lastScrollTop = 0;
      this.lastLoadedSkip = -1;
      this.isAllInitializing = true;
    }
    this.gridState.skip = 0;
    // Keep a large take to signal ALL to backend, while we manage window locally
    this.gridState.take = 9999;
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
    this.ClaimPostingListSubject.clearBuffer();
    this.gridState.skip = 0;
    this.gridState.take = take;
    this.footerRange = this.computeFooterRange();
    this.loadData(this.gridState);
  }

  private loadVirtualScrollInitial(): void {
    const params: InsuranceClaimListFilterSort = {
      ...this.gridState,
      skip: 0,
      take: this.virtualScrollPageSize,
    };
    this.loadedRanges.add(0);
    this.lastLoadedSkip = -1;
    this.lastScrollDirection = null;
    this.lastScrollTop = 0;
    this.ClaimPostingListSubject.getAll(params, true);
    setTimeout(() => {
      this.isAllInitializing = false;
      this.setupScrollPrefetch();
    }, 100);
  }

  private setupScrollPrefetch(): void {
    this.teardownScrollPrefetch();
    const gridEl = this.claimsGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
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
          const direction: 'down' | 'up' = scrollTop > this.lastScrollTop ? 'down' : 'up';
          this.lastScrollTop = scrollTop;
          if (this.lastScrollDirection && direction !== this.lastScrollDirection) {
            this.lastLoadedSkip = -1;
          }
          if (scrollTop < 20 && this.windowStart === 0) {
            return;
          }
          if (scrollPercentage >= this.prefetchThreshold && direction === 'down') {
            const totalCount = this.ClaimPostingListSubject.getCount();
            const currentData = (this.ClaimPostingListSubject.value || []) as ClaimPosting[];
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

    const currentData = (this.ClaimPostingListSubject.value || []) as ClaimPosting[];
    const currentLength = currentData.length;
    const totalCount = this.ClaimPostingListSubject.getCount();
    const skipVal = this.windowStart + currentLength;
    if (skipVal >= totalCount) return;
    if (this.lastLoadedSkip === skipVal) return;
    this.lastLoadedSkip = skipVal;

    const params: InsuranceClaimListFilterSort = { ...this.gridState, skip: skipVal, take: this.virtualScrollPageSize };
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

    const params: InsuranceClaimListFilterSort = { ...this.gridState, skip: skipVal, take: this.virtualScrollPageSize };
    const currentLength = (this.ClaimPostingListSubject.value || []).length;
    if (currentLength >= this.maxWindowSize) {
      this.loadBatchWithCleanup(params, 'up');
    } else {
      this.loadBatchUpward(params);
    }
  }

  private loadBatch(params: InsuranceClaimListFilterSort, append: boolean): void {
    this.isLoadingMoreData = true;
    const gridEl = this.claimsGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    const scrollTopBefore = gridEl?.scrollTop || 0;
    const beforeLength = this.ClaimPostingListSubject.getDataLength();
    if (append) {
      this.ClaimPostingListSubject.append(params);
    } else {
      this.ClaimPostingListSubject.getAll(params, this.isAllSelected);
    }
    this.ClaimPostingListSubject
      .pipe(
        filter((arr: any) => ((arr?.length || 0) > beforeLength)),
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
      });
  }

  private loadBatchUpward(params: InsuranceClaimListFilterSort): void {
    this.isLoadingMoreData = true;
    const beforeLength = this.ClaimPostingListSubject.getDataLength();
    this.ClaimPostingListSubject.prependBatch(params, false);
    const amountPrepended = this.virtualScrollPageSize;
    this.windowStart = Math.max(0, this.windowStart - amountPrepended);
    this.ClaimPostingListSubject.setWindowStart(this.windowStart);
    this.ClaimPostingListSubject
      .pipe(
        filter((arr: any) => ((arr?.length || 0) > beforeLength)),
        take(1)
      )
      .subscribe(() => {
        const currentLengthAfterPrepend = this.ClaimPostingListSubject.getDataLength();
        if (currentLengthAfterPrepend > this.maxWindowSize) {
          const excessRows = currentLengthAfterPrepend - this.maxWindowSize;
          this.ClaimPostingListSubject.removeFromBottom(excessRows);
        }
        const gridEl = this.claimsGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
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
            // Ensure footer range reflects the updated window after upward prepend
            this.footerRange = this.computeFooterRange();
          });
        }
        this.isLoadingMoreData = false;
      });
  }

  private loadBatchWithEndAlignment(params: InsuranceClaimListFilterSort): void {
    this.isLoadingMoreData = true;
    this.isBufferOperationInProgress = true;
    const gridEl = this.claimsGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
    const beforeLength = this.ClaimPostingListSubject.getDataLength();
    const totalCount = this.ClaimPostingListSubject.getCount();
    this.ClaimPostingListSubject.append(params);
    this.ClaimPostingListSubject
      .pipe(
        filter((arr: any) => ((arr?.length || 0) > beforeLength)),
        take(1)
      )
      .subscribe(() => {
        const currentData = (this.ClaimPostingListSubject.value || []) as ClaimPosting[];
        let currentLength = currentData.length;
        if (currentLength > this.maxWindowSize) {
          const maxBatchesInWindow = Math.floor(this.maxWindowSize / this.virtualScrollPageSize);
          const lastBatchStart = Math.floor((totalCount - 1) / this.virtualScrollPageSize) * this.virtualScrollPageSize;
          const desiredWindowStart = Math.max(0, lastBatchStart - (maxBatchesInWindow - 1) * this.virtualScrollPageSize);
          const desiredWindowSize = totalCount - desiredWindowStart;
          const rowsToRemove = currentLength - desiredWindowSize;
          if (rowsToRemove > 0) {
            this.ClaimPostingListSubject.removeFromTop(rowsToRemove);
            this.windowStart = desiredWindowStart;
            this.ClaimPostingListSubject.setWindowStart(this.windowStart);
            currentLength = desiredWindowSize;
          }
        }
        requestAnimationFrame(() => {
          if (gridEl) {
            const scrollHeight = gridEl.scrollHeight || 0;
            const clientHeight = gridEl.clientHeight || 0;
            gridEl.scrollTop = Math.max(0, scrollHeight - clientHeight);
            this.lastScrollTop = gridEl.scrollTop;
          }
          setTimeout(() => {
            this.isBufferOperationInProgress = false;
            this.isLoadingMoreData = false;
            this.footerRange = this.computeFooterRange();
          }, 100);
        });
      });
  }

  private loadBatchWithCleanup(params: InsuranceClaimListFilterSort, direction: 'up' | 'down'): void {
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
      const beforeLength = this.ClaimPostingListSubject.getDataLength();
      this.ClaimPostingListSubject.append(params);
      this.ClaimPostingListSubject
        .pipe(
          filter((arr: any) => ((arr?.length || 0) > beforeLength)),
          take(1)
        )
        .subscribe(() => {
          requestAnimationFrame(() => {
            this.applySlidingWindowCleanupDownward(actualRowHeight, scrollTopBefore);
            this.ClaimPostingListSubject.setWindowStart(this.windowStart);
            this.isLoadingMoreData = false;
            this.isBufferOperationInProgress = false;
          });
        });
    } else {
      const gridEl = this.claimsGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
      const beforeLength = this.ClaimPostingListSubject.getDataLength();
      this.ClaimPostingListSubject.prependBatch(params, false);
      const amountPrepended = this.virtualScrollPageSize;
      this.windowStart = Math.max(0, this.windowStart - amountPrepended);
      this.ClaimPostingListSubject.setWindowStart(this.windowStart);
      this.ClaimPostingListSubject
        .pipe(
          filter((arr: any) => ((arr?.length || 0) > beforeLength)),
          take(1)
        )
        .subscribe(() => {
          requestAnimationFrame(() => {
            const currentData = (this.ClaimPostingListSubject.value || []) as ClaimPosting[];
            const currentLength = currentData.length;
            if (currentLength > this.maxWindowSize) {
              const excessRows = currentLength - this.maxWindowSize;
              this.ClaimPostingListSubject.removeFromBottom(excessRows);
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
            // Update footer range after upward cleanup to avoid stale values
            this.footerRange = this.computeFooterRange();
            this.isLoadingMoreData = false;
            this.isBufferOperationInProgress = false;
          });
        });
    }
  }

  private applySlidingWindowCleanupDownward(actualRowHeight: number, scrollTopBefore: number): void {
    const currentData = (this.ClaimPostingListSubject.value || []) as ClaimPosting[];
    const currentLength = currentData.length;
    if (currentLength <= this.maxWindowSize) return;
    const excessRows = currentLength - this.maxWindowSize;
    const rowsToRemove = excessRows;
    if (rowsToRemove <= 0) return;
    const gridEl = this.getGridContentEl();
    this.ClaimPostingListSubject.removeFromTop(rowsToRemove);
    this.windowStart += rowsToRemove;
    this.ClaimPostingListSubject.setWindowStart(this.windowStart);
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
        this.footerRange = this.computeFooterRange();
      });
    }
  }

  private teardownScrollPrefetch(): void {
    if (this.scrollPrefetchSub) {
      this.scrollPrefetchSub.unsubscribe();
      this.scrollPrefetchSub = null;
    }
  }

  private getGridContentEl(): HTMLElement | null {
    return this.claimsGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
  }

  private getActualRowHeight(gridEl: HTMLElement): number {
    const firstRow = gridEl.querySelector('tbody tr');
    return firstRow ? (firstRow as HTMLElement).getBoundingClientRect().height : 40;
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
    this.ClaimPostingListSubject.clearBuffer();
    this.gridState.skip = 0;
    this.loadVirtualScrollInitial();
    this.footerRange = this.computeFooterRange();
  }

  private computeFooterRange(): string {
    const total = this.ClaimPostingListSubject.getCount();
    const currentLength = this.ClaimPostingListSubject.getDataLength();
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

  showPaid: false;
  togglePaid(showPaid: boolean) {
    this.gridState.filterModels.ShowPaid = showPaid;
    this.loadData(this.gridState);
  }

  showClientInfo(dataItem: ClaimPosting) {
    this.sidebarService
      .openRight(PatientDetailsComponent, true, 'md')
      .subscribe((rsidebarRef) =>
        rsidebarRef.instance.setData(dataItem.patientId, dataItem.patientName)
      );
  }
  showClaimInfo(claimId: number) {
    this.sidebarService
      .openRight(ClaimDetailsComponent, true, 'md')
      .subscribe((rsidebarRef) => rsidebarRef.instance.setData(claimId));
  }

  /*--------actions----------*/

  addClaimDialogToggle(model: any = null) {
    this.showAddClaimDialog = !this.showAddClaimDialog;
    if (model == null) {
      return;
    }
    this.claimPostingService
      .createPaymentEraClaims(model)
      .pipe(takeUntil(this.unsubscribeAll$))
      .subscribe((result) => {
        if (result !== null) {
          this.loadData(this.gridState);
          this.paymentDetailsInfo.loadSummary();
          if (result >= 1) {
            this.notificationService.showNotificationSuccess(
              result + ' Claim(s) Added Successfully.'
            );
          } else if (result == 0) {
            this.notificationService.showNotificationError(
              'This claim has already been linked and cannot be added again.'
            );
          }
        }
      });
  }

  addClaim(evt: any) {
    const btn = document.querySelector('.add-claim-btn');
    btn && btn.classList.toggle('active');
  }

  viewClaim(model: ClaimPosting) {
    //TODO: view claim by id
    console.log('view-click: ' + model.id);
  }

  editClaim(model: ClaimPosting) {
    //TODO: edit claim by id
    console.log('edit-click: ' + model.id);
  }

  editClaimNotes(model: ClaimPosting) {
    if (model.claimId) {
      const claimNoteGetModel: ClaimNoteGetModel = {
        id: model.claimId,
        patientId: model.patientId,
        patientName: model.patientName,
        dateOfService: model.dateOfServiceStart,
      };

      this.sidebarService
        .openRight(ClaimNotesComponent, true, 'md')
        .subscribe((rsidebarRef) =>
          rsidebarRef.instance.setData(claimNoteGetModel)
        );
    }
  }

  /*--------#print/pdf logic----------*/
  @ViewChild('print') printPopup: TemplateRef<HTMLElement>;

  printTemplate = 'payment-receipt';
  printItem: any;
  printClaim(claim: ClaimPosting) {
    this.printTemplate = 'payment-receipt';
    const dialog = this.dialogService.open({
      title: 'Print Receipt',
      width: 500,
      content: this.printPopup,
      actions: [{ text: 'No' }, { text: 'Yes', primary: true }],
    });

    dialog.result.subscribe((result: any) => {
      if (result.text === 'Yes') {
        let modelData: PaymentPostingPrintModel = {
          accountInfoId: this.idsWithUserInfoReq.AccountInfoId,
          memberId: this.idsWithUserInfoReq.MemberId,
          claimId: claim.id,
          patientId: claim.patientId,
        };
        combineLatest(
          this.claimPostingService.GetClientPrintDataById(modelData),
          this.paymentPostingService.getSummaryById(this.gridState.paymentId)
        )
          .pipe(takeUntil(this.unsubscribe))
          .subscribe(
            ([paymentData, paymentSummary]: [
              ClientPrintData,
              PaymentSummary
            ]) => {
              this.printItem = paymentData;
              paymentSummary.postDate &&
                (this.printItem.paymentPostingDate = paymentSummary.postDate);
              this.printItem.totalPayment = paymentSummary.postedAmount;
              this.printItem.remaining =
                paymentSummary.paymentAmount - paymentSummary.postedAmount;
              this.printItem.clientName =
                this.printItem.clientName || claim.patientName;
              this.printService.open(this.printTemplate, {
                returnIconName: 'Payment ID #' + this.payment.paymentIdentifier,
              });

              this.printService.onClose
                .pipe(takeUntil(this.unsubscribe))
                .subscribe((r: any) => {
                  if (r.closed) {
                    this.printItem = null;
                  }
                });
            }
          );
      } else {
        this.printItem = null;
      }
    });
  }

  /*======================================*/

  ondeleteClaim(claim: PaymentClaims) {
    const dialog = this.dialogService.open({
      title: 'Please confirm',
      width: 500,
      content: `Delete claim ${claim.claimIdentifier}?`,
      actions: [
        { text: 'Cancel' },
        {
          text: 'Delete',
          primary: true,
          do: async () => {
            await this.ClaimPostingListSubject.delete(claim, this.payment.id);
            this.updateStatus.emit(this.payment.id);
            this.updateSummary();
            this.notificationService.showNotificationSuccess(
              'Claim(s) deleted successfully'
            );
          },
        },
      ],
    });
    dialog.result.subscribe(async (result: any) => {
      if (result.text === 'Delete') {
        await result.do();
        this.loadData(this.gridState);

        const deletedClientId = claim.patientId;
        const stillHasClaims = this.gridData?.some(
          (c) => c.patientId === deletedClientId
        );
        if (!stillHasClaims && this.filterComponent?.selectedPatients) {
          this.filterComponent.selectedPatients =
            this.filterComponent.selectedPatients.filter(
              (client) => client.id !== deletedClientId
            );
        }
      }
    });
  }

  writeOffClaim(model: ClaimPosting) {
    if (model.id) {
      var dialogRef = this.dialogService.open({
        content: WriteOffClaimDialogComponent,
        title: 'Writeoff Claim',
        width: 540,
      });

      let writeOffClaimModel: ClaimOrChargeToWriteOff = {
        claimId: model.claimId,
        chargeId: 0,
        balanceAmount: model.balance,
      };

      dialogRef.content.instance.claimsOrChargeToWriteOff = [
        writeOffClaimModel,
      ];
      dialogRef.content.instance.isServiceLine = false;

      dialogRef.result.subscribe((result: any) => {
        if (result && result.data && result.data.length > 0) {
          this.writeOffService
            .writeOffClaim(result.data.first())
            .subscribe((response) => {
              if (response.success) {
                this.writeOffEventSubject.next();
                this.paymentDetailsInfo.loadSummary();
                this.loadData(this.gridState);
                this.notificationService.showNotificationSuccess(
                  'Claim has been Written-off.'
                );
              } else if (!response.success && response.errorMsg.length) {
                this.notificationService.showNotificationError(
                  'Can not perform Evenly Across write off for selected claim'
                );
              } else {
                this.notificationService.showNotificationError(
                  'This Claim could not be written off. Write off amount exceeds the claim balance.'
                );
              }
            });
        }
      });
    }
  }

  updateSummary() {
    this.paymentDetailsInfo.loadSummary();
  }

  updateClaim(id: number) {
    this.ClaimPostingListSubject.updateClaim(id);
    this.paymentDetailsInfo.loadSummary();
    // Removed: this.updateStatus.emit(this.payment.id) - causes unnecessary GetPaymentShortInfo call
    // Payment status (reconcileStatus) doesn't change when adjustments are saved
    // Only reconciliation or payment posting changes the status
    this.loadData(this.gridState);
  }

  loadPaymentShortInfo(e) {
    this.updateStatus.emit(this.payment.id);
  }

  rebillBulk() {
    const claimsToRebill = this.mySelection.filter((claimId) => !!claimId);

    if (claimsToRebill.length > 0) {
      const dialogRef = this.dialogService.open({
        content: RebillClaimDialogComponent,
        title: 'Rebill Claim',
        width: 540,
      });
      dialogRef.result.subscribe((result: any) => {
        if (result && result.submit) {
          this.claimService
            .rebillClaims({
              ClaimsToRebill: {
                claimIds: claimsToRebill,
                rebillReason: result.rebillReason,
                submissionReasonCode: result.submissionReasonId,
                note: result.note,
                claimNote: result.claimNote,
              },
              AccountInfoId: this.accountService.memberDetails.accountInfoId,
              MemberId: this.accountService.memberDetails.memberId,
            })
            .subscribe((rebilledClaimsIds) => {
              if (rebilledClaimsIds.length >= 1) {
                this.notificationService.showNotificationSuccess(
                  rebilledClaimsIds.length +
                    ' claim(s) has been rebilled successfully.'
                );
              } else {
                this.notificationService.showNotificationError(
                  claimsToRebill.length -
                    rebilledClaimsIds.length +
                    ' claim(s) are already queued for rebill.'
                );
              }
              this.loadData(this.gridState);
            });
        }
      });
    } else if (claimsToRebill.length === 0) {
      this.notificationService.showNotificationWarning(
        'Please select the claim(s).'
      );
    }
    this.mySelection = [];
    this.toggleActionDrop();
  }

  bulkPosting() {
    const claimsToRebill = this.mySelection.filter((claimId) => !!claimId);
    const claimitem = 6075;

    const abc = Array.from(new Set(this.mySelectionitems));

    if (abc.length === 0) {
      this.notificationService.showNotificationWarning(
        'Please select one or more claim(s).'
      );
      return false;
    }

    if (this.gridData.length > 0) {
      this.gridData.filter((item) => {
        abc.removeWhere(
          (id) =>
            id === item.id &&
            (item.status === this.deniedStatus ||
              (item.claimActionTypes || '')
                .split(',')
                .map((x) => x.trim().toLocaleLowerCase())
                .includes('system'))
        );
      });
      if (abc.length === 0) {
        this.notificationService.showNotificationWarning(
          'You cannot BulkPost Denied or System Processed claims.'
        );
        this.mySelection = [];
        this.toggleActionDrop();
        return false;
      }
    }

    let modelData: PaymentPostingBulkModel = {
      accountInfoId: this.idsWithUserInfoReq.AccountInfoId,
      memberId: this.idsWithUserInfoReq.MemberId,
      Ids: abc,
    };

    this.paymentPostingService.getBulkData(modelData).subscribe({
      next: (data) => {
        console.log('Data received:', data);

        if (claimsToRebill.length > 0) {
          this.paymentPostingService.setData(data);
          this.router.navigate(['/billing/paymentposting/bulkposting']);
        } else {
          this.notificationService.showNotificationWarning(
            'Please select one or more claim(s).'
          );
        }

        this.mySelection = [];
        this.toggleActionDrop();
      },
      error: (err) => {
        console.error('Error occurred:', err);
      },
      complete: () => {
        console.log('Observable completed');
      },
    });
  }

  printBulk() {
    //const claimsToPrintIds = this.mySelection.filter((claimId) => !!claimId);

    if (this.claimsToPrintIds.length > 0) {
      var dialogRef = this.dialogService.open({
        content: HFCAprintComponent,
      });
      dialogRef.content.instance.claimsToPrint = this.claimsToPrintIds;
      dialogRef.result.subscribe(function (result) {
        var payment = result;
      });
    } else if (this.claimsToPrintIds.length === 0) {
      this.notificationService.showNotificationWarning(
        'Please select the claim(s).'
      );
    }
    this.toggleActionDrop();
  }

  settleBulk() {
    if (this.mySelection.length === 0) {
      this.notificationService.showNotificationWarning(
        'Please select the claim(s).'
      );
      this.toggleActionDrop();
      return;
    }
    const dialogRef = this.dialogService.open({
      content: WriteOffClaimDialogComponent,
      title: 'Writeoff Claim',
      width: 540,
    });
    let writeOffClaimsModel: ClaimOrChargeToWriteOff[] =
      this.claimsOrChargeToWriteOff.map((claim) => ({
        claimId: claim.claimId,
        chargeId: claim.chargeId || 0,
        balanceAmount: claim.balanceAmount,
      }));
    dialogRef.content.instance.claimsOrChargeToWriteOff = writeOffClaimsModel;
    dialogRef.content.instance.isServiceLine = false;
    dialogRef.result.subscribe((result: any) => {
      var apiCalls: Observable<any>[] = [];
      if (result && result.data && result.data.length > 0) {
        apiCalls = result.data.map((value) =>
          this.writeOffService
            .writeOffClaim(value)
            .pipe(
              catchError((error) =>
                of({ success: false, error, original: value })
              )
            )
        );
        forkJoin(apiCalls).subscribe((responses: any[]) => {
          const sucessCount = responses.filter(
            (r) => r && r.success !== false
          ).length;
          const failedCount = responses.filter(
            (r) => r && r.success === false
          ).length;
          if (sucessCount > 0) {
            this.writeOffEventSubject.next();
            this.paymentDetailsInfo.loadSummary();
            this.loadData(this.gridState);
            this.notificationService.showNotificationSuccess(
              `${sucessCount} Claim(s) have been Written-off.`
            );
          }
          if (failedCount > 0) {
            this.notificationService.showNotificationError(
              failedCount + ` Claim(s) has not been Written-off.`
            );
          }
        });
      }
    });
    this.mySelection = [];
    this.claimsOrChargeToWriteOff = [];
    this.toggleActionDrop();
  }

  voidBulk() {
    const claimsToVoidIds = this.mySelection.filter((claimId) => !!claimId);
    if (claimsToVoidIds.length > 0) {
      var dialogRef = this.dialogService.open({
        content: VoidClaimDialogComponent,
        title: 'Void Claim',
      });
      dialogRef.result.subscribe((result: any) => {
        if (result && result.submit) {
          this.claimService
            .voidClaims({
              claimIds: claimsToVoidIds,
              submitToClearinghouse: result.option,
              note: result.note,
              claimNote: result.claimnote,
            })
            .subscribe((_voidedClaimsIds) => {
              this.loadData(this.gridState);
              if (_voidedClaimsIds.length >= 1) {
                this.notificationService.showNotificationSuccess(
                  _voidedClaimsIds.length + ' Claim(s) has been voided.'
                );
              } else if (_voidedClaimsIds.length == 0) {
                this.notificationService.showNotificationError(
                  'Claim(s) has already been linked and cannot be voided again.'
                );
              }
            });
        }
      });
    } else if (claimsToVoidIds.length === 0) {
      this.notificationService.showNotificationWarning(
        'Please select the claim(s).'
      );
    }
    this.mySelection = [];
    this.toggleActionDrop();
  }
  flagBulk() {
    const claimsToFlagIds = this.mySelection.filter((claimId) => !!claimId);
    this.view.subscribe((x) =>
      x.data.select((a) => {
        if (a.claimId in claimsToFlagIds) {
          if (a.isFlagged)
            claimsToFlagIds.removeWhere((b) => b.claimId == a.claimId);
        }
      })
    );
    if (claimsToFlagIds.length > 0) {
      const request = {
        Ids: claimsToFlagIds,
        AccountInfoId: this.accountService.memberDetails.accountInfoId,
        MemberId: this.accountService.memberDetails.memberId,
        rethinkuser: this.accountService.memberDetails.impersonationUserName
      };
      this.claimService.flagClaims(request).subscribe((resp: any) => {
        this.loadData(this.gridState);

        if (resp.length >= 1) {
          this.notificationService.showNotificationSuccess(
            resp.length + ' Claim(s) has been flaged.'
          );
        } else if (resp.length == 0) {
          this.notificationService.showNotificationError(
            'The Claim(s) has been already flaged.'
          );
        }
      });
    } else if (claimsToFlagIds.length === 0) {
      this.notificationService.showNotificationWarning(
        'Please select the claim(s).'
      );
    }
    this.mySelection = [];
    this.toggleActionDrop();
  }

  toggleActionDrop() {
    this.showActions = !this.showActions;
    const dropdown = document.getElementById('actions-dropdown-list');

    if (dropdown) {
      dropdown.style.height = this.showActions ? '338px' : '0px';
      dropdown.classList.toggle('active', this.showActions);
    }

    if (this.showActions) {
      setTimeout(() => {
        document.addEventListener('click', this.handleOutsideClick);
      }, 100);
    } else {
      document.removeEventListener('click', this.handleOutsideClick);
    }
  }

  handleOutsideClick = (event: MouseEvent) => {
    const dropdown = document.getElementById('actions-dropdown-list');
    const button = document.querySelector('outlined-btn');

    if (
      dropdown &&
      button &&
      !dropdown.contains(event.target as Node) &&
      !button.contains(event.target as Node)
    ) {
      this.ngZone.run(() => {
        this.showActions = false;
        dropdown.style.height = '0px';
        dropdown.classList.remove('active');
      });

      document.removeEventListener('click', this.handleOutsideClick);
    }
  };

  unFlagBulk() {
    const claimsToUnflagIds = this.mySelection.filter((claimId) => !!claimId);
    if (claimsToUnflagIds.length > 0) {
      const request = {
        Ids: claimsToUnflagIds,
        AccountInfoId: this.accountService.memberDetails.accountInfoId,
        MemberId: this.accountService.memberDetails.memberId,
        rethinkuser: this.accountService.memberDetails.impersonationUserName
      };
      this.claimService
        .unflagClaims(request)
        .subscribe((resp: any) => {
          this.loadData(this.gridState);
          if (resp.length >= 1) {
            this.notificationService.showNotificationSuccess(
              resp.length + ' Claim(s) has been unflaged.'
            );
          } else if (resp.length == 0) {
            this.notificationService.showNotificationError(
              'The Claim(s) has been already unflaged.'
            );
          }
        });
    } else if (claimsToUnflagIds.length === 0) {
      this.notificationService.showNotificationWarning(
        'Please select the claim(s).'
      );
    }
    this.mySelection = [];
    this.toggleActionDrop();
  }

  addNoteBulk() {
    const claimIds = this.mySelection.filter((claimId) => !!claimId);
    if (!claimIds.length) {
      this.notificationService.showNotificationWarning(
        'Please select the claim(s).'
      );
      return;
    }
    const dialogRef = this.dialogService.open({
      content: AddClaimNotesDialogComponent,
      title: 'Add Note',
      width: 540,
    });
    //Make Notes Dialog generic
    dialogRef.content.instance.claimIds = claimIds;

    let claimNotesModel: ClaimNotesSaveModel = {
      claimNoteModels: [],
      memberId: 0,
    };
    claimNotesModel.memberId = this.accountService.memberDetails.memberId;
    let claimsToAddNote: ClaimNoteModel = {
      claimId: 0,
      remindDate: undefined,
      note: '',
    };

    dialogRef.result.subscribe(
      (result: DialogCloseResult | AddClaimNotesDialogResult) => {
        if (
          !(result instanceof DialogCloseResult) &&
          (result as AddClaimNotesDialogResult).data
        ) {
          claimIds.forEach((claimId) => {
            claimsToAddNote.claimId = claimId;
            claimsToAddNote.note = (
              result as AddClaimNotesDialogResult
            ).data[0].note;
            claimsToAddNote.remindDate = (
              result as AddClaimNotesDialogResult
            ).data[0].remindDate;
            claimNotesModel.claimNoteModels.push(
              Object.assign({}, claimsToAddNote)
            );
            claimNotesModel.memberId =
              this.accountService.memberDetails.memberId;
          });
          this.claimNotesService
            .addToSeveral(claimNotesModel)
            .subscribe((resp) => {
              this.loadData(this.gridState);
              if (claimIds.length >= 1) {
                this.notificationService.showNotificationSuccess(
                  'Note(s) added successfully.'
                );
              }
              if (resp.error) {
                this.notificationService.showNotificationError(resp.error);
              }
            });
        }
      }
    );
    this.mySelection = [];
    this.toggleActionDrop();
  }
  /*--------#actions----------*/

  /*--------multi select--------*/

  public mySelection: number[] = [];
  public claimsOrChargeToWriteOff: ClaimOrChargeToWriteOff[] = [];
  /*--------#multi select--------*/

  onFiltersApplied(filters: any) {
    // Reset checked state for all clients
    this.allClients.forEach((client) => (client.checked = false));

    // Mark selected clients as checked
    if (filters.selectedPatients && filters.selectedPatients.length > 0) {
      filters.selectedPatients.forEach((selected) => {
        const found = this.allClients.find(
          (client) => client.id === selected.id
        );
        if (found) {
          found.checked = true;
        }
      });
    }
    this.gridState.skip = 0;

    // Reset filterModels to a new InsurancePaymentGridFilterModel
    this.gridState.filterModels = new InsurancePaymentGridFilterModel();

    // Only set filters if present
    if (filters.claimId && filters.claimId.trim().length > 0) {
      this.gridState.filterModels.ClaimIdentifier = filters.claimId.trim();
    }
    if (filters.selectedPatients && filters.selectedPatients.length > 0) {
      const patientIds = filters.selectedPatients.map((p) => p.id).join(',');
      this.gridState.filterModels.ClientIds = patientIds;
    }
    if (filters.paymentFrom != null) {
      this.gridState.filterModels.PaidAmountFrom = filters.paymentFrom;
    }
    if (filters.paymentTo != null) {
      this.gridState.filterModels.PaidAmountTo = filters.paymentTo;
    }
    if (filters.balanceFrom != null) {
      this.gridState.filterModels.BalanceAmountFrom = filters.balanceFrom;
    }
    if (filters.balanceTo != null) {
      this.gridState.filterModels.BalanceAmountTo = filters.balanceTo;
    }
    if (filters.paidStatus === undefined) {
      this.gridState.filterModels.ShowPaid = undefined;
    } else {
      this.gridState.filterModels.ShowPaid =
        filters.paidStatus === 'paid' ? true : false;
    }
    // If all filters are empty, gridState.filterModels will be default (empty)
    if (this.isAllSelected) {
      this.reinitializeAllModeWithFilters();
    } else {
      this.loadData(this.gridState);
    }
  }

  getGridClients(): ClaimFilterOptionModel[] {
    const clientsMap = new Map<number, ClaimFilterOptionModel>();
    this.gridData.forEach((item) => {
      if (item.patientId && item.patientName) {
        clientsMap.set(item.patientId, {
          id: item.patientId,
          name: item.patientName,
          checked: false,
        });
      }
    });
    return Array.from(clientsMap.values());
  }

  SecondaryFunderExists(
    claimStatus: string,
    submissionTypeId: any,
    patientResponsibility: number,
    IsSecondaryPayerAvailable?: boolean,
    isTestAccount?: boolean,
    balanceAmount?: number
  ): boolean {
    if (
      claimStatus === 'Closed' &&
      patientResponsibility !== 0 &&
      balanceAmount !== 0 &&
      IsSecondaryPayerAvailable &&
      submissionTypeId !== ClaimSubmissionType.Transfer &&
      !isTestAccount
    )
      return true;
    else return false;
  }

  billNextFunderSelectedClaim(claimId: number, mode: billingMode) {
    this.subscriptions.add(
      this.claimService.getClaimBillNextFunders(claimId).subscribe(
        (result) => {
          this.openBillNextFunderDialog(claimId, result, mode);
        },
        (error) => {
          //this.loadClaimHeaders(this.selectedTab);
          const errMsg =
            'Secondary funder not available. Please check your secondary payer information or reload claim data and try again.';
          this.notificationService.showNotificationError(errMsg);
        }
      )
    );
  }

  openBillNextFunderDialog(
    claimId: number,
    model: ClaimNextFundersAndControlNumberModel,
    mode: billingMode
  ) {
    const dialogRef = this.dialogService.open({
      content: BillNextFunderDialogComponent,
      title:
        mode === billingMode.BillNextFunder
          ? 'Bill Next Funder'
          : 'Rebill Secondary Funder',
      width: 540,
    });

    dialogRef.content.instance.claimId = claimId;
    dialogRef.content.instance.funders = model.funders;
    dialogRef.content.instance.controlNumber.value = model.controlNumber;
    dialogRef.content.instance.mode = mode;

    this.subscriptions.add(
      dialogRef.result.subscribe(
        (result: DialogCloseResult | BillNextFunderDialogResult) => {
          if (
            !(result instanceof DialogCloseResult) &&
            (result as BillNextFunderDialogResult).submit
          ) {
            const dialogResult = result as BillNextFunderDialogResult;
            var secondaryFunderDetails: SecondaryFunderDetailsModel = {
              claimId: claimId,
              secondaryFunderId: dialogResult.funderId,
              controlNumber: dialogResult.controlNumber,
            };

            let submitObservable: Observable<any>;
            if (mode === billingMode.BillNextFunder) {
              const submitModel: ClaimsSubmitModel = {
                Ids: [claimId],
                isSecondary: true,
                adjustmentLevel: dialogResult.isClaimLevelAdjustment ? 1 : 2,
                secondaryFunderDetails: [secondaryFunderDetails],
                AccountInfoId: this.accountService.memberDetails.accountInfoId,
                MemberId: this.accountService.memberDetails.memberId,
                impersonationUserName: this.impersonationUserName,
              };
              submitObservable = this.claimService.submitClaims(submitModel);
            }

            this.subscriptions.add(
              submitObservable.subscribe(
                (claimIdentifier) => {
                  const successMsg =
                    mode === billingMode.BillNextFunder
                      ? 'The claim has been billed electronically.'
                      : 'The claim has been rebilled electronically.';
                  this.notificationService.showNotificationSuccess(successMsg);
                  this.loadData(this.gridState);
                  //this.loadClaimHeaders(this.selectedTab);
                },
                (error) => {
                  const errorMsg =
                    mode === billingMode.BillNextFunder
                      ? 'The claim could not be billed. Please check validations and try again.'
                      : 'The claim could not be rebilled. Please check validations and try again.';
                  this.notificationService.showNotificationError(errorMsg);
                }
              )
            );
          }
        }
      )
    );
  }

  toggleStatusDropdown(dataItem: any | null) {
    // If dataItem is null we close the dropdown; otherwise toggle for that id
    const id = dataItem ? dataItem.id : null;
    this.showStatusDropdown = this.showStatusDropdown === id ? null : id;
  }

  updateClaimStatus(dataItem: any, statusId: string) {
    const claimId =
      typeof dataItem.claimId === 'number'
        ? dataItem.claimId
        : parseInt(dataItem.claimId, 10);
    this.claimService
      .updateClaimStatus({ claimId, claimStatusId: parseInt(statusId) })
      .subscribe({
        next: () => {
          this.notificationService.showNotificationSuccess(
            'Claim status updated successfully.'
          );
          this.loadData(this.gridState);
          this.toggleStatusDropdown(null); // Close the dropdown on success
        },
        error: (err) => {
          const message =
            err?.error?.message ||
            err?.message ||
            'An error occurred while updating claim status.';
          this.notificationService.showNotificationError(message);
        },
      });
  }

  openRowMenu(rowId: number) {
    this.activeRow = this.activeRow === rowId ? null : rowId;
  }
  closeRowMenu() {
    this.activeRow = null;
  }

  @HostListener('document:click', ['$event'])
  onDocClick(event: MouseEvent) {
    const el = event.target as HTMLElement;
    if (
      !el.closest('.row-actions-anchor') &&
      !el.closest('.row-actions-menu')
    ) {
      this.activeRow = null;
    }
  }
}
