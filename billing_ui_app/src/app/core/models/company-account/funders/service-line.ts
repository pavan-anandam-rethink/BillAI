import { Funders } from './funder';
import { BillingCodeEntity } from './billing-code-entity';

export class ServiceLine {
  id: number;
  funderId: number;
  name: string;
  used: boolean;
  availableFunders: Funders[];
  staffServiceLineId?: number;
  funders: number[];
  hcChildProfileFunderServiceLineId?: number;
  billingCodes: BillingCodeEntity[];
  isActive: boolean;
  deactivationNotAllowed: boolean;
  billingSubmissionMethodId?: number;
  hasClientAuthorization: boolean;
  billingProviderOptionId?: number;
  authTypes: AuthType[];
}

export class AuthType {
  id: number;
  name: string;
  isActive: boolean;
  viewOnly: boolean;
}

