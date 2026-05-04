import { Injectable } from '@angular/core';
import { AppointmentModel } from '@app/components/scheduler/models/appointment'
import * as moment from 'moment';

@Injectable({
    providedIn: 'root'
})
export class SchedulerScope {
    appointmentModel: AppointmentModel;

    appointmentWarnings: any;

    timestamp: string;

    isIndividual: boolean = false;
    isGroup: boolean = false;
    isAppointmentList: boolean = false;
    isGlobalLock: boolean = false;

    loadingData: boolean;

    serviceLineChanged: boolean;

    currentLocale = "en-us";
    $rootScope: any = {
        isMobileDevice: () => {
            return false;
        }
    }

    weekConfig: any;
    monthConfig: any;
    mobileConfig: any;
    navigatorConfig: any;

    month: any;
    week: any;
    seriesSingle: any;
    appointment: AppointmentModel;
    eventSelectedDate: any;
    tempEndTime: string;
    editType: string;
    apptDate: any;
    DefaultLocationId: any;
    currentFilter: string;
    task: string;
    perimeter: any;
    appointmentDateChanged: boolean;
    appointmentTimeChanged: boolean;
    occurenDateEdit: null;
    isMobileSaveClicked: boolean;

    constructor(

    ) {
    }

    CONSTANTS = {
        ZERO: 0,
        ONE: 1,
        DEFAULT_END_DATE: '0001-01-01T00:00:00'
    }

    CurrentGeoLocation = { latitude: null, longitude: null, address: null };

    // add AppointmentTypes to use
    MonthOccurrenceTypes = [
        { Id: 1, Description: "first" },
        { Id: 2, Description: "second" },
        { Id: 3, Description: "third" },
        { Id: 4, Description: "fourth" },
        { Id: 5, Description: "last" }
    ];

    startDate = moment().subtract(1, 'months').startOf('day');
    endDate = moment().add(1, 'months').startOf('day');

    self = this;
    myScope = this;
    sessionNotes = new Array();
    // scope appointments to be used for the calendars
    events = [];

    isScheduler = true;
    isAddEditAppointment = false;

    /* Mobile View */
    isMobile = window.matchMedia("only screen and (max-width: 1024px)");
    isMobileDevice = this.$rootScope.isMobileDevice();

    isWaitingOnRequest = this.$rootScope.isWaitingOnRequest;

    eventsForMobileView = new Array();
    isFromMobile = this.isMobileDevice;

    appointmentDays = null;
    selectedStaff: any = null;
    selectedClient: any = null;
    filteredClient: any = null;
    isStaff = null;
    isAvailable = true;
    isBothAddressEntered = false;
    availabilities: any;
    selectedViewType = 'Week';

    isExceedAuthorizationOn = false;

    // define the navigatorConfig

    mobileNavigatorConfig =
        {
            locale: this.currentLocale,
            selectMode: "Week",
            showMonths: 3,
            skipMonths: 1,
            weekStarts: 1,
            cellHeight: 41,
            cellWidth: 41,
            dayHeaderHeight: 41,
            titleHeight: 41,
            onTimeRangeSelected: (args: any) => {
                /*this.$apply(() => {
                    this.executeMobileView();
                    this.scrollToTodayDate(args.day);
                });*/
            },
            onVisibleRangeChanged: (arg: any) => {
            }
        };

    processingShowAddPop = false;

    setSeriesSingle(value: any) {
        this.seriesSingle = value;
    }

    // helper methods to show week, month or group

    getDayPilotDateFirstMonday(date: any) {
        var day = date.getDayOfWeek();
        var diff = 0;
        if (day == 0) {
            diff = -6;
        }
        if (day > 1) {
            diff = (1 - day);
        }
        return date.addDays(diff);
    }

    getDayPilotDateNextSunday(date: any) {
        // start on next sunday
        var day = date.getDayOfWeek();
        var diff = 0;
        if (day > 0) {
            diff = (7 - day);
        }
        return date.addDays(diff);
    }

    getIndividualStartDate() {
        if (this.weekConfig.visible == true) {
            return this.getDayPilotDateFirstMonday(this.week.startDate);
        }
        else {
            return this.getDayPilotDateFirstMonday(this.month.startDate.firstDayOfMonth());
        }
    }

    getIndividualEndDate() {
        var startDate = this.getIndividualStartDate();

        if (this.weekConfig.visible == true) {
            return startDate.addDays(6);
        }
        else {
            var start = this.month.startDate.firstDayOfMonth();
            start = start.addMonths(1).addDays(-1);

            return this.getDayPilotDateNextSunday(start);
        }
    }
}