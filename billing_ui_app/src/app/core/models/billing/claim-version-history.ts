export interface ClaimVersionHistory {
    claimId: number;
    claimIdentifier: string;
    clientName: string;
    responsibleParty: string;
    startDate: Date;
    endDate: Date;
    diagnosisCodes: string;
    authorizationNumber: string;

    balanceAmount: string;
    paymentAmount: string;
    billedAmount: string;
    patientResponsibilityAmount: string;
    placeOfService: string;

    renderingProvider: string;
    billingProvider: string;
    referringProvider: string;
    serviceProvider: string;

    benefitAssignment: string;
    submissionReason: string;
    patientReleaseAgreement: string;
    patientSignature: string;
    submissionCode: string;
    originalClaim: string;
    note: string;
}