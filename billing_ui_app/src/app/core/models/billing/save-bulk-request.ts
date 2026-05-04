export interface UserInfo {
  accountInfoId: number;
  memberId: number;
}

export interface AdjustmentDetailsModel {
  adjustmentId?: number; // optional (nullable in C#)
  amount?: number;
  isPositive?: boolean; // optional (nullable in C#)
  groupCode?: string;
  reasonCode?: string;
}

export interface AddOrEditAdjustmentModel extends UserInfo {
  claimId: number;
  serviceLineId: number;
  allowedAmount?: number;
  paymentAmount?: number;
  adjustmentDetails?: AdjustmentDetailsModel[];
}
