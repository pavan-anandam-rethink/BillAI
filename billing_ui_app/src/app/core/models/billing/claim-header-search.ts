import { SortDescriptor } from "@progress/kendo-data-query";

export class ClaimHeader {
    id: number;
    claimNumber: string;
    patientName: string;
    funderName: string;
    renderingProviderName: string;
    placeOfService: string;
    dateOfServiceStart: Date;
    dateOfServiceEnd: Date;
    authorizationNumber: string;
    billedAmount: number;
    expectedAmount: number;
    paymentAmount: number;
    adjustmentAmount: number;
    patientResponsibilityAmount: number;
    balanceAmount: number;
    billedDate: Date;
    validationAlertsCount: number;
    validationErrorsCount: number;
    status: number;
    claimStatusName: string;
    totalCount: number;
    childProfileId: number;
    childProfileAuthorizationId: number;
    childProfileFunderId: number;
    submissionStatusId: number;
    submissionTypeId: number;
    warningsCount: number;
    errorsCount: number;
    cmsPagesCount?: number;
    secondaryFunderId?: number;
    reasonCodes?: string;
    reason?: string; 
    comment?: string;
    flagReasonTransactionId?: number; 
    reasonId?: number; 
    isTestAccount: boolean = false;
    useNewClaimProcessing : boolean = false;
    assigneeId: number | 0;
    isClientDeleted: boolean = false;
}

export class ClaimHeaderSearch {
    accountInfoId: number;
    memberId: number;
    sortingModels: SortDescriptor[] = [];
    filters: ClaimHeaderFilter = new ClaimHeaderFilter();
    skip: number;
    take: number;
}

export class ClaimHeaderFilter {
    claimNumber: string;
    patientIds: string | undefined;
    funderIds: string | undefined;
    locationIds: string | undefined;
    reasonCode: string | undefined;
    reasonId: string | undefined;
    balanceFrom: number | undefined;
    balanceTo: number | undefined;
    patientResponsibilityFrom: number | undefined;
    patientResponsibilityTo: number | undefined;
    dateOfServiceFrom : Date | undefined;
    dateOfServiceTo : Date | undefined;
    renderingProviderIds: string;
    statusIds: string | undefined;
    validationIds: string | undefined;
    responseIds: string | undefined;
    tab: number;
    showVoided: boolean;
}
