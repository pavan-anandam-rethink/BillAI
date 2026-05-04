import { Component, EventEmitter, Input, OnChanges, OnDestroy, Output, SimpleChanges } from "@angular/core";
import { CdkDragDrop, moveItemInArray } from "@angular/cdk/drag-drop";
import { ClaimDetailsModel, ClaimUpdateModifiersModel } from "@core/models/billing";
import { Adjustment } from "@core/models/billing/adjustment";
import { takeUntil } from "rxjs/operators";
import { Subject } from "rxjs";
import { AccountPermissions } from "@core/enums/account";
import { AdjustmentService, ClaimService } from "@core/services/billing";
import { SidebarService } from "@app/shared/components/sidebar";
import { WriteoffService } from "@core/services/billing/writeoff.service";
import { EditWriteOffModelWithUserInfo, GetChargeEntryWriteOffModel, WriteOffChargeEntryModel, WriteOffDetailsModel, WriteOffReasonCodDescriptionModel } from "@core/models/billing/write-off-charge-entry-model";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { ConfirmDialog } from "@core/models/common";
import { NotificationService } from "@progress/kendo-angular-notification";
import { GetChargeDetails } from "@core/models/billing/get-charge-details";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";

@Component({
    selector: 'billing-code-mods',
    templateUrl: './billing-code-mods.component.html',
    styleUrls: ['./billing-code-mods.component.css',]
})

export class BillingCodeModsComponent implements OnDestroy, OnChanges {
    @Input() lineData: ClaimDetailsModel;
    @Output() onUpdate = new EventEmitter();

    private unsubscribe = new Subject<void>();


    public modifiersArr: string[] = [];
    public modifiersCheckboxesArr: boolean[] = [];
    public origModifiersArrLength = 0;
    public modifierOrig = '';
    public modifierCheckboxOrig = false;
    public needToBeSaved = false;
    public editingModifierId = -1;

    canEdit: boolean;

    adjustments: Adjustment[];
    writeOffsOrig: WriteOffChargeEntryModel[] = [];
    writeOffs: WriteOffChargeEntryModel[];
    allowedReasonCodes : WriteOffReasonCodDescriptionModel[] = [];

    deleteConfirmation = new ConfirmDialog(false, "Confirmation", "Are you sure you want to delete this write off?");
    writeOffToDeleteId: number;

    editedWriteOffIds: number[] = [];
    isDuplicateFound: boolean;

    constructor(private adjustmentService: AdjustmentService, private writeOffService: WriteoffService, private claimsService: ClaimService,
        private sidebarService: SidebarService,
        private notificationService: NotificationHandlerService,
        private accountService: AccountMemberService) {
            this.accountService
            .accountMemberSettings
            .subscribe((x) => {
                if (x) {
                    this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingEdit);
                }
            });
    }



    calculateAdjustmentsTotal() {
        var totalAdjustments =  this.adjustments == undefined ?
            0 : this.adjustments.where(x => x.isPositive).sum((x: Adjustment) => x.amount);
        totalAdjustments -= this.adjustments == undefined ?
        0 : this.adjustments.where(x => !x.isPositive).sum((x: Adjustment) => x.amount);
        return totalAdjustments;
    }

    calculateWriteOffsTotal() {
        return this.writeOffs == undefined ?
            0 : this.writeOffs.sum((x: WriteOffChargeEntryModel) => x.writeOffAmount);
    }

    getRemainingBalance(id: number) {
        var totalWriteOffAmount = this.writeOffs == undefined ?
            0 : this.writeOffs.where(x => x.id != id).sum((x: WriteOffChargeEntryModel) => x.writeOffAmount);
        return this.lineData.billedAmount - this.lineData.paymentAmount 
        + this.calculateAdjustmentsTotal() - totalWriteOffAmount;
    }

    removeLeadingDigits(value: number) {
        return value ? parseFloat(value.toFixed(2)) : value;
    }

    drop(event: CdkDragDrop<string[]>) {
        moveItemInArray(this.modifiersArr, event.previousIndex, event.currentIndex);
    }

    loadAdjustments() {
        let GetChargeDetailsModel : GetChargeDetails = { id : this.lineData.id, isServiceLine: false};
        this.adjustmentService.getServiceLineAdjustmentsByChargeId(GetChargeDetailsModel)
            .pipe(takeUntil(this.unsubscribe))
            .subscribe(x => {
                this.adjustments = x;
            });
    }

    loadWriteOffs() {
        this.writeOffService.getChargeEntryWriteOffsByChargeId(this.lineData.id, false)
            .pipe(takeUntil(this.unsubscribe))
            .subscribe(x => {
                this.writeOffs = x;
                this.writeOffs.forEach(x => x.writeOffAmountOrig = x.writeOffAmount);
            });
    }

    ngOnDestroy() {
        this.unsubscribe.next();
        this.unsubscribe.complete();
    }

    ngOnChanges(_changes: SimpleChanges) {
        if (this.lineData != null) {
            if (this.lineData.modifier1 !== null && this.lineData.modifier1.trim().length > 0) {
                this.modifiersArr.push(this.lineData.modifier1);
                this.modifiersCheckboxesArr.push(this.lineData.includeOnClaimMod1);

                if (this.lineData.modifier2 !== null && this.lineData.modifier2.trim().length > 0) {
                    this.modifiersArr.push(this.lineData.modifier2);
                    this.modifiersCheckboxesArr.push(this.lineData.includeOnClaimMod2);

                    if (this.lineData.modifier3 !== null && this.lineData.modifier3.trim().length > 0) {
                        this.modifiersArr.push(this.lineData.modifier3);
                        this.modifiersCheckboxesArr.push(this.lineData.includeOnClaimMod3);

                        if (this.lineData.modifier4 !== null && this.lineData.modifier4.trim().length > 0) {
                            this.modifiersArr.push(this.lineData.modifier4);
                            this.modifiersCheckboxesArr.push(this.lineData.includeOnClaimMod4);
                        }
                    }
                }
            }

            this.origModifiersArrLength = this.modifiersArr.length;

            this.loadAdjustments();
            this.loadWriteOffs();
        }
    }

    ngOnInit(){
        this.writeOffService.getReasonCodesWithDescriptions().subscribe(res => {
            this.allowedReasonCodes = res;
        });
    }

    isWriteOffEdited(id: number) {
        return this.editedWriteOffIds.indexOf(id) > -1;
    }

    startEditWriteOff(id: number) {
        this.editedWriteOffIds.push(id);
        let editedWriteOff = this.writeOffs.find((x: WriteOffChargeEntryModel) => x.id == id)
        this.writeOffsOrig.push(Object.assign({}, editedWriteOff));
    }

    deleteWriteOff(id: number) {
        this.writeOffToDeleteId = id;
        this.deleteConfirmation.opened = true;
    }

    acceptDeleteWriteOff(isAccepted: boolean) {
        if (isAccepted) {
            this.writeOffService.deleteChargeEntryWriteOff([this.writeOffToDeleteId])
                .pipe(takeUntil(this.unsubscribe))
                .subscribe(x => {
                    var deletedWriteOff = this.writeOffs.find((x: WriteOffChargeEntryModel) => x.id == this.writeOffToDeleteId)
                    this.writeOffs.removeWhere((x: WriteOffChargeEntryModel) => x.id === this.writeOffToDeleteId);
                    // this.updateServiceLine();
                    this.lineData.balanceAmount += deletedWriteOff.writeOffAmount;
                    this.lineData.adjustmentAmount += deletedWriteOff.writeOffAmount;
                    this.onUpdate.emit();
                    this.notificationService.showNotificationSuccess("Write off deleted successfully.");
                });
        }
    }

    saveEditWriteOff(id: number) {
        let editedWriteOff = this.writeOffs.find((x: WriteOffChargeEntryModel) => x.id == id);

        if (editedWriteOff != undefined && editedWriteOff.writeOffAmount != undefined) {
            const editWriteOff: EditWriteOffModelWithUserInfo = {
                claimId: this.lineData.claimId,
                writeOffDetails: [{
                    chargeEntryWriteOffId: editedWriteOff.id,
                    writeOffReasonCodeId: editedWriteOff.writeOffReasonCodeId,
                    writeOffAmount: editedWriteOff.writeOffAmount
                }],
                AccountInfoId: 0,
                MemberId: 0
             };
            this.writeOffService.updateChargeEntryWriteOff(editWriteOff)
                .pipe(takeUntil(this.unsubscribe))
                .subscribe(x => {
                    if (editedWriteOff != undefined) {
                        this.editedWriteOffIds.remove(id);
                        this.writeOffsOrig.removeWhere((x: WriteOffChargeEntryModel) => x.id == id);
                        editedWriteOff.dateLastModified = x.dateLastModified;
                        var amountDiff = editedWriteOff.writeOffAmount - editedWriteOff.writeOffAmountOrig
                        this.lineData.balanceAmount -= amountDiff;
                        this.lineData.adjustmentAmount -= amountDiff;
                        editedWriteOff.writeOffAmountOrig = editedWriteOff.writeOffAmount;
                        // this.updateServiceLine();
                        // this.onUpdate.emit(this.serviceLineId);
                        this.onUpdate.emit();
                        this.notificationService.showNotificationSuccess("Write off updated successfully.");
                    }
                });
        }
    }

    cancelEditWriteOff(id: number) {
        this.editedWriteOffIds.remove(id);
        let editedWriteOffOrig = this.writeOffsOrig.find((x: WriteOffChargeEntryModel) => x.id == id);
        let editedWriteOff = this.writeOffs.find((x: WriteOffChargeEntryModel) => x.id == id);

        if (editedWriteOff != undefined && editedWriteOffOrig != undefined) {

            editedWriteOff.writeOffReasonCodeId = editedWriteOffOrig.writeOffReasonCodeId;
            editedWriteOff.writeOffAmount = editedWriteOffOrig.writeOffAmount;
            editedWriteOff.description = editedWriteOffOrig.description;

            this.writeOffsOrig.remove(editedWriteOffOrig);
        }
    }

    updateWriteOffReasonCode(reasonCodeId: number, writeoff: WriteOffChargeEntryModel) {
        writeoff.writeOffReasonCodeId = reasonCodeId;
    }

    // updateServiceLine() {
    //     this.claimPostingService.getPaymentClaimServiceLine(this.serviceLineId)
    //         .pipe(takeUntil(this.unsubscribe))
    //         .subscribe(x => {
    //             this.serviceLine = x;
    //             this.onUpdate.emit(this.serviceLineId);
    //         });
    // }
}
