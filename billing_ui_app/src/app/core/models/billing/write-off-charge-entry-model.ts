import { UserInfo } from "./get-claim-by-identifier";

export class WriteOffChargeEntryModelWithUserInfo extends UserInfo {
    writeOffChargeEntryModels: WriteOffChargeEntryModel[];
}

export interface WriteOffChargeEntryModel {
    id: number;
    writeOffReasonCodeId: number;
    writeOffAmount: number;
    writeOffAmountOrig: number;
    description: string;
    dateLastModified: Date;
}

export class EditWriteOffModelWithUserInfo extends UserInfo {
    claimId: number
    writeOffDetails: WriteOffDetailsModel[];
}

export class WriteOffDetailsModel {
    chargeEntryWriteOffId: number;
    writeOffReasonCodeId: number;
    writeOffAmount: number;
}

export class WriteOffReasonCodDescriptionModel {
    id: number;
    description: string;
}

export class GetChargeEntryWriteOffModel extends UserInfo{
    id: number;
    isServiceLineId: boolean;
}
