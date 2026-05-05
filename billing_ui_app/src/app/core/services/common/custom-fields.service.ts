import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HttpService } from '@core/services';
import { CustomField } from '@core/models/common';
import { CustomFieldShort } from '@core/models/common/custom-fields';
import { MemberType } from '@core/models/company-account/memberType';


@Injectable({
    providedIn: 'root'
})
export class CustomFieldsService {

    memberType: MemberType = MemberType.client;
    typeTitle() {
        return this.memberType === MemberType.staff ? 'STAFF' : 'CLIENT';
    }

    constructor(private http: HttpService) { }

    getCustomFields(): Observable<CustomField[]> {
        return this.http.post('/core/api/Provider/Provider/GetCustomFields', this.memberType);
    }

    save(customField: CustomField): Observable<CustomField[]> {
        const url = this.memberType === MemberType.staff ? 'SaveStaffCustomFieldsAsync' : 'SaveClientCustomFieldsAsync';
        return this.http.post<CustomField[]>(`/core/api/Provider/Provider/${url}`, customField);
    }

    check(customField: CustomFieldShort): Observable<CustomField> {
        const url = this.memberType === MemberType.staff ? 'GetStaffCustomFieldDataByLabelAndType' : 'GetClientCustomFieldDataByLabelAndType';
        return this.http.post<CustomField>(`/core/api/Provider/Provider/${url}`, customField);
    }

    saveReorder(id: number, up: boolean): Observable<CustomField[]> {
        const url = this.memberType === MemberType.staff ? 'SaveStaffCustomFieldsOrdering' : 'SaveCLientCustomFieldsOrdering';
        return this.http.post<CustomField[]>(`/core/api/Provider/Provider/${url}`, { id, up });
    }

    delete(customFieldId: number): Observable<CustomField[]> {
        const url = this.memberType === MemberType.staff ? 'DeleteStaffCustomFieldsAsync' : 'DeleteClientCustomFieldsAsync';
        return this.http.post<CustomField[]>(`/core/api/Provider/Provider/${url}`, customFieldId);
    }
}