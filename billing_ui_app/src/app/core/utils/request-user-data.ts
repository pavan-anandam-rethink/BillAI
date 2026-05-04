import { Injectable } from "@angular/core";
import { GetClaimByIdentifier } from "@core/models/billing";
import { IdWithUserInfo } from "@core/models/billing/get-claim-by-identifier";
import { AccountMemberService } from "@core/services/account/account-member.service";

@Injectable({
    providedIn: 'root'
})
export class RequestUserData {
    
    constructor(
        private accountService: AccountMemberService,
    ) {
    }

    GetMemberIdModel() {
        return {
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId
        }
    }

    GetClaimByClaimIdentifier(claimIdentifier: string, Id: number): GetClaimByIdentifier {
        return {
            claimIdentifier: claimIdentifier,
            Id: Id,
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId
        }
    }

    GetIdWithUserInfo(Id: number): IdWithUserInfo {
        return {
            Id: Id,
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId
        }
    }

    ClaimGetRequestSortFilterWithUserInfo(claimHeaderModel: any) {
        return {
            ...claimHeaderModel,
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId
        }
    }

    GetClaimDetailsInfoUpdateModel(model: any) {
        return {
            ...model,
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId
        }
    }

    GetClaimDetailsInfo(Id: number) {
        return {
            ChargeId: Id,
            AccountId: this.accountService.memberDetails.memberId
        }
    }
}