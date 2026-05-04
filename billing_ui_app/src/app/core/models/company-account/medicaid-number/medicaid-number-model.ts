export interface MedicaidNumberModel {
    id: number;
    medicaidIdNumber: string;
    hasAssignedFunders: boolean;
    funderNames: string[];
}