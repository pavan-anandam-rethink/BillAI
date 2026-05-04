export interface ClaimPosting {
  rowVersion: any;
  notes: string;
  unallocatedPayment: number;
  allowedAmount: number;
  billedAmount: number;
  claimIdentifier: string;
  claimId: number | null;
  dateOfServiceStart: Date;
  id: number;
  paidAmount: number;
  patientId: number;
  patientName: string;
  patientResponsibility: number;
  status: string;
  checked: boolean;
  linkedchecked: boolean;
  balance: number;
  claimActionTypes: string;
}

export interface PaymentClaims extends ClaimPosting {
  claimStatus: string;
  submissionTypeId?: number | null;
  isSecondaryPayerAvailable?: boolean | null;
  isTestAccount?: boolean | null;
}
export class PaymentPostingPrintModel {
  accountInfoId: number;
  memberId: number;
  claimId: number;
  patientId: number;
}

export class PaymentPostingBulkModel {
  accountInfoId: number;
  memberId: number;
  Ids: number[];
}

export interface PaymentPatientModel {
  patientId: number;
  patientName: string;
}
