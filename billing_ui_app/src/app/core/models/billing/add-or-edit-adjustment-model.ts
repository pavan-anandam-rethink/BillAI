import { UserInfo } from "./get-claim-by-identifier";

export class AddOrEditAdjustmentModelWithUserInfo extends UserInfo {
    serviceLineId: number;
    claimId: number;
    adjustmentDetails: AdjustmentDetailsModel[];
}

export class AdjustmentDetailsModel {
    adjustmentId?: number;
    amount: number;
    isPositive:boolean;
    groupCode: string;
    reasonCode: string;
}
