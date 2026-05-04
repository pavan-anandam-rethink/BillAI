import { InsuranceListFilterSort, ListFilterSort } from "./payment-posting-list-filter-sort";

export class ClaimListFilterSort extends ListFilterSort {
    paymentId: number;
    id: number;
    showPaid: boolean;
}

export class InsuranceClaimListFilterSort extends InsuranceListFilterSort{
    accountInfoId: number;
    memberId: number;
    paymentId: number;
    id: number;
}