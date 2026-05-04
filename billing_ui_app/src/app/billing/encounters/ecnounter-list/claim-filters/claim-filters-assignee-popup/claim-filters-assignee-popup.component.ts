import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ClaimListingTab } from '@core/enums/billing/claim-listing-tab';
import { Subject, takeUntil } from 'rxjs';
import { ClaimService } from '@core/services/billing';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { AssigneeModel } from '@core/models/billing/assignee-model';

@Component({
  selector: 'claim-filters-assignee-popup',
  templateUrl: './claim-filters-assignee-popup.component.html',
  styleUrls: ['./claim-filters-assignee-popup.component.css']
})
export class ClaimFiltersAssigneePopupComponent {
  @Output() assigneeClicked = new EventEmitter<void>();
  @Input() selectedAssignees: ClaimFilterOptionModel[];
  @Input() tab: ClaimListingTab | null;
  private unsubscribeAll$ = new Subject<void>();

  totalCount: number;
  assignees: ClaimFilterOptionModel[] = [];
  isLoading: boolean;

  searchTimeout: any;

  constructor(private claimsService: ClaimService, private accountService: AccountMemberService) {
  }

  assigneesSearchValueChanged(event: any) {
    this.searchAssignees(event.target.value);
  }

  searchAssignees(assigneeName: string) {
    if (this.searchTimeout)
      clearTimeout(this.searchTimeout)

    this.searchTimeout = setTimeout(() => {
      this.isLoading = true;

      this.claimsService.getAssignee({Tab: this.tab, SearchValue: assigneeName , AccountInfoId: this.accountService.memberDetails.accountInfoId})
        .pipe(takeUntil(this.unsubscribeAll$))
        .subscribe(x => {
          if (assigneeName && assigneeName.trim() != "") {
            x = x.filter(a => a.name.toLowerCase().includes(assigneeName.toLowerCase()));
          }
          // Transform the AssigneeModel[] into ClaimFilterOptionModel[]
          const transformedAssignees: ClaimFilterOptionModel[] = x.map((assignee: AssigneeModel) => ({
            id: assignee.memberId,
            name: assignee.name,
            checked: false // Default checked state
          }));

          this.assignees = [
            ...this.selectedAssignees,
            ...transformedAssignees.filter(
              (p: ClaimFilterOptionModel) =>
                !this.selectedAssignees.some((s: ClaimFilterOptionModel) => s.id === p.id)
            )
          ];

          this.isLoading = false;
        });
    }, 1000);
  }

  onAssigneeClicked(assignee: ClaimFilterOptionModel) {
    if (assignee.checked) {
      this.selectedAssignees.remove(assignee);
      assignee.checked = false;
    } else {
      this.selectedAssignees.push(assignee);
      assignee.checked = true;
    }

    this.assigneeClicked.emit()
  }

  ngOnDestroy(): void {
    if (this.searchTimeout)
      clearTimeout(this.searchTimeout);

    this.unsubscribeAll$.next();
    this.unsubscribeAll$.complete();
  }

  ngOnInit(): void {
    this.assignees = [...this.selectedAssignees];
    this.searchAssignees("");
  }

}
