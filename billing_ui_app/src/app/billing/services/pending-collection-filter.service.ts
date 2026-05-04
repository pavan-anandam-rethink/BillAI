import { Injectable } from '@angular/core';
import { PendingCollectionFiltersComponent } from '../patient-invoice/pending-collection/pending-collection-filters/pending-collection-filters.component';

@Injectable({
  providedIn: 'root'
})
export class PendingCollectionFilterService {
  filter: PendingCollectionFiltersComponent;
  isFilterSet = false;
  constructor() { }

  setFilter(comp: PendingCollectionFiltersComponent) {

    if (comp.selectedPatients.length > 0 ||
      this.isNotEmpty(comp.patientResponsibilityFrom) ||
      this.isNotEmpty(comp.patientResponsibilityTo) ||
      this.isNotEmpty(comp.dateOfServiceFrom) ||
      this.isNotEmpty(comp.dateOfServiceTo) ||
      this.isNotEmpty(comp.invoiceFrom) ||
        this.isNotEmpty(comp.invoiceTo) ||
          this.isNotEmpty(comp.paymentDueFrom) ||
            this.isNotEmpty(comp.paymentDueTo)
      ) {
      this.isFilterSet = true;
    }
    this.filter = comp;

  }
  getFilter(): PendingCollectionFiltersComponent | null {
    if (this.isFilterSet) return this.filter;
    return null;
  }

  isNotEmpty(key) {
    if (!(key === undefined || key === null || key === "")) return true;
  }
}
