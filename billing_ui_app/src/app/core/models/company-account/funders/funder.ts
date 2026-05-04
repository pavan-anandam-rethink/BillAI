import { Address } from '@core/models/clients';
import { ServiceLine } from './service-line';
import { CaseManager } from './case-manager';
import { PreventableDate } from './preventable-date';
import { PropagatingAppointmentData } from './propagating-appointment-data';

export class Funders {
    id: number;
    serviceFunderId: number;
    address: Address;
    name: string;
    clientCount: number;
    isActive: boolean;
    email: string;
    fax: string;
    phone: string;
    serviceLineOptions: ServiceLine[];
    caseManagerOptions: CaseManager[];
    funderTypeId: number;
    funderType: string;
    activeAuthExist: boolean;
    canDelete: boolean;
    vendorId: string;
    billingCombineCharges?: boolean;
    referringProviderRequiredOnClaim: boolean;
    appointmentDuplicateClientTimeAlertId?: number;
    appointmentDuplicateClientTimeServiceAlertId?: number;
    appointmentMissingBillingDataAlertId?: number;
    appointmentExceedingAuthorizationAlertId?: number;
    providerLocationId?: number;
    providerLocationName: string;
    coverageTypeId?: number;
    note: string;
    preventableDates: PreventableDate[];
    propagatingData: PropagatingAppointmentData;
    kareoInsuranceCompanyId?: number;
    clearingHousePayerId?: number;
    insurancePlans: InsurancePlans[];
    billingProviderOptionId?: number;
    billingProviderNotSelected?: boolean;
    combineChargeTypeId?: number;
    allowOverlappingAppointments?: boolean;
    isSelected?: boolean;
    appointmentExpiredCertificationAlertId: number;
    includeKareoSvcApptTime?: boolean;
    electronicVisitVendorId: number;
    stateId?: number;
    businessEntityId: number;
    userId: string;
    password: string;
    payerId: string;
    programId: string;
    jurisdictionId?: number;
    sessionTimeToMilitaryTime?: boolean;
    isDPH: boolean;
    bypassAssignedClientLocation: boolean;
    bypassServiceLine: boolean;
    bypassServiceName: boolean;
    medicaidIdNumberId?: number;
    hhaxClientId?: string;
    hhaxClientSecret?: string;

    constructor() {
        this.serviceLineOptions = [];
        this.caseManagerOptions = [];
        this.preventableDates = [];
        this.insurancePlans = [];
    }
}

class InsurancePlans {
    id: number;
    planName: string;
    dateDeleted: string;
}