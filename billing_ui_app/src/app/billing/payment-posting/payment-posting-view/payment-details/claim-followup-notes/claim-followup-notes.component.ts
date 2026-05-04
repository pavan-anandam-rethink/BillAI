import { Component, EventEmitter, OnInit, Output, ViewChild } from "@angular/core";
import { ClaimNote, ClaimNotesFilter, PatientDetails } from "@core/models/billing";
import { ClaimNotesService, ClaimPostingService } from "@core/services/billing";
import { Observable, Subject } from "rxjs";
import { FormBuilder, Validators } from "@angular/forms";
import { MatTabChangeEvent } from "@angular/material/tabs";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { ClaimNoteGetAllModel, ClaimNoteGetModel, ClaimNoteSaveModel } from "@core/models/billing/notes/cliam-posting-note";
import { takeUntil } from "rxjs/operators";
import { Locale } from "@app/locale";
import { AccountPermissions } from "@core/enums/account";
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { NotificationService } from "@progress/kendo-angular-notification";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";
import { ClaimFollowUpResponseModel } from "../../../../../core/models/billing/report-model";
import { DialogService } from "@progress/kendo-angular-dialog";

@Component({
  selector: 'app-claim-followup-notes',
  templateUrl: './claim-followup-notes.component.html',
  styleUrls: ['./claim-followup-notes.component.css']
})
export class ClaimFollowupNotesComponent implements OnInit {
  @Output() noteAdded = new EventEmitter<void>();
  @Output() noteCompleted = new EventEmitter<void>();
  public unsubscribe$ = new Subject<void>();

  notes: ClaimNote[] = [];
  filter: ClaimNotesFilter;
  addMode = false;
  claim: Partial<ClaimFollowUpResponseModel>;
  patient$: Observable<PatientDetails>
  showErrors = false;
  saving = true;
  minDate = new Date();
  showButtons = true;
  canEdit: boolean;
  active: boolean = false;
  onReload = new EventEmitter();
  index: number;

  public activeTabChanged(event: MatTabChangeEvent): void {
    this.showButtons = event.tab.textLabel !== 'Deleted';
  }

  constructor(
    private claimNotesService: ClaimNotesService,
    private claimPostingService: ClaimPostingService,
    private fb: FormBuilder,
    private accountService: AccountMemberService,
    public locale: Locale,
    private notificationService: NotificationHandlerService,
    private dialogService: DialogService
  ) {

    this.accountService
      .accountMemberSettings
      .subscribe((x) => {
        if (x) {
          this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingEdit);
        }
      });
  }

  setData(claim: Partial<ClaimFollowUpResponseModel>) {
    this.claim = claim;
    this.index = claim.balance;
    if (claim.dateDeleted == null) { this.active = true }
    this.filter = { active: true, claimId: claim.claimIdValue };
  }

  markCompleted(note: Partial<ClaimFollowUpResponseModel>) {
    const dialog = this.dialogService.open({
      title: 'Please confirm',
      width: 500,
      content: 'Mark note as completed?',
      actions: [
        { text: "Cancel" },
        { text: "Save", primary: true }
      ]
    });

    dialog.result.subscribe((result: any) => {
      if (result.text === 'Save') {
        this.claimNotesService.deleteNote({ id: note.id, dateCreated: new Date(note.noteCreatedDate), memberId: note.memberId }).subscribe(result => {
          if (result.success) {
            this.onReload.emit(true)
            this.active = false;
            this.claimNotesService.setClaimNoteCompleted(this.index);
            this.noteCompleted.emit();
          }
          this.notificationService.showNotificationSuccess("Note Completed Successfully.")
        });
      }
    });
  }

  getActive(notes: ClaimNote[]) {
    const result = notes.filter(n => !n.dateDeleted);
    return result;
  }
  getInactive(notes: ClaimNote[]) {
    const result = notes.filter(n => !!n.dateDeleted);
    return result;
  }

  reload() {
    const claimNoteRequest: ClaimNoteGetAllModel = {
      id: this.claim.id,
      accountInfoId: this.accountService.memberDetails.accountInfoId
    }
    this.claimNotesService.getAll(claimNoteRequest).pipe(takeUntil(this.unsubscribe$))
      .subscribe((result) => {
        if (result.success) {
          this.notes = result.data;
        }
      });
  }

  noteForm = this.fb.group({
    remindDate: this.fb.control(null, [Validators.required]),
    note: this.fb.control(null, [Validators.required])
  });


  checkTextAndDateArea() {
    if (this.noteForm.value.note.trim() !== '' && this.noteForm.value.remindDate !== null) {
      this.saving = false;
    } else {
      this.saving = true;
    }
  }

  addNote() {
    this.addMode = true;
  }

  cleanup() {
    this.addMode = false;
    this.noteForm.reset();
    this.showErrors = false;
    this.saving = true
  }

  saveNote() {
    if (this.saving) {
      return;
    }

    if (this.noteForm.invalid) {
      this.showErrors = true;
      this.saving = true;
      return;
    }

    const model = this.noteForm.value as ClaimNoteSaveModel;
    const currentDate = new Date();
    const currentTimeZoneOffset = currentDate.getTimezoneOffset();

    model.remindDate.setTime(model.remindDate.getTime() - currentTimeZoneOffset * 1000 * 60);
    model.claimId = this.filter.claimId;
    model.memberId = this.accountService.memberDetails.memberId;
    //Comment added for testing

    this.claimNotesService.addNote(model).pipe(takeUntil(this.unsubscribe$)).subscribe(x => {
      this.reload();
      this.addMode = false;
      this.noteForm.reset();
      this.saving = true;
      this.notificationService.showNotificationSuccess("Note added successfully.");
      // Emit the event to close the sidebar
      this.noteAdded.emit();
    }, x => {
      this.saving = false;
    });

  }

  ngOnInit() {
    const defaultDate = new Date();
    defaultDate.setDate(this.minDate.getDate() + 30);

    this.noteForm.get("remindDate")!.setValue(defaultDate);
  }
}
