import { StaffMemberCredential, StaffMemberCredentialType } from '.';
import { CustomField } from '../common';
import { BasicItem } from './basic';
import { StaffMileageData } from './staff-member-settings/staff-mileage-data';
import { StaffPaycodeData } from './staff-member-settings/staff-paycode-data';
import { StaffStatusData } from './staff-member-settings/staff-status-data';
import { StaffTitleModel } from './staff-member-settings/staff-title-model';

export class Type {
  employeeTypes: BasicItem[];
  staffTitles: StaffTitleModel[];
  staffStatuses: StaffStatusData[];
  staffPaycodes: StaffPaycodeData[];
  staffMileages: StaffMileageData[];
  locationCodes: BasicItem[];
  payOverOptions: BasicItem[];
  roleTypes: BasicItem[];
  noteTypes: BasicItem[];
  credentialTypes: StaffMemberCredentialType[];
  credentials: StaffMemberCredential[];
  customFields: CustomField[];
}