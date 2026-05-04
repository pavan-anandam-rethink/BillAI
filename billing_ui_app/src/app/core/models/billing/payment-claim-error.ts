export class PaymentClaimError {
    id: number;
    patientName: string;
    patientId: number;
    claimIdentifier: string;
    expectedAmount: number;
    allowedAmount: number;
    balance: number;
    errorMessage: string;
}