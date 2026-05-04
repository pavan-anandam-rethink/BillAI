import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ReportService } from '@core/services/billing/report.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'client-filter-popup',
  templateUrl: './client-filter-popup.component.html',
  styleUrls: ['./client-filter-popup.component.css']
})
export class ClientFilterPopupComponent {
  @Output() patientClicked = new EventEmitter<number>();
  @Input() selectedPatients: ClaimFilterOptionModel[] = [];

  patients: ClaimFilterOptionModel[] = [];
  userList: ClaimFilterOptionModel[] = [];
  isLoading = false;
  searchTimeout: any;
  isAllSelect = false;
  private unsubscribeAll$ = new Subject<void>();

  
  private static cachedPatients: ClaimFilterOptionModel[] = [];

  constructor(private reportingService: ReportService) {}

  

  searchPatients(patientName: string): void {
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
    }

    
    if (ClientFilterPopupComponent.cachedPatients.length > 0) {
      this.userList = ClientFilterPopupComponent.cachedPatients;
      this.setUserList(patientName);
      return;
    }

    
    this.searchTimeout = setTimeout(() => {
      this.isLoading = true;
      this.reportingService.getClientListByIds()
        .pipe(takeUntil(this.unsubscribeAll$))
        .subscribe(x => {
          ClientFilterPopupComponent.cachedPatients = x; // store globally
          this.userList = x;
          this.patients = this.userList.map(p => ({
            ...p,
            checked: this.selectedPatients.some(sel => sel.id === p.id)
          }));
          this.patients.sort((a, b) => Number(b.checked) - Number(a.checked));
          this.isLoading = false;
          this.isAllSelect = this.patients.length > 0 && this.patients.every(f => f.checked);
        });
    }, );
  }

  setUserList(patientName: string): void {
  
  let filteredList = this.userList;
  if (patientName && patientName.trim() !== '') {
    filteredList = this.userList.filter(p => 
      p.name && p.name.toLowerCase().includes(patientName.toLowerCase())
    );
  }

  
  const selectedIds = this.selectedPatients.map(s => s.id);
  this.patients = filteredList.map(p => ({
    ...p,
    checked: selectedIds.includes(p.id)
  }));

  
  this.patients.sort((a, b) => Number(b.checked) - Number(a.checked));

  
  this.isAllSelect = this.userList.length > 0 && 
                     this.userList.every(p => selectedIds.includes(p.id));
}


onPatientClicked(patient: ClaimFilterOptionModel): void {
  const index = this.selectedPatients.findIndex(x => x.id === patient.id);
  if (index > -1) {
    this.selectedPatients.splice(index, 1);
    patient.checked = false;
  } else {
    this.selectedPatients.push({ ...patient, checked: true });
    patient.checked = true;
  }

  
  const selectedIds = this.selectedPatients.map(s => s.id);
  this.isAllSelect = this.userList.length > 0 &&
                     this.userList.every(p => selectedIds.includes(p.id));

  this.patientClicked.emit();
}



  selectAll(checked: boolean): void {
  // Clear array while maintaining reference
  this.selectedPatients.length = 0;
  
  if (checked) {
    // Add all users to the array
    this.userList.forEach(p => {
      this.selectedPatients.push({ ...p, checked: true });
    });
  }

  // Update the display list
  const selectedIds = this.selectedPatients.map(s => s.id);
  this.patients.forEach(p => p.checked = selectedIds.includes(p.id));

  this.isAllSelect = checked;
  this.patientClicked.emit();
}
ngOnInit(): void {
    
    if (ClientFilterPopupComponent.cachedPatients.length > 0) {
      this.userList = ClientFilterPopupComponent.cachedPatients;
      this.setUserList('');
    } else {
      this.searchPatients('');
    }
  }

  

  patientsSearchValueChanged(event: any): void {
    this.searchPatients(event.target.value);
  }
}
