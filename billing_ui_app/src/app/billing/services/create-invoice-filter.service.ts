import { Injectable } from '@angular/core';
import { GridFilterModel } from '@core/models/common';
import { PatientInvoiceFilter } from '@core/models/billing/patient-invoice';
import { CreateInvoiceFiltersComponent } from '../patient-invoice/create-invoice/create-invoice-filters/create-invoice-filters.component';
import { PendingCollectionFiltersComponent } from '../patient-invoice/pending-collection/pending-collection-filters/pending-collection-filters.component';

@Injectable({
  providedIn: 'root'
})
export class CreateInvoiceFilterService {
  filter: CreateInvoiceFiltersComponent;
  isFilterSet = false;
  constructor() { }

  setFilter(comp: CreateInvoiceFiltersComponent) {

    if (comp.selectedPatients.length > 0 ||
      this.isNotEmpty(comp.patientResponsibilityFrom) ||
      this.isNotEmpty(comp.patientResponsibilityTo) ||
      this.isNotEmpty(comp.dateFromString) ||
      this.isNotEmpty(comp.dateToString)) {
      this.isFilterSet = true;
    }
    this.filter = comp;

  }

  getFilter(): CreateInvoiceFiltersComponent | null {
    if (this.isFilterSet) return this.filter;
    return null;
  }

  isNotEmpty(key) {
    if (!(key === undefined || key === null || key === "")) return true;
  }
}
