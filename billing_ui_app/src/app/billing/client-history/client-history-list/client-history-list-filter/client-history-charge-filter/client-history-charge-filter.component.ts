import { AfterViewInit, Component, ElementRef, EventEmitter, HostListener, OnDestroy, OnInit, Output, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ClientHistoryService } from '@core/services/billing/client-history.service';
import { Input } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ClientsService } from '@core/services/clients/clients.service';
import { BasicOption } from '@core/models/common/basic-option';
import { forkJoin, Subject, takeUntil } from 'rxjs';
import { Router } from '@angular/router';
import { ClaimDetailsInfoModel } from '@core/models/billing/claim-details-model';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { ClaimService } from '@core/services/billing';
import { ClaimListingTab } from "@core/enums/billing/claim-listing-tab";
import { FunderServiceLine } from '@core/models/company-account/funders/client-funder-model';
import { ReportService } from '@core/services/billing/report.service';
import { DatePipe } from '@angular/common';
import { ClaimsManagementFilterService } from '@app/billing/services/claims-management-filter.service';
import { LoaderService } from '@core/services/common/loader.service';

@Component({
  selector: 'app-client-history-charge-filter',
  templateUrl: './client-history-charge-filter.component.html',
  styleUrls: ['./client-history-charge-filter.component.css']
})
export class ClientHistoryChargeFilterComponent implements OnDestroy, AfterViewInit {
  selectedFilterId: number | undefined = undefined;
  opened: boolean = false;
  selectedDosFrom: Date | undefined;  
  @Output() filterChanged = new EventEmitter();
  
  selectedDos: Date | undefined;
  selectedPlaceOfService: ClaimFilterOptionModel[] = []
  selectedFunders: ClaimFilterOptionModel[] = []
  selectedRenderingProviders: ClaimFilterOptionModel[] = []
  @Input() tab: ClaimListingTab | null;
  selectedAuthNumber:ClaimFilterOptionModel[] = []
  isFiltersApplied: boolean = false;
  @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;
  private unsubscribeAll$ = new Subject<void>();
  @ViewChild('dateAnchorEl', { read: ElementRef }) public DateOfServiceAnchor: ElementRef;
  showDatePopup = false;
  isAllSelect: boolean = false;
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
  isFilterButtonDisabled: boolean = true;
  renderingProviders: ClaimFilterOptionModel[] = [];
  selectedfunders: ClaimFilterOptionModel[] = []
  userList: ClaimFilterOptionModel[] = [];
  showClearFilter = false;
  
  @Input() claim: ClaimDetailsInfoModel;
  placeOfServices: ClaimFilterOptionModel[] = [];
  dateFromString: string | undefined;
  dateToString: string | undefined;
  @Input() filterForm: FormGroup;
   funderss: ClaimFilterOptionModel[] = [];
  placesOfService: string[] = [];
  funders: string[] = [];
  authNumbers: number[] = [];
  clientId: number;
  funderId: number;
  selectedAnchor: any;
  isClearFilterDisabled = true;
  authorizations: BasicOption[];
  searchTimeout: any;
  isLoading: boolean;
  dateFrom:Date ;
  dateTo:Date ;
  stepFormGroup: FormGroup;
  serviceLines: FunderServiceLine[];
  funderList: ClaimFilterOptionModel[] = [];
  placeOfServiceList: ClaimFilterOptionModel[] = [];
  authorizationList: ClaimFilterOptionModel[] = [];
  isAllPlaceOfServiceSelected: boolean = false;
  isAllRenderingProviderSelected: boolean = false;
  isAllAuthNumberSelected: boolean = false;
  isAllFunderSelected: boolean = false;

  private unsubscribe = new Subject<void>();
  constructor(
    private fb: FormBuilder,
    private clientHistoryService: ClientHistoryService,
    private clientService: ClientsService,
    private readonly router: Router,
    private accountService: AccountMemberService,
    private claimsService: ClaimService,
    private reportingService: ReportService,
    private datePipe: DatePipe,
    private claimsManagementFilterService:ClaimsManagementFilterService,
    private loaderService: LoaderService
    
  ) {}

  private subscriptions = [];

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
    this.unsubscribe.next();
    this.unsubscribe.complete();
  }

  ngAfterViewInit(): void {
    
    const sub = this.filterForm.valueChanges.subscribe(values => {
      this.selectedDosFrom = values.dosFrom;
      this.selectedDos = values.dosTo;
     });
    this.subscriptions.push(sub);
  }

  ngOnInit(): void {
    
    this.filterForm = this.fb.group({
      dosFrom: [''],
      dosTo: [''],
      placeOfService: [''],
      renderingProvider: [''],
      authNumber: [''],
      funder: ['']
    });

    const today = new Date();
        const fromDate = new Date();
        fromDate.setDate(today.getDate() - 90);
    
        this.dateFrom = fromDate;
        this.dateTo = today;
        this.dateFromString = this.datePipe.transform(this.dateFrom, 'MM/dd/yy');
        this.dateToString = this.datePipe.transform(this.dateTo, 'MM/dd/yy');
        this.loaderService.show();
    
        forkJoin({
              pos: this.reportingService.getPoSListByIds(),
              renderingprovider: this.claimsService.getRenderingProviders(),
              authorization: this.reportingService.getAllAuthorizationNumbers(),
              funders: this.reportingService.getPrimaryFunderListByIds(this.accountService.memberDetails.clientId),
              
            }).subscribe(results => {
              this.placeOfServiceList = results.pos.map(pos => ({ ...pos, checked: true }));
              this.selectedPlaceOfService = [...this.placeOfServiceList];
              this.isAllPlaceOfServiceSelected = this.placeOfServiceList.length > 0;

              this.funderList = results.funders.map(funder => ({ ...funder, checked: true }));
              this.selectedFunders = [...this.funderList];
              this.isAllFunderSelected = this.funderList.length > 0;

              this.userList = results.renderingprovider.map(provider => ({ ...provider, checked: true }));
              this.selectedRenderingProviders = [...this.userList];
              this.isAllRenderingProviderSelected = this.userList.length > 0;
              
              this.authorizationList = results.authorization.map(auth => ({ ...auth, checked: true }));
              this.selectedAuthNumber = [...this.authorizationList];
              this.isAllAuthNumberSelected = this.authorizationList.length > 0;
    
              this.loaderService.hide();
            });

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
  onFilterChange() {
    const selectedPosIds = new Set(this.selectedPlaceOfService.map(x => x.id));
    this.isAllPlaceOfServiceSelected = this.placeOfServiceList.length > 0 && 
      this.placeOfServiceList.every(pos => selectedPosIds.has(pos.id));

    const selectedProviderIds = new Set(this.selectedRenderingProviders.map(x => x.id));
    this.isAllRenderingProviderSelected = this.userList.length > 0 && 
      this.userList.every(provider => selectedProviderIds.has(provider.id));

    const selectedAuthIds = new Set(this.selectedAuthNumber.map(x => x.id));
    this.isAllAuthNumberSelected = this.authorizationList.length > 0 && 
      this.authorizationList.every(auth => selectedAuthIds.has(auth.id));

    const selectedFunderIds = new Set(this.selectedFunders.map(x => x.id));
    this.isAllFunderSelected = this.funderList.length > 0 && 
      this.funderList.every(funder => selectedFunderIds.has(funder.id));

    this.isFiltersApplied = true;
    this.showClearFilter = false;
    this.isClearFilterDisabled = false;
    this.checkFiltersForApply();
   }
     
  private contains(target: HTMLElement): boolean {
        return this.selectedAnchor.contains(target) ||
        (this.popup ? this.popup.nativeElement.contains(target) : false);
    }
  
searchFunders(funderName: string) {
  if (this.searchTimeout)
            clearTimeout(this.searchTimeout)

        this.searchTimeout = setTimeout(() => {
            this.isLoading = true;

            this.claimsService.getClaimFunders({ Tab: this.tab, SearchValue: funderName, AccountInfoId: this.accountService.memberDetails.accountInfoId })
                .pipe(takeUntil(this.unsubscribeAll$))
                .subscribe(x => {
                    this.funderss = this.selectedfunders;
                    this.funderss = this.funderss.concat(x.where((p: ClaimFilterOptionModel) =>
                        !this.selectedfunders.any((s: ClaimFilterOptionModel) => s.id == p.id)))

                    this.isLoading = false;
                });
        }, 1000);
    }

checkFiltersForApply() {
    this.isFiltersApplied = (
      this.selectedPlaceOfService.length > 0 ||
      this.selectedFunders.length > 0 ||
      this.selectedRenderingProviders.length > 0 ||
      this.selectedAuthNumber.length > 0 ||
      (!!this.dateFromString && !!this.dateToString)
    );
  }

  applyFilters() {
    this.showClearFilter = true;
    this.isClearFilterDisabled = false;
    this.filterChanged.emit();
  }

  clearFilters(isEventEmitted: boolean = true) { 
  this.selectedAuthNumber = [];
  this.selectedRenderingProviders = [];
  this.selectedPlaceOfService = [];
  this.selectedfunders = [];
  this.dateFromString = null;
  this.dateToString = null;
    this.selectedFunders = [];
    this.claimsManagementFilterService.isFilterSet = false;
    if (isEventEmitted) this.filterChanged.emit();
    this.showClearFilter = false;
    this.isFilterButtonDisabled = true;
    this.isFiltersApplied = false;
  }

  selectFilter(filterId: number | undefined, anchor: any = undefined) {
    if (filterId == this.selectedFilterId) {
            this.selectedFilterId = undefined;        
        } else {
            this.selectedFilterId = filterId;
        }        
        this.selectedAnchor = anchor;
        if(filterId == 4)
            if(this.selectedDosFrom == undefined || this.selectedDos == undefined)
                {
                  this.selectedDosFrom = new Date();
                  this.selectedDosFrom.setHours(0);
                  this.selectedDosFrom.setMinutes(0);
                  this.selectedDosFrom.setSeconds(0);
                  this.selectedDosFrom.setMilliseconds(0);

                  this.selectedDos = new Date();
                  this.selectedDos.setHours(0);
                  this.selectedDos.setMinutes(0);
                  this.selectedDos.setSeconds(0);
                  this.selectedDos.setMilliseconds(0);

                }
            this.showDatePopup = true;
    
  }

}
