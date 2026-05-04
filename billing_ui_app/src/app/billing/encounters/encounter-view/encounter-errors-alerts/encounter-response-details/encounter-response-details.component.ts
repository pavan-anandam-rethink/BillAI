import { Component, Input, OnInit } from '@angular/core';
import { ClaimErrorAlertModel } from '@core/models/billing/claim-errors-alerts';
import { AccountMemberSettings } from '@core/services/account/account-member.service';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { AuthService } from '@core/services/sso/auth.service';
import { EncounterAttachmentService } from '@core/services/billing/encounter-attachment.service';
import { EdiFileType } from '@core/enums/billing/edi-file-type';
import { ClaimListingTab } from '@core/enums/billing/claim-listing-tab';
import { log } from 'console';

@Component({
  selector: 'app-encounter-response-details',
  templateUrl: './encounter-response-details.component.html',
  styleUrls: ['./encounter-response-details.component.css']
})
export class EncounterResponseDetailsComponent {
  @Input('responseEl') responses: any[];
  @Input() claimStatus: string;
  @Input() listingTab: ClaimListingTab | null = null;

  @Input() claimId: number;
  public memberDetails: AccountMemberSettings | null = null;

  constructor(
    private notificationService: NotificationHandlerService,
    private authSvc: AuthService,
    private encounterAttachmentService: EncounterAttachmentService
  ) {
    this.resetSettings();
  }

  public downloadEdi837(batchId: string): void {
    if (!batchId) {
      this.notificationService.showNotificationError('No Batch ID available to download EDI 837.');
      return;
    }

    const model = {
      AccountInfoId: this.memberDetails?.accountInfoId,
      MemberId: this.memberDetails?.memberId,
      FileType: EdiFileType.EDI837,
      BatchId: batchId,
      ClaimId: this.claimId,
      PaymentId: null,
      BlobFilePath: null,
      ClearingHouseId: null
    };

    this.encounterAttachmentService.getEdiFilesFromBlob(model).subscribe({
      next: (ediContent: string) => {
        if (!ediContent || ediContent.trim() === '') {
          this.notificationService.showNotificationError('EDI file not found.');
          return;
        }
        const fileName = `EDI_837_${batchId}_${new Date().toISOString().slice(0, 10)}.txt`;
        this.downloadFile(ediContent, fileName);
        this.notificationService.showNotificationSuccess('EDI 837 file downloaded successfully.');
      },
      error: () => {
        this.notificationService.showNotificationError('Unable to download EDI 837 file. Please try again.');
      }
    });
  }

  resetSettings() {
    this.memberDetails = this.authSvc.getUserData();
  }

  public isRethinkAdminUser(): boolean {
    return !!this.memberDetails?.impersonatedUser;
  }

  public onDownloadClick(responseItem: any) {
    if (!this.memberDetails || !responseItem) {
      return;
    }
    
    const model = {
      AccountInfoId: this.memberDetails.accountInfoId,
      MemberId: this.memberDetails.memberId,
      FileType: responseItem.fileType,
      BatchId: responseItem.batchId || null,
      ClaimId: this.claimId,
      PaymentId: null,
      BlobFilePath: responseItem.blobFilePath || null,
      ClearingHouseId: responseItem.clearingHouseId || null
    };

    this.encounterAttachmentService.getEdiFilesFromBlob(model).subscribe({
      next: (response) => {
        if (response) {
          // Response is the raw EDI file content string
          const fileName = `${responseItem.fileType}_${responseItem.source || 'EDI'}_${new Date().getTime()}.txt`;
          this.downloadFile(response, fileName);
          this.notificationService.showNotificationSuccess('EDI file downloaded successfully');
        } else {
          this.notificationService.showNotificationError('EDI file not found');
        }
      },
      error: (error) => {
        this.notificationService.showNotificationError('EDI file not found.');
      }
    });
  }

  private downloadFile(content: string, fileName: string): void {
    const blob = new Blob([content], { type: 'text/plain' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);
  }

}

