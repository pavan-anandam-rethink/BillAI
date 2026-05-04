import { ClaimsSubmitModel } from "./claims-submit-model";
import { IdsWithUserInfo } from "./get-claim-by-identifier";

export class ClaimProcessRequestModel {
    batchId : string;
    claimsSubmitModel: ClaimsSubmitModel[] = [];
    totalClaims : number;
    claimStatus: string;
}

export class ClaimApproveRequestModel extends IdsWithUserInfo{
    batchId : string;
    totalClaims : number;

}
