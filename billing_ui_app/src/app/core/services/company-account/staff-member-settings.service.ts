import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HttpService } from '../http.service';
import { StaffTitleModel, BasicItem, RequestError, StaffMemberCredentialType, StaffMemberCredential } from '@core/models/company-account';
import { StaffStatusData } from '@core/models/company-account/staff-member-settings/staff-status-data';
import { StaffPaycodeData } from '@core/models/company-account/staff-member-settings/staff-paycode-data';
import { StaffMileageData } from '@core/models/company-account/staff-member-settings/staff-mileage-data';
import { StaffMileages } from '@core/models/company-account/scheduling-types';


@Injectable({
    providedIn: 'root'
})
export class StaffMemberSettingsService {
    constructor(private http: HttpService) { }

    getStaffTitles() {
        const path = `/core/api/Provider/Provider/GetStaffTitlesAsync`;
        return this.http.post<{ staffTitles: StaffTitleModel[], roleTypes: BasicItem[] }>(path, {});
    }

    saveStaffTitle(data: StaffTitleModel): Observable<StaffTitleModel> {
        const path = `/core/api/Provider/Provider/SaveStaffTitleAsync`;
        return this.http.post<StaffTitleModel>(path, { ...data });
    }

    deleteStaffTitle(id: number): Observable<void | RequestError> {
        const path = `/core/api/Provider/Provider/DeleteStaffTitleAsync`;
        return this.http.post<void | RequestError>(path, id);
    }

    getEmployeeType() {
        const path = `/core/api/Provider/Provider/GetEmployeeTypesAsync`;
        return this.http.post<BasicItem[]>(path, {});
    }

    saveEmployeeType(data: BasicItem): Observable<BasicItem> {
        const path = `/core/api/Provider/Provider/SaveEmployeeTypeAsync`;
        return this.http.post<BasicItem>(path, { ...data });
    }

    deleteEmployeeType(id: number): Observable<void | RequestError> {
        const path = `/core/api/Provider/Provider/DeleteEmployeeTypeAsync`;
        return this.http.post<void | RequestError>(path, id);
    }

    getStaffStatuses() {
        const path = `/core/api/Provider/Provider/GetStaffStatusesAsync`;
        return this.http.post<StaffStatusData[]>(path, { });
    }

    saveStaffStatus(data: StaffStatusData): Observable<StaffStatusData> {
        const path = `/core/api/Provider/Provider/SaveStaffStatusAsync`;
        return this.http.post<StaffStatusData>(path, { ...data });
    }

    deleteStaffStatus(id: number): Observable<void | RequestError> {
        const path = `/core/api/Provider/Provider/DeleteStaffStatusAsync`;
        return this.http.post<void | RequestError>(path, id);
    }

    getStaffsWithStatusNumberAsync(id: number): Observable<number> {
        const path = `/core/api/Provider/Provider/GetStaffsWithStatusNumberAsync`;
        return this.http.post<number>(path, id);
    }

    getStaffPaycodes() {
        const path = `/core/api/Provider/Provider/GetStaffPaycodesAsync`;
        return this.http.post<StaffPaycodeData[]>(path, { });
    }

    saveStaffPaycode(data: StaffPaycodeData): Observable<StaffPaycodeData> {
        const path = `/core/api/Provider/Provider/SaveStaffPaycodeAsync`;
        return this.http.post<StaffPaycodeData>(path, { ...data });
    }

    deleteStaffPaycode(id: number): Observable<void> {
        const path = `/core/api/Provider/Provider/DeleteStaffPaycodeAsync`;
        return this.http.post<void>(path, id);
    }

    getStaffMileages() {
        const path = `/core/api/Provider/Provider/GetStaffMileageAsync`;
        return this.http.post<StaffMileages[]>(path, { });
    }

    saveStaffMileage(data: StaffMileageData): Observable<StaffMileages> {
        const path = `/core/api/Provider/Provider/SaveStaffMileageAsync`;
        return this.http.post<StaffMileages>(path, { ...data });
    }

    getStaffCredentialsType() {
        const path = `/core/api/Provider/Provider/GetStaffCredentialTypes`;
        return this.http.post<StaffMemberCredentialType[]>(path, {});
    }

    saveStaffCredentialType(data: StaffMemberCredentialType) {
        const path = `/core/api/Provider/Provider/SaveStaffCredentialType`;
        return this.http.post<StaffMemberCredentialType>(path, { ...data });
    }

    deleteStaffCredentialType(item: StaffMemberCredentialType): Observable<void | RequestError> {
        const path = `/core/api/Provider/Provider/DeleteStaffCredentialType`;
        return this.http.post<void | RequestError>(path, item);
    }

    getStaffCredentials() {
        const path = `/core/api/Provider/Provider/GetStaffCredentials`;
        return this.http.post<StaffMemberCredential[]>(path, {});
    }

    saveStaffCredential(data: StaffMemberCredential): Observable<StaffMemberCredential> {
        const path = `/core/api/Provider/Provider/SaveStaffCredential`;
        return this.http.post<StaffMemberCredential>(path, { ...data });
    }

    deleteStaffCredential(item: StaffMemberCredential): Observable<void | RequestError> {
        const path = `/core/api/Provider/Provider/DeleteStaffCredential`;
        return this.http.post<void | RequestError>(path, item);
    }
}