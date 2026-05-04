export interface StaffDetails {
    ageGroups: number[];
    billableHourTarget?: string;
    billableTargetPercent?: string;
    canHandleAggression?: boolean;
    clients?: number[];
    dateOfBirth: string;
    experienceTypeId?: number;
    genderId?: number;
    hourlyRate?: number;
    hoursPerWeekAuthorized?: string;
    id: number;
    languages: number[];
    months?: string;
    numberOfClients?: number;
    startDate: string;
    substituteClients?: number[];
}