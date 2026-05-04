import { DatePipe } from "@angular/common";
import { Component, ElementRef, EventEmitter, HostListener, Input, Output, ViewChild } from "@angular/core";
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { BillingFeatures, ClaimFilingIndicatorModel } from "../../../../core/models/billing/claim-filingIndicator-model";
import { filterBy, CompositeFilterDescriptor, State } from '@progress/kendo-data-query';
import { ComboBoxComponent } from '@progress/kendo-angular-dropdowns';

@Component({
  selector: 'app-funder-settings-save',
  templateUrl: './funder-settings-save.component.html',
  styleUrls: ['./funder-settings-save.component.css'],
  providers: [DatePipe]
})
export class FunderSettingsSaveComponent {
  @Input() opened: boolean = false;
  @Input() userList: ClaimFilterOptionModel[] = [];
  @Input() claimFilingIndicators: ClaimFilingIndicatorModel[] = [];
  @Input() billingFeatures: BillingFeatures[] = [];
  @Input() isDisabled: boolean = false;

  @ViewChild('popup', { read: ElementRef }) public popup: ElementRef | undefined;
  @Output() filterChanged = new EventEmitter<void>();
  @Output() clearFilter = new EventEmitter<void>();


  @ViewChild('FunderAnchor', { read: ElementRef }) funderAnchor!: ElementRef;
  @ViewChild('CFIAnchor', { read: ElementRef }) cfiAnchor!: ElementRef;

  @ViewChild('funderCombo') funderCombo!: ComboBoxComponent;
  @ViewChild('cfiCombo') cfiCombo!: ComboBoxComponent;

  funderPopup: any = { appendTo: 'component', width: 500 }; // default fallback
  cfiPopup: any = { appendTo: 'component', width: 500 };    // default fallback
  funderComboOn = false;
  cfiComboOn = false;


  // NOTE: for single-select funder, emit a single id (number), not an array
  @Output() requestData = new EventEmitter<{
    funderId: number | null;
    funderName: string | null;
    claimFilingIndicatorId?: number | null;
    includeTaxonomyCode?: boolean;
    features: BillingFeatures[] | [];
  }>();

  isFiltersApplied = false;

  private funderInitialized = false;
  private cfiInitialized = false;

  selectedFilterId: number | undefined = undefined;
  selectedAnchor: any;

  showDatePopup = false;

  // SINGLE SELECT funder
  selectedFunderId: number | null = null;

  // CFI
  selectedCFIId: number | null = null;
  selectedCFI: ClaimFilingIndicatorModel | undefined;

  includeTaxonomyCode = false;
  billingSchedule = false;

  filterdate: Date | undefined;
  filterdateString: string | undefined;
  filteredUserList = [];
  filteredCFIList = [];
  initialCFIId: number | null = null;


  constructor(private datePipe: DatePipe) {
   }

  @HostListener('document:click', ['$event'])
  public documentClick(event: any): void {
    if (!this.selectedAnchor) return;
    if (!this.contains(event.target)) {
      if (this.selectedFilterId != undefined) this.selectFilter(undefined);
    }
  }
public state: State = {
    skip: 0,
    take: 20,

    sort: [
      {
        field: 'changeDate',
        dir: 'desc',
      },
    ],

    filter: {
      logic: 'and',
      filters: [],
    },
  };
  private contains(target: HTMLElement): boolean {
    return (this.selectedAnchor && this.selectedAnchor.contains(target)) ||
      (this.popup ? this.popup.nativeElement.contains(target) : false);
  }

  ngOnChanges() {
    this.initializeFilters();

    if (this.userList?.length) {
      this.filteredUserList = [...this.userList];
    }
    if (this.claimFilingIndicators?.length) {
      this.filteredCFIList = [...this.claimFilingIndicators];
    }

    // Set the Billing Schedule Feature flag value
    if (this.billingFeatures?.length) {
      this.billingSchedule = this.billingFeatures?.any(x => x.featureName === "BillingSchedule" && x.isEnabled === true)
    }
  }

  private initializeFilters() {
    // Initialize funder once (default to first if you want)
    if (!this.funderInitialized && this.userList?.length > 0) {
      // pick first funder by default (optional)
      /*this.selectedFunderId = this.selectedFunderId ?? this.userList[0]?.id ?? null;*/
      this.selectedFunderId = null;
      this.funderInitialized = true;
    }

    // Initialize CFI once (optional: select first by default)
    if (!this.cfiInitialized && this.claimFilingIndicators?.length > 0) {
      this.selectedCFIId = this.selectedCFIId ?? this.claimFilingIndicators[0]?.id ?? null;
      this.initialCFIId = this.selectedCFIId;
      this.selectedCFI = this.claimFilingIndicators.find(x => x.id === this.selectedCFIId!);
      this.cfiInitialized = true;
    }

    // Initialize date once (defaults to today)
    if (this.filterdate == undefined) {
      const now = new Date();
      now.setMinutes(0, 0, 0);
      this.filterdate = now;
      this.filterdateString = this.datePipe.transform(now, 'MM/dd/yy') ?? undefined;
    }
  }

  selectFilter(filterId: number | undefined, anchor: any = undefined) {
    if (
      (filterId === 0 && !this.funderComboOn) ||
      (filterId === 1 && !this.cfiComboOn)
    ) {
      this.selectedFilterId = filterId;
      this.selectedAnchor = anchor;
    } else {
      this.selectedFilterId = undefined;
      this.selectedAnchor = anchor;
      if (filterId === 0) {
        this.funderComboOn = false;
      }
      else { this.cfiComboOn = false; }
    }

    setTimeout(() => {
      if (filterId === 0 && this.funderCombo && !this.funderComboOn) {
        this.funderCombo.toggle(true);
        this.funderComboOn=true
      }

      if (filterId === 1 && this.cfiCombo && !this.cfiComboOn) {
        this.cfiCombo.toggle(true);
        this.cfiComboOn = true;
      }
    });
  }

  clearFilters() {
    this.selectedFunderId = null;
    this.selectedCFIId = this.initialCFIId;
    this.selectedCFI = this.claimFilingIndicators.find(x => x.id === this.selectedCFIId!);
    this.includeTaxonomyCode = undefined;

    this.filterChanged.emit();
    this.clearFilter.emit();
    this.isFiltersApplied = false;
  }

  OnInitClearFilters() {
    this.selectedFunderId = null;
    this.filterdate = undefined;
    this.filterdateString = undefined;
  }

  private handleOutsideClickLogic(eventTarget: any): void {
    if (!this.selectedAnchor) return;

    if (!this.contains(eventTarget)) {
      if (this.selectedFilterId != undefined) {
        this.selectFilter(undefined);
      }
    }
  }

  onFilterChange() {
    this.filterChanged.emit();
    this.isFiltersApplied = false;
    this.handleOutsideClickLogic(document.body);
  }

  applyFilters() {
    if (!this.selectedFunderId) { return; } 
    const data = {
      funderId: this.selectedFunderId,
      funderName: this.getFunderName(this.selectedFunderId),
      claimFilingIndicatorId: this.selectedCFIId,
      includeTaxonomyCode: this.includeTaxonomyCode,
      features: this.billingFeatures
    };
    this.isFiltersApplied = true;
    this.selectedFunderId = null;
    this.includeTaxonomyCode = false;
    
    this.selectedCFIId = this.initialCFIId;
    this.selectedCFI = this.claimFilingIndicators.find(x => x.id === this.selectedCFIId!);
    //this.clearFilters();
    this.requestData.emit(data);
  }

  setDatePeriod(date: Date): void {
    this.filterdateString = this.datePipe.transform(date, 'MM/dd/yy') ?? undefined;
    this.filterdate = new Date(date);
    this.filterdate.setMinutes(0, 0, 0);
    this.filterChanged.emit();
    this.showDatePopup = false;
    this.isFiltersApplied = false;
  }

  onDateFilterLeave() {
    this.showDatePopup = false;
  }

  onCFIChange(id: number) {
    this.selectedCFIId = id;
    this.selectedCFI = this.claimFilingIndicators.find(x => x.id === id);
    this.onFilterChange();
  }

  onIncludeTaxonomyToggle(value: boolean) {
    this.includeTaxonomyCode = value;
    this.onFilterChange();
  }

  onBillingScheduleToggle(value: boolean) {
    this.billingFeatures = this.billingFeatures.map(f =>
      f.featureName === "BillingSchedule"
        ? { ...f, isEnabled: value }
        : f
    );
    this.onFilterChange();
  }

  getFunderName(id: number | null): string {
    if (id == null) return '';
    const f = this.userList.find(u => u.id === id);
    return f?.name ?? '';
  }

  // event from funder dropdown (single select)
  onFunderChange(id: number) {
    this.selectedFunderId = id;
    this.onFilterChange();
  }

  handleFunderFilter(query: string) {
    if (!query) {
      this.filteredUserList = [...this.userList];   // FULL LIST
      return;
    }

    this.filteredUserList = this.userList.filter(x =>
      x.name.toLowerCase().includes(query.toLowerCase())
    );
  }

  handleCFIFilter(query: string) {
    if (!query) {
      this.filteredCFIList = [...this.claimFilingIndicators]; // FULL LIST
      return;
    }

    this.filteredCFIList = this.claimFilingIndicators.filter(x =>
      x.indicator.toLowerCase().includes(query.toLowerCase())
    );
  }

  isSaveDisabled(): boolean {
    return !this.selectedFunderId || !this.selectedCFIId;
  }

}
