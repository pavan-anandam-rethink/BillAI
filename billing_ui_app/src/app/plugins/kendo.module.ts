import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { ButtonsModule } from '@progress/kendo-angular-buttons';
import { CalendarModule } from '@progress/kendo-angular-dateinputs';
import { DateInputsModule } from '@progress/kendo-angular-dateinputs';
import { DialogModule, WindowModule } from '@progress/kendo-angular-dialog';
import { DropDownsModule, MultiSelectModule } from '@progress/kendo-angular-dropdowns';
import { PDFModule, ExcelModule, GridModule } from '@progress/kendo-angular-grid';
import { InputsModule, RadioButtonModule } from '@progress/kendo-angular-inputs';
import { IntlModule } from '@progress/kendo-angular-intl';
import { TooltipModule } from '@progress/kendo-angular-tooltip';
import { UploadModule } from '@progress/kendo-angular-upload';
import { ListViewModule } from '@progress/kendo-angular-listview';
import { PopupModule } from '@progress/kendo-angular-popup';
import { NotificationModule } from '@progress/kendo-angular-notification';
import { LabelModule } from '@progress/kendo-angular-label';
import { SwitchModule } from '@progress/kendo-angular-inputs';

@NgModule({
    imports: [CommonModule, TooltipModule, LabelModule, SwitchModule],
    exports: [
        ButtonsModule,
        CalendarModule,
        DateInputsModule,
        DialogModule,
        DropDownsModule,
        ExcelModule,
        InputsModule,
        IntlModule,
        MultiSelectModule,
        PDFModule,
        TooltipModule,
        UploadModule,
        WindowModule,
        ListViewModule,
        PopupModule,
        NotificationModule,
        GridModule,
        RadioButtonModule
    ]
})
export class KendoModule { }