export interface PaymentClaimsSearch{
    id: string;
    claimIdentifier: string;
    patientName: string;
    checked: boolean;
    startDate: Date;
    endDate: Date;
    alreadyCreated: boolean;
}

// export class PaymentClaimsSearchBase {
//     searchString = "";
//     paymentId = 0;
//     skip = 0;
//     take = 10;
// }
export class PaymentClaimsSearchBase {
    searchString = "";
    paymentId = 0;
    accountInfoId = 10;
}