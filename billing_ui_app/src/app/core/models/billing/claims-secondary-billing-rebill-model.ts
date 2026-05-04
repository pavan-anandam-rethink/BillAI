import { RebillIdWithUserInfo, UserInfo } from "./get-claim-by-identifier";

export class ClaimsSecondaryBillingRebillModel extends RebillIdWithUserInfo {
    isSecondary : boolean;
    adjustmentLevel : number;
    secondaryFunderDetails : RebillSecondaryFunderDetailsModel[] = [];
}

export class RebillSecondaryFunderDetailsModel {
    claimId : number;
    secondaryFunderId : number;
    controlNumber : string;
}