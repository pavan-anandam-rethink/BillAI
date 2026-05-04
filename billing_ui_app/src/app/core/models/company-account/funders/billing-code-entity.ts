
export interface BillingCodeEntity {
  appointmentAssginedStaffs: AppointmentAssginedStaffs[];
  billingCodeCredentials: BillingCodeCredentials[];
  billingCodeId: number;
  billingCodeName: string;
  billingCodeName2: string | null;
  billingCodeRateTypeId: number;
  billingCodeRoundingTypeId: number;
  billingCodeRoundingTypeId2: number | null;
  canDelete: boolean;
  code: string;
  combined: boolean;
  description: string | null;
  duration: number | null;
  durationTypeId: number | null;
  frequency: string;
  frequencyId: number | null;
  funderId: number;
  inactive: boolean | null;
  modifier: string | null;
  noAuthRequired: boolean;
  rate: number;
  rate2: number | null;
  renderingProviderStaffId: number | null;
  renderingProviderTypeId: number;
  restrictStaffProviderToService: boolean;
  serviceId: number;
  serviceLineId: number;
  serviceName: string;
  templateId: number;
  unitType: string;
  unitType2: string | null;
  unitTypeId: number;
  unitTypeId2: number;
  id: number;
  hasError?: boolean,
  errorMessage?: string,
  includeKareoSvcApptTime?: boolean, 
  sessionTimeToMilitaryTime?: boolean 
}

export interface AppointmentAssginedStaffs {
  certifcationId: number;
  certifcationName: string;
  staffId: number;
  staffName: string;
  certifications: { id: number;  name: string; }[];
}

export interface BillingCodeCredentials {
  id?: number;
  billingCodeId?: number | null;
  credentialId: number | null;
  contractRate: number | string;
  isPrimary: boolean;
  modifier1: string;
  modifier2: string;
  modifier3: string;
  modifier4: string;
  modifier2Name: string;
  canDelete: boolean;
}
