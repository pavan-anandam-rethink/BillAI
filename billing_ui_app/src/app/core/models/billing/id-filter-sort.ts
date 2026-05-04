import {ListFilterSort} from "@core/models/billing/payment-posting-list-filter-sort";

export class IdFilterSort extends ListFilterSort {
    Id: number;
    MemberId?: number;
    AccountInfoId?: number;
}