import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { PatientInvoiceComponent } from './patient-invoice.component';

const routes: Routes = [
  {
    path: '',
    redirectTo: 'list',
    pathMatch: 'full'
  },
  {
    path: 'list',
    component: PatientInvoiceComponent
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
export class PatientInvoiceRoutingModule { }