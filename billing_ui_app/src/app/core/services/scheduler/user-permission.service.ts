import { Injectable } from '@angular/core';

import { AccountPermissions } from '@core/enums/account';
import { AccountSubscriptionSettings } from '../../models/company-account';
import { AppointmentData } from '../../models/scheduler/calendar-data';
import { AccountMemberService } from '../account/account-member.service';

@Injectable({
    providedIn: 'root'
})
export class UserPermissionService {
    subscriptions: AccountSubscriptionSettings;

    constructor(
        private accountService: AccountMemberService
    ) {
    }


    canViewCalendar() {
        return this.accountService.checkPermissionLevel(AccountPermissions.SchedulerViewCalendar);
    }

    canViewAppointments() {
        return this.accountService.checkPermissionLevel(AccountPermissions.SchedulerViewAppointments);
    };

    canVerifyAppointments() {
        return this.accountService.checkPermissionLevel(AccountPermissions.SchedulerVerifyAppointments);
    }

    canAdminVerifyAppointments() {
        return this.accountService.checkPermissionLevel(AccountPermissions.SchedulerAdminVerify);
    };

    canBulkVerifyAppointments() {
        return this.accountService.checkPermissionLevel(AccountPermissions.SchedulerBulkVerify);
    };

    canApproveEVV() {
        return this.accountService.checkPermissionLevel(AccountPermissions.SchedulerApproveEVVAppointments);
    };

    canAddAppointments() {
        return this.accountService.checkPermissionLevel(AccountPermissions.SchedulerAddAppointments);
    }

    isAppointmentVerified(appointment: AppointmentData) {
        return (appointment.staffVerificationDate != '0001-01-01T00:00:00' && appointment.staffVerificationDate != null)
            || appointment.clientSignatureId != null
            || appointment.adminVerificationDate != null;
    }

    canEditAppts() {
        return this.accountService.checkPermissionLevel(AccountPermissions.SchedulerEditAppointments);
    }

    canEditAppointments(appointment: AppointmentData) {
        let appointmentVerified = this.isAppointmentVerified(appointment);
        let canEdit = this.accountService.checkPermissionLevel(AccountPermissions.SchedulerEditAppointments);;

        if (appointmentVerified) {
            canEdit = this.accountService.checkPermissionLevel(AccountPermissions.SchedulerEditVerifiedAppointments);
        }

        if (appointment.linkedToApprovedEncounter) {
            canEdit = canEdit && this.accountService.checkPermissionLevel(AccountPermissions.SchedulerEditApprovedAppointments);
        }

        return canEdit;
    }

    canDeleteAppointments(appointment: AppointmentData) {
        let appointmentVerified = this.isAppointmentVerified(appointment);

        if (appointmentVerified) {
            return this.accountService.checkPermissionLevel(AccountPermissions.SchedulerDeleteVerifiedAppointments);
        }

        return this.accountService.checkPermissionLevel(AccountPermissions.SchedulerDeleteAppointments);
    };

    canAppointmentVerify(apptStartDate: string) {
        if (apptStartDate == null) {
            return false;
        }

        var date = new Date(apptStartDate);
        if (date.getTime() > (new Date).getTime()) {
            return false;
        } else {
            return true;
        }
    }

    canAppointmentBeVerifiedByStaffWithEvvAccount() {
        return this.accountService.checkPermissionLevel(AccountPermissions.SchedulerOverrideClockInOutEVVAppointments);
    }

    canOverrideClockInOutEVVAppointments(isEVV: boolean) {
        if (isEVV == true) {
            return this.accountService.checkPermissionLevel(AccountPermissions.SchedulerOverrideClockInOutEVVAppointments);
        }

        return true;
    }
}
