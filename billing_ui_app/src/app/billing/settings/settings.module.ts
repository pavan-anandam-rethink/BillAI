import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SettingsComponent } from './settings.component';
import { SettingsRoutingModule } from './settings-routing.module';
import { RouterModule } from '@angular/router';
import { KendoModule } from '../../plugins/kendo.module';
import { MaterialModule } from '../../plugins/material.module';
import { ContainedButtonModule } from '../../shared/components/buttons/contained-btn/contained-btn.module';
import { OutlinedButtonModule } from '../../shared/components/buttons/outlined-btn/outlined-btn.module';
import { SharedModule } from '../../shared/components/shared.module';
import { InputsModule, TextAreaModule } from '@progress/kendo-angular-inputs';
import { DropDownsModule } from '@progress/kendo-angular-dropdowns';
import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { FunderSettingsComponent } from './funder-settings/funder-settings.component';
import { FunderSettingsSaveComponent } from './funder-settings/funder-settings-save/funder-settings-save.component';
import { ReportModule } from '../report/report.module';
import { InvoicingStatementsSettingsComponent } from './invoicing-statements-settings/invoicing-statements-settings.component';
import { FunderSettingsEditorComponent } from './funder-settings/funder-settings-editor/funder-settings-editor.component';

@NgModule({
  declarations: [
    SettingsComponent,
    FunderSettingsComponent,
    FunderSettingsSaveComponent,
    InvoicingStatementsSettingsComponent,
    FunderSettingsEditorComponent
  ],
  imports: [
    CommonModule,
    SettingsRoutingModule,
    KendoModule,
    MaterialModule,
    FormsModule,
    ReactiveFormsModule,
    RouterModule,
    ContainedButtonModule,
    OutlinedButtonModule,
    SharedModule,
    InputsModule,
    TextAreaModule,
    DropDownsModule,
    ButtonsModule,
    ReportModule
  ]
})
export class SettingsModule { }
