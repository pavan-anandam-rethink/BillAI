import { Component, ChangeDetectionStrategy, Input, Output, EventEmitter } from '@angular/core';

import { DialogService } from '@progress/kendo-angular-dialog';

import { Encounter } from '@core/models/billing';
import { EncounterStatus } from '@core/enums/billing';


@Component({
    selector: 'encounter-list-action',
    templateUrl: './encounter-list-action.component.html',
    styleUrls: ['./encounter-list-action.component.css'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class EncounterListActionComponent {
    readonly encounterStatus = EncounterStatus;

    @Input() canEdit = false;
    @Input() canApprove = false;
    @Input() canClose = false;
    @Input() dataItem: Encounter;

    @Output() actionPerformed = new EventEmitter();
    @Output() noteEditorOpened = new EventEmitter();
    @Output() paymentAdded = new EventEmitter();

    constructor(private dialogService: DialogService) { }

    openNoteEditor(): void {
        this.noteEditorOpened.emit();
    }

    saveEncounterStatus(status: EncounterStatus): void {
        if (this.dataItem.id > 0) {
            this.actionPerformed.emit({ ids: [this.dataItem.id], status: status });
        }
    }

    showPaymentEditor(): void {
        const dialogRef = this.dialogService.open({
            //content: ApplyPaymentComponent
        });

        dialogRef.content.instance.encounterId = this.dataItem.id;
        dialogRef.result.subscribe(result => {
            const payment = result as any;
            if (payment && payment.amount) {
                this.paymentAdded.emit(payment.amount);
            }
        })
    }
}