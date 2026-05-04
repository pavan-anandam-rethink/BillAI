import { AccountRole } from './account-role';

export class SchedulingTypes {
  dayTypes: DayTypes[];
  employeeTypes: Types[];
  payOverOptions: Types[];
  roleTypes: Types[];
  staffMileages: StaffMileages[];
  staffPaycodes: StaffPaycodes[];
  staffStatuses: StaffStatuses[];
  staffTitles: StaffTitles[];
}

export class Types {
  canDelete: boolean;
  dateLastModified: string;
  deactivationNotAllowed: boolean;
  description: string;
  id: number;
  isArchived: boolean;
  isIEP: boolean;
  isMastered: boolean;
  isSelected: boolean;
  isUserEntry: boolean;
  name: string;
}

export class DayTypes extends Types {
  startDayTypeId: number;
  endDayTypeId: number;
}

export class StaffMileages {
  accountInfoId: number;
  dateDeleted: string;
  id: number;
  mileage: number;
}

export class StaffPaycodes {
  accountInfoId: number;
  dateDeleted: string;
  id: number;
  name: string;
  paycode: string;
}

export class StaffStatuses {
  accountInfoId: number;
  dateDeleted: string;
  id: number;
  isActive: boolean;
  name: string;
}

export class StaffTitles {
  accountInfoId: number;
  accountRole: AccountRole;
  createdBy: number;
  dateCreated: string;
  dateDeleted: string;
  dateLastModified: string;
  description: string;
  id: number;
  modifiedBy: number;
  name: string;
  roleTypeId: number;
}


