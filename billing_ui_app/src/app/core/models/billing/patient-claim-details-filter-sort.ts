import { ListFilterSort } from "./payment-posting-list-filter-sort";

export class PatientClaimDetailsFilterSort extends ListFilterSort {
    paymentId: number;
    patientId: number;
    showPaid: boolean;
    isLinked: boolean;
}