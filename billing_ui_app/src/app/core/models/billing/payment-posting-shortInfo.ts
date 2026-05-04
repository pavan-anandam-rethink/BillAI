export interface PaymentPostingShortInfo {
    id: number;
    paymentIdentifier: string;
    reconcileStatus: string;
    errorsCount: number;
    isManual: boolean;
    isPatientType: boolean;
    isOtherType: boolean;
    isInsuranceType: boolean;
} 