
export interface PostRemovePatientClaims {
    paymentId: number;
    patientServiceLines: PatientServiceLines[];
    postingCriteriaId: number;
    accountInfoId: number;
    memberId: number;
}

export interface PatientServiceLines {
   patientId: number;
   serviceLines: ServiceLineForDelete[];
}

export interface ServiceLineForDelete {
    id: number;
    claimId: number;
    isLinked: boolean;
}