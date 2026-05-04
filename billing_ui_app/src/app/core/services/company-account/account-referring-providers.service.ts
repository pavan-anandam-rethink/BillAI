import {Injectable} from "@angular/core";
import {HttpService} from "../http.service";
import {Observable} from "rxjs";


@Injectable({
    providedIn: 'root'
})
export class AccountReferringProvidersService {

    constructor(private http: HttpService) {
    }

    getReferringProviders(gridState: any): Observable<any> {
        return this.http.post<any>('/core/api/provider/provider/GetReferringProviders', gridState);
    }

    inactiveReferringProvider(providerId: number, clientId: number): Observable<any> {
        return this.http.post('/core/api/provider/provider/InactiveReferringProvider', { providerId, clientId });
    }

    removeReferringProvider(providerId: number, clientId: number): Observable<any>{
        return this.http.post('/core/api/provider/provider/RemoveReferringProvider',{providerId, clientId});
    }
}