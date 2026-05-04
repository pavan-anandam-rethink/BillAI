import { ClaimHistoryField } from "@core/enums/billing/claim-history-field";
import { ClaimStatus } from "@core/enums/billing/claim-status";

export interface ClaimHistory {
    changeDate: Date;
    changeBy: string;
    rethinkUser: string | null;
    mode: number;
    actionId: number;
    historyActionId: number;
    status: ClaimStatus | undefined;
    fieldId: ClaimHistoryField | undefined;
    oldValue: string | undefined;
    newValue: string | undefined;
    claimVersionHistoryId: number | null;
}