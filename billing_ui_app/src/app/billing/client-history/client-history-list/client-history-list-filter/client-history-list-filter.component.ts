import { AfterViewInit, ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, OnDestroy, OnInit, Output, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';

import { ClientHistoryService } from '@core/services/billing/client-history.service';
import { Input } from '@angular/core';
import { forkJoin, Subject, takeUntil } from 'rxjs';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ReportService } from '@core/services/billing/report.service';
import { ClaimService } from '@core/services/billing/claim.service';
import { ClaimListingTab } from "@core/enums/billing/claim-listing-tab";
import { AccountMemberService } from '@core/services/account/account-member.service';
import { MatDatepicker } from '@angular/material/datepicker';
import { ClaimsManagementFilterService } from '@app/billing/services/claims-management-filter.service';
import { LoaderService } from '@core/services/common/loader.service';

@Component({
  selector: 'app-client-history-list-filter',
  templateUrl: './client-history-list-filter.component.html',
  styleUrls: ['./client-history-list-filter.component.css']
})
export class ClientHistoryListFilterComponent implements OnDestroy {
  selectedFilterId: number | undefined = undefined;
  @Output() filterChanged = new EventEmitter();
  opened: boolean = false;
  selectedLocation: ClaimFilterOptionModel[] = [];
  selectedPatients: ClaimFilterOptionModel[] = [];
  selectedfunders: ClaimFilterOptionModel[] = [];
  selectedClientId: ClaimFilterOptionModel[] = [];
  locationList: ClaimFilterOptionModel[] = [];
  selectedDOB: Date ;
  showClearFilter = false;
  dob: Date | null = null;
  funderss: ClaimFilterOptionModel[] = [];
  isAllLocationSelected: boolean = false;
  isAllClientSelected: boolean = false;
  isAllFunderSelected: boolean = false;
  @ViewChild('dateAnchorEl', { read: ElementRef }) public dateAnchor: ElementRef;
  isClearFilterDisabled: boolean = true;
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
  dobFilter: Date;
  private unsubscribeAll$ = new Subject();
  isFiltersApplied: boolean = false;
  isFilterButtonDisabled: boolean = true;
  @Input() filterForm: FormGroup;
  @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;
  locations: ClaimFilterOptionModel[] = [];
  clients: ClaimFilterOptionModel[] = [];
  clientNames: string[] = [];
     @Input() tab: ClaimListingTab | null;
  funders: string[] = [];
  clientIds: number[] = [];
  showDatePopup: boolean;
  dateFromString: string | undefined;
  dateToString: string | undefined;
  userList: ClaimFilterOptionModel[] = [];
  funderList: ClaimFilterOptionModel[] = [];
  dateOfBirth: Date | null = null;
  searchTimeout: any;
  selectedAnchor: any;
  isLoading: boolean;
  isAllSelect: boolean;
  constructor(
    private fb: FormBuilder,
    private clientHistoryService: ClientHistoryService,
    private reportingService: ReportService,
    private claimsService: ClaimService,
    private accountService: AccountMemberService,
    private loaderService: LoaderService,
    private claimsManagementFilterService:ClaimsManagementFilterService,
    private cdr: ChangeDetectorRef
  ) {}
  private subscriptions = [];

  ngOnDestroy(): void {
    this.subscriptions.forEach(sub => sub.unsubscribe());
  }
  

onDateFilterLeave() {
    this.showDatePopup = false;
  }
openDatePicker(picker: MatDatepicker<Date>) {
  this.selectFilter(5); 
  picker.open();
}

ngOnInit(): void {
    this.filterForm = this.fb.group({
      location: [''],
      clientId: [''],
      dateOfBirth: ['']
    });
    this.loaderService.show();
    
    forkJoin({
          clients: this.reportingService.getClientListByIds(),
          funders: this.reportingService.getFunderListByIds(),
          locations: this.reportingService.getLocationListByIds(),
          
        }).subscribe(results => {
          this.userList = results.clients.map(patient => ({ ...patient, checked: true }));
          this.selectedPatients = [...this.userList];
    
          this.funderList = results.funders.map(funder => ({ ...funder, checked: true }));
          this.selectedfunders = [...this.funderList];
    
          this.locationList = results.locations.map(location => ({ ...location, checked: true }));
          this.selectedLocation = [...this.locationList];
    
         
    
          this.loaderService.hide(); 
        });
  }

  

  onFilterChange() {
  this.dateOfBirth = this.filterForm.get('dateOfBirth')?.value;

  this.isAllLocationSelected = this.locationList.length > 0 && 
    this.selectedLocation.length === this.locationList.length;

  this.isAllClientSelected = this.userList.length > 0 && 
    this.selectedPatients.length === this.userList.length;

  this.isAllFunderSelected = this.funderList.length > 0 && 
    this.selectedfunders.length === this.funderList.length;

  this.isFiltersApplied = true;
  this.showClearFilter = false;
  this.isClearFilterDisabled = false;
  this.checkFiltersForApply();
  
  // change detection
  this.cdr.detectChanges();
}

  

  loadClientNameOptions() {
    this.clientHistoryService.GetClientName().subscribe((clientNames) => {
      this.clientNames = clientNames;
    });
  }

  loadFunderOptions() {
    this.clientHistoryService.GetFunder().subscribe((funders) => {
      this.funders = funders;
    });
  }

  loadClientIdOptions() {
    this.clientHistoryService.GetClientId().subscribe((clientIds) => {
      this.clientIds = clientIds;
    });
  }

  locationToggle(event: any) {
    this.selectedFilterId = 1;
   
  }

  clientNameToggle(event: any) {
    this.selectedFilterId = 2;
   
  }

  funderToggle(event: any) {
    this.selectedFilterId = 3;
   
  }

  clientIdToggle(event: any) {
    this.selectedFilterId = 4;
    
  }

  selectFilter(filterId: number | undefined, anchor: any = undefined) {
   this.selectedAnchor = anchor;
    if (filterId == this.selectedFilterId) {
            this.selectedFilterId = undefined;        
        } else {
            this.selectedFilterId = filterId;
        }        
       this.selectedAnchor = anchor;
      this.showDatePopup = true;
    
  }
  
  private contains(target: HTMLElement): boolean {
        return this.selectedAnchor.contains(target) ||
            (this.popup ? this.popup.nativeElement.contains(target) : false);
    }
  searchLocations(locationName: string) {
    
      if (this.searchTimeout)
        clearTimeout(this.searchTimeout)
  
      if (this.userList.length == 0) {
        
        this.searchTimeout = setTimeout(() => {
          this.isLoading = true;
          this.reportingService.getLocationListByIds()
            .pipe(takeUntil(this.unsubscribeAll$))
            .subscribe(x => {
              this.userList = x;
              this.setUserList(locationName);
              this.isLoading = false;
            });
        }, 1000);
      } else {
        this.setUserList(locationName);
      }
    }

    setUserList(locationName: string) {
 
    let filteredList = [];
      this.locations = this.selectedLocation; 
      if (locationName != "") 
        filteredList = this.userList.where(x => x.name != null && (x.name.toLowerCase().includes(locationName.toLowerCase()) || x.checked));

      else {
        filteredList = this.userList;
      }
      this.locations = this.locations.concat(filteredList.where((p: ClaimFilterOptionModel) =>
        !this.selectedLocation.any((s: ClaimFilterOptionModel) => s.id == p.id)))
      this.isAllSelect = this.locations.length > 0 && this.locations.every(f=>f.checked);
  }

  searchPatients(patientName: string) {
    if (this.searchTimeout)
      clearTimeout(this.searchTimeout)

    if (this.userList.length == 0) {
      this.searchTimeout = setTimeout(() => {
        this.isLoading = true;
        this.reportingService.getClientListByIds()
          .pipe(takeUntil(this.unsubscribeAll$))
          .subscribe(x => {
            this.userList = x;
            this.setUserList(patientName);
            this.isLoading = false;
          });
      }, 1000);
    } else {
      this.setUserList(patientName);
    }
  }

  searchClientByID():void {
    this.reportingService.getClientListByIds()
      .subscribe(data => {
        this.clients = data;  
       });
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


  applyFilters() {
      this.showClearFilter = true;
      this.isClearFilterDisabled = false;
      this.filterChanged.emit();
  
}


  clearFilters(isEventEmitted: boolean = true) {
    //this.filterForm.reset();
    this.selectedPatients = [];
    this.selectedLocation = [];
    this.selectedfunders = [];
    this.selectedClientId = [];
    this.dobFilter = null;
    if (this.filterForm) {
      this.filterForm.patchValue({ dateOfBirth: null });
    }
    this.claimsManagementFilterService.isFilterSet = false;
    if (isEventEmitted) this.filterChanged.emit();
    this.showClearFilter = false;
    this.isFilterButtonDisabled = true;
    this.isFiltersApplied = false;
  }

  checkFiltersForApply() {
      this.isFiltersApplied = (
      this.selectedPatients.length > 0 ||
      this.selectedfunders.length > 0 ||
      this.selectedLocation.length > 0 ||
      (!!this.dateOfBirth)
    );
  }

}
