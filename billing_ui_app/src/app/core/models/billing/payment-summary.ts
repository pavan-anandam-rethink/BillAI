import {PaymentPostingMethods} from "@core/models/billing/payment-posting-methods";

export interface PaymentSummary extends UpdateManualPaymentSummary{    
    paymentAmount: number;
    postedAmount: number;
    remainingAmount: number;
    funderName: string;
    payee: string;
    paymentMethodId: number;
    isManual: boolean;
    paymentTypeId: number;
} 

export interface UpdateManualPaymentSummary {
    id: number;
    postDate: Date|undefined;
    paymentMethodId: number;
    paymentMethod: string;
    paymentMethodEntity: PaymentPostingMethods;
    depositDate: Date|undefined;
    referenceNumber: string;
    paymentAmount?: number;
}