import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from "@angular/core";
import { Subject } from "rxjs";
import { ClaimFilterOptionModel } from "@core/models/billing/claim-filter-option-model";

@Component({
    selector: 'patient-invoice-popup',
    templateUrl: './patient-invoice-popup.component.html',
    styleUrls: ['./patient-invoice-popup.component.css'],
})

export class PatientInvoicePopupComponent implements OnDestroy, OnInit {
@Output() statusClicked = new EventEmitter<number>();
@Input() statusList: ClaimFilterOptionModel[];
@Input() selectedStatus: ClaimFilterOptionModel[];
private unsubscribeAll$ = new Subject();
invoiceStatuses: ClaimFilterOptionModel[] = [];
isAllSelect: boolean = true;
totalCount: number;
status: ClaimFilterOptionModel[] = [];
isLoading: boolean;
searchTimeout: any;

    // statusSearchValueChanged(event: any) {
    //     this.searchStatus(event.target.value);
    // }

    searchStatus(status: string) {
        if (this.searchTimeout)
            clearTimeout(this.searchTimeout)

        if(status != "")
        {
            this.invoiceStatuses = this.statusList.where(x => x.name != null && (x.name.toLowerCase().includes(status.toLowerCase())));
        }
        else{
            this.invoiceStatuses = this.statusList;
        }
        this.isAllSelect = this.invoiceStatuses.every(f => f.checked);
    }
        
    onStatusClicked(status: ClaimFilterOptionModel) {
        const index = this.selectedStatus.findIndex(x => x.id === status.id);
        if (index > -1) {
        this.selectedStatus.splice(index, 1);
        status.checked = false;
        } else {
        this.selectedStatus.push({ ...status, checked: true });
        status.checked = true;
        }

        const selectedIds = this.selectedStatus.map(s => s.id);
        this.isAllSelect = this.invoiceStatuses.length > 0 &&
        this.invoiceStatuses.every(p => selectedIds.includes(p.id));

        this.statusClicked.emit();
    }

    ngOnDestroy(): void {
        if (this.searchTimeout)
            clearTimeout(this.searchTimeout)

        //this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }


    ngOnInit(): void {
        this.invoiceStatuses = [...this.selectedStatus];
        this.searchStatus("");
    }

    selectAll(checked: boolean): void {
     // Clear array while maintaining reference
    this.selectedStatus.length = 0;
   
    if (checked) {
      // Add all invoiceStatuses to the existing array
      this.statusList.forEach(f => {
        this.selectedStatus.push({ ...f, checked: true });
      });
    }

    // Update the checked state for all invoiceStatuses in the filtered list
    this.invoiceStatuses.forEach(status => {
      status.checked = checked;
    });

    // Update isAllSelect flag
    this.isAllSelect = checked && this.invoiceStatuses.length > 0;
    this.statusClicked.emit();
  }
}