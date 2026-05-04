import { Component, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { BackCancelService } from '@app/billing/services/back-cancel.service';
import { Encounter, GetClaimByIdentifier } from '@core/models/billing';
import { ConfirmDialog, NotifyDialog } from '@core/models/common';
import { ClaimService } from '@core/services/billing';
import { AppRouteChangeService } from '@core/services/common';
import { take } from 'rxjs';
import { Location } from '@angular/common';
import { ClaimStatus } from '@core/enums/billing/claim-status';
import { ClaimListingTab } from '@core/enums/billing/claim-listing-tab';
import { Breadcrumb } from '@core/models/billing/bread-crumb';
import { MatTabChangeEvent } from '@angular/material/tabs';
import { ClearingHouseClaimModel } from '@core/models/billing/claims-submit-model';
import { MatDialog } from '@angular/material/dialog';
import { ViewChild, TemplateRef } from '@angular/core';
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";
import { AccountMemberSettings } from '@core/services/account/account-member.service';
import { AuthService } from '@core/services/sso/auth.service';

export enum EditClaimTab {
  ClaimDetails = 1,
  ChargeDetails,
  Appointments,
  ScrubbingRules,
  History,
  Attachments,
}
 
@Component({
  selector: 'app-encounter-view',
  templateUrl: './encounter-view.component.html',
  styleUrls: ['./encounter-view.component.css'],
  encapsulation: ViewEncapsulation.None
})
export class EncounterViewComponent {
  @ViewChild('ediDialogTemplate') ediDialogTemplate!: TemplateRef<any>;
  public claim: Encounter;
  public claimIdentifier: string;
  private headerText = "";
  private basicHeaderText = "Claim - ";
  private selectedTabId = 0;
  private selectedTab: EditClaimTab | null = EditClaimTab.ClaimDetails;
  public errorTabName = "Errors and Alerts";
  status = 'Processed';
  private claimListingTabIndex: number;
  notifyDialog: NotifyDialog = new NotifyDialog(false, '', '');
  breadCrumbs: Breadcrumb[] = [];
  public formattedEdiContent: string = '';
 
  public cancelChangesConfirmation = new ConfirmDialog(false, "Warning",
    "Any changes you've made will not be saved. Are you sure you want to Cancel?");
 
  public accountInfo: GetClaimByIdentifier;
  public memberDetails: AccountMemberSettings | null = null;
 
  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly claimsService: ClaimService,
    private readonly appRouteService: AppRouteChangeService,
    private readonly backCancelService: BackCancelService,
    private _location: Location,
    private dialog: MatDialog,
    private notificationService: NotificationHandlerService,
    private authSvc: AuthService
  ) {
    this.route.params.subscribe((x) => {
      if (x["id"]) {
        this.claimIdentifier = x["id"].toString();
        this.selectedTabId = Number(x["tab"]) - 1;
        this.claimsService.setTabId(this.selectedTabId);
        this.buildBreadcrumbs();
        this.claimsService.setOldFilter(true);
        this.claimsService
          .Get(this.claimIdentifier)
          .pipe(take(1))
          .subscribe((result: Encounter) => {
            if (result) {
              this.claim = result;
              this.errorTabName =
                this.getErrorTabNameByClaimStatus(
                  this.claim.status
                );
              this.claimListingTabIndex =
                this.getClaimListingTabByStatus(
                  this.claim.status,
                  this.claim.isFlagged
                ) - 1;
            } else {
              this.router.navigate(["/billing/not-found"]);
            }
          },
            (error) => {
              this.router.navigate(['/billing/claims/list']);
            });
 
        this.headerText = `${this.basicHeaderText} ${this.claimIdentifier}`;
        this.appRouteService.editTitle.emit(this.headerText);
      }
    });
    this.route.queryParams.subscribe(x => {
      if (x["tab"] !== undefined && x["tab"] !== null) {
        this.selectedTabId = +x["tab"];
      }
    });
 
    this.backCancelService.backCancelEmmiter.subscribe((allowBackCancel => {
      this.cancelButtonEventHandler(!allowBackCancel);
    }))

    this.resetSettings();
  }

   resetSettings() {
     this.memberDetails = this.authSvc.getUserData();
   }

  public isRethinkAdminUser(): boolean {
    return !!this.memberDetails?.impersonationUserName;
  }
 
  public cancelButtonEventHandler(isFormWasChanged: boolean): void {
    if (isFormWasChanged) {
      this.cancelChangesConfirmation.opened = true;
    } else {
      this.router.navigate(["/billing/claims"]);
    }
  }
 
 
 
  selectedTabChanged(event: MatTabChangeEvent) {
    const label = event.tab.textLabel?.trim();
    switch (label) {
      case 'Claim Details':
        this.selectedTab = EditClaimTab.ClaimDetails;
        break;
 
      case 'Appointments':
        this.selectedTab = EditClaimTab.Appointments;
        break;
 
      case 'History':
        this.selectedTab = EditClaimTab.History;
        break;
 
      default:
        this.selectedTab = null;
    }
    this.buildBreadcrumbs();
  }
 
  getErrorTabNameByClaimStatus(claimStatus: ClaimStatus): string {
    switch (claimStatus) {
      case ClaimStatus.RejectedClearinghouse:
      case ClaimStatus.RejectedFunder:
        return "Reject Reason";
      case ClaimStatus.Denied:
        return "Denial Reason";
      default:
        return "Errors and Alerts";
    }
  }
 
  getClaimListingTabByStatus(claimStatus: ClaimStatus, isFlagged: boolean) {
    if (isFlagged) return ClaimListingTab.Flagged;
 
    switch (claimStatus) {
      case ClaimStatus.PendingReview:
        return ClaimListingTab.PendingReview;
      case ClaimStatus.Void:
      case ClaimStatus.ReadyToBill:
      case ClaimStatus.Rebill:
      case ClaimStatus.BillNextFunder:
        return ClaimListingTab.ReadyToBill;
      case ClaimStatus.Billed:
      case ClaimStatus.Pending:
      case ClaimStatus.Paid:
        return ClaimListingTab.BillingPending;
      case ClaimStatus.Closed:
        return ClaimListingTab.Closed;
      case ClaimStatus.RejectedClearinghouse:
      case ClaimStatus.RejectedFunder:
        return ClaimListingTab.Rejected;
      case ClaimStatus.Denied:
        return ClaimListingTab.Denied;
      default:
        return ClaimListingTab.PendingReview;
    }
  }
 
  private buildBreadcrumbs() {
    this.breadCrumbs = [
      { label: 'Claims', url: '/billing/claims/list' },
      { label: this.findXLabelName(), url: '/billing/claims/list' },
      {
        label: this.selectedTab
          ? (EditClaimTab[this.selectedTab]) == "ClaimDetails" ? "Claim Details" : EditClaimTab[this.selectedTab]
          : '',
        url: '/billing/claims/list'
      }
    ];
  }
 
  findXLabelName() {
    switch (this.selectedTabId) {
      case 0:
        return 'Pending Review';
      case 1:
        return 'Ready to Bill';
      case 2:
        return 'Billing Pending';
      case 3:
        return 'Completed';
      case 4:
        return 'Rejected';
      case 5:
        return 'Denied';
      case 6:
        return 'Flagged';
    }
  }

  onDownloadClick(): void {
    const ediModel: ClearingHouseClaimModel = {
      claimId: this.claim.id,
      clearinghouseId: 0,
      isSecondary: false,
      adjustmentLevel: 1
    };
    this.claimsService.generateEdi(ediModel).subscribe({
      next: (ediString: string) => {
        this.formattedEdiContent = ediString.replace(/~/g, '~\n');
        this.dialog.open(this.ediDialogTemplate, {
          width: '80vw',
          maxHeight: '80vh',
        });
      },
      error: (error) => this.notificationService.showNotificationError('Unable to generate EDI. Please try again.')
    });
  }

  copyEdiToClipboard(): void {
    navigator.clipboard.writeText(this.formattedEdiContent).then(() => {
      this.notificationService.showNotificationSuccess('EDI content copied to clipboard.');
    }).catch(() => {
      this.notificationService.showNotificationError('Failed to copy EDI content.');
    });
  }

}
