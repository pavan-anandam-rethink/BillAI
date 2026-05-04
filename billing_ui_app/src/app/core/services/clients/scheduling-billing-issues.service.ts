import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HttpService } from '@core/services';
import { HashTable } from '@core/models/common';
import { SchedulingBillingIssueDetails } from '@core/models/clients';



@Injectable({
  providedIn: 'root'
})
export class SchedulingBillingIssuesService {

  constructor(private http: HttpService) { }

  getClientsSchedulingBillingIssues(): Observable<HashTable<string>> {
    return this.http.post<HashTable<string>>('/core/api/client/client/GetClientsSchedulingBillingIssues', {});
  }

  getSchedulingBillingIssueDetails(childProfileId: number, includeOptions: boolean): Observable<SchedulingBillingIssueDetails[]> {
    return this.http.post<SchedulingBillingIssueDetails[]>('/core/api/client/client/GetSchedulingBillingIssueDetails', { childProfileId, includeOptions });
  }

  saveSchedulingBillingIssueDetails(data: { detail: SchedulingBillingIssueDetails, childProfileId: number }): Observable<any> {
    return this.http.post<any>('/core/api/client/client/SaveSchedulingBillingIssueDetails', data);
  }
}