import { AuthorizationDiagnosisCode } from "../clients/authorization";
import { BillingProviderRequest } from "./claim-billing-provider";

export interface ClaimDetailsModel extends ClaimUpdateDetailsModel, ClaimNoteDetailsModel{
    dos: string;
    billingCode: string;
    diagnosis: string;
    units: number;
    billedAmount: number;
    expectedAmount: number;
    hours: number;
    paymentAmount: number;
    patientAmount: number;
    balanceAmount: number;
    adjustmentAmount: number;
    totalCount: number;
    associatedAppointmentsCount: number;
    reasonCode: string;
    renderingProvider: string;
    renderingProviderId: number;

    renderingProviderName?: string;
}

export interface ClaimUpdateDetailsModel extends ClaimUpdateModifiersModel{
    claimId: number;
    units: number;
    perUnitsCharge: number;
    billingCodeId?: number;
    diagnosis?: string;
    unitTypeId?: number;
    DateOfService?: Date;
    billingCode?: string;
    hours?: number;
    billedAmount?: number;
    expectedAmount?: number;
    paymentAmount?: number;
    patientAmount?: number;
    balanceAmount?: number;
    adjustmentAmount?: number;
    isSecondaryCode?: boolean;      
    pairBillingCodeId?: number;
    individualDateOfService?: any;
    renderingProviderId?: number;
}

export interface ClaimUpdateModifiersModel{
    id: number;
    modifier1: string;
    includeOnClaimMod1: boolean;
    modifier2: string;
    includeOnClaimMod2: boolean;
    modifier3: string;
    includeOnClaimMod3: boolean;
    modifier4: string;
    includeOnClaimMod4: boolean;
}

export interface ClaimStatusUpdateModel{
    claimId: number;
    claimStatusId: number;
}

export interface ClaimUpdateModifiersViewModel extends ClaimUpdateModifiersModel {
    isValid: boolean;
}

export interface ClaimNoteDetailsModel extends ClaimNoteAddModel{
    noteCreatorName: string | null;
    noteCreatedDate: Date | null;
}

export interface ClaimNoteAddModel{
    chargeId: number;
    noteText: string;
}

export interface ClaimUpdateDetailsModels{
    billingClaimDetailsModels: ClaimUpdateDetailsModel[];
    memberId: number;
}


export interface ClaimDetailsInfoModel extends ClaimDetailsInfoUpdateModel {
    id: number;
    patientId: number;
    patientName: string;
    funderId: number;
    funderName: string;
    funderTypeId: number;
    responsibleParty: string;
    dateOfServiceStart: Date;
    dateOfServiceEnd: Date;
    authorizationNumber: string;
    authorizationStatus?: string;

    referringProviderRequiredOnClaim: boolean;
    
    billedAmount: number;
    balanceAmount: number;
    paymentAmount: number;
    patientResponsibilityAmount: number;
   
    providerSignature: string;
    submissionCode: string;
    
    //from update interface
    claimId: number;
    diagnosisCodes: AuthorizationDiagnosisCode[];

    placeOfService: string;

    renderingProviderId: number;
    referringProviderId: number;
    billingProviderId: number;
    serviceFacilityId: number;

    patientReleaseAgreement: string;
    authorizePayment: string;
    benefitsAssignment: number;
    submissionReason: string;

    billingProviderOptionId: number;

    authorizationDetails: AuthorizationDetailsModel;
    serviceLineId: number;
    serviceLine:string;
    serviceId: number;
    primaryFunderId?: number;
    secondaryFunderId?: number;
}

export interface ClaimDetailsInfoUpdateModel{
    claimId: number;
    diagnosisCodes: AuthorizationDiagnosisCode[];

    renderingProviderId: number;
    referringProviderId: number;
    billingProviderId?: number;
    serviceFacilityId: number;

    renderingProvider: string;
    referringProvider: string;
    billingProvider: string;
    serviceFacility: string;
    placeOfService: string;

    placeOfServiceId: number;
    patientReleaseAgreementId: number;
    authorizePaymentId: number;
    submissionReasonId: number;
    benefitAssignmentId: number;
    originalClaim: string;
    note: string;  
    
}

interface AuthorizationDetailsModel {
    renderingProviderId?: number;
}


export class ClaimUpdateModel {
    isClaimUpdated: boolean = false;
    isChargeEntryUpdated: boolean = false;
    claimModel: ClaimDetailsInfoUpdateModel;
    chargeEntryModel: ClaimUpdateDetailsModels;
    impersonationUserName: string | null;
    billingProviderRequest?: BillingProviderRequest | null;
}
