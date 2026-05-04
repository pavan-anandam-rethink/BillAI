import { Component, ElementRef, EventEmitter, HostListener, Input, OnInit, Output, ViewChild } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';

@Component({
  selector: 'payment-details-filter',
  templateUrl: './payment-details-filter.component.html',
  styleUrls: ['./payment-details-filter.component.css']
})
export class PaymentDetailsFilterComponent implements OnInit {
  @Input() opened: boolean = true;
  @Input() appInfo: any[] = [];
  @Input() paymentId: number;
  @Output() filterChanged = new EventEmitter();
  @Output() filtersApplied = new EventEmitter<any>();
  @ViewChild('popup', { read: ElementRef }) public popup: ElementRef;

  selectedFilterId: number | undefined = undefined;
  selectedPatients: ClaimFilterOptionModel[] = [];
  paymentFrom?: number;
  paymentTo?: number;
  balanceFrom?: number;
  balanceTo?: number;
  selectedAnchor: any;
  showClearFilter = false;
  isClearFilterDisabled = true;
  paidStatus: 'paid' | 'unpaid' | 'both' = 'both';
  claimId: string = '';

  @HostListener('document:click', ['$event'])
  public documentClick(event: any): void {
    if (!this.selectedAnchor) return;
    if (!this.contains(event.target)) {
      if (this.selectedFilterId != undefined) this.selectFilter(undefined);
    }
  }

  private contains(target: HTMLElement): boolean {
    return this.selectedAnchor?.contains(target) ||
      (this.popup ? this.popup.nativeElement.contains(target) : false);
  }

  applyFilters() {
    this.showClearFilter = true;
    this.filtersApplied.emit({
      claimId: this.claimId,
      selectedPatients: this.selectedPatients.map(p => ({ ...p, id: p.id })),
      paymentFrom: this.paymentFrom,
      paymentTo: this.paymentTo,
      balanceFrom: this.balanceFrom,
      balanceTo: this.balanceTo,
      paidStatus: this.paidStatus === 'both' ? undefined : this.paidStatus
    });
    this.filterChanged.emit();
  }

  clearFilters() {
    this.selectedFilterId = undefined;
    this.selectedPatients = [];
    this.claimId = '';
    this.paymentFrom = undefined;
    this.paymentTo = undefined;
    this.balanceFrom = undefined;
    this.balanceTo = undefined;
    this.paidStatus = undefined;
    this.showClearFilter = false;
    this.onFilterChange();
    this.filtersApplied.emit({
      claimId: '',
      selectedPatients: [],
      paymentFrom: undefined,
      paymentTo: undefined,
      balanceFrom: undefined,
      balanceTo: undefined,
      paidStatus: undefined
    });
  }

  selectFilter(filterId: number | undefined, anchor: any = undefined) {
    this.selectedFilterId = filterId;
    this.selectedAnchor = anchor;
  }

  onFilterChange() {
    // Always show "Apply Filters" and enable the button when any filter changes
    this.showClearFilter = false;
    this.isClearFilterDisabled = !this.canApplyFilters();
  }

  onPaidStatusChange(status: 'paid' | 'unpaid' | 'both') {
    this.paidStatus = status;
    this.onFilterChange();
  }

  canApplyFilters(): boolean {
    return (
      this.selectedPatients.length > 0 ||
      this.paymentFrom != null ||
      this.paymentTo != null ||
      this.balanceFrom != null ||
      this.balanceTo != null ||
      this.paidStatus != null ||
      (this.claimId && this.claimId.trim().length > 0)
    );
  }

  ngOnInit(): void {
    this.showClearFilter = true; // Show "Clear Filter" button on load
    this.filtersApplied.emit({
      claimId: this.claimId,
      selectedPatients: this.selectedPatients.map(p => ({ ...p, id: p.id })),
      paymentFrom: this.paymentFrom,
      paymentTo: this.paymentTo,
      balanceFrom: this.balanceFrom,
      balanceTo: this.balanceTo,
      paidStatus: this.paidStatus === 'both' ? undefined : this.paidStatus
    });
  }
}
