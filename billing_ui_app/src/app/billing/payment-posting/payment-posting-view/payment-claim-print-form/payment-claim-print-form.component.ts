import {Component, Input, OnInit, EventEmitter} from '@angular/core';
import {ClaimEOBInfo, ClientPrintData, PaymentClaimServiceLine, PaymentEOBInfo} from '@core/models/billing';
import { PaymentPostingPrintModel } from '@core/models/billing/cliam-posting';
import { IdsWithUserInfo } from '@core/models/billing/get-claim-by-identifier';
import { AccountMemberService } from '@core/services/account/account-member.service';
import {ClaimPostingService} from '@core/services/billing';

@Component({
    selector: 'payment-claim-print-form',
    templateUrl: './payment-claim-print-form.html',
    styleUrls: ['./payment-claim-print-form.css',
        '../../status-actions.css']
})
export class PaymentClaimPrintFormComponent implements OnInit {
    @Input() payment: PaymentEOBInfo;
    @Input() claims: ClaimEOBInfo[];
    @Input() showErrors = false;
    currentDate = new Date();
    // companyInfo$ = new EventEmitter<ClientPrintData>();
    companyInfo$: any;
    private idsWithUserInfoReq: IdsWithUserInfo = {AccountInfoId: this.accountService.memberDetails.accountInfoId, MemberId: this.accountService.memberDetails.memberId, Ids: []};


    constructor(
        private claimPostingService: ClaimPostingService,
        private accountService: AccountMemberService
    ) {

    }

    ngOnInit(): void {
        //this.companyInfo$ = this.claimPostingService.GetCompanyPrintDataById(payment.accountId);
        if (this.claims && this.claims.length) {
            let modelData: PaymentPostingPrintModel = {
                accountInfoId: this.idsWithUserInfoReq.AccountInfoId,
                memberId: this.idsWithUserInfoReq.MemberId,
                claimId: this.claims[0].id,
                patientId: this.claims[0].patientId
            }
            this.claimPostingService.GetClientPrintDataById(modelData).subscribe((data: any) => {
                this.companyInfo$.emit(data);
            });
        };
    }

    notErroredServiceLines(serviceLines: PaymentClaimServiceLine[]) {
        return !serviceLines ? [] : serviceLines.filter(x => !x.hasErrors || !this.showErrors);
    }

    erroredServiceLines(serviceLines: PaymentClaimServiceLine[]) {
        return !serviceLines ? [] : serviceLines.filter(x => x.hasErrors && this.showErrors);
    }
}