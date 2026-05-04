import { Injectable } from '@angular/core';
import { Observable } from 'rxjs/internal/Observable';

import { HttpService } from '../http.service';
import { DataSettings } from '@core/models/company-account';


@Injectable({
  providedIn: 'root'
})
export class DataSettingsService {
  constructor(private http: HttpService) { }

  getDataSettings(): Observable<DataSettings> {
    const path = `/core/api/Provider/Provider/GetDataCollectionSettings`;
    return this.http.post<DataSettings>(path, {});
  }

  saveData(data: DataSettings): Observable<void> {
    const path = `/core/api/Provider/Provider/SaveDataCollectionSettingsAsync`;
    return this.http.post<void>(path, data);
  }
}