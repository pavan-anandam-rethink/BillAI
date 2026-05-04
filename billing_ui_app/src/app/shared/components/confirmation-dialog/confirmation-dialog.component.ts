import { Component, Input, Output, EventEmitter, ViewEncapsulation } from '@angular/core';
import { ConfirmDialog } from '@core/models/common';

@Component({
    selector: 'confirmation-dialog',
    templateUrl: './confirm-dialog.component.html',
    styleUrls: ['./confirmation-dialog.css'],
    encapsulation: ViewEncapsulation.None
})
export class ConfirmationDialogComponent {
    @Input() width = 450;
    @Input() public confirmDialog: ConfirmDialog;
    @Output() public onConfirm = new EventEmitter<boolean>();
    @Output() public onCancel = new EventEmitter<boolean>();

    public innerModel: any = null;

    set opened(opened: boolean) {
        this.confirmDialog.opened = opened;
    }

    CloseDialog(){
        this.confirmDialog.opened = closed;
    }

    onClickedOutside(e: Event) {
        this.close(false);
    }

    close(status: boolean): void {
        this.confirmDialog.opened = false;
        if (status) {
            this.onConfirm.emit(status);
        } else {
            this.onCancel.emit();
        }

        this.confirmDialog.subject.next(status);
    }
}