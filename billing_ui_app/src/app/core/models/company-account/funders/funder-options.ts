import { Jurisdiction } from './jurisdiction';
import { SandataState } from './sandata-state';
import { BasicOption } from '@core/models/common';
import { GetMedicaidNumberOption, StaffMemberCredential } from '..';
import { BillingCodeTemplateModel } from '../billing-code-template';
import { ClearingHousePayer } from './clearing-house-view';

export interface FunderOptions {
  funderTypes: BasicOption[];
  alertOptions: BasicOption[];
  funderCoverageTypes: BasicOption[];
  combineChargeTypes: BasicOption[];
  billingProviderOptions: BasicOption[];
  credentials: StaffMemberCredential[];
  billingCodeDurations: BasicOption[];
  billingCodeTemplates: BillingCodeTemplateModel[];
  billingCodeRateTypes: BasicOption[];
  billingCodeRoundingTypes: BasicOption[];
  billingSubmissionMethods: BasicOption[];
  billingUnitTypes: BasicOption[];
  providerLocations: BasicOption[];
  clearingHousePayers: ClearingHousePayer[];
  authRenderingProviders: AuthRenderingProviderType[];
  billingIntervals: BasicOption[];
  electronicVisitVendors: BasicOption[];
  sandataStates: SandataState[];
  jurisdictions: Jurisdiction[];
  medicaidNumbers: GetMedicaidNumberOption[];
}

export interface AuthRenderingProviderType {
  name: string;
  id: number;
  staffMemberId?: number;
}
