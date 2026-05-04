import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ReportService } from '@core/services/billing/report.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'funder-filter-popup',
  templateUrl: './funder-filter-popup.component.html',
  styleUrls: ['./funder-filter-popup.component.css']
})
export class FunderFilterPopupComponent {
  @Output() funderClicked = new EventEmitter<number>();
    @Input() selectedfunders: ClaimFilterOptionModel[];
    @Input() funderList: ClaimFilterOptionModel[] = [];
    funders: ClaimFilterOptionModel[] = [];
    userList: ClaimFilterOptionModel[] = [];
    isLoading: boolean;
    searchTimeout: any;
    isAllSelect: boolean = false;
    private unsubscribeAll$ = new Subject();
  
    constructor(private reportingService: ReportService) {}
  
    searchfunders(funderName: string) {
      if (this.searchTimeout)
        clearTimeout(this.searchTimeout)

      this.searchTimeout = setTimeout(() => {
        this.setUserList(funderName);
      }, 300);
    }
  
    setUserList(funderName: string) {
      let filteredList = [];
      this.funders = this.selectedfunders;
      if (funderName != "") 
        filteredList = this.funderList.filter(x => x.name != null && (x.name.toLowerCase().includes(funderName.toLowerCase()) || x.checked));
      else {
        filteredList = this.funderList;
      }
      this.funders = this.funders.concat(filteredList.filter((p: ClaimFilterOptionModel) =>
        !this.selectedfunders.some((s: ClaimFilterOptionModel) => s.id == p.id)));
      this.isAllSelect = this.funders.length > 0 && this.funders.every(f => f.checked);
    }
  
    onFunderClicked(funder: ClaimFilterOptionModel) {
      if (funder.checked) {
        this.selectedfunders.remove(funder);
        funder.checked = false;
      } else {
        this.selectedfunders.push(funder);
        funder.checked = true;
      }
  
      this.isAllSelect = this.funders.every(f => f.checked);
  
      this.funderClicked.emit()
    }
  
  
    selectAll(checked: boolean): void {
      this.funders.forEach(funder => {
        if (funder.checked && !checked) {
          this.selectedfunders.remove(funder);
        } else if (!funder.checked && checked) {
          this.selectedfunders.push(funder);
        }
        funder.checked = checked
        this.funderClicked.emit()
      });
  
      this.isAllSelect = this.funders.every(f => f.checked);
    }
  
    fundersSearchValueChanged(event: any) {
      this.searchfunders(event.target.value);
    }
  
    ngOnInit(): void {
      this.funders = [...this.selectedfunders];
      this.setUserList("");
    }
}
