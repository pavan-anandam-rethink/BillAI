import { Component, EventEmitter, Inject, Input, OnInit, Output, ViewChild } from "@angular/core";
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog";
import { Diagnosis } from "@core/models/clients";
import { AccountMemberService } from "@core/services/account/account-member.service";
import { ClientsService } from "@core/services/clients/clients.service";
import { debounceTime, distinctUntilChanged, Subject } from "rxjs";

export interface BillingCodeAddNoteResult {
    save: boolean;
    selectedDiagnosisCode: any;
}

@Component({
    selector: 'add-diagnosis-popup',
    templateUrl: './add-diagnosis-popup.component.html',
    styleUrls: ['./add-diagnosis-popup.component.css'],
})

export class AddDiagnosisPopupComponent implements OnInit {
    @Output() save = new EventEmitter<any>();
    @Input() addedCodeIds: number[];

    note = "";
    isOpened: boolean = false;
    public diagnosisCodes: any[];
    delayedCall: any;
    requestValue: string;
    selectedDiagnosis: any;
    isLoading: boolean;
    subject: Subject<any> = new Subject();
    constructor(public dialogRef: MatDialogRef<AddDiagnosisPopupComponent>,
        @Inject(MAT_DIALOG_DATA) public data: any, private clientService: ClientsService
        , private accountService: AccountMemberService) { }

    close() {
        this.dialogRef.close();
        
    }

    ngOnInit(): void {
        this.subject
            .pipe(debounceTime(1000), distinctUntilChanged())
            .subscribe((e) => {
                this.getDiagnosisCodessByValue(e);
            });
    }

    diagnosisSearchValueChange(event: any) {
        this.subject.next(event);
    }

    getDiagnosisCodessByValue(name: any): void {
        if (!name) {
            this.selectedDiagnosis = undefined;
        }
        this.delayedCall = window.setTimeout(() => {
            if (name == this.requestValue) {
                this.searchDiagnosis(name);
            } else {
                this.isLoading = false;
            }
        }, 1000);

        this.requestValue = name;
    }

    diagnosisSelected(event: any) {
        this.selectedDiagnosis = this.diagnosisCodes.find((x: any) => x.diagnosisFullDescription === event);
    }

    popupOpen() {
        this.requestValue = "";
        this.diagnosisCodes = [];
    }

    searchDiagnosis(searchTerm: any): void {
        if (searchTerm.length < 3) return;
        this.isLoading = true;
        const req = {
            SearchTerm: searchTerm,
            addCustom: true,
            accountInfoId: this.accountService.memberDetails.accountInfoId
        };
        if (searchTerm) {
            this.isLoading = true;
            this.clientService.searchDiagnosisCodes(req).subscribe((result: any) => {
                this.diagnosisCodes = result.diagnosisInfo
                    .filter((x: Diagnosis) => this.data.addedCodeIds.indexOf(x.diagnosisId) < 0)
                    .map((x: Diagnosis) => ({
                        ...x,
                        diagnosisFullDescription: `${x.diagnosisLUCode} - ${x.diagnosisLUDescription}`
                    }));
                this.isLoading = false;
            });
        }
    }

    onSave() {
        const result: BillingCodeAddNoteResult = { save: true, selectedDiagnosisCode: this.selectedDiagnosis };
        this.dialogRef.close(result);
    }

    open(): void {
        this.isOpened = true
    }
}