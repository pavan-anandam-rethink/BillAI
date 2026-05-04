import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  forwardRef,
  NgZone,
  OnDestroy,
  OnInit,
  Renderer2,
  TemplateRef,
  ViewChild,
} from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { Overlay } from '@angular/cdk/overlay';
import { GridFilterOperators, NotificationTypes } from '@core/enums/common';
import { ListFilterSort, PaymentPosting } from '@core/models/billing/';
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
import { SortDescriptor } from '@progress/kendo-data-query';
import {
  catchError,
  BehaviorSubject,
  forkJoin,
  Subject,
  map,
  Observable,
  of,
  Subscription,
  takeUntil,
  filter,
  fromEvent,
  throttleTime,
} from 'rxjs';
import { debounceTime, distinctUntilChanged, take, tap } from 'rxjs/operators';
import { PaymentNotesComponent } from './payment-notes';
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
import { DialogAction } from '@progress/kendo-angular-dialog';
import { NotificationService } from '@progress/kendo-angular-notification';
import { PaginationService } from '@core/services/billing/pagination.service';
import { ActivatedRoute, Router } from '@angular/router';
import { PaymentPostingListFilterComponent } from './payment-posting-list-filter/payment-posting-list-filter.component';
import { PaymentPostingFilterService } from '@app/billing/services/payment-posting-filter.service';
import { DatePipe } from '@angular/common';
import { ClaimsManagementFilterService } from '@app/billing/services/claims-management-filter.service';
import { CreateInvoiceFilterService } from '@app/billing/services/create-invoice-filter.service';
import { PendingCollectionFilterService } from '@app/billing/services/pending-collection-filter.service';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { ClaimService } from '@core/services/billing';
import { Breadcrumb } from '@core/models/billing/bread-crumb';
import { SafeResourceUrl } from '@angular/platform-browser';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'payment-posting-list',
  templateUrl: './payment-posting-list.component.html',
  styleUrls: ['./payment-posting-list.component.css', '../status-actions.css'],
})
export class PaymentPostingListComponent
  implements OnInit, AfterViewInit, OnDestroy
{
  private unsubscribeAll$ = new Subject<void>();
  public isSubjectLoading$: Observable<boolean>;
  public isLoading = false;
  public webTokenUrl: SafeResourceUrl | null = null;
  public isRevSpringEnabled: boolean = false;

  filterForm: FormGroup;
  showFilter = false;
  showActions = false;
  subscriptions = new Subscription();
  canEditApprove = false;

  showNotification = false;
  notificationType: NotificationTypes;
  notificationText: string;
  public mode: SelectableMode = 'multiple';
  showAddPaymentDialog = false;
  public isUserSelectAllChecked = false;
  paymentPostingListSubject: PaymentPostingListSubject;
  view: Observable<GridDataResult>;

  public selectedIds: number[] = [];
  redraw = false;
  gridState: ListFilterSort = new ListFilterSort();
  public dialogForTotalCount: boolean = false;
  public gatClaimsTabData: boolean = false;
  public dialogRef: MatDialogRef<any>;

  readonly pagingSettings: PagerSettings = {
    buttonCount: 5,
    type: 'numeric',
    pageSizes: true,
    previousNext: true,
  };
  headerText = 'Payment Posting';

  readonly selectableSettings: SelectableSettings = {
    checkboxOnly: true,
    mode: this.mode,
  };
  // =======================
  //  NEW – VIRTUAL SCROLL STATE

  // =======================
  public isAllSelected = false;
  public isLoadingMoreData = false;

  private virtualScrollPageSize = 50; // REQUIRED
  private loadedRanges = new Set<number>();

    private scrollPrefetchSub: Subscription | null = null;
    private prefetchThreshold = 0.8; // 80% for downward scroll
    private scrollDebounceMs = 80; // Faster response for smoother scrolling
    private lastBatchLoadTime: number = 0;
    private batchLoadCooldown: number = 150; // Lower cooldown to avoid hesitation
    private upwardResetPercentage: number = 0.3; // Reposition scrollbar to ~35%
    
    // Sliding window state
    private windowStart: number = 0; // First record index in current window
    private windowEnd: number = 0; // Last record index in current window
    private maxWindowSize: number = 200; // Max DOM rows before cleanup
    private lastScrollDirection: 'down' | 'up' | null = null;
    private lastScrollTop: number = 0;
    private lastLoadedSkip: number = -1; // Guard against duplicate loads
    private isAllInitializing: boolean = false;
    private isBufferOperationInProgress: boolean = false;

  serviceBreakdown = false;

  public confirmReconcileDialog: ConfirmDialog = new ConfirmDialog(
    false,
    'Manual Reconciliation',
    "Are you sure you'd like to perform this action?",
    'Reconcile',
    'Cancel'
  );
  private paymentToActionId: number;

  private deleteNonReconcileText =
    "Are you sure you'd like to perform this action?";
  private deleteReconcileText =
    'Deleting this payment will cause all amounts posted against reconciled claims to be voided, are you sure?';
  public confirmDeleteTransactionDialog: ConfirmDialog = new ConfirmDialog(
    false,
    'Delete Payment Transaction',
    this.deleteNonReconcileText,
    'Delete',
    'Cancel'
  );

  @ViewChild('testAccountDialog') testAccountDialog!: TemplateRef<any>;
  @ViewChild(GridComponent) paymentPostingGrid: GridComponent;
  @ViewChild(forwardRef(() => PaymentPostingListFilterComponent))
  paymentPostingListFilterComponent: PaymentPostingListFilterComponent;
  canEdit: boolean;

    gridPageSizes: any;
    constructor(private ngZone: NgZone,
        private dialogService: DialogService,
        private fb: FormBuilder,
        private paymentPostingService: PaymentPostingService,
        private sidebarService: SidebarService,
        private accountService: AccountMemberService,
        private paymentNotesService: PaymentNotesService,
        private paginationServices: PaginationService,
        private notificationService: NotificationHandlerService,
        private renderer: Renderer2,
        private readonly route: ActivatedRoute,
        private readonly router: Router,
        private filterService: PaymentPostingFilterService,
        private claimFilterService: ClaimsManagementFilterService,
        private createInvoiceFilter: CreateInvoiceFilterService,
        private pendingCollectionFilter: PendingCollectionFilterService,
        private datePipe: DatePipe,
        private claimsService: ClaimService,
        private cdr: ChangeDetectorRef,
        private dialog: MatDialog,
        private overlay: Overlay
    ) {
        this.paymentPostingListFilterComponent = new PaymentPostingListFilterComponent(
            datePipe,
            filterService,
            claimFilterService,
            createInvoiceFilter,
            pendingCollectionFilter);

    this.paymentPostingListSubject = new PaymentPostingListSubject(
      this.paymentPostingService
    );
    localStorage.setItem('lastPageSize', this.gridState.take.toString());
    this.view = this.paymentPostingListSubject.pipe(
      map(
        (data) => {
          let result = {
            data: data,
            total: this.paymentPostingListSubject.getCount(),
            isRevSpringEnabled: this.paymentPostingListSubject.getRevSpringEnabled()
          };
          this.isRevSpringEnabled = result.isRevSpringEnabled;
          return result;
        },
        tap(() => setTimeout(() => this.fitColumns(), 250))
      )
    );
    this.loadData(this.gridState);

    this.getGridPageSizes();

    this.subscriptions.add(
      this.accountService.accountMemberSettings.subscribe((x) => {
        if (x) {
          this.canEditApprove = this.accountService.checkPermissionLevel(
            AccountPermissions.BillingEditApprovedAppointments
          );
        }
      })
    );
  }
  
  breadcrumbs: Breadcrumb[] = [
    {
      label: 'Payment Posting',
      url: '/billing/paymentposting/list',
      tabIndex: 0,
    },
  ];

  // RevSpring helpers
  public isRevSpringPayment(dataItem: PaymentPosting): boolean {
    const name = (dataItem?.paymentMethodName || '').toLowerCase();
    return name === 'revspring';
  }

  public getPaymentIndicator(dataItem: PaymentPosting): 'M' | 'E' {
    // Force 'E' for RevSpring, otherwise preserve manual/electronic indicator
    if (this.isRevSpringPayment(dataItem)) {
      return 'E';
    }
    return dataItem?.isManual ? 'M' : 'E';
  }

  public getPaymentIndicatorTitle(dataItem: PaymentPosting): string {
    if (dataItem?.isManual) {
      return 'Entered Manually';
    }
    // Electronic
    return this.isRevSpringPayment(dataItem)
      ? 'Received Electronically - RevSpring'
      : 'Received Electronically - Payer';
  }

    onPageChange(event: PageChangeEvent): void {
        // Guard: Don't re-select if already in the same mode
        if (event.take === 0 && this.isAllSelected) {
            return; // Already in ALL mode, do nothing
        }
        
        if (event.take !== 0 && event.take === this.gridState.take && event.skip === this.gridState.skip) {
            return; // Same page size and position, do nothing
        }

        if (event.take === 0 && !this.isAllSelected) {           

        // If total < 1000, allow "All" without modal
        this.isAllSelected = true;
        this.isUserSelectAllChecked = false;
        this.selectedIds = [];
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
      return;
    }

    this.gridState.skip = event.skip;
    this.gridState.take = event.take;

        if (this.isAllSelected && event.take !== 0) {
            this.isAllSelected = false;
            this.isUserSelectAllChecked = false;
            this.selectedIds = [];
            this.loadedRanges.clear();
            this.windowStart = 0;
            this.windowEnd = 0;
            this.lastScrollDirection = null;
            this.lastLoadedSkip = -1;
            this.teardownScrollPrefetch();
        }
        this.paymentPostingListSubject.getAll(this.gridState, false); // reset virtual mode
    }

    get selectedKeysArray(): number[] {
  return Array.from(this.selectedIds);
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

  onSortChange(sortParams: SortDescriptor[]): void {
    this.gridState.sortingModels = sortParams;
    this.gridState.skip = 0;

        // Sorting in ALL mode must NOT load everything
        if (this.isAllSelected) {
            this.loadedRanges.clear();
            this.isUserSelectAllChecked = false;
            this.selectedIds = [];
            this.windowStart = 0;
            this.windowEnd = 0;
            this.lastLoadedSkip = -1;
            this.isAllInitializing = true;
            this.teardownScrollPrefetch();
            // Keep pager in ALL mode while using virtual scroll
            this.paginationServices.setPageSizes(0);
            this.loadVirtualScrollInitial(); // loads only 50
            return;
        }

    // Normal pagination
    this.loadData(this.gridState);
  }

  public onSelectChange(event: SelectionEvent): void {

    if (
      event.selectedRows.length > 1 &&
      !this.isUserSelectAllChecked
    ) {
      this.isUserSelectAllChecked = true;
    }

    // SELECT
    for (const row of event.selectedRows) {
      const id = row.dataItem.id;
      if (!this.selectedIds.includes(id)) {
        this.selectedIds.push(id);
      }
    }

    // DESELECT
    for (const row of event.deselectedRows) {
      const id = row.dataItem.id;
      this.selectedIds = this.selectedIds.filter(x => x !== id);
      this.isUserSelectAllChecked = false;
    }
  }

   onFilterChanged() {
        this.gridState.skip = 0;

        if (this.isAllSelected) {
            // pager to stay in ALL mode
            this.gridState.take = 9999;
            this.paginationServices.setPageSizes(0);

            this.reinitializeAllModeWithFilters();
            return;
        }
        this.isUserSelectAllChecked = false;
        this.selectedIds = [];
        this.loadData(this.gridState);
    }


    private reinitializeAllModeWithFilters(): void {
        this.isAllInitializing = true;
        this.isLoadingMoreData = false;

        // Reset virtual scroll state
        this.loadedRanges.clear();
        this.windowStart = 0;
        this.windowEnd = 0;
        this.lastScrollDirection = null;
        this.lastScrollTop = 0;
        this.lastLoadedSkip = -1;

        // Clear existing buffer
        this.paymentPostingListSubject.clearBuffer();

        // Force virtual batch size
        this.gridState.skip = 0;

        // Ensure pager shows ALL after filter clear in virtual mode
        this.paginationServices.setPageSizes(0);

        // Load first virtual chunk
        this.loadVirtualScrollInitial();
    }

  applySelectedFilters() {
    this.gridState.filterModels = [];
    if (this.paymentPostingListFilterComponent.selectedFunders.length > 0) {
      let funderobj: GridFilterModel = {
        propertyName: 'funderName',
        operatorName: 'eqany',
        value: this.paymentPostingListFilterComponent.selectedFunders
          .map((e) => e.funderName)
          .join(','),
      };
      this.gridState.filterModels.push(funderobj);
    }
    if (this.paymentPostingListFilterComponent.selectedStatuses.length > 0) {
      let statusobj: GridFilterModel = {
        propertyName: 'reconcileStatus',
        operatorName: 'eqany',
        value: this.paymentPostingListFilterComponent.selectedStatuses
          .map((e) => e.statusName)
          .join(','),
      };
      this.gridState.filterModels.push(statusobj);
    }
    if (
      this.paymentPostingListFilterComponent.selectedPaymentMethods.length > 0
    ) {
      let methodobj: GridFilterModel = {
        propertyName: 'paymentMethodId',
        operatorName: 'eqany',
        value: this.paymentPostingListFilterComponent.selectedPaymentMethods
          .map((e) => e.enumValue)
          .join(','),
      };
      this.gridState.filterModels.push(methodobj);
    }
    if (
      !(
        this.paymentPostingListFilterComponent.referenceNumber == '' ||
        this.paymentPostingListFilterComponent.referenceNumber == null ||
        this.paymentPostingListFilterComponent.referenceNumber == undefined
      )
    ) {
      let referenceObj: GridFilterModel = {
        propertyName: 'reference',
        operatorName: 'contains',
        value:
          this.paymentPostingListFilterComponent.referenceNumber.toLowerCase(),
      };
      this.gridState.filterModels.push(referenceObj);
    }
    if (
      !(
        this.paymentPostingListFilterComponent.selectedStartDate == undefined ||
        this.paymentPostingListFilterComponent.selectedStartDate == ''
      ) &&
      !(
        this.paymentPostingListFilterComponent.selectedEndDate == undefined ||
        this.paymentPostingListFilterComponent.selectedEndDate == ''
      )
    ) {
      let dates =
        this.paymentPostingListFilterComponent.selectedStartDate +
        ',' +
        this.paymentPostingListFilterComponent.selectedEndDate;
      let methodobj: GridFilterModel = {
        propertyName: 'receivedDate',
        operatorName: 'rangedate',
        value: dates,
      };
      this.gridState.filterModels.push(methodobj);
    }
    if (this.paymentPostingListFilterComponent.selectedModes.length > 0) {
      let modeobj: GridFilterModel = {
        propertyName: 'isManual',
        operatorName: 'eqany',
        value: this.paymentPostingListFilterComponent.selectedModes
          .map((e) => e.modeName)
          .join(','),
      };
      this.gridState.filterModels.push(modeobj);
    }
  }

  loadData(params: ListFilterSort): void {
    this.applySelectedFilters();
    this.isSubjectLoading$ = this.paymentPostingListSubject.getLoading();
    this.filterService.setFilter(this.paymentPostingListFilterComponent);
    this.paymentPostingListSubject.getAll(params);
  }

  reloadData() {
    this.loadData(this.gridState);
    this.isUserSelectAllChecked = false;
    this.selectedIds = [];
    this.selectedIds = [];
  }

  fitColumns(): void {
    this.ngZone.onStable
      .asObservable()
      .pipe(take(1))
      .subscribe(() => {
        this.paymentPostingGrid.autoFitColumns(
          this.paymentPostingGrid.columnList.toArray()
        );
        this.paymentPostingGrid.autoFitColumns();
      });
  }

  ngOnDestroy() {
    this.paymentPostingListSubject.unsubscribe();
    this.unsubscribeAll$.next();
    this.unsubscribeAll$.complete();
  }

  ngAfterViewInit() {
    this.fitColumns();
    if (this.filterService.isFilterSet) {
      window.setTimeout(
        (res) =>
          (<HTMLElement>(
            document.querySelector('.filter-btn .outlined-btn')
          ))!.click(),
        1000
      );
    }
  }

  ngOnInit() {
    this.filterForm = this.fb.group({
      paymentMethodId: this.fb.group({
        inputData: [''],
        value: [''],
        operator: [GridFilterOperators.equalAny],
      }),
      receivedDate: this.fb.group({
        value: [''],
        operator: [GridFilterOperators.rangeDate],
      }),
      funderName: this.fb.group({
        inputData: [''],
        value: [''],
        operator: [GridFilterOperators.equalAny],
      }),
      reconcileStatus: this.fb.group({
        value: [''],
        operator: [GridFilterOperators.equalAny],
      }),
    });
    this.paymentPostingListSubject.subscribe(data => {
  if (
    !this.isAllSelected ||               
    !this.isUserSelectAllChecked ||     
    !data || data.length === 0
  ) {
    return;
  }

  for (const item of data) {
    if (!this.selectedIds.includes(item.id)) {
      this.selectedIds.push(item.id);
    }
  }
});

    this.checkPermission();
  }

  addNoteBulk() {
    if (!this.selectedIds.length) return;
    const dialogRef = this.dialogService.open({
      content: AddClaimNotesDialogComponent,
      title: 'Add Note',
      width: 540,
    });
    //Make Notes Dialog generic
    dialogRef.content.instance.claimIds = this.selectedIds;

    this.subscriptions.add(
      dialogRef.result.subscribe(
        (result: DialogCloseResult | AddClaimNotesDialogResult) => {
          if (
            !(result instanceof DialogCloseResult) &&
            (result as AddClaimNotesDialogResult).data
          ) {
            let paymentModel: PaymentNoteSaveModel = {
              paymentId: 0,
              remindDate: undefined,
              note: '',
              memberId: 0,
            };
            let paymentsToAddNote: PaymentNoteSaveModel[] = [];
            this.selectedIds.forEach((paymentId) => {
              paymentModel.remindDate = (
                result as AddClaimNotesDialogResult
              ).data[0].remindDate;
              paymentModel.note = (
                result as AddClaimNotesDialogResult
              ).data[0].note;
              paymentModel.paymentId = paymentId;
              paymentModel.memberId =
                this.accountService.memberDetails.memberId;
              paymentsToAddNote.push(Object.assign({}, paymentModel));
            });
            this.paymentNotesService
              .addToSeveral(paymentsToAddNote)
              .subscribe(() => {
                this.notificationService.showNotificationSuccess(
                  this.selectedIds.length + ' Note(s) added successfully.'
                );
              });
          }
        }
      )
    );
  }

  checkPermission() {
    this.accountService.accountMemberSettings.subscribe((x) => {
      if (x) {
        this.canEdit = this.accountService.checkPermissionLevel(
          AccountPermissions.BillingEditApprovedAppointments
        );
      }
    });
  }

  toggleFilter(event: boolean) {
    this.showFilter = event;
    this.paymentPostingListFilterComponent.opened = event;
  }

  openReconcileDialog(id: number): void {
    this.paymentToActionId = id;
    this.confirmReconcileDialog.opened = true;
  }

  closeReconcileDialog(): void {
    this.paymentToActionId = 0;
    this.confirmReconcileDialog.opened = false;
  }

  openDeletePaymentDialog(item: PaymentPosting): void {
    this.paymentToActionId = item.id;
    this.confirmDeleteTransactionDialog.message = item.isManualReconciled
      ? this.deleteReconcileText
      : this.deleteNonReconcileText;
    this.confirmDeleteTransactionDialog.opened = true;
  }

  closeDeletePaymentDialog(): void {
    this.paymentToActionId = 0;
    this.confirmDeleteTransactionDialog.opened = false;
  }

  showNotificationComponent(type: NotificationTypes, text: string): void {
    this.notificationType = type;
    this.notificationText = text;
    this.showNotification = true;

    window.setTimeout(() => this.hideNotificationComponent(), 5000);
  }

  hideNotificationComponent(): void {
    this.showNotification = false;
  }

  /*--------actions----------*/

  addPaymentDialogToggle() {
    this.showAddPaymentDialog = !this.showAddPaymentDialog;
  }

  handleIframeUrl(url: SafeResourceUrl) {
    this.webTokenUrl = url;
    this.dialogRef =this.dialog.open(this.testAccountDialog, {
      width: '100%',  
      height: '100% !important',  
      disableClose: true,
      hasBackdrop: true,

      autoFocus: false, 
      restoreFocus: false, 
      scrollStrategy: this.overlay.scrollStrategies.noop(),
      panelClass: 'iframe-dialog'
    });

    this.dialogRef.backdropClick().subscribe(e => {
      e.stopPropagation(); 
    });

    this.showError = false;
  }

  onReconcilePayment(status: any): void {
    if (status) {
      this.paymentPostingService
        .reconcilePayment(
          this.paymentToActionId == 0
            ? this.selectedIds
            : [this.paymentToActionId]
        )
        .subscribe(
          (result) => {
            let resultData: any = result;
            let successCount = resultData.length;
            let failedCount =
              this.paymentToActionId === 0
                ? this.selectedIds.length - successCount
                : successCount === 0
                ? 1
                : 0;
            if (this.paymentToActionId === 0) {
              // Bulk
              this.showNotifications(
                successCount,
                failedCount,
                this.selectedIds.length
              );
            } else {
              // Manual
              if (failedCount > 0) {
                this.notificationService.showNotificationError(
                  `${failedCount} payment(s) is not reconciled`
                );
              }
              if (successCount > 0) {
                this.notificationService.showNotificationSuccess(
                  `${successCount} payment(s) were manually reconciled`
                );
              }
            }
          },
          (error) => {
            let notifyText = error.error;
            if (this.selectedIds.length >= 1) {
              this.notificationService.showNotificationError(
                this.selectedIds.length + ' ' + notifyText
              );
              this.selectedIds = [];
            } else if (this.selectedIds.length == 0) {
              this.notificationService.showNotificationError(notifyText);
            }
          },
          () => {
            this.closeReconcileDialog();
            this.loadData(this.gridState);
            this.reloadData();
          }
        );
    } else {
      this.closeReconcileDialog();
    }
  }
  showNotifications(
    successCount: number,
    failedCount: number,
    totalSelected: number
  ) {
    if (failedCount > 0) {
      this.notificationService.showNotificationError(
        `${failedCount} payment(s) is not reconciled`
      );
    }

    if (successCount > 0) {
      this.notificationService.showNotificationSuccess(
        `${successCount} payment(s) were manually reconciled`
      );
    }

    if (failedCount === 0 && totalSelected > 0 && successCount === 0) {
      this.notificationService.showNotificationError(
        `${totalSelected} payment(s) is not reconciled`
      );
    }
  }

  onDeletePaymentTransaction(status: any): void {
    if (status) {
      this.isLoading = true;
      this.paymentPostingService
        .deletePayment(
          this.paymentToActionId == 0
            ? this.selectedIds
            : [this.paymentToActionId]
        )
        .subscribe(
          () => {
            // let notifyText = `Payment ID ${this.paymentToActionId} was successfully deleted`;
            if (this.selectedIds.length >= 1) {
              this.notificationService.showNotificationSuccess(
                this.selectedIds.length +
                  ' payment(s) were successfully deleted'
              );
              this.selectedIds = [];
            } else if (this.selectedIds.length == 0) {
              this.notificationService.showNotificationSuccess(
                'payment is successfully deleted'
              );
            }
            this.loadData(this.gridState);
            this.isLoading = false;
          },
          (error) => {
            this.isLoading = false;
            let notifyText = error.error;
            if (this.selectedIds.length >= 1) {
              this.notificationService.showNotificationError(
                this.selectedIds.length + ' ' + notifyText
              );
              this.selectedIds = [];
            } else if (this.selectedIds.length == 0) {
              this.notificationService.showNotificationError(notifyText);
            }
          },
          () => {
            this.closeDeletePaymentDialog();
          }
        );
    } else {
      this.closeDeletePaymentDialog();
      this.loadData(this.gridState);
    }
  }

  paymentNotes(payment: PaymentPosting) {
    this.sidebarService
      .openRight(PaymentNotesComponent, true, 'md')
      .subscribe((rsidebarRef) => rsidebarRef.instance.setData(payment));
  }

  editPayment() {
    //TODO: edit payment by id
  }

  /* bulk actions */

  // billNextFunderBulk() {
  //     if (this.selectedIds.length === 0) return;
  //     console.log('bild-next-click');
  // }

  // writeOffClaim() {
  //     if (this.selectedIds.length === 0) return;
  //     console.log('writeOffClaim-click');
  // }

  // voidBulk() {
  //     if (this.selectedIds.length === 0) return;
  //     console.log('void-click');
  // }

  // flagBulk() {
  //     if (this.selectedIds.length === 0) return;
  //     console.log('flag-click');
  // }

  // addNoteBulk() {
  //     if (this.selectedIds.length === 0) return;
  //     this.addNote();
  // }

  openReconcileDialogBulk() {
    if (this.selectedIds.length === 0) return;
    this.paymentToActionId = 0;
    this.confirmReconcileDialog.opened = true;
  }

  openDeletePaymentDialogBulk() {
    if (this.selectedIds.length === 0) return;
    this.paymentToActionId = 0;
    this.confirmDeleteTransactionDialog.opened = true;
  }
  /*--------#actions----------*/

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

    private loadVirtualScrollInitial(): void {
        // Apply filters first so params captures updated filterModels
        this.applySelectedFilters();

        const params = { ...this.gridState };
        params.skip = 0;
        params.take = this.virtualScrollPageSize;
        this.loadedRanges.add(0);

      // Reset scroll state and duplicate guard
      this.lastLoadedSkip = -1;
      this.lastScrollDirection = null;
      this.lastScrollTop = 0;

        this.paymentPostingListSubject.getAll(params, true);
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
                
                // Calculate scroll percentage
                const scrollPercentage = (scrollTop + clientHeight) / scrollHeight;
                
                // Detect scroll direction
                const direction = scrollTop > this.lastScrollTop ? 'down' : 'up';
                this.lastScrollTop = scrollTop;

                // Reset duplicate-load guard when direction changes
                if (this.lastScrollDirection && direction !== this.lastScrollDirection) {
                  this.lastLoadedSkip = -1;
                }
                
                // Avoid triggering at the very top only if we are already at the first window
                if (scrollTop <= 1 && direction === 'up' && this.windowStart === 0) {
                  return;
                }
                
                // Downward scroll: 80% threshold
                if (scrollPercentage >= this.prefetchThreshold && direction === 'down') {
                  const totalCount = this.paymentPostingListSubject.getCount();
                  const currentData = this.paymentPostingListSubject.value || [];
                  const currentEnd = this.windowStart + currentData.length;
                  
                  // Only trigger if there's more data to load
                  if (currentEnd < totalCount) {
                    this.lastScrollDirection = 'down';
                    this.ngZone.run(() => this.loadNextVirtualBatch());
                  }
                }
                
                // Upward scroll: 20% threshold (less strict to avoid missed calls)
                else if (scrollPercentage <= 0.20 && direction === 'up') {
                  // Only trigger if we're not at the very beginning
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

        // Cooldown check to prevent rapid consecutive loads
        const now = Date.now();
        if (now - this.lastBatchLoadTime < this.batchLoadCooldown) return;
        this.lastBatchLoadTime = now;

        // Ensure latest filters are applied before building params
        this.applySelectedFilters();

        const currentData = this.paymentPostingListSubject.value || [];
        const currentLength = currentData.length;
        const totalCount = this.paymentPostingListSubject.getCount();
        const skipVal = this.windowStart + currentLength;

        // Check if we're trying to load beyond the end
        if (skipVal >= totalCount) return;

        // Prevent duplicate requests
        if (this.lastLoadedSkip === skipVal) return;
        this.lastLoadedSkip = skipVal;

        const params = { ...this.gridState };
        params.skip = skipVal;
        params.take = this.virtualScrollPageSize;

        // Calculate how many items remain after this batch
        const itemsAfterThisLoad = totalCount - skipVal;
        const willLoadLastBatch = itemsAfterThisLoad <= this.virtualScrollPageSize;

        // If we're loading the last batch and will exceed maxWindowSize, use special end-alignment
        if (willLoadLastBatch && (currentLength + itemsAfterThisLoad) > this.maxWindowSize) {
            this.loadPaymentPostingBatchWithEndAlignment(params);
        } else if (currentLength >= this.maxWindowSize) {
            this.loadPaymentPostingBatchWithCleanup(params, 'down');
        } else {
            this.loadPaymentPostingBatch(params, true);
        }
    }

    private loadPreviousVirtualBatch(): void {
        if (this.isLoadingMoreData) {
            return;
        }
        
        // Can't go back if already at the start
        if (this.windowStart === 0) return;

        // Cooldown check to prevent rapid consecutive loads
        const now = Date.now();
        if (now - this.lastBatchLoadTime < this.batchLoadCooldown) {
            return;
        }
        this.lastBatchLoadTime = now;

        // Ensure latest filters are applied before building params
        this.applySelectedFilters();

        const skipVal = Math.max(0, this.windowStart - this.virtualScrollPageSize);
        
        // Prevent duplicate requests
        if (this.lastLoadedSkip === skipVal) {
            return;
        }
        this.lastLoadedSkip = skipVal;

        const params = { ...this.gridState };
        params.skip = skipVal;
        params.take = this.virtualScrollPageSize;

        const currentLength = (this.paymentPostingListSubject.value || []).length;
        // Always use cleanup for upward scroll to maintain window size
        if (currentLength >= this.maxWindowSize) {
            this.loadPaymentPostingBatchWithCleanup(params, 'up');
        } else {
            this.loadPaymentPostingBatchUpward(params);
        }
    }

    private loadPaymentPostingBatch(params: any, append: boolean): void {
        this.isLoadingMoreData = true;
        this.applySelectedFilters();

        const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
        const scrollTopBefore = gridEl?.scrollTop || 0;

        const beforeLength = this.paymentPostingListSubject.getDataLength();

        if (append) {
          this.paymentPostingListSubject.append(params);
        } else {
          this.paymentPostingListSubject.getAll(params, this.isAllSelected);
        }

        // Wait for buffer to grow before restoring scroll and unlocking
        this.paymentPostingListSubject
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

    private loadPaymentPostingBatchUpward(params: any): void {
      this.isLoadingMoreData = true;
      this.applySelectedFilters();

      const beforeLength = this.paymentPostingListSubject.getDataLength();
      this.paymentPostingListSubject.prependBatch(params, false);

      const amountPrepended = this.virtualScrollPageSize;
      this.windowStart = Math.max(0, this.windowStart - amountPrepended);

      // Wait for buffer to grow before unlocking
      this.paymentPostingListSubject
        .pipe(
          filter(arr => (arr?.length || 0) > beforeLength),
          take(1)
        )
        .subscribe(() => {
          // If prepending pushed us over the max window, trim from bottom
          const currentLengthAfterPrepend = this.paymentPostingListSubject.getDataLength();
          if (currentLengthAfterPrepend > this.maxWindowSize) {
            const excessRows = currentLengthAfterPrepend - this.maxWindowSize;
            this.paymentPostingListSubject.removeFromBottom(excessRows);
          }
          this.isLoadingMoreData = false;
          this.cdr.detectChanges();
        });
    }

    private loadPaymentPostingBatchWithEndAlignment(params: any): void {
        this.isLoadingMoreData = true;
        this.isBufferOperationInProgress = true;
        this.applySelectedFilters();

        const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
        const beforeLength = this.paymentPostingListSubject.getDataLength();
        const totalCount = this.paymentPostingListSubject.getCount();
        
        // Append the last batch
        this.paymentPostingListSubject.append(params);

        this.paymentPostingListSubject
            .pipe(
                filter(arr => (arr?.length || 0) > beforeLength),
                take(1)
            )
            .subscribe(() => {
                this.ngZone.run(() => {
                    const currentData = this.paymentPostingListSubject.value || [];
                    let currentLength = currentData.length;

                    // Calculate batch-aligned window start for the last window
                    if (currentLength > this.maxWindowSize) {
                        // Calculate desired window start: align to show last N complete batches
                        const maxBatchesInWindow = Math.floor(this.maxWindowSize / this.virtualScrollPageSize);
                        const lastBatchStart = Math.floor((totalCount - 1) / this.virtualScrollPageSize) * this.virtualScrollPageSize;
                        const desiredWindowStart = Math.max(0, lastBatchStart - (maxBatchesInWindow - 1) * this.virtualScrollPageSize);
                        
                        // Calculate rows to remove to achieve desired window
                        const desiredWindowSize = totalCount - desiredWindowStart;
                        const rowsToRemove = currentLength - desiredWindowSize;

                        if (rowsToRemove > 0) {
                            this.paymentPostingListSubject.removeFromTop(rowsToRemove);
                            this.windowStart = desiredWindowStart;
                            currentLength = desiredWindowSize;
                        }
                    }
                    // Force change detection to ensure buffer is updated
                    this.cdr.detectChanges();

                    // Adjust scroll position to bottom
                    requestAnimationFrame(() => {
                        if (gridEl) {
                            const scrollHeight = gridEl.scrollHeight || 0;
                            const clientHeight = gridEl.clientHeight || 0;
                            gridEl.scrollTop = Math.max(0, scrollHeight - clientHeight);
                            this.lastScrollTop = gridEl.scrollTop;
                        }
                        
                        // Allow a brief moment for buffer to settle before allowing scroll events
                        setTimeout(() => {
                            this.isBufferOperationInProgress = false;
                            this.isLoadingMoreData = false;
                        }, 100);
                    });
                });
            });
    }

    private loadPaymentPostingBatchWithCleanup(params: any, direction: 'up' | 'down'): void {
        this.isLoadingMoreData = true;
        this.applySelectedFilters();

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

          const beforeLength = this.paymentPostingListSubject.getDataLength();
          this.paymentPostingListSubject.append(params);

          // Wait for append completion, then cleanup and unlock
          this.paymentPostingListSubject
            .pipe(
              filter(arr => (arr?.length || 0) > beforeLength),
              take(1)
            )
            .subscribe(() => {
              requestAnimationFrame(() => {
                this.applySlidingWindowCleanupDownward(actualRowHeight, scrollTopBefore);
                this.ngZone.run(() => this.cdr.detectChanges());
                this.isLoadingMoreData = false;
              });
            });
        } else {
          const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
          const beforeLength = this.paymentPostingListSubject.getDataLength();
          
          this.paymentPostingListSubject.prependBatch(params, false);
            
          const amountPrepended = this.virtualScrollPageSize;
          this.windowStart = Math.max(0, this.windowStart - amountPrepended);

          // Wait for prepend completion, then cleanup and unlock
          this.paymentPostingListSubject
            .pipe(
              filter(arr => (arr?.length || 0) > beforeLength),
              take(1)
            )
            .subscribe(() => {
              this.ngZone.run(() => {
                requestAnimationFrame(() => {
                  // Ensure we maintain exactly maxWindowSize after prepend
                  const currentData = this.paymentPostingListSubject.value || [];
                  const currentLength = currentData.length;
                  
                  if (currentLength > this.maxWindowSize) {
                    const excessRows = currentLength - this.maxWindowSize;
                    this.paymentPostingListSubject.removeFromBottom(excessRows);
                    
                    // Verify after removal
                    const finalLength = this.paymentPostingListSubject.getDataLength();
                  }
                  
                  // Reposition scrollbar to avoid immediate re-trigger at 20%
                  if (gridEl) {
                    const clientHeight = gridEl.clientHeight || 0;
                    const scrollHeight = gridEl.scrollHeight || 0;
                    if (clientHeight > 0 && scrollHeight > 0) {
                      const target = scrollHeight * this.upwardResetPercentage - clientHeight;
                      const desiredTop = Math.max(0, Math.min(scrollHeight - clientHeight, target));
                      gridEl.scrollTop = desiredTop;
                      this.lastScrollTop = desiredTop;
                    }
                  }
                  
                  this.cdr.detectChanges();
                  this.isLoadingMoreData = false;
                });
              });
            });
        }
    }

    private applySlidingWindowCleanupDownward(actualRowHeight: number, scrollTopBefore: number): void {
        const currentData = this.paymentPostingListSubject.value || [];
        const currentLength = currentData.length;

        if (currentLength <= this.maxWindowSize) return;

        // Remove exactly the excess to maintain maxWindowSize
        const excessRows = currentLength - this.maxWindowSize;
        const rowsToRemove = excessRows; // Remove all excess, not just batch size

        if (rowsToRemove <= 0) return;

        const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
        
        // Remove rows and update window
        this.paymentPostingListSubject.removeFromTop(rowsToRemove);
        this.windowStart += rowsToRemove;
        
        // Preserve scroll position synchronously after DOM updates
        if (gridEl) {
            requestAnimationFrame(() => {
                const scrollAdjustment = rowsToRemove * actualRowHeight;
                const newScrollTop = Math.max(0, scrollTopBefore - scrollAdjustment);
                gridEl.scrollTop = newScrollTop;
                this.lastScrollTop = newScrollTop;
            });
        }
    }

    private applySlidingWindowCleanup(direction: 'up' | 'down'): void {
        const currentData = this.paymentPostingListSubject.value || [];
        const currentLength = currentData.length;

        if (currentLength <= this.maxWindowSize) return;

        // Remove exactly the excess to maintain maxWindowSize
        const excessRows = currentLength - this.maxWindowSize;
        const rowsToRemove = excessRows; // Remove all excess rows

        if (rowsToRemove <= 0) return;

        const gridEl = this.paymentPostingGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
        const scrollTopBefore = gridEl?.scrollTop || 0;
        
        // Calculate actual row height from DOM before cleanup
        let actualRowHeight = 40; // default
        if (gridEl) {
            const firstRow = gridEl.querySelector('tbody tr');
            if (firstRow) {
                actualRowHeight = firstRow.getBoundingClientRect().height;
            }
        }

        if (direction === 'down') {
            this.paymentPostingListSubject.removeFromTop(rowsToRemove);
            this.windowStart += rowsToRemove;
            
            // Preserve scroll position immediately
            if (gridEl) {
                requestAnimationFrame(() => {
                    const scrollAdjustment = rowsToRemove * actualRowHeight;
                    const newScrollTop = Math.max(0, scrollTopBefore - scrollAdjustment);
                    gridEl.scrollTop = newScrollTop;
                    this.lastScrollTop = newScrollTop;
                });
            }
        } else {
            // Upward scroll: remove from bottom to maintain exactly 200 rows
            this.paymentPostingListSubject.removeFromBottom(rowsToRemove);
            // windowStart doesn't change when removing from bottom

            // Reposition scrollbar to avoid immediate re-trigger at 20%
            if (gridEl) {
                requestAnimationFrame(() => {
                    const clientHeight = gridEl.clientHeight || 0;
                    const scrollHeight = gridEl.scrollHeight || 0;
                    if (clientHeight > 0 && scrollHeight > 0) {
                        // Position at 35% to give buffer before 20% threshold
                        const target = scrollHeight * this.upwardResetPercentage - clientHeight;
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

        const currentData = this.paymentPostingListSubject.value || [];
        const total = this.paymentPostingListSubject.getCount();
        
        if (currentData.length === 0) {
            return '0';
        }

        // Show actual DOM window: windowStart to windowStart + actual data length
        // But ensure we never exceed total count
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
      return this.paymentPostingListSubject.getDataLength();
    const skip =
      this.gridState && this.gridState.skip ? this.gridState.skip : 0;
    const take =
      this.gridState && this.gridState.take ? this.gridState.take : 20;
    return Math.min(skip + take, total);
  }

  openIframeDialog(): void {
    this.dialogRef = this.dialog.open(this.testAccountDialog, {
      width: '90%',
      disableClose: true
    });
  }

  onCloseClick(): void {
    const shouldClose = window.confirm(
      'Are you sure you want to cancel this payment?'
    );

    if (shouldClose) {
      this.dialogRef.close();
      this.loadData(this.gridState);
    }
  }

  showError = false;

  async onIframeLoad() {    
    let isUS = await this.checkCountry();
    if (!isUS) {
      this.showError = true;
    }
  }

  // This method is to check the IP for the US only restriction for PersonaPay RevSpring Payment
  checkCountry(): Promise<boolean> {
    return fetch('https://api.country.is/')
      .then(res => res.json())
      .then(data => {
        return data.country === 'US';
      })
      .catch(() => false); // fail closed
  }
}
