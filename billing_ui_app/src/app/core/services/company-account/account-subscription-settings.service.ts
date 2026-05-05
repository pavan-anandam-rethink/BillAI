import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { HttpService } from '../http.service';
import { AccountSubscriptionSettings } from '@core/models/company-account';

@Injectable({
    providedIn: 'root'
})
export class AccountSubscriptionSettingsService {
    private accountSubscriptionSettingsSubject = new BehaviorSubject<any|null>(null);
    accountSubscriptionSettings: Observable<AccountSubscriptionSettings | null> = this.accountSubscriptionSettingsSubject.asObservable();
    public accountSubscriptionSettingsSnapshot: AccountSubscriptionSettings | null = null;
    loading: boolean;

    constructor(private http: HttpService) {
        this.loading = true;
        this.reloadAccountSubscriptionSettings();
    }

    reloadAccountSubscriptionSettings() {
        this.getAccountSubscriptionSettings()
            .subscribe((x: any) => {
                this.accountSubscriptionSettingsSnapshot = x;
                this.accountSubscriptionSettingsSubject.next(x);
                this.loading = false;
            })
    }


    getAccountSubscriptionSettings() {
        return this.http.post('/core/api/Provider/Provider/GetAccountSubscriptionSettings', {})
    }
}