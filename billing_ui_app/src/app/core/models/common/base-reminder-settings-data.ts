export interface BaseReminderSettingsData {
  isEmail: boolean;
  isSms: boolean;
  emailDays: number;
  emailHours: number;
  smsDays: number;
  smsHours: number;
  phone: string;
  isClientEmailDisabled: boolean | null;
  isStaffEmailDisabled: boolean | null;
  isClientSmsDisabled: boolean | null;
  isStaffSmsDisabled: boolean | null;
}