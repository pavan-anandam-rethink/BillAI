import { CompanyAccountLocation } from "../company-account";

export class Authorization {
    constructor(data: Authorization | null = null) {
        if (data) {
            this.id = data.copyFromId ? 0 : data.id;
            this.authorizationNumber = data.copyFromId ? null : data.authorizationNumber;
            this.originalAuthNumber = data.copyFromId ? data.authorizationNumber : null;

            this.isActive = data.isActive;
            this.authorizationSubmissionTypeId = data.authorizationSubmissionTypeId;
            this.funderId = data.funderId;
            this.funderAppointmentExceedingAuthorizationAlertId = data.funderAppointmentExceedingAuthorizationAlertId;
            this.renderingProviderId = data.renderingProviderId;
            this.renderingProviderTypeId = data.renderingProviderTypeId;
            this.renderingProviderName = data.renderingProviderName;
            this.startDate = data.startDate;
            this.endDate = data.endDate;
            this.inactiveDate = data.inactiveDate;
            this.referringProviderId = data.referringProviderId
            this.referringProviderIsActive = data.referringProviderIsActive
            this.serviceProviderId = data.serviceProviderId
            this.billingProviderId = data.billingProviderId
            this.serviceLineId = data.serviceLineId;
            this.diagnosisCodes = data.diagnosisCodes;
            this.totalNumberOfUnits = data.totalNumberOfUnits;
            this.authorizationDistributionTypeId = data.authorizationDistributionTypeId;
            this.billingCodes = data.billingCodes
            this.childProfileFunderServiceLineMappingId = data.childProfileFunderServiceLineMappingId;
            this.childProfileFunderMappingIsActive = data.childProfileFunderMappingIsActive;
            this.childProfileFunderMappingId = data.childProfileFunderMappingId
            this.showAuthorizationByTypeId = data.showAuthorizationByTypeId;
            this.appointmentsCount = data.appointmentsCount;
            this.appointmentsInfo = data.appointmentsInfo;
            this.isStartDateValid = data.isStartDateValid;
            this.isEndDateValid = data.isEndDateValid;
            this.isInactiveDateValid = data.isInactiveDateValid;
            this.isFunderValid = data.isFunderValid;
            this.diactivatedById = data.diactivatedById;
            this.propagatingRenderingProviderData = data.propagatingRenderingProviderData;
            this.copyFromId = data.copyFromId || 0;
        }
    }

    id = 0;
    isActive = false;
    authorizationNumber: string | null;
    originalAuthNumber: string | null;
    authorizationSubmissionTypeId: number | null;
    funderId: number;
    funderAppointmentExceedingAuthorizationAlertId: number | null;
    renderingProviderId: number | null;
    renderingProviderTypeId: number | null;
    renderingProviderName: string;
    startDate: string;
    endDate: string;
    inactiveDate: string | null;
    referringProviderId: number;
    referringProviderIsActive: boolean;
    serviceProviderId: number | null;
    billingProviderId: number | null;
    serviceLineId: number | null;
    diagnosisCodes: AuthorizationDiagnosisCode[] = [];
    totalNumberOfUnits: number | null;
    authorizationDistributionTypeId: number;
    billingCodes: AuthorizationBillingCodeInfo[] = [];
    childProfileFunderServiceLineMappingId: number;
    childProfileFunderMappingIsActive: boolean;
    childProfileFunderMappingId: number;
    showAuthorizationByTypeId: number | null;
    appointmentsCount: number;
    appointmentsInfo: AuthorizationAppointment[] = [];
    isStartDateValid: boolean;
    isEndDateValid: boolean;
    isInactiveDateValid: boolean;
    isFunderValid: boolean;
    diactivatedById: number | null;
    propagatingRenderingProviderData?: PropagatingRenderingProviderData;
    copyFromId: number;

    get isCopy() {
        return (this.copyFromId || 0) > 0;
    }
    get isEditMode() {
        return this.id > 0 || this.isCopy
    };
}

export class AuthorizationDiagnosisCode {
    description: string;
    diagnosisCode: string;
    diagnosisId: number;
    order: number;
    includeOnClaims: boolean;
    manuallyAdded: boolean;
    startDate: Date;
    endDate?: Date;
    isActive: boolean;
}

export class AuthorizationBillingCodeInfo {
    id: number;
    billingCodeId: number;
    unitTypeId: number;
    frequencyType: string;
    noOfUnits: number;
    interval: number | null;
    schedulingGoalNoOfUnits: number;
    schedulingGoalNoOfUnitsCalculated: number | null;
    schedulingGoalHoursCalculated: number | null;
    totalScheduledGoals: number | null;
    totalScheduledGoalsUnit: number | null;
    totalHours: number;
    totalUnits: number;
    remainingHours: number;
    remainingUnits: number;
    schedulingGoalFrequencyTypeId: 1 | 2 | 3 | 4 | null;
    schedulingGoalInterval: number | null;
    childProfileAuthorizationId: number | null;
    providerBillingCode: {} | null;
}

export class AuthorizationAppointment {
    id: number;
    startDate: Date;
    endDate?: Date;
    startTime: Date;
    endTime: Date;

    funderName: string;
    serviceName: string;
    procedureCodeId: number | null;
    frequencyInterval: number;
    unitType: number;
    eventMinutes: number | null;
}

export class AuthorizationEditOptions {
    billingCodes: AuthorizationBillingCode[] = [];
    childProfileFacilityId: number;
    funders: AuthorizationFunder[] = [];
    locations: CompanyAccountLocation[] = [];
    propagatingAppointmentDataTypes: PropagatingAppointmentDataTypes[] = [];
    referringProviders: AuthorizatioReferringProvider[] = [];
    renderingProviders: AuthorizationRenderingProvider[] = [];
}

export class AuthorizationFunder {
    id: number;
    funderId: number;
    funderName: string;
    funderType: number;
    serviceLines: AuthorizationServiceLine[] = [];
    referringProviderRequiredOnClaim: boolean;
    startDate: string;
    endDate: string | null;
    isActive: boolean;
    billingProviderOptionId: number | null;
}

export class AuthorizationBillingCode {
    rate2?: number;
    rate?: number;
    billingCodeId: number;
    billingCodeName: string;
    billingCodeName2: string;
    billingCodeDescription: string;
    frequencyTypeId: number | null;
    funderId: number;
    serviceLineId: number;
    unitTypeId: number;
    unitTypeId2: number;
    providerServiceId: number;
    inactive: boolean | null;
    noAuthRequired: boolean;
    serviceName: string;
    renderingProviderStaffId: number | null;

    used = false;
}

export class PropagatingRenderingProviderData {
    dateLastModified: Date | null;
    modifiedBy: string;
    renderingProviderEndDate: Date | null;
    lastSelectionTypeId: number | null;
    infoMessage: string;
}

export class PropagatingAppointmentDataTypes {
    id: number;
    name: string;
}

export class AuthorizatioReferringProvider {
    id: number;
    isActive: boolean;
    isDefault: boolean;
    providerName: string;
}

export class AuthorizationRenderingProvider {
    id: number;
    name: string;
    staffMemberId: number;
}

export class AuthorizationServiceLine {
    mappingId: number;
    serviceId: number;
    name: string;
    sequence: string;
    billingCodes: any;
    billingProviderOptionId?: number;
}

export class AuthAttachment {
    id: number;
    fileName: string;
    dateCreated: Date;
    createdBy: string;
}
