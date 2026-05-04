import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { BulkPostingComponent, PaymentPostingListComponent, PaymentPostingViewComponent } from '.';
import { AccountPermissions } from '@core/enums/account/account-permissions';
import { roleGuard } from '@core/guards/role-guard';


const routes: Routes = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full'
  },
  {
    path: 'list',
    component: PaymentPostingListComponent,
    canActivateChild: [roleGuard],
    data: { permissions: [AccountPermissions.BillingPostPayments] }
  },
  {
    path: 'edit/:id',
    component: PaymentPostingViewComponent,
    canActivateChild: [roleGuard],
    data: { permissions: [AccountPermissions.BillingPostPayments] }
  },
  { path: 'edit/:id/:patientId',
    component: PaymentPostingViewComponent,
    canActivateChild: [roleGuard],
    data: { permissions: [AccountPermissions.BillingPostPayments] }
  },
  {
    path: 'bulkposting',
    component: BulkPostingComponent,
    canActivateChild: [roleGuard],
    data: { permissions: [AccountPermissions.BillingPostPayments] }
  },
  {
    path: '**',
    loadChildren: () => import('../not-found/not-found.module').then(m => m.NotFoundModule)
  }

];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
  ],
  exports: [
  ],
})
export class PaymentPostingRoutingModule { }
