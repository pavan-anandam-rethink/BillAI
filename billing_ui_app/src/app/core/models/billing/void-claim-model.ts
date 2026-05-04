import { UserInfo } from "./get-claim-by-identifier";

export class ClaimsVoidModelWithUserInfo extends UserInfo {
    ClaimsToVoid: VoidClaimsModel;
}

export interface VoidClaimsModel {
    claimIds: number[];
    submitToClearinghouse: boolean;
    note: string;    
    claimNote:string;
}