export interface PaymentPostingGrid{
    data: PaymentPosting[];
    totalCount: number;
    isRevSpringEnabled: boolean;
}

export interface PaymentPosting {
    id: number;
    paymentIdentifier: number;
    paymentMethodName: string;
    receivedDate: Date;
    funderName: string;
    reference: string;
    paymentAmount: number;
    appliedAmount: number;
    claimsCount: number;
    deniedClaimsCount: number;
    reconcileStatus: string;
    isManual: boolean;
    isManualReconciled: boolean;
} 

export interface UnAllocatedPaymentsModel {
  AccountInfoId: number;
  PaymentId: number;         
  ChildProfileId: number;   
  UnAllocatedAmount: number | null;
  Notes?: string | null;
  MemberId: number;
}
