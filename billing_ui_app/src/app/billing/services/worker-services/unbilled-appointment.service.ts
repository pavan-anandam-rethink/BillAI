import { Injectable } from "@angular/core";
import { UnbilledAppointmentsWorkerService } from "./unbilled-appointments-worker.service";
import { environment } from "src/environments/environment";
import { BehaviorSubject, Observable } from "rxjs";
import { AccountMemberService } from "@core/services/account/account-member.service";

@Injectable()
export class UnbilledAppointmentsService {
    apiBaseUrl: string = "";
    constructor(private workerService: UnbilledAppointmentsWorkerService,
        private accountService: AccountMemberService
    ) {
        this.apiBaseUrl = environment.claimApiBaseUrl;
        const worker = this.workerService.getWorker();
    }

    exportToExcel(model): Observable<any> {
        model.accountInfoId = this.accountService.memberDetails.accountInfoId;
        model.memberId = this.accountService.memberDetails.memberId;

        const token = localStorage.getItem("token");
        const url = this.apiBaseUrl + "/AppointmentReports/ExportUnbilledAppointmentData";
        const dataSubject = new BehaviorSubject<any>(null);

        const worker = this.workerService.getWorker();
        const request = { url, token, model };

        worker!.postMessage(request);
        worker!.onmessage = (event) => {
            dataSubject.next(event);
        };
        return dataSubject.asObservable();
    }

    exportUnprocessedToExcel(model): Observable<any> {
        model.accountInfoId = this.accountService.memberDetails.accountInfoId;
        model.memberId = this.accountService.memberDetails.memberId;

        const token = localStorage.getItem("token");
        const url = this.apiBaseUrl + "/AppointmentReports/ExportUnprocessedAppointmentData";
        const dataSubject = new BehaviorSubject<any>(null);

        const worker = this.workerService.getWorker();
        const request = { url, token, model };

        worker!.postMessage(request);
        worker!.onmessage = (event) => {
            dataSubject.next(event);
        };
        return dataSubject.asObservable();
    }
}