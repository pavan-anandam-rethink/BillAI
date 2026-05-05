
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HttpService } from '../http.service';
import { AbcData, RequestError } from '@core/models/company-account';


@Injectable({
  providedIn: 'root'
})
export class AbcOptionsService {
  constructor(private http: HttpService) { }

  getAbcOptions(): Observable<AbcData> {
    const path = `/core/api/Provider/DataEntry/GetAbcOptionsAsync`;
    return this.http.post<AbcData>(path, {});
  }

  saveAbcOptions(name: string, type: number): Observable<void> {
    const path = `/core/api/Provider/DataEntry/SaveAbcOptions`;
    return this.http.post<void>(path, { name, type });
  }

  deleteAbcOptions(id: number): Observable<RequestError | void> {
    const path = `/core/api/Provider/DataEntry/DeleteAbcOptions`;
    return this.http.post<RequestError | void>(path, id);
  }
}