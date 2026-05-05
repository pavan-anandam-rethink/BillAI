import { HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { AddressAutocomplete } from '@core/models/common/address-autocomplete';
import { Observable } from 'rxjs';
import { HttpService } from '..';

@Injectable({
    providedIn: 'root'
})
export class AddressAutocompleteService {

    private options = { headers: new HttpHeaders({'Content-Type': 'application/json'}) };

    constructor(private http: HttpService) {}
    
    public getAddresses(searchText: string): Observable<AddressAutocomplete[]> {
        return this.http.post<AddressAutocomplete[]>('/core/api/common/Shared/GetAddressPredictions', 
        JSON.stringify(searchText), 
        this.options);
    }
}