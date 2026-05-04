import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ClientHistoryComponent } from './client-history.component';
import { ClientHistoryRoutingModule } from './client-history-routing.module';
import { SharedModule } from '@app/shared/components/shared.module'; // Make sure this contains outlined-btn
import { ButtonModule } from '@progress/kendo-angular-buttons';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { KendoModule } from '@app/plugins/kendo.module';
import { NgbModal, NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { ContainedButtonModule } from '@app/shared/components/buttons/contained-btn/contained-btn.module';
import { OutlinedButtonModule } from '@app/shared/components/buttons/outlined-btn/outlined-btn.module';
import { RouterModule } from '@angular/router';
import { PopupModule } from '@progress/kendo-angular-popup';
import { HeaderModule } from '@progress/kendo-angular-grid';
import { MaterialModule } from '@app/plugins/material.module';
import { ClientHistoryListFilterComponent } from './client-history-list/client-history-list-filter/client-history-list-filter.component';

import { ClientHistoryChargeComponent } from './client-history-charge/client-history-charge.component';
import { ClientHistoryChargeFilterComponent } from './client-history-list/client-history-list-filter/client-history-charge-filter/client-history-charge-filter.component';
import { LocationFilterPopupComponent } from './client-history-list/client-history-list-filter/location-filter-popup/location-filter-popup.component';
import { ClientFilterPopupComponent } from './client-history-list/client-history-list-filter/client-filter-popup/client-filter-popup.component';
import { FunderFilterPopupComponent } from './client-history-list/client-history-list-filter/funder-filter-popup/funder-filter-popup.component';
import { CalendarPopupComponent } from './client-history-list/client-history-list-filter/claim-history-list-filter-calendar-popup/calendar-popup.component';
import { PlaceofserviceFilterPopupComponent } from './client-history-list/client-history-list-filter/placeofservice-filter-popup/placeofservice-filter-popup.component';
import { EncoutersModule } from '../encounters/encouters.module';
import { ClaimFiltersRenderingProviderPopupComponent } from './client-history-list/client-history-list-filter/claim-filters-rendering-provider-popup/claim-filters-rendering-provider-popup.component';
import { AuthorizationFilterPopupComponent } from './client-history-list/client-history-list-filter/authorization-filter-popup/authorization-filter-popup.component';
import { ClientHistoryInvoiceComponent } from './client-history-invoice/client-history-invoice.component';
import { ClientHistoryInvoiceFilterComponent } from './client-history-invoice-filter/client-history-invoice-filter.component';
import { FilterByRangePopupClientHistoryComponent } from './filter-by-range-popup-client-history/filter-by-range-popup-client-history.component';
import { PatientInvoicePopupComponent } from '../patient-invoice/shared/patient-invoice-popup/patient-invoice-popup.component';



@NgModule({
  declarations: [ClientHistoryComponent, 
    ClientHistoryListFilterComponent, 
    ClientHistoryChargeComponent, 
    ClientHistoryChargeFilterComponent,
    LocationFilterPopupComponent,
    ClientFilterPopupComponent,
    FunderFilterPopupComponent,
    CalendarPopupComponent,
    PlaceofserviceFilterPopupComponent,
    ClaimFiltersRenderingProviderPopupComponent,
    AuthorizationFilterPopupComponent,
    ClientHistoryInvoiceComponent,
    ClientHistoryInvoiceFilterComponent,
    FilterByRangePopupClientHistoryComponent,
    PatientInvoicePopupComponent

  ],
  imports: [
    CommonModule,
    ClientHistoryRoutingModule,
    SharedModule,
    ButtonModule ,
    FormsModule,
    MatTabsModule,
    InputsModule,
    ReactiveFormsModule,
    KendoModule,
    NgbModule,
    ContainedButtonModule,
    OutlinedButtonModule,
    RouterModule,
    SharedModule,
    PopupModule,
    HeaderModule,
    MaterialModule,
    EncoutersModule
  ]
})
export class ClientHistoryModule {}
