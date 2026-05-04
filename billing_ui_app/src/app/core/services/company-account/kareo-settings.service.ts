import { Injectable } from '@angular/core';
import { Observable } from 'rxjs/internal/Observable';

import { HttpService } from '../http.service';
import { KareoSettings } from '@core/models/company-account';
import { RequestResult } from '@core/models/common';


@Injectable({
  providedIn: 'root'
})
export class KareoSettingsService {
  constructor(private http: HttpService) { }

  getKareoSettings(): Observable<KareoSettings> {
    const path = `/core/api/Provider/Provider/GetKareoSettings`;
    return this.http.post<KareoSettings>(path, {});
  }

  saveKareoSettings(data: KareoSettings): Observable<RequestResult> {
    const path = `/core/api/Provider/Provider/SaveKareoSettings`;
    return this.http.post<RequestResult>(path, data);
  }
}