import { NgModule } from '@angular/core';
import { UnauthorizedComponent } from './unauthorized.component';
import { MaterialModule } from "@app/plugins/material.module";
import { KendoModule } from "@app/plugins/kendo.module";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { RouterModule } from "@angular/router";
import { SharedModule } from "@app/shared/components/shared.module";
import { UnauthorizedRoutingModule } from './unauthorized-routing.module';


@NgModule({
  declarations: [
    UnauthorizedComponent,
  ],
  imports: [
    UnauthorizedRoutingModule,
    KendoModule,
    MaterialModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    SharedModule,
  ]
})
export class UnauthorizedModule { }
