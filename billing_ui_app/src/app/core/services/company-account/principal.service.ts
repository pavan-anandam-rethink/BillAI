import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HttpService } from '../http.service';
import { SchedulingOptions, SchedulingTypes, SchedulingTag, PrincipalSignatureModel } from '@core/models/company-account';


@Injectable({
  providedIn: 'root'
})
export class PrincipalService {
  constructor(private http: HttpService) { }

  getPrincipalSignature(): Observable<PrincipalSignatureModel> {
    const path = `/core/api/Provider/Provider/GetPrincipalSignature`;
    return this.http.post<PrincipalSignatureModel>(path, {});
  }

  savePrincipalSignature(data: PrincipalSignatureModel): Observable<PrincipalSignatureModel> {
    const path = `/core/api/Provider/Provider/SavePrincipalSignature`;
    return this.http.post<PrincipalSignatureModel>(path, data);
  }

}