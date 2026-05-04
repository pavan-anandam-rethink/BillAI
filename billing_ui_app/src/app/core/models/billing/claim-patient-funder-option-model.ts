export interface ClaimPatientFunderOptionModel {
    id: number;
    name: string;
    responsibilitySequence: number;
}

export interface ClaimNextFundersAndControlNumberModel {
    funders: ClaimPatientFunderOptionModel[];
    controlNumber: string;
}