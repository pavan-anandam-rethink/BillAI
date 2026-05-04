import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ReportService } from '@core/services/billing/report.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'staff-filter-popup',
  templateUrl: './staff-filter-popup.component.html',
  styleUrls: ['./staff-filter-popup.component.css']
})
export class StaffFilterPopupComponent {

@Output() StaffClicked = new EventEmitter<ClaimFilterOptionModel[]>();
@Output() isAllSelectedChange = new EventEmitter<boolean>();
  @Input() selectedStaff: ClaimFilterOptionModel[];
  @Input() userList: ClaimFilterOptionModel[] = [];

  staffs: ClaimFilterOptionModel[] = [];
  isLoading: boolean;
  searchTimeout: any;
  isAllSelect: boolean = false;
  private unsubscribeAll$ = new Subject();

  constructor(private reportingService: ReportService) {}

  searchStaffs(StaffName: string) {
    if (this.searchTimeout)
      clearTimeout(this.searchTimeout)

    this.searchTimeout = setTimeout(() => {
      this.setUserList(StaffName);
    }, 300);
  }

  setUserList(StaffName: string) {
    let filteredList = [];
    this.staffs = this.selectedStaff;
    if (StaffName != "") 
      filteredList = this.userList.filter(x => x.name != null && (x.name.toLowerCase().includes(StaffName.toLowerCase()) || x.checked));
    else {
      filteredList = this.userList;
    }
    this.staffs = this.staffs.concat(filteredList.filter((p: ClaimFilterOptionModel) =>
      !this.selectedStaff.some((s: ClaimFilterOptionModel) => s.id == p.id)));
    this.isAllSelect = this.staffs.length > 0 && this.staffs.every(f => f.checked);
  }

  onStaffClicked(staff: any) {
    if (staff.checked) {
      this.selectedStaff.remove(staff);
      staff.checked = false;
    } else {
      this.selectedStaff.push(staff);
      staff.checked = true;
    }

    this.isAllSelect = this.staffs.every(f => f.checked);

    this.StaffClicked.emit()
  }


  selectAll(checked: boolean): void {
    this.selectedStaff.length = 0;
    this.staffs.forEach(Staff => {
      Staff.checked = checked;
      if (checked) {
        this.selectedStaff.push(Staff);
      }
    });

    this.isAllSelect = checked;
    this.isAllSelectedChange.emit(this.isAllSelect);
    this.StaffClicked.emit();
  }

  StaffsSearchValueChanged(event: any) {
    this.searchStaffs(event.target.value);
  }

  ngOnInit(): void {
    this.staffs = [...this.selectedStaff];
    this.setUserList("");
  }
}
