interface ReasonCodeData {
  // define the properties of ReasonCodeData here
}

interface PaymentClaimServiceLineAdjustmentModel {
  // define the properties of PaymentClaimServiceLineAdjustmentModel here
}

export interface Adjustment {
  groupCode: any;
  reasonCode: string;
  amount: number | null;
  isPositive: boolean | false;
  description?: string;
}

export interface Adjustmentlist {
  id: number;
  amount: number;
  isPositive: boolean;
  groupCode: string;
  reasonCode: string;
  description: string;
  paymentIdentifier: string;
  postDate: Date;
  paymentId: number;
  reasonCodeKey: string;
}

export interface Charge {
  id: number;
  clientName: string;
  serviceDate: Date;
  procedureCode: string;
  modifiers: string;
  billedAmount: number;
  allowedAmount: number;
  writeOff: number ;
  paymentAmount: number;
  adjustments: Adjustment[];
}

export interface BulkPaymentResponse {
  id: number;
  claimId?: number | null;
  claimIdentifier?: string | null;
  chargeEntryId?: number | null;
  units?: number | null;
  reasonCode?: string[];
  description?: string[];
  reasonCodeData?: ReasonCodeData[];
  isLinked: boolean;
  paidAmount?: number | null;
  patientResponsibility?: number | null;
  patientResponsibilityBalance?: number | null;
  adjustment?: number | null;
  status?: string | null;
  insurancePayment?: number | null;
  serviceLineId: number;
  patientId: number;
  patientName?: string | null;
  dateOfService: Date;
  procedure?: string | null;
  mods?: string | null;
  billedAmount: number;
  allowedAmount: number;
  writeOff: number ;
  patientPayment?: number | null;
  positiveAdjustment?: number | null;
  negativeAdjustment?: number | null;
  positivePatientResponsibility?: number | null;
  negativePatientResponsibility?: number | null;
  adjustments?: PaymentClaimServiceLineAdjustmentModel[];
  balance?: number | null;
  expectedAmount?: number | null;
  totalCount: number;
  hasErrors: boolean;
  dateLastModified?: Date | null;
  dateDeleted?: Date | null;
}
