export interface PaymentEOBInfo {
    id: number;
    paymentAmount: number;
    paymentMethod: string;
    paymentMethodName: string;
    isManual: boolean;
    checkNumber: string;
    issuedDate: Date;
    recievedDate: Date;
    payerName: string;
    payerLocation: string;
    payerPhoto: string;
    payerPhoneNumber: string;
    payerRoutingNumber: string;
    payerBankId: string;
    payerId: number;
    payeePhoto: string;
    payeeName: string;
    ppayeeLocation: string;
    payeeBankId: number;
    payeeId: string;
    payeeAddress: string;
    accountInfoId: number;
    payeeAdressObject: any;
} 

export class PayeeAddress {
    payeeAddress1: string;
    payeeAddress2: string;
    payeeAddressCity: string;
    payeeAddressState: string;
    payeeAddressZip: string;
    payeeAddressCountry: string;
}