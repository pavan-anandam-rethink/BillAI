export interface ManualPaymentPatientSearch{
    id: string;
    patientName: string;
    paymentAmounts: number;
    notes: string;
    checked: boolean;
}

// export class ManualPaymentPatientSearchBase {
//     personName = "";
//     skip = 0;
//     take = 10;
// }
export class ManualPaymentPatientSearchBase {
    personName = "";
    accountInfoId = 0;
}
