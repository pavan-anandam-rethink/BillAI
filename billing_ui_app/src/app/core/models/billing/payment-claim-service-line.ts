export class PaymentClaimServiceLine implements PaymentClaimServiceLineSmall{
    paymentType: string;
    paymentIdentifier: string;
    adjustment: number;
    allowedAmount: number;
    allowedAmountOrig: number;
    balance: number;
    billedAmount: number;
    dateOfService: Date;
    expectedAmount: number;
    id: number;
    mods: string;
    paidAmount: number;    
    insurancePayment: number;
    serviceLinePaymentAmount: number;
    paidAmountOrig: number;
    patientPayment:number;
    patientResponsibility: number;
    procedure: string;
    dateLastModified: Date;
    claimId: number;
    claimIdentifier: string;
    units: number;
    reasonCode: string;
    description: string;
    hasErrors: boolean;
    paymentId: number;
    patientResponsibilityBalance: any;
}

export interface PaymentClaimServiceLineSmall {
    id: number;
    paymentId: number;
    paymentIdentifier: string;
    allowedAmount: number;
    paidAmount: number;
    allowedAmountOrig: number;
    paidAmountOrig: number;
    dateLastModified: Date;
    paymentType: string;
}