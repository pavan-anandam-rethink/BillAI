import { SortDescriptor } from "@progress/kendo-data-query";
import { GridFilterModel } from '@core/models/common';
import { InsurancePaymentGridFilterModel } from "../common/grid-filter-model";

export class ListFilterSort {
    filterModels: GridFilterModel[] = [];
    sortingModels: SortDescriptor[] = [];
    skip: number = 0;
    take: number = 20;
    accountInfoId?: number = 0;
}

export class InsuranceListFilterSort {
    filterModels: InsurancePaymentGridFilterModel = new InsurancePaymentGridFilterModel();
    sortingModels: SortDescriptor[] = [];
    skip: number = 0;
    take: number = 20;
}