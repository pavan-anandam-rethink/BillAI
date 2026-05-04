import { Injectable } from '@angular/core';
import { ClientHistoryChargeFilterComponent } from '../client-history/client-history-list/client-history-list-filter/client-history-charge-filter/client-history-charge-filter.component';
import { ClientHistoryListFilterComponent } from '../client-history/client-history-list/client-history-list-filter/client-history-list-filter.component';

@Injectable({
  providedIn: 'root'
})
export class ClientHistoryFilterService {

  filter: ClientHistoryChargeFilterComponent;
  filter2: ClientHistoryListFilterComponent;
  isFilterSet = false;
 constructor() { }
 setFilter(comp: ClientHistoryChargeFilterComponent) {
   
       if (comp.selectedFunders.length > 0 ||
         comp.selectedRenderingProviders.length > 0 || 
         comp.selectedAuthNumber.length > 0 || 
         comp.selectedPlaceOfService.length > 0 ||
         comp.selectedDos ||
         this.isNotEmpty(comp.authNumbers)) {
         this.isFilterSet = true;
       }
       this.filter = comp;
      }
      setFilter2(comp: ClientHistoryListFilterComponent) {
   
       if (comp.selectedLocation.length > 0 ||
         comp.selectedPatients.length > 0 || 
         comp.selectedDOB ) {
         this.isFilterSet = true;
       }
       this.filter2 = comp;
      }
   
     getFilter(): ClientHistoryChargeFilterComponent | null {
       if (this.isFilterSet) return this.filter; 
       return null;
     }
   
     isNotEmpty(key) {
       if (!(key === undefined || key === null || key === "")) return true;
     }
}
