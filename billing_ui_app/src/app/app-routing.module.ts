import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { Routes, RouterModule } from '@angular/router';
import { ShellComponent } from './shell/shell.component';
import { authGuard } from '@core/guards/auth-guard';

const routes: Routes = [
  {
    path: '',
    component: ShellComponent,
    children: [
      { path: 'billing', loadChildren: () => import('./billing/billing.module').then(m => m.BillingModule), canActivate: [authGuard]},
      //Need a Home Component
      { 
        path: '',
        redirectTo: 'billing',
        pathMatch: 'full'
      },
      {path: '**', loadChildren: () => import('./billing/not-found/not-found.module').then(m => m.NotFoundModule)},
    ]
  },
];

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forRoot(routes)
  ],
  exports: [
  ],
})
export class AppRoutingModule { }