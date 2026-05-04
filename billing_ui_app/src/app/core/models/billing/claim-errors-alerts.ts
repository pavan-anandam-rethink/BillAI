import { ClaimErrorSource } from "@core/enums/billing/claim-error-source";

type ClaimErrorSeverity = "Error" | "Alert" | "Response";
export interface ClaimErrorAlertModel {
    id: number;
    type: ClaimErrorSeverity,
    source: string,
    errorCode: string,
    description: string,
    message: string,
    adjustmentLevel: string,
    claimErrorSource: ClaimErrorSource,
    batchId: string,
    fileType: string
    responseDate: any;
    refValidationId: number;
    codeDescription: string[];
    messageDescription: MessageDescription[];
}

export interface AdjustmentReasonModel {
    groupCode: string,
    adjustmentCode: string,
    description: string,
}
export class MessageDescription {
    public description: string;
    public message: string;
  }