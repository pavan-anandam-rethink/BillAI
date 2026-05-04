export interface UpdatePaymentSummary {
    id: number;
    depositDate: Date|undefined;
    postDate: Date|undefined;
    paymentMethodId: number;
    paymentAmount?: number;
}