import { Component, ElementRef, Input, OnDestroy, ViewChild, ViewEncapsulation, AfterViewInit, Output, EventEmitter } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';

import { Observable, Subject } from "rxjs";

import { PaymentPostingService } from '@core/services/billing/index';
import { PaymentPostingMethods } from '@core/models/billing/index';
import { PaymentPostingListFunderSearch } from '@core/models/billing';
import { DatePipe } from "@angular/common";
import { PaymentPostingFilterService } from '@app/billing/services/payment-posting-filter.service';
import { ClaimsManagementFilterService } from '@app/billing/services/claims-management-filter.service';
import { CreateInvoiceFilterService } from '@app/billing/services/create-invoice-filter.service';
import { PendingCollectionFilterService } from '@app/billing/services/pending-collection-filter.service';

@Component({
    selector: 'payment-posting-list-filter',
    templateUrl: './payment-posting-list-filter.component.html',
    styleUrls: ['./payment-posting-list-filter.component.css'],
    encapsulation: ViewEncapsulation.None,
    providers: [DatePipe]
})

export class PaymentPostingListFilterComponent implements OnDestroy, AfterViewInit {
    public defaultItem = "Date Received";
    @Input() filterForm: FormGroup;
    @Input() opened: boolean = false;
    @Output() filterChanged = new EventEmitter();

    @ViewChild("paymentMethodMultiselect") public paymentMethodMultiselect: any;
    @ViewChild("receivedDateDropdown") public dropdownlist: any;
    @ViewChild("funderAnchorEl") public funderInput: any;
    @ViewChild("reconcileStatusMultiselect") public reconcileStatusMultiselect: any;
    @ViewChild('funderAnchorEl', { read: ElementRef }) public funderAnchor: ElementRef;
    @ViewChild('statusAnchorEl', { read: ElementRef }) public statusAnchor: ElementRef;
    @ViewChild('methodAnchorEl', { read: ElementRef }) public methodAnchor: ElementRef;
    @ViewChild('optionAnchorEl', { read: ElementRef }) public optionAnchor: ElementRef;
    @ViewChild('calendar', { read: ElementRef }) public receivedDateDropdownEl: ElementRef;
    @ViewChild('dateReceivedPopup', { read: ElementRef }) public parentPopup: ElementRef;
    @ViewChild('modeAnchorEl', { read: ElementRef }) public modeAnchor: ElementRef;
    private unsubscribe = new Subject();

    selectedPaymentMethods: PaymentPostingMethods[] = [];
    selectedFunders: PaymentPostingListFunderSearch[] = [];
    selectedStatuses: any[] = [];
    selectedOption: string = "";
    selectedModes: any[] = [];
    showFunderPopup = false;
    showStatusPopup = false;
    showMethodPopup = false;
    showOptionPopup = false;
    showModePopup = false;
    selectedFilterId = 0;
    funderElAnchor: any;
    statusElAnchor: any;
    methodElAnchor: any;
    modeElAnchor: any;

    receivedPopupAnchorEl: any;
    receivedPopupMargin: any = {
        horizontal: 0,
        vertical: 0
    };
    showReceivedDatePopup = false;
    isDefaultDateSelected = true;
    statusNames: string;
    methodsNames: string;
    modeNames: string;
    isFiltersApplied: boolean = false;
    selectedOptionString: string;
    modeOptions: string[] = ['Electronic', 'Manual', 'None'];

    isFilterButtonDisabled = true;
    referenceNumber: string = "";

    constructor(private datePipe: DatePipe
        , private filterService: PaymentPostingFilterService
        , private claimFilterService: ClaimsManagementFilterService
        , private createInvoiceFilter: CreateInvoiceFilterService
        , private pendingCollectionFilter: PendingCollectionFilterService
    ) {
        this.setPreviouslyAppliedFilters();
    }

    methodChanged(selectedMethods: PaymentPostingMethods[]): void {
        this.selectedPaymentMethods = selectedMethods;

        const methodControlValue: FormControl = this.filterForm.get("paymentMethodId.value") as FormControl;
        const methodControlInputData: FormControl = this.filterForm.get("paymentMethodId.inputData") as FormControl;

        const methodEnumsArr: string[] = [];
        const methodNamesArr: string[] = [];

        this.selectedPaymentMethods.forEach(method => {
            methodEnumsArr.push(method.enumValue);
            methodNamesArr.push(method.displayName);
        });
        methodControlValue.setValue(methodEnumsArr);
        methodControlInputData.setValue(methodNamesArr.toString());
    }

    isMethodChecked(methodName: string): boolean {
        return this.selectedPaymentMethods.some((item: PaymentPostingMethods) => item.displayName == methodName);
    }

    excludeDefault(): void {
        this.dropdownlist.defaultItem = null;
    }

    receivedDateChanged(value: string): void {
        let calculatedDate;
        let startRangeDate: string = '';
        let endRangeDate: Date = new Date(Date.now());

        switch (value) {
            case 'Last 7 days':
                calculatedDate = new Date(Date.now() - 7 * 24 * 60 * 60 * 1000);
                startRangeDate = calculatedDate.toDateString();
                break;
            case 'Last 30 days':
                calculatedDate = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000);
                startRangeDate = calculatedDate.toDateString();
                break;
            case 'This month':
                calculatedDate = new Date(endRangeDate.getFullYear(), endRangeDate.getMonth(), 1);
                startRangeDate = calculatedDate.toDateString();
                break;
            case 'Last month':
                calculatedDate = new Date(Date.now());
                calculatedDate.setDate(1);
                calculatedDate.setMonth(endRangeDate.getMonth() - 1);
                startRangeDate = calculatedDate.toDateString();
                endRangeDate = new Date(endRangeDate.getFullYear(), endRangeDate.getMonth(), 0)
                break;
            case 'Custom Date Range':
                this.receivedDateToggle(true);
                break;
            default:
                startRangeDate = '';
                break;
        }

        if (value !== 'Custom Date Range') {
            let selectedRange = {
                start: startRangeDate,
                end: endRangeDate.toDateString()
            }
            this.setReceivedDate(selectedRange, false);
        }

        this.isDefaultDateSelected = value == this.defaultItem;
    }
    selectedStartDate: any;
    selectedEndDate: any;
    setReceivedDate(dateRange: any, isCustomRange: boolean): void {

        if (dateRange.start !== '') {
            this.dateFromString = this.datePipe.transform(dateRange.start, 'MM/dd/yy');
            this.dateToString = this.datePipe.transform(dateRange.end, 'MM/dd/yy');
            this.dateFrom = dateRange.start;
            this.dateTo = dateRange.end;
            this.selectedStartDate = this.datePipe.transform(dateRange.start, 'MM/dd/yyyy');
            this.selectedEndDate = this.datePipe.transform(dateRange.end, 'MM/dd/yyyy');
            this.selectedOptionString = isCustomRange ? this.selectedStartDate + ' - ' + this.selectedEndDate : this.selectedOption;
        }
        this.isFiltersApplied = false;
    }

    onDateClose(event: any): void {
        let activeEl = document.activeElement;

        if (this.dropdownlist.wrapper.nativeElement.contains(activeEl) ||
            (activeEl !== null && activeEl.id == "calendarComponent")) {
            event.preventDefault();
        } else {
            let receivedDateControlValue = this.filterForm.get("receivedDate.value.value");
            if (receivedDateControlValue == null) {
                this.dropdownlist.defaultItem = this.defaultItem;
            }
        }
    }

    receivedDateToggle(state: boolean): void {
        if (state) {
            this.receivedPopupAnchorEl = document.getElementById("receivedDate");
            if (this.receivedPopupAnchorEl !== null) {
                let positions = this.receivedPopupAnchorEl.getBoundingClientRect();
                this.receivedPopupMargin.horizontal = positions.width;
                this.receivedPopupMargin.vertical = 100;
            }
        }

        this.showReceivedDatePopup = state;
        this.showOptionPopup = state;
    }

    closeCalendar(e: any) {
        this.showReceivedDatePopup = false;
        this.showOptionPopup = false;
    }

    funderToggle(event: Event): void {
        this.funderElAnchor = event.currentTarget;
        this.showFunderPopup = !this.showFunderPopup;
        this.selectedFilterId = 2;
    }

    statusToggle(event: Event): void {
        this.statusElAnchor = event.currentTarget;
        this.showStatusPopup = !this.showStatusPopup;
        this.selectedFilterId = 3;
    }
    methodToggle(event: Event): void {
        this.methodElAnchor = event.currentTarget;
        this.showMethodPopup = !this.showMethodPopup;
        this.selectedFilterId = 1;
    }
    optionToggle(event: Event): void {
        this.methodElAnchor = event.currentTarget;
        this.showOptionPopup = !this.showOptionPopup;
        this.selectedFilterId = 4;
    }

    funderClicked(funder: PaymentPostingListFunderSearch) {
        funder.checked = !funder.checked;

        if (funder.checked) {
            this.selectedFunders.push(funder);
        } else {
            let itemIndex = this.selectedFunders.indexOf(funder);
            this.selectedFunders.splice(itemIndex, 1);
        }
        this.isFiltersApplied = false;
        this.isFilterButtonDisabled = false;
    }

    statusClicked(status: any) {
        status.checked = !status.checked;

        if (status.checked) {
            this.selectedStatuses.push(status);
        } else {
            let itemIndex = this.selectedStatuses.indexOf(status);
            this.selectedStatuses.splice(itemIndex, 1);
        }
        this.statusNames = this.selectedStatuses.map(itm => itm.statusName).join(', ');
        this.isFiltersApplied = false;
        this.isFilterButtonDisabled = false;
    }

    methodClicked(method: any) {
        method.checked = !method.checked;

        if (method.checked) {
            this.selectedPaymentMethods.push(method);
        } else {
            let itemIndex = this.selectedPaymentMethods.indexOf(method);
            this.selectedPaymentMethods.splice(itemIndex, 1);
        }

        // Update the methods names, truncated if there are more than 2 items
        if (this.selectedPaymentMethods.length > 2) {
            // Show first 2 items followed by "..."
            this.methodsNames = this.selectedPaymentMethods.slice(0, 2).map(itm => itm.displayName).join(', ') + ', ...';
        } else {
            // Show all selected items
            this.methodsNames = this.selectedPaymentMethods.map(itm => itm.displayName).join(', ');
        }

        this.isFiltersApplied = false;
        this.isFilterButtonDisabled = false;
    }

    optionClicked(option: any) {
        this.selectedOption = option;
        this.selectedOptionString = option != 'Custom Date Range' ? this.selectedOption : this.selectedOptionString;
        this.receivedDateChanged(option);
        this.isFiltersApplied = false;
        this.isFilterButtonDisabled = false;
    }

    referenceNumberChange(event) {
        this.referenceNumber = event.target.value;
        this.isFiltersApplied = false;
        this.isFilterButtonDisabled = false;
    }

    onWheel(event: any) {
        let target = (<HTMLElement>event.target);
        if (target.tagName === 'SPAN' && target && target.parentElement) {
            let parent = target.parentElement;
            if (parent && parent.parentElement) {
                let parent2 = parent.parentElement;
                if (parent2.tagName === 'UL') {
                    parent2.scrollLeft += event.deltaY;
                } else {
                    if (parent2 && parent2.parentElement) {
                        let parent3 = parent2.parentElement;
                        parent3.scrollLeft += event.deltaY;
                    }
                }
            }
        } else if (target.tagName === 'LI' && target && target.parentElement) {
            let parent = target.parentElement;
            parent.scrollLeft += event.deltaY;
        } else if (target.tagName === 'UL' && target) {
            target.scrollLeft += event.deltaY;
        }
        event.preventDefault();
    }

    ngAfterViewInit() {
        let method = document.getElementById('paymentMethod');
        if (method) {
            let child = method.getElementsByClassName('k-reset')[0];
            if (child) {
                child.addEventListener('wheel', this.onWheel.bind(this));
            }
        }
    }

    ngOnDestroy() {
        this.unsubscribe.next(void 0);
        this.unsubscribe.complete();
    }


    dateFrom: Date | undefined;
    dateTo: Date | undefined;
    dateFromString: string | undefined;
    dateToString: string | undefined;

    clearFilters() {
        this.selectedFunders = [];
        this.selectedPaymentMethods = [];
        this.selectedStatuses = [];
        this.selectedOption = "";
        this.selectedOptionString = "";
        this.selectedStartDate = undefined;
        this.selectedEndDate = undefined;
        this.referenceNumber = undefined;
        this.filterService.isFilterSet = false;
        this.isFilterButtonDisabled = true;
        this.dateFrom = undefined;
        this.dateTo = undefined;
        this.dateFromString = undefined;
        this.dateToString = undefined;
        this.isFiltersApplied = false;
        this.selectedModes = [];
        this.filterChanged.emit();

    }
    applyFilters() {
        this.filterChanged.emit();
        this.isFiltersApplied = true;
        this.isFilterButtonDisabled = false;
    }

    setPreviouslyAppliedFilters() {
        this.claimFilterService.isFilterSet = false;
        this.createInvoiceFilter.isFilterSet = false;
        this.pendingCollectionFilter.isFilterSet = false;
        let filters: PaymentPostingListFilterComponent = this.filterService.getFilter();
        if (filters) {
            let ComponentVarList = [
                "selectedPaymentMethods",
                "selectedFunders",
                "selectedStatuses",
                "selectedStartDate",
                "selectedOption",
                "selectedEndDate",
                "dateToString",
                'referenceNumber',
                "selectedModes"
            ];

            ComponentVarList.forEach((key) => {
                if (key in filters) {
                    this[key] = filters[key];
                }
            });
            this.methodsNames = this.selectedPaymentMethods.map(itm => itm.displayName).join(', ');
            this.statusNames = this.selectedStatuses.map(itm => itm.statusName).join(', ');
            this.modeNames = this.selectedModes.map(item => item.modeName).join(', ');
            this.selectedOptionString = this.selectedOption != 'Custom Date Range' ? this.selectedOption : this.selectedStartDate + ' - ' + this.selectedEndDate;
            this.isFiltersApplied = true;
            this.isFilterButtonDisabled = false;
        }
    }

    modeToggle(event: Event) {
        console.log('Opening popup. selectedModes:', this.selectedModes);
        this.modeElAnchor = event.currentTarget;
        this.showModePopup = !this.showModePopup;
        this.selectedFilterId = 5;
    }

    modeClicked(mode: any) {

        mode.checked = !mode.checked;

        if (mode.checked) {
             this.selectedModes.push(mode);
        }
        else {
            let itemIndex = this.selectedModes.indexOf(mode);
            this.selectedModes.splice(itemIndex, 1);
        }
        this.modeNames = this.selectedModes.map(itm => itm.modeName).join(', ');
        this.isFiltersApplied = false;
        this.isFilterButtonDisabled = false;
    }
}
