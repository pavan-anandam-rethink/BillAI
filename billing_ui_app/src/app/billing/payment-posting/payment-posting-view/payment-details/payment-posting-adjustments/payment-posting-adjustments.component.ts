import {Component, EventEmitter, Input, NgZone, OnDestroy, OnInit, Output, ViewChild} from '@angular/core';
import {ClaimDetailsListFilterSort} from '@core/models/billing';
import {ClaimPostingService, ClaimService, AdjustmentService} from '@core/services/billing';
import {GridComponent, GridDataResult, RowClassArgs} from '@progress/kendo-angular-grid';
import {process, SortDescriptor, State} from '@progress/kendo-data-query';
import {Observable, Subject, Subscription, forkJoin} from 'rxjs';
import {map, take, tap, first, takeUntil} from 'rxjs/operators';
import {PaymentPostingAdjustmentsDetailsComponent} from "../payment-posting-adjustments-details";
import { SidebarService } from '@app/shared/components/sidebar';
import { ClaimPostingDetailsSubject } from '@core/subjects/claim-posting-details.subject';
import { WriteOffClaimDialogComponent, WriteOffClaimDialogResult } from '@app/billing/encounters/ecnounter-list/write-off-claim-dialog/write-off-claim-dialog.component';
import { DialogService, DialogCloseResult } from '@progress/kendo-angular-dialog';
import { ClaimOrChargeToWriteOff } from '@core/models/billing/write-off-claim-model';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { AccountPermissions } from '@core/enums/account/account-permissions';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { WriteoffService } from '@core/services/billing/writeoff.service';

@Component({
    selector: 'payment-posting-adjustments',
    templateUrl: './payment-posting-adjustments.html',
    styleUrls: ['./payment-posting-adjustments.css']
})
export class PaymentPostingAdjustmentsComponent implements OnInit, OnDestroy {
    _paymentClaimId: number;
    @Input() set paymentClaimId(value: number) {
        this._paymentClaimId = value;
        this.redrawGrid();
    }

    _claimId: number;
    @Input() set claimId(value: number) {
        this._claimId = value;
        this.redrawGrid();
    }

    @Output() updateClaim = new EventEmitter<number>();
    @Output() adjustmentClick = new EventEmitter();
    @Input() writeOffEvent: Observable<void>;
    @Input() printType: string;
    @Input() isErrors: boolean;
    @Input() isManual: boolean;
    @Input() paymentPostingId: number;
    @Input() claimStatus: string;

    private unsubscribe = new Subject<void>();
    private writeOffEventSubscription: Subscription;

    @ViewChild(GridComponent) grid: GridComponent;

    ClaimPostingDetailsSubject: ClaimPostingDetailsSubject;
    view: Observable<GridDataResult>

    selectedIds: number[] = [];
    gridView: Observable<GridDataResult>;
    gridState: ClaimDetailsListFilterSort = new ClaimDetailsListFilterSort();
    canEdit: boolean;
    private reasonCodesLoaded = false;

    constructor(
        private ngZone: NgZone,
        private claimPostingService: ClaimPostingService,
        private sidebarService: SidebarService,
        private dialogService: DialogService,
        private writeOffService: WriteoffService,
        private accountService: AccountMemberService,
        private notificationService: NotificationHandlerService,
        private adjustmentService: AdjustmentService
    ) {
    }

    ngOnInit(): void {
        this.gridState.skip = 0;
        this.gridState.take = 1000;

        // Preload reason codes when component initializes (on row expand)
        this.preloadReasonCodes();

        this.sidebarService.adjustmentChanged$.pipe(takeUntil(this.unsubscribe))
            .subscribe((data: number) => {
                if(data == this._paymentClaimId){
                    this.sidebarService.closeAll();
                    // Removed: this.updateClaim.emit(data) - causes duplicate GetClaims call
                    // updateClaim is already emitted by onUpdate event chain
                    // This subscription is only needed to close the sidebar
                }
            })
        if(this.writeOffEvent)
            this.writeOffEventSubscription = this.writeOffEvent.subscribe(() =>{
                this.redrawGrid();
        });
        this.accountService
            .accountMemberSettings
            .subscribe((x) => {
                if (x) {
                    this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingEditApprovedAppointments);
                }
            });

    }

    redrawGrid() {
        this.gridState.claimId = this._paymentClaimId;

        if (this.gridState.claimId) {
            this.ClaimPostingDetailsSubject = new ClaimPostingDetailsSubject(this.claimPostingService);
            this.view = this.ClaimPostingDetailsSubject.pipe(
                map(data => {
                        return process(data, this.gridState)
                    },
                    tap(() => setTimeout(() => this.fitColumns(), 250)))
            );
            this.loadData(this.gridState);

        }
    }

    onSortChange(sortParams: SortDescriptor[]): void {
        this.gridState.sortingModels = sortParams;
        this.loadData(this.gridState);
    }

    onStateChange(state: State): void {
        this.gridState.skip = state.skip;
        this.gridState.take = state.take;
        this.gridState.sortingModels = state.sort;
        this.ClaimPostingDetailsSubject.sync();
    }


    loadData(params: ClaimDetailsListFilterSort): void {
        this.ClaimPostingDetailsSubject.getAll(params);
    }

    fitColumns(): void {
        this.ngZone.onStable.asObservable().pipe(take(1)).subscribe(() => {
            this.grid.autoFitColumns(this.grid.columnList.toArray());
            this.grid.autoFitColumns();
        });
    }

    preloadReasonCodes(): void {
        if (!this.reasonCodesLoaded) {
            this.reasonCodesLoaded = true;
            // Preload both adjustment and write-off reason codes in parallel
            forkJoin({
                adjustmentCodes: this.adjustmentService.getAdjustmentReasonDescriptions(''),
                writeOffCodes: this.writeOffService.getReasonCodesWithDescriptions()
            })
            .pipe(takeUntil(this.unsubscribe))
            .subscribe({
                next: (result) => {
                    // Codes are now cached in the services/components that need them
                    console.log('Reason codes preloaded for service line details');
                },
                error: (error) => {
                    console.error('Error preloading reason codes:', error);
                }
            });
        }
    }

    ngOnDestroy() {
        //unsubscribe from all here!
        this.ClaimPostingDetailsSubject && (this.ClaimPostingDetailsSubject.unsubscribe());
        this.writeOffEventSubscription.unsubscribe();
    }

    ngAfterViewInit() {
        this.fitColumns();
    }

    lastClickedId: number;

    adjustmentClicked(event: any) {
        if (event.id && !this.isErrors) {

            this.selectedIds.remove(event.id);
            if (this.lastClickedId !== event.id) {
                this.lastClickedId = event.id;
                this.sidebarService.openRight(PaymentPostingAdjustmentsDetailsComponent, true, "md",true).subscribe(rsidebarRef => {
                    rsidebarRef.instance.setData(event.id, this._paymentClaimId, this._claimId, this.isManual,false,this.claimStatus,this.paymentPostingId);
                    /*prevents doudle click on the same elemets : TODO*/
                    this.sidebarService.rightSidebarComponentRef.instance.onClose.pipe(first()).subscribe(x => {
                        this.lastClickedId = 0;
                    });

                    rsidebarRef.instance.onClose.subscribe(x => {
                        this.sidebarService.rightSidebarComponentRef.instance.close();
                    });
    
                    rsidebarRef.instance.onUpdate.subscribe((x: number) => {
                        this.ClaimPostingDetailsSubject.updateServiceLine(x)
                        let claimId = this.ClaimPostingDetailsSubject.getClaimId(x);
                        this.updateClaim.emit(claimId);
                    });
                });
            }
        }
    }

    writeOffCharge(event: any) {
        if (event.id)
        {
            var dialogRef = this.dialogService.open({
                content: WriteOffClaimDialogComponent,
                title: "Writeoff Claim",
                width: 540,
            });

            let writeOffchargeModel :ClaimOrChargeToWriteOff = {
                claimId : this._claimId,
                chargeId : event.chargeEntryId,
                balanceAmount : event.balance
            }

            dialogRef.content.instance.claimsOrChargeToWriteOff = [writeOffchargeModel];
            dialogRef.content.instance.isServiceLine = true;

            dialogRef.result.subscribe((result: any) => {
                if (result && result.data && result.data.length > 0) {
                    this.writeOffService.writeOffClaim(result.data.first()).subscribe(
                        (response) => {
                            if(response.success)
                            {
                                this.updateClaim.emit(event.claimId);
                                this.notificationService.showNotificationSuccess("Claim has been Written-off.");
                            }
                            else
                            {
                                this.notificationService.showNotificationError("This Claim could not be written off. Write off amount exceeds the claim balance.");
                            }
                            this.loadData(this.gridState);
                        }
                    );
                }
            });
        }
    }

    public rowCallback = (context: RowClassArgs) => {
        const hasErrors = context.dataItem.hasErrors;
        return {
            hasErrors: hasErrors && this.isErrors
        };
    }
}