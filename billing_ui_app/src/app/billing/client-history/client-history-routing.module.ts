import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ClientHistoryComponent } from './client-history.component';
import { ClientHistoryChargeComponent } from './client-history-charge/client-history-charge.component';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '@progress/kendo-angular-grid';
import { ButtonModule } from '@progress/kendo-angular-buttons';
import { MatTabsModule } from '@angular/material/tabs';
// import { AccountPermissions } from '@core/enums/account/account-permissions';
// import { roleGuard } from '@core/guards/role-guard';


const routes: Routes = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full'
  },
  {
    path: 'list',
    component: ClientHistoryComponent,
  },
  {
    path: 'charge/:clientName',
    component: ClientHistoryChargeComponent
  },
  {
    path: '**', 
    loadChildren: () => import('../not-found/not-found.module').then(m => m.NotFoundModule)
  }
];

@NgModule({
  declarations: [
    
  ],
  imports: [
    CommonModule,
    SharedModule,
    ButtonModule,
    MatTabsModule,
    RouterModule.forChild(routes)
  ]
})
export class ClientHistoryRoutingModule { }
