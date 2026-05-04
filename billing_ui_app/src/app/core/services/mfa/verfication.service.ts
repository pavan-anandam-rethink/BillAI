import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HttpService } from '../http.service';
import { MFACodeVerificatorType } from '@core/enums/mfa';
import { RequestResult } from '@core/models/common';

@Injectable({
    providedIn: 'root'
})
export class VerificationService {
    constructor(private http: HttpService) {
    }

    public GetMFAMethods(): Observable<any> {
        return this.http.post<any>('/HealthCare/User/GetMFAMethods', {});
    }

    public SendCode(codeVerificatorType: MFACodeVerificatorType): Observable<boolean> {
        return this.http.post<boolean>('/HealthCare/User/SendCode', {codeVerificatorType});
    }

    public CheckCode(codeVerificatorType: MFACodeVerificatorType, code: string): Observable<RequestResult> {
        return this.http.post<RequestResult>('/HealthCare/User/CheckCode', { codeVerificatorType, code });
    }

    public SetStaffPhone(phone: string): Observable<any> {
        return this.http.post('/HealthCare/User/SetStaffPhone', { phone });
    }
}