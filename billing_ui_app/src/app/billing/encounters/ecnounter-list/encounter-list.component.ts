import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  forwardRef,
  HostListener,
  NgZone,
  OnDestroy,
  OnInit,
  Renderer2,
  TemplateRef,
  ViewChild,
} from '@angular/core';
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
  firstValueFrom,
  take,
} from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
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
import {
  DialogAction,
  DialogCloseResult,
  DialogService,
} from '@progress/kendo-angular-dialog';
import { Locale } from '@app/locale';
import { AccountPermissions } from '@core/enums/account';
import { ClaimSubmissionStatus } from '@core/enums/billing/claim-submission-status';
import {
  ClaimHeader,
  ClaimHeaderSearch,
} from '@core/models/billing/claim-header-search';
import { ClaimNextFundersAndControlNumberModel } from '@core/models/billing/claim-patient-funder-option-model';
import { ClaimsCount } from '@core/models/billing/claims-count';
import { MemberViewSettings } from '@core/models/billing/member-view-settings';
import { ConfirmDialog } from '@core/models/common';
import { ClaimNotesService, ClaimService } from '@core/services/billing';
import {
  AddClaimNotesDialogComponent,
  AddClaimNotesDialogResult,
} from './add-claim-notes-dialog/add-claim-notes-dialog.component';
import {
  BillNextFunderDialogComponent,
  BillNextFunderDialogResult,
} from './bill-next-funder-dialog/bill-next-funder-dialog.component';
import { ClaimFiltersComponent } from './claim-filters/claim-filters.component';
import {
  RebillClaimDialogComponent,
  RebillClaimDialogResult,
} from './rebill-claim-dialog/rebill-claim-dialog.component';
import {
  VoidClaimDialogComponent,
  VoidClaimDialogResult,
} from './void-claim-dialog/void-claim-dialog.component';
import { WriteOffClaimDialogComponent } from './write-off-claim-dialog/write-off-claim-dialog.component';
import { MatTabGroup } from '@angular/material/tabs';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { SidebarService } from '@app/shared/components/sidebar';
import { EncounterErrorsAlertsComponent } from '../encounter-view/encounter-errors-alerts/encounter-errors-alerts.component';
import { ConfirmationDialogComponent } from '@app/shared/components/confirmation-dialog/confirmation-dialog.component';
import { HFCAprintComponent } from '@app/shared/components';
import { ClaimNotesComponent } from '@app/billing/payment-posting';
import {
  ClaimNoteGetModel,
  ClaimNoteModel,
  ClaimNotesSaveModel,
} from '@core/models/billing/notes/cliam-posting-note';
import { PatientDetailsComponent } from '@app/billing/payment-posting/payment-posting-view/payment-details/patient-details';
import { PaginationService } from '@core/services/billing/pagination.service';
import { ClaimDashboardListSubject } from '@core/subjects/claim-dashboard-list.subject';
import { ClaimToPrint } from '@app/shared/components/HFCA/HFCAprint.component';
import { environment } from 'src/environments/environment';
import { ClaimOrChargeToWriteOff } from '@core/models/billing/write-off-claim-model';
import { ClaimSubmissionType } from '@core/enums/billing/claim-submission-type';
import { billingMode } from '@core/enums/billing/billingMode';
import {
  ClaimsSubmitModel,
  SecondaryFunderDetailsModel,
} from '@core/models/billing/claims-submit-model';
import { ClaimsSecondaryBillingRebillModel } from '@core/models/billing/claims-secondary-billing-rebill-model';
import { Helper } from '../common/common-helper';
import { ClaimsManagementFilterService } from '@app/billing/services/claims-management-filter.service';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { WriteoffService } from '@core/services/billing/writeoff.service';
import { TakeUntilDestroyService } from '@core/services/common/takeuntill-destroy.service';
import { CarcCodes } from '@core/models/billing/carc-codes';
import { EncounterListDetailsComponent } from './encounter-list-details/encounter-list-details.component';
import { ClaimListingTab } from '@core/enums/billing/claim-listing-tab';
import { EdiFileType } from '@core/enums/billing/edi-file-type';
import { NotificationService } from '@core/services/account/notification.service';
import { FlagClaimDialogComponent } from './flag-claim-dialog/flag-claim-dialog.component';
import { FlagClaimsRequest } from '@core/models/billing/claim';
import {
  AssigneeModel,
  AssigneeRequestModel,
} from '@core/models/billing/assignee-model';
import { ClaimEdiFilesModel } from '@core/models/billing/claim-filter-option-model';
import { ExternalCodes } from '@core/models/billing/external-codes';

export interface ClaimHeadersDataResult {
  data: ClaimHeader[];
  totalCount: number;
  claimsCount: ClaimsCount;
}

export interface IndexResult {
  index: number;
  anchor: HTMLAnchorElement;
  claimId: number;
}

const SubmissionStatusesToRejectClaimAction = [
  ClaimSubmissionStatus.FunderDenied,
  ClaimSubmissionStatus.FunderProcessed,
  ClaimSubmissionStatus.Abandoned,
  ClaimSubmissionStatus.ClearinghouseRejected,
  ClaimSubmissionStatus.FunderRejected,
];

@Component({
  selector: 'encounter-list',
  templateUrl: './encounter-list.component.html',
  styleUrls: ['./encounter-list.component.css', '../status-actions.css'],
})
export class EncounterListComponent
  implements AfterViewInit, OnInit, OnDestroy {

  /** Sentinel value used for gridState.take when the user selects "All records" mode. */
  private readonly ALL_RECORDS_TAKE = 99999;
  @ViewChild(forwardRef(() => ClaimFiltersComponent))
  claimFiltersComponent: ClaimFiltersComponent;
  @ViewChild('claimListingTabGroup') claimListingTabGroup: MatTabGroup;
  @ViewChild('encountersGrid') claimsGrid: GridComponent;
  @ViewChild('approveNoErrors')
  confirmApproveNoErrorsDialog: ConfirmationDialogComponent;
  @ViewChild('approveWithErrors')
  confirmApproveWithErrorsDialog: ConfirmationDialogComponent;
  @ViewChild(EncounterListDetailsComponent)
  detailComponents!: EncounterListDetailsComponent;
  @ViewChild('reasonTooltipTemplate', { static: true })
  reasonTooltipTemplate!: TemplateRef<any>;
  private unsubscribeAll$ = new Subject<void>();
  popupOpen: boolean[][] = []; // tracks open popups per row per pill
  selectAllChecked = false; 
  gridData: any[] = [];

  confirmApproveNoErrors = new ConfirmDialog(
    false,
    'Confirmation',
    'Approve?',
    'Approve',
    'Cancel'
  );

  confirmApproveWithErrors = new ConfirmDialog(
    false,
    'Confirmation',
    'Claim(s) has validation Errors, are you sure you want to Approve?',
    'Approve',
    'Cancel'
  );
  // claim main dialogs
  confirmExportDialog = new ConfirmDialog(
    false,
    'Confirmation',
    'Export and complete Claim(s)',
    'Export',
    'Cancel'
  );

  confirmDeleteClaimDialog = new ConfirmDialog(
    false,
    'Confirmation',
    'Are you sure you want to delete Claim(s)?',
    'Delete',
    'Cancel'
  );

  confirmExportToExcelDialog = new ConfirmDialog(
    false,
    'Confirmation',
    'Export and Complete Claim(s)?',
    'Export',
    'Cancel'
  );

  subscriptions = new Subscription();
  private onGetClaimHeadersSubscription: Subscription | null;
  private refreshClaims$ = new Subject<void>(); // Subject to trigger claims grid refresh
  public mode: SelectableMode = 'multiple';
  public isReRunValidation = false;
  public reRunValidationLoaderMsg =
    'Please wait while validation is in progress...';
  headerTitle = 'Claims';
  breadcrumbs: any[] = [];
  public isSubjectLoading$: boolean = false;
  public isStopValidation$ = new BehaviorSubject<boolean>(false);
  public defaultPageSize: number;
  public dialogForTotalCount: boolean = false;
  public gatClaimsTabData: boolean = false;
  public childViewVoidedFlag: boolean = false;
  public isAllSelected: boolean = false;
  public isLoadingMoreData: boolean = false;
  public isVirtualScrollLoading: boolean = false;
  private scrollTimeout: any;
  private loadedRanges: Set<number> = new Set();
  private virtualScrollPageSize: number = 50; // Batch size for loading chunks in ALL mode
  private scrollPrefetchSub: Subscription | null = null;
  private prefetchThreshold = 0.8; // Trigger prefetch when 80% down the scroll
  private scrollDebounceMs = 150; // Throttle scroll handling for performance
  private lastTotalLoaded = 0;
  private lastTotalCount = 0;
  private isAllInitializing: boolean = false;

  // Sliding window state
  private windowStart: number = 0; // First record index in current window
  private windowEnd: number = 0; // Last record index in current window
  private maxWindowSize: number = 150; // Max DOM rows before cleanup (increased to prevent rapid cleanup cycles)
  private lastScrollDirection: 'down' | 'up' | null = null;
  private lastScrollTop: number = 0;
  private lastLoadedSkip: number = -1; // Guard against duplicate loads
  private isDropdownOpenDisabled: boolean = true;
  private isGriddropdownOpenDisabled: boolean = true;
  private hasMissingProvider: boolean = false;

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

  filterChangedTimeout: number;

  claimsCountForTabs: ClaimsCount = {
    pendingReviewTotalCount: 0,
    readyToBillTotalCount: 0,
    billingPendingTotalCount: 0,
    closedTotalCount: 0,
    rejectedTotalCount: 0,
    deniedTotalCount: 0,
    flaggedTotalCount: 0,
  };

  selectedTab: ClaimListingTab | null;
  selectedTabIndex = 0;
  showSelectorPopup = false;
  flipSelector = false;
  viewColumnsSettings: MemberViewSettings;
  showPrintPopup = false;
  flipPrintPopup = false;
  canEdit = false;
  canEditApprove = false;
  canApprove = false;
  canSubmit = false;
  viewVoidedClaims = false;
  isUserSelectAllChecked = false;
  IndexList: IndexResult[] = [];
  public mySelection: number[] = [];
  public claimsToPrint: ClaimToPrint[] = [];
  showAssignUserPopupId: number | null = null;
  showBulkAssignUserPopupId: number[] = [];
  selectedAssigneeId: number | 0 = 0;
  assignableUsers: AssigneeModel[] = [];
  hoveredAssigneeId: number | null = null;
  impersonationUserName: string | null = null;

  claimDashboardListSubject: ClaimDashboardListSubject;
  view: Observable<GridDataResult>;

  redraw = false;
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

  @ViewChild(GridComponent) encounterGrid: GridComponent;
  @ViewChild('selectorAnchorEl', { read: ElementRef })
  public selectorAnchor: ElementRef;
  @ViewChild('printAnchorEl', { read: ElementRef })
  public printAnchor: ElementRef;
  showFilter: boolean;

  rethinkUrl: string;
  expendedIndex: number = -1;
  expendedAnchor: HTMLAnchorElement;
  isDetailsExpanded: boolean = false;

  carcCodes: CarcCodes[] = [];
  allcarcCodes: CarcCodes[] = [];
  externalCodes: ExternalCodes[] = [];
  allExternalCodes: ExternalCodes[] = [];

  gridPageSizes: any;

  @ViewChild('testAccountDialog', { static: true })
  testAccountDialog!: TemplateRef<any>;
  showStatusDropdown = 0;
  hoveredItemId: number | null = null;
  currentTabIndex: number = 0;
  oldfilter: boolean = false;
  data: ClaimHeader[];
  filteredAssignableUsers: any[] = [];

  latestEncounterId: number[] = [];

  // Breadcrumb navigation handler
  breadcrumbClickHandler: any;

  constructor(
    private router: Router,
    private route: ActivatedRoute,
    private dialogService: DialogService,
    private accountService: AccountMemberService,
    private claimsService: ClaimService,
    private sidebarService: SidebarService,
    private notificationService: NotificationHandlerService,
    private claimNotesService: ClaimNotesService,
    private paginationService: PaginationService,
    public locale: Locale,
    private cdr: ChangeDetectorRef,
    private renderer: Renderer2,
    private writeOffService: WriteoffService,
    private destroy: TakeUntilDestroyService,
    private claimsManagementFilterService: ClaimsManagementFilterService,
    private notifyService: NotificationService,
    private ngZone: NgZone
  ) {
    this.rethinkUrl = environment.rethinkBHUrl;
    this.impersonationUserName =
      this.accountService.memberDetails.impersonationUserName;
    this.claimDashboardListSubject = new ClaimDashboardListSubject(
      this.claimsService
    );

    this.getGridPageSizes();
    // Default to 20 items per page unless user explicitly chooses otherwise
    this.defaultPageSize = 20;

    this.view = this.claimDashboardListSubject.pipe(
      map((data) => {
        let result = {
          data: data,
          total: this.claimDashboardListSubject.getCount(),
        };
        this.data = result.data;
        this.claimsCountForTabs =
          this.claimDashboardListSubject.getClaimsCountForTabs();
        return result;
      })
    );
    this.mySelection = [];
    this.subscriptions.add(
      this.accountService.accountMemberSettings.subscribe((x) => {
        if (x) {
          this.canEdit = this.accountService.checkPermissionLevel(
            AccountPermissions.BillingEdit
          );
          this.canEditApprove = this.accountService.checkPermissionLevel(
            AccountPermissions.BillingEditApprovedAppointments
          );
          this.canApprove = this.accountService.checkPermissionLevel(
            AccountPermissions.BillingApprove
          );
          this.canSubmit = this.accountService.checkPermissionLevel(
            AccountPermissions.BillingSubmitClaims
          );
        }
      })
    );

    this.selectedTab = ClaimListingTab.PendingReview;

    // Subscribe to refreshClaims$ to reload grid
    this.refreshClaims$.subscribe(() => {
      this.loadClaimHeaders(this.selectedTab);
      this.detailComponents.loadData();
    });
    this.claimsService.getOldFilter().subscribe((filter) => {
      if (filter !== null) {
        this.oldfilter = filter;
      }
    });

    // Subscribe to failed Claim Submission ID notifications
    this.subscriptions.add(this.notifyService.encounterId$.subscribe((id) => {
      if (!this.latestEncounterId.includes(id)) {
        this.latestEncounterId.push(id);
      }
    }));

    // Subscribe to view failed Claim Submission IDs notifications
    this.subscriptions.add(this.notifyService.viewFailedIds$.subscribe((ids) => {
      // switch to "Ready to Bill" tab (index 1)
      this.claimListingTabGroup.selectedIndex = 1;

      // Filter the claimDashboardListSubject to only include claims with IDs in the received list
      const currentList = this.claimDashboardListSubject.value;
      const updatedList = currentList.filter((x) => ids.includes(x.id));
      this.claimDashboardListSubject.next(updatedList);
    }));

    // Subscribe to failed Claim Submission ID notifications - Pending Review
    this.subscriptions.add(this.notifyService.encounterIdPendingReview$.subscribe(
      (id) => {
        if (!this.latestEncounterId.includes(id)) {
          this.latestEncounterId.push(id);
        }
      }
    ));

    //Pending Review Tab Index Subscription and Subscribe to failed Claim Submission ID notifications
    this.subscriptions.add(this.notifyService.failureClaimId$.subscribe((ids) => {
      // switch to "Pending Review" tab (index 0)
      this.claimListingTabGroup.selectedIndex = 0;

      // Filter the claimDashboardListSubject to only include claims with IDs in the received list
      const currentList = this.claimDashboardListSubject.value;
      const updatedList = currentList.filter((x) => ids.includes(x.id));
      this.claimDashboardListSubject.next(updatedList);
    }));

    // Subscribe to remove the successfully Claim Submission IDs from grid
    this.subscriptions.add(this.notifyService.successClaimId$.subscribe((id) => {
      // Filter the claimDashboardListSubject to only include claims with IDs in the received list
      const currentList = this.claimDashboardListSubject.value;
      currentList.any((x) => x.id === id);
      const updatedList = currentList.filter((x) => x.id !== id);
      this.claimDashboardListSubject.next(updatedList);

      // Debounce the refresh - only execute when no IDs arrive for 2 seconds
      if (this.scrollTimeout) {
        clearTimeout(this.scrollTimeout);
      }
      this.scrollTimeout = setTimeout(() => {
        this.refreshClaims$.next();
        this.detailComponents.loadData();
        this.scrollTimeout = null;
      }, 2000);
    }));
  }

  get selectedKeysArray(): number[] {
  return Array.from(this.mySelection);
}

  // Toggle popup on pill click
  togglePopup(rowIndex: number, i: number) {
    this.closeAllPopups();
    if (!this.popupOpen[rowIndex]) this.popupOpen[rowIndex] = [];
    this.popupOpen[rowIndex][i] = !this.popupOpen[rowIndex][i];
  }

  // Close popup manually
  closePopup(rowIndex: number, i: number) {
    if (!this.popupOpen[rowIndex]) return;
    this.popupOpen[rowIndex][i] = false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    this.closeAllPopups();
    if (this.isDropdownOpenDisabled) {
      this.closeBulkAssignPopup();
      this.isDropdownOpenDisabled = true;
    }
    if (this.isGriddropdownOpenDisabled) {
      this.closeAssignPopup();
      this.isGriddropdownOpenDisabled = true;
    }
  }

  dropdownopen(isOpen: boolean): void {
    if (isOpen) {
      this.isDropdownOpenDisabled = false;
    }
    if (!isOpen) {
      this.isDropdownOpenDisabled = true;
    }
  }

  griddropdownopen(isOpen: boolean): void {
    if (isOpen) {
      this.isGriddropdownOpenDisabled = false;
    }
    if (!isOpen) {
      this.isGriddropdownOpenDisabled = true;
    }
  }

  onAssigneeSelected(event: any) {
    this.isDropdownOpenDisabled = false;
    this.isGriddropdownOpenDisabled = false;
  }

  onGridAssigneeSelected(event: any) {
    this.isGriddropdownOpenDisabled = false;
    this.isDropdownOpenDisabled = false;
  }

  openEditModal(dataItem: any, rowIndex: number, i: number) {
    this.closePopup(rowIndex, i);

    // Open dialog in edit mode
    const dialogRef = this.dialogService.open({
      title: 'Edit Flagged Reason',
      content: FlagClaimDialogComponent,
      width: 540,
    });

    // After dialog is created, patch form values
    const dialogInstance = dialogRef.content
      .instance as FlagClaimDialogComponent;
    dialogInstance.isEditMode = true;
    dialogInstance.formGroup.patchValue({
      reasonId: dataItem.reasonId,
      note: dataItem.comment || '',
      assigneeId: dataItem.assigneeId || 0,
    });

    // Handle dialog result
    this.subscriptions.add(
      dialogRef.result.subscribe(
        (
          result:
            | DialogCloseResult
            | {
              submit: boolean;
              reasonId: number;
              assigneeId: number;
              notes?: string;
            }
        ) => {
          if (!(result instanceof DialogCloseResult) && result.submit) {
            // Prepare the update request

            const assigneeRequest: AssigneeRequestModel = {
              claimIds: [dataItem.id],
              assigneeId: result.assigneeId,
              memberId: this.accountService.memberDetails.memberId,
            };

            this.subscriptions.add(
              this.claimsService.assignUserToClaim(assigneeRequest).subscribe()
            );
            const request: FlagClaimsRequest = {
              claimIds: [dataItem.id], // single claim
              reasons: [{ reasonId: result.reasonId }],
              notes: result.notes,
              AccountInfoId: this.accountService.memberDetails.accountInfoId,
              MemberId: this.accountService.memberDetails.memberId,
              claimFlagTransactionId: dataItem.flagReasonTransactionId,
              impersonationUserName: this.accountService.memberDetails.impersonationUserName
            };

            // Send request to backend
            this.subscriptions.add(
              this.claimsService.flagClaimsWithReason(request).subscribe(() => {
                this.loadClaimHeaders(this.selectedTab);
                this.notificationService.showNotificationSuccess(
                  `Claim has been updated with the selected reason.`
                );
              })
            );
          }
        }
      )
    );
  }

  // Keep the same reference
  setRowClass = this._setRowClass.bind(this);

  private _setRowClass({ dataItem }: { dataItem: any }) {
    return {
      'highlight-row': this.latestEncounterId.includes(dataItem.id),
      'loading-row': dataItem.isLoadingRow === true,
    };
  }

  selectedTabChanged(id: number) {
    this.claimsService.setId(id);
    this.sidebarService.closeAll();

    if (
      this.onGetClaimHeadersSubscription &&
      !this.onGetClaimHeadersSubscription.closed
    ) {
      this.onGetClaimHeadersSubscription.unsubscribe();
    }
    this.gridState.sort = [{ dir: 'desc', field: 'dateOfServiceStart' }];

    // Always exit ALL mode and use default page size on tab change
    const wasAllSelected = this.isAllSelected;
    this.isAllSelected = false;
    this.teardownScrollPrefetch();
    this.loadedRanges.clear();
    this.isLoadingMoreData = false;
    this.lastTotalLoaded = 0;
    this.lastTotalCount = 0;
    this.lastLoadedSkip = -1;
    if (this.scrollTimeout) {
      clearTimeout(this.scrollTimeout);
      this.scrollTimeout = null;
    }
    this.gridState.skip = 0;
    this.gridState.take = this.defaultPageSize || 20;
    this.paginationService.setPageSizes(this.gridState.take);
    localStorage.setItem('lastPageSize', this.gridState.take.toString());
    // Clear current buffer to avoid stale totals/data crossing tabs
    this.claimDashboardListSubject.clearBuffer();
    // Clear any lingering status filters when switching tabs (prevents accidental statusIds like "6")
    if (this.claimFiltersComponent) {
      if (Array.isArray((this.claimFiltersComponent as any).selectedStatuses)) {
        (this.claimFiltersComponent as any).selectedStatuses = [];
      }
    }

    //Clearing grid filters on tab change if the “Apply Filter” action is not triggered
    if(!this.claimFiltersComponent.isFiltersApplied){
      this.claimFiltersComponent.clearFilters();
    }

    // Reset scroll position to top when changing tabs
    this.resetGridScrollTop();

    // Keep current data visible until new data arrives (no new loader)

    const isTabChanged = true;
    switch (id) {
      case 0:
        this.selectedTab = ClaimListingTab.PendingReview;
        this.breadcrumbs = [
          { label: 'Claims', url: '/billing/claims/list' },
          { label: 'Pending Review', url: '/billing/claims/list' },
        ];
        this.hideExpendedRow();
        this.loadClaimHeaders(this.selectedTab, isTabChanged);
        break;
      case 1:
        this.selectedTab = ClaimListingTab.ReadyToBill;
        this.breadcrumbs = [
          { label: 'Claims', url: '/billing/claims/list' },
          { label: 'Ready to Bill', url: '/billing/claims/list' },
        ];
        this.hideExpendedRow();
        this.loadClaimHeaders(this.selectedTab, isTabChanged);
        break;
      case 2:
        this.selectedTab = ClaimListingTab.BillingPending;
        this.breadcrumbs = [
          { label: 'Claims', url: '/billing/claims/list' },
          { label: 'Billed - Pending', url: '/billing/claims/list' },
        ];
        this.hideExpendedRow();
        this.loadClaimHeaders(this.selectedTab, isTabChanged);
        break;
      case 3:
        this.selectedTab = ClaimListingTab.Closed;
        this.breadcrumbs = [
          { label: 'Claims', url: '/billing/claims/list' },
          { label: 'Completed', url: '/billing/claims/list' },
        ];       
        this.loadClaimHeaders(this.selectedTab, isTabChanged);
        break;
      case 4:
        this.selectedTab = ClaimListingTab.Rejected;
        this.breadcrumbs = [
          { label: 'Claims', url: '/billing/claims/list' },
          { label: 'Rejected', url: '/billing/claims/list' },
        ];
        this.hideExpendedRow();
        this.getExternalCodes(isTabChanged);
        break;
      case 5:
        this.selectedTab = ClaimListingTab.Denied;
        this.breadcrumbs = [
          { label: 'Claims', url: '/billing/claims/list' },
          { label: 'Denied', url: '/billing/claims/list' },
        ];
        this.hideExpendedRow();
        this.getCarcCodes(isTabChanged);
        break;
      case 6:
        this.selectedTab = ClaimListingTab.Flagged;
        this.breadcrumbs = [
          { label: 'Claims', url: '/billing/claims/list' },
          { label: 'Flagged', url: '/billing/claims/list' },
        ];
        this.hideExpendedRow();
        this.loadClaimHeaders(this.selectedTab, isTabChanged);
        break;
    }
    this.claimFiltersComponent.tab = this.selectedTab;    
  }

  // Removed forced change detection to prevent UI freezes

  hideExpendedRow() {
    this.expendedAnchor != null
      ? this.expendedAnchor.classList.remove('k-minus')
      : null;
    this.expendedAnchor != null
      ? this.expendedAnchor.classList.add('k-plus')
      : null;
    this.claimsGrid.collapseRow(this.expendedIndex);
    this.expendedIndex = -1;
    this.expendedAnchor = null as any;
    this.IndexList = [];
    this.isDetailsExpanded = false;
    // Update breadcrumbs when hiding expanded row
    this.updateBreadcrumbsForExpansion(false);
  }

  //TODO: filter
  toggleFilter(event: boolean) {
    this.claimFiltersComponent.opened = event;
    this.showFilter = event;
    this.claimFiltersComponent.tab = this.selectedTab;

    // Filter panel opens/closes but API calls continue in background
    // Scroll and data loading remain active
  }

  onUpdateServicelines() {
    this.loadClaimHeaders(this.selectedTab);
  }

  private toCsv(list: any[] = [], key: string = 'id'): string | undefined {
    if (!list || list.length === 0) return undefined;
    return list.map((x) => x[key]).join(',');
  }

  private buildClaimHeaderSearch(
    tabIndex: ClaimListingTab | null,
    skip: number,
    take: number,
    isInitial: boolean,
    isTabChanged: boolean
  ): ClaimHeaderSearch {
    const search = new ClaimHeaderSearch();
    search.take = take;
    search.skip = skip;
    search.sortingModels = this.gridState.sort || [];
    const isFiltersApplied = this.claimFiltersComponent.isFiltersApplied;
    const filters: any = {
      tab: tabIndex || 1,
      ...(isFiltersApplied && {
        patientIds: this.toCsv(this.claimFiltersComponent.selectedPatients, 'id'),
        reasonCode: this.toCsv(this.claimFiltersComponent.selectedReasonCode, 'code'),
        funderIds: this.toCsv(this.claimFiltersComponent.selectedFunders, 'id'),
        assigneeIds: this.toCsv(this.claimFiltersComponent.selectedAssignees, 'id'),
        locationIds: this.toCsv(this.claimFiltersComponent.selectedLocations, 'id'),
        reasonIds: this.toCsv(this.claimFiltersComponent.selectedFlaggedReason, 'id'),
        renderingProviderIds: this.toCsv(this.claimFiltersComponent.selectedRenderingProviders, 'id'),
        balanceFrom: this.claimFiltersComponent.balanceFrom,
        balanceTo: this.claimFiltersComponent.balanceTo,
        patientResponsibilityFrom: this.claimFiltersComponent.patientResponsibilityFrom,
        patientResponsibilityTo: this.claimFiltersComponent.patientResponsibilityTo,
        dateOfServiceFrom: Helper.shiftDateToUTC(this.claimFiltersComponent.dateFrom),
        dateOfServiceTo: Helper.shiftDateToUTC(this.claimFiltersComponent.dateTo),
        statusIds: this.toCsv(this.claimFiltersComponent.selectedStatuses, 'id')
      }),
      showVoided:
        this.viewVoidedClaims ||
        (this.selectedTab === ClaimListingTab.Closed &&
          this.childViewVoidedFlag),
    };

    if (isFiltersApplied) {
      const validationIdsArr = this.claimFiltersComponent.selectedValidations
        .filter((x) => x.id != 4)
        .map((x) => x.id);

      if (validationIdsArr.length) filters.validationIds = validationIdsArr.join(',');

      const responseIdsArr = this.claimFiltersComponent.selectedValidations
        .filter((x) => x.id === 4)
        .map((x) => x.id);
      if (responseIdsArr.length) filters.responseIds = responseIdsArr.join(',');
    }
    search.filters = filters;
    return search;
  }

  loadClaimHeadersForVirtualScroll(
    tabIndex: ClaimListingTab | null,
    isInitial: boolean,
    isTabChanged = false,
    skipOverride?: number
  ) {
    if (!this.gridState.sort || this.gridState.sort.length === 0) {
      this.gridState.sort = [{ dir: 'desc', field: 'dateOfServiceStart' }];
    }
    const skipVal =
      typeof skipOverride === 'number'
        ? skipOverride
        : this.gridState.skip || 0;
    const filter = this.buildClaimHeaderSearch(
      tabIndex,
      skipVal,
      this.virtualScrollPageSize,
      isInitial,
      isTabChanged
    );

    this.claimsManagementFilterService.setFilter(
      this.claimFiltersComponent,
      filter.filters
    );

    if (isInitial) {
      // Show spinner on initial load (tab change, filter apply, filter clear)
      (filter as any).showSpinner = true;
      this.claimDashboardListSubject.getAll(filter, true);
      setTimeout(() => {
        this.isLoadingMoreData = false;
        if (this.isAllSelected) {
          this.setupScrollPrefetch();
        }
        // Mark ALL initialization as complete so appends can proceed
        this.isAllInitializing = false;
      }, 100);
    } else {
      // Don't show spinner when appending data during scroll
      (filter as any).showSpinner = false;
      this.claimDashboardListSubject.append(filter);
      setTimeout(() => {
        this.isLoadingMoreData = false;
      }, 100);
    }

    this.claimsToPrint = [];
  }

  loadClaimHeaders(tabIndex: ClaimListingTab | null, isTabChanged = false) {
    // If ALL mode is active and this is not a tab change,
    // route to virtual scroll path to keep behavior consistent
    if (this.isAllSelected && !isTabChanged) {
      this.gridState.skip = 0;
      this.isAllInitializing = true;
      this.isLoadingMoreData = true;
      this.loadedRanges.clear();
      this.claimDashboardListSubject.clearBuffer();
      this.teardownScrollPrefetch();
      this.loadClaimHeadersForVirtualScroll(this.selectedTab, true, false, 0);
      this.setupScrollPrefetch();
      this.mySelection = [];
      this.claimsToPrint = [];
      return;
    }

    if (!this.gridState.sort || this.gridState.sort.length === 0) {
      this.gridState.sort = [{ dir: 'desc', field: 'dateOfServiceStart' }];
    }

    if (!this.claimFiltersComponent.isFiltersApplied) {
      if (this.showFilter)
        window.setTimeout(
          (res) =>
            (<HTMLElement>(
              document.querySelector('.filter-btn .outlined-btn')
            ))!.click(),
          1000
        );
    }
    const skipVal = isTabChanged ? 0 : this.gridState.skip || 0;
    const takeVal = this.gridState.take || 20;
    const filter = this.buildClaimHeaderSearch(
      tabIndex,
      skipVal,
      takeVal,
      false,
      isTabChanged
    );

    this.claimsManagementFilterService.setFilter(
      this.claimFiltersComponent,
      filter.filters
    );

    // Clear virtual scroll mode for normal pagination
    this.isAllSelected = false;
    this.loadedRanges.clear();
    this.windowStart = 0;
    this.windowEnd = 0;
    this.lastScrollDirection = null;
    this.lastLoadedSkip = -1;
    this.claimDashboardListSubject.clearBuffer();

    if (this.oldfilter === true) {
      this.claimsService.getClaimHeaderFilter().pipe(take(1)).subscribe((storedFilter) => {
        if (storedFilter) {
          this.claimDashboardListSubject.getAll(storedFilter, false);
        }
      });
    } else {
      this.claimDashboardListSubject.getAll(filter, false);
      this.claimsService.setClaimHeaderFilter(filter);
    }
    this.mySelection = [];
    this.claimsToPrint = [];
  }

  onSortChange(sortParams: SortDescriptor[]): void {
    this.gridState.sort = sortParams;
    this.claimFiltersComponent.isFiltersApplied = true;
    if (this.isAllSelected) {
      // Reinitialize virtual scroll to apply new sorting without heavy full render
      this.gridState.skip = 0;
      this.isAllInitializing = true;
      this.isLoadingMoreData = true;
      this.loadedRanges.clear();
      this.claimDashboardListSubject.clearBuffer();
      this.teardownScrollPrefetch();
      this.loadClaimHeadersForVirtualScroll(this.selectedTab, true, false, 0);
      this.setupScrollPrefetch();
    } else {
      this.loadClaimHeaders(this.selectedTab);
    }
  }

 public onSelectChange(event: SelectionEvent): void {

  // If header checkbox triggered selection
  if (
    event.selectedRows.length > 1 &&
    !this.isUserSelectAllChecked
  ) {
    this.isUserSelectAllChecked = true;
  }

  // HANDLE SELECTED ROWS
  for (const row of event.selectedRows) {
    const id = row.dataItem.id;

    if (!this.mySelection.includes(id)) {
      this.mySelection.push(id);
    }

    if (!this.claimsToPrint.some(c => c.claimId === id)) {
      this.claimsToPrint.push({
        claimId: id,
        cmsPages: row.dataItem.cmsPagesCount
      });
    }
  }

  // HANDLE DESELECTED ROWS
  for (const row of event.deselectedRows) {
    const id = row.dataItem.id;

    this.mySelection = this.mySelection.filter(x => x !== id);
    this.claimsToPrint.removeWhere(c => c.claimId === id);
    this.isUserSelectAllChecked = false;
   }
}

  // Removed onScrollBottom - using setupScrollPrefetch instead for better performance

  private setupScrollPrefetch(): void {
    // Clean any previous listener
    if (this.scrollPrefetchSub) {
      this.scrollPrefetchSub.unsubscribe();
      this.scrollPrefetchSub = null;
    }

    const gridEl =
      this.claimsGrid && this.claimsGrid.wrapper
        ? this.claimsGrid.wrapper.nativeElement.querySelector('.k-grid-content')
        : null;

    if (!gridEl) return;

    // Handle scrolls outside Angular to avoid change detection on every pixel
    this.ngZone.runOutsideAngular(() => {
      this.scrollPrefetchSub = fromEvent(gridEl, 'scroll', { passive: true })
        .pipe(
          throttleTime(this.scrollDebounceMs),
          filter(() => this.isAllSelected && !this.isLoadingMoreData)
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

          // Avoid triggering at the very edges to prevent double-loads
          if (scrollTop <= 1 && direction === 'up') {
            return;
          }

          // Downward scroll: 80% threshold
          if (
            scrollPercentage >= this.prefetchThreshold &&
            direction === 'down'
          ) {
            this.lastScrollDirection = 'down';
            this.ngZone.run(() => this.handleVirtualScrollLoad());
          }

          // Upward scroll: 20% threshold
          else if (
            scrollPercentage <= 0.2 &&
            direction === 'up' &&
            this.windowStart > 0
          ) {
            this.lastScrollDirection = 'up';
            this.ngZone.run(() => this.handleVirtualScrollLoadUpward());
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

  private handleVirtualScrollLoad(): void {
    if (!this.isAllSelected || this.isLoadingMoreData || this.isAllInitializing)
      return;

    const currentLength = this.claimDashboardListSubject.getDataLength();
    const totalCount = this.claimDashboardListSubject.getCount();

    // Calculate global skip position (not DOM length)
    const skipVal = this.windowStart + currentLength;

    this.lastTotalLoaded = skipVal;
    this.lastTotalCount = totalCount;

    // Check if we've reached the end using global position
    if (skipVal >= totalCount) {
      return;
    }

    // Guard: Prevent duplicate load of same batch during DOM mutation
    if (skipVal === this.lastLoadedSkip) {
      return;
    }

    // Calculate page index based on global position
    const pageIndex = Math.floor(skipVal / this.virtualScrollPageSize);

    if (!this.loadedRanges.has(pageIndex)) {
      this.loadedRanges.add(pageIndex);
      this.isLoadingMoreData = true;
      this.lastLoadedSkip = skipVal; // Mark this batch as loading

      this.loadClaimHeadersForVirtualScroll(
        this.selectedTab,
        false,
        false,
        skipVal
      );

      // Update window end to global position
      this.windowEnd = skipVal + this.virtualScrollPageSize;

      // Apply sliding window cleanup: remove from TOP when exceeding max window
      if (currentLength >= this.maxWindowSize) {
        this.applySlidingWindowCleanup('down');
      }
    }
  }

  private handleVirtualScrollLoadUpward(): void {
    if (!this.isAllSelected || this.isLoadingMoreData || this.windowStart <= 0)
      return;

    // Calculate previous batch skip value
    const previousSkip = Math.max(
      0,
      this.windowStart - this.virtualScrollPageSize
    );
    const pageIndex = Math.floor(previousSkip / this.virtualScrollPageSize);

    // Only load if we haven't loaded this batch yet
    if (!this.loadedRanges.has(pageIndex)) {
      this.loadedRanges.add(pageIndex);
      this.isLoadingMoreData = true;

      // Store current scroll position to restore after prepend
      const gridEl =
        this.claimsGrid?.wrapper?.nativeElement?.querySelector(
          '.k-grid-content'
        );
      const scrollTopBefore = gridEl ? gridEl.scrollTop : 0;
      const scrollHeightBefore = gridEl ? gridEl.scrollHeight : 0;

      this.loadClaimHeadersBatchUpward(this.selectedTab, previousSkip, () => {
        // Update window start
        this.windowStart = previousSkip;

        // Preserve scroll position after prepending data
        if (gridEl) {
          const scrollHeightAfter = gridEl.scrollHeight;
          const heightDifference = scrollHeightAfter - scrollHeightBefore;
          gridEl.scrollTop = scrollTopBefore + heightDifference;
        }

        // Apply sliding window cleanup: remove from BOTTOM when exceeding max window
        const totalInWindow = this.claimDashboardListSubject.getDataLength();
        if (totalInWindow >= this.maxWindowSize) {
          this.applySlidingWindowCleanup('up');
        }
      });
    }
  }

  private applySlidingWindowCleanup(direction: 'down' | 'up'): void {
    const currentData = this.claimDashboardListSubject.getDataLength();

    if (currentData <= this.maxWindowSize) return;

    const recordsToRemove = currentData - this.maxWindowSize;

    // Get scroll container for position preservation
    const gridEl =
      this.claimsGrid?.wrapper?.nativeElement?.querySelector('.k-grid-content');

    if (direction === 'down') {
      // Scrolling down: remove from TOP
      // CRITICAL: Preserve scroll position to prevent jump
      const scrollTopBefore = gridEl ? gridEl.scrollTop : 0;
      const scrollHeightBefore = gridEl ? gridEl.scrollHeight : 0;

      const oldWindowStart = this.windowStart;
      this.windowStart += recordsToRemove;
      this.claimDashboardListSubject.removeFromTop(recordsToRemove);

      // Remove old page indexes from cache
      for (let i = 0; i < recordsToRemove; i += this.virtualScrollPageSize) {
        const pageToRemove = Math.floor(
          (oldWindowStart + i) / this.virtualScrollPageSize
        );
        this.loadedRanges.delete(pageToRemove);
      }

      // Restore scroll position after DOM update to prevent auto-scroll
      if (gridEl) {
        requestAnimationFrame(() => {
          const scrollHeightAfter = gridEl.scrollHeight;
          const heightDiff = scrollHeightBefore - scrollHeightAfter;
          gridEl.scrollTop = scrollTopBefore - heightDiff;
        });
      }
    } else {
      // Scrolling up: remove from BOTTOM
      const oldWindowEnd = this.windowEnd;
      this.windowEnd -= recordsToRemove;
      this.claimDashboardListSubject.removeFromBottom(recordsToRemove);

      // Remove old page indexes from cache
      for (let i = 0; i < recordsToRemove; i += this.virtualScrollPageSize) {
        const pageToRemove = Math.floor(
          (oldWindowEnd - recordsToRemove + i) / this.virtualScrollPageSize
        );
        this.loadedRanges.delete(pageToRemove);
      }
    }
  }

  private loadClaimHeadersBatchUpward(
    tabIndex: ClaimListingTab | null,
    skip: number,
    onComplete?: () => void
  ): void {
    if (!this.gridState.sort || this.gridState.sort.length === 0) {
      this.gridState.sort = [{ dir: 'desc', field: 'dateOfServiceStart' }];
    }

    const filter = this.buildClaimHeaderSearch(
      tabIndex,
      skip,
      this.virtualScrollPageSize,
      false,
      false
    );
    this.claimsManagementFilterService.setFilter(
      this.claimFiltersComponent,
      filter.filters
    );

    this.onGetClaimHeadersSubscription = this.claimDashboardListSubject
      .prependBatch(filter, false)
      .subscribe(
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

  private resetGridScrollTop(): void {
    if (this.claimsGrid && this.claimsGrid.wrapper) {
      const scrollContainer =
        this.claimsGrid.wrapper.nativeElement.querySelector('.k-grid-content');
      if (scrollContainer) {
        scrollContainer.scrollTop = 0;
      }
    }
  }

  trackByClaimId(index: number, item: any) {
    // Kendo Grid passes GridItem; prefer item.data.id, fallback to item.id
    return item && item.data && item.data.id
      ? item.data.id
      : item
        ? item.id
        : index;
  }

  getFooterRange(): string {
    // Only calculate window range when in ALL mode
    if (!this.isAllSelected) {
      return '0';
    }

    const currentLength = this.claimDashboardListSubject.getDataLength();
    if (currentLength === 0) return '0';

    const from = this.windowStart + 1;
    const to = this.windowStart + currentLength;

    // If showing only one item, return single number format
    if (from === to) {
      return `${from}`;
    }

    return `${from}–${to}`;
  }

  /**
   * Return the 1-based start index for the current page (used in pager)
   */
  public getPageStart(total: number): number {
    if (!total || total === 0) return 0;
    if (this.isAllSelected) return this.windowStart + 1;
    const skip = this.gridState.skip || 0;
    return skip + 1;
  }

  /**
   * Return the end index for the current page (used in pager)
   */
  public getPageEnd(total: number): number {
    if (!total || total === 0) return 0;
    if (this.isAllSelected)
      return this.windowStart + this.claimDashboardListSubject.getDataLength();
    const skip = this.gridState.skip || 0;
    const take = this.gridState.take || this.defaultPageSize || 20;
    return Math.min(skip + take, total);
  }

  onPageChange(event: PageChangeEvent): void {
    // Handle 'All' selection - user clicked ALL button
    if (event.take === 0 && !this.isAllSelected) {
      this.isAllSelected = true;
      this.mySelection = [];
      this.claimsToPrint = [];
      this.loadedRanges.clear();
      this.lastTotalLoaded = 0;
      this.lastTotalCount = 0;
      this.isUserSelectAllChecked = false;
      this.windowStart = 0;
      this.windowEnd = 0;
      this.lastScrollDirection = null;
      this.lastScrollTop = 0;
      this.lastLoadedSkip = -1;
      this.loadedRanges.add(0);
      this.gridState.take = this.ALL_RECORDS_TAKE;
      this.gridState.skip = 0;
      this.paginationService.setPageSizes(0);
      this.isAllInitializing = true;
      this.claimDashboardListSubject.clearBuffer();
      this.loadClaimHeadersForVirtualScroll(this.selectedTab, true, false);
      // Clear stale status filters that might cause zero results
      if (
        this.claimFiltersComponent &&
        Array.isArray((this.claimFiltersComponent as any).selectedStatuses)
      ) {
        (this.claimFiltersComponent as any).selectedStatuses = [];
      }
      // Clear previous tab/page buffer to avoid stale totals/data affecting ALL
      this.claimDashboardListSubject.clearBuffer();

      this.cdr.detectChanges();
      // Ensure scroll is at top when switching to ALL
      this.resetGridScrollTop();
      // Prefetch listeners are set up after initial load in loadClaimHeadersForVirtualScroll

      localStorage.setItem('lastPageSize', '0');
      this.gatClaimsTabData = true;
    }
    // User clicked a specific page size while in ALL mode
    else if (this.isAllSelected && event.take !== 0) {
      this.teardownScrollPrefetch();
      this.isAllSelected = false;
      this.loadedRanges.clear();
      this.windowStart = 0;
      this.windowEnd = 0;
      this.lastScrollDirection = null;
      this.gridState.take = event.take;
      this.gridState.skip = 0;
      this.dialogForTotalCount = false;
      this.gatClaimsTabData = true;
      this.paginationService.setPageSizes(this.gridState.take);
      this.loadClaimHeaders(this.selectedTab);
      localStorage.setItem('lastPageSize', this.gridState.take.toString());
    }
    // Normal pagination
    else if (!this.isAllSelected) {
      this.teardownScrollPrefetch();
      this.loadedRanges.clear();
      this.windowStart = 0;
      this.windowEnd = 0;
      this.lastLoadedSkip = -1;
      this.gridState.skip = event.skip;
      this.gridState.take = event.take;
      this.dialogForTotalCount = false;
      this.gatClaimsTabData = true;
      this.paginationService.setPageSizes(this.gridState.take);
      this.loadClaimHeaders(this.selectedTab);
      localStorage.setItem('lastPageSize', this.gridState.take.toString());
    }
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

  getDateOfServiceFieldValue(item: ClaimHeader): string {
    if (item.dateOfServiceStart === item.dateOfServiceEnd)
      return item.dateOfServiceStart.toLocaleDateString('en-US');
    else
      return `${item.dateOfServiceStart.toLocaleDateString(
        'en-US'
      )} - ${item.dateOfServiceEnd.toLocaleDateString('en-US')}`;
  }

  onFilterChanged() {
    // Debounce to avoid UI lag when many rows are rendered
    if (this.filterChangedTimeout) {
      clearTimeout(this.filterChangedTimeout);
    }
    this.filterChangedTimeout = window.setTimeout(() => {
      if (this.isAllSelected) {
        // Reinitialize virtual scroll under current filters (including clear)
        this.reinitializeAllModeWithFilters();
        this.gatClaimsTabData = true;
      } else {
        // Normal pagination path - reset window state
        this.windowStart = 0;
        this.windowEnd = 0;
        this.lastLoadedSkip = -1;
        this.gridState.skip = 0;
        this.loadClaimHeaders(this.selectedTab);
      }
    }, 150);
  }

  private reinitializeAllModeWithFilters() {
    this.isAllInitializing = true;
    this.isLoadingMoreData = true;
    this.loadedRanges.clear();
    this.windowStart = 0;
    this.windowEnd = 0;
    this.lastScrollDirection = null;
    this.lastScrollTop = 0;
    this.lastLoadedSkip = -1;
    this.claimDashboardListSubject.clearBuffer();
    this.resetGridScrollTop();
    this.teardownScrollPrefetch();
    this.loadClaimHeadersForVirtualScroll(this.selectedTab, true, false, 0);
    this.setupScrollPrefetch();
    this.mySelection = [];
    this.claimsToPrint = [];
  }

  columnsSelectorToggle(): void {
    this.showSelectorPopup = !this.showSelectorPopup;
    this.flipSelector = !this.flipSelector;
  }

  columnsPrintToggle(): void {
    if (!this.mySelection.length) return;
    this.showPrintPopup = !this.showPrintPopup;
    this.flipPrintPopup = !this.flipPrintPopup;
  }

  onSelectorLeave(state: boolean) {
    this.showSelectorPopup = state;
    this.flipSelector = state;
  }

  onPrintPopupLeave(state: boolean) {
    this.showPrintPopup = state;
    this.flipPrintPopup = state;
  }

  setColumns(selectedColumns: string[]) {
    this.subscriptions.add(
      this.claimsService.saveSelectedColumns(selectedColumns).subscribe((x) => {
        this.viewColumnsSettings = x;
      })
    );
  }

  onColumnSelect(selectedColumns: string[]) {
    Object.keys(this.viewColumnsSettings).forEach((vcsKey) => {
      if (selectedColumns.includes(vcsKey)) {
        this.viewColumnsSettings[vcsKey] = true;
      } else {
        this.viewColumnsSettings[vcsKey] = false;
      }
    });
    localStorage.setItem(
      'viewColumnsSettings',
      JSON.stringify(this.viewColumnsSettings)
    );
  }

  showErrorsAlerts(
    claim: ClaimHeader,
    tabType: 'Error' | 'Alert' | 'Response'
  ) {
    this.subscriptions.add(
      this.sidebarService
        .openRight(EncounterErrorsAlertsComponent, true, 'md')
        .subscribe((rsidebarRef) => {
          rsidebarRef.instance.setData(claim, tabType, this.selectedTab);
        })
    );
  }

  approveSelectedClaims() {
    if (!this.mySelection.length) return;
    const claimsToSubmit: number[] = [];
    const claimsToApproveIds: number[] = [];
    var dataToSubmit: any;
    this.view.pipe(take(1)).subscribe((result) => {
      for (const dataItem of result.data) {
        if (this.mySelection.indexOf(dataItem.id) !== -1) {
          if (dataItem.errorsCount !== 0) {
            const selectionItem = dataItem.id;

            const confirmDialog = this.dialogService.open({
              title: 'Please confirm',
              width: 500,
              content: `Claim has validaiton errors, do you still want to Approve?`,
              actions: [{ text: 'Cancel' }, { text: 'Yes', primary: true }],
            });
            this.subscriptions.add(
              confirmDialog.result.subscribe((result) => {
                if ((result as DialogAction).text === 'Yes') {
                  const claimWithErrorsId: number[] = [];
                  claimsToSubmit.addRange(claimWithErrorsId);
                  if (claimsToSubmit.length > 0) {
                    const submitModel: ClaimsSubmitModel = {
                      Ids: claimsToSubmit,
                      isSecondary: false,
                      adjustmentLevel: 1,
                      secondaryFunderDetails: [],
                      impersonationUserName: this.impersonationUserName,
                      AccountInfoId:
                        this.accountService.memberDetails.accountInfoId,
                      MemberId: this.accountService.memberDetails.memberId,
                    };

                    if (!dataToSubmit.useNewClaimProcessing) {
                      claimWithErrorsId.push(selectionItem);
                      this.subscriptions.add(
                        this.claimsService
                          .approveClaims(claimWithErrorsId)
                          .subscribe((submittedClaimsIds) => {
                            this.showNotifications(
                              submittedClaimsIds.length,
                              claimsToSubmit.length - submittedClaimsIds.length,
                              'approved'
                            );
                            this.loadClaimHeaders(this.selectedTab, false);
                          })
                      );
                    } else {
                      this.subscriptions.add(
                        this.claimsService
                          .SubmitClaimToServiceBus(submitModel)
                          .subscribe((submittedClaimsIds) => {
                            this.notificationService.showNotificationSuccess(
                              `Claim(s) have been approved successfully and are being processed in the background. You will be notified once processing begins.`
                            );
                            this.loadClaimHeaders(this.selectedTab, false);
                          })
                      );
                    }
                  }
                }
              })
            );
          } else {
            claimsToApproveIds.push(dataItem.id);
          }
        }
      }
    });

    if (claimsToApproveIds.length > 0) {
      this.subscriptions.add(
        this.claimsService.approveClaims(claimsToApproveIds).subscribe(() => {
          this.loadClaimHeaders(this.selectedTab);
        })
      );
    }
  }

  approveSelectedClaimsWithErrors(result: boolean) {
    if (result) {
      this.subscriptions.add(
        this.claimsService
          .approveClaims(this.confirmApproveWithErrorsDialog.innerModel)
          .subscribe((approvedClaims) => {
            this.notificationService.showNotificationSuccess(
              `Claim(s) have been approved successfully and are being processed in the background. You will be notified once processing begins.`
            );
            this.loadClaimHeaders(this.selectedTab, false);
          })
      );
    }
  }

  approveSelectedClaimsNoErrors(result: boolean) {
    if (result) {
      this.subscriptions.add(
        this.claimsService
          .approveClaims(this.confirmApproveNoErrorsDialog.innerModel)
          .subscribe((approvedClaims) => {
            this.notificationService.showNotificationSuccess(
              `Claim(s) have been approved successfully and are being processed in the background. You will be notified once processing begins.`
            );
            this.loadClaimHeaders(this.selectedTab, false);
          })
      );
    }
  }

  unapproveSelectedClaims() {
    if (!this.mySelection.length) return;

    const claimsToUnapproveIds: number[] = [];

    this.view.pipe(take(1)).subscribe((result) => {
      for (const dataItem of result.data) {
        if (this.mySelection.indexOf(dataItem.id) !== -1) {
          claimsToUnapproveIds.push(dataItem.id);
        }
      }
    });

    if (claimsToUnapproveIds.length > 0) {
      this.subscriptions.add(
        this.claimsService
          .unapproveClaims(claimsToUnapproveIds)
          .subscribe(() => {
            this.loadClaimHeaders(this.selectedTab);
            this.notificationService.showNotificationSuccess(
              claimsToUnapproveIds.length + ' claim(s) unapproved successfully.'
            );
          })
      );
    }
  }

  flagSelectedClaims() {
    if (!this.mySelection.length) return;

    // Open the flag claim dialog
    const dialogRef = this.dialogService.open({
      content: FlagClaimDialogComponent, // Make sure to import this component
      title: 'Flagged Reason',
      width: 540,
    });
    // // Handle dialog result
    this.subscriptions.add(
      dialogRef.result.subscribe(
        (
          result:
            | DialogCloseResult
            | {
              submit: boolean;
              reasonId: number;
              assigneeId: number;
              notes?: string;
            }
        ) => {
          if (!(result instanceof DialogCloseResult) && result.submit) {
            const request: FlagClaimsRequest = {
              claimIds: [...this.mySelection],
              reasons: [{ reasonId: result.reasonId }],
              notes: result.notes,
              AccountInfoId: this.accountService.memberDetails.accountInfoId,
              MemberId: this.accountService.memberDetails.memberId,
              impersonationUserName: this.accountService.memberDetails.impersonationUserName
            };
            const assigneeRequest: AssigneeRequestModel = {
              claimIds: [...this.mySelection],
              assigneeId: result.assigneeId,
              memberId: this.accountService.memberDetails.memberId,
            };

            this.subscriptions.add(
              this.claimsService.assignUserToClaim(assigneeRequest).subscribe()
            );

            this.subscriptions.add(
              this.claimsService.flagClaimsWithReason(request).subscribe(() => {
                this.loadClaimHeaders(this.selectedTab);
                this.notificationService.showNotificationSuccess(
                  `${request.claimIds.length} Claim(s) have been flagged.`
                );
              })
            );
          }
        }
      )
    );
  }

  unflagSelectedClaims() {
    if (!this.mySelection.length) return;

    const claimsToUnflagIds: number[] = [];

    this.view.pipe(take(1)).subscribe((result) => {
      for (const dataItem of result.data) {
        if (this.mySelection.indexOf(dataItem.id) !== -1) {
          claimsToUnflagIds.push(dataItem.id);
        }
      }
    });

    if (claimsToUnflagIds.length > 0) {
      const request = {
        Ids: claimsToUnflagIds,
        AccountInfoId: this.accountService.memberDetails.accountInfoId,
        MemberId: this.accountService.memberDetails.memberId,
        rethinkuser: this.accountService.memberDetails.impersonationUserName
      };
      this.subscriptions.add(
        this.claimsService.unflagClaims(request).subscribe(() => {
          this.loadClaimHeaders(this.selectedTab);
          this.notificationService.showNotificationSuccess(
            claimsToUnflagIds.length + ' Claim(s) has been unflaged.'
          );
        })
      );
    }
  }

  assignUserSelectedClaims() {
    if (!this.mySelection.length) return;

    const claimsToAssignId: number[] = [];

    this.view.pipe(take(1)).subscribe((result) => {
      for (const dataItem of result.data) {
        if (this.mySelection.indexOf(dataItem.id) !== -1) {
          claimsToAssignId.push(dataItem.id);
        }
      }
    });

    if (claimsToAssignId.length > 0) {
      const assigneeRequest: AssigneeRequestModel = {
        claimIds: claimsToAssignId,
        assigneeId: this.selectedAssigneeId,
        memberId: this.accountService.memberDetails.memberId,
      };

      this.subscriptions.add(
        this.claimsService.assignUserToClaim(assigneeRequest).subscribe(() => {
          this.loadClaimHeaders(this.selectedTab);
          this.notificationService.showNotificationSuccess(
            claimsToAssignId.length + ' Claim(s) has been assigned.'
          );
          this.closeBulkAssignPopup();
        })
      );
    }
  }

  public deleteSelectedClaims(result: boolean): void {
    if (result) {
      const claimsToDelete: ClaimHeader[] = [];

      this.view.pipe(take(1)).subscribe((result) => {
        for (const dataItem of result.data) {
          if (this.mySelection.indexOf(dataItem.id) !== -1) {
            claimsToDelete.push(dataItem);
          }
        }
      });

      if (claimsToDelete.length > 0) {
        // if (this.selectedTab === ClaimListingTab.ReadyToBill ||
        if (this.selectedTab === ClaimListingTab.Flagged) {
          const allowedToDeleteClaimsIds = claimsToDelete
            .filter(
              (claim) =>
                !this.canPerformActionOnClaim(claim.submissionStatusId) ||
                claim.submissionStatusId === ClaimSubmissionStatus.Unknown
            )
            .map((claim) => claim.id);

          const rejectedToDeleteClaimsIds = claimsToDelete.filter(
            (claim) => !allowedToDeleteClaimsIds.includes(claim.id)
          );
          rejectedToDeleteClaimsIds.forEach((claim) =>
            this.showDeleteNotificationError(claim.claimNumber)
          );

          if (allowedToDeleteClaimsIds.length > 0) {
            this.deleteClaims(allowedToDeleteClaimsIds);
          }
        } else {
          this.deleteClaims(claimsToDelete.map((claim) => claim.id));
        }
      }
    }
  }

  public onDeleteSelectedClaims(): void {
    if (!this.mySelection.length) return;
    this.confirmDeleteClaimDialog.opened = true;
  }

  onApproveSelectedClaims(): void {
    if (!this.mySelection.length) return;

    const claimsToApproveIds: number[] = [];
    const requests = [];
    this.view.pipe(take(1)).subscribe((result) => {
      for (const dataItem of result.data) {
        if (this.mySelection.indexOf(dataItem.id) !== -1) {
          claimsToApproveIds.push(dataItem.id);

          requests.push(this.claimsService.getClaimErrorsAndAlerts(dataItem.id));
        }
      }

      forkJoin(requests).subscribe(allErrors => {
        let hasErrors = false;

        allErrors.forEach(errors => {
          if (errors?.length) {
            hasErrors = true;
          }

          if (errors?.some(e => e.errorCode === "Billing Provider - Enrollment Missing")) {
            this.hasMissingProvider = true;
          }
        });

        if (hasErrors) {
          const msg = this.hasMissingProvider 
              ? `${claimsToApproveIds.length} Claims have validation errors including missing Provider Enrollment, do you still want to approve?` 
              : `${claimsToApproveIds.length} Claims have validation errors, do you still want to approve?`;

          this.confirmApproveWithErrors.message = msg;
          this.confirmApproveWithErrorsDialog.innerModel = claimsToApproveIds;
          this.hasMissingProvider = false;
          this.confirmApproveWithErrorsDialog.opened = true;
        } else {
          this.confirmApproveNoErrorsDialog.innerModel = claimsToApproveIds;
          this.confirmApproveNoErrorsDialog.opened = true;
        }
      });
    });
  }

  submitSelectedClaims() {
    if (!this.mySelection.length) return;

    var dataToSubmit: any;

    const claimsToSubmit: number[] = [];
    const claimWithErrorsId: number[] = [];
    const requests = [];

    this.view.pipe(take(1)).subscribe((result) => {
      dataToSubmit = result.data.find((e) => e.id === this.mySelection[0]);
      for (const dataItem of result.data) {
        if (this.mySelection.includes(dataItem.id)) {
          requests.push(
            this.claimsService.getClaimErrorsAndAlerts(dataItem.id).pipe(
              map(errors => ({ id: dataItem.id, errors, dataItem }))
            )
          );
        }
      }

      forkJoin(requests).subscribe(results => {

        results.forEach(({ id, errors, dataItem }) => {

          if (errors?.length) {
            claimWithErrorsId.push(id);

            // Check missing provider from API
            if (errors.some(e => e.errorCode === "Billing Provider - Enrollment Missing")) {
              this.hasMissingProvider = true;
            }

          } else {
            claimsToSubmit.push(id);
          }
        });

        // Check if the account is a test account
        if (dataToSubmit.isTestAccount) {
          const testAccountDialog = this.testAccountClaimSubmittionDialog();
          this.subscriptions.add(
            testAccountDialog.result.subscribe((result) => {
              if ((result as DialogAction).text === 'Yes') {
                //this.printMarkAsBilledBulk();
                this.markAsBilledBulk();
              }
            })
          );
        } else {
          const confirmDialog = this.submitDialog(claimWithErrorsId);

          this.subscriptions.add(
            confirmDialog.result.subscribe((result) => {
              if ((result as DialogAction).text === 'Yes') {
                claimsToSubmit.addRange(claimWithErrorsId);
                if (claimsToSubmit.length > 0) {
                  const submitModel: ClaimsSubmitModel = {
                    Ids: claimsToSubmit,
                    isSecondary: false,
                    adjustmentLevel: 1,
                    secondaryFunderDetails: [],
                    AccountInfoId: this.accountService.memberDetails.accountInfoId,
                    MemberId: this.accountService.memberDetails.memberId,
                    impersonationUserName: this.impersonationUserName,
                  };
                  if (!dataToSubmit.useNewClaimProcessing) {
                    this.subscriptions.add(
                      this.claimsService
                        .submitClaims(submitModel)
                        .subscribe((submittedClaimsIds) => {
                          this.showNotifications(
                            submittedClaimsIds.length,
                            claimsToSubmit.length - submittedClaimsIds.length,
                            'submitted'
                          );
                          this.loadClaimHeaders(this.selectedTab, false);
                        })
                    );
                  } else {
                    this.subscriptions.add(
                      this.claimsService
                        .SubmitClaimToServiceBus(submitModel)
                        .subscribe((submittedClaimsIds) => {
                          this.notificationService.showNotificationSuccess(
                            `Claim(s) have been submitted successfully and are being processed in the background. You will be notified once processing begins.`
                          );
                          this.loadClaimHeaders(this.selectedTab, false);
                        })
                    );
                  }
                }
              }
            })
          );
        }
      });
    });
  }

  testAccountClaimSubmittionDialog() {
    const confirmDialog = this.dialogService.open({
      title: '⚠️ Prevent Claim Submission for Test Account',
      width: 500,
      content: this.testAccountDialog,
      actions: [{ text: 'Cancel' }, { text: 'Yes', primary: true }],
    });
    return confirmDialog;
  }

  submitDialog(claimWithErrorsId: any) {
    if (claimWithErrorsId.length != 0) {

      let message = ''; 
      if (this.hasMissingProvider) { 
        message = claimWithErrorsId.length +` Claims have validation errors including missing Provider Enrollment, do you still want to Submit?`; 
      } else { 
        message = claimWithErrorsId.length +` Claim has validation errors, do you still want to Submit?`; 
      }

      this.hasMissingProvider = false;
      const confirmDialog = this.dialogService.open({
        title: 'Please confirm',
        width: 500,
        content: message,
        actions: [{ text: 'Cancel' }, { text: 'Yes', primary: true }],
      });
      return confirmDialog;
    } else {
      const confirmDialog = this.dialogService.open({
        title: 'Please confirm',
        width: 500,
        content: `Do you want to Submit?`,
        actions: [{ text: 'Cancel' }, { text: 'Yes', primary: true }],
      });
      return confirmDialog;
    }
  }

  voidSelectedClaims() {
    if (!this.mySelection.length) return;

    const claimsToVoid: ClaimHeader[] = [];

    this.view.pipe(take(1)).subscribe((result) => {
      for (const dataItem of result.data) {
        if (this.mySelection.indexOf(dataItem.id) !== -1) {
          claimsToVoid.push(dataItem);
        }
      }
    });

    if (claimsToVoid.length > 0) {
      if (
        this.selectedTab === ClaimListingTab.Closed ||
        this.selectedTab === ClaimListingTab.ReadyToBill ||
        this.selectedTab === ClaimListingTab.Flagged
      ) {
        const allowedToVoidClaimsIds = claimsToVoid
          .filter((claim) =>
            this.canPerformActionOnClaim(claim.submissionStatusId)
          )
          .map((claim) => claim.id);
        const rejectedClaimsToVoidIds = claimsToVoid.filter(
          (claim) => !allowedToVoidClaimsIds.includes(claim.id)
        );

        rejectedClaimsToVoidIds.forEach((claim) =>
          this.showVoidNotificationError(claim.claimNumber)
        );

        if (allowedToVoidClaimsIds.length > 0) {
          this.openVoidClaimDialog(allowedToVoidClaimsIds);
        }
      } else {
        this.openVoidClaimDialog(claimsToVoid.map((claim) => claim.id));
      }
    }
  }

  openVoidClaimDialog(claimIds: number[]) {
    const dialogRef = this.dialogService.open({
      content: VoidClaimDialogComponent,
      title: 'Void Claim',
    });
    this.subscriptions.add(
      dialogRef.result.subscribe(
        (result: DialogCloseResult | VoidClaimDialogResult) => {
          if (
            !(result instanceof DialogCloseResult) &&
            (result as VoidClaimDialogResult).submit
          ) {
            const dialogResult = result as VoidClaimDialogResult;

            this.subscriptions.add(
              this.claimsService
                .voidClaims({
                  claimIds,
                  submitToClearinghouse: dialogResult.option,
                  note: dialogResult.note,
                  claimNote: dialogResult.claimnote,
                })
                .subscribe((voidedClaimsIds) => {
                  // voidedClaimsIds.forEach(claimIdentifier => {
                  //     this.notificationService.showNotificationSuccess(`Claim <${claimIdentifier}/> has been voided`);
                  // });
                  this.showNotifications(
                    voidedClaimsIds.length,
                    claimIds.length - voidedClaimsIds.length,
                    'voided'
                  );
                  this.loadClaimHeaders(this.selectedTab);
                })
            );
          }
        }
      )
    );
  }

  openRebillClaimDialog(claimIds: number[]) {
    const dialogRef = this.dialogService.open({
      content: RebillClaimDialogComponent,
      title: 'Rebill Claim',
      width: 540,
    });

    this.subscriptions.add(
      dialogRef.result.subscribe(
        (result: DialogCloseResult | RebillClaimDialogResult) => {
          if (
            !(result instanceof DialogCloseResult) &&
            (result as RebillClaimDialogResult).submit
          ) {
            const dialogResult = result as RebillClaimDialogResult;

            this.subscriptions.add(
              this.claimsService
                .rebillClaims({
                  ClaimsToRebill: {
                    claimIds,
                    rebillReason: dialogResult.rebillReason,
                    submissionReasonCode: dialogResult.submissionReasonId,
                    note: dialogResult.note,
                    claimNote: dialogResult.claimnote,
                  },
                  AccountInfoId:
                    this.accountService.memberDetails.accountInfoId,
                  MemberId: this.accountService.memberDetails.memberId,
                })
                .subscribe((rebilledClaimsIds) => {
                  // rebilledClaimsIds.forEach(claimIdentifier => {
                  //     this.notificationService.showNotificationSuccess(`Claim <${claimIdentifier}/> has been rebilled`);
                  // })
                  this.showNotifications(
                    rebilledClaimsIds.length,
                    claimIds.length - rebilledClaimsIds.length,
                    'rebilled'
                  );
                  this.loadClaimHeaders(this.selectedTab);
                })
            );
          }
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
              submitObservable = this.claimsService.submitClaims(submitModel);
            } else {
              const rebillsubmitModel: ClaimsSecondaryBillingRebillModel = {
                ClaimId: claimId,
                isSecondary: true,
                adjustmentLevel: dialogResult.isClaimLevelAdjustment ? 1 : 2,
                secondaryFunderDetails: [secondaryFunderDetails],
                AccountInfoId: this.accountService.memberDetails.accountInfoId,
                MemberId: this.accountService.memberDetails.memberId,
              };
              submitObservable =
                this.claimsService.rebillSecondaryBillingClaims(
                  rebillsubmitModel
                );
            }

            this.subscriptions.add(
              submitObservable.subscribe(
                (claimIdentifier) => {
                  const successMsg =
                    mode === billingMode.BillNextFunder
                      ? 'The claim has been billed electronically.'
                      : 'The claim has been rebilled electronically.';
                  this.notificationService.showNotificationSuccess(successMsg);
                  this.loadClaimHeaders(this.selectedTab);
                },
                (error) => {
                  const errorMsg =
                    mode === billingMode.BillNextFunder
                      ? 'The claim could not be billed. Please check validations and try again.'
                      : 'The claim could not be rebilled. Please check validations and try again.';
                  this.showVoidNotificationError(errorMsg);
                }
              )
            );
          }
        }
      )
    );
  }

  deleteClaims(claimIds: number[]) {
    this.subscriptions.add(
      this.claimsService
        .deleteClaims(claimIds)
        .subscribe((deletedClaimsIds) => {
          this.showNotifications(
            deletedClaimsIds.length,
            claimIds.length - deletedClaimsIds.length,
            'deleted'
          );
          this.loadClaimHeaders(this.selectedTab);
        })
    );
  }

  reRunValidation() {
    if (this.mySelection.length) {
      this.isReRunValidation = true;

      var apiCalls = this.mySelection.map((value) =>
        this.claimsService.ReRunValidation({
          Id: value,
          isSecondary: false,
          secondaryFunderId: undefined,
          AccountInfoId: this.accountService.memberDetails.accountInfoId,
          MemberId: this.accountService.memberDetails.memberId,
        })
      );

      forkJoin(apiCalls)
        .pipe(takeUntil(this.destroy.destroy))
        .subscribe({
          next: (response) => {
            this.showNotifications(
              response.where((x) => x != -1).length,
              response.where((x) => x == -1).length,
              'validated'
            );
            this.loadClaimHeaders(this.selectedTab);
            this.isReRunValidation = false;
          },
        });
    }
  }

  rebillSelectedClaim() {
    if (!this.mySelection.length) return;
    const claimsToRebill: ClaimHeader[] = [];

    this.view.pipe(take(1)).subscribe((result) => {
      for (const dataItem of result.data) {
        if (this.mySelection.indexOf(dataItem.id) != -1) {
          claimsToRebill.push(dataItem);
        }
      }
    });

    if (claimsToRebill.length > 0) {
      if (
        this.selectedTab === ClaimListingTab.Flagged ||
        this.selectedTab === ClaimListingTab.Closed
      ) {
        const allowedToRebillClaimsIds = claimsToRebill
          .filter((claim) =>
            this.canPerformActionOnClaim(claim.submissionStatusId)
          )
          .map((claim) => claim.id);

        const rejectedClaimsToRebillIds = claimsToRebill.filter(
          (claim) => !allowedToRebillClaimsIds.includes(claim.id)
        );
        rejectedClaimsToRebillIds.forEach((claim) =>
          this.showRebillNotificationError(claim.claimNumber)
        );

        if (allowedToRebillClaimsIds.length > 0) {
          this.openRebillClaimDialog(allowedToRebillClaimsIds);
        }
      } else {
        this.openRebillClaimDialog(claimsToRebill.map((claim) => claim.id));
      }
    }
  }

  billNextFunderSelectedClaim(claimId: number, mode: billingMode) {
    var selectedClaim: any;
    // if(claimId)
    // {
    this.view.pipe(take(1)).subscribe((result) => {
      selectedClaim = result.data.first((item) => item.id === claimId);
    });
    // }
    // else{
    //     this.view.subscribe( result => {
    //         selectedClaim = result.data.first(item => item.id === this.mySelection[0]);
    //         });
    //     claimId = this.mySelection[0];
    // }

    this.subscriptions.add(
      this.claimsService.getClaimBillNextFunders(selectedClaim.id).subscribe(
        (result) => {
          this.openBillNextFunderDialog(selectedClaim.id, result, mode);
        },
        (error) => {
          this.loadClaimHeaders(this.selectedTab);
          const errMsg =
            'Secondary funder not available. Please check your secondary payer information or reload claim data and try again.';
          this.notificationService.showNotificationError(errMsg);
        }
      )
    );
  }

  SecondaryFunderExists(
    claimStatus: string,
    submissionTypeId: any,
    patientResponsibility: number,
    IsSecondaryPayerAvailable?: boolean
  ): boolean {
    if (
      claimStatus != 'Void - Closed' &&
      patientResponsibility !== 0 &&
      IsSecondaryPayerAvailable &&
      (this.selectedTab == 4 ||
        this.selectedTab == 5 ||
        this.selectedTab == 6) &&
      submissionTypeId !== ClaimSubmissionType.Transfer
    )
      return true;
    else return false;
  }

  RebillPostSecondaryBillingExists(
    claimStatus: string,
    submissionTypeId: any,
    patientResponsibility: number,
    IsSecondaryPayerAvailable?: boolean
  ): boolean {
    if (
      claimStatus != 'Void - Closed' &&
      IsSecondaryPayerAvailable &&
      (this.selectedTab == 4 ||
        this.selectedTab == 5 ||
        this.selectedTab == 6) &&
      submissionTypeId == ClaimSubmissionType.Transfer
    )
      return true;
    else return false;
  }

  writeOffClaims(model?: ClaimHeader) {
    if (!this.mySelection.length && !model) return;
    const claimsOrChargeToWriteOff: ClaimOrChargeToWriteOff[] = [];

    if (!model) {
      if (this.mySelection.length > 0) {
        this.view.pipe(take(1)).subscribe((result) => {
          for (const dataItem of result.data) {
            if (this.mySelection.indexOf(dataItem.id) != -1) {
              let writeOffchargeModel: ClaimOrChargeToWriteOff = {
                claimId: dataItem.id,
                chargeId: null,
                balanceAmount: dataItem.balanceAmount,
              };
              claimsOrChargeToWriteOff.push(writeOffchargeModel);
            }
          }
        });
      }
    } else {
      let writeOffchargeModel: ClaimOrChargeToWriteOff = {
        claimId: model.id,
        chargeId: null,
        balanceAmount: model.balanceAmount,
      };
      claimsOrChargeToWriteOff.push(writeOffchargeModel);
    }

    const dialogRef = this.dialogService.open({
      content: WriteOffClaimDialogComponent,
      title: 'Writeoff Claim',
      width: 540,
    });

    dialogRef.content.instance.claimsOrChargeToWriteOff =
      claimsOrChargeToWriteOff;
    dialogRef.content.instance.isServiceLine = false;
    dialogRef.result.subscribe((result: any) => {
      var apiCalls: Observable<any>[] = [];
      if (result && result.data) {
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
          const failedCount = responses.length - sucessCount;
          if (apiCalls.length > 1 && sucessCount > 0) {
            claimsOrChargeToWriteOff.forEach((x) => {
              let index = this.IndexList.first((a) => a.claimId == x.claimId);
              if (index != null)
                this.onExpanderClick(index.index, index.anchor, index.claimId);
            });
            this.refreshClaims$.next(); // Refresh grid after multiple write-offs
            this.notificationService.showNotificationSuccess(
              `${sucessCount} Claim(s) have been Written-off.`
            );
          }
          if (apiCalls.length > 1 && failedCount > 0) {
            this.notificationService.showNotificationError(
              failedCount + ` Claim(s) has not been Written-off.`
            );
          }
          if (apiCalls.length === 1) {
            const response = responses.first();
            if (response.success) {
              this.notificationService.showNotificationSuccess(
                'Claim has been Written-off.'
              );
              this.refreshClaims$.next(); // Refresh grid after single write-off
            } else if (!response.success && response.errorMsg.length) {
              this.notificationService.showNotificationError(
                'Can not perform Evenly Across write off for selected claim'
              );
            } else {
              this.notificationService.showNotificationError(
                'This Claim could not be written off. Write off amount exceeds the claim balance.'
              );
            }
          }
        });
      }
    });
  }

  showNotifications(sucessCount: number, failedCount: number, action: string) {
    if (failedCount > 0) {
      this.notificationService.showNotificationError(
        failedCount + ` Claim(s) has not been ` + action
      );
    }
    if (sucessCount > 0) {
      this.notificationService.showNotificationSuccess(
        sucessCount + ` Claim(s) has been ` + action
      );
    }
  }

  showDeleteNotificationError(claimIdentifier: string) {
    const content = `Claim <${claimIdentifier}/> cannot be deleted as it has been submitted to funder`;
    this.notificationService.showNotificationError(content);
  }

  showVoidNotificationError(claimIdentifier: string) {
    const content = `Claim <${claimIdentifier}/> cannot be voided as it has been not previously billed. Delete the claim instead of Void`;
    this.notificationService.showNotificationError(content);
  }

  showRebillNotificationError(claimIdentifier: string) {
    const content = `Claim <${claimIdentifier}/> cannot be rebilled as it has been not previously billed.`;
    this.notificationService.showNotificationError(content);
  }

  addNote() {
    if (!this.mySelection.length) return;
    const dialogRef = this.dialogService.open({
      content: AddClaimNotesDialogComponent,
      title: 'Add Note',
      width: 540,
    });
    dialogRef.content.instance.claimIds = this.mySelection;

    this.subscriptions.add(
      dialogRef.result.subscribe(
        (result: DialogCloseResult | AddClaimNotesDialogResult) => {
          if (
            !(result instanceof DialogCloseResult) &&
            (result as AddClaimNotesDialogResult).data
          ) {
            let claimNotesModel: ClaimNotesSaveModel = {
              claimNoteModels: [],
              memberId: 0,
            };
            claimNotesModel.memberId =
              this.accountService.memberDetails.memberId;
            let claimsToAddNote: ClaimNoteModel[] = [];
            let claimToAddNote: ClaimNoteModel = {
              claimId: 0,
              remindDate: undefined,
              note: '',
            };
            this.mySelection.forEach((claimId) => {
              claimToAddNote.claimId = claimId;
              claimToAddNote.note = (
                result as AddClaimNotesDialogResult
              ).data[0].note;
              claimToAddNote.remindDate = (
                result as AddClaimNotesDialogResult
              ).data[0].remindDate;
              claimsToAddNote.push(Object.assign({}, claimToAddNote));
              claimNotesModel.memberId =
                this.accountService.memberDetails.memberId;
            });
            claimNotesModel.claimNoteModels = claimsToAddNote;

            this.claimNotesService
              .addToSeveral(claimNotesModel)
              .subscribe(() => {
                this.notificationService.showNotificationSuccess(
                  this.mySelection.length + ' note(s) added successfully.'
                );
                this.loadClaimHeaders(this.selectedTab);
              });
          }
        }
      )
    );
  }

  editClaimNotes(model: ClaimHeader) {
    const claimNoteGetModel: ClaimNoteGetModel = {
      id: model.id,
      patientId: model.childProfileId,
      patientName: model.patientName,
      dateOfService: model.dateOfServiceStart,
    };

    this.subscriptions.add(
      this.sidebarService
        .openRight(ClaimNotesComponent, true, 'md')
        .subscribe((rsidebarRef) =>
          rsidebarRef.instance.setData(claimNoteGetModel)
        )
    );
  }

  printBulk(clearSelection = true) {
    if (this.mySelection.length > 0) {
      const dialogRef = this.dialogService.open({
        content: HFCAprintComponent,
      });
      dialogRef.content.instance.claimsToPrint = this.claimsToPrint;
    }
    this.claimsToPrint = [];
    if (clearSelection) this.mySelection = [];
  }

  markAsBilledBulk() {
    if (this.mySelection.length > 0) {
      const claimsToBill = [];
      this.view.pipe(take(1)).subscribe((result) => {
        for (const dataItem of result.data) {
          if (this.mySelection.indexOf(dataItem.id) !== -1) {
            claimsToBill.push(dataItem.id);
          }
        }
      });

      if (claimsToBill.length > 0) {
        this.subscriptions.add(
          this.claimsService.markBilledClaims(claimsToBill).subscribe(() => {
            this.loadClaimHeaders(this.selectedTab);
          })
        );
      }
    }
  }

  canPerformActionOnClaim(submissionStatusId: ClaimSubmissionStatus) {
    return !SubmissionStatusesToRejectClaimAction.some(
      (statusId) => submissionStatusId === statusId
    );
  }

  printMarkAsBilledBulk() {
    this.printBulk(false);
    this.markAsBilledBulk();
  }

  showClientInfo(dataItem: ClaimHeader) {
    this.subscriptions.add(
      this.sidebarService
        .openRight(PatientDetailsComponent, true, 'md')
        .subscribe((rsidebarRef) =>
          rsidebarRef.instance.setData(
            dataItem.childProfileId,
            dataItem.patientName
          )
        )
    );
  }

  private getCurrentTabName(): string {
    switch (this.selectedTab) {
      case ClaimListingTab.PendingReview:
        return 'Pending Review';
      case ClaimListingTab.ReadyToBill:
        return 'Ready to Bill';
      case ClaimListingTab.BillingPending:
        return 'Billed - Pending';
      case ClaimListingTab.Closed:
        return 'Completed';
      case ClaimListingTab.Rejected:
        return 'Rejected';
      case ClaimListingTab.Denied:
        return 'Denied';
      case ClaimListingTab.Flagged:
        return 'Flagged';
      default:
        return 'Pending Review';
    }
  }

  private updateBreadcrumbsForExpansion(expanded: boolean) {
    const tabName = this.getCurrentTabName();
    if (expanded) {
      this.breadcrumbs = [
        { label: 'Claims', url: '/billing/claims/list' },
        { label: tabName, url: '/billing/claims/list' },
        { label: 'Charge Details', url: null },
      ];
    } else {
      this.breadcrumbs = [
        { label: 'Claims', url: '/billing/claims/list' },
        { label: tabName, url: '/billing/claims/list' },
      ];
    }
  }

  handleBreadcrumbClick(breadcrumb: any, index: number): void {
    if (index === this.breadcrumbs.length - 1) {
      return; // Don't navigate if clicking on the last breadcrumb
    }

    if (breadcrumb.label === 'Claims') {
      // Navigate back to Pending Review tab (default) and collapse any expanded details
      this.hideExpendedRow();

      // Reset filter state similar to selectedTabChanged
      if (this.claimFiltersComponent) {
        this.claimFiltersComponent.isFiltersApplied = false;
      }

      // Clear selections
      this.mySelection = [];

      this.selectedTabIndex = 0; // Default to Pending Review tab (index 0)
      this.selectedTab = ClaimListingTab.PendingReview;
      this.claimListingTabGroup.selectedIndex = 0;

      // Update breadcrumbs to show default state
      this.breadcrumbs = [
        { label: 'Claims', url: '/billing/claims/list' },
        { label: 'Pending Review', url: '/billing/claims/list' },
      ];

      // Load the default tab data
      this.loadClaimHeaders(this.selectedTab, true);
    } else if (breadcrumb.label === this.getCurrentTabName()) {
      // Just collapse expanded details, stay on current tab
      this.hideExpendedRow();
    }
  }

  onExpanderClick(index: number, anchor: HTMLAnchorElement, claimId: number) {
    const isExpanded = !anchor.classList.contains('k-plus');
    this.expendedIndex = isExpanded ? -1 : index;
    this.expendedAnchor = isExpanded ? null : anchor;
    this.isDetailsExpanded = !isExpanded;

    if (isExpanded) {
      this.IndexList.removeWhere((x) => x.claimId == claimId);
      anchor.classList.remove('k-minus');
      anchor.classList.add('k-plus');

      this.claimsGrid.collapseRow(index);
      // Update breadcrumbs when collapsing
      this.updateBreadcrumbsForExpansion(false);
    } else {
      this.IndexList.push({ claimId: claimId, index: index, anchor: anchor });
      anchor.classList.remove('k-plus');
      anchor.classList.add('k-minus');

      this.claimsGrid.expandRow(index);
      // Update breadcrumbs when expanding
      this.updateBreadcrumbsForExpansion(true);
    }
  }

  // private fitColumns(): void {
  //     this.ngZone.onStable
  //       .asObservable()
  //       .pipe(take(1))
  //       .subscribe(() => {
  //         this.claimsGrid.autoFitColumns(this.claimsGrid.columns.);
  //       });
  //   }

  ngAfterViewInit(): void {
    // this.fitColumns()
    this.currentTabIndex = this.claimListingTabGroup.selectedIndex;
    this.claimsService.getTabId().subscribe((id) => {
      if (id !== null) {
        this.currentTabIndex = id;
        this.selectedTabIndex = id;
        this.claimsService.getOldFilter().subscribe((value) => {
          this.oldfilter = value;
        });
      }
    });
    if (this.currentTabIndex === this.selectedTabIndex) {
      this.selectedTabChanged(this.selectedTabIndex);
    }

    this.claimListingTabGroup.selectedIndex = this.selectedTabIndex;
    if (this.claimsManagementFilterService.isFilterSet) {
      window.setTimeout(
        (res) =>
          (<HTMLElement>(
            document.querySelector('.filter-btn .outlined-btn')
          ))!.click(),
        1000
      );
    }
  }

  ngOnInit(): void {
    // Set initial breadcrumbs
    this.breadcrumbs = [
      { label: 'Claims', url: '/billing/claims/list' },
      { label: 'Pending Review', url: '/billing/claims/list' },
    ];

    // Setup breadcrumb navigation handler
    this.breadcrumbClickHandler = (event: any) => {
      const target = event.target;
      if (target && target.classList.contains('breadcrumb-link')) {
        event.preventDefault(); // Prevent router navigation
        const breadcrumbText = target.textContent?.trim();
        const breadcrumbIndex = this.breadcrumbs.findIndex(
          (b) => b.label === breadcrumbText
        );
        if (
          breadcrumbIndex !== -1 &&
          breadcrumbIndex < this.breadcrumbs.length - 1
        ) {
          this.handleBreadcrumbClick(
            this.breadcrumbs[breadcrumbIndex],
            breadcrumbIndex
          );
        }
      }
    };
    document.addEventListener('click', this.breadcrumbClickHandler);

    this.subscriptions.add(
      this.route.queryParams.subscribe((params) => {
        if (params.tab) {
          this.selectedTabIndex = +params.tab;
          this.router.navigate([], {
            queryParams: {
              tab: null,
            },
            queryParamsHandling: 'merge',
          });
        }
      })
    );

    const storedViewColumnsSettings = localStorage.getItem(
      'viewColumnsSettings'
    )
      ? JSON.parse(localStorage.getItem('viewColumnsSettings') || '')
      : null;

    if (storedViewColumnsSettings) {
      this.viewColumnsSettings = storedViewColumnsSettings;
    } else {
      this.subscriptions.add(
        this.claimsService.getMemberViewSettings().subscribe((x) => {
          this.viewColumnsSettings = x;
        })
      );
    }
    if (this.canEdit) {
      this.getAssignee();
    }
    this.claimDashboardListSubject.subscribe(data => {
    if (
      !this.isAllSelected ||       
      !this.isUserSelectAllChecked || 
      !data || data.length === 0
    ) {
      return;
    }

    for (const item of data) {
      if (!this.mySelection.includes(item.id)) {
        this.mySelection.push(item.id);
      }
    }
    }); 
  }
  private closeAllPopups(): void {
    this.popupOpen.forEach((row, rowIndex) => {
      if (!row) return;
      row.forEach((_, i) => {
        this.popupOpen[rowIndex][i] = false;
      });
    });
  }
  ngOnDestroy(): void {
    this.closeAllPopups();
    this.sidebarService.closeAll();
    this.subscriptions.unsubscribe();
    this.teardownScrollPrefetch();
    if (this.scrollTimeout) {
      clearTimeout(this.scrollTimeout);
    }
    if (this.filterChangedTimeout) {
      clearTimeout(this.filterChangedTimeout);
    }
    // Remove breadcrumb click event listener
    if (this.breadcrumbClickHandler) {
      document.removeEventListener('click', this.breadcrumbClickHandler);
    }
  }

  navigateToFunderSetup(clientId, funderId) {
    var url = this.rethinkUrl + '/core/clients/' + clientId + '/info/funders'; // /edit/' + funderId;
    window.open(url, '_blank');
  }
  navigateToRPSetup(renderingProviderId) {
    var url = this.rethinkUrl + '/core/staff/' + renderingProviderId;
    window.open(url, '_blank');
  }
  navigateToAuthorization(clientId, authId) {
    var url =
      this.rethinkUrl + '/core/clients/' + clientId + '/info/authorizations'; // /' + authId;
    window.open(url, '_blank');
  }

  getUniqueCodes(reasonCodes: string): string[] {
    if (!reasonCodes) return [];
    return Array.from(new Set(reasonCodes.split(',').map((c) => c.trim())));
  }

  getReason(reasonCodes: string): string[] {
    if (!reasonCodes) return [];
    return Array.from(new Set(reasonCodes.split(',').map((c) => c.trim())));
  }

  getCarcCodes(isTabChanged: boolean): void {
    if (this.carcCodes.length === 0) {
      this.subscriptions.add(
        this.claimsService.getCarcCodes().subscribe((codes: CarcCodes[]) => {
          this.allcarcCodes = codes;
          this.carcCodes = codes;
          this.claimsService.setCarcCode(this.carcCodes);
          this.loadClaimHeaders(this.selectedTab, isTabChanged);
        })
      );
    }
  }

  getCarcDescription(code: string): string {
    const match = this.carcCodes.find((c) => c.code === code.trim());
    return match ? match.description : 'No description available';
  }

  getReasonDescription(trid: any): string {
    const id = Number(trid);

    const match = this.data.find((c) => c.flagReasonTransactionId === id);

    if (!match) {
      return `Reason:\nNo reason available\n\nComment:\nNo comment available`;
    }

    return `${match.comment}`;
  }

  getGridPageSizes(): void {
    const storedGridPageSizes = localStorage.getItem('gridPageSizes')
      ? JSON.parse(localStorage.getItem('gridPageSizes') || '')
      : null;
    const storedDefaultPageSize = localStorage.getItem('defaultPageSize')
      ? JSON.parse(localStorage.getItem('defaultPageSize') || '')
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

  toggleStatusDropdown(dataItem: any | null) {
    // If dataItem is null we close the dropdown; otherwise toggle for that id
    const id = dataItem ? dataItem.id : null;
    this.showStatusDropdown = this.showStatusDropdown === id ? null : id;
  }

  updateClaimStatus(dataItem: any, statusId: string) {
    const claimId =
      typeof dataItem.id === 'number' ? dataItem.id : parseInt(dataItem.id, 10);
    this.claimsService
      .updateClaimStatus({ claimId, claimStatusId: parseInt(statusId) })
      .subscribe({
        next: () => {
          this.notificationService.showNotificationSuccess(
            'Claim status updated successfully.'
          );
          this.loadClaimHeaders(this.selectedTab, true);
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

  onShowVoidedChange(value: boolean) {
    this.childViewVoidedFlag = !!value;
  }

  getAssignee() {
    this.claimsService
      .getAssignee({Tab: 0, SearchValue: '' , AccountInfoId: this.accountService.memberDetails.accountInfoId})
      .pipe(takeUntil(this.unsubscribeAll$))
      .subscribe((users: AssigneeModel[]) => {
        this.assignableUsers = users;
        this.filteredAssignableUsers = [...this.assignableUsers];
      });
  }

  // ASSIGNEE – OPEN POPUP (ROW LEVEL)
  openInlineAssignUserPopup(claim: ClaimHeader) {
    this.closeBulkAssignPopup();
    this.isGriddropdownOpenDisabled = true;
    this.showAssignUserPopupId = claim.id;

    //select first value from API
    this.selectedAssigneeId = claim.assigneeId ? claim.assigneeId : 0;
  }

  // ASSIGNEE – OPEN POPUP (BULK LEVEL)
  openBulkAssignUserPopup() {
    this.closeAssignPopup();
    this.isDropdownOpenDisabled = true;
    const claimsToAssignId: number[] = [];
    this.view.pipe(take(1)).subscribe((result) => {
      for (const dataItem of result.data) {
        if (this.mySelection.indexOf(dataItem.id) !== -1) {
          claimsToAssignId.push(dataItem.id);
        }
      }
    });
    this.showBulkAssignUserPopupId = claimsToAssignId;
    this.selectedAssigneeId = 0;
  }

  // ASSIGNEE – ASSIGN USER TO CLAIM
  Assignee() {
    if (this.showAssignUserPopupId || this.showBulkAssignUserPopupId) {
      const assigneeRequest: AssigneeRequestModel = {
        claimIds: this.showAssignUserPopupId
          ? [this.showAssignUserPopupId]
          : this.showBulkAssignUserPopupId,
        assigneeId: this.selectedAssigneeId,
        memberId: this.accountService.memberDetails.memberId,
      };
      this.claimsService.assignUserToClaim(assigneeRequest).subscribe((x) => {
        if (x) {
          this.notificationService.showNotificationSuccess(
            'Assignee updated successfully.'
          );
          this.loadClaimHeaders(this.selectedTab);
          this.closeAssignPopup();
        } else {
          this.notificationService.showNotificationError(
            'Error while updating assignee.'
          );
        }
      });
    } else {
      this.notificationService.showNotificationError(
        'No claims selected for assignee update.'
      );
    }
  }

  // ASSIGNEE – CLOSE POPUP
  closeAssignPopup() {
    this.showAssignUserPopupId = null;
    this.selectedAssigneeId = 0;
    this.filteredAssignableUsers = [...this.assignableUsers];
  }

  //BULK  ASSIGNEE – CLOSE POPUP
  closeBulkAssignPopup() {
    this.showBulkAssignUserPopupId = [];
    this.selectedAssigneeId = 0;
    this.filteredAssignableUsers = [...this.assignableUsers];
  }

  // Add the search method
  assigneeSearchValueChanged(event: any): void {
    const searchValue = event.target.value.toLowerCase();
    if (searchValue) {
      this.filteredAssignableUsers = this.assignableUsers.filter((user) =>
        user.name.toLowerCase().includes(searchValue)
      );
    } else {
      this.filteredAssignableUsers = [...this.assignableUsers];
    }
  }

  // Clear the search input
  clearAssigneeSearch(searchInput: HTMLInputElement): void {
    searchInput.value = '';
    this.filteredAssignableUsers = [...this.assignableUsers];
  }

  toggleSelectAll(event: Event) {
  const checkbox = event.target as HTMLInputElement;
  this.selectAllChecked = checkbox.checked;

  this.view.subscribe((gridData) => {
    if (!gridData?.data) return;

    const selectableRows = gridData.data.filter(item => item.patientName);
    const selectableIds = selectableRows.map(item => item.id);

    if (this.selectAllChecked) {
      this.mySelection = Array.from(
        new Set([...this.mySelection, ...selectableIds])
      );
      for (const row of selectableRows) {
        if (!this.claimsToPrint.some(c => c.claimId === row.id)) {
          this.claimsToPrint.push({
            claimId: row.id,
            cmsPages: row.cmsPagesCount
          });
        }
      }
    } else {
      this.mySelection = this.mySelection.filter(
        id => !selectableIds.includes(id)
      );
      this.claimsToPrint = this.claimsToPrint.filter(
        c => !selectableIds.includes(c.claimId)
      );
    }
  }).unsubscribe(); 
}

  onCheckboxChange(event: Event, dataItem: any) {
    const checkbox = event.target as HTMLInputElement;

    if (checkbox.checked) {
      if (!this.mySelection.includes(dataItem.id)) {
        this.mySelection.push(dataItem.id);
      }
      if (!this.claimsToPrint.some(c => c.claimId === dataItem.id)) {
        this.claimsToPrint.push({
          claimId: dataItem.id,
          cmsPages: dataItem.cmsPagesCount
        });
      }
    } else {
      this.mySelection = this.mySelection.filter(
        id => id !== dataItem.id
      );
      this.claimsToPrint = this.claimsToPrint.filter(
        c => c.claimId !== dataItem.id
      );
    }

    this.updateSelectAllState();
  }

  updateSelectAllState() {
    this.view.subscribe((gridData) => {
      if (!gridData?.data) return;

      const selectableRows = gridData.data.filter(item => item.patientName);
      this.selectAllChecked =
        selectableRows.length > 0 &&
        selectableRows.every(row => this.mySelection.includes(row.id));
    }).unsubscribe();
  }

  getExternalCodes(isTabChanged: boolean): void {
    if (this.externalCodes.length === 0) {
      this.subscriptions.add(
        this.claimsService.getExternalCodes().subscribe((codes: ExternalCodes[]) => {
          this.allExternalCodes = codes;
          this.externalCodes = codes;
          this.claimsService.setExternalCode(this.externalCodes);
          this.loadClaimHeaders(this.selectedTab, isTabChanged);
        })
      );
    } 
  }

  getExternalDescription(code: string): string {
    const match = this.externalCodes.find((c) => c.code === code.trim());
    return match ? match.description : 'No description available';
  }
}
