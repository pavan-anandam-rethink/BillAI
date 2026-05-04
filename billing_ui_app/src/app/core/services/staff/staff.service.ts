import { Injectable, EventEmitter } from '@angular/core';
import { Observable, BehaviorSubject, Subject, of, throwError } from 'rxjs';

import { HttpService } from '@core/services';
import { GridDataSet } from '@core/models/common';
import {
    ShortInfo,
    StaffAttributes,
    StaffFilter,
    StaffListItem,
    StaffSearch,
    StaffReminderSettingsData,
    StaffSignature,
    StaffPreferences
} from '@core/models/staff';
import { AssignType, ClientAssigment } from '@core/models/common/assignments';
import { StaffMemberCredentialType } from '@core/models/company-account';
import { Helper } from '@common/common-helper';


@Injectable({
    providedIn: 'root'
})
export class StaffService {
    private readonly unsubscribeAll$ = new Subject();
    public $viewModeChanged = new BehaviorSubject<boolean>(false);
    public $reloadGeneralInfo: EventEmitter<boolean> = new EventEmitter<boolean>();
    private staffIdSubject = new BehaviorSubject<number | null>(null);
    readonly staffId: Observable<number | null> = this.staffIdSubject.asObservable();

    constructor(private http: HttpService) {
        this.$reloadGeneralInfo = new EventEmitter();
    }

    ngOnDestroy(): void {
        this.unsubscribeAll$.next(void 0);
        this.unsubscribeAll$.complete();
    }

    setStaffId(id: number) {
        this.staffIdSubject.next(id);
    }

    getAssignedClients(staffId: number): Observable<ClientAssigment[]> {
        return this.http.post<ClientAssigment[]>('/core/api/staff/staff/getassignedclients', staffId);
    }

    saveStaffClientAssignment(staffId: number, clientId: number, assignType: AssignType): Observable<any> {
        return this.http.post('/core/api/staff/staff/saveStaffClientAssignment', { staffMemberId: staffId, clientId: clientId, type: assignType });
    }

    getUnassignedClients(staffId: number): Observable<ClientAssigment[]> {
        return this.http.post<ClientAssigment[]>('/core/api/staff/staff/getUnassignedClientsForEdit', staffId);
    }

    getAllClients(staffId: number): Observable<ClientAssigment[]> {
        return this.http.post<ClientAssigment[]>('/core/api/staff/staff/getAllClientsForEdit', staffId);
    }

    getAttributes(staffId: number): Observable<StaffAttributes> {
        return this.http.post('/core/api/staff/staff/getadditionalstaffinfo', staffId);
    }

    //getAvailability(staffId: number): Observable<any> {
    //    return this.http.post('/core/api/staff/staff/getadditionalstaffinfo', staffId);
    //}

    getFilter(): Observable<StaffSearch> {
        return this.http.post('/core/api/staff/staff/getstaffsearchoptions', {});
    }

    getGeneralInformation(staffMemberId: number): Observable<any> {
        return this.http.post('/core/api/staff/staff/getstaffinfo', staffMemberId);
    }

    saveSchedulingDetail(data: any): Observable<any> {
        return this.http.post('/core/api/staff/staff/SaveSchedulingDetail', data);
    }

    checkStaffUsage(staffId: number) {
        return this.http.post('/core/api/staff/staff/checkStaffUsage', staffId);
    }

    saveGeneralInformation(model: any, propagatingData: any): Observable<any> {
        return this.http.post('/core/api/staff/staff/saveStaffInfo', { model: model, propagatingData: propagatingData });
    }

    uploadFile(memberId: number, file: any): Observable<any> {
        const data = new FormData();
        data.append('memberId', memberId.toString());
        data.append('file', file.rawFile);
        return this.http.post('/core/api/staff/staff/uploadFile', data);
    }

    getStaffList(filter: StaffFilter,
        sort: number = 0,
        desc: boolean = false,
        skip: number = 0,
        take: number = 10
    ): Observable<GridDataSet<StaffListItem>> {
        return this.http.post('/core/api/staff/staff/getstaff', { ...filter, skip, take, sort, descending: desc });
    }

    getStaffShortInfo(staffMemberId: number): Observable<ShortInfo> {
        return this.http.post<ShortInfo>('/core/api/staff/staff/getstaffdetails', staffMemberId);
    }

    saveAttributes(value: any): Observable<any> {
        const payload = {
            id: value.id,
            dateOfBirth: value.dob,
            startDate: value.startDate,
            genderId: value.gender,
            experienceTypeId: value.experience,
            months: value.months,
            canHandleAggression: value.canHandleAggression,
            ageGroups: value.ageGroups || [],
            languages: value.languages || [],
            numberOfClients: value.maximumCaseload
        };

        return this.http.post('/core/api/staff/staff/saveadditionaldetails', payload);
    }

    getPayrolls(staffMemberId: number): Observable<any> {
        return this.http.post('/core/api/staff/staff/getPayrolls', staffMemberId);
    }

    checkPayrolUsage(staffId: number, payroll: any): Observable<boolean> {
        if (!payroll.paycodeId) {
            return of(false);
        }

        return this.http.post('/core/api/staff/staff/checkPayrolUsage', { staffMemberId: staffId, ...payroll });
    }

    savePayroll(staffId: number, payroll: any): Observable<any> {
        payroll.eve
        return this.http.post('/core/api/staff/staff/savepayroll', {
            staffMemberId: staffId,
            ...payroll,
            effectiveDate: Helper.shiftDateToUTC(payroll.effectiveDate),
            endDate: Helper.shiftDateToUTC(payroll.endDate)
        });
    }

    deletePayroll(payrollId: number): Observable<any> {
        return this.http.post('/core/api/staff/staff/deletePayroll', payrollId);
    }

    saveStaffSalary(staffId: number, formGroup: any) {
        return this.http.post('/core/api/staff/staff/saveStaffSalary', { staffMemberId: staffId, ...formGroup });
    }

    getSignature(staffId: number): Observable<StaffSignature> {
        return this.http.post('/core/api/staff/staff/getStaffSignature', staffId);
    }

    saveSignature(signature: StaffSignature): Observable<StaffSignature> {
        return this.http.post('/core/api/staff/staff/saveStaffSignature', signature);
    }

    getStaffReminderSettings(staffMemberId: number): Observable<StaffReminderSettingsData> {
        return this.http.post<StaffReminderSettingsData>('/core/api/staff/staff/GetStaffReminderSettingsAsync', staffMemberId);
    }

    saveStaffReminderSettings(data: StaffReminderSettingsData): Observable<StaffReminderSettingsData> {
        return this.http.post<StaffReminderSettingsData>('/core/api/staff/staff/SaveStaffReminderSettingsAsync', data);
    }

    getStaffCredentials(
        staffMemberId: number,
        sort: number = 0,
        desc: boolean = false): Observable<any> {
        var data = {
            memberId: staffMemberId,
            sort: sort,
            descending: desc
        };
        return this.http.post('/core/api/staff/staff/GetStaffCredentials', data);
    }

    getStaffCredentialTypes() {
        return this.http.post<StaffMemberCredentialType[]>('/core/api/staff/staff/GetStaffCredentialTypes', {});
    }

    getStaffCredentialsList(): Observable<any> {
        return this.http.post('/core/api/staff/staff/GetStaffCredentialsList', {});
    }

    saveStaffCredential(data: any): Observable<any> {
        return this.http.post('/core/api/staff/staff/SaveStaffCredential', data);
    }

    deleteStaffCredential(id: number): Observable<any> {
        return this.http.post('/core/api/staff/staff/DeleteStaffCredential', id);
    }

    dismissStaffCredentialWarning(id: number): Observable<any> {
        return this.http.post('/core/api/staff/staff/DismissStaffCredentialWarning', id);
    }

    setDefaultStaffCredential(id: number): Observable<any> {
        return this.http.post('/core/api/staff/staff/SetDefaultStaffCredential', id);
    }

    uploadLicenceAttachmentFile(id: number, file: any): Observable<any> {
        const data = new FormData();
        data.append('staffMemberCredentialId', id.toString());
        data.append('file', file.rawFile);
        return this.http.post('/core/api/staff/staff/UploadLicenceAttachmentFile', data);
    }

    deleteLicenceAttachmentFile(id: number): Observable<any> {
        return this.http.post('/core/api/staff/staff/DeleteLicenceAttachmentFile', id);
    }

    isCredentialDeleteable(id: number, staffId: number): Observable<any> {
        return this.http.post('/core/api/staff/staff/isCredentialDeleteable', { id, staffId });
    }

    getStaffMembers(): Observable<any> {
        return this.http.get('/core/api/staff/staff/GetStaffMembers');
    }

    credentialDateCanBeChanged(id: number, staffId: number, issueDate: Date, expiredDate: Date): Observable<boolean> {
        return this.http.post('/core/api/staff/staff/credentialDateCanBeChanged', { id, staffId, issueDate, expiredDate });
    }

    getStaffPreferences(memberId: number): Observable<StaffPreferences> {
        return this.http.post('/core/api/staff/staff/getStuffMemberPreferences', memberId);
    }

    updateStaffPreferences(memberId: number, preferences: StaffPreferences): Observable<any> {
        if (!preferences) {
            return throwError(new Error('preferences cannot be null'));
        }

        return this.http.post('/core/api/staff/staff/updateStuffMemberPreferences', { memberId: memberId, ...preferences });
    }

    getStaffName(staffId: number): Observable<string> {
        return this.http.post('/core/api/staff/staff/getStaffName', staffId);
    }
}