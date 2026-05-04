import { Component, OnInit, OnDestroy, Input, Output, EventEmitter } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { GridDataResult, PageChangeEvent, PagerSettings } from '@progress/kendo-angular-grid';
import { SortDescriptor } from '@progress/kendo-data-query';

import { EncounterAttachmentService } from '@core/services/billing';
import { ConfirmDialog } from '@core/models/common';
import { PaymentAttachment } from "@core/models/billing/payment-attachment";
import { IdFilterSort } from "@core/models/billing/id-filter-sort";
import { ActivatedRoute } from "@angular/router";
import { RenameAttachmentModel } from '@core/services/billing/encounter-attachment.service';

import { NotificationService } from '@progress/kendo-angular-notification';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';

@Component({
  selector: 'app-encounter-attachments',
  templateUrl: './encounter-attachments.component.html',
  styleUrls: ['./encounter-attachments.component.css']
})



export class EncounterAttachmentsComponent implements OnDestroy {
  @Input() claimId: number;
  private unsubscribeAll$ = new Subject<void>();

  view: GridDataResult = {
    data: [],
    total: 0
  };

  existingFileNames:string[];

  deleteConfirmation = new ConfirmDialog(false, "Confirmation", "Are you sure you want to delete this attachment?");
  attachmentToDelete: PaymentAttachment;

  attachmentsOrig: PaymentAttachment[] = [];

  showAddAttachmentDialog = false;
  attachments: PaymentAttachment[] = [];

  claimIdentifier: string = '';
  @Output() onAttachmentCountChanged = new EventEmitter<number>();
  req: { Id: number; AccountInfoId: number; MemberId: number; };


  constructor(private route: ActivatedRoute, private encounterAttachmentService: EncounterAttachmentService, private notificationService: NotificationHandlerService
    , private accountService: AccountMemberService) {
  }

  loadData(parentData: any) {
    this.claimIdentifier = parentData.claimIdentifier;
    this.loadPaymentAttachments(parentData.claimId);
  }

  loadPaymentAttachments(claimId: number) {
    this.claimId = claimId;
    this.req = { Id: this.claimId, AccountInfoId: this.accountService.memberDetails.accountInfoId, MemberId: this.accountService.memberDetails.memberId };
    this.encounterAttachmentService.GetForEncounter(this.req)
      .pipe(takeUntil(this.unsubscribeAll$))
      .subscribe((x: any) => {
        this.view.data = x.data;
        this.view.total = x.totalCount;
        this.existingFileNames = x.data?.map(file=>file.filename);
        this.onAttachmentCountChanged.emit(x.totalCount);
      });
  }

  downloadAttachment(attachment: PaymentAttachment) {
    this.req.Id = attachment.id;
    this.encounterAttachmentService.getFileUpload(this.req);
  }

  openAddAttachmentDialog() {
    this.showAddAttachmentDialog = true;
  }

  closeAddAttachmentDialog() {
    this.showAddAttachmentDialog = false;
    this.loadPaymentAttachments(this.claimId);
  }

  deleteAttachment(file: PaymentAttachment) {
    this.attachmentToDelete = file;
    this.deleteConfirmation.opened = true;
  }

  startEdit(file: PaymentAttachment) {
    this.attachmentsOrig.push({ ...file });

    file.filename = file.filename.substr(0, file.filename.lastIndexOf('.'));
  }

  isEdited(file: PaymentAttachment) {
    return this.attachmentsOrig.find(x => x.id == file.id) != undefined;
  }

  cancelEdit(file: PaymentAttachment) {
    let fileOrig = this.attachmentsOrig.find(x => x.id == file.id);
    if (fileOrig == undefined)
      return;

    file.filename = fileOrig.filename;
    this.attachmentsOrig = this.attachmentsOrig.filter(x => x.id !== file.id);
  }

  acceptEdit(file: PaymentAttachment) {
    let fileOrig = this.attachmentsOrig.find(x => x.id == file.id);
    if (fileOrig == undefined)
      return;

    let newFileName = `${file.filename}.${fileOrig.filename.split('.').pop()}`;

    if (file.filename.trim() === null || file.filename.trim() === '') {
      this.notificationService.showNotificationError("Enter the attachment name.");
      return;
    }

    if (fileOrig.filename != newFileName) {
      var renameAttachment = new RenameAttachmentModel();
      renameAttachment.AttachmentId = file.id;
      renameAttachment.FileName = newFileName;
      renameAttachment.AccountInfoId = this.accountService.memberDetails.accountInfoId;
      renameAttachment.MemberId = this.accountService.memberDetails.memberId;

      if (this.existingFileNames.some(element => element == renameAttachment.FileName)) {
        this.notificationService.showNotificationError("File name is already exist.");
        file.filename = fileOrig.filename;
        this.startEdit(file);
        return false;
      }
      const index = this.existingFileNames.indexOf(fileOrig.filename);
      if (index !== -1) {
        this.existingFileNames[index] = newFileName;
      }

      this.encounterAttachmentService.renameAttachment(renameAttachment)
        .pipe(takeUntil(this.unsubscribeAll$))
        .subscribe(x => {
          file.filename = newFileName;
          this.attachmentsOrig.removeWhere((x: PaymentAttachment) => x.id == file.id);
        })
      this.notificationService.showNotificationSuccess("Attachment(s) updated successfully.");
    }
    else {
      this.notificationService.showNotificationError("Attachment is not updated.");
    }
  }
  
  acceptDeleteAttachment(isAccepted: boolean) {
    if (isAccepted) {
      this.req.Id = this.attachmentToDelete.id;
      this.encounterAttachmentService.deleteUpload(this.req)
        .pipe(takeUntil(this.unsubscribeAll$))
        .subscribe(x => {
          this.loadPaymentAttachments(this.claimId);
          this.notificationService.showNotificationSuccess("Attachment(s) deleted successfully.");
        });
    }
  }
  
  ngOnDestroy(): void {
    this.unsubscribeAll$.next();
    this.unsubscribeAll$.complete();
  }

  // ngOnInit(): void {
  //   this.loadPaymentAttachments(this.claimId);
  // }
}



