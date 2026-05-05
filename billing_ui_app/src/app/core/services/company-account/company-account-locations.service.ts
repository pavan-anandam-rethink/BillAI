import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';

import { HttpService } from '../http.service';
import { CompanyAccountLocations, CompanyAccountLocation } from '@core/models/company-account';
import { Country, RequestResult, State } from '@core/models/common';


@Injectable({
    providedIn: 'root'
})
export class CompanyAccountLocationsService {
    private stateSource = new BehaviorSubject<State[]>([]);
    state = this.stateSource.asObservable();

    private countrySource = new BehaviorSubject<Country[]>([]);
    country = this.countrySource.asObservable();

    constructor(private http: HttpService) {
        http.post<{ states: State[] }>('/core/api/Provider/Provider/GetStates', {})
            .subscribe(x => {
                this.stateSource.next(x.states || []);
            });

        http.post<{ countries: Country[] }>('/core/api/Provider/Provider/GetCountries', {})
            .subscribe(x => {
                this.countrySource.next(x.countries || []);
            });
    }

    getLocation(): Observable<CompanyAccountLocations> {
        const path = `/core/api/Provider/Provider/GetLocationsAsync`;
        return this.http.post<CompanyAccountLocations>(path, {});
    }

    saveLocation(data: CompanyAccountLocation): Observable<void> {
        const path = `/core/api/Provider/Provider/SaveLocationAsync`;
        return this.http.post<void>(path, { ...data });
    }

    deleteLocation(providerLocationId: number): Observable<RequestResult> {
        const path = `/core/api/Provider/Provider/DeleteProviderLocationAsync`;
        return this.http.post<RequestResult>(path, providerLocationId);
    }
}