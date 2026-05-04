import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HttpService } from '../http.service';
import { VbMappSettings, VbMappModel } from '@core/models/company-account';


@Injectable({
  providedIn: 'root'
})
export class VbMappService {
  constructor(private http: HttpService) { }

  getVbMappSettings(): Observable<VbMappSettings> {
    const path = `/core/api/Provider/VbMapp/GetVbMappSettingsAsync`;
    return this.http.post<VbMappSettings>(path, {});
  }

    saveVbMapp(data: VbMappModel): Observable<any> {
    const path = `/core/api/Provider/VbMapp/PurchaseVbMapp`;
        return this.http.post<any>(path, { ...data });
  }
}