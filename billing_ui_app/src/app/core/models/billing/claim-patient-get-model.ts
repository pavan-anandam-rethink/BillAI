import { ClaimListingTab } from '@core/enums/billing/claim-listing-tab';

export interface ClaimPatientGetModel {
    Tab: ClaimListingTab | 0;
    SearchValue: string;
    AccountInfoId: number;
}

export interface RenderingProviderGetModel {
    name: string;
    id: number;
    staffMemberId: number | null;

}