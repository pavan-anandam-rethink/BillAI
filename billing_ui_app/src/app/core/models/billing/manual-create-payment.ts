import { UserInfo } from "./get-claim-by-identifier";

export interface ManualCreatePayment extends BasicInfo, UserInfo {
    paymentAmount: string;
    referenceNumber: number;
    postDate: Date;
    depositDate: Date;
    funderId?: number;
}

export interface UnallocatedManualCreatePayment extends ManualCreatePayment, BasicInfo, UserInfo {
  patientId?: number;
  unAllocatedAmount?: number;
  notes?: string;
  eobFileId?: number;
}

interface BasicInfo {
    funderType: string;
    paymentMethod: string;
}
