import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HttpService } from '../http.service';

import { ServiceLines, ProviderService, ProviderServiceLine } from '@core/models/company-account';
import { Helper } from '../../../common/common-helper';


@Injectable({
    providedIn: 'root'
})
export class ServiceLinesService {
    constructor(private http: HttpService) { }

    getServiceLines(): Observable<ServiceLines> {
        const path = `/core/api/Provider/Provider/GetServiceLinesAsync`;
        return this.http.post<ServiceLines>(path, {});
    }

    saveServiceLines(data: ProviderServiceLine): Observable<ProviderServiceLine> {
        const path = `/core/api/Provider/Provider/SaveServiceLineAsync`;
        return this.http.post<ProviderServiceLine>(path, { ...data });
    }

    saveProviderService(data: ProviderService): Observable<ProviderService> {
        if (data.propagatingData && data.propagatingData.startDate) {
            const propagatingDate = Helper.shiftDateToUTC(new Date(data.propagatingData.startDate)) as Date;
            data.propagatingData.startDate = propagatingDate.toISOString();
        }
        const path = `/core/api/Provider/Provider/SaveProviderServiceAsync`;
        return this.http.post<ProviderService>(path, { ...data });
    }

    deleteServiceLines(id: number): Observable<void> {
        const path = `/core/api/Provider/Provider/DeleteServiceLineAsync`;
        return this.http.post<void>(path, id);
    }

    deleteProviderService(id: number): Observable<void> {
        const path = `/core/api/Provider/Provider/DeleteServiceAsync`;
        return this.http.post<void>(path, id);
    }
}