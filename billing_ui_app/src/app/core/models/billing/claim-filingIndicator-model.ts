export interface ClaimFilingIndicatorModel {
  id: number;
  indicator: string;
  checked?: boolean;
}


export interface BillingFeatures{
  featureId: number;
  featureName: string;
  isEnabled: boolean;
}

export interface BillingSettingInformationModel {
  payToAddressOverrideOption: number;
  companyName?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  zip?: string;
  zipExtension?: string;
  dunningMessage?: string;
  globalMessage?: string;
}

export interface BillingDefaultModel extends billingProviderAddress {
  payToOverride: string;
  address1?: string;
  address2?: string;
  dunningMessage?: string;
  globalMessage?: string;
}

export interface SaveBillingSettingRequest extends BillingSettingInformationModel {
  accountId: number; 
}

export interface billingProviderAddress
{
  companyName?: string;
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  zip?: string;
  zipExtension?: string;
}

export interface BillingFunderIdRequestModel {
  id: number;
  accountInfoId: number;
  funderId: number;
  funderName: string;
  scheduleType: number;
  scheduleTime: string | null;
  scheduleTimeZone: string;
  weeklyDays: string;
  monthlyFrequency: string;
  combineChargesForSameClient: boolean | null;
  claimFilingIndicatorId: number;
  includeTaxonomyCode: boolean | null;
}
