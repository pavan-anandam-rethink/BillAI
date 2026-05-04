export interface ReferringProvider extends ReferringProviderForDropdown {
    childProfileId: number;

    firstName: string;
    lastName: string;

    taxonomyCode: string;
    facilityName: string;

    address1: string;
    address2: string;
    city: string;
    zip: string;

    stateId: number;
    stateName: string;
    phone: string;
    fax: string;
    isActive: boolean;
}

export interface ReferringProviderWithData extends ReferringProvider {
    isActiveOnCompany: boolean;
    taxonomyCodeList: string[];
}

export interface ReferringProviderForDropdown {
    id: number;
    providerName: string;
    npi: string;
    isActive: boolean;
    alreadyUsed: boolean;
}

export interface ClientReferringProviderForDropdown {
    id: number;
    providerName: string;
    isDefault: boolean;
    isActive: boolean;
}