import { BaseReminderSettingsData } from '../common';

export interface ContactReminderSettingsData extends BaseReminderSettingsData {
  contactId?: number;
}