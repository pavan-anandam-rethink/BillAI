import {
  ChangeDetectorRef,
  Component,
  EventEmitter,
  Input,
  NgZone,
  OnDestroy,
  Output,
  ViewChild,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import {
  ClaimListFilterSort,
  ClaimPosting,
  PatientServiceLines,
  PaymentPostingShortInfo,
  PaymentSummary,
  PostRemovePatientClaims,
} from '@core/models/billing/';
import {
  ConfirmDialog,
  GridFilterModel,
  NotifyDialog,
} from '@core/models/common';
import {
  ClaimNotesService,
  ClaimPostingService,
  ClaimService,
  PaymentPostingService,
} from '@core/services/billing';
import {
  GridComponent,
  GridDataResult,
  PageChangeEvent,
  PagerSettings,
} from '@progress/kendo-angular-grid';
import { SortDescriptor } from '@progress/kendo-data-query';
import { Observable, Subject } from 'rxjs';
import { map, take, takeUntil, tap } from 'rxjs/operators';
import { PaymentPostingAdjustmentsDetailsComponent } from '@app/billing/payment-posting';
import { SidebarService } from '@app/shared/components/sidebar';
import { ManualPaymentDetailsInfoComponent } from '@app/billing/payment-posting/payment-posting-view/manual-posting-patient/manual-payment-details-info/manual-payment-details-info.component';
import { PatientDetailsComponent } from '../../payment-details/patient-details';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { BulkPostingCriteria } from '@core/enums/billing';
import { AccountPermissions } from '@core/enums/account';
import { PatientClaimPostingListSubject } from '@core/subjects/patient-claim-posting-list.subject';
import { PaymentDetailsInfoComponent } from '../../payment-details-info/payment-details-info.component';
import {
  AddClaimNotesDialogComponent,
  AddClaimNotesDialogResult,
} from '@app/billing/encounters/ecnounter-list/add-claim-notes-dialog/add-claim-notes-dialog.component';
import {
  ClaimNoteModel,
  ClaimNotesSaveModel,
} from '@core/models/billing/notes/cliam-posting-note';
import {
  DialogService,
  DialogCloseResult,
} from '@progress/kendo-angular-dialog';
import { PatientClaimDetailsComponent } from '../patient-claim-details/patient-claim-details.component';
import { GridFilterOperators } from '@core/enums/common';
import { PatientClaimDetailsUnlinkComponent } from '../patient-claim-details-unlinked/patient-claim-details-unlink.component';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { UnAllocatedPaymentsModel } from '@core/models/billing/payment-posting';
import { EditUnallocatedResult } from '../unallocated-payment-posting-view/edit-unallocated-dialog.component';

@Component({
  selector: 'manual-payment-patient-details',
  templateUrl: './manual-payment-patient-details.component.html',
  styleUrls: ['./manual-payment-patient-details.component.css'],
})
export class ManualPaymentPatientDetailsComponent implements OnDestroy {
  @Input() postPaymentLinesEvent: Observable<void>;
  @Input() paymentShortInfo: PaymentPostingShortInfo;
  @Output() zeroOpenClaimsNotify = new EventEmitter();
  @Output() updateStatus = new EventEmitter<number>();
  @Output() postBtnDisabled = new EventEmitter<boolean>();
  deleteBtnDisabled = true;

  private unsubscribe = new Subject<void>();
  public isSubjectLoading$: Observable<boolean>;
  private unsubscribeAll$ = new Subject();

  public notifyDialog = new NotifyDialog(false, '', '');
  showAddPatientDialog = false;
  public showEditUnallocatedDialog = false;

  public editDialogModel: {
    patientId?: number;
    patientName?: string;
    currentUnallocated?: number;
    note?: string;
    rowVersion?: any;
    accountInfoId?: number;
    memberId?: number;
  } | null = null;

  @ViewChild(GridComponent) patientsGrid: GridComponent;
  @ViewChild('paymentPatientSummary')
  paymentPatientSummaryComponent: ManualPaymentDetailsInfoComponent;
  @ViewChild('paymentOtherSummary')
  paymentOtherSummaryComponent: PaymentDetailsInfoComponent;
  @ViewChild('paymentInsuranceSummary')
  paymentInsuranceSummaryComponent: PaymentDetailsInfoComponent;
  @ViewChild(ManualPaymentDetailsInfoComponent)
  manualPaymentDetailsInfoComponent: ManualPaymentDetailsInfoComponent;
  @ViewChild(PatientClaimDetailsComponent)
  patientClaimDetailsComponent: PatientClaimDetailsComponent;
  @ViewChild(PatientClaimDetailsUnlinkComponent)
  patientClaimDetailsUnlinkComponent: PatientClaimDetailsUnlinkComponent;
  @ViewChild('manualPaymentGrid') manualPaymentGrid: GridComponent;

  PatientClaimPostingListSubject: PatientClaimPostingListSubject;
  view: Observable<GridDataResult>;
  viewData: ClaimPosting[];

  redraw = false;

  gridState: ClaimListFilterSort = new ClaimListFilterSort();

  readonly pagingSettings: PagerSettings = {
    buttonCount: 5,
    type: 'numeric',
    pageSizes: true,
    previousNext: true,
  };

  serviceBreakdown = false;
  payment: PaymentSummary;

  showFilter = false;
  showActions = false;
  totalCount: number;

  selectedPatients: number[] = [];
  selectedPatientWithLines: PatientServiceLines[] = [];
  canEdit: boolean;
  isRevSpringPayment: boolean | undefined = undefined;

  showHint = false;
  hintText: string;
  hintAnchor: HTMLAnchorElement;
  bulkCriteria = BulkPostingCriteria;
  selectedBulkCriteriaId = 0;

  gridPageSizes: any;

  constructor(
    private ngZone: NgZone,
    private route: ActivatedRoute,
    private cd: ChangeDetectorRef,
    private claimPostingService: ClaimPostingService,
    private sidebarService: SidebarService,
    private accountService: AccountMemberService,
    private notificationService: NotificationHandlerService,
    private dialogService: DialogService,
    private claimsService: ClaimService,
    private claimNotesService: ClaimNotesService,
    private paymentPostingService: PaymentPostingService
  ) {
    this.route.params.pipe(takeUntil(this.unsubscribe)).subscribe((x) => {
      if (x['id']) {
        const id = +x['id'];
        id && (this.gridState.paymentId = id);
        // Determine RevSpring by payment summary method
        if (id) {
          this.paymentPostingService.getSummaryById(id)
            .pipe(takeUntil(this.unsubscribe))
            .subscribe((summary) => {
              const method = (summary?.paymentMethod || '').toLowerCase();
              this.isRevSpringPayment = method === 'revspring';
            });
        }

        this.PatientClaimPostingListSubject =
          new PatientClaimPostingListSubject(this.claimPostingService);
        this.view = this.PatientClaimPostingListSubject.pipe(
          map(
            (data) => {
              return {
                data: data,
                total: this.PatientClaimPostingListSubject.totalCount,
              };
            },
            tap(() => setTimeout(() => this.fitColumns(), 250))
          )
        );
        this.loadData(this.gridState);
      }
    });
    this.accountService.accountMemberSettings.subscribe((x) => {
      if (x) {
        this.canEdit = this.accountService.checkPermissionLevel(
          AccountPermissions.BillingEditApprovedAppointments
        );
      }
    });

    this.getGridPageSizes();
  }

  onPageChange(event: PageChangeEvent) {
    this.gridState.skip = event.skip;
    this.gridState.take = event.take;

    if (this.gridState.take === 0)
      this.gridState.take = this.PatientClaimPostingListSubject.totalCount;
    this.loadData(this.gridState);
  }

  onSortChange(sortParams: SortDescriptor[]): void {
    this.gridState.sortingModels = sortParams;
    this.loadData(this.gridState);
  }

  showPaid: false;
  togglePaid(showPaid: boolean) {
    this.isShowPaidFilterApplied = true;
    this.PatientClaimPostingListSubject.setLoading();
    if (showPaid) {
      this.updateFilters({
        patientPayment: {
          value: 0,
          operator: GridFilterOperators.greaterThan,
        },
      });
    } else {
      this.updateFilters({});
    }
  }

  updateFilters(newFormVals: Object): void {
    this.gridState.filterModels = [];
    Object.keys(newFormVals).forEach((key) => {
      let formFilter = newFormVals[key];
      if (formFilter.value !== null && formFilter.value !== '') {
        let filterEl: GridFilterModel = new GridFilterModel(
          key,
          formFilter.operator,
          formFilter.value.toString()
        );
        this.gridState.filterModels.push(filterEl);
      }
    });
    if (this.showPaid) this.gridState.showPaid = true;
    else this.gridState.showPaid = false;

    this.loadData(this.gridState);
  }

  updateClaimDetails() {
    this.loadData(this.gridState);
    this.collapseAll();
  }

  loadPaymentShortInfo(e) {
    this.updateStatus.emit(this.paymentShortInfo.id);
  }

  isShowPaidFilterApplied = false;
  async loadData(params: ClaimListFilterSort) {
    this.PatientClaimPostingListSubject.setLoading();
    this.isSubjectLoading$ = this.PatientClaimPostingListSubject.getLoading();
    this.PatientClaimPostingListSubject.getAll(params);
    this._selectAllLines = false;
  }

  showClientInfo(patientId: number) {
    this.sidebarService
      .openRight(PatientDetailsComponent, true, 'md')
      .subscribe((rsidebarRef) => rsidebarRef.instance.setData(patientId));
  }

  isChecked(patientId: number): boolean {
    return this.selectedPatients.any((x: any) => x == patientId);
  }

  selectAllClaimLines(patientId: number): boolean {
    if (this.selectedPatients.any((x: number) => x == 0)) {
      return true;
    } else {
      return this.selectedPatients.any((x: number) => x == patientId);
    }
  }

  postPayments(): void {
    //post btn disabled
    this.deleteBtnDisabled = true;
    this.postBtnDisabled.emit(true);

    let selectedPatientWithLinesCopy = this.selectedPatientWithLines.slice();

    selectedPatientWithLinesCopy.forEach((x: any) => {
      x.selectedLines.forEach((sl: any, index: number) => {
        sl = {
          id: sl.id,
          claimId: sl.claimId,
        };

        x.selectedLines[index] = sl;
      });

      x.serviceLines = x.selectedLines;
      delete x.selectedLines;
    });

    if (this.selectedPatients.length != selectedPatientWithLinesCopy.length) {
      this.selectedPatients.forEach((patientId) => {
        let result = selectedPatientWithLinesCopy.find(
          (x) => x.patientId == patientId
        );
        if (result == undefined) {
          selectedPatientWithLinesCopy.push({
            patientId: patientId,
            serviceLines: [],
          });
        }
      });
    }

    let model: PostRemovePatientClaims = {
      paymentId: this.gridState.paymentId,
      patientServiceLines: selectedPatientWithLinesCopy,
      postingCriteriaId: this.selectedBulkCriteriaId,
      memberId: this.accountService.memberDetails.memberId,
      accountInfoId: this.accountService.memberDetails.accountInfoId,
    };

    this.claimPostingService
      .postManualPatientPayment(model)
      .pipe(takeUntil(this.unsubscribe))
      .subscribe(
        (result) => {
          this.selectedPatientWithLines = [];
          this.selectedPatients = [];
          this.postBtnDisabled.emit(true);

          this.updateStatus.emit(this.paymentShortInfo.id);
        },
        (error) => {
          this.deleteBtnDisabled = false;
          this.postBtnDisabled.emit(false);
        },
        () => {
          if (this.paymentShortInfo.isOtherType) {
            this.paymentOtherSummaryComponent.loadSummary();
          }
          if (this.paymentShortInfo.isInsuranceType) {
            this.paymentInsuranceSummaryComponent.loadSummary();
          } else {
            this.paymentPatientSummaryComponent.loadPaymentSummary(
              this.gridState.paymentId
            );
          }
          this.loadData(this.gridState);
          this.updateStatus.emit();
        }
      );
  }

  changeLineSelections(event: any, isLinked: boolean): void {
    if (event.serviceLines && event.serviceLines.length > 0) {
      this.deleteBtnDisabled = false;
      this.postBtnDisabled.emit(false);
    } else {
      this.deleteBtnDisabled = true;
      this.postBtnDisabled.emit(true);
    }

    let patientWithLinesArrEl = this.selectedPatientWithLines.find(
      (x) => x.patientId == event.patientId
    );

    if (patientWithLinesArrEl == undefined) {
      this.selectedPatientWithLines.push(event);
    } else {
      patientWithLinesArrEl.serviceLines =
        patientWithLinesArrEl.serviceLines.filter(
          (x) => x.isLinked == !isLinked
        );

      event.serviceLines.forEach((element) => {
        patientWithLinesArrEl.serviceLines.push(element);
      });
    }
  }
  isAllLinkedSelected = false;
  isAllUnlinkedSelected = false;
  changePatientSelectionForLinked(event: any): void {
    this.isAllLinkedSelected = event.event.currentTarget.checked;
    this.changePatientSelection(event);
  }
  changePatientSelectionForUnlinked(event: any): void {
    this.isAllUnlinkedSelected = event.event.currentTarget.checked;
    this.changePatientSelection(event);
  }

  changePatientSelection(event: any): void {
    this.selectionLinesChanged(event.event, event.patientId);
    // if (this.isAllLinkedSelected && this.isAllUnlinkedSelected) {
    //     this.selectionLinesChanged(event.event, event.patientId);
    // } else {
    //     let item = this.viewData.find(item => item.patientId == event.patientId);
    //     if (item !== undefined) {
    //         item.checked = false;
    //     }
    //     this._selectAllLines = false;
    // }
  }

  allLinesSelected(emitModel: any): void {
    this.selectionLinesChanged(emitModel.event, emitModel.claimId);
  }

  _selectAllLines: boolean;
  selectionLinesChanged(event: any, patientId: number): void {
    let isChecked = event.currentTarget.checked;

    if (isChecked) {
      if (patientId === 0) {
        this.selectedPatients = [];
        this.viewData.forEach((item) => {
          item.checked = isChecked;
          this.selectedPatients.push(item.patientId);
        });
        this._selectAllLines = true;
      } else {
        let item = this.viewData.find((item) => item.patientId == patientId);
        if (item !== undefined) {
          item.checked = isChecked;
        }
        this.selectedPatients.push(patientId);
        this.selectedPatients = this.selectedPatients.filter(
          (item, index) => this.selectedPatients.indexOf(item) === index
        );
        if (this.selectedPatients.length == this.viewData.length)
          this._selectAllLines = true;
      }
    } else {
      if (patientId === 0) {
        this.selectedPatients = [];
        this.viewData.forEach((item) => {
          item.checked = isChecked;
        });
        this.selectedPatientWithLines = [];
        this._selectAllLines = false;
      } else {
        let item = this.viewData.find((item) => item.patientId == patientId);
        if (item !== undefined) {
          item.checked = isChecked;
        }
        this.selectedPatients.remove(patientId);
        this._selectAllLines = false;
      }
    }

    this.selectedPatients = this.selectedPatients.filter(
      (test, index, array) =>
        index === array.findIndex((findTest) => findTest === test)
    );

    //post btn disabled
    if (this.selectedPatients.length > 0) {
      this.deleteBtnDisabled = false;
      this.postBtnDisabled.emit(false);
    } else {
      this.deleteBtnDisabled = true;
      this.postBtnDisabled.emit(true);
    }
    this.selectedPatients = this.selectedPatients.filter(
      (item, index) => this.selectedPatients.indexOf(item) === index
    );
    this.PatientClaimPostingListSubject.next(this.viewData);
  }

  fitColumns(): void {
    this.ngZone.onStable
      .asObservable()
      .pipe(take(1))
      .subscribe(() => {
        this.patientsGrid.autoFitColumns(
          this.patientsGrid.columnList.toArray()
        );
        this.patientsGrid.autoFitColumns();
      });
  }

  ngOnDestroy() {
    //unsubscribe from all here!
    this.PatientClaimPostingListSubject.unsubscribe();
    this.unsubscribe.next();
    this.unsubscribe.complete();
  }

  ngAfterViewInit() {
    this.fitColumns();
  }

  ngOnInit() {
    this.postPaymentLinesEvent
      .pipe(takeUntil(this.unsubscribe))
      .subscribe(() => this.postPayments());
    this.view.pipe(takeUntil(this.unsubscribe)).subscribe((result) => {
      this.viewData = result.data;
      this.totalCount = result.total;
      this.viewData.forEach((item) => {
        item.checked = this.selectedPatients.includes(item.patientId);
        this._selectAllLines =
          this.viewData.length > 0 &&
          this.viewData.every((item) => item.checked)
            ? true
            : false;
      });
    });

    this.sidebarService.adjustmentChanged$
      .pipe(takeUntil(this.unsubscribe))
      .subscribe((data: number) => {
        this.sidebarService.closeAll();
        this.updateSummary(data);
        this.loadData(this.gridState);
        this.updateStatus.emit(this.paymentShortInfo.id);
      });
  }

  collapseAll() {
    var i = 0;
    if (this.viewData) {
      this.viewData.forEach((x) => {
        this.manualPaymentGrid.collapseRow(i);
        i++;
      });
    }
    this.isShowPaidFilterApplied = false;
  }

  /*--------actions----------*/
  addPatientDialogToggle(model: any = null) {
    this.showAddPatientDialog = !this.showAddPatientDialog;
    if (model == null) {
      return;
    }

    // Check if PatientIds already exist in the grid
    let existingPatientIds = this.viewData.map((x) => x.patientId);
    let newPatientIds = model.patientIds.filter(
      (id: number) => !existingPatientIds.includes(id)
    );
    model.patientIds = newPatientIds;

    if (model.patientIds.length == 0) {
      this.notificationService.showNotificationWarning(
        'This client has already been added. Duplicate entries are not allowed.'
      );
      return;
    }

    // Add Patients to Payment
    this.claimPostingService
      .createPaymentPatientClaims(model)
      .pipe(takeUntil(this.unsubscribeAll$))
      .subscribe((result) => {
        this.loadData(this.gridState);
        var createdClaimsCount = result;

        var successPatients = createdClaimsCount.filter(
          (x) => x.isAttached == true
        );
        var failedPatients = createdClaimsCount.filter(
          (x) => x.isAttached == false
        );

        if (!this.showAddPatientDialog && successPatients.length > 0) {
          this.notificationService.showNotificationSuccess(
            successPatients.length + ' Patient(s) Added Successfully'
          );
        }

        if (!this.showAddPatientDialog && failedPatients.length > 0) {
          //var stringMsg = "No Patient Responsibility is present for below patient(s)\n";
          failedPatients.forEach((element) => {
            //stringMsg = stringMsg + element.patientName + "\n";
            var data = document.getElementById('depositeDate');
            this.notificationService.showNotificationError(
              "No Patient Responsibility found for '" +
                element.patientName +
                "' on or before Deposit Date " +
                data.innerText
            );
          });
          // setTimeout(() => {
          //     this.notifyDialog.message = stringMsg;
          //     this.notifyDialog.opened = true;
          // });
        }
      });
  }

  public confirmDeleteDialog: ConfirmDialog = new ConfirmDialog(
    false,
    'Please confirm',
    'This will make the payment 0 for selected charges. Do you want to proceed?',
    'Delete',
    'Cancel'
  );
  public confirmDeletePatientDialog: ConfirmDialog = new ConfirmDialog(
    false,
    'Please confirm',
    'Do you want to delete the patient from the payment?',
    'Delete',
    'Cancel'
  );
  claimForDelete: ClaimPosting;

  openConfirmDialog(): void {
    this.confirmDeleteDialog.opened = true;
  }

  patientToDelete: number = 0;

  openConfirmPatientDialog(patientId: number) {
    this.patientToDelete = patientId;
    this.confirmDeletePatientDialog.opened = true;
  }

  onDeletePatient(event: any) {
    if (this.patientToDelete == 0) {
      return;
    }
    let obj: PatientServiceLines[] = [];
    obj.push({ patientId: this.patientToDelete, serviceLines: [] });
    let model: PostRemovePatientClaims = {
      paymentId: this.gridState.paymentId,
      patientServiceLines: obj,
      postingCriteriaId: 0,
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId,
    };

    this._selectAllLines = false;
    // this.patientClaimDetailsComponent._selectAllLines = false;
    this.claimPostingService
      .deleteSelectedPatients(model)
      .pipe(takeUntil(this.unsubscribe))
      .subscribe((result) => {
        if (this.paymentShortInfo.isOtherType) {
          this.paymentOtherSummaryComponent.loadSummary();
        }
        if (this.paymentShortInfo.isInsuranceType) {
          this.paymentInsuranceSummaryComponent.loadSummary();
        } else {
          this.paymentPatientSummaryComponent.loadPaymentSummary(
            this.gridState.paymentId
          );
        }
        this.loadData(this.gridState);

        this.deleteBtnDisabled = false;
        this.postBtnDisabled.emit(false);
        this.notificationService.showNotificationSuccess(
          'Patient deleted successfully.'
        );
      });
  }

  onDeletePayments(status: boolean) {
    if (
      !status ||
      (this.selectedPatients.length == 0 &&
        this.selectedPatientWithLines.length == 0)
    ) {
      return;
    }

    //delete btn disabled
    this.deleteBtnDisabled = true;
    this.postBtnDisabled.emit(true);

    let selectedPatientWithLinesCopy = this.selectedPatientWithLines.slice();
    if (this.selectedPatients.length != selectedPatientWithLinesCopy.length) {
      this.selectedPatients.forEach((patientId) => {
        let result = selectedPatientWithLinesCopy.find(
          (x) => x.patientId == patientId
        );
        if (result == undefined) {
          selectedPatientWithLinesCopy.push({
            patientId: patientId,
            serviceLines: [],
          });
        }
      });
    }

    let model: PostRemovePatientClaims = {
      paymentId: this.gridState.paymentId,
      patientServiceLines: selectedPatientWithLinesCopy,
      postingCriteriaId: 0,
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId,
    };

    this.claimPostingService
      .deleteSelectedPaymentAmounts(model)
      .pipe(takeUntil(this.unsubscribe))
      .subscribe((result) => {
        if (this.paymentShortInfo.isOtherType) {
          this.paymentOtherSummaryComponent.loadSummary();
        }
        if (this.paymentShortInfo.isInsuranceType) {
          this.paymentInsuranceSummaryComponent.loadSummary();
        } else {
          this.paymentPatientSummaryComponent.loadPaymentSummary(
            this.gridState.paymentId
          );
        }
        this.updateClaimDetails();
        this.deleteBtnDisabled = false;
        this.postBtnDisabled.emit(false);
        let msgString = 'Payment amount for ';
        if (this.selectedPatients.length > 0)
          msgString += this.selectedPatients.length + ' Patient(s)';
        let chargeCount = 0;
        this.selectedPatientWithLines.forEach((ele) => {
          chargeCount = chargeCount + ele.serviceLines.length;
        });
        if (chargeCount > 0) {
          msgString +=
            (this.selectedPatients.length > 0 ? ' & ' : '') +
            chargeCount +
            ' Charge(s)';
        }
        msgString += ' deleted successfully.';
        this.notificationService.showNotificationSuccess(msgString);
        //SUCCESS message
      });
  }

  /*deletePayment() {
        this.PatientClaimPostingListSubject.delete(this.claimForDelete);
        return false;
    }*/

  rebillBulk() {
    if (this.mySelection.length > 0) {
      this.claimPostingService.rebill(this.mySelection).subscribe((result) => {
        const processed = result.processed;
        const rejected = result.rejected;
        this.notifyDialog.message = `Claims with ids: ${processed.join(
          ', '
        )} are processed. Claims with ids: ${rejected.join(
          ', '
        )} are rejected.`;
        this.notifyDialog.opened = true;
      });
    }
  }

  /*--------#actions----------*/

  /*--------multi select--------*/

  public mySelection: number[] = [];
  /*--------#multi select--------*/

  /*--------#adjustments sidebar--------*/
  adjustmentClick(serviceLineid: number, patientId: number) {
    this.sidebarService
      .openRight(PaymentPostingAdjustmentsDetailsComponent, true, 'md', true)
      .subscribe((rsidebarRef) => {
        rsidebarRef.instance.setData(serviceLineid, patientId, 0, true);
        rsidebarRef.instance.onClose.subscribe((x) => {
          this.sidebarService.rightSidebarComponentRef.instance.close();
        });
      });
  }
  updateClaim(data: any) {
    this.patientClaimDetailsComponent.loadData(
      this.patientClaimDetailsComponent.gridState
    );
    this.patientClaimDetailsUnlinkComponent.loadData(
      this.patientClaimDetailsUnlinkComponent.gridState
    );
    if (this.paymentShortInfo.isInsuranceType) {
      this.PatientClaimPostingListSubject.updateClaim(data);
    }
    this.updateSummary();
    this.updateStatus.emit(this.paymentShortInfo.id);
  }

  bulkFocused(event: any, hintText: string): void {
    this.hintText = hintText;
    this.hintAnchor = event.currentTarget;
    this.showHint = true;
  }

  bulkUnfocused(event: any): void {
    this.showHint = false;
  }

  bulkClicked(criteriaId: number) {
    if (this.selectedBulkCriteriaId == criteriaId) {
      this.selectedBulkCriteriaId = 0;
    } else {
      this.selectedBulkCriteriaId = criteriaId;
    }
  }

  addNoteBulk() {
    if (
      this.selectedPatients.length == 0 &&
      this.selectedPatientWithLines.length == 0
    ) {
      this.notificationService.showNotificationWarning(
        'Please select the claim(s).'
      );
      return;
    }
    //const claimIds = this.mySelection.filter((claimId) => !!claimId);
    let claimIds: number[] = [];
    let selectedPatientWithLinesCopy = this.selectedPatientWithLines.slice();
    selectedPatientWithLinesCopy.forEach((x: any, index: number) => {
      x.serviceLines.forEach((sl: any) => {
        claimIds.push(sl.claimId);
      });
    });

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
          this.claimNotesService.addToSeveral(claimNotesModel).subscribe(() => {
            this.loadData(this.gridState);
            this.notificationService.showNotificationSuccess(
              'Note Added successfully.'
            );
          });
        }
      }
    );
  }

  updateSummary(patientId = 0) {
    this.manualPaymentDetailsInfoComponent.loadPaymentSummary();
    if (patientId != 0) {
      this.patientClaimDetailsComponent.paymentId = this.gridState.paymentId;
      this.patientClaimDetailsComponent.patientId = patientId;
      this.patientClaimDetailsUnlinkComponent.paymentId =
        this.gridState.paymentId;
      this.patientClaimDetailsUnlinkComponent.patientId = patientId;
    }
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

    if (
      !this.gridState ||
      this.gridState.take === 0 ||
      this.gridState.take === 9999999
    ) {
      const getWindowStart = (this.PatientClaimPostingListSubject as any)
        ?.getWindowStart;
      if (typeof getWindowStart === 'function') {
        const ws = (
          this.PatientClaimPostingListSubject as any
        ).getWindowStart();
        return (ws || 0) + 1;
      }
      const getDataLength = (this.PatientClaimPostingListSubject as any)
        ?.getDataLength;
      const len =
        typeof getDataLength === 'function'
          ? (this.PatientClaimPostingListSubject as any).getDataLength()
          : 0;
      return len > 0 ? 1 : 0;
    }

    const start = (this.gridState.skip || 0) + 1;
    return Math.min(start, total);
  }

  getPageEnd(total: number): number {
    if (!total) {
      return 0;
    }

    if (
      !this.gridState ||
      this.gridState.take === 0 ||
      this.gridState.take === 9999999
    ) {
      const getDataLength = (this.PatientClaimPostingListSubject as any)
        ?.getDataLength;
      const len =
        typeof getDataLength === 'function'
          ? (this.PatientClaimPostingListSubject as any).getDataLength()
          : 0;
      return Math.min(len || 0, total);
    }

    const end = Math.min(
      (this.gridState.skip || 0) + (this.gridState.take || 0),
      total
    );
    return end;
  }

  openEditUnallocatedDialog(dataItem: any, event?: Event) {
    event?.stopPropagation();
    if (!this.canEdit || this.isRevSpringPayment) return;

    const requestModel: UnAllocatedPaymentsModel = {
      PaymentId: this.gridState.paymentId,
      ChildProfileId: dataItem.patientId,
      MemberId: this.accountService.memberDetails.memberId,
      AccountInfoId: this.accountService.memberDetails.accountInfoId,
      UnAllocatedAmount: 0,
    };

    this.paymentPostingService
      .getUnAllocatedPayments(requestModel)
      .pipe(takeUntil(this.unsubscribe))
      .subscribe({
        next: (res: any) => {
          // Use backend data if found, otherwise fallback to grid values
          const latestUnallocated = Number(
            res?.UnAllocatedAmount ??
              res?.unAllocatedAmount ??
              dataItem.unallocatedPayment ??
              0
          );

          //Prepare model to pass to dialog
          this.editDialogModel = {
            patientId: dataItem.patientId,
            patientName: dataItem.patientName,
            currentUnallocated: latestUnallocated,
            rowVersion: res?.rowVersion || dataItem.rowVersion || null,
            accountInfoId: this.accountService.memberDetails.accountInfoId,
            memberId: this.accountService.memberDetails.memberId,
            note:
              res?.Notes || res?.notes || dataItem.notes || dataItem.note || '',
          };
          this.showEditUnallocatedDialog = true;
        },
        error: (err: any) => {
          console.error('Error fetching latest unallocated payments:', err);
          this.notificationService.showNotificationWarning(
            'Unable to fetch unallocated payment details. Opening dialog with existing grid data.'
          );

          // fallback — use grid data only
          this.editDialogModel = {
            patientId: dataItem.patientId,
            patientName: dataItem.patientName,
            currentUnallocated: dataItem.unallocatedPayment ?? 0,
            note: dataItem.notes || dataItem.note || '',
            rowVersion: dataItem.rowVersion || null,
            accountInfoId: this.accountService.memberDetails.accountInfoId,
            memberId: this.accountService.memberDetails.memberId,
          };
          this.showEditUnallocatedDialog = true;
        },
      });
  }

  onEditUnallocatedClose(event: any) {
    this.showEditUnallocatedDialog = false;

    if (!event) {
      // cancelled — nothing to do
      this.editDialogModel = null;
      return;
    }

    this.editDialogModel = null;
    this.applyUnallocatedUpdate({
      patientId: event.patientId,
      unallocatedAmount: event.unallocatedAmount,
      note: event.note,
      rowVersion: event.rowVersion,
    });
  }

  applyUnallocatedUpdate(result: EditUnallocatedResult) {
    const paymentId = this.gridState.paymentId; // number
    const patientId = result.patientId; // ChildProfileId
    const unallocatedAmount = result.unallocatedAmount;
    const notes = result.note ?? '';

    // Build the model matching backend
    const model: UnAllocatedPaymentsModel = {
      AccountInfoId: this.accountService.memberDetails.accountInfoId,
      PaymentId: paymentId,
      ChildProfileId: patientId,
      UnAllocatedAmount: unallocatedAmount,
      Notes: notes,
      MemberId: this.accountService.memberDetails.memberId,
    };

    this.paymentPostingService
      .addUnAllocatedPayment(model)
      .pipe(
        tap(() =>
          this.paymentPatientSummaryComponent.loadPaymentSummary(paymentId)
        ),
        tap(() =>
          this.notificationService.showNotificationSuccess(
            'Unallocated payment saved successfully.'
          )
        ),
        tap(() => this.loadData(this.gridState)), // <- one place to refresh the grid
        takeUntil(this.unsubscribe)
      )
      .subscribe({
        error: (err) => {
          const msg =
            err?.error?.message ?? 'Failed to save unallocated payment.';
          this.notificationService.showNotificationError(msg);
        },
      });
  }
}
