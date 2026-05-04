import { AcknowledgeableException } from './acknowledgeable-exception';
import { DayPilot } from 'daypilot-pro-angular';
import { EVVActionsTaken, EVVReason, EVVReasonCode } from '@core/services/scheduler/scheduler.service';
import { PlaceOfService, StaffMemberCredential } from '../company-account';
import { AcknowledgeableExceptionInfo } from './acknowledgeable-exception-info';

export class viewOnMap {
    static googleMapUrl: "https://maps.google.com/?saddr=";
    static googleMapUrlLocation: "https://maps.google.com/?q=";
}

export interface AppointmentSchedulingRules {
    timeOverlap: AppointmentEvent[];
    procedureOverlap: AppointmentEvent[];
    datePreventedOverlap: AppointmentEvent[];
    exceedAuthorizationHours?: boolean;
    exceedAuthorizationGoalHours?: boolean;
    eventEndDate: string;
    isSkipExceedAuthorizationHours?: boolean;
    isSkipOverlap?: boolean;
}

export interface AppointmentEvent {
    appt: AppointmentData;
    event: DayPilot.Event;
    apptItem: AppointmentData;
}

export class CalendarData {
    staffId: number;
    staffs: StaffData[];
    clients: ClientData[];
    appointmentDetails: AppointmentDetails;
    staffTimezoneUtcOffset: number;
    staffTimezoneDescription: string;
    companyStartHour: number;
    companyEndHour: number;
    defaultLocationId: number | null;
    isParent: boolean;
    clientAssignedOnly: boolean;
    adjustedEstOffset: number;
    isStaffSignatureOnFile: boolean;
    hasCompanyPrincipalSignature: boolean;
    weekStarts: number;
    globalLockDate: any;
    isParentVerificationRequired: boolean;
    isSessionNoteEnteredRequired: boolean;
    isStaffVerificationRequired: boolean;
    globalLockStaffExceptions: GlobalLockStaffException[];
    staffTimezoneId: string;
    isStaffTimezoneSupportDST: boolean;
}

export interface GlobalLockStaffException {
    appliedById: number;
    appliedByName: string;
    dateLastModified: string;
    effectiveDate: string;
    exceptionId: number;
    staffId: number;
    staffName: string;
}

export interface StaffData extends CommonData {
    conflict: boolean;
    available: boolean;
    credentials: StaffMemberCredential[];
    memberId: number;
    titleId: number;
    typeId: number;
    supervisorId: number;
    credentialId: number;
    ageGroups: number[];
    experienceId: number;
    canHandleAgression: boolean;
    assignedClients: number[];
    driveTimes: any;
    paycodes?: PayCode[];
    isSchedulingEnabled: boolean;
    clientAssignedOnly: boolean;
    currentTimeAvailable?: number;
    totalTimeAvailable?: number;
    events: AppointmentEvent[];
}

export interface ClientData extends CommonData {
    statusId: number;
    funders: Option[];
    authorizationNumbers: number | null;
    diagnosisCodes: Option[];
    procedureCodes: ExtOptions[];
    assignedStaffs: any[];
    unscheduledHours: number;
    scheduledHours: number;
    totalAuthorizedHours: number;
    services: Service[];
    isMissingBillingInfo: true;
    events: AppointmentEvent[];
    fetachedClientData: any;
}

export interface CommonData {
    id: number;
    name: string;
    initials: string;
    serviceLineId: number;
    languages: Option[];
    appointments: any[];
    availabilities: Availability[];
    locations: Location[];
    timezoneUtcOffset: number;
    timezoneDescription: string;
    statusInActive: boolean;
    clientFacilityId: number;
    percentage: number;
}

export interface AppointmentDetails {
    appointmentStatuses: OptionWithStringId[];
    appointmentTypes: Option[];
    occurrenceTypes: Option[];
    cancellationTypes: Option[];
    cancellationTags: Option[];
    dayTypes: Option[];
    serviceLines: Option[];
    services: Option[];
    providerBillingCodes: Option[];
    paycodes: Option[];
    locations: PlaceOfService[];
    states: Option[];
    countries: Option[];
    billUnder: Option[];
    staffTitles: Option[];
    staffTypes: Option[];
    staffSupervisors: Option[];
    credentials: StaffMemberCredential[];
    languages: Option[];
    ageGroups: Option[];
    experience: Option[];
    clientStatus: Option[];
    staffLocations: Option[];
    billableActivityTags: Option[];
    nonBillableActivityTags: Option[];
    paymentMethodTypes: Option[];
    evvReasons: EVVReasonCode[];
    acknowledgeableExceptions: AcknowledgeableExceptionInfo[];
    evvActionsTaken: EVVActionsTaken[];
}

export interface Service {
    id?: string;
    funderCopayTypeId: number;
    authorizationEndDayPilotDate: DayPilot.Date;
    authorizationStartDayPilotDate: DayPilot.Date;
    authorizationInactiveDate?: DayPilot.Date;
    selectedServiceLine?: Service;
    credentialIdList: any;
    funderId: number;
    serviceLineId: number;
    providerServiceId: number;
    procedureCodeId: number;
    clientFunderId: number | null;
    inactiveMapping: boolean;
    providerBillingCodeId: number;
    providerBillingCodeCredentialId: number;
    groupName: string;
    description: string;
    funderCopay: number;
    funderName: string;
    serviceLineName: string;
    serviceName: string;
    restrictToStaffCredential: boolean;
    credentialId: number;
    totalAuthHours: number;
    authorizationNumber: string;
    checkExceedAuthorizationWarning: boolean;
    authorizationStartDate: string;
    authorizationEndDate: string;
    billingCodeDescription: string;
    noOfUnits: number;
    unit: number;
    billingCode: string;
    billingCode2: string;
    schedulingGoalNoOfUnits: number | null;
    isMissingBillingInfo: boolean;
    isUntimed: boolean;
    isPendingSubmission: boolean;
    hcFrequencyTypeId: number;
    totalFrequencyUnit: number;
    schedulingGoalFrequencyTypeId: number;
    totalSchedulingFrequencyUnit: number;
    authorizedWeeklyHours: string;
    noAuthRequired: boolean;
    schedulingRules: SchedulingRules;
    preventBillableAppointmentDates: any[];
    attachedCredentialIds: any[];
    isDummy?: boolean;
    isDPH?: boolean;
    diagnosisEndDate: string;
    isAppointmentCurrentService?: boolean;
    isMissingReferringProvider?: boolean;
    clientFunderEndDate?: DayPilot.Date | string;
    clientFunderStartDate: DayPilot.Date | string;
}

export interface SchedulingRules {
    appointmentExpiredCertificationAlertId: number;
    appointmentDuplicateClientTimeAlertId: number;
    appointmentDuplicateClientTimeServiceAlertId: number;
    appointmentMissingBillingDataAlertId: number;
    appointmentExceedingAuthorizationAlertId: number;
}

export interface PayCode {
    isDefaultBillableAppointment: boolean | null;
    isDefaultNonBillableAppointment: boolean | null;
    isDefaultTravelAppointment: boolean | null;
    startDate: string;
    endDate: string | null;
    name: string;
    id: number;
    description: string;
    isActive: boolean | null;
}

export interface Option {
    id: number;
    description: string;
    name?: string | null;
    isActive?: boolean | null;
    isDPH?: boolean | null;
}

export type OptionWithStringId = {
    id: string;
    description: string;
};

export interface ExtOptions extends Option {
    authorizedHours: number;
    unscheduledHours: number;
    untimed: boolean;
    isValid: boolean;
}

export interface Availability {
    id: number;
    dayId: number;
    startHour: number;
    endHour: number;
    startMinute: number;
    endMinute: number;
}

export interface Location {
    id: number;
    name: string;
    description: string;
    address: Address;
    fullAddress: string;
}

export interface Address {
    id: number;
    street1: string;
    street2: string;
    city: string;
    stateId: number;
    zip: string;
    countryId: number;
    town: string;
    state: string;
    country: string;
}

export class AppointmentData {
    displayActualEndTime: string;
    displayActualStartTime: string;
    principalSignatureId: string;
    public occurrenceText: string;
    public occurrenceEndType: number;
    public formatedPrincipalVerificationDate: string;
    public principalVerificationDate?: string | DayPilot.Date;
    //principalVerificationDate: Date | null;
    public formatedClientVerificationDate: string;
    public formatedStaffVerificationDate: string;
    public isExceedSchedulingGoal: boolean;

    // TODO! remove exceedAuthorizedHours
    exceedAuthorizedHours: boolean;

    public isExceedAuthorizationHours: boolean;
    public startEventDate: any;
    public formatedEndTime: string;
    public formatedStartTime: string;
    public formatedEndDate: string;
    public formatedStartDate: string;
    actualStartDate: DayPilot.Date | string;
    actualEndDate: DayPilot.Date | string;

    validation: string;
    public description: string;
    public signatureParentName: string;
    public verifiedFromSessionNote?: boolean;
    public clientContactId: number;
    public clientLocation: string;
    public cancellationTypeName: string;
    public rescheduleDueDate: string;
    //rescheduleDueDate: Date;
    public rescheduleAssignedToId: number;
    public rescheduleAssignedToName?: string;
    public cancellationNote: string;
    public sessionNoteFormId: number;
    public sessionNoteComments: SessionNoteComment[];
    public staffSignature: number[] | string | null;
    public clientSignature: number[] | string | null;
    public principalSignature: number[] | string | null;
    public associatedAppointmentId: number;
    public diagnosisId: number;
    public address: Address | {};
    public startingAddress: Address;
    public endingAddress: Address;
    public sessionNoteReviewedOn?: string | DayPilot.Date;
    public sessionNoteReviewedByName: string;
    public sessionNoteReviewedByTitle: string;
    public copaymentMethodId?: number;
    public copaymentReferenceNumber: string;
    public copaymentAmountCollected?: number;
    public cancellationTagId?: number;
    public cancellationTagName: string;
    public authorizationNumber: string;
    public checkAuthorizationExceed: boolean;
    public verifiedById?: number;
    public verifiedByName: string;
    public verifiedByTitle: string;
    public rescheduleFromId?: number;

    public initAppointmentStartDate?: string | DayPilot.Date | null;
    public initAppointmentStartTime?: number;
    public initAppointmentEndTime?: number;
    //initAppointmentStartDate: Date | null;
    public missingSessionNotesFileCabinet?: boolean;

    public sessionNoteDraftFormId?: number;
    public sessionNoteDraftResponseId?: number;
    public sessionNoteDraftOn?: string | DayPilot.Date;
    public sessionNoteDraftStaffMemberId?: number;
    public sessionNoteDraftStaffMemberName: string;
    public linkedToEncounter: boolean;
    public linkedToApprovedEncounter: boolean;

    public hasTrialSetData?: boolean;
    public hasBehaviorPlanData?: boolean;

    public id: number;
    public staffId: number;
    public staffName?: string;
    public staffInitials: string;
    public staffTitle: string;
    public staffLocation: string;
    public staffSupervisorName: string;
    public clientId?: number;
    public clientName?: string;
    public clientInitials: string;
    public clientShortName: string;
    public occurrenceTypeId: number;
    public occurrenceFrequency: number;
    public frequencyInterval: number;
    public occurrenceTypeName: string;
    public statusId: number;
    public statusName: string;
    public startDate: string | DayPilot.Date;
    //startDate: Date;
    public endDate?: string | DayPilot.Date;
    //endDate: Date;
    public startTime: number;
    public endTime: number;
    public actualStartTime?: number;
    public actualEndTime?: number;
    public scheduledById: number;
    public dateCreated: Date;
    public appointmentTypeId: number;
    public dayTypes: number[];
    public monthDay: number;
    public monthTypeId?: number;
    public monthOccurrenceTypeId?: number;
    public monthOccurrenceDayId?: number;
    public appointmentTypeName: string;
    public staffVerificationDate?: string | DayPilot.Date;
    //staffVerificationDate: Date | null;
    public clientVerificationDate?: string | DayPilot.Date;
    //clientVerificationDate: Date | null;
    public parentVerification?: string | DayPilot.Date;
    public funderId?: number;
    public funderName: string;
    public funderIsActive: boolean;
    public stateId?: number;
    public evvClearingHouseId?: number;
    public serviceId?: number;
    public service?: Service;
    public providerServiceId?: number;
    public serviceName: string;
    public providerServiceName: string;
    public propagatingServiceName: string;
    public propagatingServiceEndDate?: Date;
    public fromLocationId?: number;
    public fromLocation: string;
    public fromLocationName: string;
    public toLocationId?: number;
    public toLocation: string;
    public toLocationName: string;
    public locationId?: number;
    public location: string;
    public locationName: string;
    public mileage?: number;
    public procedureCodeId: number;
    public staffSignatureId: string;
    public clientSignatureId: string;
    public events: AppointmentEvent[];
    public sessionNoteResponseId: number;
    public seriesAppointmentId?: number;
    public occurrenceEndDate?: string | DayPilot.Date;
    //occurrenceEndDate: Date | null;
    public statusDoeBilled?: string;
    public clientFunderId: number | null;
    public clientFunderStart: DayPilot.Date | string;
    public clientFunderEnd?: DayPilot.Date | string;

    public providerBillingCodeId?: number;
    public providerBillingCodeCredentialId: number | null;
    public billingCode: string;
    public renderingProvider: string;
    public expirationDate?: Date;
    public seriesAppointmentStartDate?: DayPilot.Date;
    public isParentVerificationRequired?: boolean;
    public isSessionNoteEnteredRequired?: boolean;
    public sessionNoteReviewedBy?: number;
    public authorizationUsedHours?: number;
    public activityTagName: string;
    public cancellationTypeId: number;
    public checkExceedAuthorizationHours?: boolean;
    public procedureCodeIdPreviousReference?: number;
    public notes: string;
    public clientContactName: string;
    public clientContactRelationship: string;
    public paycodeId: number;
    public paycode: string;
    public paycodeName: string;
    public signatureParentRelationship: string;
    public adminVerificationDate?: string;
    //adminVerificationDate: Date | null;
    public adminVerifiedBy?: number;
    public adminSignatureId?: number;
    public parentLatitude?: number;
    public parentLongitude?: number;
    public staffMemberLatitude?: number;
    public staffMemberLongitude?: number;
    public parentVerifiedAddress: string;
    public staffVerifiedAddress: string;
    public staffVerified: string;
    public clientVerified: string;
    public activityTagId?: number | null;

    public modifier1: string;
    public modifier2: string;

    public units: number;

    public dateBillingReported?: Date | string;
    public datePayrollReported?: Date | string;
    public modifiedBy: number;
    public dateLastModified?: Date | string;

    public status: string;

    public isEVV: boolean;
    public evvStatusId?: number;
    public evvStatusName: string;
    public evvReasons: EVVReason[];
    public acknowledgeableExceptions: AcknowledgeableException[];
    public evvRejectedReason?: string;

    public evvActionsTaken: EVVActionsTaken[];
    public evvActionTakenId?: number;

    public clockInLatitude: number | undefined;
    public clockInLongitude: number | undefined;
    public clockOutLatitude: number | undefined;
    public clockOutLongitude: number | undefined;
    public clockInAddress?: string;
    public clockOutAddress?: string;

    displayStartTime: string;
    displayEndTime: string;
    providerPrincipalSignature?: boolean;
    existingStartTime: number;
    existingEndTime: number;
    ApptStartTime: Date;
    ApptEndTime: Date;

    existingStartDate: string | DayPilot.Date;
    hasErrors: any;

    event: AppointmentEvent;

    authorizationStartDate: string;
    //authorizationStartDate: Date | null;
    authorizationEndDate: string;
    //authorizationEndDate: Date | null;


    authorizedWeeklyHours: number;
    sessionNotes: string;
    isClientDemo: boolean;

    savedAppointmentTypeId: number | null;
    eVVStatusId: number | null;
    eVVStatusName: string | null;
    eVVRejectedReason: string | null;
    initOccurrenceStartDate?: string | null;
    initOccurrenceEndDate?: string | null;
    dateDoeBilled: Date | null;
    occurrenceLookup: boolean;
    lastModified: Date;
    isMissingBillingInfo: boolean;
    isPendingSubmission: boolean;
    sessionNoteSubmitOn?: string | DayPilot.Date;
    sessionNoteSubmitBy: string | null;
    noAuthRequired: boolean;
    sessionNoteStatus: string | null;

    originalStartTime: number;
    originalEndTime: number;

    clearingHouseId?: number;
}

export interface ExceedAuthAppointmentData{
    id: number,
    date: string
}

export interface SessionNoteComment {
    id?: number;
    appointmentId: number;
    comment: string;
    reviewedById: number;
    reviewedByName: string;
    reviewedOn: DayPilot.Date;
    createdById: number;
    createdByName: string;
    dateCreated: DayPilot.Date;
}

export interface AppointmentEvent {
    id: number;
    uniqId: string;
    bubbleHtml: string;
    checkExceedAuthorizationHours: boolean;
    hours: number;

    // TODO! remove
    ExceedAuthorizedHours: boolean;

    isExceedAuthorizationHours: boolean;
    isExceedSchedulingGoal: any;
    minutes: number;
    procedureCodeId: number;
    resource: number;
    start: string;
    end: string;
    statusId: number;
    statusName: string;
    text: string;
    html: string;
    timeText: string;
    eventDate?: DayPilot.Date;
    date?: string;
    app: AppointmentData;
    cssClass: string;
    eventId: number;
    isClientEvent: boolean;
    clientId: number | null;
    staffId: number | null;
    appointmentTypeId: number;
    authorizedWeeklyHours: number;

    title: string;

    startEvent: string;

    sortStartDate: any;
    sortEndDate: any;
    eventDataMonth: any;
    eventDayName: any;
    eventIsToday: any;
}

export interface AttendeeShortData {
    id: number;
    name: string;
    memberId: number;
}

export interface ContextMenuObject {
    StartDate: DayPilot.Date;
    StartTime: number;
    EndTime: number;
    eventSelected: ContextMenuObjectEvent;
}

export interface ContextMenuObjectEvent {
    start: number;
    end: number;
    eventDate: DayPilot.Date;
}

export interface AppointmentWarnings {
    viewed: boolean,
    notAllowableWarnings: WarningModel[],
    allowableWarnings: WarningModel[]
}

export interface SortOption {
    isSort: boolean,
    clientSortType: string | null,
    staffSortType: string | null
}

export interface WarningModel {
    Description: string;
    OverlapType?: string;
    OverlapAppointments?: AppointmentEvent[];
    AlertName?: string;
}