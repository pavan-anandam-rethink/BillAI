import { Component, OnDestroy, OnInit, EventEmitter, Output, Input } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Helper } from '@app/billing/encounters/common/common-helper';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { PaymentPostingService } from '@core/services/billing';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { DatePickerComponent } from '@progress/kendo-angular-dateinputs';
import { UnallocatedManualCreatePayment } from '@core/models/billing/manual-create-payment';

@Component({
  selector: 'pending-collection-payment-dialog',
  templateUrl: './pending-collection-payment-dialog.component.html',
  styleUrls: ['./pending-collection-payment-dialog.component.css']
})
export class PendingCollectionPaymentDialogComponent implements OnInit, OnDestroy {
  @Output() closeDialog = new EventEmitter();
  @Input() paymentData: { patientId: number, patientBalance: number };
  paymentMethods: string[] = ['Credit Card', 'ACH', 'Check', 'Cash'];
  selectedPaymentMethod: string = '';
  paymentMethodTypes: string[] = [];
  referenceNumber: string = '';
  postDate: Date = new Date();
  depositDate: Date = new Date();
  isLoading: boolean = false;
  startedParsingLoading: boolean = false;

  paymentInfo: FormGroup = new FormGroup({
    funderType: new FormControl('Patient', [Validators.required]),
    paymentMethod: new FormControl('', [Validators.required]),
    referenceNumber: new FormControl('', [Validators.required]),
    depositDate: new FormControl(new Date(), [Validators.required]),
    postDate: new FormControl(new Date(), [Validators.required])
  });

  formGroup: FormGroup = new FormGroup({
    note: new FormControl('', Validators.required),
    unallocatedAmount: new FormControl('', Validators.required)
  }); 

  datePickerMinDate = new Date(2000, 1, 1);
  isReferenceNumberRequired: boolean = false;
  showNoteDialog: boolean = false;
  AddNote: boolean = true;
  hasUnallocatedAmount: boolean = false;

  constructor(private fb: FormBuilder, 
              private router: Router, 
              private paymentPostingService: PaymentPostingService, 
              private accountService: AccountMemberService,
              private notificationService: NotificationHandlerService,) {}

  ngOnInit(): void {
    this.populateDefaults();
  }

  populateDefaults(): void {
    this.selectedPaymentMethod = '';
    this.referenceNumber = '';
    this.postDate = new Date();
    this.depositDate = new Date();
    this.paymentInfo.patchValue({
      paymentMethod: '',
      referenceNumber: '',
      depositDate: new Date(),
      postDate: new Date()
    });
  }

  ngOnDestroy(): void {}

  onDatePickerClose(element: DatePickerComponent) {
    window.setTimeout(() => { element.blur() }, 0);
  }

  methodChanged(methodValue: any): void {
    if (methodValue == "Cash") {
      this.isReferenceNumberRequired = false;
    } else {
      this.isReferenceNumberRequired = true;
    }
    this.paymentInfo.patchValue({ paymentMethod: methodValue });
  }

  close() {
    this.formGroup.reset();
    this.closeDialog.emit();
  }

  openNoteDialog() {
      this.showNoteDialog = true;
      this.AddNote = true;
  }

  checkTextArea(event: Event): void { 
    const value = (event.target as HTMLInputElement).value;
    this.formGroup.get('note')?.setValue(value);
    const noteValue = this.formGroup.get('note')?.value;
    this.AddNote = !noteValue || noteValue.trim() === '';
  }

  onUnallocatedAmountChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    let value = input.value;
    const isValid = /^-?\d+(\.\d{0,5})?$/.test(value);
    if (isValid) {
      const roundedValue = Number(parseFloat(value).toFixed(2));
      this.formGroup.get('unallocatedAmount')?.setValue(roundedValue);
      if (roundedValue === 0)
      {
        this.hasUnallocatedAmount = false;
        return;
      }
      this.hasUnallocatedAmount = true;
    }
    else {
      input.value = '';
      this.formGroup.get('unallocatedAmount')?.setValue("");
      this.hasUnallocatedAmount = false;
    }
  }

  addNote() {
      this.showNoteDialog = false;
  }

  cancelNote() {
    this.showNoteDialog = false;
    this.formGroup.get('note')?.setValue("");
  }

  createPayment(): void {
      if (!this.hasUnallocatedAmount) {
        this.notificationService.showNotificationWarning("Please enter a valid unallocated amount other than zero.");
        return;
      }
      if(!this.paymentInfo.valid){
          return;
      }
      this.isLoading = true;
      let formValue = this.paymentInfo.value;
      let formsNoteValue = this.formGroup.value;

      let patientModel: UnallocatedManualCreatePayment = {
        funderType: formValue.funderType,
        paymentMethod: formValue.paymentMethod,
        paymentAmount: this.paymentData.patientBalance.toString(),
        referenceNumber: formValue.referenceNumber,
        postDate: Helper.shiftDateToUTC(new Date(formValue.postDate)),
        depositDate: Helper.shiftDateToUTC(new Date(formValue.depositDate)),
        AccountInfoId: this.accountService.memberDetails.accountInfoId,
        MemberId: this.accountService.memberDetails.memberId,
        patientId: this.paymentData.patientId,
        unAllocatedAmount: Number(formsNoteValue.unallocatedAmount),
        notes: formsNoteValue.note,
      }
          
      this.paymentPostingService.manualCreatePatientPayment(patientModel)
              .subscribe((result: number) => {
                  this.router.navigate([
                    '/billing/paymentposting/edit',
                    result,
                    this.paymentData.patientId
                  ]);
                },error => {
                  this.isLoading = false;
                  this.notificationService.showNotificationError("Failed to create payment record.");
              });
  }

}
