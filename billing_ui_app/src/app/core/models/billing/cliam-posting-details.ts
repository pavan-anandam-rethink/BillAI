export interface ClaimPostingDetails extends ClaimManualPostingDetails{
    patientResponsibilityBalance: any;
    mods: string;
    billedAmount: number;
    expectedAmount: number;
    allowedAmount: number;
    adjustment: number;
    claimId: number;
    claimIdentifier: string;
    status: string;
    patientId: number;
    patientPayment:number;
    isLinked: boolean;
} 

export interface ClaimManualPostingDetails {
    id: number;
    DOS: Date;
    procedureId: number;
    paidAmount: number;
    patientResponsibility: number;
    balance: number;
    checked: boolean;
}