import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { ReportService } from '@core/services/billing/report.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'funder-filter-popup',
  templateUrl: './funder-filter-popup.component.html',
  styleUrls: ['./funder-filter-popup.component.css'],
})
export class FunderFilterPopupComponent {
  @Output() funderClicked = new EventEmitter<number>();
  @Input() selectedfunders: ClaimFilterOptionModel[] = [];
  @Input() funderList: ClaimFilterOptionModel[] = [];
  @Input() callingFrom: string;

  funders: ClaimFilterOptionModel[] = [];
  userList: ClaimFilterOptionModel[] = [];
  isLoading: boolean = false;
  searchTimeout: any;
  isAllSelect: boolean = false;

  private unsubscribeAll$ = new Subject();

  constructor(private reportingService: ReportService,
    private accountService: AccountMemberService,
  ) {}

  ngOnInit(): void {
    // Only load funders once on init
    if (this.userList.length === 0) {
      this.isLoading = true;
      const funderObservable =
        this.callingFrom === 'clientHistory'
          ? this.reportingService.getFunderListByIds()
          : this.reportingService.getPrimaryFunderListByIds(this.accountService.memberDetails.clientId);

      funderObservable
        .pipe(takeUntil(this.unsubscribeAll$))
        .subscribe((data) => {
          this.userList = data;
          this.isLoading = false;
          this.setUserList('');
        });
    } else {
      this.setUserList('');
    }
  }

  fundersSearchValueChanged(event: any) {
    const searchValue = event.target.value;
    if (this.searchTimeout) clearTimeout(this.searchTimeout);

    this.searchTimeout = setTimeout(() => {
      this.setUserList(searchValue);
    }, 500); 
  }

  setUserList(searchValue: string) {
   let filteredList: ClaimFilterOptionModel[] = [];

    if (searchValue && searchValue.trim()) {
      const search = searchValue.trim().toLowerCase();
      filteredList = this.userList.filter(
        (f) => f.name && f.name.toLowerCase().includes(search)
      );
    } else {
      filteredList = [...this.userList];
    }

    // Mark checked funders
    filteredList = filteredList.map((f) => ({
      ...f,
      checked: this.selectedfunders.some((sel) => sel.id === f.id),
    }));

    // Sort selected funders to top
    filteredList.sort((a, b) => Number(b.checked) - Number(a.checked));

    this.funders = filteredList;
    // isAllSelect should check if ALL funders in the entire list are selected, not just filtered
    this.isAllSelect = this.userList.length > 0 && this.userList.every((f) =>
      this.selectedfunders.some((sel) => sel.id === f.id)
    );
  }

onFunderClicked(funder: ClaimFilterOptionModel) {
  const index = this.selectedfunders.findIndex(x => x.id === funder.id);
  if (index > -1) {
    this.selectedfunders.splice(index, 1);
    funder.checked = false;
  } else {
    this.selectedfunders.push(funder);
    funder.checked = true;
  }

  // Recalculate "Select All" checkbox state
  this.isAllSelect = this.funders.length > 0 && this.funders.every(f => 
    this.selectedfunders.some(sel => sel.id === f.id)
  );

  this.funderClicked.emit();
}

selectAll(funders: ClaimFilterOptionModel[]): void {
  // If "Select All" checkbox is unchecked, select all funders
  if (!this.isAllSelect) {
    funders.forEach(funder => {
      if (!this.selectedfunders.some(sel => sel.id === funder.id)) {
        this.selectedfunders.push(funder);
        funder.checked = true;
      }
    });
  } else {
    // If "Select All" checkbox is checked, unselect all funders
    funders.forEach(funder => {
      const index = this.selectedfunders.findIndex(x => x.id === funder.id);
      if (index > -1) {
        this.selectedfunders.splice(index, 1);
        funder.checked = false;
      }
    });
  }

  // Update "Select All" checkbox state
  this.isAllSelect = funders.length > 0 && funders.every(f => 
    this.selectedfunders.some(sel => sel.id === f.id)
  );

  this.funderClicked.emit();
}

openPopup() {
    this.userList.forEach((funder) => {
      funder.checked = this.selectedfunders.some((sel) => sel.id === funder.id);
    });
    this.isAllSelect = this.userList.length > 0 && this.userList.every((f) => f.checked);
  }
}
