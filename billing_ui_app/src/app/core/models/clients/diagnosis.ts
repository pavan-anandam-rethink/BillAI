export interface Diagnosis {
    diagnosisLUTypeId: string;
    isCustom: boolean;
    accountInfoId: number;
    canDelete: boolean;
    childProfileId: number;
    diagnosisDescription: string;
    diagnosisId: number;
    diagnosisLUCode: string;
    diagnosisLUDescription: string;
    diagnosisFullDescription: string;
    id: number;
    npiNumber: string;
    physician: string;
    physicianAddress: string;
    physicianCredential: string;
}