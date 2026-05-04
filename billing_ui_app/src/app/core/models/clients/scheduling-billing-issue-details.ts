import { AuthRenderingProviderType } from '../company-account/funders/funder-options';
import { BasicOption } from '../common';
import { Address, Demographic } from '.';

export interface SchedulingBillingIssueDetails {
  id: number;
  name: string;
  coverageTypeId: number;
  medicalRecordNumber: number;
  address: Address
  dob: string
  gender: number;
  insuredFirstName: string;
  insuredLastName: string;
  relationshipToInsured: string;
  insuredAddress: Address;
  releaseOfInformationConfirmationTypeId: number;
  releaseOfInformationConfirmationDate: string;
  authorizedPaymentConfirmationTypeId: number;
  renderingProviderTypeId: number;
  renderingProviderStaffId: number;
  options: SchedulingBillingIssueOptions;
  insurancePolicyNumber: number | string;
}

export interface SchedulingBillingIssueOptions {
  client: Demographic;
  insuranceConfirmationTypes: BasicOption[];
  authorizationRenderingProviderTypes: AuthRenderingProviderType[];
  funderCoverageTypes: BasicOption[];
  states: BasicOption[];
  countries: BasicOption[];
  relationshipToInsuredOption: BasicOption[];
}