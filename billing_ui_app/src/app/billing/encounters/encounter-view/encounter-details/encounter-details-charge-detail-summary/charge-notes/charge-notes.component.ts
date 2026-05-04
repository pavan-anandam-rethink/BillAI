import { Component } from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AccountPermissions } from '@core/enums/account/account-permissions';

import { ClaimDetailsModel, ClaimNoteDetailsModel, PatientDetails } from '@core/models/billing';
import { ConfirmDialog } from '@core/models/common';
import { AccountMemberService, AccountMemberSettings } from '@core/services/account/account-member.service';
import { ChargeEntryService, ClaimPostingService } from '@core/services/billing';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { NotificationService } from '@progress/kendo-angular-notification';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-charge-notes',
  templateUrl: './charge-notes.component.html',
  styleUrls: ['./charge-notes.component.css'],
})
export class ChargeNotesComponent {
  chargeId: number;
  chargeEntry: ClaimDetailsModel | null;
  isInEditMode = false;

  noteForm = this.fb.group({
      note: this.fb.control(null, [Validators.required, Validators.maxLength(80)])
  });

  minDate = new Date();
  showErrors = false;
  buttonOn=true;
  canEdit: boolean;

  details: PatientDetails;

  public confirmDeleteNoteDialog: ConfirmDialog = new ConfirmDialog(false, "Delete Charge Detail Note",
      "Are you sure you'd like to perform this action?", "Delete", "Cancel");

  public confirmAddNoteDialog: ConfirmDialog = new ConfirmDialog(false, "Add Charge Detail Note",
      "Only one note can be stored. All other notes will be deleted. " +
      "Are you sure you'd like to perform this action?", "Add Note", "Cancel");
  
  private unsubscribeAll$ = new Subject<void>();

  constructor(private chargeService: ChargeEntryService,private notificationService: NotificationHandlerService, private claimService: ClaimPostingService,
              private fb: FormBuilder, private accountService: AccountMemberService) {
                this.accountService
            .accountMemberSettings
            .subscribe((x: AccountMemberSettings|null) => {
                if (x) {
                    this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingEdit);
                }
            });
  }

  openDeleteNoteDialog(): void {
      this.confirmDeleteNoteDialog.opened = true;
  }

  closeDeleteNoteDialog(): void {
      this.confirmDeleteNoteDialog.opened = false;
  }

  openAddNoteDialog(): void {
      this.confirmAddNoteDialog.opened = true;
  }

  closeAddNoteDialog(): void {
      this.confirmAddNoteDialog.opened = false;
  }
  
  addNote() {        
      let noteText = this.noteForm.get('note')!.value;

      let model = {
          chargeId: this.chargeEntry!.id,
          noteText: noteText,
          noteCreatedBy: this.accountService.memberDetails.memberId
      };
      
      this.chargeService.addChargeNote(model)
          .pipe(takeUntil(this.unsubscribeAll$))
          .subscribe((result: ClaimNoteDetailsModel) => {
              this.chargeEntry!.noteText = result.noteText;
              this.chargeEntry!.noteCreatorName = result.noteCreatorName;
              this.chargeEntry!.noteCreatedDate = result.noteCreatedDate;
              
              this.clearNoteForm();
              this.notificationService.showNotificationSuccess("Note Added successfully.");
          });
  }
  
  checkTextArea(){
    if(this.noteForm.value.note.trim()!=='')
        {
            this.buttonOn=false
        }
        else{
            this.buttonOn=true
        }
  }

  deleteNote(){
      this.chargeService.deleteChargeNote(this.chargeEntry!.id)
          .pipe(takeUntil(this.unsubscribeAll$))
          .subscribe(() => {
              this.chargeEntry!.noteCreatorName = null;
              this.chargeEntry!.noteCreatedDate = null;
              this.chargeEntry!.noteText = '';
              this.notificationService.showNotificationSuccess("Note deleted successfully.");
          });
  }

  setChargeData(chargeEntry: ClaimDetailsModel) {
      this.chargeEntry = chargeEntry;
  }

  setData(patientId: number) {
      this.claimService.getPatientDetails(patientId)
          .pipe(takeUntil(this.unsubscribeAll$))
          .subscribe((x: any) => {
              this.details = x;
              this.details.address = x.address.split(/[\r\n]+/).where((y: string) => y != ",   ").join("\r\n");
          });
  }

  getInitials(patientName: string) {
      return patientName.split(' ').select((x: string) => x.charAt(0)).join('');
  }

  clearNoteForm() {
      this.isInEditMode = false;
      this.noteForm.get('note')!.setValue(null);
      this.buttonOn=true
  }

  ngOnDestroy(): void {
      this.unsubscribeAll$.next();
      this.unsubscribeAll$.complete();
  }
}