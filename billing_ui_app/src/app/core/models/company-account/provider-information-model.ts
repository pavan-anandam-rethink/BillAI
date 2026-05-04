import { MedicaidNumberModel } from "./medicaid-number/medicaid-number-model";
import { SaveMedicaidNumberModel } from "./medicaid-number/save-medicaid-number-model";

export class ProviderInformationModel {
  public appointmentReminderTypes: string;
  public billingProviderEmail: string;
  public billingProviderExtension: string;
  public billingProviderFax: string;
  public billingProviderName: string;
  public billingProviderPhone: string;
  public calendarEndHour: number;
  public calendarStartHour: number;
  public cancellationName: string;
  public clearingHouseId: number;
  public defaultLocationCodeId: number;
  public enableAppointmentReminders: boolean;
  public isParentVerificationRequired: boolean
  public isSessionNoteEnteredRequired: boolean
  public isStaffVerificationRequired: boolean
  public propagatingData: string;
  public tagName: string;

  public id: number;
  public name: string;
  public email: string;
  public phone: string;
  public fax: string;
  public city: string;
  public website: string;
  public federalTaxId: string;
  public npiNumber: string;
  public showLogoOnRbt: boolean;
  public isInternational: boolean;
  public timezoneId: number;
  public logoPhoto: string;
  public includeLogo: boolean;
  public useCustomDomains: boolean;
  public programCode: string;

  public requireAppointmentLocation: boolean;
  public allowToVerifyWithoutAuthorization: boolean;
  public allowToEnterLocation: boolean;
  public contactRemidersRequiered: boolean;

  public medicaidNumbers: MedicaidNumberModel[] | SaveMedicaidNumberModel[];
}

export class Provider {
  enablePropagatingData: boolean;
  propagatingAppointmentDataTypes: PropagatingAppointmentDataTypes[];
  providerInformationData: ProviderInformationModel;
}

export class PropagatingAppointmentDataTypes {
  id: number;
  description: string;
  name: string;
  isActive: boolean;
}

export class LogoUrl {
  success: boolean;
  downloadUrl: string;
}

export interface FileUploadSuccess {
  file: string;
  filePath: string;
  fileMimeType: string
  fileLink: string
}
