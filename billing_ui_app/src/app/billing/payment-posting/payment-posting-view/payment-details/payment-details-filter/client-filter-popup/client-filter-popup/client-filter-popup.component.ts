import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ReportService } from '@core/services/billing/report.service';
import { Subject, takeUntil } from 'rxjs';
import { ClaimPostingService } from '@core/services/billing/claim-posting.service';
import { PaymentPatientModel } from '@core/models/billing/cliam-posting';

@Component({
  selector: 'client-filter-popup',
  templateUrl: './client-filter-popup.component.html',
  styleUrls: ['./client-filter-popup.component.css']
})
export class ClientFilterPopupComponent {
@Output() patientClicked = new EventEmitter<number>();
  @Input() selectedPatients: ClaimFilterOptionModel[];
  @Input() paymentId: number;
  patients: ClaimFilterOptionModel[] = [];
  isLoading: boolean;
  searchTimeout: any;
  isAllSelect: boolean = false;
  private unsubscribeAll$ = new Subject();

  constructor(private claimsPostingService: ClaimPostingService) {}

  ngOnInit(): void {
    this.patients = [...this.selectedPatients];
    this.searchPatients("");
  }

  searchPatients(patientName: string) {
  if (!this.paymentId) {
    // If paymentId is not set, don't do the search
    this.patients = [];
    return;
  }

  if (this.searchTimeout) {
    clearTimeout(this.searchTimeout);
  }

  this.searchTimeout = setTimeout(() => {
    this.isLoading = true;

    this.claimsPostingService.getPaymentPatients(this.paymentId)
      .pipe(takeUntil(this.unsubscribeAll$))
      .subscribe((allPatientsFromApi: PaymentPatientModel[]) => {

        // Map API model to ClaimFilterOptionModel and preserve checked state
        this.patients = allPatientsFromApi.map(p => ({
          id: p.patientId,
          name: p.patientName,
          checked: this.selectedPatients?.some(s => s.id === p.patientId) || false
        }));

        // Filter patients by search term if provided
        if (patientName) {
          const searchTerm = patientName.toLowerCase();
          this.patients = this.patients.filter(p =>
            p.name?.toLowerCase().includes(searchTerm)
          );
        }
        this.isLoading = false;
      }, error => {
        console.error('Error fetching patients:', error);
        this.isLoading = false;
      });
  }, 300); // debounce delay
}

  onPatientClicked(patient: ClaimFilterOptionModel) {
    const index = this.selectedPatients.findIndex(x => x.id === patient.id);
    if (index > -1) {
      // Deselect patient
      this.selectedPatients.splice(index, 1);
      patient.checked = false;
    } else {
      // Select patient
      this.selectedPatients.push({ ...patient, checked: true });
      patient.checked = true;
    }
    this.patientClicked.emit();
  }

  patientsSearchValueChanged(event: any) {
    this.searchPatients(event.target.value);
  }
}
