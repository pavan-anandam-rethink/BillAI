import { EncounterStatus } from '@core/enums/billing';
import { PaymentInfo } from './payment-info';


export class Encounter {
    id: number;
    startDate: Date;
    endDate: Date;
    childProfileId: number;
    childName: string | null;
    memberId: number;
    memberName: string | null;
    locationCodeId: number;
    locationCode: string | null;
    authorizationId: number | null;
    authorizationNumber: string | null;
    locationId: number | null;
    locationName: string | null;
    primaryFunderId: number;
    billTo: number;
    primaryFunder: string | null;
    secondaryFunderId: number | null;
    secondaryFunder: string | null;
    status: number;
    note: string | null;
    hasAppointmentLinks: boolean;
    totalCharges: number;
    isAppointmentDeleted: boolean;
    paidAmount: number | null;
    dateLastModified: Date;
    encounterChargeInfoItems: PaymentInfo[];
    totalPayments: number;
    statusFrom: number;
    forbidAddAppointment?: number;
    billedPreviously?: boolean;
    isFlagged: boolean;
    isManual: boolean;

    constructor() {
        this.id = 0;
        this.startDate = new Date();
        this.endDate = new Date();
        this.childProfileId = 0;
        this.childName = null;
        this.memberId = 0;
        this.memberName = null;
        this.locationCodeId = 0;
        this.locationCode = null;
        this.authorizationId = null;
        this.authorizationNumber = null;
        this.locationId = null;
        this.locationName = null;
        this.primaryFunderId = 0;
        this.primaryFunder = null;
        this.secondaryFunderId = null;
        this.secondaryFunder = null;
        this.statusFrom = EncounterStatus.None;
        this.status = EncounterStatus.PendingReview;
        this.note = null;
        this.hasAppointmentLinks = false;
        this.totalCharges = 0;
        this.isAppointmentDeleted = false;
        this.paidAmount = 0;
        this.dateLastModified = new Date();
    }
}