import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs/internal/BehaviorSubject';
import { Observable } from 'rxjs/internal/Observable';

import { HttpService } from '../http.service';
import { Funders, FunderOptions } from '@core/models/company-account';
import { CaseManager } from '@core/models/company-account/funders/case-manager';
import { ServiceLine } from '@core/models/company-account/funders/service-line';
import { RequestResult } from '@core/models/common';
import { BillingCodeEntity } from '@core/models/company-account/funders/billing-code-entity';
import { DayPilot } from 'daypilot-pro-angular';


@Injectable({
    providedIn: 'root'
})
export class FundersService {
    private getProviderFundersSubject = new BehaviorSubject<Funders[]>([]);
    readonly providerFundersList: Observable<Funders[]> = this.getProviderFundersSubject.asObservable();
    private providerFundersListSnapshotLoaded = false;
    
    constructor(private http: HttpService) { }

    getProviderFunderOptions(): Observable<FunderOptions> {
        const path = `/core/api/Provider/Provider/GetProviderFunderOptionsAsync`;
        return this.http.post<FunderOptions>(path, {});
    }

    resetFundersCache() {
        this.providerFundersListSnapshotLoaded = false;
    }

    getProviderFunders(): Observable<Funders[]> {
        if(!this.providerFundersListSnapshotLoaded){
            this.providerFundersListSnapshotLoaded = true;

            this.http.post<Funders[]>('/core/api/Provider/Provider/GetProviderFundersAsync', {})
                .subscribe((x) => {
                    this.getProviderFundersSubject.next(x);
                });
        }
        
        return this.providerFundersList;
    }

    saveCaseManager(data: CaseManager): Observable<CaseManager> {
        const path = `/core/api/Provider/Provider/SaveCaseManager`;
        return this.http.post<CaseManager>(path, { ...data });
    }

    saveServiceFunder(data: { serviceLineId: number, funderId: number, billingSubmissionMethodId: number }): Observable<ServiceLine> {
        const path = `/core/api/Provider/Provider/AssignServiceFunder`;
        return this.http.post<ServiceLine>(path, { ...data });
    }

    deleteFunderServiceLine(serviceLineId: number, funderId: number): Observable<RequestResult> {
        const path = `/core/api/Provider/Provider/DeleteFunderServiceLineAsync`;
        return this.http.post<RequestResult>(path, { id: serviceLineId, funderId });
    }

    updateFunderStatus(funderId: number, isActive: boolean): Observable<RequestResult> {
        const path = `/core/api/Provider/Provider/UpdateFunderStatusAsync`;
        return this.http.post<RequestResult>(path, { funderId, isActive });
    }

    deleteFunder(funderId: number): Observable<RequestResult> {
        const path = `/core/api/Provider/Provider/DeleteFunderAsync`;
        return this.http.post<RequestResult>(path, funderId);
    }

    duplicateOldBillingCodes(data: BillingCodeEntity[]): Observable<BillingCodeEntity[]> {
        const path = `/core/api/Provider/Provider/DuplicateOldBillingCodesAsync`;
        return this.http.post<BillingCodeEntity[]>(path, data);
    }

    saveFunder(data: Funders): Observable<Funders> {
        const offset = new Date().getTimezoneOffset();
        if (data.propagatingData && data.propagatingData.startDate) {
            data.propagatingData.startDate = new DayPilot.Date(data.propagatingData.startDate).addMinutes(-1 * offset).toDate().toISOString();
        }
        const path = `/core/api/Provider/Provider/SaveFunderAsync`;
        return this.http.post<Funders>(path, data);
    }

    saveBillingCodeStatus(billingCodeId: number, inactive: boolean): Observable<void> {
        const path = `/core/api/Provider/Provider/SaveBillingCodeStatusAsync`;
        return this.http.post<void>(path, { billingCodeId, inactive });
    }

    // BillingCodeEntity

    saveBillingCode(billingCode: any): Observable<BillingCodeEntity> {
        const path = `/core/api/Provider/Provider/SaveBillingCode`;
        return this.http.post<BillingCodeEntity>(path, billingCode);
    }

    deleteBillingCode(billingCodeId: number): Observable<RequestResult> {
        const path = `/core/api/Provider/Provider/DeleteBillingCodeAsync`;
        return this.http.post<RequestResult>(path, billingCodeId);
    }
}