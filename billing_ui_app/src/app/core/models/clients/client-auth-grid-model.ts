export interface ClientAuthorizationGridModel {
    appointmentsCount: number;
    authorizationDistributionTypeId: number;
    authorizationNumber: string;
    authorizationSubmissionTypeId?: number;
    authorizationSubmissionType: string;
    authorizedTime: number;
    billingCodes: ClientAuthorizationGridBillingCodeModel[]
    billingProvider: string;
    billingProviderId?: number;
    diagnosisCodes: ClientAuthorizationGridDiagnosisCodeModel[];
    endDate: Date;
    funder: string;
    funderId: number;
    funderName: string;
    hasAttachment: boolean;
    id: number;
    insurancePolicyNumber: string;
    isActive: boolean;
    isMissingReferringProvider: boolean;
    isReferringProviderActive: boolean;
    referringProvider: string;
    referringProviderId?: number;
    renderingProvider: string;
    renderingProviderStaffId?: number;
    renderingProviderTypeId?: number;
    responsibilitySequence: string;
    serviceFacility: string;
    serviceLine: string;
    serviceLineString: string;
    serviceProviderId?: number;
    startDate: Date;
    status: string;
}

export interface ClientAuthorizationGridBillingCodeModel {
    billingCodeDescription: string;
    billingCodeName: string;
    id: number;
    interval?: number;
    noOfUnits: number;
    remainingHours: number;
    remainingUnits: number;
    schedulingGoalInterval?: number;
    schedulingGoalNoOfUnits?: number;
    serviceName: string;
    totalHours: number;
    totalScheduledGoals: number;
    totalUnits: number;
    unit?: number;
    unitTypeId: number;
    usedHours: number;
}

interface ClientAuthorizationGridDiagnosisCodeModel {
    id: number;
    isActive: boolean;
    diagnosisCode: string;
    order: number;
}