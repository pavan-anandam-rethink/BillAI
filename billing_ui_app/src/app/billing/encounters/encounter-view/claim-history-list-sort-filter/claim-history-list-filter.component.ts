import {Component, ElementRef, Input, OnDestroy, ViewChild, ViewEncapsulation, Output, EventEmitter} from '@angular/core';
import {FormGroup, FormControl} from '@angular/forms';
import {DatePipe} from "@angular/common";
import { CompositeFilterDescriptor } from '@progress/kendo-data-query';
import { ClaimHistoryListSearch } from '@core/models/billing/claim-history-list-search';
import { HistoryFilterModels } from '../encounter-transaction/encounter-transaction.component';
import { Subject } from 'rxjs/internal/Subject';

@Component({
    selector: 'claim-history-list-filter',
    templateUrl: './claim-history-list-filter.component.html',
    styleUrls: ['./claim-history-list-filter.component.css'],
    encapsulation: ViewEncapsulation.None,
    providers: [DatePipe]
})

export class ClaimHistoryListFilterComponent implements OnDestroy {    
    @Input() filterForm: FormGroup;

    @Input() set showFilter(value: boolean) {
        const dropdown = document.getElementById('claimHistoryGridFilterColumns');
        if (dropdown) {
            dropdown.style.height = value ? '64px' : '0px';
            dropdown.classList.toggle('active');
        }
    }

    @Input() historyFilterModels: HistoryFilterModels;
    @Output() filterChanged= new EventEmitter();

    @ViewChild('dateAnchorEl', { read: ElementRef }) public dateAnchor: ElementRef;
    @ViewChild('userAnchorEl', { read: ElementRef }) public userAnchor: ElementRef;
    @ViewChild('actionAnchorEl', { read: ElementRef }) public actionAnchor: ElementRef;
    @ViewChild('modeAnchorEl', { read: ElementRef }) public modeAnchor: ElementRef;

    private unsubscribe = new Subject<void>();

    selectedItems: ClaimHistoryListSearch[] = [];
    userSelectedItems: ClaimHistoryListSearch[] = [];
    actionSelectedItems: ClaimHistoryListSearch[] = [];
    modeSelectedItems: ClaimHistoryListSearch[] = [];

    showDatePopup = false;
    showUserPopup = false;
    showActionPopup = false;
    showModePopup = false;

    isDateActive = false;
    isUserActive = false;
    isActionActive = false;       
    isModeActive = false;       
    
    sortDateElAnchor: any;   
    sortUserElAnchor: any;
    sortActionElAnchor: any;
    sortModeElAnchor: any;
    dateElAnchor: any;
    userElAnchor: any;
    actionElAnchor: any;
    modeElAnchor: any;   
    
    isFiltersApplied: boolean = false;
    isFilterButtonDisabled: boolean = true;

    constructor(private datePipe: DatePipe) {
    } 

    dateToggle(event: Event): void {
        this.dateElAnchor = document.getElementById("changeDate");
        this.showDatePopup = !this.showDatePopup;
        this.isDateActive = this.showDatePopup;
    }

    userToggle(event: Event): void {
        this.userElAnchor = event.currentTarget;
        this.showUserPopup = !this.showUserPopup;
        this.isUserActive = this.showUserPopup;
        if (this.userSelectedItems.length > 0) {
            this.selectedItems = this.userSelectedItems;
        }
    }

    actionToggle(event: Event): void {
        this.actionElAnchor = event.currentTarget;
        this.showActionPopup = !this.showActionPopup;
        this.isActionActive = this.showActionPopup;
        if (this.actionSelectedItems.length > 0) {
            this.selectedItems = this.actionSelectedItems;
        }
    }

    modeToggle(event: Event): void {
        this.modeElAnchor = event.currentTarget;
        this.showModePopup = !this.showModePopup;
        this.isModeActive = this.showModePopup;
        if (this.modeSelectedItems.length > 0) {
            this.selectedItems = this.modeSelectedItems;
        }
    }

    onFilterLeave(state: boolean, type: string) {
        if (type === 'date') {
            this.showDatePopup = state;
            this.isDateActive = state;
        } else if (type === 'user') {
            this.isUserActive = state;
        } else if (type === 'action') {
            this.isActionActive = state;
        } else if (type === 'mode') {
            this.isModeActive = state;
        }
    }

    resetForm(): void {
        let transactionDateInput = document.getElementById("changeDate");
        let transactionDateControlValue: FormControl = this.filterForm.get("changeDate.value") as FormControl;

        transactionDateInput && transactionDateInput.setAttribute('value', '');
        transactionDateControlValue.setValue('');

        this.userSelectedItems = [];
        this.actionSelectedItems = [];
        this.modeSelectedItems = [];

        this.setSearchItems([], 'clear');
        this.filterChanged.emit();
        this.isFilterButtonDisabled = true;
        this.isFiltersApplied = false;
    }

    setDatePeriod(dateRange: any): void {
        let transactionDateInput = document.getElementById("changeDate");
        let transactionDateControlValue: FormControl = this.filterForm.get("changeDate.value") as FormControl;
        const filters = [];

        dateRange.end = this.datePipe.transform(dateRange.end, 'MM/dd/yy');
        dateRange.end = dateRange.end.toString() + ' 23:59:59';

        if (dateRange.start) {
            filters.push({
                field: 'changeDate',
                operator: "gte",
                value: dateRange.start
            });
        }

        if (dateRange.end) {
            filters.push({
                field: 'changeDate',
                operator: "lte",
                value: dateRange.end
            });
        }        

        let root: CompositeFilterDescriptor = {
            logic: "and",
            filters: []
        };

        if (filters.length) {
            root.filters.push(...filters);
        }

        transactionDateInput && transactionDateInput.setAttribute('value', `${this.datePipe.transform(dateRange.start, 'MM/dd/yy')} - ${this.datePipe.transform(dateRange.end, 'MM/dd/yy')}`);
        transactionDateControlValue.setValue(root.filters);
        this.emitFilterChangeEvent();
    }

    searchItemClicked(searchItemObject: any, field: string) {
        searchItemObject.searchItem.checked = !searchItemObject.searchItem.checked;

        if (field === 'user') {
            if (searchItemObject.searchItem.checked) {
                this.userSelectedItems.push(searchItemObject.searchItem);
            } else {
                let itemIndex = this.userSelectedItems.indexOf(searchItemObject.searchItem);
                this.userSelectedItems.splice(itemIndex, 1);
            }

            this.setSearchItems(this.userSelectedItems, searchItemObject.field);
        } else if (field === 'action') {
            if (searchItemObject.searchItem.checked) {
                this.actionSelectedItems.push(searchItemObject.searchItem);
            } else {
                let itemIndex = this.actionSelectedItems.indexOf(searchItemObject.searchItem);
                this.actionSelectedItems.splice(itemIndex, 1);
            }

            this.setSearchItems(this.actionSelectedItems, searchItemObject.field);
        } else if (field === 'mode') {
            if (searchItemObject.searchItem.checked) {
                this.modeSelectedItems.push(searchItemObject.searchItem);
            } else {
                let itemIndex = this.modeSelectedItems.indexOf(searchItemObject.searchItem);
                this.modeSelectedItems.splice(itemIndex, 1);
            }

            this.setSearchItems(this.modeSelectedItems, searchItemObject.field);
        }
    }

    setSearchItems(selectedItems: ClaimHistoryListSearch[], field: string): void {
        let controlValue: FormControl;
        let fieldValue: FormControl;
        let controlInputData: HTMLElement;
        let namesArr: string[] = [];
        if (field === 'user') {            
            controlValue = this.filterForm.get("changeBy.value") as FormControl;
            fieldValue = this.filterForm.get("changeBy.field") as FormControl;
            controlInputData = <HTMLElement>document.getElementById("changeBy");
            selectedItems.forEach(item => {
                namesArr.push(item.name);
            });

            let filterObj: CompositeFilterDescriptor = {
                logic: 'or',
                filters: []
            }

            namesArr.forEach(name => {
                filterObj.filters.push({
                    field: 'changeBy',
                    operator: "eq",
                    value: name
                });
            })

            controlValue.setValue(filterObj);
            fieldValue.setValue(field.toString());
            controlInputData && controlInputData.setAttribute('value', namesArr.length > 0 ? (namesArr.length == 1 ? namesArr.length + " item" : namesArr.length + " items") : "");            
        }
        if (field === 'action') {
            controlValue = this.filterForm.get("action.value") as FormControl;
            fieldValue = this.filterForm.get("action.field") as FormControl;
            controlInputData = <HTMLElement>document.getElementById("action");

            selectedItems.forEach(item => {
                namesArr.push(item.name);
            });

            let filterObj: CompositeFilterDescriptor = {
                logic: 'or',
                filters: []
            }

            namesArr.forEach(name => {
                filterObj.filters.push({
                    field: 'action',
                    operator: "eq",
                    value: name
                });
            })

            controlValue.setValue(filterObj);
            fieldValue.setValue(field.toString());
            controlInputData && controlInputData.setAttribute('value', namesArr.length > 0 ? (namesArr.length == 1 ? namesArr.length + " item" : namesArr.length + " items") : "");            
        }
        if (field === 'mode') {
            controlValue = this.filterForm.get("mode.value") as FormControl;
            fieldValue = this.filterForm.get("mode.field") as FormControl;
            controlInputData = <HTMLElement>document.getElementById("mode");

            selectedItems.forEach(item => {
                namesArr.push(item.name);
            });

            let filterObj: CompositeFilterDescriptor = {
                logic: 'or',
                filters: []
            }

            namesArr.forEach(name => {
                filterObj.filters.push({
                    field: 'mode',
                    operator: "eq",
                    value: name
                });
            })

            controlValue.setValue(filterObj);
            fieldValue.setValue(field.toString());
            controlInputData && controlInputData.setAttribute('value', namesArr.length > 0 ? (namesArr.length == 1 ? namesArr.length + " item" : namesArr.length + " items") : "");            
        }
        if (field === 'clear') {
            let userControlValue = this.filterForm.get("changeBy.value") as FormControl;
            let userFieldValue = this.filterForm.get("changeBy.field") as FormControl;
            let userControlInputData = <HTMLElement>document.getElementById("changeBy");

            userControlValue.setValue('');
            userFieldValue.setValue(field.toString());
            userControlInputData && userControlInputData.setAttribute('value', namesArr.toString());

            let actionControlValue = this.filterForm.get("action.value") as FormControl;
            let actionFieldValue = this.filterForm.get("action.field") as FormControl;
            let actionControlInputData = <HTMLElement>document.getElementById("action");

            actionControlValue.setValue('');
            actionFieldValue.setValue(field.toString());
            actionControlInputData && actionControlInputData.setAttribute('value', namesArr.toString());

            let modeControlValue = this.filterForm.get("mode.value") as FormControl;
            let modeFieldValue = this.filterForm.get("mode.field") as FormControl;
            let modeControlInputData = <HTMLElement>document.getElementById("mode");

            modeControlValue.setValue('');
            modeFieldValue.setValue(field.toString());
            modeControlInputData && modeControlInputData.setAttribute('value', namesArr.toString());

            this.selectedItems = [];
        }
        this.emitFilterChangeEvent();
    }

    emitFilterChangeEvent() {
        this.isFiltersApplied = false;
        this.isFilterButtonDisabled = false;
    }

    applyFilters() {
        this.filterChanged.emit();
        this.isFiltersApplied = true;
    }

    ngOnDestroy() {
        this.unsubscribe.next();
        this.unsubscribe.complete();
    }
}