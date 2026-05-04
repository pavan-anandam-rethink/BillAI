import { Address, ProviderBaseModel } from '../clients';

export interface CompanyAccountLocation extends ProviderBaseModel {
  accountInfoId: number;
  name: string;
  phone: string;
  email: string;
  address: Address;
  isMainLocation: boolean;  
  fax: string;
  isBillingLocation: boolean;
  agencyName: string;
  federalTaxId: number;
  npiNumber: number;
  taxonomyCode: number;
  effectiveDate: string;
  providerCommercialNumber: string; 
  stateLicenseNumber: string;
  locationNumber: string;
  isInternational: boolean;
}

export interface CompanyAccountLocations {
  locations: CompanyAccountLocation[];
}
