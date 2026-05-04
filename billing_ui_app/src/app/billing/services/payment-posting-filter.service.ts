import { Injectable } from '@angular/core';
import { PaymentPostingListFilterComponent } from '../payment-posting';
import { GridFilterModel } from '@core/models/common';

@Injectable({
  providedIn: 'root'
})
export class PaymentPostingFilterService {
  filter: PaymentPostingListFilterComponent;
  isFilterSet = false;
  constructor() { }

  setFilter(comp: PaymentPostingListFilterComponent) {
  
      if (comp.selectedFunders.length > 0 ||
        comp.selectedStatuses.length > 0 || 
        comp.selectedPaymentMethods.length > 0 || 
        comp.selectedOption.length > 0 ||
        this.isNotEmpty(comp.referenceNumber)) {
        this.isFilterSet = true;
      }
      this.filter = comp;
     }
  
    getFilter(): PaymentPostingListFilterComponent | null {
      if (this.isFilterSet) return this.filter; 
      return null;
    }
  
    isNotEmpty(key) {
      if (!(key === undefined || key === null || key === "")) return true;
    }
}
