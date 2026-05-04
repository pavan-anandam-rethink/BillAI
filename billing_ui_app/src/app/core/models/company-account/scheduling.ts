export class SchedulingTag {
    active: string;
    canDelete: boolean;
    customTagTypeId: number;
    customTagTypeName: string;
    id: number;
    isActive: boolean;
    isArchived: boolean;
    isSelected: boolean;
    isUserEntry: boolean;
    name: string;
    isIEP: boolean;
    isMastered: boolean;
    appointmentCustomTagTypeId: number;
}

export class PayOver {
    dayTypeId: number;
    startCalendarWorkweekDayTypeId: number;
    endWorkweekDayTypeId: number;
    id: number;
    isActiveOverTime1: boolean;
    isActiveOverTime2: boolean;
    isArchived: boolean;
    isSelected: boolean;
    isUserEntry: boolean;
    name: string;
    overTimeRate1: number;
    overTimeRate2: number;
    overTimeRateDecimal1: number
    overTimeRateDecimal2: number
    overTimeRateDigit1: number
    overTimeRateDigit2: number
    startOverTimeBy7Day1: number;
    startOverTimeBy7Day2: number;
    startOverTimeByDay1: number;
    startOverTimeByDay2: number;
    startOverTimeByWeek1: number;
    startOverTimeByWeek2: number;
    startWorkweekDayTypeId: number;
    hcPayOverOptionId: number;
    isIEP: boolean;
    isMastered: boolean;
    workweekId: number;
    workweekLastChange: string;
}

export class SchedulingOptions {
    billableTags: SchedulingTag[];
    cancellationTags: SchedulingTag[];
    nonbillableTags: SchedulingTag[];
    payOver: PayOver;
    placeOfServices: PlaceOfService[];
    insuranceFunderPolicies: any[];
}

export class AppointmentReminder {
    client: boolean;
    clients: BasicAppointmentReminder;
    staffMember: boolean;
    staffMembers: BasicAppointmentReminder;
}

export class BasicAppointmentReminder {
    email: boolean;
    emailDays: number;
    emailHours: number;
    emailTemplate: string;
    sms: boolean;
    smsDays: number;
    smsHours: number;
    smsTemplate: string;
}

export class PlaceOfService {
    id: number;
    code: string;
    description: string;
    isActive?: boolean;
    isDPH?: boolean;
}

export class SchedilingBasicInfoSettings {
    public appointmentReminderTypes: string;
    public enableAppointmentReminders: boolean;

    public calendarEndHour: number;
    public calendarStartHour: number;

    public isParentVerificationRequired: boolean;
    public isSessionNoteEnteredRequired: boolean;
    public isStaffVerificationRequired: boolean;

    public requireAppointmentLocation: boolean;
    public allowToVerifyWithoutAuthorization: boolean;
    public insuranceFunderPolicy: number;
    public allowToEnterLocation: boolean;
}