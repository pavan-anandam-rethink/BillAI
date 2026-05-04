export class ClaimDiagnosisCodeModel {
    diagnosisId: number;
    diagnosisCode: string;
    diagnosisDescription: string;
    diagnosisFullDescription: string;
    description: string;
    order: number;
    includeOnClaims: boolean;
    startDate: Date;
    endDate?: Date;
}