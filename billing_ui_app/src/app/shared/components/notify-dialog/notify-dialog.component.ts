import { Component, Input, OnInit, Output, OnDestroy, EventEmitter, ViewEncapsulation } from '@angular/core';
import { NotifyDialog } from '@core/models/common';

@Component({
    selector: 'notify-dialog',
    templateUrl: './notify-dialog.component.html',
    styleUrls: ['./notify-dialog.component.css'],
    encapsulation: ViewEncapsulation.None
})
export class NotifyDialogComponent implements OnInit, OnDestroy {
    @Input() public notifyDialog: NotifyDialog;
    @Input() public enableClickOut = true;
    @Output() public onConfirm = new EventEmitter<boolean>();

    constructor() {
        this.notifyDialog = new NotifyDialog(false, "Information", "For your information");
    }

    public ngOnInit(): void {
    }

    public ngOnDestroy(): void {
    }

    onClickedOutside(e: Event) {
        this.enableClickOut && this.close();
    }

    public close() {
        this.notifyDialog.opened = false;
        if (this.onConfirm) this.onConfirm.emit();
    }
}