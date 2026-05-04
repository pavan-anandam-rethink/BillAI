import { BaseReminderSettingsData } from '../common';

export interface ReminderSettings {
  templateType: string;
  isClientSettingsAvailable?: boolean | null;
  isStaffSettingsAvailable?: boolean | null;
  accountInfoId?: number;
  staffMemberId?: number;
  personId?: number;
  clientSettingsDetails: ReminderSettingsDetailsModel;
  staffSettingsDetails: ReminderSettingsDetailsModel;
}


export class ReminderSettingsDetailsModel {
  isEmail: boolean;
  isSms: boolean;
  emailDays: number;
  emailHours: number;
  smsDays: number;
  smsHours: number;
  templates: TemplatesData[];
}

export enum ReminderType {
  New = 1,
  Cancelled = 2,
  Modified = 3
}

export interface ReminderSettingsData {
  id: number;
  isStaffSettingsAvailable: boolean | null;
  isClientSettingsAvailable: boolean | null;
  settingsLevel: string;
  staffMemberId: number | null;
  accountInfoId: number | null;
  personId: number | null;
  clientSettingsDetails: ReminderSettingsDetailsData;
  staffSettingsDetails: ReminderSettingsDetailsData;
  contactRemidersRequiered: boolean;
  dateDeleted: string | null;
  dateCreated: string;
  dateLastModified: string;
  createdBy: number;
  modifiedBy: number;
}

export interface ReminderSettingsDetailsData extends BaseReminderSettingsData {
  emailTemplate?: string;
  smsTemplate?: string;
  templates: TemplatesData[];
}

export interface TemplatesData {
  id?: number;
  accountInfoId?: number;
  templateType: string;
  recipientType?: string;
  emailText: string;
  smsText: string;
  dateDeleted?: string | null;
  dateCreated?: string;
  createdBy?: number;
  dateLastModified?: string;
  modifiedBy?: number;
}