import {
  Component,
  Input,
  Output,
  EventEmitter,
  OnInit,
  ViewChild,
  NgZone,
} from '@angular/core';
import { BehaviorSubject, Observable, Subject, Subscription, fromEvent, map, take, tap, auditTime } from 'rxjs';
import { ConfirmDialog } from '@core/models/common'; // your actual import path
import { Location } from '@angular/common';
import { ActivatedRoute, Router, Routes } from '@angular/router';
import { ClaimService, PaymentPostingService } from '@core/services/billing';
import { AdjustmentService } from '@core/services/billing';
import { PaymentPostingBulkModel } from '../../../core/models/billing/cliam-posting';
import { AdjustmentReasonCodes } from '../../../core/models/billing/adjustmentReasonCodes';
import {
  AbstractControl,
  FormArray,
  FormControl,
  FormGroup,
} from '@angular/forms';
import {
  AddOrEditAdjustmentModel,
  AdjustmentDetailsModel,
} from '../../../core/models/billing/save-bulk-request';
import { NotificationHandlerService } from '../../../core/services/common/notification-handler.service';
import {
  DataStateChangeEvent,
  GridComponent,
  GridDataResult,
  PagerSettings,
  PageChangeEvent,
} from '@progress/kendo-angular-grid';
import { Charge } from '../../../core/models/billing/bulk-payment-response';
import { Breadcrumb } from '@core/models/billing/bread-crumb';

@Component({
  selector: 'app-bulk-posting',
  templateUrl: './bulk-posting.component.html',
  styleUrls: ['./bulk-posting.component.css'],
})
export class BulkPostingComponent implements OnInit {
  @Input() charges: Charge[] = [];
  @Input() adjustmentCodes: string[] = [];
  @ViewChild(GridComponent) paymentPostingGrid: GridComponent;
  @Output() saved = new EventEmitter<Charge[]>();
  @Output() cancelled = new EventEmitter<void>();
  view: Observable<GridDataResult>;
  gridState = {
    skip: 0,
    take: 20,
    sort: [],
  };

  readonly pagingSettings: PagerSettings = {
    buttonCount: 5,
    type: 'numeric',
    pageSizes: true,
    previousNext: true,
  };
  private chargesSubject = new BehaviorSubject<any[]>([]);
  private virtualBufferSubject = new BehaviorSubject<Charge[]>([]);
  private virtualBuffer: Charge[] = [];
  private windowStartIndex = 0; // index in full charges array of first buffered row

  adjustments: any[] = []; // to track number of adjustment columns
  editId: number;
  bulkdata: any;

  accountInfoId: number;
  memberId: number;
  allowedReasonCodesWithDescription: AdjustmentReasonCodes[] = [];
  allowedReasonCodes: string[] = [];
  newReasonCodes: string[] = [];
  adjustmentForm!: FormGroup;
  addAdjustmentFormVisible = false;
  reasonCodeExisting = false;
  existingReasonCodesCount: number;
  adjustmentDetailsModel: AdjustmentDetailsModel[];
  editedAdjustmentIds = new Set<number>();
  adjustmentlist: any;
  colwidth: number;
  SaveDisable: boolean = false;
  adjArray: any[] = []; // or provide actual data if available
  maxAdjustmentLength: number;

  gridPageSizes: any;

  constructor(
    private location: Location,
    private router: Router,
    private paymentService: PaymentPostingService,
    private adjustmentService: AdjustmentService,
    private notificationService: NotificationHandlerService,
    private ngZone: NgZone,
    private claimsService: ClaimService
  ) {
    this.view = this.chargesSubject.pipe(
      map((data) => {
        if (this.gridState.take === 0) this.gridState.take = data.length;
        const safeData = data || [];
        return {
          data:
            this.gridState.take === 0
              ? safeData.slice(0)
              : safeData.slice(
                  this.gridState.skip,
                  this.gridState.skip + this.gridState.take
                ),
          total: safeData.length,
        };
      }),
      tap(() => {
        this.ngZone.onStable.pipe(take(1)).subscribe(() => {
          try {
            this.fitColumns();
          } catch (err) {
            console.warn('fitColumns failed:', err);
          }
        });
      })
    );

    this.getGridPageSizes();
  }
  confirmDialog: ConfirmDialog = {
    subject: new BehaviorSubject<any | null>(null),
    result: null,
    opened: false,
    title: 'Confirmation',
    message:
      "Any changes you've made will not be saved, are you sure you want to cancel?",
    confirmText: 'Yes',
    cancelText: 'No',
  };

  reasonCodeValidator(
    control: AbstractControl
  ): { [key: string]: boolean } | null {
    return control.value == null || control.value == 'Select One'
      ? { reasonCode: true }
      : null;
  }

  ngOnInit() {
    // Initialize with one adjustment column by default (optional)
    if (this.adjustments.length === 0) {
      this.adjustments.push({});
    }

    // Initialize adjustments array for each charge
    this.charges.forEach((c) => {
      if (!c.adjustments || c.adjustments.length !== this.adjustments.length) {
        c.adjustments.push({
          amount: null,
          isPositive: false,
          groupCode: '',
          reasonCode: '',
        });
      }
    });

    this.confirmDialog.subject.subscribe((result: boolean) => {
      if (result) {
        this.cancel();
      } else {
        this.confirmDialog.opened = false;
      }
    });

    this.paymentService.getId().subscribe((id) => {
      if (id !== null) {
        this.editId = id;
      }
    });

    this.paymentService.getData().subscribe((data) => {
      if (data !== null && Array.isArray(data)) {
        this.bulkdata = data;

        this.charges = data.map((item) => ({
          id: item.id,
          clientName: item.patientName || item.patientName || '',
          claimIdentifier: item.claimIdentifier || '',
          serviceDate: new Date(item.dateOfService || item.dateOfService),
          procedureCode: item.procedure || item.procedure || '',
          modifiers: item.mods || '',
          billedAmount: item.billedAmount || 0,
          allowedAmount: item.allowedAmount || 0,
          writeOff: item.writeOff || 0,
          balance: item.balance || 0,
          paymentAmount: item.paidAmount || 0,
          adjustments: item.adjustments || [],
          adjustmentsCode: Array.isArray(item.adjustments)
            ? item.adjustments.map((adj) => {
                return `${adj.groupCode}-${adj.ReasonCode}`;
              })
            : [],
        }));

        this.maxAdjustmentLength = this.charges.reduce((max, charge) => {
          return Math.max(max, charge.adjustments.length);
        }, 0);
        this.preaddAdjustmentCol(this.maxAdjustmentLength);
      }
    });
    this.charges.forEach((charge) => {
      while (
        this.maxAdjustmentLength > 3
          ? charge.adjustments.length < this.maxAdjustmentLength
          : charge.adjustments.length < 3
      ) {
        charge.adjustments = charge.adjustments || [];

        charge.adjustments.push({
          amount: null,

          isPositive: false,

          groupCode: '',

          reasonCode: '',
        });
      }
    });

    this.chargesSubject.next(this.charges);
    this.getAdjustmentReasonCodes('');

    this.paymentService.getId().subscribe((id) => {
      if (id !== null) {
        this.editId = id;
        // Update breadcrumb URL with editId
        this.breadcrumbs[1] = {
          label: 'Payment Details',
          url: `/billing/paymentposting/edit/${this.editId}`,
          tabIndex: 1,
        };
      }
    });
  }

  breadcrumbs: Breadcrumb[] = [
    { label: 'Payment Posting', url: '/billing/paymentposting', tabIndex: 0 },
    {
      label: 'Payment Details',
      url: '/billing/paymentposting/edit',
      tabIndex: 1,
    },
    {
      label: 'Bulk Posting',
      url: '/billing/paymentposting/bulkposting',
      tabIndex: 2,
    },
  ];

  public calcBalance(c: Charge): number {
    const sumAdj = c.adjustments.reduce((sum, a) => {
      // If isPositive is true, subtract the amount; else, add it
      const amt = a.amount || 0;
      return sum + (a.isPositive ? amt : -amt);
    }, 0);
    const writeCal = c.writeOff || 0;
    return round(c.billedAmount - c.paymentAmount + sumAdj - writeCal, 2);
  }

  onGridStateChange(state: DataStateChangeEvent): void {
    this.gridState = {
      ...this.gridState,
      ...state,
      sort: state.sort ?? [],
    };
    this.chargesSubject.next(this.charges);
  }

  fitColumns(): void {
    if (this.paymentPostingGrid) {
      this.paymentPostingGrid.autoFitColumns();
    }
  }

  preaddAdjustmentCol(length: number) {
    this.existingReasonCodesCount = length;
    if (this.existingReasonCodesCount > 3) {
      this.adjArray[0] = this.existingReasonCodesCount;
    } else {
      this.adjArray[0] = 3;
    }

    while (this.adjustments.length < length) {
      this.adjustments.push({});
    }
  }

  checkboxClick(value: boolean, adj: any): void {
    adj.isPositive = value;
  }

  markAsUpdatedAdjustment(id: number, adj_id: number) {
    this.editedAdjustmentIds.add(id);
    const item = this.bulkdata.find((entry) => entry.id === id);

    if (item && Array.isArray(item.adjustments)) {
      const adjustmentItem = item.adjustments.find((adj) => adj.id === adj_id);
      if (adjustmentItem) {
        adjustmentItem.isPositive = true;
      }
    }
  }

  removeLeadingDigits(value: number) {
    return value ? parseFloat(value.toFixed(2)) : value;
  }

  removeAdjustmentRow(row: any, adjIndex: number) {
    this.colwidth = this.colwidth - 420;

    if (row.adjustments && row.adjustments.length > adjIndex) {
      row.adjustments.splice(adjIndex, 1);
      const maxAdjustmentLength = this.charges.reduce((max, charge) => {
        return Math.max(max, charge.adjustments.length);
      }, 0);
      this.adjArray[0] = maxAdjustmentLength;
    }
  }

  calcGridMinWidth(): number {
    const adjColumnsWidth = this.adjustments.length * 2 * 250;
    return adjColumnsWidth;
  }

  addAdjustmentCol() {
    this.colwidth = this.colwidth;

    if (this.charges.length <= 100) {
      for (let i = this.charges.length - 1; i >= 0; i--) {
        this.charges[i].adjustments.push({
          amount: null,
          isPositive: false,
          groupCode: '',
          reasonCode: '',
        });
      }
      const maxAdjustmentLength = this.charges.reduce((max, charge) => {
        return Math.max(max, charge.adjustments.length);
      }, 0);
      this.existingReasonCodesCount = maxAdjustmentLength - 1;
      this.adjArray[0] = maxAdjustmentLength;
    }
  }

  addAdjustmentRow(row: any) {
    this.colwidth = this.colwidth + 420;
    if (!row.adjustments) row.adjustments = [];
    row.adjustments.push({
      amount: null,
      isPositive: false,
      reasonCodeKey: '',
    });
  }

  reasonCodesSearchValueChanged(
    newVal: string,
    index: number,
    c: Charge
  ): void {
    const searchText = newVal?.trim() ?? '';
    if (searchText.length > 0) {
      if (this.allowedReasonCodesWithDescription.length > 0) {
        this.filterAdjustmentReasonCodes(searchText);
      } else {
        this.getAdjustmentReasonCodes(searchText);
      }
    } else {
      if (searchText == '') {
        const adj = c.adjustments[index];
        adj.groupCode = '';
        adj.reasonCode = '';
      }
      this.allowedReasonCodes = this.allowedReasonCodesWithDescription.map(
        (item) => item.reasonCode
      );
    }
  }

  onReasonCodeChange(value: string, adj: any) {
    if (value.length > 0) {
      const isValid = this.allowedReasonCodes.includes(value);

      if (isValid) {
        this.setGroupReasonCode(adj, value);
      } else {
        // Clear everything when value is not in the list

        this.notificationService.showNotificationError(
          'Please select a valid code from the list.'
        );
        this.setGroupReasonCode(adj, '');
      }
    }
  }

  markRequiredReasonCode(
    c: Charge,
    idx: number,
    adjAmount: any,
    value?: string
  ) {
    const adj = c.adjustments[idx];
    adj.groupCode = c.adjustments[idx].groupCode;
    adj.reasonCode = c.adjustments[idx].reasonCode;
    if (!value || value.trim() === '') {
      adjAmount.groupCode = '';
      adjAmount.reasonCode = '';
    }
  }

  paymentAmountchange(c: Charge, idx: number) {
    this.bulkdata[idx].paidAmount = c.paymentAmount;
  }

  allowedAmountChange(c: Charge, idx: number) {
    this.bulkdata[idx].allowedAmount = c.allowedAmount;
  }

  markRequiredAmount(c: Charge, idx: number) {
    const adj = c.adjustments[idx];
    adj.amount = null;
  }

  onOpenDropdown() {
    this.allowedReasonCodes = this.allowedReasonCodesWithDescription
      .filter((item) => item.isDefault)
      .map((item) => item.reasonCode);
  }

  updateDescriptionAndMarkUpdated(
    code: string,
    adjustment: AdjustmentDetailsModel
  ) {
    const parts = code.split('-');

    adjustment.groupCode = parts[0];
    adjustment.reasonCode = parts[1];
  }

  addAdjustmentField() {
    if (!this.addAdjustmentFormVisible) {
      this.addAdjustmentFormVisible = true;
      this.adjustmentForm = new FormGroup({
        adjustments: new FormArray([
          new FormGroup({
            amount: new FormControl(''),
            isPositive: new FormControl(false),
            reasonCode: new FormControl('Select One', [
              this.reasonCodeValidator,
            ]),
          }),
        ]),
      });
    } else {
      const searchText = '';
      this.getAdjustmentReasonCodes(searchText);
      (this.adjustmentForm.get('adjustments') as FormArray).push(
        new FormGroup({
          amount: new FormControl(''),
          isPositive: new FormControl(false),
          reasonCode: new FormControl('Select One', [this.reasonCodeValidator]),
        })
      );
    }
  }

  private getAdjustmentReasonCodes(searchText: string) {
    this.adjustmentService
      .getAdjustmentReasonDescriptions(searchText)
      .subscribe((x) => {
        this.allowedReasonCodesWithDescription = x;
        this.allowedReasonCodes = x
          .filter((item) => item.isDefault)
          .map((item) => item.reasonCode);
      });
  }

  private filterAdjustmentReasonCodes(newVal: string) {
    this.allowedReasonCodes = this.allowedReasonCodesWithDescription
      .filter((item) =>
        item.reasonCode.toLowerCase().includes(newVal.toLowerCase())
      )
      .map((item) => item.reasonCode);
    this.newReasonCodes = this.allowedReasonCodes;
  }

  save() {
    const result: AddOrEditAdjustmentModel[] = this.bulkdata.map((item) => ({
      accountInfoId: item.accountInfoId,
      memberId: item.memberId,
      claimId: item.claimId,
      serviceLineId: item.serviceLineId,
      allowedAmount: item.allowedAmount,
      paymentAmount: item.paidAmount,
      adjustmentDetails: item.adjustments
        .filter(
          (adj) =>
            adj.amount != null ||
            (adj.groupCode &&
              adj.groupCode.trim() !== '' &&
              adj.reasonCode &&
              adj.reasonCode.trim() !== '')
        )
        .map((adj) => ({
          adjustmentId: adj.id,
          amount: adj.amount,
          isPositive: adj.isPositive,
          groupCode: adj.groupCode,
          reasonCode: adj.reasonCode,
        })),
    }));

    // Only check for missing fields if at least one field is filled in the adjustment
    const hasMissingFields = result.some((item) =>
      item.adjustmentDetails.some((adj) => {
        const hasAnyValue =
          (adj.amount !== null && adj.amount !== undefined) ||
          (adj.groupCode && adj.groupCode !== '') ||
          (adj.reasonCode && adj.reasonCode !== '');
        if (hasAnyValue) {
          return !adj.groupCode || !adj.reasonCode || adj.amount == null;
        }
        return false; // skip empty adjustments
      })
    );

    if (hasMissingFields) {
      this.notificationService.showNotificationError(
        'The highlighted Reason Code or Amount is missing.'
      );
      return;
    }

    this.paymentService.saveData(result).subscribe({
      next: (data) => {
        if (data.length === 0) {
          this.notificationService.showNotificationSuccess(
            'All data saved successfully'
          );
          this.router.navigate(['/billing/paymentposting/edit', this.editId]);
        }
      },
      error: (err) => {
        this.notificationService.showNotificationError('Save failed');
      },
    });
  }

  confirmCancel() {
    this.confirmDialog.opened = true;
  }

  cancel() {
    this.confirmDialog.opened = false;
    this.cancelled.emit();
    this.router.navigate(['/billing/paymentposting/edit', this.editId]); // Navigate back when "Yes" is confirmed
  }

  getGroupReasonCode(adj: any): string | null {
    const group = adj.groupCode || '';
    const reason = adj.reasonCode || '';
    return group && reason ? `${group}-${reason}` : null;
  }

  setGroupReasonCode(adj: any, value: string) {
    if (!value || value.trim() === '') {
      adj.groupCode = '';
      adj.reasonCode = '';
      return;
    }
    const [group, reason] = value.split('-');
    adj.groupCode = group || '';
    adj.reasonCode = reason || '';
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

  getPageStart(total: number): number {
    if (!total) {
      return 0;
    }
    const start = (this.gridState.skip || 0) + 1;
    return Math.min(start, total);
  }

  getPageEnd(total: number): number {
    if (!total) {
      return 0;
    }
    const take = this.gridState.take || 0;
    if (take === 0 || take >= total) {
      return total;
    }
    return Math.min((this.gridState.skip || 0) + take, total);
  }

  // =======================
  //  NEW – Minimal ALL mode virtual list wiring
  // =======================
  public isAllSelected = false;
  private virtualScrollPageSize = 50;
  private selectedIdsForVirtual: number[] | null = null;
  // Prefetch + windowing state
  // Track only window indices; no historical range tracking needed
  private loadedRanges = new Set<number>();
  private scrollPrefetchSub: Subscription | null = null;
  private prefetchThreshold = 0.8; // 80% down
  private upPrefetchThreshold = 0.2; // 20% from top
  private upScrollPxThreshold = 150; // px from top
  private scrollDebounceMs = 80;
  private batchLoadCooldownMs = 150;
  private lastBatchLoadTime = 0;
  private maxWindowSize = 200; // rows kept in DOM
  private lastLoadedSkip = -1;
  private isBufferOperationInProgress = false;

  onPageChange(event: PageChangeEvent): void {
    // Enter ALL mode
    if (event.take === 0) {
      if (!this.isAllSelected) {
        this.enterAllMode();
      }
      return;
    }

    // Exit ALL mode
    if (this.isAllSelected && event.take > 0) {
      this.exitAllModeToPaged(event.take);
      return;
    }

    // Normal paged mode
    this.gridState.skip = event.skip;
    this.gridState.take = event.take;
    this.chargesSubject.next(this.charges);
  }

  private enterAllMode(): void {
    this.isAllSelected = true;
    if (!this.charges || this.charges.length === 0) {
      this.gridState.skip = 0;
      this.gridState.take = 0;
      return;
    }
    // Swap view to local virtual buffer
    this.view = this.virtualBufferSubject.pipe(
      map((data) => ({ data, total: this.charges.length }))
    );
    // Reset scroll/window state
    this.loadedRanges.clear();
    this.lastLoadedSkip = -1;
    this.isBufferOperationInProgress = false;
    this.windowStartIndex = 0;
    // Load first batch locally
    this.virtualBuffer = this.charges.slice(0, this.virtualScrollPageSize);
    this.virtualBufferSubject.next(this.virtualBuffer);
    this.loadedRanges.add(0);
    this.ngZone.onStable.pipe(take(1)).subscribe(() => {
      this.setupScrollPrefetch();
    });
  }

  private exitAllModeToPaged(take: number): void {
    this.isAllSelected = false;
    this.teardownScrollPrefetch();
    this.gridState.skip = 0;
    this.gridState.take = take;
    // Restore original view backed by chargesSubject
    this.view = this.chargesSubject.pipe(
      map((data) => {
        const safeData = data || [];
        return {
          data: safeData.slice(this.gridState.skip, this.gridState.skip + this.gridState.take),
          total: safeData.length,
        };
      })
    );
    this.chargesSubject.next(this.charges);
  }

  // Footer helper for All-mode
  getAllModeFooterRange(): string {
    if (!this.isAllSelected) return '';
    const total = this.charges ? this.charges.length : 0;
    const current = this.virtualBuffer ? this.virtualBuffer.length : 0;
    if (!total || total === 0 || current === 0) return '0 of 0';
    const start = Math.min(this.windowStartIndex + 1, total);
    const end = Math.min(this.windowStartIndex + current, total);
    return `${start}–${end} of ${total}`;
  }

  // =======================
  //  Prefetch + sliding window logic
  // =======================
  private setupScrollPrefetch(): void {
    this.teardownScrollPrefetch();
    const content = this.getGridContentEl();
    if (!content || !this.isAllSelected) return;
    this.ngZone.runOutsideAngular(() => {
      this.scrollPrefetchSub = fromEvent(content, 'scroll')
        .pipe(auditTime(this.scrollDebounceMs))
        .subscribe(() => {
          const now = Date.now();
          if (now - this.lastBatchLoadTime < this.batchLoadCooldownMs) return;
          const { scrollTop, scrollHeight, clientHeight } = content;
          const scrollable = Math.max(0, scrollHeight - clientHeight);
          const atBottom = scrollTop >= this.prefetchThreshold * scrollable;
          if (atBottom) {
            this.ngZone.run(() => this.loadNextVirtualBatch());
            return;
          }
          const nearTop = scrollTop <= this.upPrefetchThreshold * scrollable;
          if (nearTop) {
            this.ngZone.run(() => this.loadPreviousVirtualBatch());
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

  private loadNextVirtualBatch(): void {
    if (this.isBufferOperationInProgress) return;
    const total = this.charges ? this.charges.length : 0;
    const current = this.virtualBuffer.length;
    const nextSkip = this.windowStartIndex + current;
    if (current >= total || nextSkip >= total) return;
    this.loadBatch({ skip: nextSkip, take: this.virtualScrollPageSize }, 'down');
  }

  private loadPreviousVirtualBatch(): void {
    if (this.isBufferOperationInProgress) return;
    const prevSkip = Math.max(0, this.windowStartIndex - this.virtualScrollPageSize);
    if (prevSkip === this.windowStartIndex) return;
    this.loadBatch({ skip: prevSkip, take: this.virtualScrollPageSize }, 'up');
  }

  private loadBatch(params: { skip: number; take: number }, direction: 'up' | 'down'): void {
    const content = this.getGridContentEl();
    const rowH = this.getActualRowHeight(content);
    const beforeLen = this.virtualBuffer.length;
    this.isBufferOperationInProgress = true;
    this.lastBatchLoadTime = Date.now();
    const end = Math.min((params.skip || 0) + (params.take || 0), this.charges.length);
    const slice = this.charges.slice(params.skip || 0, end);
    if (direction === 'down') {
      // Append next batch
      this.virtualBuffer = this.virtualBuffer.concat(slice);
      const appendedCount = slice.length;
      const total = this.charges.length;
      let newStart = this.windowStartIndex;
      // Bootstrap: grow to 40 without trimming
      if (this.windowStartIndex === 0 && beforeLen + appendedCount <= this.maxWindowSize) {
        newStart = 0;
      } else {
        // Always step by batch size for consistent footer (even at end)
        newStart = this.windowStartIndex + this.virtualScrollPageSize;
      }
      const desiredLength = Math.min(this.maxWindowSize, Math.max(0, total - newStart));
      const lenAfterAppend = beforeLen + appendedCount;
      let removeTop = Math.max(0, lenAfterAppend - desiredLength);
      if (removeTop > 0) {
        this.virtualBuffer.splice(0, removeTop);
        if (content) content.scrollTop -= removeTop * rowH;
      }
      this.windowStartIndex = newStart;
    } else {
      // Prepend previous batch
      const addedTop = slice.length;
      this.virtualBuffer = slice.concat(this.virtualBuffer);
      if (content && addedTop > 0) content.scrollTop += addedTop * rowH;
      const total = this.charges.length;
      const newStart = params.skip || 0;
      const desiredLength = Math.min(this.maxWindowSize, Math.max(0, total - newStart));
      const lenAfterPrepend = beforeLen + addedTop;
      const removeBottom = Math.max(0, lenAfterPrepend - desiredLength);
      if (removeBottom > 0) {
        this.virtualBuffer.splice(this.virtualBuffer.length - removeBottom, removeBottom);
      }
      this.windowStartIndex = newStart;
    }
    // Publish and finalize
    this.virtualBufferSubject.next(this.virtualBuffer);
    this.ngZone.onStable.pipe(take(1)).subscribe(() => {
      this.isBufferOperationInProgress = false;
    });
  }

  private getGridContentEl(): HTMLElement | null {
    const grid = document.getElementById('bulk-grid');
    if (!grid) return null;
    const content = grid.querySelector('.k-grid-content') as HTMLElement | null;
    return content;
  }

  private getActualRowHeight(container: HTMLElement | null): number {
    if (!container) return 40;
    const row = container.querySelector('.k-grid-table tr') as HTMLElement | null;
    if (row && row.clientHeight) return row.clientHeight;
    return 40;
  }
}

function round(num: number, decimals: number): number {
  return Math.round(num * Math.pow(10, decimals)) / Math.pow(10, decimals);
}
