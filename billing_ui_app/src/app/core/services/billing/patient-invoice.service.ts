import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { HttpService } from '@core/services';
import {
    Encounter
} from '@core/models/billing';
import { Observable } from 'rxjs';
import { RequestUserData } from '@core/utils/request-user-data';
import { environment } from 'src/environments/environment';
import { AccountMemberService } from '../account/account-member.service';
import { InvoiceRequest,PatientInvoiceHeaderSearch } from '@core/models/billing/patient-invoice';

@Injectable({
    providedIn: 'root'
})
export class PatientInvoiceService {

    public onLoad: Subject<Encounter>;
    private apiBaseUrl: string;

    constructor(private http: HttpService, private reqUserData: RequestUserData, private accountService: AccountMemberService) {
        this.onLoad = new Subject<Encounter>();
        this.apiBaseUrl = environment.claimApiBaseUrl;
    }

    getPatientInvoiceDetails(filters: PatientInvoiceHeaderSearch, showSpinner: boolean = true) {
        filters.accountInfoId = this.accountService.memberDetails.accountInfoId;
        return this.http.post<any>(this.apiBaseUrl + '/PatientInvoice/GetPICreationDetails', filters, { showSpinner }, true);
    }

    PrintPreview(request: InvoiceRequest[]){
        request.forEach(x => x.accountId = this.accountService.memberDetails.accountInfoId);
        return this.http.post<any>(this.apiBaseUrl + '/PatientInvoice/PrintPreview',
            request);
    }

    PrintAndSubmit(invoiceRequests: InvoiceRequest[],includePreviousInvoices: boolean){
        invoiceRequests.forEach(x => x.accountId = this.accountService.memberDetails.accountInfoId);
        return this.http.post<any>(this.apiBaseUrl + '/PatientInvoice/PrintAndSubmit',
            {invoiceRequests,includePreviousInvoices});
    }

    getInvoiceDetails(filters: PatientInvoiceHeaderSearch, showSpinner: boolean = false) {
        filters.accountInfoId = this.accountService.memberDetails.accountInfoId;
        // Pass showSpinner option to disable global loader when desired
        return this.http.post<any>(this.apiBaseUrl + '/PatientInvoice/GetInvoiceDetails', filters, { showSpinner }, true);
    }

   
    getInvoicePDFPrint(invoiceNo: any, clientId) {
        var model = {
            accountId: this.accountService.memberDetails.accountInfoId,
            clientId: clientId,
            invoiceNo: invoiceNo
        }
        return this.http.post<any>(this.apiBaseUrl + '/PatientInvoice/GetInvoicePDF', model);
    }
}
