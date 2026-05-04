import { ListFilterSort } from "./payment-posting-list-filter-sort";

export class PatientInvoiceHeader {
    id:number;
    clientName: string;
    charges: number;
    insuranceAmount: number;
    adjustment_Non_Patient_responsibility: number;
    adjustment_Patient_responsibility: number;
    patientAmount: number;
    patientBalance: number;
    checked: boolean;
    guarantorName: string;
}

export class PatientInvoiceDetails {
    id:number;
    claimId: number;
    clientId: number;
    clientName: string;
    billingCode: string;
    dateOfService: string;
    units: number;
    charges: number;
    insuranceAmount: number;
    adjustment_Non_Patient_responsibility: number;
    adjustment_Patient_responsibility: number;
    patientAmount: number;
    patientBalance: number;
    invoicestatus: string;
    guarantorName: string;
    checked: boolean;
}

export class PatientInvoiceHeaderSearch {
    accountInfoId: number;
    filters: PatientInvoiceFilter = new PatientInvoiceFilter();
    skip: number;
    take: number;
}

export class PatientInvoiceFilter extends ListFilterSort {
    clientIds: string | undefined;
    patientResponsibilityFrom: number | undefined;
    patientResponsibilityTo: number | undefined;
    dateOfServiceFrom : Date | undefined;
    dateOfServiceTo : Date | undefined;
    invoiceFrom  : Date | undefined;
    invoiceTo  : Date | undefined;
    paymentDueFrom  : Date | undefined;
    paymentDueTo  : Date | undefined;
}

export class PatientInvoiceCharge {
    chargeId:number;
    billingCode: string;
    units: number;
    dos: string;
    billedAmount: number;
    insurancePayments: number;
    adjustmentNonPatientResponsibility: number;
    adjustmentPatientResponsibility: number;
    patientPayments: number;
    patientBalance: number;
}

export class InvoiceRequest {
    accountId: number;
    clientId: number;
    charges: PatientInvoiceCharge[] = [];
}

export class InvoiceDetailsModel {
    id:number; // client id
    clientName: string;
    totalBilledAmount: number;
    totalAdjustments: number;
    totalAdjustmentsPR: number;
    totalInsurancePayments: number;
    totalPatientPayments: number;
    totalPatientBalance: number;
    guarantorName: string;
    billingDetails: BillingDetailView[] = [];
    checked: boolean;

}

export class BillingDetailView {
    id:number;
    clientId:number;
    billingCode: string;
    units: number;
    dateOfService: string;
    billedAmount: number;
    adjustments: number;
    adjustmentsPR: number;
    insurancePayments: number;
    patientPayments: number;
    patientBalance: number;
    invoiceNumber:string;
    invoiceDate: string;
    paymentDue: string;
    status: string;
    checked: boolean;
}


