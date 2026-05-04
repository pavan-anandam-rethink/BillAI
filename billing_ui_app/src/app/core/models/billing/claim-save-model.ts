import { BillingProviderRequest, ClaimBillingProviderModel } from './claim-billing-provider';

export class ClaimSaveRequestModel {
    accountInfoId:number;
    memberId: number;
    claim: ClaimSaveModel;
    billingProviderRequest?: BillingProviderRequest | null;
}

export class ClaimSaveModel {
    claimInfo: ClaimInfoModel;
    provider: ProviderInfoModel;
    diagnosisCode: DiagnosisCode;
    impersonationUserName: string | null;
}

export interface DiagnosisCode {
    diagnosisCodesToSave: ClaimDiagnosisCode[];
    billingCodes: ClaimBillingCode[];
}

export interface ClaimInfoModel {
    clientId: number;
    funderId: number;
    clientFunderId: number;
    responsiblePartyId: number;
    serviceLineId: number;
    serviceId: number;
    authorizationNumberId: number | undefined;
    authorizationNumber: string | undefined;
    allowManualAuthorization: boolean;
    placeOfServiceCodeId: number;
}

export interface ProviderInfoModel {
    renderingProviderId: number;
    renderingProviderTypeId: number;
    billingProviderId: number | null;
    serviceFacilityLocationId: number;
    dateOfServiceStart: Date | undefined;
    dateOfServiceEnd: Date | undefined;
    referringProviderId: number | null;
}

export interface ClaimBillingCode {
    billingCodeId: number;
    unitTypeId: number;
    individualDateOfService: Date;
    modifier1: string;
    modifier2: string;
    modifier3: string;
    modifier4: string;
    noOfUnits: number;
    rate: number;
    totalCharges: number;
    renderingProviderStaffId: number;
}

interface ClaimDiagnosisCode {
    diagnosisId: number;
    diagnosisCode: string;
    description: string;
    order: number;
    includeOnClaims: boolean;
}
