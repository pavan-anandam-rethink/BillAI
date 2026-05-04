import { Component, EventEmitter } from "@angular/core";
import { Router } from "@angular/router";
import { AccountPermissions } from "@core/enums/account/account-permissions";
import { ClaimErrorAlertModel, MessageDescription } from "@core/models/billing/claim-errors-alerts";
import { ClaimValidationModel } from "@core/models/billing/claim-validation-model";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { ClaimService } from "@core/services/billing";
import { NotificationHandlerService } from "@core/services/common/notification-handler.service";
import { TakeUntilDestroyService } from "@core/services/common/takeuntill-destroy.service";
import { Subject, takeUntil } from "rxjs";
import { ClaimListingTab } from "@core/enums/billing/claim-listing-tab";


@Component({
    selector: 'app-encounter-errors-alerts',
    templateUrl: './encounter-errors-alerts.component.html',
    styleUrls: ['./encounter-errors-alerts.component.css']
})

export class EncounterErrorsAlertsComponent {
    errorsArr: ClaimErrorAlertModel[] = [];
    alertsArr: ClaimErrorAlertModel[] = [];
    responsesArr: ClaimErrorAlertModel[] = [];

    tabType = '';
    claimId: number;
    claimStatusName: string = '';
    listingTab: ClaimListingTab | null = null;
    canEdit = false;
    private unsubscribeAll$ = new Subject<void>();
    public validationPassed = new EventEmitter();
    ClaimIdentifier: any;
    selectedTabIndex: number = 0;
    responseData: ResponseModel[] = [];

    constructor(private claimService: ClaimService,
        private accountService: AccountMemberService,
        private notificationService: NotificationHandlerService,
        private destroyService: TakeUntilDestroyService) {
        this.accountService
            .accountMemberSettings
            .subscribe((x) => {
                if (x) {
                    this.canEdit = this.accountService.checkPermissionLevel(AccountPermissions.BillingEdit);
                }
            });
    }

    setData(parentData: any, tabType: 'Error' | 'Alert' | 'Response', listingTab?: ClaimListingTab | null) {
        this.tabType = tabType;
        this.ClaimIdentifier = parentData.claimIdentifier == undefined ? parentData.claimNumber : parentData.claimIdentifier;
        this.claimId = parentData.claimId == undefined ? parentData.id : parentData.claimId;
        this.claimStatusName = parentData.claimStatusName || '';
        this.listingTab = listingTab || null;

        switch (tabType) {
            case 'Error':
                this.selectedTabIndex = 0;
                this.IsShowRunValidation = true;
                break;
            case 'Alert':
                this.selectedTabIndex = 1;
                this.IsShowRunValidation = true;
                break;
            case 'Response':
                this.selectedTabIndex = 2;
                this.IsShowRunValidation = false;
                break;
        }

        this.claimService.getClaimErrorsAndAlerts(this.claimId).subscribe(
            (claimsErrors) => {
                this.errorsArr = claimsErrors.filter((x) => x.type === "Error");
                this.alertsArr = claimsErrors.filter((x) => x.type === "Alert");
                this.responsesArr = claimsErrors.filter((x) => x.type === "Response");

                this.responsesArr.forEach(item => {
                    item.messageDescription = [];
                    if (item.codeDescription.length > 1) {
                        item.messageDescription.push({ description: item.codeDescription[1], message: "" });
                    }
                    item.messageDescription.push({ description: item.codeDescription[0], message: item.message });
                });

                var secondaryErrors = this.responsesArr.filter(x => !(x.refValidationId == null || x.refValidationId == 0));
                secondaryErrors.forEach(item => {
                    this.responsesArr.filter(x => x.id == item.refValidationId)[0].messageDescription.push({ description: item.codeDescription[0], message: item.message });
                    this.responsesArr = this.responsesArr.filter(x => x.id != item.id);
                });

                this.responsesArr.sort((a, b) => new Date(b.responseDate).getTime() - new Date(a.responseDate).getTime());

                var batchIds = []; this.responsesArr.forEach(function (x) { if (!batchIds.includes(x.batchId)) batchIds.push(x.batchId); });
                this.responseData = [];
                batchIds.forEach(ele => {
                    var obj = new ResponseModel();
                    obj.batchId = ele;
                    obj.identifierNo = parseInt(obj.batchId.substring(13, obj.batchId.length));
                    var data = this.responsesArr.filter(x => x.batchId === ele);
                    obj.data = data.sort((a, b) => a.fileType.localeCompare(b.fileType));
                    this.responseData.push(obj);
                });
                this.responseData.sort((a, b) => b.identifierNo - a.identifierNo);
            }
        );
    }

    reRunValidation() {
        if (this.claimId) {
            const validateModel: ClaimValidationModel = {
                Id: this.claimId,
                isSecondary: false,
                secondaryFunderId: undefined,
                AccountInfoId: this.accountService.memberDetails.accountInfoId,
                MemberId: this.accountService.memberDetails.memberId
            };
            this.claimService.ReRunValidation(validateModel)
                .pipe(takeUntil(this.destroyService.destroy))
                .subscribe(() => {
                    this.setData({ claimIdentifier: this.ClaimIdentifier, claimId: this.claimId }, this.selectedTabIndex == 0 ? 'Error' : this.selectedTabIndex == 1 ? 'Alert' : 'Response');
                    this.notificationService.showNotificationSuccess('Claim Validated successfully');
                },
                    () => {
                    },
                    () => {
                        this.validationPassed.emit();
                    });
        }
    }

    IsShowRunValidation: boolean = true;
    selectedTabChanged(id: number) {
        switch (id) {
            case 0:
                this.IsShowRunValidation = true;
                break;
            case 1:
                this.IsShowRunValidation = true;
                break;
            case 2:
                this.IsShowRunValidation = false;
                break;
            default:
                this.IsShowRunValidation = true;
        }
    }

}


class ResponseModel {
    public batchId: string;
    public identifierNo: number;
    public data: ClaimErrorAlertModel[];
}

