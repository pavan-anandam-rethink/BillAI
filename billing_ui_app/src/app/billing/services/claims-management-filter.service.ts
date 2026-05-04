import { Injectable } from '@angular/core';
import { ClaimFiltersComponent } from '../encounters/ecnounter-list/claim-filters/claim-filters.component';
import { ClaimHeaderFilter } from '@core/models/billing/claim-header-search';

@Injectable({
  providedIn: 'root'
})
export class ClaimsManagementFilterService {

  filter: ClaimFiltersComponent;
  isFilterSet = false;
  constructor() { }

  setFilter(comp: ClaimFiltersComponent, filter: ClaimHeaderFilter) {

    if (this.isNotEmpty(filter.patientIds) ||
    this.isNotEmpty(filter.reasonCode) ||
    this.isNotEmpty(filter.funderIds) ||
    this.isNotEmpty(filter.balanceFrom) ||
    this.isNotEmpty(filter.balanceTo) ||
    this.isNotEmpty(filter.patientResponsibilityFrom) ||
    this.isNotEmpty(filter.patientResponsibilityTo) ||
    this.isNotEmpty(filter.dateOfServiceFrom) ||
    this.isNotEmpty(filter.dateOfServiceTo) ||
    filter.showVoided ||
    this.isNotEmpty(filter.statusIds) ||
    this.isNotEmpty(filter.validationIds)) {
      this.isFilterSet = true;
    }
    this.filter = comp;
  }

  getFilter(): ClaimFiltersComponent | null {
    if (this.isFilterSet) return this.filter; 
    return null;
  }

  isNotEmpty(key) {
    if (!(key === undefined || key === null || key === "")) return true;
  }
}
