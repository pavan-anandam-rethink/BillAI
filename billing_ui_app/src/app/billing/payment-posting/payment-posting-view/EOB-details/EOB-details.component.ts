import { Component, EventEmitter, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ClaimEOBInfo, PaymentEOBInfo } from '@core/models/billing';
import { CarcCodes } from '@core/models/billing/carc-codes';
import { ClaimPostingService, PaymentPostingService } from '@core/services/billing';
import { saveAs } from 'file-saver';
import { Subscription } from 'rxjs';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { EdiFileType } from '@core/enums/billing/edi-file-type';
import { ClaimEdiFilesModel } from '@core/models/billing/claim-filter-option-model';
import { AccountMemberSettings } from '../../../../core/services/account/account-member.service';
import { AuthService } from '@core/services/sso/auth.service';
import { ClaimHeader } from '@core/models/billing/claim-header-search';
@Component({
  selector: 'EOB-details',
  templateUrl: './EOB-details.html',
    styleUrls: ['./EOB-details.css',
        '../../status-actions.css']
})
export class EOBDetailsComponent implements OnDestroy {
  subscriptions = new Subscription();
  claims$ = new EventEmitter<ClaimEOBInfo[]>();
  payment: PaymentEOBInfo;
  currentDate = new Date();
  carcCodes: CarcCodes[] = [];

  paymentId: number;

  formattedEdiContent: string = '';
  claim: any;
  public memberDetails: AccountMemberSettings | null = null;
  constructor(
    private route: ActivatedRoute,
    private paymentPostingService: PaymentPostingService,
    private claimPostingService: ClaimPostingService,
    private notificationService: NotificationHandlerService,
    private authSvc: AuthService
  ) {
    this.subscriptions.add(this.route.params.subscribe(x => {
      if (x["id"]) {
        this.paymentId = +x["id"];
      }
    }));
    this.resetSettings();
  }

  loadInfo() {
    this.subscriptions.add(this.paymentPostingService.getEOBInfoById(this.paymentId).subscribe(payment => {
      this.payment = payment;
    }));
    this.subscriptions.add(this.claimPostingService.getEOBClaims(this.paymentId).subscribe(claims => {
      claims.map(claim => {
        return claim;
      });
      if (claims && claims.length > 0) {
        this.claim = claims[0];
      }
      this.claims$.emit(claims);
    }));
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  resetSettings() {
    this.memberDetails = this.authSvc.getUserData();
  }

  public isRethinkAdminUser(): boolean {
    return !!this.memberDetails?.impersonatedUser;
  }

  print() {
    this.loadEOBPDF(true);
  }

  loadEOBPDF(printDoc = false) {
    const currentUserDateTime = new Date();
        this.subscriptions.add(this.claimPostingService.getEOBPdf(this.paymentId, [], currentUserDateTime, false).subscribe((result: any) => {
          this.processPDF(result, printDoc);
        }));
  }

  private processPDF(response: any, printDoc = false) {
        const contentDispositionHeader = response.headers.get('Content-Disposition');
        if (contentDispositionHeader === undefined)
            return '';

    let name = contentDispositionHeader.split(';')[1].trim().split('=')[1];
    name = name.replace(/"/g, '');
    const filename = `${name}.pdf`;

    const blob = new Blob([response.body], { type: 'application/pdf' });

    if (printDoc) {
      const blobUrl = URL.createObjectURL(blob);
      const iframe = document.createElement('iframe');
      document.body.appendChild(iframe);

      iframe.style.display = 'none';
      iframe.src = blobUrl;
      iframe.onload = function () {
        window.setTimeout(function () {
          iframe.focus();
          iframe.contentWindow && iframe.contentWindow.print();
        }, 100);
      };
    } else {
      saveAs(blob, filename);
    }
  }

  onDownloadERA(model?: ClaimHeader): void {
    if (!this.paymentId) {
      return;
    }

    const ediModel: ClaimEdiFilesModel = {
      PaymentId: this.paymentId,
      FileType: EdiFileType.EDI835,
      BatchId: this.claim.ClaimSubmissionIdentifier,
      ClaimId: this.claim.Id
    };


    this.paymentPostingService.getEdiFilesFromBlobData(ediModel).subscribe({
      next: (blob: string) => {
        if (!blob || blob.length === 0) {
          this.notificationService.showNotificationError('EDI file not found.');
          return;
        }
        const url = window.URL.createObjectURL(new Blob([blob], { type: 'text/plain' }));
        const link = document.createElement('a');
        link.href = url;
        link.download = `EDI_835_${this.paymentId}_${new Date().toISOString().slice(0, 10)}.txt`;
        link.click();
        window.URL.revokeObjectURL(url);
        this.notificationService.showNotificationSuccess('EDI 835 file downloaded successfully.');
      },
      error: () => {
        this.notificationService.showNotificationError('EDI file not found.');
      }
    });
  }
}
