import { Injectable } from '@angular/core';
import { Observable, BehaviorSubject } from 'rxjs';

import { HttpService } from '../http.service';
import {
    SchedulingOptions,
    SchedulingTypes,
    ReminderSettingsData,
    ReminderSettings,
    PlaceOfService
} from '@core/models/company-account';
import { map } from 'rxjs/operators';
import { SchedilingBasicInfoSettings, SchedulingTag } from '@core/models/company-account/scheduling';


@Injectable({
    providedIn: 'root'
})
export class SchedulingService {
    isReminderSettingsLoaded = false
    companyAccountReminderSettingsSource = new BehaviorSubject<any>(false);

    constructor(private http: HttpService) { }

    getTypes(): Observable<SchedulingTypes> {
        const path = `/core/api/Provider/Provider/GetTypes`;
        return this.http.post<SchedulingTypes>(path, {});
    }

    getSchedulingOptions(): Observable<SchedulingOptions> {
        const path = `/core/api/Provider/Provider/GetSchedulingBasicInfoOptions`;
        return this.http.post<SchedulingOptions>(path, {});
    }

    savePayOver(data: any): Observable<void> {
        const path = `/core/api/Provider/Provider/SavePayOverAsync`;
        return this.http.post<void>(path, { ...data });
    }

    saveAppointmentCustomTag(data: SchedulingTag): Observable<SchedulingTag> {
        const path = `/core/api/Provider/Provider/SaveAppointmentCustomTagAsync`;
        return this.http.post<SchedulingTag>(path, { ...data });
    }

    deleteAppointmentCustomTag(id: number): Observable<void> {
        const path = `/core/api/Provider/Provider/DeleteAppointmentCustomTagAsync`;
        return this.http.post<void>(path, id);
    }

    getCompanyAccountReminderSettingsShort(reload: boolean = false): Observable<ReminderSettingsData | null> {
        const path = `/core/api/Provider/Provider/GetCompanyAccountReminderSettingsShort`;
        return this.http.post<ReminderSettingsData>(path, {});
    }

    getCompanyAccountReminderSettings(reload = false): Observable<ReminderSettingsData | null> {
        const path = `/core/api/Provider/Provider/GetCompanyAccountReminderSettings`;
        if (!this.isReminderSettingsLoaded || reload) {
            return this.http.post<ReminderSettingsData>(path, {}).pipe(
                map((response: ReminderSettingsData) => {
                    this.companyAccountReminderSettingsSource.next(response);
                    this.isReminderSettingsLoaded = true;
                    return response;
                }
                )
            );
        } else {
            return this.companyAccountReminderSettingsSource.asObservable();
        }
    }

    saveCompanyAccountReminderSettings(data: ReminderSettings): Observable<void> {
        const path = `/core/api/Provider/Provider/SaveCompanyAccountReminderSettings`;
        return this.http.post<void>(path, data);
    }

    inactivatePlaceOfService(data: PlaceOfService): Observable<any> {
        const path = `/core/api/Provider/Provider/InactivatePlaceOfService`;
        return this.http.post<void>(path, data);
    }

    savePlaceOfService(id: number, description: string): Observable<any> {
        const path = `/core/api/Provider/Provider/SavePlaceOfService`;
        return this.http.post<void>(path, { id, description });
    }

    saveDefaultPlaceOfService(defaultLocationCodeId: number): Observable<void> {
        const path = `/core/api/Provider/Provider/SaveDefaultPlaceOfService`;
        return this.http.post<void>(path, +defaultLocationCodeId);
    }

    getNonbillableTags() {
        const path = `/core/api/Provider/Provider/getTags`;
        return this.http.post<SchedulingTag[]>(path, 3);
    }

    getBillableTags() {
        const path = `/core/api/Provider/Provider/getTags`;
        return this.http.post<SchedulingTag[]>(path, 2);
    }

    getCancellationTypes() {
        const path = `/core/api/Provider/Provider/getTags`;
        return this.http.post<SchedulingTag[]>(path, 1);
    }

    getPlaceOfServices() {
        const path = `/core/api/Provider/Provider/getPlaceOfServices`;
        return this.http.post<{ placeOfServices: PlaceOfService[]; defaultPlaceOfServiceId: number }>(path, {});
    }

    getSchedulingBasicInfoSettings() {
        const path = `/core/api/Provider/Provider/GetSchedulerBasicInfoSettingsAsync`;
        return this.http.post<SchedilingBasicInfoSettings>(path, {});
    }

    SaveSchedulerBasicInfoSettings(info: SchedilingBasicInfoSettings) {
        const path = `/core/api/Provider/Provider/SaveSchedulerBasicInfoSettingsAsync`;
        return this.http.post<any>(path, info);
    }
}