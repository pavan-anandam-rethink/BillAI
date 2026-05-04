import { IdWithUserInfo, UserInfo } from "./get-claim-by-identifier";

export interface Claim {
    id: number;
    submitDate: Date;
    client: string;
    insurance: string;
    status: string;
    errorMessage: string;
}

export interface FlagClaimsRequest extends UserInfo {
    claimIds: number[];
    reasons: FlagReason[];  
    notes?: string;
    // Optional: for edit mode
    claimFlagTransactionId?: number;
    impersonationUserName?: string;
}

export interface FlagReason {
    reasonId: number;
}

export interface ClaimUpdateResult {
  success: boolean;
  message?: string;
}

export interface UnflagImperson extends IdWithUserInfo {
  rethinkuser?: string;
}
