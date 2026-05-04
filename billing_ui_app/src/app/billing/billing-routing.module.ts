import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { Routes, RouterModule } from '@angular/router';
import { roleGuard } from '@core/guards/role-guard';
import { AccountPermissions } from '@core/enums/account/account-permissions';
import { authGuard } from '@core/guards/auth-guard';

const routes: Routes = [
  { 
    path: 'claims',
    loadChildren: () => import('./encounters/encouters.module').then(m => m.EncoutersModule),
    canActivateChild: [roleGuard], 
    data: { 
      permissions: [AccountPermissions.BillingView]
    }
  },
  { 
    path: 'paymentposting', 
    loadChildren: () => import('./payment-posting/payment-posting.module').then(m => m.PaymentPostingModule), 
    canActivate: [roleGuard], 
    data: { 
      permissions: [AccountPermissions.BillingPostPayments]
    }
  },
  { 
    path: 'patientinvoicing', 
    loadChildren: () => import('./patient-invoice/patient-invoice.module').then(m => m.PatientInvoicingModule), 
    canActivateChild: [roleGuard], 
    data: { 
      permissions: [AccountPermissions.BillingReopenEncounter]
    } 
  },
  {
    path: 'settings',
    loadChildren: () => import('./settings/settings.module').then(m => m.SettingsModule),
    canActivateChild: [roleGuard],
    data: {
      permissions: [AccountPermissions.BillingReopenEncounter]
    }
  },
  { 
    path: 'reporting', 
    loadChildren: () => import('./report/report.module').then(m => m.ReportModule), 
    canActivateChild: [roleGuard], 
    data: { 
      permissions: [AccountPermissions.BillingCloseEncounters]
    }
  },
  { 
    path: 'clienthistory', 
    loadChildren: () => import('./client-history/client-history.module').then(m => m.ClientHistoryModule), 
    canActivateChild: [roleGuard], 
    // data: { 
    //   permissions: [AccountPermissions.BillingClientHistory]
    // }
  },
  { 
    path: 'unauthorized', 
    loadChildren: () => import('./unauthorized/unauthorized.module').then(m => m.UnauthorizedModule), 
    canActivate: [authGuard]
  },
  //Need a Home Component
  { 
    path: '',
    redirectTo: 'claims',
    pathMatch: 'full'
  },
  {
    path: '**', 
    loadChildren: () => import('./not-found/not-found.module').then(m => m.NotFoundModule)
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
export class BillingRoutingModule { }
