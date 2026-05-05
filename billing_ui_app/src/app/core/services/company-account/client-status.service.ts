import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HttpService } from '../http.service';
import { ClientStatus, RequestError } from '@core/models/company-account';


@Injectable({
    providedIn: 'root'
})
export class ClientStatusService {

    constructor(private http: HttpService) { }

    getClientStatus(): Observable<ClientStatus[]> {
        const path = `/core/api/Provider/Provider/GetClientStatus`;
        return this.http.post<ClientStatus[]>(path, {});
    }

    saveClientStatus(data: ClientStatus): Observable<void> {
        const path = `/core/api/Provider/Provider/SaveClientStatusAsync`;
        data.clientsAreInactive = !data.clientsAreInactive;
        return this.http.post<void>(path, { ...data });
    }

    deleteClientStatus(id: number): Observable<RequestError | void> {
        const path = `/core/api/Provider/Provider/DeleteClientStatusAsync`;
        return this.http.post<RequestError | void>(path, id);
    }

    getClientStatusUsage(statusId: number): Observable<number> {
        const path = `/core/api/Provider/Provider/GetClientStatusUsageAsync`;
        return this.http.post<number>(path, statusId);
    }
}