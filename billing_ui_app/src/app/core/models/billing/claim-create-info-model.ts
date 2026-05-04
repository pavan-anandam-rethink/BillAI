import { CompanyAccountLocation } from '@core/models/company-account';
import { ClientBillingCode } from './billing-code';
import { ClientReferringProviderForDropdown } from '@core/models/clients/referring-provider';
import { ClientRenderingProvider } from '../clients/rendering-provider';

export interface ClaimCreateInfoModel {
    billingCodes: ClientBillingCode[];
    locations: CompanyAccountLocation[];
    referringProviders: ClientReferringProviderForDropdown[];
    renderingProviders: ClientRenderingProvider[];
}

export interface ClaimCreateInfoGetModel {
    clientId: number;
    funderId: number;
    serviceId: number;
    accountInfoId:number;
}