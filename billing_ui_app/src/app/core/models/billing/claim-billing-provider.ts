/**
 * Model for manual billing provider data when "Other" is selected
 * Maps to ClaimBillingProvider table
 */
export interface ClaimBillingProviderModel {
    claimId?: number;
    providerType: 'Entity' | 'Person';
    firstName: string;
    lastNameOrFacilityName: string;
    npi: string;
    taxId: string;
    taxonomyCode: string;
    addressLine1: string;
    addressLine2: string;
    city: string;
    state: string;
    zip: string;
    zipExt: string;
}

/**
 * Request wrapper for billing provider with claimId
 */
export interface BillingProviderRequest {
    claimId: number;
    billingProvider: ClaimBillingProviderModel;
}

/**
 * Response DTO from GET /api/claims/{claimId}/billing-provider-other
 */
export interface ClaimBillingProviderDto {
    claimId: number;
    providerType: string;
    firstName: string;
    lastNameOrFacilityName: string;
    npi: string;
    taxId: string;
    taxonomyCode: string;
    addressLine1: string;
    addressLine2: string;
    city: string;
    state: string;
    zip: string;
    zipExt: string;
}
