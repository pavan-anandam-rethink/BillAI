import { IdsWithUserInfo, UserInfo } from "./get-claim-by-identifier";

export class ClaimsSubmitModel extends IdsWithUserInfo {
    isSecondary : boolean;
    adjustmentLevel : number;
    secondaryFunderDetails : SecondaryFunderDetailsModel[] = [];
    impersonationUserName: string | null;
}

export interface ClearingHouseClaimModel {
  claimId: number;
  clearinghouseId: number;
  isSecondary: boolean;        
  adjustmentLevel?: number;
}

export class SecondaryFunderDetailsModel {
    claimId : number;
    secondaryFunderId : number;
    controlNumber : string;
}
