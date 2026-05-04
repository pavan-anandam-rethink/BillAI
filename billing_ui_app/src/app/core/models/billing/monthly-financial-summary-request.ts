
  export interface FinancialSummaryRow {
    monthYear: string;
    charges: number;
    insurancePay: number;
    patientPay: number;
    totalPay: number;
    adjustments: number;
    writeOffs: number;
    periodBalance: number;
    endingAR: number;
  }
  
  export interface UnappliedCredits {
    insuranceUnapplied: number;
    patientUnapplied: number;
    totalUnapplied: number;
  }
  
  export interface FinancialSummaryResponse {
    startingAR: number;
    dateBasis: string;
    rows: FinancialSummaryRow[];
    total: FinancialSummaryRow;
    unappliedCredits: UnappliedCredits;
  }


  // Funder-specific row interface
  export interface FunderFinancialSummaryRow {
    funderName: string;
    priorPeriodBalance: number;
    charges: number;
    insurancePay: number;
    patientPay: number;
    totalPay: number;
    adjustments: number;
    writeOffs: number;
    periodBalance: number;
    totalBalance: number;
  }

  // Funder-specific response interface
  export interface FunderFinancialSummaryResponse {
    dateBasis: string;
    rows: FunderFinancialSummaryRow[];
    total: FunderFinancialSummaryRow;
    unappliedCredits: UnappliedCredits;
  }
  
  export class FinancialSummaryBaseRequest {
  accountInfoId!: number;
  startDate!: Date;
  endDate!: Date;
  funderIds?: number[];
  locationIds?: number[];
  renderingProviderIds?: number[];
  billingProviderIds?: number[];
  dateType?: string;
  locationNames?: string[];
  billingProviderNames?: string[];
  renderingProviderNames?: string[];
}
