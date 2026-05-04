import { Component, Input, OnInit, OnDestroy, ElementRef, ViewChild, HostListener, Output, EventEmitter } from "@angular/core";
import { MemberViewSettings } from "@core/models/billing/member-view-settings";
import { Subject } from "rxjs";

interface ColumnSettings {
    name: string;
    identifier: string;
    checked: boolean;
}

@Component({
    selector: 'selector-popup',
    templateUrl: './selector-popup.component.html',
    styleUrls: ['./selector-popup.component.css'],
})

export class SelectorPopupComponent implements OnInit, OnDestroy {
    @Input() anchor: ElementRef<HTMLDivElement>;
    @Input() viewColumns: MemberViewSettings;
    @Output() onSelectorLeave = new EventEmitter<boolean>();
    @Output() selectedColumnsEmitter = new EventEmitter();
    @Output() onColumnSelect = new EventEmitter();
    @ViewChild('popup', { read: ElementRef }) public popup: ElementRef<HTMLElement>;

    @HostListener('document:click', ['$event'])
    public documentClick(event: PointerEvent): void {
        if (!this.contains(event.target as Node)) {
            this.onSelectorLeave.emit(false);
        }
    }
    
    private unsubscribe = new Subject();
    selectedColumns: string[] = [];

    columns: ColumnSettings[] = [
        { identifier: 'client', name: 'Client', checked: true },
        { identifier: 'funder', name: 'Funder', checked: true },
        { identifier: 'renderingProvider', name: 'Rendering Provider', checked: true },
        { identifier: 'placeOfService', name: 'POS', checked: true },
        { identifier: 'dateOfService', name: 'Date of Service', checked: true },
        { identifier: 'authorization', name: 'Authorization', checked: true },
        { identifier: 'expected', name: 'Expected', checked: true },
        { identifier: 'billed', name: 'Billed', checked: true },
        { identifier: 'adjustment', name: 'Adjustment', checked: true },
        { identifier: 'payment', name: 'Payment', checked: true },
        { identifier: 'patientResponsible', name: 'Patient Responsibility', checked: true },
        { identifier: 'balance', name: 'Balance', checked: true },
        { identifier: 'billedDate', name: 'Billed Date', checked: true },
        { identifier: 'status', name: 'Status', checked: true },
        { identifier: 'assigneeName', name: 'Assignee', checked: true },
        { identifier: 'validation', name: 'Validation', checked: true },
        { identifier: 'actions', name: 'Actions', checked: true },
    ]

    constructor() {
    }

    private contains(target: Node): boolean {
        return this.anchor.nativeElement.contains(target) ||
            (this.popup ? this.popup.nativeElement.contains(target) : false);
    }

    selectorItemClicked(selectorItem: ColumnSettings): void {
        selectorItem.checked = !selectorItem.checked;
        if (selectorItem.checked) {
            this.selectedColumns.push(selectorItem.identifier);
        } else {
            let itemIndex = this.selectedColumns.indexOf(selectorItem.identifier);
            this.selectedColumns.splice(itemIndex, 1);
        }
        this.onColumnSelect.emit(this.selectedColumns);
    }

    emitSave(): void {
        if (this.selectedColumns.length !== 0) {
            this.selectedColumnsEmitter.emit(this.selectedColumns);
        }
        this.onSelectorLeave.emit(false);
    }

    loadSelectedItems(selectedItems: MemberViewSettings) {
        Object.keys(selectedItems).forEach(key => {            
            const hasSelectedColumn = this.columns.some((x) => x.identifier.indexOf(key) !== -1 && key != "id");
            const isVisible = selectedItems[key];

            if (hasSelectedColumn && isVisible) {
                this.selectedColumns.push(key.toString());
            }
        })
        this.columns = [
            { identifier: 'client', name: 'Client', checked: this.setVisibility("client") },
            { identifier: 'funder', name: 'Funder', checked: this.setVisibility("funder") },
            { identifier: 'renderingProvider', name: 'Rendering Provider', checked: this.setVisibility("renderingProvider") },
            { identifier: 'placeOfService', name: 'POS', checked: this.setVisibility("placeOfService") },
            { identifier: 'dateOfService', name: 'Date of Service', checked: this.setVisibility("dateOfService") },
            { identifier: 'authorization', name: 'Authorization', checked: this.setVisibility("authorization") },
            { identifier: 'expected', name: 'Expected', checked: this.setVisibility("expected") },
            { identifier: 'billed', name: 'Billed', checked: this.setVisibility("billed") },
            { identifier: 'payment', name: 'Payment', checked: this.setVisibility("payment") },
            { identifier: 'adjustment', name: 'Adjustment', checked: this.setVisibility("adjustment") },
            { identifier: 'patientResponsible', name: 'Patient Responsibility', checked: this.setVisibility("patientResponsible") },
            { identifier: 'balance', name: 'Balance', checked: this.setVisibility("balance") },
            { identifier: 'billedDate', name: 'Billed Date', checked: this.setVisibility("billedDate") },
            { identifier: 'status', name: 'Status', checked: this.setVisibility("status") },
            { identifier: 'assigneeName', name: 'Assignee', checked: this.setVisibility("assigneeName") },
            { identifier: 'validation', name: 'Validation', checked: this.setVisibility("validation") },
            { identifier: 'actions', name: 'Actions', checked: this.setVisibility("actions") },
        ]
    }

    ngOnDestroy() {
        //this.unsubscribe.next();
        this.unsubscribe.complete();
    }
    
    ngOnInit() {
        this.loadSelectedItems(this.viewColumns);
    }

    setVisibility(column: string) {
        return this.selectedColumns.indexOf(column) > -1;
    }

}