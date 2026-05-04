import { Injectable } from '@angular/core';
import { ClientHistoryInvoiceFilterComponent } from '../client-history/client-history-invoice-filter/client-history-invoice-filter.component';

@Injectable({
  providedIn: 'root'
})
export class ClientHistoryInvoiceFilterService {
  filter: ClientHistoryInvoiceFilterComponent;
  isFilterSet = false;
  constructor() { }

  setFilter(comp: ClientHistoryInvoiceFilterComponent) {

    if (comp.selectedStatus.length > 0 ||
      this.isNotEmpty(comp.patientResponsibilityFrom) ||
      this.isNotEmpty(comp.patientResponsibilityTo) ||
      this.isNotEmpty(comp.dateOfServiceFrom) ||
      this.isNotEmpty(comp.dateOfServiceTo) ||
      this.isNotEmpty(comp.invoiceDateFrom) ||
      this.isNotEmpty(comp.invoiceDateTo) ||
      this.isNotEmpty(comp.invoiceDueDateFrom) ||
      this.isNotEmpty(comp.invoiceDueDateTo) ||
      this.isNotEmpty(comp.patientBalanceFrom) ||
      this.isNotEmpty(comp.patientBalanceTo)
      ) {
      this.isFilterSet = true;
    }
    this.filter = comp;

  }
  getFilter(): ClientHistoryInvoiceFilterComponent | null {
    if (this.isFilterSet) return this.filter;
    return null;
  }

  isNotEmpty(key) {
    if (!(key === undefined || key === null || key === "")) return true;
  }
}