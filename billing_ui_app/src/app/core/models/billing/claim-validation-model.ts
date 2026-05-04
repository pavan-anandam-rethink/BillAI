import { IdWithUserInfo, UserInfo } from "./get-claim-by-identifier";

export class ClaimValidationModel extends IdWithUserInfo {
    isSecondary : boolean;
    secondaryFunderId : number;
}

export class ClaimResponseModel{
    claimid: number;
    error?: string = '';
}