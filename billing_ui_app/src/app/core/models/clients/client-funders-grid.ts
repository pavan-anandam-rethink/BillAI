import {ListFilterSort} from "@core/models/billing";

export interface ClientFundersGrid {
    childProfileId: number;
    showInactive: boolean;
    listSortModel: ListFilterSort
}