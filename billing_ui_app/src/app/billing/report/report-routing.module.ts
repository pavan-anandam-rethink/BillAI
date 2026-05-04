import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { ReportComponent } from './report.component';
import { UnbilledAppointmentsComponent } from './appointment-reports/unbilled-appointments/unbilled-appointments.component';
import { AccountsReceivablesComponent } from './claim-reports/accounts-receivables/accounts-receivables.component';
import { PaymentsAdjustmentsComponent } from './claim-reports/payments-adjustments/payments-adjustments.component';
import { AccountsReceivablesChargeLevelComponent } from './charge-reports/accounts-receivables-charge-level/accounts-receivables-charge-level.component';
import { ClaimFollowUpComponent } from './claim-reports/claim-followup/claim-follow-up/claim-follow-up.component';
import { UnprocessedAppointmentsComponent } from './appointment-reports/unprocessed-appointments/unprocessed-appointments.component';
import { FinancialSummaryReportComponent } from './financial-summary/components/financial-summary-report/financial-summary-report.component';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full'
  },
  {
    path: 'list',
    component: ReportComponent
  },
  {
    path: 'account-receivables',
    component: AccountsReceivablesComponent
  },
  {
    path: 'payment-adjustments',
    component: PaymentsAdjustmentsComponent
  },
  {
    path: 'app-claim-follow-up',
    component: ClaimFollowUpComponent
  },
  {
    path: 'unbilled-appointments',
    component: UnbilledAppointmentsComponent
  },
  {
    path: 'unprocessed-appointments',
    component: UnprocessedAppointmentsComponent
  },
  {
    path: 'accounts-receivables-charge-level',
    component: AccountsReceivablesChargeLevelComponent
  },
  {
    path: 'financial-summary-report',
    component: FinancialSummaryReportComponent
  },
  {
    path: '**', 
    loadChildren: () => import('../not-found/not-found.module').then(m => m.NotFoundModule)
  }
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes)
  ],
  exports: [
  ],
})
export class ReportRoutingModule { }
