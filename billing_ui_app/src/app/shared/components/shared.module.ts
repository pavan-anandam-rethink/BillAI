import { RouterModule } from '@angular/router';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { DatePickerModule, TimePickerModule } from '@progress/kendo-angular-dateinputs'
import { AutoCompleteModule, ComboBoxModule } from '@progress/kendo-angular-dropdowns';
import { KendoModule } from '../../plugins/kendo.module';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { InputsModule } from '@progress/kendo-angular-inputs';
import { SidebarModule } from './sidebar/sidebar.module';
import { ContainedButtonModule } from './buttons/contained-btn/contained-btn.module';
import { OutlinedButtonModule } from './buttons/outlined-btn/outlined-btn.module';
import { SHARED_COMPONENTS } from '.';
import { ConfirmDialogModule } from './confirmation-dialog/confirm-dialog.module';
import { NgModule } from '@angular/core';
import { MaterialModule } from '@app/plugins/material.module';
import { DIRECTIVES } from '../directives';
import { PdfViewerModule } from 'ng2-pdf-viewer';

@NgModule({
    imports: [
        CommonModule,
        FormsModule,
        DialogModule,
        FormsModule,
        ReactiveFormsModule,
        InputsModule,
        SidebarModule,
        RouterModule,
        DatePickerModule,
        TimePickerModule,
        AutoCompleteModule,
        ComboBoxModule,
        ContainedButtonModule,
        OutlinedButtonModule,
        MaterialModule,
        KendoModule,
        PdfViewerModule,
    ],
    declarations: [SHARED_COMPONENTS, DIRECTIVES],
    exports: [
        ConfirmDialogModule,
        SHARED_COMPONENTS,
        DIRECTIVES
    ],
    providers:[
        DatePipe
    ]
})
export class SharedModule { }