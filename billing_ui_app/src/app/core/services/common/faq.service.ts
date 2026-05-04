import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs/internal/BehaviorSubject';

import { HttpService } from '../http.service';


@Injectable({
    providedIn: 'root'
})
export class FaqService {
    constructor(private http: HttpService) { }
    private cacheDataSubject = new BehaviorSubject<any>(null);
    private cacheDataObservable = this.cacheDataSubject.asObservable();
    private cacheData: any = null;

    getFaqlist() {
        if (this.cacheData == null) {
            this.http.post('/core/api/workarea/support/GetFaqlist', {}).subscribe(response => {
                this.cacheDataSubject.next(response);
                this.cacheData = response;
            });
        }
        return this.cacheDataObservable;
    };
    trackUserGuideClick(page:string) {
        return this.http.post('/core/api/workarea/support/TrackUserGuideClick', { page: page });
    }
}