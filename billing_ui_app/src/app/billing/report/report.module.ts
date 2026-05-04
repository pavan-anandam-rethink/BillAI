import { NgModule } from '@angular/core';
import { ReportComponent } from './report.component';
import { MaterialModule } from "@app/plugins/material.module";
import { KendoModule } from "@app/plugins/kendo.module";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { RouterModule } from "@angular/router";
import { ContainedButtonModule } from "@app/shared/components/buttons/contained-btn/contained-btn.module";
import { OutlinedButtonModule } from "@app/shared/components/buttons/outlined-btn/outlined-btn.module";
import { SharedModule } from "@app/shared/components/shared.module";
import { ReportRoutingModule } from './report-routing.module';
import { FunderPopupComponent } from './shared/funder-popup/funder-popup.component';
import { PatientInvoicingModule } from '../patient-invoice/patient-invoice.module';
import { AccountsReceivablesCalendarPopupComponent } from './shared/accounts-receivables-calendar-popup/accounts-receivables-calendar-popup.component';
import { ExcelExportModule } from '@progress/kendo-angular-excel-export';
import { UnbilledAppointmentsComponent } from './appointment-reports/unbilled-appointments/unbilled-appointments.component';
import { AccountsReceivablesComponent } from './claim-reports/accounts-receivables/accounts-receivables.component';
import { AccountsReceivablesFiltersComponent } from './claim-reports/accounts-receivables/accounts-receivables-filters/accounts-receivables-filters.component';
import { PaymentsAdjustmentsComponent } from './claim-reports/payments-adjustments/payments-adjustments.component';
import { PaymentsAdjustmentsFiltersComponent } from './claim-reports/payments-adjustments/payments-adjustments-filters/payments-adjustments-filters.component';
import { UnbilledAppointmentsFiltersComponent } from './appointment-reports/unbilled-appointments/unbilled-appointments-filters/unbilled-appointments-filters.component';
import { ClientFilterPopupComponent } from './appointment-reports/unbilled-appointments/unbilled-appointments-filters/client-filter-popup/client-filter-popup.component';
import { FunderFilterPopupComponent } from './appointment-reports/unbilled-appointments/unbilled-appointments-filters/funder-filter-popup/funder-filter-popup.component';
import { StaffFilterPopupComponent } from './appointment-reports/unbilled-appointments/unbilled-appointments-filters/staff-filter-popup/staff-filter-popup.component';
import { LocationFilterPopupComponent } from './appointment-reports/unbilled-appointments/unbilled-appointments-filters/location-filter-popup/location-filter-popup.component';
import { PlaceofserviceFilterPopupComponent } from './appointment-reports/unbilled-appointments/unbilled-appointments-filters/placeofservice-filter-popup/placeofservice-filter-popup.component';
import { UnbilledAppointmentsWorkerService } from '../services/worker-services/unbilled-appointments-worker.service';
import { UnbilledAppointmentsService } from '../services/worker-services/unbilled-appointment.service';
import { AccountsReceivablesChargeLevelComponent } from './charge-reports/accounts-receivables-charge-level/accounts-receivables-charge-level.component';
import { ClaimFollowUpComponent } from './claim-reports/claim-followup/claim-follow-up/claim-follow-up.component';
import { ClaimFollowUpFiltersComponent } from './claim-reports/claim-followup/claim-follow-up-filters/claim-follow-up-filters.component';
import { UnprocessedAppointmentsComponent } from './appointment-reports/unprocessed-appointments/unprocessed-appointments.component';
import { UnprocessedAppointmentsFiltersComponent } from './appointment-reports/unprocessed-appointments/unprocessed-appointments-filters/unprocessed-appointments-filters.component';
import { FinancialSummaryReportComponent } from './financial-summary/components/financial-summary-report/financial-summary-report.component';
import { FinancialSummaryFiltersComponent } from './financial-summary/financial-summary-filters/financial-summary-filters/financial-summary-filters.component';
import { FinancialSummaryFiltersRenderingProviderPopupComponent } from './financial-summary/rendering-provider-filters/rendering-provider-filters/financial-summary-filters-rendering-provider-popup.component';


@NgModule({
  declarations: [
    AccountsReceivablesComponent,
    PaymentsAdjustmentsComponent,
    ReportComponent,
    AccountsReceivablesComponent,
    FunderPopupComponent,
    AccountsReceivablesFiltersComponent,
    PaymentsAdjustmentsFiltersComponent,
    AccountsReceivablesCalendarPopupComponent,
    UnbilledAppointmentsComponent,
    UnbilledAppointmentsFiltersComponent,
    ClientFilterPopupComponent,
    FunderFilterPopupComponent,
    StaffFilterPopupComponent,
    LocationFilterPopupComponent,
    PlaceofserviceFilterPopupComponent,
    AccountsReceivablesChargeLevelComponent,
    ClaimFollowUpComponent,
    ClaimFollowUpFiltersComponent,
    UnprocessedAppointmentsComponent,
    UnprocessedAppointmentsFiltersComponent,
    FinancialSummaryReportComponent,
    FinancialSummaryFiltersComponent,
    FinancialSummaryFiltersRenderingProviderPopupComponent
  ],
  providers: [
    UnbilledAppointmentsWorkerService,
    UnbilledAppointmentsService
  ],
  imports: [
    ReportRoutingModule,
    KendoModule,
    MaterialModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    ContainedButtonModule,
    OutlinedButtonModule,
    SharedModule,
    PatientInvoicingModule,
    ExcelExportModule
  ]
})

export class ReportModule { }
