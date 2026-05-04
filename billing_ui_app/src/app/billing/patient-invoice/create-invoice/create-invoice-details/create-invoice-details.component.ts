import { Component, EventEmitter, Input, NgZone, OnDestroy, OnInit, Output, ViewChild } from "@angular/core";
import { PatientInvoiceDetails, PatientInvoiceHeaderSearch } from "@core/models/billing/patient-invoice";
import { PatientInvoiceService } from "@core/services/billing/patient-invoice.service";
import { GridComponent } from '@progress/kendo-angular-grid';
import { data } from "jquery";
import { Observable, Subject } from "rxjs";
import { take } from "rxjs/operators";


@Component({
    selector: 'create-invoice-details',
    templateUrl: './create-invoice-details.component.html',
    styleUrls: ['./create-invoice-details.component.css',]
})
export class CreateInvoiceDetailsComponent implements OnInit, OnDestroy {

    _clientId: number;
    @Input() set clientId(value: number) {
        this._clientId = value;
        // this.redrawGrid();
    }

    @Input() gridData: PatientInvoiceDetails[] = [];

    _selectedPatientLines: PatientInvoiceDetails[] = [];
    @Input() set selectedPatientLines(value:any) {
        this._selectedPatientLines = value;
    };
    _selectAllLines: boolean;
    @Input() set selectAllLines(value: boolean) {
        this._selectAllLines = value;
        this.allLinesSelectionChanges();
    };

    @Output() selectedLinesChangeEmitter = new EventEmitter<any>();
    @Output() allLinesChangeEmitter = new EventEmitter<any>();

    private unsubscribe = new Subject<void>();
    @ViewChild(GridComponent) grid: GridComponent;

    gridState: PatientInvoiceHeaderSearch = new PatientInvoiceHeaderSearch();

    selectedItems: PatientInvoiceDetails[] = [];
    public isSubjectLoading$: Observable<boolean>;

    public mySelection: number[] = [];

    constructor(private patientInvoiceService: PatientInvoiceService,
        private ngZone: NgZone) {
    }


    allLinesSelectionChanges(): void {
        if (this.gridData.length == 0) {
            return;
        }

        if (this._selectAllLines) {
            this.gridData.forEach(dataItem => {
                dataItem.checked = true;
                let item: any = dataItem;

                let alreadyExist = this.selectedItems.find((x: any) => x.id == dataItem.id);

                if (alreadyExist) {
                    alreadyExist = item;
                } else {
                    this.selectedItems.push(item);
                }
            });
        } else {
            if(this._selectedPatientLines.length == 0)
            {
                this.gridData.forEach(dataItem => {
                    dataItem.checked = false;
                })
                this.selectedItems = []
            }
            else{
                this.gridData.filter(dataItem => {
                    !this._selectedPatientLines.select(x => x.clientId).includes(dataItem.id) 
                }).forEach(dataItem => {
                    dataItem.checked = false;
                })
                this.selectedItems = this._selectedPatientLines;
            }
            
        }

        this.selectedLinesChanged();

    }

    rowSelectionClick(event: any, dataItem: any) {
        if (event.currentTarget.checked) {
            dataItem.checked = true;
            let selectedItem = dataItem;
            selectedItem.clientId = this._clientId;
            if(!this.selectedItems.includes(selectedItem))
            {
                this.selectedItems.push(selectedItem);
            }
        } else {
            dataItem.checked = false;
            let deselectedItem = dataItem;
            this.selectedItems.find(x => x.id == deselectedItem.id).checked = false;
        }

        this.selectedLinesChanged(event);
    }

    checkIsSelected(dataItemId: any, _event: any = undefined): boolean {
        return this.selectedItems.any((x: any) => x.id == dataItemId);
    }

    selectedLinesChanged(event: any = null): void {
        if (event !== null) {
            let selectionState = event.currentTarget.checked;
            let calculatedLength = this.selectedItems.filter(x => x.checked).length;

            if (calculatedLength == this.gridData.length) {
                if (selectionState) {
                    this._selectAllLines = true;
                    this.selectAllLinesChanged(event);
                } else {
                    this._selectAllLines = false;
                    // this.selectAllLinesChanged(event);
                }
            }
            if(this.selectedItems.length == 0) {
                this.allLinesChangeEmitter.emit({
                    event: event,
                    patientId: this._clientId});
            }

            this.selectedLinesChangeEmitter.emit(this.selectedItems);
        }
    }

    selectAllLinesChanged(event: any): void {
        let emitModel = {
            event: event,
            patientId: this._clientId
        };
        this.selectedItems = [];
        if (event?.target?.checked) {
            this.gridData.forEach(element => {
                    element.checked = true;
                let selectedItem: any = element
                selectedItem.clientId = this._clientId;
                this.selectedItems.push(selectedItem);
                // this.selectedLinesChanged(event);
            });
        }
        else {
            this.gridData.forEach(element => {
                    element.checked = false;
                    // this.selectedLinesChanged(event);
                });
        }
        this.allLinesChangeEmitter.emit(emitModel);
    }

    fitColumns(): void {
        this.ngZone.onStable.asObservable().pipe(take(1)).subscribe(() => {
            this.grid.autoFitColumns(this.grid.columnList.toArray());
            this.grid.autoFitColumns();
        });
    }

    ngOnDestroy() {
        this.unsubscribe.next();
        this.unsubscribe.complete();
    }

    ngAfterViewInit() {
        this.fitColumns();
    }

    ngOnInit(): void {
        this.allLinesSelectionChanges();
    }
}
