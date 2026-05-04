import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { DialogModule } from '@progress/kendo-angular-dialog';
import { ContainedButtonModule } from '../buttons/contained-btn/contained-btn.module';
import { OutlinedButtonModule } from '../buttons/outlined-btn/outlined-btn.module';
import { ConfirmationDialogComponent } from './confirmation-dialog.component';
@NgModule({
    imports: [
        CommonModule,
        DialogModule,
        ContainedButtonModule,
        OutlinedButtonModule
    ],
    declarations: [ConfirmationDialogComponent],
    exports: [
        ConfirmationDialogComponent
    ]
})
export class ConfirmDialogModule { }