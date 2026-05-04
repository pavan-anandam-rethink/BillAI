import { Component, Input, OnDestroy, ViewChild } from '@angular/core';
import { Subject, Subscription, fromEvent, Observable } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { takeUntil } from 'rxjs/operators';
import { AttachmentService } from '@core/services/billing/attachment.service';
import { PaymentAttachment } from '@core/models/billing/payment-attachment';
import { SortDescriptor } from '@progress/kendo-data-query';
import {
  GridDataResult,
  PageChangeEvent,
  PagerSettings,
} from '@progress/kendo-angular-grid';
import { ConfirmDialog } from '@core/models/common';
import { IdFilterSort } from '@core/models/billing/id-filter-sort';
import { PaymentDetailsInfoComponent } from '../payment-details-info/payment-details-info.component';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { ManualPaymentDetailsInfoComponent } from '../manual-posting-patient/manual-payment-details-info/manual-payment-details-info.component';

import { RenameAttachmentModel } from '@core/services/billing/encounter-attachment.service';
import { IdWithUserInfo } from '@core/models/billing/get-claim-by-identifier';
import { NotificationService } from '@progress/kendo-angular-notification';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { ClaimService } from '@core/services/billing';
import { AccountPermissions } from '@core/enums/account/account-permissions';
import { AttachmentsListSubject } from '@core/subjects/attachments-list.subject';
import { map, throttleTime, filter, take } from 'rxjs/operators';

@Component({
  selector: 'attachments',
  templateUrl: './attachments.html',
  styleUrls: ['./attachments.css'],
})
export class AttachmentsComponent implements OnDestroy {
  @Input() isPatient: boolean = false;
  @Input() paymentIdentifier: string;
  @ViewChild(PaymentDetailsInfoComponent)
  paymentDetailsInfoComponent!: PaymentDetailsInfoComponent;
  @ViewChild(ManualPaymentDetailsInfoComponent)
  manualPaymentDetailsInfoComponent!: ManualPaymentDetailsInfoComponent;
  private unsubscribeAll$ = new Subject<void>();

  readonly pagingSettings: PagerSettings = {
    buttonCount: 5,
    type: 'numeric',
    pageSizes: true,
    previousNext: true,
  };

  view: Observable<GridDataResult>;

  deleteConfirmation = new ConfirmDialog(
    false,
    'Confirmation',
    'Are you sure you want to delete this attachment?'
  );
  attachmentToDelete!: PaymentAttachment;

  existingFileNames: string[] = [];
  subscriptions = new Subscription();
  attachmentsOrig: PaymentAttachment[] = [];

  showAddAttachmentDialog = false;
  attachments: PaymentAttachment[] = [];

  totalCount: number = 0;
  currentPage: number = 1;

  gridState: IdFilterSort = new IdFilterSort();

  paymentId!: number;

  gridPageSizes: any;
  canEdit: boolean = false;
  // Virtual scroll state
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
  private readonly UP_SCROLL_THRESHOLD = 150;
  footerRange: string = '0';
  attachmentsListSubject: AttachmentsListSubject;
  constructor(
    private route: ActivatedRoute,
    private notificationService: NotificationHandlerService,
    private attachmentService: AttachmentService,
    private accountService: AccountMemberService,
    private claimsService: ClaimService
  ) {
    this.gridState.sortingModels = [{ dir: 'desc', field: 'dateCreated' }];

    // Initialize subject and observable before route subscription
    this.attachmentsListSubject = new AttachmentsListSubject(this.attachmentService);
    this.view = this.attachmentsListSubject.pipe(
      map((data) => ({ data, total: this.attachmentsListSubject.getCount() }))
    );

    // Load immediately when paymentId is available to show loader on time
    this.route.params.pipe(takeUntil(this.unsubscribeAll$)).subscribe((x) => {
      if (x['id']) {
        this.paymentId = +x['id'];
        this.loadPaymentAttachments();
      }
    });

    this.getGridPageSizes();
  }

  ngOnInit(): void {
    this.accountService.accountMemberSettings.subscribe((x: any) => {
      if (x) {
        this.canEdit = this.accountService.checkPermissionLevel(
          AccountPermissions.BillingEdit
        );
      }
    });
    // Keep footerRange in sync for both paged and ALL modes
    this.subscriptions.add(
      this.attachmentsListSubject.subscribe(() => {
        if (this.isBufferOperationInProgress) return;
        this.footerRange = this.computeFooterRange();
      })
    );
  }

  loadPaymentDetailsInfo() {
    this.paymentDetailsInfoComponent &&
      this.paymentDetailsInfoComponent.loadSummary();
    this.manualPaymentDetailsInfoComponent &&
      this.manualPaymentDetailsInfoComponent.loadPaymentSummary(this.paymentId);
  }

  loadPaymentAttachments() {
    this.gridState.Id = this.paymentId;
    this.gridState.MemberId = this.accountService.memberDetails!.memberId;
    this.gridState.AccountInfoId = this.accountService.memberDetails!.accountInfoId;
    this.attachmentsListSubject.getAll(this.gridState, this.isAllSelected);
    // keep existing file names for duplicate name checking
    this.attachmentsListSubject
      .pipe(take(1))
      .subscribe((data: PaymentAttachment[]) => {
        this.existingFileNames = (data || []).map((file: PaymentAttachment) => file.filename);
      });
  }

  downloadAttachment(attachment: PaymentAttachment) {
    this.attachmentService.downloadFile(attachment.id);
  }

  openAddAttachmentDialog() {
    this.showAddAttachmentDialog = true;
  }

  closeAddAttachmentDialog() {
    this.showAddAttachmentDialog = false;
    // If still in ALL mode, reset to the first batch (fresh window)
    if (this.isAllSelected) {
      // Reset scroll position to top to start from the first batch visually
      const contentEl = (document.querySelector('.attachments-list .k-grid .k-grid-content') as HTMLElement);
      if (contentEl) {
        requestAnimationFrame(() => {
          contentEl.scrollTop = 0;
          this.lastScrollTop = 0;
        });
      }
      this.reinitializeAllModeWithFilters();
    } else {
      this.loadPaymentAttachments();
    }
  }

  deleteAttachment(file: PaymentAttachment) {
    this.attachmentToDelete = file;
    this.deleteConfirmation.opened = true;
  }

  onSortChange(sortParams: SortDescriptor[]): void {
    this.gridState.sortingModels = sortParams;
    if (this.isAllSelected) {
      this.reinitializeAllModeWithFilters();
      return;
    }
    this.loadPaymentAttachments();
  }

  onPageChange(event: PageChangeEvent): void {
    this.gridState.skip = event.skip;
    this.gridState.take = event.take;
    if (event.take === 0) {
      // Enter ALL mode
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
      this.gridState.take = 9999;
      this.loadVirtualScrollInitial();
      return;
    }

    if (this.isAllSelected && event.take > 0) {
      this.exitAllModeToPaged(event.take);
      return;
    }

    // Normal paged mode
    this.loadPaymentAttachments();
  }

  startEdit(file: PaymentAttachment) {
    this.attachmentsOrig.push({ ...file });

    file.filename = file.filename.substr(0, file.filename.lastIndexOf('.'));
  }

  isEdited(file: PaymentAttachment) {
    return this.attachmentsOrig.find((x) => x.id == file.id) != undefined;
  }

  cancelEdit(file: PaymentAttachment) {
    let fileOrig = this.attachmentsOrig.find((x) => x.id == file.id);
    if (fileOrig == undefined) return;

    file.filename = fileOrig.filename;
    this.attachmentsOrig = this.attachmentsOrig.filter((x) => x.id !== file.id);
  }

  acceptEdit(file: PaymentAttachment) {
    if (file.filename.trim() === '') {
      this.notificationService.showNotificationError(
        'Enter the attachment name.'
      );
      return;
    }
    let fileOrig = this.attachmentsOrig.find((x) => x.id == file.id);
    if (fileOrig == undefined) return;

    let newFileName = `${file.filename.trim()}.${fileOrig.filename
      .trim()
      .split('.')
      .pop()}`;
    if (fileOrig.filename != newFileName) {
      var renameAttachment = new RenameAttachmentModel();
      renameAttachment.AttachmentId = file.id;
      renameAttachment.FileName = newFileName;
      const renamePayload: any = {
        AttachmentId: file.id,
        FileName: newFileName,
        AccountInfoId: this.accountService.memberDetails!.accountInfoId,
        MemberId: this.accountService.memberDetails!.memberId
      };

      if (
        this.existingFileNames.some(
          (element) => element == renameAttachment.FileName
        )
      ) {
        this.notificationService.showNotificationError(
          'File name is already exist.'
        );
        file.filename = fileOrig.filename;
        this.startEdit(file);
        return false;
      }
      
      this.attachmentService
        .renameAttachment(renamePayload)
        .pipe(takeUntil(this.unsubscribeAll$))
        .subscribe((x: any) => {
          file.filename = newFileName;
          this.attachmentsOrig = this.attachmentsOrig.filter((x: PaymentAttachment) => x.id !== file.id);
          const index = this.existingFileNames.indexOf(fileOrig.filename);
          if (index !== -1) {
            this.existingFileNames[index] = newFileName;
          }
          this.notificationService.showNotificationSuccess(
            'Attachment(s) updated successfully.'
          );
        });
    } else {
      this.notificationService.showNotificationError(
        'Attachment is not updated.'
      );
    }
  }

  acceptDeleteAttachment(isAccepted: boolean) {
    const deleteAttachmentModel: IdWithUserInfo = {
      Id: this.attachmentToDelete.id,
      AccountInfoId: this.accountService.memberDetails!.accountInfoId,
      MemberId: this.accountService.memberDetails!.memberId,
    };
    if (isAccepted) {
      this.attachmentService
        .deleteUpload(deleteAttachmentModel)
        .pipe(takeUntil(this.unsubscribeAll$))
        .subscribe((x: any) => {
          this.notificationService.showNotificationSuccess(
            'Attachment(s) deleted successfully.'
          );
          this.loadPaymentAttachments();
        });
    }
  }

  ngOnDestroy(): void {
    this.unsubscribeAll$.next();
    this.unsubscribeAll$.complete();
    this.subscriptions.unsubscribe();
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
    this.attachmentsListSubject.clearBuffer();
    this.gridState.skip = 0;
    this.gridState.take = take;
    this.footerRange = this.computeFooterRange();
    this.loadPaymentAttachments();
  }

  private loadVirtualScrollInitial(): void {
    const params: IdFilterSort = {
      ...this.gridState,
      skip: 0,
      take: this.virtualScrollPageSize,
    };
    this.loadedRanges.add(0);
    this.lastLoadedSkip = -1;
    this.lastScrollDirection = null;
    this.lastScrollTop = 0;
    this.attachmentsListSubject.getAll(params, true);
    setTimeout(() => {
      this.isAllInitializing = false;
      this.setupScrollPrefetch();
      this.subscriptions.add(
        (this.attachmentsListSubject as any).subscribe(() => {
          if (
            (this.isAllInitializing && this.attachmentsListSubject.getDataLength() === 0) ||
            this.isBufferOperationInProgress
          ) {
            return;
          }
          this.footerRange = this.computeFooterRange();
        })
      );
    }, 100);
  }

  private setupScrollPrefetch(): void {
    this.teardownScrollPrefetch();
    const gridEl = (document.querySelector('#attachments-grid .k-grid-content') as HTMLElement) || undefined;
    // Fallback: try any grid content in current component
    const contentEl = gridEl || (document.querySelector('.attachments-list .k-grid .k-grid-content') as HTMLElement);
    if (!contentEl) return;
    this.scrollPrefetchSub = fromEvent(contentEl, 'scroll', { passive: true })
      .pipe(
        throttleTime(this.scrollDebounceMs, undefined, { trailing: true }),
        filter(() => this.isAllSelected && !this.isLoadingMoreData && !this.isAllInitializing && !this.isBufferOperationInProgress)
      )
      .subscribe(() => {
        const scrollTop = contentEl.scrollTop;
        const clientHeight = contentEl.clientHeight;
        const scrollHeight = contentEl.scrollHeight;
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
          const totalCount = this.attachmentsListSubject.getCount();
          const currentData = (this.attachmentsListSubject.value || []) as PaymentAttachment[];
          const currentEnd = this.windowStart + currentData.length;
          if (currentEnd < totalCount) {
            this.lastScrollDirection = 'down';
            this.loadNextVirtualBatch();
          }
        } else if (scrollTop < this.UP_SCROLL_THRESHOLD && direction === 'up') {
          if (this.windowStart > 0) {
            this.lastScrollDirection = 'up';
            this.loadPreviousVirtualBatch();
          }
        }
      });
  }

  private loadNextVirtualBatch(): void {
    if (this.isLoadingMoreData) return;
    const now = Date.now();
    if (now - this.lastBatchLoadTime < this.batchLoadCooldown) return;
    this.lastBatchLoadTime = now;
    const currentData = (this.attachmentsListSubject.value || []) as PaymentAttachment[];
    const currentLength = currentData.length;
    const totalCount = this.attachmentsListSubject.getCount();
    const skipVal = this.windowStart + currentLength;
    if (skipVal >= totalCount) return;
    if (this.lastLoadedSkip === skipVal) return;
    this.lastLoadedSkip = skipVal;
    const params: IdFilterSort = { ...this.gridState, skip: skipVal, take: this.virtualScrollPageSize };
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
    const params: IdFilterSort = { ...this.gridState, skip: skipVal, take: this.virtualScrollPageSize };
    const currentLength = (this.attachmentsListSubject.value || []).length;
    if (currentLength >= this.maxWindowSize) {
      this.loadBatchWithCleanup(params, 'up');
    } else {
      this.loadBatchUpward(params);
    }
  }

  private loadBatch(params: IdFilterSort, append: boolean): void {
    this.isLoadingMoreData = true;
    const contentEl = (document.querySelector('.attachments-list .k-grid .k-grid-content') as HTMLElement);
    const scrollTopBefore = contentEl?.scrollTop || 0;
    const beforeLength = this.attachmentsListSubject.getDataLength();
    if (append) {
      this.attachmentsListSubject.append(params);
    } else {
      this.attachmentsListSubject.getAll(params, this.isAllSelected);
    }
    this.attachmentsListSubject
      .pipe(
        filter((arr: any) => ((arr?.length || 0) > beforeLength)),
        take(1)
      )
      .subscribe(() => {
        if (append && contentEl) {
          requestAnimationFrame(() => {
            contentEl.scrollTop = scrollTopBefore;
            this.lastScrollTop = scrollTopBefore;
          });
        }
        this.isLoadingMoreData = false;
      });
  }

  private loadBatchUpward(params: IdFilterSort): void {
    this.isLoadingMoreData = true;
    const beforeLength = this.attachmentsListSubject.getDataLength();
    this.attachmentsListSubject.prependBatch(params, false);
    const amountPrepended = this.virtualScrollPageSize;
    this.windowStart = Math.max(0, this.windowStart - amountPrepended);
    this.attachmentsListSubject.setWindowStart(this.windowStart);
    this.attachmentsListSubject
      .pipe(
        filter((arr: any) => ((arr?.length || 0) > beforeLength)),
        take(1)
      )
      .subscribe(() => {
        const currentLengthAfterPrepend = this.attachmentsListSubject.getDataLength();
        if (currentLengthAfterPrepend > this.maxWindowSize) {
          const excessRows = currentLengthAfterPrepend - this.maxWindowSize;
          this.attachmentsListSubject.removeFromBottom(excessRows);
        }
        const contentEl = (document.querySelector('.attachments-list .k-grid .k-grid-content') as HTMLElement);
        if (contentEl) {
          requestAnimationFrame(() => {
            const clientHeight = contentEl.clientHeight || 0;
            const scrollHeight = contentEl.scrollHeight || 0;
            if (clientHeight > 0 && scrollHeight > 0) {
              const target = scrollHeight * this.upwardResetPercentage;
              const desiredTop = Math.max(0, Math.min(scrollHeight - clientHeight, target));
              contentEl.scrollTop = desiredTop;
              this.lastScrollTop = desiredTop;
            }
            // Update footer range after upward cleanup to avoid stale values
            this.footerRange = this.computeFooterRange();
          });
        }
        this.isLoadingMoreData = false;
      });
  }

  private loadBatchWithEndAlignment(params: IdFilterSort): void {
    this.isLoadingMoreData = true;
    this.isBufferOperationInProgress = true;
    const contentEl = (document.querySelector('.attachments-list .k-grid .k-grid-content') as HTMLElement);
    const beforeLength = this.attachmentsListSubject.getDataLength();
    const totalCount = this.attachmentsListSubject.getCount();
    this.attachmentsListSubject.append(params);
    this.attachmentsListSubject
      .pipe(
        filter((arr: any) => ((arr?.length || 0) > beforeLength)),
        take(1)
      )
      .subscribe(() => {
        const currentData = (this.attachmentsListSubject.value || []) as PaymentAttachment[];
        let currentLength = currentData.length;
        if (currentLength > this.maxWindowSize) {
          const maxBatchesInWindow = Math.floor(this.maxWindowSize / this.virtualScrollPageSize);
          const lastBatchStart = Math.floor((totalCount - 1) / this.virtualScrollPageSize) * this.virtualScrollPageSize;
          const desiredWindowStart = Math.max(0, lastBatchStart - (maxBatchesInWindow - 1) * this.virtualScrollPageSize);
          const desiredWindowSize = totalCount - desiredWindowStart;
          const rowsToRemove = currentLength - desiredWindowSize;
          if (rowsToRemove > 0) {
            this.attachmentsListSubject.removeFromTop(rowsToRemove);
            this.windowStart = desiredWindowStart;
            this.attachmentsListSubject.setWindowStart(this.windowStart);
            currentLength = desiredWindowSize;
          }
        }
        requestAnimationFrame(() => {
          if (contentEl) {
            const scrollHeight = contentEl.scrollHeight || 0;
            const clientHeight = contentEl.clientHeight || 0;
            contentEl.scrollTop = Math.max(0, scrollHeight - clientHeight);
            this.lastScrollTop = contentEl.scrollTop;
          }
          setTimeout(() => {
            this.isBufferOperationInProgress = false;
            this.isLoadingMoreData = false;
            this.footerRange = this.computeFooterRange();
          }, 100);
        });
      });
  }

  private loadBatchWithCleanup(params: IdFilterSort, direction: 'up' | 'down'): void {
    this.isLoadingMoreData = true;
    this.isBufferOperationInProgress = true;
    if (direction === 'down') {
      const contentEl = (document.querySelector('.attachments-list .k-grid .k-grid-content') as HTMLElement);
      if (!contentEl) {
        this.isLoadingMoreData = false;
        this.isBufferOperationInProgress = false;
        return;
      }
      const actualRowHeight = this.getActualRowHeight(contentEl);
      const scrollTopBefore = contentEl.scrollTop;
      const beforeLength = this.attachmentsListSubject.getDataLength();
      this.attachmentsListSubject.append(params);
      this.attachmentsListSubject
        .pipe(
          filter((arr: any) => ((arr?.length || 0) > beforeLength)),
          take(1)
        )
        .subscribe(() => {
          requestAnimationFrame(() => {
            this.applySlidingWindowCleanupDownward(actualRowHeight, scrollTopBefore);
            this.attachmentsListSubject.setWindowStart(this.windowStart);
            this.isLoadingMoreData = false;
            this.isBufferOperationInProgress = false;
          });
        });
    } else {
      const contentEl = (document.querySelector('.attachments-list .k-grid .k-grid-content') as HTMLElement);
      const beforeLength = this.attachmentsListSubject.getDataLength();
      this.attachmentsListSubject.prependBatch(params, false);
      const amountPrepended = this.virtualScrollPageSize;
      this.windowStart = Math.max(0, this.windowStart - amountPrepended);
      this.attachmentsListSubject.setWindowStart(this.windowStart);
      this.attachmentsListSubject
        .pipe(
          filter((arr: any) => ((arr?.length || 0) > beforeLength)),
          take(1)
        )
        .subscribe(() => {
          requestAnimationFrame(() => {
            const currentData = (this.attachmentsListSubject.value || []) as PaymentAttachment[];
            const currentLength = currentData.length;
            if (currentLength > this.maxWindowSize) {
              const excessRows = currentLength - this.maxWindowSize;
              this.attachmentsListSubject.removeFromBottom(excessRows);
            }
            if (contentEl) {
              const clientHeight = contentEl.clientHeight || 0;
              const scrollHeight = contentEl.scrollHeight || 0;
              if (clientHeight > 0 && scrollHeight > 0) {
                const target = scrollHeight * this.upwardResetPercentage;
                const desiredTop = Math.max(0, Math.min(scrollHeight - clientHeight, target));
                contentEl.scrollTop = desiredTop;
                this.lastScrollTop = desiredTop;
              }
            // Update footer range after upward cleanup to avoid stale values
            this.footerRange = this.computeFooterRange();
            }
            this.isLoadingMoreData = false;
            this.isBufferOperationInProgress = false;
          });
        });
    }
  }

  private applySlidingWindowCleanupDownward(actualRowHeight: number, scrollTopBefore: number): void {
    const currentData = (this.attachmentsListSubject.value || []) as PaymentAttachment[];
    const currentLength = currentData.length;
    if (currentLength <= this.maxWindowSize) return;
    const excessRows = currentLength - this.maxWindowSize;
    const rowsToRemove = excessRows;
    if (rowsToRemove <= 0) return;
    const contentEl = (document.querySelector('.attachments-list .k-grid .k-grid-content') as HTMLElement);
    this.attachmentsListSubject.removeFromTop(rowsToRemove);
    this.windowStart += rowsToRemove;
    this.attachmentsListSubject.setWindowStart(this.windowStart);
    if (contentEl) {
      requestAnimationFrame(() => {
        const clientHeight = contentEl.clientHeight || 0;
        const scrollHeight = contentEl.scrollHeight || 0;
        if (clientHeight > 0 && scrollHeight > 0) {
          const target = scrollHeight * this.downwardResetPercentage;
          const desiredTop = Math.max(0, Math.min(scrollHeight - clientHeight, target));
          contentEl.scrollTop = desiredTop;
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
    this.attachmentsListSubject.clearBuffer();
    this.gridState.skip = 0;
    this.loadVirtualScrollInitial();
    this.footerRange = this.computeFooterRange();
  }

  private computeFooterRange(): string {
    const total = this.attachmentsListSubject.getCount();
    const currentLength = this.attachmentsListSubject.getDataLength();
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
