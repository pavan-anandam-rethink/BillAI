import { DatePipe } from "@angular/common";
import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild } from "@angular/core";
import { PendingCollectionFilterService } from "@app/billing/services/pending-collection-filter.service";
import { ClaimFilterOptionModel } from "@core/models/billing/claim-filter-option-model";

@Component({
    selector: 'pending-collection-filters',
    templateUrl: './pending-collection-filters.component.html',
    styleUrls: ['./pending-collection-filters.component.css']
})
export class PendingCollectionFiltersComponent {
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
    dateOfServiceFrom: Date | undefined;
    dateOfServiceTo: Date | undefined;
    dateOfServiceFromString: string | undefined;
    dateOfServiceToString: string | undefined;
    invoiceFrom: Date | undefined;
    invoiceTo: Date | undefined;
    invoiceFromString: string | undefined;
    invoiceToString: string | undefined;
    paymentDueFrom: Date | undefined;
    paymentDueTo: Date | undefined;
    paymentDueFromString: string | undefined;
    paymentDueToString: string | undefined;
    patientResponsibilityFrom: number | undefined;
    patientResponsibilityTo: number | undefined;

    showDatePopup = false;
    dateElAnchor: any;
    isDateActive = false;

    constructor(private datePipe: DatePipe, 
        private filterService: PendingCollectionFilterService) {
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
        this.showDatePopup = true;
    }

    clearFilters() {
        this.selectedPatients = [];
        this.patientResponsibilityFrom = undefined;
        this.patientResponsibilityTo = undefined;
        this.dateOfServiceFrom = undefined;
        this.dateOfServiceTo = undefined;
        this.paymentDueFrom = undefined;
        this.paymentDueTo = undefined;
        this.invoiceFrom = undefined;
        this.invoiceTo = undefined;
        this.dateOfServiceFromString = undefined;
        this.dateOfServiceToString = undefined;
        this.userList.forEach(x => x.checked = false);
        this.filterService.isFilterSet = false;
        this.filterChanged.emit();  
        this.isFilterButtonDisabled = true;
        this.isFiltersApplied = false;
    }

    onDateFilterLeave(){
        this.showDatePopup =false;
    }
    
    onFilterChanged() {
        this.filterChanged.emit();
    }

    setDOSPeriod(dateRange: any): void {
         this.dateOfServiceFromString = this.datePipe.transform(dateRange.start, 'MM/dd/yy');
         this.dateOfServiceToString = this.datePipe.transform(dateRange.end, 'MM/dd/yy');
         this.dateOfServiceFrom = dateRange.start;
         this.dateOfServiceTo = dateRange.end;
         this.onFilterChange();
         this.showDatePopup = false;
    }

    setInvoiceDatePeriod(dateRange: any): void {
        this.invoiceFromString = this.datePipe.transform(dateRange.start, 'MM/dd/yy');
        this.invoiceToString = this.datePipe.transform(dateRange.end, 'MM/dd/yy');
        this.invoiceFrom = dateRange.start;
        this.invoiceTo = dateRange.end;
        this.onFilterChange();
        this.showDatePopup = false;

    }

   setpaymentDueDatePeriod(dateRange: any): void {
        this.paymentDueFromString = this.datePipe.transform(dateRange.start, 'MM/dd/yy');
        this.paymentDueToString = this.datePipe.transform(dateRange.end, 'MM/dd/yy');
        this.paymentDueFrom = dateRange.start;
        this.paymentDueTo = dateRange.end;
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
}