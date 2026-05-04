import { Component } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ClaimService } from '@core/services/billing';
import { DialogRef } from '@progress/kendo-angular-dialog';
import { AssigneeModel } from '@core/models/billing/assignee-model';
import { AccountMemberService } from '../../../../core/services/account/account-member.service';


@Component({
  selector: 'app-flag-claim-dialog',
  templateUrl: './flag-claim-dialog.component.html',
  styleUrls: ['./flag-claim-dialog.component.css']
})
export class FlagClaimDialogComponent {
  formGroup: FormGroup;
  flagReasons: any[] = [];
  assignees: AssigneeModel[] = [];
  isEditMode = false;

  constructor(
    private fb: FormBuilder,
    private dialog: DialogRef,
    private claimService: ClaimService,
    private accountService: AccountMemberService
  ) {

    this.formGroup = this.fb.group({
      reasonId: [null, Validators.required],
      assigneeId: [0, Validators.required], 
      note: [''],
    });

    this.loadFlagReasons();
    this.loadAssignees();
  }


  loadFlagReasons(): void {
    this.claimService.getClaimFlagReasons(true).subscribe({
      next: (reasons) => {
        this.flagReasons = reasons ?? [];
      },
      error: () => {
        this.flagReasons = [];
      }
    });
  }

  loadAssignees(): void {
    this.claimService.getAssignee({Tab: 0, SearchValue: '' , AccountInfoId: this.accountService.memberDetails.accountInfoId}).subscribe({
      next: (users) => {
        this.assignees = users ?? [];
      },
      error: () => {
        this.assignees = [];
      }
    });
  }

  cancel(): void {
    this.dialog.close();
  }

  submit(): void {
    if (!this.formGroup.valid) {
      this.formGroup.markAllAsTouched();
      return;
    }

    const { reasonId, assigneeId, note } = this.formGroup.value;
      this.dialog.close({
        submit: true,
        reasonId,
        assigneeId,
        notes: note,
        isEditMode: this.isEditMode
      });
  }
}
