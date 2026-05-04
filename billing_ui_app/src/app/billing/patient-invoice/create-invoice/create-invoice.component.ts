import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  forwardRef,
  NgZone,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { fromEvent, map, Observable, Subscription } from 'rxjs';
import { filter, throttleTime } from 'rxjs/operators';
import { ActivatedRoute, Router } from '@angular/router';
import {
  GridComponent,
  GridDataResult,
  PageChangeEvent,
  PagerSettings,
  SelectableMode,
  SelectableSettings,
} from '@progress/kendo-angular-grid';
import { SortDescriptor, State } from '@progress/kendo-data-query';
import { Locale } from '@app/locale';
import { MemberViewSettings } from '@core/models/billing/member-view-settings';
import { PaginationService } from '@core/services/billing/pagination.service';
import { environment } from 'src/environments/environment';
import {
  InvoiceRequest,
  PatientInvoiceCharge,
  PatientInvoiceDetails,
  PatientInvoiceHeader,
  PatientInvoiceHeaderSearch,
} from '@core/models/billing/patient-invoice';
import { PatientInvoiceService } from '@core/services/billing/patient-invoice.service';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { PatientInvoiceListSubject } from '@core/subjects/patient-invoice-list.subject';
import { ConfirmDialog } from '@core/models/common';
import { NotificationService } from '@progress/kendo-angular-notification';
import { error } from 'console';
import { CreateInvoiceFiltersComponent } from './create-invoice-filters/create-invoice-filters.component';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { AccountPermissions } from '@core/enums/account/account-permissions';
import { PrintInvoiceShared } from '../shared/print-invoice-shared';
import { Helper } from '@app/billing/encounters/common/common-helper';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { CreateInvoiceFilterService } from '@app/billing/services/create-invoice-filter.service';
import { ClaimService } from '@core/services/billing';
import { DialogAction } from '@progress/kendo-angular-dialog';
import { DialogService } from '@progress/kendo-angular-dialog';

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
  selector: 'create-invoice',
  templateUrl: './create-invoice.component.html',
  styleUrls: ['./create-invoice.component.css', '../status-actions.css'],
})
export class CreateInvoiceComponent
  implements OnInit, AfterViewInit, OnDestroy
{
  @ViewChild(forwardRef(() => CreateInvoiceFiltersComponent))
  filtersComponent: CreateInvoiceFiltersComponent;
  @ViewChild('encountersGrid') claimsGrid: GridComponent;
  @ViewChild(GridComponent) encounterGrid: GridComponent;
  @ViewChild('selectorAnchorEl', { read: ElementRef })
  public selectorAnchor: ElementRef;
  @ViewChild('printAnchorEl', { read: ElementRef })
  public printAnchor: ElementRef;

  confirmIncludePreviousInvoices = new ConfirmDialog(
    false,
    'Confirmation',
    'Do you want to include any previous invoices, if available?',
    'Yes',
    'No'
  );

  subscriptions = new Subscription();
  public mode: SelectableMode = 'multiple';

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

  selectedPatientWithLines: PatientInvoiceDetails[] = [];
  readonly printInvoiceShared: PrintInvoiceShared;

  filterChangedTimeout: number;

  showSelectorPopup = false;
  flipSelector = false;
  showPrintPopup = false;
  flipPrintPopup = false;
  selectedClients: number[] = [];
  // Added
  guarantorMap: Record<number, string> = {};
  guarantorLoadError = false;

  selectAllPatients = false;

  IndexList: IndexResult[] = [];
  public mySelection: number[] = [];
  userList: ClaimFilterOptionModel[] | [];

  patientInvoiceListSubject: PatientInvoiceListSubject;
  view: Observable<GridDataResult>;
  invoiceDetails: PatientInvoiceDetails[] = [];
  viewData: PatientInvoiceHeader[] = [];
  totalInvoiceHeaders = 0;
  footerRange: string = '0';
  public dialogForTotalCount: boolean = false;
  public gatClaimsTabData: boolean = false;

  gridState: State = {
    sort: [{ dir: 'desc', field: 'dateOfServiceStart' }],
    skip: 0,
    take: 20,

    // Initial filter descriptor
    filter: {
      logic: 'and',
      filters: [],
    },
  };

  showFilter: boolean;
  rethinkUrl: string;
  canEdit = false;

  gridPageSizes: any;

  // ===== Virtual Scroll (ALL mode) =====
  public isAllSelected = false;
  public isLoadingMoreData = false;
  private isAllInitializing = false;
  private isBufferOperationInProgress = false;

  // For local Testing Due To low data
  // private virtualScrollPageSize = 20;
  // private maxWindowSize = 40;
  
  //   // Virtual scroll batch size - number of records fetched per API call
  private virtualScrollPageSize = 50;
  
  // // Maximum DOM window size - total records kept in DOM at once

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
  
  // Prefetch threshold for upward scrolling - triggers API call at 20% scroll position
  private prefetchUpThreshold = 0.2;
  private readonly UP_SCROLL_THRESHOLD = 150; // px from top to trigger upward load
  private upwardResetPercentage: number = 0.3;
  private downwardResetPercentage: number = 0.4;
  
  // Guard flags to prevent duplicate loads and scroll-induced retriggers
  private isAdjustingWindow = false;
  private lastLoadAt = 0;
  private loadCooldownMs = 300;
  // ===== End Virtual Scroll State =====

  constructor(
    private router: Router,

    private route: ActivatedRoute,
    private patientInvoiceService: PatientInvoiceService,
    private paginationService: PaginationService,
    public locale: Locale,
    private cdr: ChangeDetectorRef,
    private notificationService: NotificationHandlerService,
    private accountService: AccountMemberService,
    private claimsService: ClaimService,
    private filterService: CreateInvoiceFilterService,
    private dialogService: DialogService,
    private ngZone: NgZone
  ) {
    this.filterService.isFilterSet = false;
    this.rethinkUrl = environment.rethinkBHUrl;
    this.printInvoiceShared = new PrintInvoiceShared();
    this.userList = [];
    this.patientInvoiceListSubject = new PatientInvoiceListSubject(
      this.patientInvoiceService
    );
    this.getGridPageSizes();
    this.view = this.patientInvoiceListSubject.pipe(
      map((data) => {
        let result = {
          data: data,
          total: this.patientInvoiceListSubject.getCount(),
        };
        
        // Update totalInvoiceHeaders for footer display
        this.totalInvoiceHeaders = result.total;
        
        this.userList =
          this.userList.length == 0
            ? result.data.map((x) => ({
                id: x.id,
                name: x.clientName,
                checked: false,
              }))
            : this.userList;
        this.viewData = data;
        this.invoiceDetails =
          this.patientInvoiceListSubject.getInvoiceDetails();
        if (this.gridState.take === 0) {
          result.data = result.data.slice(0);
        } else if (result.total >= this.gridState.take + this.gridState.skip) {
          result.data = result.data.slice(
            this.gridState.skip,
            this.gridState.take + this.gridState.skip
          );
        } else if (result.total > this.gridState.take) {
          result.data = result.data.slice(this.gridState.skip);
        }
        this.mySelection = [];
        return result;
      })
    );
    // Update footer range whenever the buffer changes (avoid flicker mid-load)
    this.subscriptions.add(
      this.patientInvoiceListSubject.subscribe(() => {
        if (
          (this.isAllInitializing && this.patientInvoiceListSubject.getDataLength() === 0) ||
          this.isBufferOperationInProgress ||
          (this.isAllSelected && this.isLoadingMoreData)
        ) {
          return;
        }
        this.footerRange = this.computeFooterRange();
      })
    );
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

  ngAfterContentChecked() {
    this.cdr.detectChanges();
  }

  //TODO: filter
  toggleFilter(event: boolean) {
    this.filtersComponent.opened = event;
    this.showFilter = event;
  }

  loadPatientHeaders() {
    const filter = new PatientInvoiceHeaderSearch();
    filter.filters.clientIds = this.filtersComponent.selectedPatients
      .map((x) => x.id)
      .join(',');
    filter.filters.dateOfServiceFrom = Helper.shiftDateToUTC(
      this.filtersComponent.dateFrom
    );
    filter.filters.dateOfServiceTo = Helper.shiftDateToUTC(
      this.filtersComponent.dateTo
    );
    filter.filters.patientResponsibilityFrom =
      this.filtersComponent.patientResponsibilityFrom;
    filter.filters.patientResponsibilityTo =
      this.filtersComponent.patientResponsibilityTo;
    filter.take = this.gridState.take || 0;
    filter.skip = this.gridState.skip || 0;

    this.selectedClients = [];
    this.selectedPatientWithLines = [];
    this.mySelection = [];
    this.selectAllPatients = false;
    this.patientInvoiceListSubject.getAll(filter);
    this.filterService.setFilter(this.filtersComponent);
  }

  columnsPrintToggle(): void {
    if (!this.selectedPatientWithLines.length) return;
    this.showPrintPopup = !this.showPrintPopup;
    this.flipPrintPopup = !this.flipPrintPopup;
  }

  onPrintPopupLeave(state: boolean) {
    this.showPrintPopup = state;
    this.flipPrintPopup = state;
  }

  printBulk() {
    var invoiceRequests: InvoiceRequest[] = [];
    this.selectedPatientWithLines.forEach((x) => {
      if (invoiceRequests.length != 0) {
        var request = invoiceRequests.first((r) => r.clientId == x.clientId);
      }
      if (request == null || request == undefined)
        request = new InvoiceRequest();
      const charge = new PatientInvoiceCharge();
      charge.chargeId = x.id;
      charge.billedAmount = x.charges;
      charge.billingCode = x.billingCode;
      charge.dos = x.dateOfService;
      charge.adjustmentNonPatientResponsibility =
        x.adjustment_Non_Patient_responsibility;
      charge.adjustmentPatientResponsibility =
        x.adjustment_Patient_responsibility;
      charge.insurancePayments = x.insuranceAmount;
      charge.patientBalance = x.patientBalance;
      charge.patientPayments = x.patientAmount;
      charge.units = x.units;
      if (invoiceRequests.length != 0 && request.clientId != undefined) {
        request.charges.push(charge);
      } else {
        request.clientId = x.clientId;
        request.charges.push(charge);
        invoiceRequests.push(request);
      }
    });
    this.subscriptions.add(
      this.patientInvoiceService
        .PrintPreview(invoiceRequests)
        .subscribe((result: any) => {
          if (result.errors.length) {
            this.notificationService.showNotificationError(
              'Failed to generate PDF invoices for ' +
                result.errors.length +
                ' client(s).'
            );
          }
          if (result.pdfBase64 != null) {
            if (result.errors.length) {
              window.setTimeout(() => {
                this.printInvoiceShared.processPDF(result);
              }, 1000);
            } else {
              this.printInvoiceShared.processPDF(result);
            }
          }
          this.mySelection = [];
          this.selectAllPatients = false;
          this.selectedClients = [];
          this.selectedPatientWithLines = [];
          this.view.subscribe((x) =>
            x.data.forEach((x) => (x.checked = false))
          );
          this.invoiceDetails.forEach((x) => (x.checked = false));
        })
    );
  }

  printAndMarkAsSubmittedBulk(includePreviousInvoices: boolean) {
    var invoiceRequest: InvoiceRequest[] = [];
    this.selectedPatientWithLines.forEach((x) => {
      if (invoiceRequest.length != 0) {
        var request = invoiceRequest.first((r) => r.clientId == x.clientId);
      }
      if (request == null || request == undefined)
        request = new InvoiceRequest();
      const charge = new PatientInvoiceCharge();
      charge.chargeId = x.id;
      charge.billedAmount = x.charges;
      charge.billingCode = x.billingCode;
      charge.dos = x.dateOfService;
      charge.adjustmentNonPatientResponsibility =
        x.adjustment_Non_Patient_responsibility;
      charge.adjustmentPatientResponsibility =
        x.adjustment_Patient_responsibility;
      charge.insurancePayments = x.insuranceAmount;
      charge.patientBalance = x.patientBalance;
      charge.patientPayments = x.patientAmount;
      charge.units = x.units;
      if (invoiceRequest.length != 0 && request.clientId != undefined) {
        request.charges.push(charge);
      } else {
        request.clientId = x.clientId;
        request.charges.push(charge);
        invoiceRequest.push(request);
      }
    });
    this.subscriptions.add(
      this.patientInvoiceService
        .PrintAndSubmit(invoiceRequest, includePreviousInvoices)
        .subscribe((result: any) => {
          if (result.errors.length) {
            this.notificationService.showNotificationError(
              'Failed to generate PDF invoices for ' +
                result.errors.length +
                ' clients.'
            );
          }
          if (result.pdfBase64 != null) {
            if (result.errors.length) {
              window.setTimeout(() => {
                this.printInvoiceShared.processPDF(result);
              }, 1000);
            } else {
              this.printInvoiceShared.processPDF(result);
            }
          }
          this.mySelection = [];
          this.selectAllPatients = false;
          this.selectedClients = [];
          this.selectedPatientWithLines = [];
          this.view.subscribe((x) =>
            x.data.forEach((x) => (x.checked = false))
          );
          this.invoiceDetails.forEach((x) => (x.checked = false));
          this.loadPatientHeaders();
        })
    );
  }

  printMarkAsSubmittedBulk() {
    let isAnyGuarantorMissing = false;
    isAnyGuarantorMissing = this.viewData.some(
      (x) =>
        x.checked === true &&
        (!x.guarantorName || x.guarantorName === 'Missing Guarantor')
    );

    if (!isAnyGuarantorMissing) {
      isAnyGuarantorMissing = this.invoiceDetails.some(
        (x) =>
          x.checked === true &&
          (!x.guarantorName || x.guarantorName === 'Missing Guarantor')
      );
    }
    this.selectedClients;
    if (isAnyGuarantorMissing) {
      this.notificationService.showNotificationWarning(
        'This client does not have a Guarantor assigned. Please add one before submitting or printing the invoice.'
      );
    }
    this.confirmIncludePreviousInvoices.opened = true;
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
      this.patientInvoiceListSubject.getCount() > 1000
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
    this.patientInvoiceListSubject.clearBuffer();

    this.cdr.detectChanges();
    this.resetGridScrollTop();
    this.loadPatientHeadersForVirtualScroll(true, 0);

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

  private buildInvoiceFilter(skip: number, take: number): PatientInvoiceHeaderSearch {
    const filter = new PatientInvoiceHeaderSearch();
    filter.filters.clientIds = this.filtersComponent.selectedPatients
      .map((x) => x.id)
      .join(',');
    filter.filters.dateOfServiceFrom = Helper.shiftDateToUTC(
      this.filtersComponent.dateFrom
    );
    filter.filters.dateOfServiceTo = Helper.shiftDateToUTC(
      this.filtersComponent.dateTo
    );
    filter.filters.patientResponsibilityFrom =
      this.filtersComponent.patientResponsibilityFrom;
    filter.filters.patientResponsibilityTo =
      this.filtersComponent.patientResponsibilityTo;
    filter.take = take;
    filter.skip = skip;

    return filter;
  }

  private loadPatientHeadersForVirtualScroll(isInitial: boolean, skip: number): void {
    const filter = this.buildInvoiceFilter(skip, this.virtualScrollPageSize);

    if (isInitial) {
      this.patientInvoiceListSubject.getAll(filter, true);
      setTimeout(() => {
        this.isAllInitializing = false;
        this.setupScrollPrefetch();
        this.isLoadingMoreData = false;
      }, 100);
    } else {
      const beforeLength = this.patientInvoiceListSubject.getDataLength();
      this.patientInvoiceListSubject.append(filter);
      setTimeout(() => {
        const totalInWindow = this.patientInvoiceListSubject.getDataLength();
        const uniqueTotal = this.patientInvoiceListSubject.getCount();
        const itemsAfterThisLoad = Math.max(0, uniqueTotal - (this.windowStart + beforeLength));
        const willLoadLastBatch = itemsAfterThisLoad <= this.virtualScrollPageSize;
        let repositionNeeded = false;
        if (willLoadLastBatch) {
          this.alignWindowToEnd(uniqueTotal);
          repositionNeeded = true;
        } else {
          const needsTrimForMax = totalInWindow > this.maxWindowSize;
          const needsTrimForSmallTotals = uniqueTotal <= this.maxWindowSize && totalInWindow > (this.virtualScrollPageSize * 2);
          if (needsTrimForMax || needsTrimForSmallTotals) {
            this.applySlidingWindowCleanup('down');
            repositionNeeded = true;
          }
        }

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

    const currentLength = this.patientInvoiceListSubject.getDataLength();
    const totalCount = this.patientInvoiceListSubject.getCount();

    const skipVal = this.windowStart + currentLength;

    if (skipVal >= totalCount || skipVal === this.lastLoadedSkip) return;

    const pageIndex = Math.floor(skipVal / this.virtualScrollPageSize);

    if (!this.loadedRanges.has(pageIndex)) {
      this.loadedRanges.add(pageIndex);
      this.isLoadingMoreData = true;
      this.lastLoadedSkip = skipVal;
      this.lastLoadAt = Date.now();

      this.loadPatientHeadersForVirtualScroll(false, skipVal);
      this.windowEnd = skipVal + this.virtualScrollPageSize;

      if (currentLength >= this.maxWindowSize) {
        this.applySlidingWindowCleanup('down');
      }
    }
  }

  private handleScrollUp(): void {
    if (!this.isAllSelected || this.isLoadingMoreData || this.windowStart <= 0) return;

    // If currently at dataset end, base previous skip on the desired end-aligned window
    const totalCount = this.patientInvoiceListSubject.getCount();
    const currentLength = this.patientInvoiceListSubject.getDataLength();
    const atEndNow = (this.windowStart + currentLength) >= totalCount;
    let baseWindowStart = this.windowStart;
    if (atEndNow) {
      const maxBatchesInWindow = Math.floor(this.maxWindowSize / this.virtualScrollPageSize) || 1;
      const lastBatchStart = Math.floor((totalCount - 1) / this.virtualScrollPageSize) * this.virtualScrollPageSize;
      baseWindowStart = Math.max(0, lastBatchStart - (maxBatchesInWindow - 1) * this.virtualScrollPageSize);
    }
    const previousSkip = Math.max(0, baseWindowStart - this.virtualScrollPageSize);
    const pageIndex = Math.floor(previousSkip / this.virtualScrollPageSize);
    // Upward: allow fetch even if pageIndex was previously loaded, since it may have been trimmed
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

      const totalInWindow = this.patientInvoiceListSubject.getDataLength();
      const uniqueTotal = this.patientInvoiceListSubject.getCount();
      this.windowEnd = this.windowStart + totalInWindow;

      const needsTrimForMax = totalInWindow > this.maxWindowSize;
      const needsTrimForSmallTotals = uniqueTotal <= this.maxWindowSize && totalInWindow > (this.virtualScrollPageSize * 2);
      if (needsTrimForMax || needsTrimForSmallTotals) {
        this.applySlidingWindowCleanup('up');
      }
    });
  }

  private loadPatientHeadersBatchUpward(skip: number, onComplete?: () => void): void {
    const filter = this.buildInvoiceFilter(skip, this.virtualScrollPageSize);

    this.patientInvoiceListSubject.prependBatch(filter).subscribe(
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
    const currentData = this.patientInvoiceListSubject.getDataLength();
    const totalCount = this.patientInvoiceListSubject.getCount();

    // Always remove at most one batch; for upward scroll, also trim for small totals
    let recordsToRemove = 0;
    const excessRows = Math.max(0, currentData - this.maxWindowSize);
    if (direction === 'down') {
      if (excessRows > 0) {
        recordsToRemove = excessRows;
      } else if (totalCount <= this.maxWindowSize && currentData > (this.virtualScrollPageSize * 2)) {
        recordsToRemove = this.virtualScrollPageSize;
      }
    } else {
      if (excessRows > 0) {
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
      this.patientInvoiceListSubject.removeFromTop(recordsToRemove);

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
      this.patientInvoiceListSubject.removeFromBottom(recordsToRemove);
      this.windowEnd = this.windowStart + this.patientInvoiceListSubject.getDataLength();

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

      const endAfterTrim = this.windowEnd;
      const lastPageKept = Math.floor((endAfterTrim - 1) / this.virtualScrollPageSize);
      const lastPageRemoved = Math.floor((oldWindowEnd - 1) / this.virtualScrollPageSize);
      for (let i = lastPageKept + 1; i <= lastPageRemoved; i++) {
        this.loadedRanges.delete(i);
      }
    }
    this.isAdjustingWindow = false;
  }

  private resetGridScrollTop(): void {
    if (this.claimsGrid && this.claimsGrid.wrapper) {
      const scrollContainer = this.claimsGrid.wrapper.nativeElement.querySelector('.k-grid-content');
      if (scrollContainer) {
        scrollContainer.scrollTop = 0;
      }
    }
  }

  private getGridContentEl(): HTMLElement | null {
    return this.claimsGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
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

    this.patientInvoiceListSubject.clearBuffer();
    this.resetGridScrollTop();
    this.teardownScrollPrefetch();

    this.loadPatientHeadersForVirtualScroll(true, 0);
    this.mySelection = [];
    this.selectedPatientWithLines = [];
    this.selectedClients = [];
    this.selectAllPatients = false;
  }

  private alignWindowToEnd(totalCount: number): void {
    this.isBufferOperationInProgress = true;
    const gridEl = this.getGridContentEl();
    const currentLength = this.patientInvoiceListSubject.getDataLength();
    const maxBatchesInWindow = Math.floor(this.maxWindowSize / this.virtualScrollPageSize);
    const lastBatchStart = Math.floor((totalCount - 1) / this.virtualScrollPageSize) * this.virtualScrollPageSize;
    const desiredWindowStart = Math.max(0, lastBatchStart - Math.max(0, (maxBatchesInWindow - 1)) * this.virtualScrollPageSize);
    const desiredWindowSize = totalCount - desiredWindowStart;
    const rowsToRemove = Math.max(0, currentLength - desiredWindowSize);
    if (rowsToRemove > 0) {
      this.patientInvoiceListSubject.removeFromTop(rowsToRemove);
    }
    this.windowStart = desiredWindowStart;
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

  getFooterRange(): string {
    // Return computed property to avoid frequent recalculation during change detection
    return this.footerRange;
  }

  private computeFooterRange(): string {
    const total = this.totalInvoiceHeaders || this.patientInvoiceListSubject.getCount();
    const currentLength = this.patientInvoiceListSubject.getDataLength();
    if (!total || total === 0 || currentLength === 0) return '0';
    if (this.isAllSelected) {
      const atEnd = (this.windowStart + currentLength) >= total;
      if (atEnd) {
        const maxBatchesInWindow = Math.floor(this.maxWindowSize / this.virtualScrollPageSize) || 1;
        const lastBatchStart = Math.floor((total - 1) / this.virtualScrollPageSize) * this.virtualScrollPageSize;
        const desiredWindowStart = Math.max(0, lastBatchStart - (maxBatchesInWindow - 1) * this.virtualScrollPageSize);
        const start = desiredWindowStart + 1;
        const end = total;
        return `${start}–${end}`;
      }
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

  // ===== End Virtual Scroll Methods =====

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

  getInvoiceDetails(clientId: number): PatientInvoiceDetails[] | [] {
    return this.invoiceDetails.where((x) => x.clientId == clientId);
  }

  onFilterChanged() {
    if (this.isAllSelected) {
      // Reinitialize virtual scroll under current filters
      this.reinitializeAllModeWithFilters();
      this.gatClaimsTabData = true;
    } else {
      // Normal pagination path - reset window state
      this.windowStart = 0;
      this.windowEnd = 0;
      this.lastLoadedSkip = -1;
      this.gridState.skip = 0;
      this.loadPatientHeaders();
    }
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
    event.forEach((x) => {
      if (x.checked && !this.selectedPatientWithLines.includes(x)) {
        this.selectedPatientWithLines.push(x);
      } else if (!x.checked && this.selectedPatientWithLines.includes(x)) {
        this.selectedPatientWithLines = this.selectedPatientWithLines.filter(
          (s) => s.id != x.id
        );
        this.selectedClients.remove(x.clientId);
      }
    });
    if (
      this.invoiceDetails.filter((x) => x.clientId == clientId).length !=
      this.selectedPatientWithLines.filter((x) => x.clientId == clientId).length
    ) {
      this.view.subscribe((x) => {
        x.data.first((s) => s.id == clientId).checked = false;
      });
    }
  }

  changePatientSelection(event: any): void {
    this.selectionLinesChanged(event.event, event.patientId);
  }

  selectionLinesChanged(event: any, clientId: number): void {
    let isChecked = event.currentTarget.checked;

    if (clientId == 0) {
      this.view.subscribe((x) => {
        if (event.currentTarget != null)
          x.data.forEach((item) => {
            item.checked = isChecked;
            this.selectedPatientWithLines.addRange(
              this.invoiceDetails.filter((i) => i.clientId == item.clientId)
            );
          });
      });
    } else {
      var item = undefined;
      this.view.subscribe((x) => {
        if (event.currentTarget != null)
          item = x.data.find((item) => item.id == clientId);
      });
      if (item !== undefined) {
        item.checked = isChecked;
        if (isChecked)
          this.invoiceDetails
            .where((x) => x.clientId == clientId)
            .forEach((x) => {
              if (!this.selectedPatientWithLines.includes(x)) {
                this.selectedPatientWithLines.push(x);
              }
            });
        else {
          this.selectedPatientWithLines = this.selectedPatientWithLines.filter(
            (x) => x.clientId != clientId
          );
          this.selectedClients.remove(clientId);
        }
      }
    }

    if (isChecked) {
      if (clientId === 0) {
        this.selectedClients = [];
        this.view.subscribe((x) => {
          if (event.currentTarget != null)
            x.data.forEach((item) => this.selectedClients.push(item.id));
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
    this.view.subscribe((x) => {
      if (event.currentTarget != null) {
        if (this.selectedClients.length == x.data.length)
          this.selectAllPatients = true;
        else this.selectAllPatients = false;
      }
    });
  }

  selectAllClaimLines(clientId: number): boolean {
    if (this.selectedClients.any((x: number) => x == 0)) {
      return true;
    } else {
      return this.selectedClients.any((x: number) => x == clientId);
    }
  }

  getSelectedPatientLines(clientId: number): any {
    return this.selectedPatientWithLines.filter(
      (x) => x.clientId == clientId && x.checked
    );
  }

  onExpanderClick(index: number, anchor: HTMLAnchorElement, clientId: number) {
    const isExpanded = !anchor.classList.contains('k-plus');

    if (isExpanded) {
      this.IndexList.removeWhere((x) => x.clientId == clientId);
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

  onDetailExpand(event: any): void {
    // Update parent breadcrumbs via custom event
    const breadcrumbs = [
      { label: 'Patient Invoicing', url: '/billing/patientinvoicing/list' },
      { label: 'Create Invoice', url: '/billing/patientinvoicing/list' },
      { label: 'Invoice Details', url: '' },
    ];
    window.dispatchEvent(
      new CustomEvent('updateBreadcrumbs', { detail: breadcrumbs })
    );
  }

  onDetailCollapse(event: any): void {
    // Update parent breadcrumbs via custom event
    const breadcrumbs = [
      { label: 'Patient Invoicing', url: '/billing/patientinvoicing/list' },
      { label: 'Create Invoice', url: '/billing/patientinvoicing/list' },
    ];
    window.dispatchEvent(
      new CustomEvent('updateBreadcrumbs', { detail: breadcrumbs })
    );
  }

  ngOnInit(): void {
    // Listen for grid collapse events from parent
    window.addEventListener('collapseGrids', () => {
      this.collapseAllGridRows();
    });

    this.subscriptions.add(
      this.route.queryParams.subscribe((params) => {
        if (params.tab) {
          this.router.navigate([], {
            queryParams: {
              tab: null,
            },
            queryParamsHandling: 'merge',
          });
        }
      })
    );
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
      { label: 'Create Invoice', url: '/billing/patientinvoicing/list' },
    ];
    window.dispatchEvent(
      new CustomEvent('updateBreadcrumbs', { detail: breadcrumbs })
    );
  }

  ngAfterViewInit(): void {
    this.loadPatientHeaders();
    if (this.filterService.isFilterSet) {
      window.setTimeout(
        (res) =>
          (<HTMLElement>(
            document.querySelector('.filter-btn .outlined-btn')
          ))!.click(),
        500
      );
    }
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

  ngOnDestroy(): void {
    this.teardownScrollPrefetch();
    this.subscriptions.unsubscribe();
  }

  public getPageStart(total: number): number {
    if (!total || total === 0) return 0;
    const skip =
      this.gridState && this.gridState.skip ? this.gridState.skip : 0;
    // If take === 0 means 'All' selection — start at 1
    if (this.gridState && this.gridState.take === 0) return 1;
    return skip + 1;
  }

  public getPageEnd(total: number): number {
    if (!total || total === 0) return 0;
    // If take === 0 means 'All' selection — end is total
    if (this.gridState && this.gridState.take === 0) return total;
    const skip =
      this.gridState && this.gridState.skip ? this.gridState.skip : 0;
    const take =
      this.gridState && this.gridState.take ? this.gridState.take : 20;
    return Math.min(skip + take, total);
  }
}
