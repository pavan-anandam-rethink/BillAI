import { DatePipe } from "@angular/common";
import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild } from "@angular/core";
import { ClaimsManagementFilterService } from "@app/billing/services/claims-management-filter.service";
import { CreateInvoiceFilterService } from "@app/billing/services/create-invoice-filter.service";
import { PaymentPostingFilterService } from "@app/billing/services/payment-posting-filter.service";
import { PendingCollectionFilterService } from "@app/billing/services/pending-collection-filter.service";
import { ClaimFilterOptionModel } from "@core/models/billing/claim-filter-option-model";
import { event } from "jquery";

@Component({
    selector: 'create-invoice-filters',
    templateUrl: './create-invoice-filters.component.html',
    styleUrls: ['./create-invoice-filters.component.css']
})
export class CreateInvoiceFiltersComponent {
    @Input() opened: boolean = false;
    @Input() userList: ClaimFilterOptionModel[];
    @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;
    @ViewChild('dateAnchorEl', { read: ElementRef }) public dateAnchor: ElementRef;
    @Output() filterChanged = new EventEmitter();
    isFiltersApplied: boolean = false;
    isFilterButtonDisabled = true;

    @HostListener('keydown', ['$event'])
    public keydown(event: any): void {
        if (event.keyCode === 27) {
            this.selectFilter(undefined);
        }
    }

    @HostListener('document:click', ['$event'])
    public documentClick(event: any): void {
        if (!this.selectedAnchor)
            return;

        if (!this.contains(event.target)) {
            if (this.selectedFilterId != undefined)
                this.selectFilter(undefined);
        }
    }
    selectedFilterId: number | undefined = undefined;
    selectedAnchor: any;

    selectedPatients: ClaimFilterOptionModel[] = [];

    patientName: string;
    dateFrom: Date | undefined;
    dateTo: Date | undefined;
    dateFromString: string | undefined;
    dateToString: string | undefined;
    patientResponsibilityFrom: number | undefined;
    patientResponsibilityTo: number | undefined;

    showDatePopup = false;
    dateElAnchor: any;
    isDateActive = false;

    constructor(private datePipe: DatePipe, 
        private filterService: CreateInvoiceFilterService,
            private paymentPostingFilterService: PaymentPostingFilterService,
            private claimFilterService: ClaimsManagementFilterService) {
        this.setPreviouslyAppliedFilters();
    }

    private contains(target: HTMLElement): boolean {
        return this.selectedAnchor.contains(target) ||
            (this.popup ? this.popup.nativeElement.contains(target) : false);
    }

    selectFilter(filterId: number | undefined, anchor: any = undefined) {
        if (filterId == this.selectedFilterId) {
            this.selectedFilterId = undefined;        
        } else {
            this.selectedFilterId = filterId;
        }
        this.selectedAnchor = anchor;
        if(filterId == 2)
            if(this.dateFrom == undefined || this.dateTo == undefined)
                {
                  this.dateFrom = new Date();
                  this.dateTo = new Date();
                }
            this.showDatePopup = true;
    }

    clearFilters() {
        this.selectedPatients = [];
        this.patientResponsibilityFrom = undefined;
        this.patientResponsibilityTo = undefined;
        this.dateFrom = undefined;
        this.dateTo = undefined;
        this.dateFromString = undefined;
        this.dateToString = undefined;
        this.userList.forEach(x => x.checked = false);
        this.filterService.isFilterSet = false;
        this.filterChanged.emit();
        this.isFilterButtonDisabled = true;
        this.isFiltersApplied = false;
    }

    onDateFilterLeave(){
        this.showDatePopup =false;
    }

    setDatePeriod(dateRange: any): void {
         this.dateFromString = this.datePipe.transform(dateRange.start, 'MM/dd/yy');
         this.dateToString = this.datePipe.transform(dateRange.end, 'MM/dd/yy');
         this.dateFrom = dateRange.start;
         this.dateFrom.setMinutes(0);
         this.dateFrom.setSeconds(0);
         this.dateFrom.setMilliseconds(0);
         this.dateTo = dateRange.end;
         this.dateTo.setMinutes(0);
         this.dateTo.setSeconds(0);
         this.dateTo.setMilliseconds(0);
         this.onFilterChange();
         this.showDatePopup = false;
    }

    setPatientResponsibilityRange(range: any): void {
        this.patientResponsibilityFrom = range.From;
        this.patientResponsibilityTo = range.To;
        this.onFilterChange();
    }

    applyFilters() {
        this.filterChanged.emit();
        this.isFiltersApplied = true;
        this.isFilterButtonDisabled = false;
    }

    onFilterChange() {
        this.isFiltersApplied = false;
        this.isFilterButtonDisabled = false;
    }

    setPreviouslyAppliedFilters() {
        this.claimFilterService.isFilterSet = false;
        this.paymentPostingFilterService.isFilterSet = false;
        let filters: CreateInvoiceFiltersComponent = this.filterService.getFilter();
        if (filters) {
            let ComponentVarList = [
                "selectedPatients",
                "patientResponsibilityFrom",
                "patientResponsibilityTo",
                "dateFromString",
                "dateToString",                
                "dateFrom",
                "dateTo",
            ];

            ComponentVarList.forEach((key) => {
                if (key in filters) {
                    this[key] = filters[key];
                }
            }); 
            this.isFiltersApplied = true;
            this.isFilterButtonDisabled = false;
        }
    }
}