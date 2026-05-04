import { Component } from "@angular/core";
import { PatientDetails } from "@core/models/billing";
import { ClaimPostingService } from "@core/services/billing";
import { Observable, Subject } from "rxjs";
import { takeUntil } from "rxjs/operators";
import { environment } from "src/environments/environment";

@Component({
    selector: 'patient-details',
    templateUrl: './patient-details.html',
    styleUrls: ['./patient-details.css']
})
export class PatientDetailsComponent {
    details: PatientDetails;
    private unsubscribeAll$ = new Subject();
    clientName: string;
    rethinkUrl: string;

    constructor(private claimService: ClaimPostingService) {
        this.rethinkUrl = environment.rethinkBHUrl;
    }

    setData(patientId: number, clientName = '') {
        this.claimService.getPatientDetails(patientId)
            .pipe(takeUntil(this.unsubscribeAll$))
            .subscribe(x => {
                
                this.clientName = clientName;
                if (x) {
                    this.details = x;
                    if (x.address) {
                        this.details.address = x.address.split(/[\r\n]+/).where((y: any) => y && y != ",   ").join("\r\n");
                    }
                }
            });
    }

    getInitials(patientName: string) {
        if(patientName == undefined) return '';
        return patientName.split(' ').select((x: string) => x.charAt(0)).join('');
    }

    ngOnDestroy(): void {
        this.unsubscribeAll$.next(void 0);
        this.unsubscribeAll$.complete();
    }
    navigateToRethink(childProfileId) {
        var url = this.rethinkUrl + '/core/clients/' + childProfileId;
        window.open(url, '_blank');
    }
}