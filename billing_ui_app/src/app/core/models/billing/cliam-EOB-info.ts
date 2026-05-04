import { PaymentClaimServiceLine } from "./payment-claim-service-line";

export interface ClaimEOBInfo {
    id: number;
    patientId: number;
    patientName: string;
    claimIdentifier: string;
    billedAmount: number; 
    paidAmount: number;
    patientResponsibility: number;
    status: string;
    allowedAmount: number;

    providerName: string;
    providerId: string;

    payerClaimNumber: string;
    placeOfService: string;

    claimDateFrom: Date;
    claimDateTo: Date;

    claimReceived: Date;
    patientResponsible : number;
    totalBuilled : number;
    totalPaid : number;

    serviceLines: PaymentClaimServiceLine[];
} 