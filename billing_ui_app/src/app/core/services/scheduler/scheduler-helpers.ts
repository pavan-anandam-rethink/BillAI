import {
    AppointmentData,
    CalendarData,
    ClientData,
    PayCode,
    Service,
    StaffData,
} from '@core/models/scheduler/calendar-data';

export function getApptAdditionalStringValues(appt: AppointmentData, calendar: CalendarData) {
    let clientName = appt.clientName || null;
    if (appt.clientId && !clientName) {
        const client = calendar.clients.find(c => c.id === appt.clientId);
        clientName = client && client.name || null;
    }

    let staffName = appt.staffName || null;
    if (appt.staffId && !staffName) {
        const staff = calendar.staffs.find(s => s.id === appt.staffId);
        staffName = staff && staff.name || null;
    }

    let paycodeName = appt.paycodeName || null;
    if (appt.paycodeId && !paycodeName) {
        const paycodes = getCalendarPaycodes(calendar);
        const paycode = paycodes[appt.paycodeId];
        paycodeName = paycode && paycode.name || null;
    }

    let appointmentType: string | null = appt.appointmentTypeName;
    if (appt.appointmentTypeId && !appointmentType) {
        const apptType = calendar.appointmentDetails.appointmentTypes.find(t => t.id === appt.appointmentTypeId);
        appointmentType = apptType && apptType.description || null;
    }

    let tag = appt.activityTagName || null;
    if (appt.activityTagId && !tag) {
        const t = [
            ...calendar.appointmentDetails.billableActivityTags,
            ...calendar.appointmentDetails.nonBillableActivityTags,
        ].find(t => t.id === appt.activityTagId);

        tag = t && t.description || null;
    }

    let serviceName = appt.providerServiceName || appt.serviceName || appt.service && appt.service.serviceName || null;
    // if (appt.serviceId && !serviceName) {
    //   const services = getCalendarServices(calendar);
    //   const service = services[appt.serviceId];

    //   serviceName = service && service.description || null;
    // }

    let placeOfService = appt.locationName || null;

    let fromLocation = appt.fromLocationName || null;

    let toLocation = appt.toLocationName || null;

    let cancellationType = appt.cancellationTypeName || null;

    let cancellationTag = appt.cancellationTagName || null;

    let assignTaskTo = appt.rescheduleAssignedToName || null;

    let evvStatus = appt.evvStatusName || null;

    return {
        clientName,
        staffName,
        paycode: paycodeName,
        appointmentType,
        tag,
        serviceName,
        placeOfService,
        fromLocation,
        toLocation,
        cancellationType,
        cancellationTag,
        assignTaskTo,
        evvStatus
    };
}

export function getCalendarPaycodes(cd: CalendarData) {
    return cd.staffs.reduce<{ [key: string]: PayCode }>((acc, staff) => {
        return {
            ...acc,
            ...getStaffPaycodes(staff)
        };

    }, {});
}

export function getStaffPaycodes(staff: StaffData) {
    if (!staff.paycodes) {
        return {};
    }

    return staff.paycodes.reduce<{ [key: string]: PayCode }>((acc, pc) => {
        acc[pc.id] = pc;
        return acc;
    }, {});
}

export function getClientServices(client: ClientData) {
    return client.services.reduce<{ [key: string]: Service }>((acc, s) => {
        acc[s.serviceLineId] = s;
        return acc;
    }, {});
}

export function getCalendarServices(cd: CalendarData) {
    return cd.clients.reduce<{ [key: string]: Service }>((acc, client) => {
        return {
            ...acc,
            ...getClientServices(client)
        };

    }, {});
}

export interface AdditionalTextValues {
    clientName: string;
    staffName: string;
    paycode: string;
    appointmentType: string;
    tag: string;
    serviceName: string;
    placeOfService: string;
    fromLocation: string;
    toLocation: string;
    cancellationType: string;
    cancellationTag: string;
    assignTaskTo: string;
}
