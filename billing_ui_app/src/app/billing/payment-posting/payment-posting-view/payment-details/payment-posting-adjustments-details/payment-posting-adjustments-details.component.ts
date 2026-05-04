import { Component, ElementRef, EventEmitter, Input, OnDestroy, Output, ViewChild } from "@angular/core";
import { BehaviorSubject, forkJoin, Observable, of, Subject } from "rxjs";
import { AbstractControl, FormArray, FormControl, FormGroup } from "@angular/forms";
import { Adjustment } from "@core/models/billing/adjustment";
import { catchError, filter, map, switchMap, take, takeUntil } from "rxjs/operators";
import { ClaimPostingService, AdjustmentService, PaymentPostingService } from "@core/services/billing";
import { PaymentClaimServiceLine } from "@core/models/billing/payment-claim-service-line";
import { ConfirmDialog, NotifyDialog } from "@core/models/common";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { SidebarService } from "@app/shared/components/sidebar";
import { AccountPermissions } from "@core/enums/account";
import { ConfirmationDialogComponent } from "@app/shared/components/confirmation-dialog/confirmation-dialog.component";
import { EditWriteOffModelWithUserInfo, WriteOffChargeEntryModel, WriteOffDetailsModel, WriteOffReasonCodDescriptionModel } from "@core/models/billing/write-off-charge-entry-model";
import { WriteoffService } from "@core/services/billing/writeoff.service";
import { AddOrEditAdjustmentModelWithUserInfo, AdjustmentDetailsModel } from "@core/models/billing/add-or-edit-adjustment-model";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";
import { AdjustmentReasonCodes } from "@core/models/billing/adjustmentReasonCodes";

@Component({
  selector: 'payment-posting-adjustments-details',
  templateUrl: './payment-posting-adjustments-details.html',
  styleUrls: ['./payment-posting-adjustments-details.css'],
})
export class PaymentPostingAdjustmentsDetailsComponent implements OnDestroy {
  @ViewChild('addAdjustmentForm') addAdjustmentFormRef: ElementRef;
  @ViewChild(ConfirmationDialogComponent)
  confirmDialog: ConfirmationDialogComponent;
  @Output() onUpdate = new EventEmitter<number>();
  @Output() onClose = new EventEmitter();
  private unsubscribe = new Subject<void>();

  updateId: number;
  serviceLineId: number;
  patientId: number;
  claimId: number;
  serviceLine = new PaymentClaimServiceLine();

  allowedAmountOrig: number;
  paidAmountOrig: number;

  adjustments: Adjustment[];
  adjustmentsOrig: Adjustment[] = [];
  allowedReasonCodesWithDescription: AdjustmentReasonCodes[];
  addAdjustmentFormVisible = false;
  adjustmentAmount: number;
  isPositive = false;
  isServiceLineEdit = false;
  isReconcilePayment = false;
  claimStatus: string | null = null;
  paymentPostingId: number;
  reasonInvalid = false;
  defaultReason = 'Select One';

  editedAdjustmentIds = new Set<number>();
  deletedAdjustmentIds = new Set<number>();
  deletedWriteoffIds = new Set<number>();
  editedWriteoffIds = new Set<number>();

  allowedReasonCodes: string[] = [];
  newReasonCodes: string[] = [];

  deleteConfirmation = new ConfirmDialog(
    false,
    'Confirmation',
    'Are you sure you want to delete this adjustment?'
  );
  cancelConfirmation = new ConfirmDialog(
    false,
    'Confirmation',
    'You have unsaved changes. Are you sure you want to leave and discard all unsaved changes?'
  );
  adjustmentToDeleteId: number;

  newAdjustmentDescription: string;

  adjustmentForm = new FormGroup({
    adjustments: new FormArray([
      new FormGroup({
        amount: new FormControl(''),
        isPositive: new FormControl(false),
        reasonCode: new FormControl('Select One', [this.reasonCodeValidator]),
      }),
    ]),
  });

  isManual = false;
  canEdit: boolean;

  writeOffsOrig: WriteOffChargeEntryModel[] = [];
  writeOffs: WriteOffChargeEntryModel[] = [];
  allowedWriteOffReasonCodes: WriteOffReasonCodDescriptionModel[] = [];
  deleteConfirmationWriteOff = new ConfirmDialog(
    false,
    'Confirmation',
    'Are you sure you want to delete this write off?'
  );
  writeOffToDeleteId: number;
  isPatient: boolean;
  // BehaviorSubject to hold reason codes (null until loaded)
  private reasonCodes$ = new BehaviorSubject<AdjustmentReasonCodes[] | null>(null);

  constructor(
    private sidebarService: SidebarService,
    private claimPostingService: ClaimPostingService,
    private adjustmentService: AdjustmentService,
    private notificationService: NotificationHandlerService,
    private accountService: AccountMemberService,
    private writeOffService: WriteoffService,
    private paymentPostingService: PaymentPostingService
  ) {
    this.accountService.accountMemberSettings.subscribe((x) => {
      if (x) {
        this.canEdit = this.accountService.checkPermissionLevel(
          AccountPermissions.BillingEditApprovedAppointments
        );
      }
    });
  }

  get adjustmentFormArray(): FormArray {
    return this.adjustmentForm.get('adjustments') as FormArray;
  }

  afterUpdate() {
    this.sidebarService.dataChanged(this.updateId);
  }

  addAdjustmentField() {
    if (!this.addAdjustmentFormVisible) {
      this.addAdjustmentFormVisible = true;
      this.adjustmentForm = new FormGroup({
        adjustments: new FormArray([
          new FormGroup({
            amount: new FormControl<string>(''),
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
          amount: new FormControl<string>(''),
          isPositive: new FormControl(false),
          reasonCode: new FormControl('Select One', [this.reasonCodeValidator]),
        })
      );
    }
  }

  applyAsPositive() {
    this.isPositive = !this.isPositive;
  }

  setData(
    serviceLineId: number,
    updateId: number = 0,
    claimId = 0,
    isManual = false,
    isPatient = true,
    claimStatus?: string,
    paymentPostingId?: number,
  ) {
    this.isManual = isManual;
    this.serviceLineId = serviceLineId;
    this.claimId = claimId;
    this.updateId = updateId;
    this.isPatient = isPatient;
    this.claimStatus = claimStatus ?? null;
    this.paymentPostingId = paymentPostingId;
    this.updateServiceLine();


  // Wait until reason-codes are available (either already loaded or when they arrive),
   // then fetch adjustments and map descriptions.
  this.reasonCodes$
     .pipe(
        filter((rc) => rc !== null), // wait until not null
        take(1),
        switchMap(() => this.adjustmentService.getServiceLineAdjustments(serviceLineId)),
        takeUntil(this.unsubscribe)
        )
      .subscribe((x) => {
        this.adjustments = x;
        this.adjustments.forEach((adj) => {
          const codeKey = `${adj.groupCode}-${adj.reasonCode}`;
          adj.reasonCodeKey = codeKey;
          if (!this.allowedReasonCodes.includes(codeKey)) {
            this.allowedReasonCodes.push(codeKey);
          }
          const matched = this.allowedReasonCodesWithDescription?.find(
            (item) => item.reasonCode === codeKey
          );
          adj.description = matched?.description ?? `${adj.groupCode}:${adj.reasonCode}`;
        });
      });

    // load write-offs in parallel (doesn't depend on reason-codes)
    this.loadWriteOffs(serviceLineId);


  }

  loadWriteOffs(serviceLineId: number) {
    this.writeOffService
      .getChargeEntryWriteOffsByChargeId(serviceLineId, true)
      .pipe(takeUntil(this.unsubscribe))
      .subscribe((x) => {
        this.writeOffs = x;
      });
  }

  updateServiceLine() {
    this.claimPostingService
      .getPaymentClaimServiceLine(this.serviceLineId)
      .pipe(takeUntil(this.unsubscribe))
      .subscribe((x) => {
        this.serviceLine = x;
        this.paidAmountOrig = x.serviceLinePaymentAmount;
        this.allowedAmountOrig = x.allowedAmount;
        // OPTIMIZATION: Don't emit onUpdate when just loading data - only emit after actual save
        // this.onUpdate.emit(this.serviceLineId);
      });
  }

  Save() {
    const apiCalls: Observable<any>[] = [];

    // Initialize variables
    let addAdjustmentsModel: AddOrEditAdjustmentModelWithUserInfo | null = null;
    let editAdjustmentsModel: AddOrEditAdjustmentModelWithUserInfo | null =
      null;
    let deletedAdjustmentIds: number[] = [];
    let editWriteOffsModel: EditWriteOffModelWithUserInfo | null = null;
    let deletedWriteOffIds: number[] = [];

    // Process update payment amounts
    if (this.isServiceLineEdit) {
      apiCalls.push(
        this.claimPostingService.updatePaymentClaimServiceLineAmounts(
          this.serviceLine.id,
          this.serviceLine.allowedAmount,
          this.serviceLine.serviceLinePaymentAmount,
          this.isManual
        )
      );
      if (!this.isPatient && this.claimStatus === 'Billed' && this.serviceLine.serviceLinePaymentAmount! > 0  ) {
        this.isReconcilePayment = true;
      }
    }
    // Process new adjustments
    if (this.adjustmentForm.value.adjustments?.length > 0) {
      let formValues = this.adjustmentForm.value.adjustments;
      if (formValues && Array.isArray(formValues)) {
        const reasonCodes = formValues
          .map(item => item.reasonCode)
          .filter((code): code is string => !!code); 
        this.newReasonCodes.push(...reasonCodes);
      }
      let newAdjustments = formValues
        .map((formValue: any) => {
          if (this.newReasonCodes.indexOf(formValue.reasonCode) === -1) {
            return null;
          }
          let splittedReasonCode = formValue.reasonCode.split('-');
          let addAdjustmentModel = new AdjustmentDetailsModel();
          addAdjustmentModel.groupCode = splittedReasonCode[0];
          addAdjustmentModel.reasonCode = splittedReasonCode[1];
          addAdjustmentModel.amount = parseFloat(formValue.amount);
          addAdjustmentModel.isPositive = formValue.isPositive ?? false;
          return addAdjustmentModel;
        })
        .filter((adjustment: any) => adjustment !== null);
      if (newAdjustments.filter(x => x.amount != null && x.reasonCode != null).length > 0) {
        addAdjustmentsModel = {
          serviceLineId: this.serviceLineId,
          claimId: this.claimId,
          adjustmentDetails: newAdjustments,
          AccountInfoId: 0,
          MemberId: 0,
        };
        apiCalls.push(
          this.adjustmentService.addPaymentServiceLineAdjustments(
            addAdjustmentsModel
          )
        );
      }
    }
    // Process edited adjustments
    if (this.editedAdjustmentIds.size > 0) {
      let editedIds = [...this.editedAdjustmentIds].filter(
        (x) => !this.deletedAdjustmentIds.has(x)
      );
      let editedAdjustments: AdjustmentDetailsModel[] = [];
      if (editedIds.length > 0) {
        editedAdjustments = editedIds
          .map((id) => {
            let adjustment = this.adjustments.find((adj) => adj.id === id);
            if (!adjustment) return null;
            let model = new AdjustmentDetailsModel();
            model.adjustmentId = adjustment.id;
            model.groupCode = adjustment.groupCode;
            model.reasonCode = adjustment.reasonCode;
            model.amount = adjustment.amount;
            model.isPositive = adjustment.isPositive;
            return model;
          })
          .filter((model) => model !== null);

        if (editedAdjustments.length > 0) {
          editAdjustmentsModel = {
            serviceLineId: this.serviceLineId,
            claimId: this.claimId,
            adjustmentDetails: editedAdjustments,
            AccountInfoId: 0,
            MemberId: 0,
          };
          apiCalls.push(
            this.adjustmentService.updateServiceLineAdjustments(
              editAdjustmentsModel
            )
          );
        }
      }
    }

    // Process deleted adjustments
    deletedAdjustmentIds = Array.from(this.deletedAdjustmentIds);
    if (deletedAdjustmentIds.length > 0) {
      apiCalls.push(
        this.adjustmentService.deleteServiceLineAdjustments(
          deletedAdjustmentIds
        )
      );
    }

    // Process edited writeOffs
    if (this.editedWriteoffIds.size > 0) {
      let editedIds = [...this.editedWriteoffIds].filter(
        (x) => !this.deletedWriteoffIds.has(x)
      );
      let editedWriteOffs: WriteOffDetailsModel[] = [];
      if (editedIds.length > 0) {
        editedWriteOffs = editedIds
          .map((id) => {
            let writeOff = this.writeOffs.find((wo) => wo.id === id);
            if (!writeOff) return null;
            let model = new WriteOffDetailsModel();
            model.chargeEntryWriteOffId = writeOff.id;
            model.writeOffAmount = writeOff.writeOffAmount;
            model.writeOffReasonCodeId = writeOff.writeOffReasonCodeId;
            return model;
          })
          .filter((model) => model !== null);

        if (editedWriteOffs.length > 0) {
          editWriteOffsModel = {
            claimId: this.claimId,
            writeOffDetails: editedWriteOffs,
            AccountInfoId: 0,
            MemberId: 0,
          };
          apiCalls.push(
            this.writeOffService.updateChargeEntryWriteOff(editWriteOffsModel)
          );
        }
      }
    }

    // Process deleted writeOffs
    deletedWriteOffIds = Array.from(this.deletedWriteoffIds);
    if (deletedWriteOffIds.length > 0) {
      apiCalls.push(
        this.writeOffService.deleteChargeEntryWriteOff(deletedWriteOffIds)
      );
    }

    // Execute API calls if any exist
    if (apiCalls.length > 0) {
      // Wrap each API call with map for success and catchError for failure
      const safeApiCalls = apiCalls.map((apiCall) =>
        apiCall.pipe(
          map((response) => ({ success: true, data: response })),
          catchError((error) => {
            console.error('API call error caught:', error);
            return of({ success: false, error });
          })
        )
      );
      const startTime = performance.now();
      forkJoin(safeApiCalls).subscribe((results) => {
        let index = 0;
        const endTime = performance.now();

        const finalResult: any = {};
        if (this.isServiceLineEdit) {
          finalResult.updatePaymentAmountsResult = results[index++];
          this.isServiceLineEdit = false;
        }
        if (addAdjustmentsModel) {
          finalResult.addAdjustmentsResult = results[index++];
        }
        if (editAdjustmentsModel) {
          finalResult.updateAdjustmentsResult = results[index++];
        }
        if (deletedAdjustmentIds.length > 0) {
          finalResult.deleteAdjustmentsResult = results[index++];
        }
        if (editWriteOffsModel) {
          finalResult.updateWriteOffsResult = results[index++];
        }
        if (deletedWriteOffIds.length > 0) {
          finalResult.deleteWriteOffsResult = results[index++];
        }

        var hasError = false;
        // Show individual error messages
        if (finalResult.addAdjustmentsResult?.success === false) {
          this.notificationService.showNotificationError(
            'Failed to add adjustment(s).'
          );
          hasError = true;
        }
        if (finalResult.updateAdjustmentsResult?.success === false) {
          this.notificationService.showNotificationError(
            'Failed to update adjustment(s).'
          );
          hasError = true;
        }
        if (finalResult.deleteAdjustmentsResult?.success === false) {
          this.notificationService.showNotificationError(
            'Failed to delete adjustment(s).'
          );
          hasError = true;
        }
        if (finalResult.updateWriteOffsResult?.success === false) {
          this.notificationService.showNotificationError(
            'Failed to edit write-off(s).'
          );
          hasError = true;
        }
        if (finalResult.deleteWriteOffsResult?.success === false) {
          this.notificationService.showNotificationError(
            'Failed to delete write-off(s).'
          );
          hasError = true;
        }
        if (finalResult.updatePaymentAmountsResult?.success === false) {
          this.notificationService.showNotificationError(
            'Failed to add payment.'
          );
          hasError = true;
        }

        if (!hasError && this.isReconcilePayment) {
          this.paymentPostingService.reconcileClaimPayment([this.paymentPostingId], this.claimId).subscribe(
            () => {
              this.isReconcilePayment = false;
              this.isServiceLineEdit = false;
              this.onUpdate.emit(this.serviceLineId);
              this.afterUpdate();
              this.notificationService.showNotificationSuccess(
                'Payment service line updated successfully.'
              );
            },
            (error) => {
              this.isReconcilePayment = false;
              this.isServiceLineEdit = false;
              this.onUpdate.emit(this.serviceLineId)
              this.afterUpdate();
              this.notificationService.showNotificationError(
                'Payment updated but reconciliation failed.'
              );
            }
          );
          return; // Exit after starting reconcile, cleanup will happen in callbacks
        }

        this.isServiceLineEdit = false;
        // Removed: this.updateServiceLine() - causes duplicate getPaymentClaimServiceLine call
        // The service line is updated via onUpdate.emit -> ClaimPostingDetailsSubject.updateServiceLine
        this.onUpdate.emit(this.serviceLineId);
        this.afterUpdate(); // Needed to close sidebar and trigger grid refresh

        // Only show success message if no individual errors occurred
        if (!hasError) {
          this.notificationService.showNotificationSuccess(
            'Payment service line updated successfully.'
          );
        }
      });
    }
  }

  Cancel() {
    if (this.isSidebarValuesChanged()) {
      this.cancelConfirmation.opened = true;
    } else {
      this.onClose.emit();
    }
  }

  Close() {
    this.onClose.emit();
  }

  markEdited() {
  this.isServiceLineEdit = true;
}

  isSidebarValuesChanged(): boolean {
    if (
      this.isServiceLineEdit ||
      this.deletedAdjustmentIds.size ||
      this.deletedWriteoffIds.size ||
      this.adjustmentForm.valid ||
      this.editedAdjustmentIds.size ||
      this.editedWriteoffIds.size
    )
      return true;
    else return false;
  }

  removeAdjustmentField(index: number) {
    (this.adjustmentForm.get('adjustments') as FormArray).removeAt(index);
    this.addAdjustmentFormVisible =
      (this.adjustmentForm.get('adjustments') as FormArray).length == 0
        ? false
        : this.addAdjustmentFormVisible;
  }

  validateServiceLineAmounts(amount: number, isPaymentAmount: boolean) {
    if (isPaymentAmount)
      this.serviceLine.serviceLinePaymentAmount =
        this.removeLeadingDigits(amount);
    else this.serviceLine.allowedAmount = this.removeLeadingDigits(amount);
    this.isServiceLineEdit = true;

    if (this.isPatient) {
      if (
        this.serviceLine.serviceLinePaymentAmount == undefined ||
        this.serviceLine.serviceLinePaymentAmount == 0
      ) {
        this.notificationService.showNotificationWarning(
          'Please add the payment.'
        );
        return;
      } else if (this.serviceLine.serviceLinePaymentAmount < 0) {
        this.notificationService.showNotificationWarning(
          'Please add the correct amount.'
        );
        return;
      } else {
        if (this.isServiceLineEdit) {
          if (
            this.serviceLine.serviceLinePaymentAmount >
            this.paidAmountOrig + this.serviceLine.serviceLinePaymentAmount
          ) {
            this.notificationService.showNotificationError(
              'Payment amount is exceeding the balance amount.'
            );
            return;
          }
        } else if (
          this.serviceLine.serviceLinePaymentAmount > this.serviceLine.balance
        ) {
          this.notificationService.showNotificationError(
            'Payment amount is exceeding the balance amount.'
          );
          return;
        }
      }
    } else {
      if (this.serviceLine.allowedAmount == undefined) {
        if (this.serviceLine.serviceLinePaymentAmount == undefined) {
          this.notificationService.showNotificationWarning(
            'Please add the allowed and payment amount.'
          );
          return;
        }
        this.notificationService.showNotificationWarning(
          'Please add the allowed amount.'
        );
        return;
      } else if (this.serviceLine.serviceLinePaymentAmount == undefined) {
        this.notificationService.showNotificationWarning(
          'Please add the payment.'
        );
        return;
      }
    }
    if (
      this.serviceLine.serviceLinePaymentAmount == this.paidAmountOrig &&
      this.serviceLine.allowedAmount == this.allowedAmountOrig
    )
      this.isServiceLineEdit = false;
  }

  isAdjustmentEdited(id: number) {
    return this.editedAdjustmentIds.has(id);
  }

  cancelEditServiceLineAmounts() {
    this.isServiceLineEdit = false;
    this.serviceLine.allowedAmount = this.allowedAmountOrig;
    this.serviceLine.paidAmount = this.paidAmountOrig;
  }

  cancelEditAdjustments() {
    this.editedAdjustmentIds.clear();
    this.adjustments = this.adjustmentsOrig;
  }

  markAsUpdatedAdjustment(adjustment: Adjustment, isAmountUpdated: boolean) {
    this.editedAdjustmentIds.add(adjustment.id);
    if (isAmountUpdated)
      adjustment.amount = this.removeLeadingDigits(adjustment.amount);
  }

  scroll(el: HTMLElement) {
    el.scrollIntoView({ behavior: 'smooth' });
  }

  removeLeadingDigits(value: number) {
    return value ? parseFloat(value.toFixed(2)) : value;
  }

  getAdjustmentDescription(index: number): string {
    const reason = this.adjustmentForm.value.adjustments.at(index).reasonCode;
    if (!reason) return 'No description';

    const matched = this.allowedReasonCodesWithDescription.find(
      (item) => item.reasonCode === reason
    );
    return matched?.description ?? reason.replace('-', ':');
  }

  calculateAdjustmentsTotal() {
    var total =
      this.adjustments == undefined
        ? 0
        : this.adjustments
            .where((x: Adjustment) => x.isPositive == true)
            .sum((x: Adjustment) => (parseFloat(x.amount.toString()) || 0)) -
          this.adjustments
            .where((x: Adjustment) => x.isPositive == false)
            .sum((x: Adjustment) => (parseFloat(x.amount.toString()) || 0));
    var newAdjustments = this.adjustmentForm.value.adjustments;
    if (newAdjustments.length) {
      total +=
        newAdjustments.where((x) => x.isPositive == true).sum((x) => (parseFloat(x.amount.toString()) || 0)) -
        newAdjustments.where((x) => x.isPositive == false).sum((x) => (parseFloat(x.amount.toString()) || 0));
    }
    return total;
  }

  reasonCodeValidator(
    control: AbstractControl
  ): { [key: string]: boolean } | null {
    return control.value == null || control.value == 'Select One'
      ? { reasonCode: true }
      : null;
  }

  acceptDeleteAdjustment(isAccepted: boolean) {
    if (isAccepted) {
      this.deletedAdjustmentIds.add(this.adjustmentToDeleteId);
      this.adjustments = this.adjustments.filter(
        (x) => x.id != this.adjustmentToDeleteId
      );
    }
  }

  newAdjustmentAmountFocusOut(index: number) {
    let value = (this.adjustmentForm.get('adjustments') as FormArray)
      .at(index)
      .get('amount')!.value;
    let newValue = this.removeLeadingDigits(parseFloat(value));
    (this.adjustmentForm.get('adjustments') as FormArray)
      .at(index)
      .patchValue({ amount: newValue.toString() });
  }

  markDeletedAdjustment(id: number) {
    this.adjustmentToDeleteId = id;
    this.deleteConfirmation.opened = true;
  }

  updateDescriptionAndMarkUpdated(reason: string, adjustment: Adjustment) {
    const searchText = reason?.trim() ?? '';

     if (!searchText) {
      // Clear fields when user clicks the X icon
      adjustment.reasonCodeKey = '';
      adjustment.groupCode = '';
      adjustment.reasonCode = '';
      adjustment.description = '';
      return;
    }

    this.editedAdjustmentIds.add(adjustment.id);
    let splittedReasonCode = reason.split('-');
    adjustment.groupCode = splittedReasonCode[0];
    adjustment.reasonCode = splittedReasonCode[1];
    adjustment.reasonCodeKey = reason;
    const matched = this.allowedReasonCodesWithDescription.find(
      (item) => item.reasonCode === reason
    );

    adjustment.description = matched?.description ?? reason.replace('-', ':');
  }

  ngOnDestroy() {
    this.unsubscribe.next();
    this.unsubscribe.complete();
  }

   ngOnInit() {
   // get the default adjustment reason codes
   this.getAdjustmentReasonCodes("");

   this.writeOffService.getReasonCodesWithDescriptions().subscribe((res) => {
     this.allowedWriteOffReasonCodes = res;
   });
 }

 reasonCodesSearchValueChanged(newVal: string): void {
   const searchText = newVal?.trim() ?? '';
   if (searchText.length > 0) {
     if (this.allowedReasonCodesWithDescription.length > 0) {
       this.filterAdjustmentReasonCodes(searchText);
     } else if (this.allowedReasonCodesWithDescription.length === 0) {
       this.getAdjustmentReasonCodes(searchText); // this will call the API to get the data
     }
   } else {
     this.allowedReasonCodes = this.allowedReasonCodesWithDescription
       .map(item => item.reasonCode);
   }
 }

 public onDropdownClosed() {
   this.getAdjustmentReasonCodes('');
 }

  private filterAdjustmentReasonCodes(newVal: string) {
    this.allowedReasonCodes = this.allowedReasonCodesWithDescription
      .filter(item => item.reasonCode.toLowerCase().includes(newVal.toLowerCase()))
      .map(item => item.reasonCode);

      this.newReasonCodes = this.allowedReasonCodes;
  }

  private getAdjustmentReasonCodes(newVal: string) {
    this.adjustmentService
      .getAdjustmentReasonDescriptions(newVal)
      .pipe(takeUntil(this.unsubscribe))
      .subscribe((x) => {
        this.allowedReasonCodesWithDescription = x;
            
        this.allowedReasonCodes = this.allowedReasonCodesWithDescription
          .filter(item => item.isDefault === true)
          .map(item => item.reasonCode);
           // notify reasonCodes$ that codes are available
           this.reasonCodes$.next(this.allowedReasonCodesWithDescription ?? []);
      });
  }

  isWriteOffEdited(id: number) {
    return this.editedWriteoffIds.has(id);
  }

  deleteWriteOff(id: number) {
    this.writeOffToDeleteId = id;
    this.deleteConfirmationWriteOff.opened = true;
  }

  acceptDeleteWriteOff(isAccepted: boolean) {
    if (isAccepted) {
      this.deletedWriteoffIds.add(this.writeOffToDeleteId);
      this.writeOffs = this.writeOffs.filter(
        (x) => x.id != this.writeOffToDeleteId
      );
    }
  }

  markAsUpdatedWriteOff(
    writeOff: WriteOffChargeEntryModel,
    isAmountUpdated: boolean
  ) {
    this.editedWriteoffIds.add(writeOff.id);
    if (isAmountUpdated)
      writeOff.writeOffAmount = this.removeLeadingDigits(
        writeOff.writeOffAmount
      );
  }

  calculateWriteOffsTotal() {
    return this.writeOffs == undefined
      ? 0
      : this.writeOffs.sum((x: WriteOffChargeEntryModel) => x.writeOffAmount);
  }

  getRemainingBalance(id: number) {
    var totalWriteOffAmount =
      this.writeOffs == undefined
        ? 0
        : this.writeOffs
            .where((x) => x.id != id)
            .sum((x: WriteOffChargeEntryModel) => x.writeOffAmount);
    return (
      this.serviceLine.billedAmount -
      this.serviceLine.paidAmount +
      this.calculateAdjustmentsTotal() -
      totalWriteOffAmount
    );
  }
}
