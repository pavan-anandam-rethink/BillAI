export class UserInfo {
    AccountInfoId: any;
    MemberId: any;
}

export class IdWithUserInfo extends UserInfo {
    Id: number;
    // Optional pagination for server-side requests
    Skip?: number;
    Take?: number;
}

export class GetClaimByIdentifier extends IdWithUserInfo {
    claimIdentifier: any
}

export class IdsWithUserInfo extends UserInfo {
    Ids: number[];
}

export interface UnflagImperson extends IdsWithUserInfo {
    rethinkuser?: string;
}

export class SaveSelectedColumn extends UserInfo {
    SelectedColumns: string[];
}

export class RebillIdWithUserInfo extends UserInfo {
    ClaimId: number;
}