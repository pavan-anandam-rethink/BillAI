import { Component, forwardRef, OnInit, ViewChild } from '@angular/core';
import { ListFilterSort } from '@core/models/billing';
import {
  UnbilledAppointmentsRequestModel,
  UnbilledAppointmentsResponse,
  UnprocessedAppointmentsResponseModel,
} from '@core/models/billing/report-model';
import { PaginationService } from '@core/services/billing/pagination.service';
import { ReportService } from '@core/services/billing/report.service';
import {
  GridDataResult,
  PageChangeEvent,
  PagerSettings,
  SelectableSettings,
  SelectionEvent,
} from '@progress/kendo-angular-grid';
import { SortDescriptor } from '@progress/kendo-data-query';
import { map, Observable, Subscription } from 'rxjs';
import { UnbilledAppointmentsListSubject } from '@core/subjects/unbilled-appointments.subject';
import { Helper } from '@app/billing/encounters/common/common-helper';
import { ToastrService } from 'ngx-toastr';
import { UnbilledAppointmentsService } from '@app/billing/services/worker-services/unbilled-appointment.service';
import { NotifyDialog } from '@core/models/common';
import { ClaimService } from '@core/services/billing';
import { DialogAction } from '@progress/kendo-angular-dialog';
import { DialogService } from '@progress/kendo-angular-dialog';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { AccountPermissions } from '@core/enums/account';
import { UnbilledAppointmentsFiltersComponent } from '../unbilled-appointments/unbilled-appointments-filters/unbilled-appointments-filters.component';
import { UnprocessedAppointmentsListSubject } from '../../../../core/subjects/unprocessed-appointments.subject';
import { Breadcrumb } from '@core/models/billing/bread-crumb';

@Component({
  selector: 'app-unprocessed-appointments',
  templateUrl: './unprocessed-appointments.component.html',
  styleUrls: ['./unprocessed-appointments.component.css'],
})
export class UnprocessedAppointmentsComponent implements OnInit {
  headerTitle = 'Unbilled Appointments';
  public isSubjectLoading$: Observable<boolean>;
  apptInfo: any[] = [];

  subscriptions = new Subscription();
  @ViewChild('encountersGrid', { static: true }) grid: any;
  view: Observable<GridDataResult>;
  unprocessedAppointmentsListSubject: UnprocessedAppointmentsListSubject;

  gridState: ListFilterSort = new ListFilterSort();
  showFilter: boolean = true;
  isLoading: boolean = false;
  @ViewChild(forwardRef(() => UnbilledAppointmentsFiltersComponent))
  filtersComponent: UnbilledAppointmentsFiltersComponent;

  notifyDialog: NotifyDialog = new NotifyDialog(false, '', '');

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
  public mySelection: number[] = [];
  dataToExport: UnbilledAppointmentsResponse[] = [];
  public dialogForTotalCount: boolean = false;
  public gatClaimsTabData: boolean = false;
  canEdit: boolean = false;

  gridPageSizes: any;
  dataItem: UnprocessedAppointmentsResponseModel[];
  constructor(
    public reportingService: ReportService,
    private paginationService: PaginationService,
    private toastr: ToastrService,
    private claimsService: ClaimService,
    private unbilledApptService: UnbilledAppointmentsService,
    private dialogService: DialogService,
    private accountService: AccountMemberService
  ) {
    this.unprocessedAppointmentsListSubject =
      new UnprocessedAppointmentsListSubject(this.reportingService);

    this.isSubjectLoading$ =
      this.unprocessedAppointmentsListSubject.getLoading();

    this.view = this.unprocessedAppointmentsListSubject.pipe(
      map((data) => {
        let result = {
          data: data,
          total: this.unprocessedAppointmentsListSubject.getCount(),
        };
        this.dataToExport = result.data;
        this.dataItem = result.data;
        return result;
      })
    );

    this.getGridPageSizes();
    this.accountService.accountMemberSettings.subscribe((x) => {
      if (x) {
        this.canEdit = this.accountService.checkPermissionLevel(
          AccountPermissions.BillingEdit
        );
      }
    });
  }

  ngOnInit(): void {}

  public onSelectChange(event: SelectionEvent): void {
    this.mySelection.addRange(event.selectedRows.select((x) => x.dataItem.id));
    event.deselectedRows.forEach((x) => this.mySelection.remove(x.dataItem.id));
  }

  loadAppointments() {
    this.isLoading = true;

    const staff: any[] = this.filtersComponent.selectedStaff;
    const filter = new UnbilledAppointmentsRequestModel();
    filter.payerOrFunder = this.filtersComponent.selectedfunders.length === this.filtersComponent.funderList.length ? [] : this.filtersComponent.selectedfunders.map(
      (x) => x.id
    );
    filter.clients = this.filtersComponent.selectedPatients.length === this.filtersComponent.userList.length ? [] : this.filtersComponent.selectedPatients.map((x) => x.id);
    filter.staff = this.filtersComponent.selectedStaff.length === this.filtersComponent.staffList.length ? [] : staff.map((x) => x.typeId);
    filter.location = this.filtersComponent.selectedLocation.length === this.filtersComponent.locationList.length ? [] : this.filtersComponent.selectedLocation.map((x) => x.id);
    filter.placeOfService = this.filtersComponent.selectedPlaceOfService.length === this.filtersComponent.placeOfServiceList.length ? [] : this.filtersComponent.selectedPlaceOfService.map(
      (x) => x.id
    );
    filter.startDate = Helper.shiftDateToUTC(this.filtersComponent.dateFrom);
    filter.endDate = Helper.shiftDateToUTC(this.filtersComponent.dateTo);
    filter.accountInfoId = 0;
    filter.memberId = 0;
    filter.sortingModels = this.gridState.sortingModels;

    filter.take = this.gridState.take || 0;
    filter.skip = this.gridState.skip || 0;
    this.mySelection = [];
    this.unprocessedAppointmentsListSubject.getAll(filter);
  }

  onSortChange(sortParams: SortDescriptor[]): void {
    if (this.dataToExport.length > 0) {
      this.gridState.sortingModels = sortParams;
      this.loadAppointments();
    }
  }

  onPageChange(event: PageChangeEvent): void {
    this.gridState.skip = event.skip;
    this.gridState.take = event.take;

    if (
      this.gridState.take === 0 &&
      this.unprocessedAppointmentsListSubject.getCount() > 1000
    ) {
      this.dialogForTotalCount = true;
    } else {
      this.dialogForTotalCount = false;
      this.gatClaimsTabData = true;
      if (this.gridState.take === 0) this.gridState.take = 9999999;
      this.paginationService.setPageSizes(this.gridState.take);
      this.loadAppointments();
      localStorage.setItem('lastPageSize', this.gridState.take.toString());
    }

    if (this.gridState.take === 0) this.gridState.take = 9999999;
    if (this.dialogForTotalCount) {
      this.subscriptions.add(
        this.SubmitPageCount().result.subscribe((result) => {
          if ((result as DialogAction).text === 'Yes') {
            this.loadAppointments();
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
      content:
        'There are more than 1000 records to display. This may take more time to process. You can either proceed or apply a filter to narrow down the results',
      actions: [{ text: 'Cancel' }, { text: 'Yes', primary: true }],
    });
    return confirmDialog;
  }

  createClaims(): void {
    if (this.dataToExport.length === 0) {
      this.toastr.warning('Please load the appointment data first');
      return;
    }
    if (this.mySelection.length === 0) {
      this.toastr.warning('Please select appointments to create claims');
      return;
    }
    this.reportingService.createClaims(this.mySelection).subscribe((result) => {
      setTimeout(() => {
        this.notifyDialog.title = 'Create Claims';
        this.notifyDialog.message =
          'Appointment(s) have been successfully queued for claim creation. Claim(s) may take several minutes before they are visible on the claims screen.';
        this.notifyDialog.opened = true;
        this.removeRowsFromGrid(this.mySelection);
      });
    });
  }

  removeRowsFromGrid(idsToRemove: number[]) {
    this.dataToExport = this.dataToExport.filter(
      (item) => !this.mySelection.includes(item.id)
    );
    this.mySelection = [];
    const currentData = this.unprocessedAppointmentsListSubject.getValue();
    const updatedData = currentData.filter(
      (item) => !idsToRemove.includes(item.id)
    );
    this.unprocessedAppointmentsListSubject.setCount(updatedData.length);
    this.unprocessedAppointmentsListSubject.next(updatedData);
  }

  exportToExcel() {
    if (this.dataToExport.length === 0) {
      this.toastr.warning('Please load the appointment data first');
      return;
    }
    const staff: any[] = this.filtersComponent.selectedStaff;
    const filter = new UnbilledAppointmentsRequestModel();
    filter.payerOrFunder = this.filtersComponent.selectedfunders.length === this.filtersComponent.funderList.length ? [] : this.filtersComponent.selectedfunders.map(
      (x) => x.id
    );
    filter.clients = this.filtersComponent.selectedPatients.length === this.filtersComponent.userList.length ? [] : this.filtersComponent.selectedPatients.map((x) => x.id);
    filter.staff = this.filtersComponent.selectedStaff.length === this.filtersComponent.staffList.length ? [] : staff.map((x) => x.typeId);
    filter.location = this.filtersComponent.selectedLocation.length === this.filtersComponent.locationList.length ? [] : this.filtersComponent.selectedLocation.map((x) => x.id);
    filter.placeOfService = this.filtersComponent.selectedPlaceOfService.length === this.filtersComponent.placeOfServiceList.length ? [] : this.filtersComponent.selectedPlaceOfService.map(
      (x) => x.id
    );
    filter.startDate = Helper.shiftDateToUTC(this.filtersComponent.dateFrom);
    filter.endDate = Helper.shiftDateToUTC(this.filtersComponent.dateTo);
    filter.accountInfoId = 0;
    filter.memberId = 0;
    filter.sortingModels = this.gridState.sortingModels;

    filter.take = this.gridState.take || 0;
    filter.skip = this.gridState.skip || 0;

    this.toastr.success('The report will be downloaded shortly');

    this.unbilledApptService.exportUnprocessedToExcel(filter).subscribe((res) => {
      if (res) {
        const byteArray = this.base64ToBlob(res.data);
        const url = window.URL.createObjectURL(byteArray);

        const a = document.createElement('a');
        a.href = url;
        a.download = 'Unprocessed Appointments report.xlsx'; // File name for the download
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        a.remove();
      }
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

  getPageStart(total: number): number {
    if (!total) return 0;
    if (this.gridState && this.gridState.take === 0) {
      const subjAny = this.unprocessedAppointmentsListSubject as any;
      const windowStartFn = subjAny.getWindowStart;
      const windowStart =
        typeof windowStartFn === 'function' ? windowStartFn.call(subjAny) : 0;
      return Math.min(windowStart + 1, total);
    }
    const skip = this.gridState?.skip || 0;
    return Math.min(skip + 1, total);
  }

  getPageEnd(total: number): number {
    if (!total) return 0;
    if (this.gridState && this.gridState.take === 0) {
      const subjAny = this.unprocessedAppointmentsListSubject as any;
      const dataLengthFn = subjAny.getDataLength;
      const loaded =
        typeof dataLengthFn === 'function' ? dataLengthFn.call(subjAny) : 0;
      return Math.min(loaded, total);
    }
    const skip = this.gridState?.skip || 0;
    const take = this.gridState?.take || 0;
    if (take === 0) return total;
    return Math.min(skip + take, total);
  }

  breadcrumbs: Breadcrumb[] = [
    {
      label: 'Reporting',
      url: '/billing/reporting/list',
      tabIndex: 0,
      isReportsPage: true,
    },
    {
      label: 'Appointment Reports',
      url: '/billing/reporting/list',
      tabIndex: 2,
      isReportsPage: true,
    },
    {
      label: 'Claim Creation Failed',
      url: '/billing/reporting/unprocessed-appointments',
      tabIndex: 2,
      isReportsPage: true,
    },
  ];
}
