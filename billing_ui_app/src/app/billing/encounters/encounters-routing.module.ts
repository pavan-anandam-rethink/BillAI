import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { EncounterViewComponent } from './encounter-view/encounter-view.component';
import { RouterModule, Routes } from '@angular/router';
import { AddClaimComponent } from './add-claim/add-claim.component';
import { EncounterListComponent } from './ecnounter-list/encounter-list.component';
import { DirtyFormGuard } from '@core/guards/dirty-form-guard';
import { roleGuard } from '@core/guards/role-guard';
import { AccountPermissions } from '@core/enums/account/account-permissions';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full'
  },
  {
    path: 'list',
    component: EncounterListComponent,
    canActivateChild: [roleGuard], 
    data: { 
      permissions: [AccountPermissions.BillingView]
    },
  },
  {
    path: 'add',
    component: AddClaimComponent,
    canActivateChild: [roleGuard], 
    data: { 
      permissions: [AccountPermissions.BillingEdit, AccountPermissions.BillingView]
    },
  },
  {
    path: 'edit/:tab/:id',
    component: EncounterViewComponent,
    canActivateChild: [roleGuard], 
    data: { 
      permissions: [AccountPermissions.BillingEdit, AccountPermissions.BillingView]
    },
  },
  // {
  //   path: '**',
  //   redirectTo: 'list',
  //   pathMatch: 'full'
  // },
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
export class EncoutersRoutingModule { }