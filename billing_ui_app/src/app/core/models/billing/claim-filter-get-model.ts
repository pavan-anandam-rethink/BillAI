import { ClaimListingTab } from '@core/enums/billing/claim-listing-tab';

export interface ClaimFilterGetModel {
    tab: ClaimListingTab | null;
    searchValue: string;
}