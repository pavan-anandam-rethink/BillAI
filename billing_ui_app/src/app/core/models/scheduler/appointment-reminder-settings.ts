export interface AppointmentReminderTabData {
    appointmentId: number;
    isAvailable?: boolean;
    clientContacts: RecipientReminderData[];
    staffMember: RecipientReminderData;
    startDate: string;
    appointmentDateChanged: boolean;
}

export interface RecipientReminderData {
    id: number;
    isChecked: boolean;
    name: string;
    additionalInfo: string;
}

export interface RecipientReminderModel {
    appointmentId: number;
    staffMemberId?: number;
    clientContactId?: number;
    isChecked?: boolean;
}