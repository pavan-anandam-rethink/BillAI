export class BillingCode {
    constructor(public id: number, public funderId: number, public billingCode: string, public displayName: string, public rate: number | null, public unitTypeId: number | null)
    {
    }
}

export interface ClientBillingCode {
    inactive: boolean | null;
    funderId: number;
    serviceName: string;
    billingCodeDescription: string;
    billingCodeId: number;
    billingCodeName: string;
    billingCodeName2: string;
    frequencyTypeId: number | null;
    noAuthRequired: boolean | null;
    providerServiceId: number | null;
    serviceLineId: number | null;
    unitTypeId: number;
    unitTypeId2: number | null;
    rate: number;
    rate2: number | null;
    used: boolean;
}

export interface BillingCodeOptionModel {
    billingCodeId: number;
    billingCodeName: string;
    unitTypeId: number;
    rate: number;
    isSecondaryCode: boolean;
}

export interface ServiceLineIdModel {
    accountInfoId: number,
    serviceLineId: number
}