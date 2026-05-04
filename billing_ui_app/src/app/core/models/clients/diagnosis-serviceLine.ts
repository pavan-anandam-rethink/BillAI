import { Diagnosis, ServiceLine } from ".";

export interface DiagnosisServiceLine {
    id: number;
    childProfileId: number;
    diagnosisCodeId: number;
    diagnosisLUCode: string;
    diagnosisLUDescription: string;
    order: number;
    serviceLineId: number;
    serviceLine: string;
    serviceLineIsActive: boolean;
    dateCreated: string;
    endDate: string;
    startDate: string;
    status: string;
    editableDescription?: boolean;
    hasAuthorization?: boolean;
}

export interface DiagnosisServiceLineMap {
    serviceLineId: number;
    serviceLineName: string;
    serviceLineIsActive: boolean;
    isSLUsedInAuth: boolean;
    diagnosisServiceLines: Array<DiagnosisServiceLine>;
}

export interface DiagnosisServiceLinesList {
    diagnosis: Diagnosis;
    diagnosisLineMapId?: number;
    assignedServiceLines: boolean;
    serviceLines: Array<ServiceLine>;
}

