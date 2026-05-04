export interface NewPayment {
    chargeEntryId: number;
    amount: number;
    reasonCodeId: number;
    paymentMethodId: number;
    reference: string;
}
