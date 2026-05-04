import { ChangeDetectorRef, Component, ElementRef, forwardRef, Renderer2, ViewChild ,OnInit, OnDestroy} from '@angular/core';
import { Observable, Subscription } from "rxjs";
import { ActivatedRoute, Router } from '@angular/router';
import { GridComponent, GridDataResult, PagerSettings, SelectableMode, SelectableSettings } from '@progress/kendo-angular-grid';
import { State } from '@progress/kendo-data-query';
import { Locale } from '@app/locale';
import { MemberViewSettings } from '@core/models/billing/member-view-settings';
import { MatTabGroup } from '@angular/material/tabs';
import { SidebarService } from '@app/shared/components/sidebar';
import { PaginationService } from '@core/services/billing/pagination.service';
import { PatientInvoiceDetails, PatientInvoiceHeader } from '@core/models/billing/patient-invoice';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { PatientInvoiceListSubject } from '@core/subjects/patient-invoice-list.subject';
import { ConfirmDialog } from '@core/models/common';
import { CreateInvoiceFiltersComponent } from './create-invoice/create-invoice-filters/create-invoice-filters.component';
import { ClaimsManagementFilterService } from '@app/billing/services/claims-management-filter.service';

export enum InvoiceListingTab {
    CreateInvoice = 1,
    PendingCollection,
    ClientHistory
}

export interface IndexResult {
    index: number;
    anchor: HTMLAnchorElement;
    clientId: number;
}

@Component({
    selector: 'patient-invoice',
    templateUrl: './patient-invoice.component.html',
    styleUrls: ['./patient-invoice.component.css',
        './status-actions.css']
})
export class PatientInvoiceComponent implements OnInit, OnDestroy {
    @ViewChild(forwardRef(() => CreateInvoiceFiltersComponent)) claimFiltersComponent: CreateInvoiceFiltersComponent;
    @ViewChild("patientInvoiceListingTabGroup") patientInvoiceListingTabGroup: MatTabGroup;
    @ViewChild("encountersGrid") claimsGrid: GridComponent;
    @ViewChild(GridComponent) encounterGrid: GridComponent;
    @ViewChild('selectorAnchorEl', { read: ElementRef }) public selectorAnchor: ElementRef;
    @ViewChild('printAnchorEl', { read: ElementRef }) public printAnchor: ElementRef;

    confirmIncludePreviousInvoices = new ConfirmDialog(false, "Confirmation",
        "Include Previous Invoices?", "Yes", "No");


    subscriptions = new Subscription();
    public mode: SelectableMode = "multiple";
    headerTitle = "Patient Invoicing";
    public isSubjectLoading$: boolean = false;
    breadcrumbs: any[] = [];
    private breadcrumbClickHandler: (event: any) => void;

    readonly pagingSettings: PagerSettings = {
        buttonCount: 5,
        type: 'numeric',
        pageSizes: true,
        previousNext: true
    };

    readonly selectableSettings: SelectableSettings = {
        checkboxOnly: true,
        enabled: true,
        mode: 'multiple'
    };

    selectedPatientWithLines: PatientInvoiceDetails[] = [];


    filterChangedTimeout: number;

    selectedTab: InvoiceListingTab | null;
    selectedTabIndex = 0;
    showSelectorPopup = false;
    flipSelector = false;
    viewColumnsSettings: MemberViewSettings;
    showPrintPopup = false;
    flipPrintPopup = false;
    selectedClients: number[] = [];
    selectAllPatients = false;

    IndexList: IndexResult[] = [];


    public mySelection: number[] = [];
    userList: ClaimFilterOptionModel[] | [];

    patientInvoiceListSubject: PatientInvoiceListSubject;
    view: Observable<GridDataResult>
    invoiceDetails: PatientInvoiceDetails[] = [];
    viewData: PatientInvoiceHeader[] = [];
    totalInvoiceHeaders = 0;


    gridState: State = {
        sort: [{ dir: "desc", field: "dateOfServiceStart" }],
        skip: 0,
        take: 10,

        // Initial filter descriptor
        filter: {
            logic: 'and',
            filters: []
        }
    };

    showFilter: boolean;

    rethinkUrl: string;

    constructor(
        private router: Router,
        private route: ActivatedRoute,
        private sidebarService: SidebarService,
        private paginationService: PaginationService,
        public locale: Locale,
        private cdr: ChangeDetectorRef,
        private renderer: Renderer2,
        private claimsManagementFilterService:ClaimsManagementFilterService
    ) {
        this.selectedTab = InvoiceListingTab.CreateInvoice;
        this.gridState.take = this.paginationService.getPageSize();
    }
    ngOnInit(): void {
        // Set initial breadcrumbs
        this.breadcrumbs = [
            { label: 'Patient Invoicing', url: '/billing/patientinvoicing/list' },
            { label: 'Create Invoice', url: '/billing/patientinvoicing/list' }
        ];

        // Listen for breadcrumb updates from child components
        window.addEventListener('updateBreadcrumbs', (event: any) => {
            this.breadcrumbs = event.detail;
        });

        // Intercept breadcrumb clicks to override header navigation
        this.breadcrumbClickHandler = (event: any) => {
            const target = event.target;
            if (target && target.classList.contains('breadcrumb-link')) {
                event.preventDefault(); // Prevent router navigation
                const breadcrumbText = target.textContent?.trim();
                const breadcrumbIndex = this.breadcrumbs.findIndex(b => b.label === breadcrumbText);
                if (breadcrumbIndex !== -1 && breadcrumbIndex < this.breadcrumbs.length - 1) {
                    this.handleBreadcrumbClick(this.breadcrumbs[breadcrumbIndex], breadcrumbIndex);
                }
            }
        };
        document.addEventListener('click', this.breadcrumbClickHandler);

        this.route.queryParams.subscribe(params => {
            const tab = params['tab'];
            if (tab === 'pendingCollection') {
            this.selectedTabIndex = 2;
            this.selectedTab = InvoiceListingTab.PendingCollection;
            }
        });
    }

    handleBreadcrumbClick(breadcrumb: any, index: number): void {
        if (index === this.breadcrumbs.length - 1) {
            return; // Don't navigate if clicking on the last breadcrumb
        }
        
        if (breadcrumb.label === 'Patient Invoicing') {
            // Navigate to default tab (Create Invoice)
            this.selectedTabIndex = 0;
            this.selectedTab = InvoiceListingTab.CreateInvoice;
            this.patientInvoiceListingTabGroup.selectedIndex = 0;
            
            // Update breadcrumb and collapse any details
            this.breadcrumbs = [
                { label: 'Patient Invoicing', url: '/billing/patientinvoicing/list' },
                { label: 'Create Invoice', url: '/billing/patientinvoicing/list' }
            ];
            // Trigger grid collapse in child components
            this.collapseChildGrids();
        } else if (breadcrumb.label === 'Create Invoice' || breadcrumb.label === 'Pending Collection') {
            // Remove Invoice Details if present
            this.breadcrumbs = this.breadcrumbs.slice(0, 2);
            this.collapseChildGrids();
        }
    }

    collapseChildGrids(): void {
        // Emit custom event to child components to collapse their grids
        window.dispatchEvent(new CustomEvent('collapseGrids'));
    }

    selectedTabChanged(id: number) {
        switch (id) {
            case 0:
                this.selectedTab = InvoiceListingTab.CreateInvoice;
                this.breadcrumbs = [
                    { label: 'Patient Invoicing', url: '/billing/patientinvoicing/list' },
                    { label: 'Create Invoice', url: '/billing/patientinvoicing/list' }
                ];
                break;
            case 1:
                this.selectedTab = InvoiceListingTab.PendingCollection;
                this.breadcrumbs = [
                    { label: 'Patient Invoicing', url: '/billing/patientinvoicing/list' },
                    { label: 'Pending Collection', url: '/billing/patientinvoicing/list' }
                ];
                break;
            case 2:
                this.selectedTab = InvoiceListingTab.ClientHistory;
                this.breadcrumbs = [
                    { label: 'Patient Invoicing', url: '/billing/patientinvoicing/list' },
                    { label: 'Client History', url: '/billing/patientinvoicing/list' }
                ];
                break;
        }
    }

    // Method for child components to update breadcrumbs
    updateBreadcrumbs(breadcrumbs: any[]): void {
        this.breadcrumbs = breadcrumbs;
    }

    ngAfterContentChecked() {
        this.cdr.detectChanges();
    }

    ngAfterViewInit(): void {
        const currentTabIndex = this.patientInvoiceListingTabGroup.selectedIndex;
        if (currentTabIndex === this.selectedTabIndex) {
            this.selectedTabChanged(this.selectedTabIndex)
        }

        this.patientInvoiceListingTabGroup.selectedIndex = this.selectedTabIndex;
    }

    ngOnDestroy(): void {
        this.subscriptions.unsubscribe();
        // Remove breadcrumb click event listener
        document.removeEventListener('click', this.breadcrumbClickHandler);
    }
}
