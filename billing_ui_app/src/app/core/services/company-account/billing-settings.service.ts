import { Injectable } from '@angular/core';
import { Observable } from 'rxjs/internal/Observable';

import { HttpService } from '../http.service';
import { BillingSettings, BillingClearingHouse } from '@core/models/company-account';


@Injectable({
  providedIn: 'root'
})
export class BillingSettingsService {
  constructor(private http: HttpService) { }

  getBillingSettings(): Observable<BillingClearingHouse[]> {
    const path = `/core/api/Provider/Provider/GetBillingClearingHouse`;
    return this.http.post<BillingClearingHouse[]>(path, {});
  }

  saveBillingSettings(data: BillingSettings): Observable<void> {
    const path = `/core/api/Provider/Provider/SaveProviderBillingSettings`;
    return this.http.post<void>(path, {...data});
  }
}