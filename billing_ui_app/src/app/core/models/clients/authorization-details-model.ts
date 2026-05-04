export interface AuthorizationDetailsViewModel {
    billing: AuthorizationDetailsBillingModel;
}

export interface AuthorizationDetailsBillingModel {
    billingPropagatingDate?: Date;
    billingPropagatingType?: string;
    billingPropagatingTypeId?: number;
    billingProviderId?: number;
    referringPropagatingDate?: Date;
    referringPropagatingType?: string;
    referringPropagatingTypeId?: number;
    referringProviderId?: number;
    renderingPropagatingDate?: Date;
    renderingPropagatingType?: string;
    renderingPropagatingTypeId?: number;
    renderingProviderId: number;
    renderingProviderTypeId: number;
    servicePropagatingDate?: Date;
    servicePropagatingType?: string;
    servicePropagatingTypeId?: number;
    serviceProviderId?: number;
}