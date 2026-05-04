import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from "@angular/core";
import { Subject } from "rxjs";

import { ClaimFilterOptionModel } from "@core/models/billing/claim-filter-option-model";


@Component({
    selector: 'patient-popup',
    templateUrl: './patient-popup.component.html',
    styleUrls: ['./patient-popup.component.css'],
})

export class PatientPopupComponent implements OnDestroy, OnInit {
    @Output() patientClicked = new EventEmitter<number>();
    @Input() selectedPatients: ClaimFilterOptionModel[];
    @Input() userList: ClaimFilterOptionModel[];
    private unsubscribeAll$ = new Subject();

    totalCount: number;
    patients: ClaimFilterOptionModel[] = [];
    isLoading: boolean;

    searchTimeout: any;

    patientsSearchValueChanged(event: any) {
        this.searchPatients(event.target.value);
    }

    searchPatients(patientName: string) {
        if (this.searchTimeout)
            clearTimeout(this.searchTimeout)

        if(patientName != "")
        {
           this.patients = this.userList.where(x => x.name != null && (x.name.toLowerCase().includes(patientName.toLowerCase()) || x.checked));
        }
        else{
            this.patients = this.userList;
        }
    }

    onPatientClicked(patient: ClaimFilterOptionModel) {
        if (patient.checked) {
            this.selectedPatients.remove(patient);
            patient.checked = false;
        } else {
            this.selectedPatients.push(patient);
            patient.checked = true;
        }

        this.patientClicked.emit()
    }

    ngOnDestroy(): void {
        if (this.searchTimeout)
            clearTimeout(this.searchTimeout)

        //this.unsubscribeAll$.next();
        this.unsubscribeAll$.complete();
    }

    ngOnInit(): void {
        this.patients = [...this.selectedPatients];
        this.searchPatients("");
    }
}