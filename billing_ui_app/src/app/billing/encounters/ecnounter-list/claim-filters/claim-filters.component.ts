import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild } from "@angular/core";

import { ClaimFilterOptionModel } from "@core/models/billing/claim-filter-option-model";
import { DatePipe } from "@angular/common";
import { ClaimsManagementFilterService } from "@app/billing/services/claims-management-filter.service";
import { PaymentPostingFilterService } from "@app/billing/services/payment-posting-filter.service";
import { CreateInvoiceFilterService } from "@app/billing/services/create-invoice-filter.service";
import { PendingCollectionFilterService } from "@app/billing/services/pending-collection-filter.service";
import { CarcCodes } from "../../../../core/models/billing/carc-codes";
import { ClaimService } from '@core/services/billing';
import { SimpleChanges } from '@angular/core';
import { AccountMemberService } from "@core/services/account/account-member.service";
import { ClaimStatus } from "@core/enums/billing/claim-status";
import { ClaimListingTab } from "@core/enums/billing/claim-listing-tab";
import { FlaggedReason } from "@core/models/billing/flagged-reason";

@Component({
    selector: 'claim-filters',
    templateUrl: './claim-filters.component.html',
    styleUrls: ['./claim-filters.component.css']
})
export class ClaimFiltersComponent {
    @Input() opened: boolean = false;
    @Input() tab: ClaimListingTab | null;
    @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;
    @ViewChild('dateAnchorEl', { read: ElementRef }) public dateAnchor: ElementRef;
    @Output() filterChanged = new EventEmitter();
    @Output() setPreviousFilterEmitter = new EventEmitter<boolean>();
    isFiltersApplied: boolean = false;
    isClosedFilterReset: boolean = false;
    @Output() showVoidedEmitter = new EventEmitter<boolean>();
    public viewVoidedFromStatusPopup: boolean = false;

    isFilterButtonDisabled = true;
    rejectedid: number;
    isClosedFilterSet = false;
    availableStatusIds = new Set<number>();

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
    selectedFunders: ClaimFilterOptionModel[] = [];
    selectedRenderingProviders: ClaimFilterOptionModel[] = [];
    selectedStatuses: ClaimFilterOptionModel[] = []
    selectedValidations: ClaimFilterOptionModel[] = []
    selectedReasonCode: CarcCodes[] = [];
    selectedLocations: ClaimFilterOptionModel[] = [];
    selectedFlaggedReason: any[] = [];
    selectedAssignees: ClaimFilterOptionModel[] = [];
    patientName: string;
    funderName: string;
    balanceFrom: number | undefined;
    balanceTo: number | undefined;
    patientResponsibilityFrom: number | undefined;
    patientResponsibilityTo: number | undefined;
    renderingProviderName: string | undefined;
    statusName: string | undefined;
    dateFrom: Date | undefined;
    dateTo: Date | undefined;
    dateFromString: string | undefined;
    dateToString: string | undefined;

    showDatePopup = false;
    userTouchedStatuses = false;
    @Output() userTouchedStatusesChange = new EventEmitter<boolean>();
    @Output() showVoidedChange = new EventEmitter<boolean>();

    constructor(private datePipe: DatePipe,
        private claimsManagementFilterService: ClaimsManagementFilterService,
        private paymentPostingFilterService: PaymentPostingFilterService,
        private createInvoiceFilter: CreateInvoiceFilterService,
        private pendingCollectionFilter: PendingCollectionFilterService,
        private claimsService: ClaimService,
        private accountService: AccountMemberService) {
        this.setPreviousFilters();
        this.claimsService.getId().subscribe(id => {
            if (id !== null) {
                this.rejectedid = id
            }
        });
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

        if (filterId == 4)
            if (this.dateFrom == undefined || this.dateTo == undefined) {
                this.dateFrom = new Date();
                this.dateFrom.setHours(0);
                this.dateFrom.setMinutes(0);
                this.dateFrom.setSeconds(0);
                this.dateFrom.setMilliseconds(0);

                this.dateTo = new Date();
                this.dateTo.setHours(0);
                this.dateTo.setMinutes(0);
                this.dateTo.setSeconds(0);
                this.dateTo.setMilliseconds(0);

            }
        this.showDatePopup = true;
    }

    clearFilters(isEventEmitted: boolean = true) {
        this.selectedPatients = [];
        this.selectedFunders = [];
        this.selectedLocations = [];
        this.selectedRenderingProviders = [];
        this.balanceFrom = undefined;
        this.balanceTo = undefined;
        this.patientResponsibilityFrom = undefined;
        this.patientResponsibilityTo = undefined;
        this.dateFrom = undefined;
        this.dateTo = undefined;
        this.renderingProviderName = undefined;
        this.selectedStatuses = [];
        this.selectedValidations = [];
        this.selectedReasonCode = [];
        this.selectedFlaggedReason = [];
        this.selectedAssignees = [];
        this.dateFromString = undefined;
        this.dateToString = undefined;
        this.claimsManagementFilterService.isFilterSet = false;

        this.isFilterButtonDisabled = true;
        this.isFiltersApplied = false;
        this.isClosedFilterSet = false;
        this.isClosedFilterReset = false;
        this.userTouchedStatuses = false;

        this.viewVoidedFromStatusPopup = false;
        this.showVoidedEmitter.emit(false);

        if (this.tab === ClaimListingTab.Closed) {
            this.claimsService.getClaimTabStatuses({
                Tab: this.tab,
                SearchValue: '',
                AccountInfoId: this.accountService.memberDetails.accountInfoId
            })
                // .pipe(take(1))
                .subscribe((statuses) => {
                    const closed = statuses.find(s => (s.name || '').toLowerCase() === 'closed');
                    if (closed) {
                        this.selectedStatuses = [{ ...closed, checked: true }];
                        this.isClosedFilterSet = true;
                    }
                    if (isEventEmitted) this.filterChanged.emit();  // now the parent will read statusIds with Closed included
                });

            // prevent early emit; we will emit inside the subscribe once Closed is set
            return;
        }
        if (isEventEmitted) this.filterChanged.emit();
    }

    applyFilters() {
        this.isFiltersApplied = true;
        this.isFilterButtonDisabled = false;
        this.isClosedFilterReset = true;
        this.filterChanged.emit();
    }

    onFilterChange() {
        this.isFiltersApplied = false;
        this.isFilterButtonDisabled = false;
    }

    onDateFilterLeave() {
        this.showDatePopup = false;
    }

    setDatePeriod(dateRange: any): void {
        this.dateFromString = this.datePipe.transform(dateRange.start, 'MM/dd/yy');
        this.dateToString = this.datePipe.transform(dateRange.end, 'MM/dd/yy');
        this.dateFrom = dateRange.start;
        this.dateTo = dateRange.end;
        this.onFilterChange();
        this.showDatePopup = false;
    }

    setPreviousFilters() {
        this.paymentPostingFilterService.isFilterSet = false;
        this.createInvoiceFilter.isFilterSet = false;
        this.pendingCollectionFilter.isFilterSet = false;
        let filters: ClaimFiltersComponent = this.claimsManagementFilterService.getFilter();
        if (filters) {
            let ComponentVarList = [
                "selectedFilterId",
                "selectedPatients",
                "selectedFunders",
                "selectedAssignees",
                "selectedLocations",
                "selectedRenderingProviders",
                "selectedStatuses",
                "selectedValidations",
                "selectedReasonCode",
                "selectedFlaggedReason",
                "balanceFrom",
                "balanceTo",
                "dateFrom",
                "dateFromString",
                "dateTo",
                "dateToString",
                "patientResponsibilityFrom",
                "patientResponsibilityTo",
                "isClosedFilterSet",
                "isClosedFilterReset",
                "userTouchedStatuses"
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

    onChildShowVoidedChange(value: boolean) {
        this.viewVoidedFromStatusPopup = value;
        this.showVoidedEmitter.emit(value);
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['tab'] && !changes['tab'].firstChange) {
            // reset voided & flags
            this.viewVoidedFromStatusPopup = false;
            this.showVoidedEmitter.emit(false);
            this.isClosedFilterSet = false;
            this.isClosedFilterReset = false;
            this.userTouchedStatuses = false;


            // clear statuses when leaving Closed
            if (this.tab !== ClaimListingTab.Closed) {
                const before = this.selectedStatuses || [];
                const filtered = before.filter(s => {
                    const n = (s.name || '').toLowerCase();
                    return n !== 'closed' && n !== 'void-closed';
                });
                if (filtered.length !== before.length) {
                    this.selectedStatuses = filtered;
                }
            } else {
                // entering Closed tab -> default Closed immediately so badge matches
                this.selectedStatuses = [{
                    id: ClaimStatus.Closed,
                    name: 'Closed',
                    checked: true
                }];
                this.isClosedFilterSet = true;
            }
        }
    }

    checkedStatusCount(): number {
        if (!this.availableStatusIds || this.availableStatusIds.size === 0) return 0;
        return (this.selectedStatuses || []).filter(
            s => !!s.checked && this.availableStatusIds.has(s.id)
        ).length;
    }

    onStatusesLoaded(ids: number[]) {
        this.availableStatusIds = new Set(ids || []);
    }
}
