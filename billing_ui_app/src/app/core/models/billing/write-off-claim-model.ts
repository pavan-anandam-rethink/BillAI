import { UserInfo } from "./get-claim-by-identifier";

export class WriteOffClaimModelWithUserInfo extends UserInfo {
    claimId: number;
    serviceLineId: number | null;
    amountTypeId: number;
    amount: number;
    applicationTypeId: number | null;
    reasonCodeId: number;
    note: string;
    isServiceLine: boolean | null;
}

export class ClaimOrChargeToWriteOff {
    balanceAmount: number;
    claimId: number;
    chargeId: number;
}

export class AddWriteOffResponseModel {
    success : boolean;
    errorMsg : string;
}