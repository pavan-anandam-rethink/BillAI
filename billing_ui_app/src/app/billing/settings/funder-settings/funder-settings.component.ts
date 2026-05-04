import {
  Component,
  forwardRef,
  Input,
  ViewChild,
  OnInit,
  AfterViewInit,
  ChangeDetectorRef,
} from '@angular/core';
import {
  GridDataResult,
  PageChangeEvent,
  PagerSettings,
  SelectableSettings,
} from '@progress/kendo-angular-grid';
import {
  map,
  Observable,
  of,
  Subscription,
  filter,
  fromEvent,
  throttleTime
} from 'rxjs';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { AccountsReceivablesRequestModel } from '@core/models/billing/report-model';
import { ReportService } from '@core/services/billing/report.service';
import { SortDescriptor, State } from '@progress/kendo-data-query';
import { PaginationService } from '@core/services/billing/pagination.service';
import { ListFilterSort } from '@core/models/billing';
import { DatePipe } from '@angular/common';
import { NotificationService } from '@progress/kendo-angular-notification';
import { Helper } from '@app/billing/encounters/common/common-helper';
import { ClaimService } from '@core/services/billing';
import { DialogService } from '@progress/kendo-angular-dialog';
import { AccountPermissions } from '@core/enums/account/account-permissions';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { Breadcrumb } from '@core/models/billing/bread-crumb';
import { FunderSettingsSaveComponent } from './funder-settings-save/funder-settings-save.component';
import { FunderSettingsListSubject } from '../../../core/subjects/funder-settings.subject';
import { BillingFeatures, ClaimFilingIndicatorModel } from '@core/models/billing/claim-filingIndicator-model';
import { BillingFunderSettingService } from '../../../core/services/billing/billing-funder-setting.service';
import { BillingFunderListRequestModel } from '../../../core/models/billing/billingFunderSetting-model';
import { BillingFunderSettingRequestModel } from '../../../core/models/billing/billingFunderSetting-model';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { BillingFunderSettings } from '@core/models/billing/billingFunderSetting-model';
import { Router } from '@angular/router';


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
  selector: 'app-funder-settings',
  templateUrl: './funder-settings.component.html',
  styleUrls: ['./funder-settings.component.css']
})
export class FunderSettingsComponent {
  @ViewChild(forwardRef(() => FunderSettingsSaveComponent))
  filtersComponent: FunderSettingsSaveComponent;
  @ViewChild('encountersGrid', { static: true }) grid: any;
  userList: ClaimFilterOptionModel[] = [];
  userEditList: ClaimFilterOptionModel[] = [];
  claimFilingIndicator: ClaimFilingIndicatorModel[] = [];
  view: Observable<GridDataResult>;
  funderSettingsListSubject: FunderSettingsListSubject;
  searchText: string = '';
  subscriptions = new Subscription();
  billingFunderListRequest: any;
  gridState: ListFilterSort = new ListFilterSort();
  gridHeight = 600;
  gridPageSizes: any;
  canEdit = false;
  public isAllSelected: boolean = false;
  public prefetchThresholdprefetchThreshold: boolean = false;
  private virtualScrollPageSize: number = 50;
  private scrollPrefetchSub: Subscription | null = null;
  private prefetchThreshold = 0.7;
  private scrollDebounceMs = 150;
  public isVirtualMode = false;
  public scrollSkip = 0;
  private windowStart = 0;
  private readonly pageSize = 50;
  private readonly maxWindowSize = 200;
  private lastScrollTop: number = 0;
  private isScrollLocked = false;
  private isProgrammaticScroll = false;
  isEditMode = false;
  selectedFunderSettingId: number | null = null;
  selectedEditRecord: any = null;
  billingFeatures: BillingFeatures[] = [];
  public isLoading = true;
  public funderSettingsData: BillingFunderSettings[] = [];
  public funderList: any[] = [];
  public claimIndicatorList: any[] = [];
  public selectedRowData: any; 
  public totalRecords: number = 0;
  removefunderid: number[] = [];
  public getTimeZone ={};
  public claimFilingIndicatorModel = [];


  constructor(private datePipe: DatePipe,
    private reportingService: ReportService,
    private billingFunderSettingService: BillingFunderSettingService,
    private paginationService: PaginationService,
    private cdr: ChangeDetectorRef,
    private claimsService: ClaimService,
    private notificationService: NotificationService,
    private notificationHandlerService: NotificationHandlerService,
    private dialogService: DialogService,
    private accountService: AccountMemberService,
    private router: Router
  ) {
   
    this.billingFunderSettingService.getAllClaimFilingIndicators().subscribe((x) => {
      this.claimFilingIndicator =
        this.claimFilingIndicator.length == 0
          ? x.map((y) => ({
            id: y.id,
            indicator: y.indicator,
            checked: false,
          }))
        : this.claimFilingIndicator;
      this.isLoading = false;
    });

    this.funderSettingsListSubject = new FunderSettingsListSubject(
      this.reportingService,
      this.billingFunderSettingService
    );
    this.isLoading = true;
    this.isSubjectLoading$ = this.funderSettingsListSubject.getLoading();
    this.refreshGrid();
    this.view = this.funderSettingsListSubject.asObservable().pipe(
      map((data) => ({
       data,
        total: this.funderSettingsListSubject.getCount(),
      }))
    );
    this.getGridPageSizes();

    //Get Billing Features
    this.billingFunderSettingService.getBillingFeatures().subscribe((x) => {
      this.billingFeatures = x;
      console.log(this.billingFeatures);
    });
  }

  private softReloadCurrentComponent(): void {
    const currentUrl = this.router.url; // preserves query params too
    this.router
      .navigateByUrl('/__refresh__', { skipLocationChange: true }) // fake hop
      .then(() => this.router.navigateByUrl(currentUrl));         // back to same
  }

  ngOnInit() {
    this.subscriptions.add(
      this.accountService.accountMemberSettings.subscribe((x) => {
        if (x) {
          this.canEdit = this.accountService.checkPermissionLevel(
            AccountPermissions.BillingEdit
          );
        }
      })
    );

    this.subscriptions.add(
      this.funderSettingsListSubject.asObservable()
        .subscribe(data => {
          this.funderSettingsData = data;
          this.removefunderid = data.map(x => x.funderId);
          this.totalRecords = this.funderSettingsListSubject.getCount();
          this.getTimeZone = this.funderSettingsListSubject.getTimeZone();
          this.claimFilingIndicatorModel = this.funderSettingsListSubject.getClaimFilingIndicatorModel();
        })
    );
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
  }

  clearFilter() {
    this.isLoading = false;
    if (this.funderSettingsListSubject.getCount()) {
      this.funderSettingsListSubject.setCount(0);
      this.funderSettingsListSubject.clear();
    } else {
      this.showNotificationError('No data to clear');
    }
  }

  onFunderSearch(): void {
    
      const params = new BillingFunderListRequestModel();
      params.filterModels = [{
        propertyName: 'funderName',
        operatorName: 'contains', 
        value: this.searchText.trim()
      }];
      params.take = 20;
     this.funderSettingsListSubject.getReport(params);
    
    
  }
  
  showNotificationError(content: string) {
    this.notificationService.show({
      content,
      animation: { type: 'fade', duration: 500 },
      position: { horizontal: 'center', vertical: 'top' },
      type: { style: 'error', icon: false },
      closable: false,
    });
  }

  filterChangedTimeout: number;
/*  public isLoading = true;*/
  public isSubjectLoading$: Observable<boolean>;
  public mySelection: number[] = [];
  showFilter: boolean;
  public dialogForTotalCount: boolean = false;
  public gatClaimsTabData: boolean = false;

 public onSortChange(sort: any[]): void {
    this.gridState.sortingModels = sort;
    this.loadData(this.gridState);
  }


  ngAfterViewInit() {
    this.filtersComponent.OnInitClearFilters();
    this.toggleFilter(true);
    if (this.grid) {
      this.grid.autoFitColumns(this.grid.columns.toArray(), 'header');
    }
    setTimeout(() => this.setupScrollListener(), 0);
  }
  private setupScrollListener(): void {
    const gridEl =
      this.grid?.wrapper?.nativeElement?.querySelector('.k-grid-content');

    if (!gridEl) return;

    this.scrollPrefetchSub = fromEvent(gridEl, 'scroll')
      .pipe(throttleTime(this.scrollDebounceMs))
      .subscribe(() => {
        if (this.funderSettingsListSubject.getVirtualLoadingValue()) return;

        const gridEl = this.getGridScrollElement();
        if (!gridEl) return;

        const scrollTop = gridEl.scrollTop;
        const clientHeight = gridEl.clientHeight;
        const scrollHeight = gridEl.scrollHeight;

        const direction = scrollTop > this.lastScrollTop ? 'down' : 'up';
        this.lastScrollTop = scrollTop;

        const ratio = (scrollTop + clientHeight) / scrollHeight;

        if (ratio >= 0.8 && direction === 'down') {
          this.handleVirtualScrollLoadDown();
        } else if (ratio <= 0.2 && direction === 'up') {
          this.handleVirtualScrollLoadUp();
        }
      });

  }
  getFooterRange(): string {
    if (!this.isAllSelected) {
      return '0';
    }
    const currentLength = this.funderSettingsListSubject.getDataLength();
    const total = this.funderSettingsListSubject.getCount();

    if (currentLength === 0) {
      return '0';
    }
    const loadedData = this.funderSettingsListSubject.getLoadedPages().size * total;
    const _start = loadedData - 200;
    const start = (_start < 0 ? 0 : _start) + 1;
    const end = Math.min(loadedData, total);

    return `${start}–${end > 0 ? end : this.pageSize}`;
  }


  loadData(params: ListFilterSort): void {    

    const filter = new BillingFunderListRequestModel();
  
    filter.sortingModels = params.sortingModels;
    if (this.isVirtualMode) {
      filter.take = params.take == undefined ? this.virtualScrollPageSize : params.take;
      filter.skip = 0;
    } else {
      filter.take = this.gridState.take || 0;
      filter.skip = this.gridState.skip || 0;
    }

    this.funderSettingsListSubject.getReport(filter);
  }

  toggleFilter(event: boolean) {
    this.filtersComponent.opened = event;
    this.showFilter = event;
    this.cdr.detectChanges();
  }
  onFilterChanged() {
    if (this.filterChangedTimeout) {
      clearTimeout(this.filterChangedTimeout);
    }

    this.filterChangedTimeout = window.setTimeout(() => 1000);
  }
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

  onPageChange(event: PageChangeEvent): void {
    this.dialogForTotalCount = false;
    if (event.skip === 0 && event.take === 0) {
      this.isVirtualMode = true;

      this.funderSettingsListSubject.setVirtualMode(true);
      this.windowStart = 0;
      this.gridState.skip = 0;
      this.gridState.take = 9999999;

      this.paginationService.setPageSizes(0);
      this.resetGridScroll();

      this.funderSettingsListSubject.clear();
      this.funderSettingsListSubject.setCount(0);
      this.isLoading = true;
      this.funderSettingsListSubject.getReport(
        this.buildVirtualFilter(0,9999999)
      );
      this.isLoading = false;
      this.isAllSelected = true;
      return;
    }
    this.isAllSelected = false;
    this.isVirtualMode = false;
    this.gridState.skip = event.skip;
    this.gridState.take = event.take;

    this.resetGridScroll();
    this.loadPatientHeaders();
  }

  public getPageStart(total: number): number {
    if (!total || total === 0) return 0;
    if (this.isVirtualMode) return 1;
    const skip =
      this.gridState && this.gridState.skip ? this.gridState.skip : 0;
    return skip + 1;
  }

  public getPageEnd(total: number): number {
    if (!total || total === 0) return 0;
    if (this.isVirtualMode)
      return this.funderSettingsListSubject.getDataLength();
    const skip =
      this.gridState && this.gridState.skip ? this.gridState.skip : 0;
    const take =
      this.gridState && this.gridState.take ? this.gridState.take : 20;
    return Math.min(skip + take, total);
  }

  loadPatientHeaders() {
    const filter = new BillingFunderListRequestModel();
    filter.take = this.isVirtualMode
      ? this.virtualScrollPageSize
      : this.gridState.take || 0;
    filter.skip = this.gridState.skip || 0;

    this.mySelection = [];

    this.funderSettingsListSubject.getReport(filter);
  }

  applyFilter(event) {
    this.resetGridScroll();
    if (event == undefined) {
      this.showNotificationError('Please select filters');
    } else {
      const filter = new BillingFunderListRequestModel();
      if (this.isVirtualMode) {
        filter.take = this.virtualScrollPageSize;
      } else {
        filter.take = this.gridState.take || 0;
        this.isLoading = false;
      }
      
      this.billingFunderListRequest = event;


      const billingFunderSettingRequestModel = new BillingFunderSettingRequestModel();
      billingFunderSettingRequestModel.accountInfoId = this.accountService.memberDetails.accountInfoId;
      billingFunderSettingRequestModel.funderId = event.funderId;
      billingFunderSettingRequestModel.funderName = event.funderName;
      billingFunderSettingRequestModel.billingFeatures = event.features

      this.billingFunderSettingService
        .setBillingFunderSettings(billingFunderSettingRequestModel)
        .subscribe({
          next: () => {
            this.notificationHandlerService.showNotificationSuccess("Funder Setting added successfully");
            this.refreshGrid();
            this.softReloadCurrentComponent();
          },
          error: (error) => {
            this.notificationHandlerService.showNotificationError("Funder Setting failed to add");
            this.refreshGrid();
            this.softReloadCurrentComponent();
          }
        });

    }
  }

  base64ToBlob(base64: string): Blob {
    const byteCharacters = atob(base64);
    const byteArrays = [];
    for (let offSet = 0; offSet < byteCharacters.length; offSet += 1024) {
      const slice = byteCharacters.slice(
        offSet,
        Math.min(offSet + 1024, byteCharacters.length)
      );
      const byteNumbers = new Array(slice.length);
      for (let i = 0; i < slice.length; i++) {
        byteNumbers[i] = slice.charCodeAt(i);
      }
      const byteArray = new Uint8Array(byteNumbers);
      byteArrays.push(byteArray);
    }
    return new Blob(byteArrays, {
      type: 'application/vnd.openxlmformats-officedocument.spreadsheetml.sheet',
    });
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


  private getGridScrollElement(): HTMLElement | null {
    return this.grid?.wrapper?.nativeElement?.querySelector('.k-grid-content');
  }
  private resetGridScroll(): void {
    const gridEl = this.getGridScrollElement();
    if (!gridEl) return;

    gridEl.scrollTo({
      top: Math.max(0, (gridEl.scrollHeight - gridEl.clientHeight) / 2),
      behavior: 'smooth'
    });
  }

  private handleVirtualScrollLoadDown(): void {
    if (!this.isVirtualMode) return;
    if (this.funderSettingsListSubject.getVirtualLoadingValue()) return;

    const _loadedData = this.funderSettingsListSubject.getLoadedPages().size * this.virtualScrollPageSize;
    if (_loadedData >= this.funderSettingsListSubject.getCount()) return;

    const nextStart = this.windowStart + this.pageSize;
    const total = this.funderSettingsListSubject.getCount();
    if (nextStart >= total) return;

    const loadedData = this.funderSettingsListSubject.getLoadedPages().size * this.virtualScrollPageSize;
    this.windowStart = (loadedData - this.maxWindowSize) >= 0 ? (loadedData - this.maxWindowSize) : 0;

    this.funderSettingsListSubject.append(
      this.buildVirtualFilter(loadedData)
    );

    this.recenterAfterLoad();
  }
  private handleVirtualScrollLoadUp(): void {
    if (!this.isVirtualMode) return;
    if (this.funderSettingsListSubject.getVirtualLoadingValue()) return;

    const _loadedData = this.funderSettingsListSubject.getLoadedPages().size * this.virtualScrollPageSize;
    if (_loadedData == 150) {
      return;
    }
    const loadedData = this.funderSettingsListSubject.getLoadedPages().size * this.virtualScrollPageSize;
    const prevStart = Math.max(0, loadedData - this.pageSize);
    this.windowStart = prevStart;
    this.funderSettingsListSubject.prepend(
      this.buildVirtualFilter(prevStart)
    );

    this.recenterAfterLoad();
  }
  private recenterAfterLoad(): void {
    const gridEl = this.getGridScrollElement();
    if (!gridEl) return;

    this.isProgrammaticScroll = true;
    requestAnimationFrame(() => {
      const maxScrollTop =
        gridEl.scrollHeight - gridEl.clientHeight;

      if (maxScrollTop > 0) {
        gridEl.scrollTop = Math.floor(maxScrollTop / 2);
      }
      this.isProgrammaticScroll = false;
      this.unlockScroll();
    });
  }
  private buildVirtualFilter(skip: number, take?: number): BillingFunderListRequestModel {
    const filter = new BillingFunderListRequestModel();
    //filter. = this.billingSettingsRequest.funder;
    //filter.sortingModels = this.billingSettingsRequest.sortingModels || [];
    filter.skip = skip;
    filter.take = take == undefined ? this.virtualScrollPageSize: take;
    this.scrollSkip = filter.skip;
    return filter;
  }
  private unlockScroll(): void {
    const gridEl = this.getGridScrollElement();
    if (!gridEl) return;

    this.isScrollLocked = false;
    gridEl.style.overflowY = 'auto';
  }

  public isEditPopupOpen = false;

  public editModel: any = {
    funderId: null,
    claimIndicatorId: null,
    includeTaxonomy: false,
    claimFilingIndicator: null,
    funderName:null
  };

    

  private refreshGrid(): void {
    const filter = new BillingFunderListRequestModel();

    if (this.isVirtualMode) {
      this.windowStart = 0;
      filter.skip = 0;
      filter.take = this.virtualScrollPageSize;
    } else {
      filter.skip = this.gridState.skip || 0;
      filter.take = this.gridState.take || 20;
    }

    this.funderSettingsListSubject.clear();
    this.funderSettingsListSubject.setCount(0);

    this.isLoading = true;

    this.funderSettingsListSubject.getReport(filter);
 
    setTimeout(() => {
      this.isLoading = false;
    }, 5);
  }
 selectedFunder: any = null;

  onEdit(dataItem: any) {
  this.selectedFunder = dataItem;
  this.isEditPopupOpen = true;
}
closeEdit() {
  this.isEditPopupOpen = false;
  this.selectedFunder = null;
}
  closePopup() {
    this.isEditPopupOpen = false;
  }

  onFunderChange(selectedFunderId: any) {
    const selectedFunder = this.userList
      .find(x => x.id === selectedFunderId).name;
    this.editModel.funderId = selectedFunderId;
    this.editModel.funderName = selectedFunder;
  }

  onClaimIndicatorChange(selectedIndicatorId: any) {
    const selectedIndicator = this.claimFilingIndicator
      .find(x => x.id === selectedIndicatorId).indicator;
    this.editModel.claimFilingIndicatorId = selectedIndicatorId;
    this.editModel.claimFilingIndicator = selectedIndicator;
  }

  onIncludeTaxonomyChange(includeTaxonomy: boolean) {
    this.editModel.includeTaxonomy = includeTaxonomy;
  }

  saveEdit() {
    // Row ko update karo
    this.selectedRowData.funderId = this.editModel.funderId;
    this.selectedRowData.claimFilingIndicator = this.editModel.claimFilingIndicator;
    this.selectedRowData.includeTaxonomyCode = this.editModel.includeTaxonomyCode;

    const billingFunderSettingRequestModel = new BillingFunderSettingRequestModel();
    billingFunderSettingRequestModel.accountInfoId = this.accountService.memberDetails.accountInfoId;
    billingFunderSettingRequestModel.funderId = this.editModel.funderId;
    billingFunderSettingRequestModel.funderName = this.editModel.funderName;
  
    this.billingFunderSettingService
      .setBillingFunderSettings(billingFunderSettingRequestModel)
      .subscribe({
        next: () => {
          this.notificationHandlerService.showNotificationSuccess("Funder Setting added successfully");
          this.refreshGrid();
        },
        error: (error) => {
          this.notificationHandlerService.showNotificationError("Funder Setting failed to add");
          this.refreshGrid();
        }
      });

    this.isEditPopupOpen = false;
  }


  isDeletePopupOpen = false;

  onDelete(dataItem: any) {
    this.selectedRowData = dataItem;
    this.isDeletePopupOpen = true;
  }

  closeDeletePopup() {
    this.isDeletePopupOpen = false;
    this.selectedRowData = null;
  }

  confirmDelete() {
    if (!this.selectedRowData) return;
    this.billingFunderSettingService
      .deleteBillingFunderSetting(this.selectedRowData.id)
      .subscribe({
        next: () => {
          this.notificationHandlerService
            .showNotificationSuccess("Funder Setting deleted successfully");
          this.refreshGrid();
        },
        error: (error) => {
          this.notificationHandlerService
            .showNotificationError("Funder Setting failed to delete");
          this.refreshGrid();
        }
      });
    this.isDeletePopupOpen = false;
  }
}
