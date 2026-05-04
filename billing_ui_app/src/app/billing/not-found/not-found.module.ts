import { NgModule } from '@angular/core';
import { NotFoundComponent } from './not-found.component';
import { MaterialModule } from "@app/plugins/material.module";
import { KendoModule } from "@app/plugins/kendo.module";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { RouterModule } from "@angular/router";
import { SharedModule } from "@app/shared/components/shared.module";
import { NotFoundRoutingModule } from './not-found-routing.module';


@NgModule({
  declarations: [
    NotFoundComponent,
  ],
  imports: [
    NotFoundRoutingModule,
    KendoModule,
    MaterialModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    SharedModule,
  ]
})
export class NotFoundModule { }
