import { Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges, ViewChild } from "@angular/core";
import { ClaimService } from "@core/services/billing";
import { Subject, async } from "rxjs";
import { takeUntil } from "rxjs/operators";
import { GridComponent, GridDataResult } from '@progress/kendo-angular-grid';
import { CurrencyPipe } from "@angular/common";
import { ClaimDetailsListFilterSort, ClaimDetailsModel } from "@core/models/billing";
import { ClaimChargeDetailsShared } from "../../shared/claim-charge-details/claim-charge-details-shared";
import { ConfirmDialog } from "@core/models/common";
import { SidebarService } from "@app/shared/components/sidebar";
import { BillingCodeComponent } from "./billing-code/billing-code.component";
import { ChargeNotesComponent } from "../../encounter-view/encounter-details/encounter-details-charge-detail-summary/charge-notes/charge-notes.component";
import { ActivatedRoute, Router } from "@angular/router";
import { NotificationService } from "@progress/kendo-angular-notification";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { AccountPermissions } from "@core/enums/account/account-permissions";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";
import { CarcCodes } from "../../../../core/models/billing/carc-codes";

@Component({
    selector: 'encounter-list-details',
    templateUrl: './encounter-list-details.component.html',
    styleUrls: ['./encounter-list-details.component.css',]
})
export class EncounterListDetailsComponent implements OnDestroy, OnChanges {
    @Input() claimId: number;
    @Input() manualClaim: boolean
    @Output() onUpdateEvent = new EventEmitter();
    @ViewChild("encounterDetailsGrid") detailsGrid: GridComponent;

    private unsubscribeAll$ = new Subject<void>();
    private lineId: number;

    onLineDeleteConfirmDialog = new ConfirmDialog(false, "Delete Charge Detail Line",
        "Are you sure you'd like to perform this action?", "Delete", "Cancel");

    view: GridDataResult = {
        data: [],
        total: 0
    };
    readonly claimChargeDetailsShared: ClaimChargeDetailsShared;
    canEdit: boolean;
    rejectedid: number;
    carcCodes: CarcCodes[];

    //remove when distinction between manual and auto-created claims will be implemented
    // manualClaim = false;

    constructor(private claimsService: ClaimService, private sidebarService: SidebarService,
        private notificationService: NotificationHandlerService,
        private currencyPipe: CurrencyPipe, private notificationHandler: NotificationHandlerService,
        private router: Router,
        private readonly route: ActivatedRoute, private accountService: AccountMemberService) {
        this.claimChargeDetailsShared = new ClaimChargeDetailsShared(claimsService, currencyPipe, router, route, accountService, notificationHandler);
        this.claimChargeDetailsShared.manualClaim = this.manualClaim;
        if (this.claimId > 0) {
            this.loadData();
        }
        this.claimChargeDetailsShared.onUpdateEvent.subscribe(x => {
            this.onUpdateEvent.emit();
        });
        this.accountService
            .accountMemberSettings
            .subscribe((x) => {
                if (x) {
                    this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingEdit);
                }
            });
      this.claimsService.getId().subscribe(id => {
        if (id !== null) {
          this.rejectedid = id
        }
      });
      this.claimsService.getCarcCode().subscribe(carcCode => {
        if (carcCode !== null) {
          this.carcCodes = carcCode;
        }
      });
    }

    showChargeInfo(item: ClaimDetailsModel) {
        this.sidebarService.openRight(BillingCodeComponent, true, "md").subscribe(rsidebarRef => {
            rsidebarRef.instance.setData(item.id, item, this.claimId);
            rsidebarRef.instance.updateLineEmitter.subscribe(() => {
                this.updateLines();
            });

            this.sidebarService.getUpdateEmitter().pipe(takeUntil(this.unsubscribeAll$))
                .subscribe(() => {
                    this.loadData();
                })

            this.unsubscribeAll$.subscribe(_x => this.sidebarService.closeAll());
        });
    }

    updateLines() {
        this.claimChargeDetailsShared.chargeDetailsData = this.view.data;
        this.claimChargeDetailsShared.updateLines();
    }

    removeLine() {
        this.claimsService.removeBillingClaimDetail(this.lineId).pipe(takeUntil(this.unsubscribeAll$))
            .subscribe(_result => {
                this.notificationHandler.showNotificationSuccess("Charge has been deleted.")
             }, _error => {
                this.notificationHandler.showNotificationError("Charge has not been deleted.")
              }, () => {
                this.loadData();
                this.onUpdateEvent.emit();
            });
    }

    showLineNotesSidebar(item: ClaimDetailsModel) {
        this.sidebarService.openRight(ChargeNotesComponent, true, "md").subscribe(rsidebarRef =>
            rsidebarRef.instance.setChargeData(item)
        );
    }

    openDeleteLineDialog(lineId: number): void {
        this.onLineDeleteConfirmDialog.opened = true;
        this.lineId = lineId;
    }

    loadData(): void {
        const params = new ClaimDetailsListFilterSort();
        params.claimId = this.claimId;
        //to mark be that we ignore paggination settings
        params.take = 0;

        this.claimsService.getBillingClaimDetails(params).pipe(takeUntil(this.unsubscribeAll$))
            .subscribe(result => {
                this.view.data = result;
                this.view.total = result.length > 0 ? result[0].totalCount : 0;
                this.claimChargeDetailsShared.fillArrayForm(this.view.data);
            });
    }

    ngOnDestroy() {
        this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }

    ngOnChanges(_changes: SimpleChanges) {
        if (this.claimId > 0) {
            this.claimChargeDetailsShared.claimId = this.claimId;
            this.loadData();
        }
    }

  getCarcDescription(code: string): string {
    const match = this.carcCodes.find(c => c.code === code.trim());
    return match ? match.description : 'No description available';
  }
}
