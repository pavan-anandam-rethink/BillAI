import { UserInfo } from "./get-claim-by-identifier";

export class ClaimsRebillModelWithUserInfo extends UserInfo {
    ClaimsToRebill: RebillClaimsModel;
}

export interface RebillClaimsModel {
    claimIds: number[];
    rebillReason: string;
    submissionReasonCode: number;
    note: string;    
    claimNote:string;
}