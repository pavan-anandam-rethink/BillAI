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
  @Input() selectedPatients: ClaimFilterOptionModel[];
  @Input() userList: ClaimFilterOptionModel[] = [];
  patients: ClaimFilterOptionModel[] = [];
  isLoading: boolean;
  searchTimeout: any;
  isAllSelect: boolean = false;
  private unsubscribeAll$ = new Subject();

  constructor(private reportingService: ReportService) {}

  searchPatients(patientName: string) {
    this.setUserList(patientName);
  }

  setUserList(patientName: string) {
    let filteredList = [];
      this.patients = this.selectedPatients; 
      if (patientName != "") 
        filteredList = this.userList.where(x => x.name != null && (x.name.toLowerCase().includes(patientName.toLowerCase()) || x.checked));
      else {
        filteredList = this.userList;
      }
      this.patients = this.patients.concat(filteredList.where((p: ClaimFilterOptionModel) =>
        !this.selectedPatients.any((s: ClaimFilterOptionModel) => s.id == p.id)))
      this.isAllSelect = this.patients.length > 0 && this.patients.every(f=>f.checked);
  }

  onPatientClicked(patient: ClaimFilterOptionModel) {
    if (patient.checked) {
      this.selectedPatients.remove(patient);
      patient.checked = false;
    } else {
      this.selectedPatients.push(patient);
      patient.checked = true;
    }

    this.isAllSelect = this.patients.every(f => f.checked);

    this.patientClicked.emit()
  }


  selectAll(checked: boolean): void {
    this.patients.forEach(patient => {
      if (patient.checked && !checked) {
        this.selectedPatients.remove(patient);
      } else if (!patient.checked && checked) {
        this.selectedPatients.push(patient);
      }
      patient.checked = checked
      this.patientClicked.emit()
    });

    this.isAllSelect = this.patients.every(f => f.checked);
  }

  patientsSearchValueChanged(event: any) {
    this.searchPatients(event.target.value);
  }

  ngOnInit(): void {
    this.patients = [...this.selectedPatients];
    this.setUserList("");
  }
}
