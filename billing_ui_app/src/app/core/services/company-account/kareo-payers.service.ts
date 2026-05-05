import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HttpService } from '../http.service';
import { KareoPayer } from '@core/models/company-account';
import { RequestResult } from '@core/models/common';


@Injectable({
  providedIn: 'root'
})
export class KareoPayersService {
  constructor(private http: HttpService) { }

  getKareoPayers(): Observable<KareoPayer[]> {
    const path = `/core/api/Provider/Provider/GetKareoPayers`;
    return this.http.post<KareoPayer[]>(path, {});
  }

  deleteKareoPayer(data: KareoPayer): Observable<RequestResult> {
    const path = `/core/api/Provider/Provider/DeleteKareoPayer`;
    return this.http.post<RequestResult>(path, data);
  }

  saveKareoPayer(data: KareoPayer): Observable<KareoPayer> {
    const path = `/core/api/Provider/Provider/SaveKareoPayer`;
    return this.http.post<KareoPayer>(path, data);
  }

}