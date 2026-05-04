import { Injectable } from "@angular/core";
import { SecuritySettings } from "@core/models/company-account";
import { Observable } from "rxjs";
import { HttpService } from "../http.service";


@Injectable({
    providedIn: 'root'
  })
export class SecuritySettingsService {

    constructor(private httpService: HttpService) { }

    getSecuritySettings(): Observable<SecuritySettings> {
        return this.httpService.get(`/core/api/Provider/Provider/GetSecuritySettings`);
    }

    savePasswordExpirySettings(data: SecuritySettings): Observable<SecuritySettings> {
        return this.httpService.post(`/core/api/Provider/Provider/SavePasswordExpirySettings`, data);
    }

    saveMultiFactorAuthenticationSettings(data: SecuritySettings): Observable<SecuritySettings> {
        return this.httpService.post(`/core/api/Provider/Provider/SaveMultiFactorAuthenticationSettings`, data);
    }
}