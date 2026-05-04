import { SortDescriptor } from '@progress/kendo-data-query';
import { BillingFeatures } from './claim-filingIndicator-model';
import { GridFilterModel } from '../common/grid-filter-model';


export class BillingFunderSettingRequestModel {
  accountInfoId: number;
  funderId: number;
  funderName: string;
  clearingHousePayerName: string;
  clearingHousePayerId: number;
  insuranceType: string;
  billingFeatures: BillingFeatures[];
}

export class BillingFunderListRequestModel {
  accountInfoId: number;
  memberId: number;
  skip: number;
  take: number;
  sortingModels: SortDescriptor[];
  filterModels: GridFilterModel[];
}

export interface BillingFunderSettingResponseModel {
  data: BillingFunderSettings[];
  total: number;
  timeZone: { [key: number]: string };
  claimFilingIndicator: ClaimFilingIndicatorModel[];
}

export interface BillingFunderSettings {
  id: number;
  funderId: number;
  funderName: string;
  clearingHousePayerName: string;
  clearingHousePayerId: number;
  insuranceType: string;
}

export interface ClaimFilingIndicatorModel{
  id: number;
  indicator: string;
}
