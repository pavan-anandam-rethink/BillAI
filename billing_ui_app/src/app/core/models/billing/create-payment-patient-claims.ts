export interface CreatePaymentPatientClaims {
    paymentId: number;
    patientIds: string[];
    unAllocatedAmount: number[];
    notes: string[];
    memberId: number;
    accountInfoId: number;
}

export interface AddPatientResponseClaims {
    patientId: number;
    patientName: string;
    isAttached: boolean;
}
