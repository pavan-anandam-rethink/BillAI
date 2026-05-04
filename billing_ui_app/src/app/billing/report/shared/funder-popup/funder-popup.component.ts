import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';

@Component({
  selector: 'Funder-popup',
  templateUrl: './funder-popup.component.html',
  styleUrls: ['./funder-popup.component.css']
})
export class FunderPopupComponent {
  @Output() fundersClicked = new EventEmitter<number>();
  @Input() userList: ClaimFilterOptionModel[];
  funders: ClaimFilterOptionModel[] = [];
  @Input() selectedFunder: ClaimFilterOptionModel[];

  isLoading: boolean;
  searchTimeout: any;
  isAllSelect:boolean =false;

  searchfunders(FunderName: string) {
    if (this.searchTimeout)
        clearTimeout(this.searchTimeout)

    if(FunderName != "")
    {
       this.funders = this.userList.where(x => x.name != null && (x.name.toLowerCase().includes(FunderName.toLowerCase()) || x.checked));
    }
    else{
        this.funders = this.userList;
    }
    this.isAllSelect = this.funders.length > 0 && this.funders.every(f=>f.checked);
  }

  onFunderClicked(funder: ClaimFilterOptionModel) {
    if (funder.checked) {
        this.selectedFunder.remove(funder);
        funder.checked = false;
    } else {
        this.selectedFunder.push(funder);
        funder.checked = true;
    }

    this.isAllSelect = this.funders.every(f=>f.checked);

    this.fundersClicked.emit()
  }


  selectAll(checked: boolean): void {
    this.funders.forEach(funder => {
      if (funder.checked && !checked) {
        this.selectedFunder.remove(funder);
      } else if (!funder.checked && checked) {
         this.selectedFunder.push(funder);
        //this.selectedFunder = []
      }
      funder.checked = checked
      this.fundersClicked.emit()
    });

    this.isAllSelect = this.funders.every(f => f.checked);
  }

  funderSearchValueChanged(event: any) {
    this.searchfunders(event.target.value);
  }

  ngOnInit(): void {
    this.funders = [...this.selectedFunder];
    this.searchfunders("");
}
}
