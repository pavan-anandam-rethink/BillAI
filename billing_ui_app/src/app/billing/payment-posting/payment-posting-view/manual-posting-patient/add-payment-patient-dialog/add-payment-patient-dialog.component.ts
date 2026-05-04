import {
    Component, EventEmitter,
    OnDestroy, Input, Output, ViewChild,
    ViewEncapsulation, ElementRef
} from '@angular/core';

import {PaymentPostingService} from '@core/services/billing';
import {
    CreatePaymentPatientClaims,
    ManualPaymentPatientSearch,
    ManualPaymentPatientSearchBase
} from '@core/models/billing';

import {Subject, Subscription} from "rxjs";
import {ClaimPostingService} from "@core/services/billing/claim-posting.service";
import {debounceTime, distinctUntilChanged, takeUntil} from "rxjs/operators";
import { AccountMemberService } from '@core/services/account/account-member.service';
import { AddPatientResponseClaims } from '@core/models/billing/create-payment-patient-claims';
import { AddClaimNotesDialogResult } from '../../../../encounters/ecnounter-list/add-claim-notes-dialog/add-claim-notes-dialog.component';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';

@Component({
    selector: 'add-payment-patient-dialog',
    templateUrl: './add-payment-patient-dialog.component.html',
    styleUrls: ['./add-payment-patient-dialog.component.css'],
    encapsulation: ViewEncapsulation.None
})

export class AddPaymentPatientDialogComponent implements OnDestroy {
    @Input() paymentId: number;
    @Output() closeDialogEmitter = new EventEmitter<CreatePaymentPatientClaims>();
    @ViewChild("patientAutocomplete") patientAutocomplete: any;
    @ViewChild('patientSearchContainer', { read: ElementRef }) public patientSearchContainerEl: ElementRef;
    private unsubscribeAll$ = new Subject();
    isLoading: boolean;
    @Output() noteEditorOpened = new EventEmitter();
    private delayedCall: any;
    
    patientSearch: string = "";
    loading: boolean = false;
    subscriptions = new Subscription();
    isAddPatientBtnDisabled = false;

    patientSearchRequest = new ManualPaymentPatientSearchBase();
    patients: ManualPaymentPatientSearch[] = [];
    selectedPatients: ManualPaymentPatientSearch[] = [];
    subject: Subject<any> = new Subject();
    showNoteDialog: boolean = false; 
    formGroup: FormGroup;           
    AddNote: boolean = true;         
    patient: any = { notes: '' };    
    indexId: number;

    constructor(private paymentPostingService: PaymentPostingService,
      private claimPostingService: ClaimPostingService, private accountService: AccountMemberService, private notificationService: NotificationHandlerService) {
    }
    
    valueChanged(e: any){
        this.patientAutocomplete.text = this.patientSearchRequest.personName;
    }

    ngOnInit(): void {
            this.subject
            .pipe(debounceTime(500),distinctUntilChanged())
            .subscribe((e) => {
                this.getPatientsByValue(e);
                }
      );

      this.formGroup = new FormGroup({
        note: new FormControl('', Validators.required)
      });

    }

           
        patientsSearchValueChange(event: any = "") {
            this.subject.next(event);
        }

        getPatientsByValue(event: any): void {
        event = event ? event : "";
        let newVal = event;
        this.patientSearch = event;
        this.delayedCall = window.setTimeout(() => {
            if (newVal == this.patientSearchRequest.personName) {
                this.searchPatient();
            } else {
                this.isLoading = false;
            }
        }, 1500);
        
        if(!this.patientAutocomplete.isOpen) {
            this.patientAutocomplete.toggle(true);
        }
        this.patientSearchRequest.personName = newVal;
    }

    searchPatient(): void {
        this.isLoading = true;
        this.patientSearchRequest.accountInfoId = this.accountService.memberDetails.accountInfoId;
        this.paymentPostingService.getPatients(this.patientSearchRequest)
            .subscribe(result => {
                this.patients = result.where((x: ManualPaymentPatientSearch) =>
                    !this.selectedPatients.any((p: ManualPaymentPatientSearch) => p.id === x.id));
                this.patients = this.selectedPatients.concat(this.patients);
                this.isLoading = false;
            });
    }

    patientClick(clickedPatient: ManualPaymentPatientSearch): void {
        if (clickedPatient.checked)
            this.uncheckPatient(clickedPatient);
        else this.checkPatient(clickedPatient);

    }

    checkPatient(patient: ManualPaymentPatientSearch) {
        this.selectedPatients.push(patient);
        patient.checked = true;
        // this.sortPatients();
    }

    uncheckPatient(patient: ManualPaymentPatientSearch) {
        this.selectedPatients.remove(patient);
        patient.checked = false;
        // this.sortPatients();
    }

    sortPatients() {
        this.patients.sort((a, b) => (a.patientName).localeCompare(b.patientName));
        this.patients.sort((a, b) => (a.checked === b.checked) ? 0 : a.checked ? -1 : 1);
    }

    onCloseAutocomplete(event: any) {
        event.preventDefault();

        // //Close the list if the component is no longer focused
        setTimeout(() => {
            if (!this.patientAutocomplete.wrapper.contains(document.activeElement)) {
                this.patientAutocomplete.toggle(false);
                this.patients = this.selectedPatients;
                this.patientAutocomplete.reset();
            }
        });
    }

    onCloseDialog(data: CreatePaymentPatientClaims = null): void {
        this.closeDialogEmitter.emit(data);
    }

    addPatientClaims(): void {
        if (this.selectedPatients.length == 0) {
            return;
        }

        this.isAddPatientBtnDisabled = true;

        let patientIds: string[] = [];
        let paymentAmounts: number[] = [];
        let notes: string[] = [];
        this.selectedPatients.forEach(x => patientIds.push(x.id));
        this.selectedPatients.forEach(x => paymentAmounts.push(x.paymentAmounts));
        this.selectedPatients.forEach(x => notes.push(x.notes));
        let hasEmpty = this.selectedPatients.some((x) => x.paymentAmounts == null);


        let model: CreatePaymentPatientClaims = {
            paymentId: this.paymentId,
            patientIds: patientIds,
            unAllocatedAmount: paymentAmounts,
            notes: notes,
            accountInfoId: this.accountService.memberDetails.accountInfoId,
            memberId: this.accountService.memberDetails.memberId
        };
        if (!hasEmpty)
        {
          this.onCloseDialog(model);
          this.isAddPatientBtnDisabled = true;
        }
        /*    this.onCloseDialog(model);*/
        else
        {
          this.isAddPatientBtnDisabled = false;
          this.notificationService.showNotificationWarning("Payment amount is required and the payment is not saved.");
          return;
        }
        
    }

    getInitials(patient: ManualPaymentPatientSearch) {
        return patient.patientName.split(' ').select((x: string) => x.charAt(0)).join('');
    }  

    ngOnDestroy() {
        this.unsubscribeAll$.next(void 0);
        this.unsubscribeAll$.complete();
   }
   

  openNoteDialog(patient: ManualPaymentPatientSearch) {
    if (this.selectedPatients.length > 0) {
      this.showNoteDialog = true;
      this.AddNote = true;

      this.indexId = this.selectedPatients.findIndex((item: any) => item.id === patient.id);

      var savetext = this.selectedPatients[this.indexId].notes;
      this.formGroup.controls['note'].setValue(savetext);
    }
  }

  checkTextArea() {
    const noteValue = this.formGroup.get('note')?.value;
    this.AddNote = !noteValue || noteValue.trim() === '';
  }

  cancelNote() {
    this.showNoteDialog = false;
    this.formGroup.reset();
  }

  addNote() {
    if (this.formGroup.valid) {
      if (this.indexId !== -1) {
        this.selectedPatients[this.indexId].notes = this.formGroup.value.note;
      }
      this.showNoteDialog = false;
      this.formGroup.reset();
    }
  }

}
