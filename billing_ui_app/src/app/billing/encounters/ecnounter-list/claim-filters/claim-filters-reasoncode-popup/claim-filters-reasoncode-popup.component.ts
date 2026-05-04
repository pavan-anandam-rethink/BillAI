import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from "@angular/core";
import { Subject } from "rxjs";
import { ClaimService } from "@core/services/billing";
import { CarcCodes } from "../../../../../core/models/billing/carc-codes";

@Component({
  selector: 'claim-filters-reasoncode-popup',
  templateUrl: './claim-filters-reasoncode-popup.component.html',
  styleUrls: ['./claim-filters-reasoncode-popup.component.css'],
})
export class ClaimFiltersReasonCodePopupComponent implements OnInit, OnDestroy {
  @Output() reasonCodeClicked = new EventEmitter<CarcCodes[]>();
  @Input() selectedReasonCodes: CarcCodes[] = [];
  private unsubscribeAll$ = new Subject<void>();

  allReasonCodes: CarcCodes[] = [];  
  filteredReasonCodes: CarcCodes[] = [];  
  isLoading = false;
  searchTimeout: any;

  constructor(private claimsService: ClaimService) { }

  ngOnInit(): void {
    this.claimsService.getCarcCode().subscribe(carcCode => {
      if (carcCode !== null) {
        this.allReasonCodes = carcCode;
        this.allReasonCodes.forEach(code => {
          code.checked = this.selectedReasonCodes.some(selected => selected.code === code.code);
        });
        this.filterReasonCodes('');
      }
    });
  }

  reasonCodesSearchValueChanged(event: any) {
    const searchValue = event.target.value;
    if (this.searchTimeout) clearTimeout(this.searchTimeout);
    this.searchTimeout = setTimeout(() => {
      this.filterReasonCodes(searchValue);
    }, 300);  // debounce 300ms
  }

  filterReasonCodes(searchValue: string) {
    const searchLower = searchValue.toUpperCase();

    this.filteredReasonCodes = [
      ...this.selectedReasonCodes,
      ...this.allReasonCodes.filter(code =>
        code.code.includes(searchLower) &&
        !this.selectedReasonCodes.some(selected => selected.code === code.code))
    ];
  }

  onReasonCodeClicked(reasonCode: CarcCodes) {
    if (reasonCode.checked) {
      const index = this.selectedReasonCodes.findIndex(rc => rc.code === reasonCode.code);
      if (index !== -1) {
        this.selectedReasonCodes.splice(index, 1);
      }
      reasonCode.checked = false;
    } else {
      this.selectedReasonCodes.push(reasonCode);
      reasonCode.checked = true;
    }

    this.reasonCodeClicked.emit();
  }

  toggleReasonCode(reasonCode: CarcCodes) {
    reasonCode.checked = !reasonCode.checked;
  }

  ngOnDestroy(): void {
    if (this.searchTimeout)
      clearTimeout(this.searchTimeout);
    this.unsubscribeAll$.next();
    this.unsubscribeAll$.complete();
  }
}

