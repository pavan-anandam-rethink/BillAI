import { Component, EventEmitter, Input, OnDestroy, OnInit, Output } from "@angular/core";
import { Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";

import { ClaimService } from "@core/services/billing";
import { ClaimFilterOptionModel } from "@core/models/billing/claim-filter-option-model";
import { ClaimListingTab } from "@core/enums/billing/claim-listing-tab";
import { AccountMemberService } from "@core/services/account/account-member.service";


@Component({
    selector: 'claim-filters-patient-popup',
    templateUrl: './claim-filters-patient-popup.component.html',
    styleUrls: ['./claim-filters-patient-popup.component.css'],
})

export class ClaimFiltersPatientPopupComponent implements OnDestroy, OnInit {
    @Output() patientClicked = new EventEmitter<number>();
    @Input() selectedPatients: ClaimFilterOptionModel[];
    @Input() tab: ClaimListingTab | null;
    private unsubscribeAll$ = new Subject();

    totalCount: number;
    patients: ClaimFilterOptionModel[] = [];
    isLoading: boolean;

    searchTimeout: any;

    constructor(private claimsService: ClaimService, private accountService: AccountMemberService) {
    }

    patientsSearchValueChanged(event: any) {
        this.searchPatients(event.target.value);
    }

    searchPatients(patientName: string) {
        if (this.searchTimeout)
            clearTimeout(this.searchTimeout)

        this.searchTimeout = setTimeout(() => {
            this.isLoading = true;

            this.claimsService.getClaimPatients({Tab: this.tab, SearchValue: patientName , AccountInfoId: this.accountService.memberDetails.accountInfoId})
                .pipe(takeUntil(this.unsubscribeAll$))
                .subscribe(x => {
                    this.patients = this.selectedPatients;
                    this.patients = this.patients.concat(x.where((p: ClaimFilterOptionModel) =>
                        !this.selectedPatients.any((s: ClaimFilterOptionModel) => s.id == p.id)))

                    this.isLoading = false;
                });
        }, 1000);
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