import { Injectable } from '@angular/core';
import { FileTagsToFiles } from '@core/models/common/file-cabinet/file-tags-to-files';
import { FileTag } from '@core/models/common/file-tag/file-tag';
import { AccountPermissions } from '@core/enums/account';
import { DayPilot } from 'daypilot-pro-angular';
import { Observable } from 'rxjs/internal/Observable';
import { filter, finalize, map, shareReplay } from 'rxjs/operators';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { LoaderService } from '../common';
import { HttpService } from '../http.service';

@Injectable({
    providedIn: 'root'
})
export class FileTagService {
    private readonly baseApiPath: string = '/core/api/Provider/Provider';

    constructor(public http: HttpService, private loaderService: LoaderService, private accountService: AccountMemberService) { }

    canEdit$ = this.accountService
        .accountMemberSettings
        .pipe(
            filter(x => !!x),
            map(() => {
                return this.accountService.checkPermissionLevel(AccountPermissions.ProviderAccountEdit);
            }),
            shareReplay(1),
        );

    getFileTags(): Observable<FileTag[]> {
        this.loaderService.show();
        return this.http.post<FileTag[]>(`${this.baseApiPath}/GetFileTagsByAccountId`, {}).pipe(
            finalize(() => (this.loaderService.hide())
            ));
    }

    getFileTagsByFileId(id: number): Observable<FileTag[]> {
        this.loaderService.show();
        return this.http.post<FileTag[]>(`${this.baseApiPath}/GetFileTagsByFileId`, id).pipe(
            finalize(() => (this.loaderService.hide())
            ));
    }

    addFileTag(name: string): Observable<FileTag> {
        this.loaderService.show();
        return this.http.post<FileTag>(`${this.baseApiPath}/AddFileTag`, { name }).pipe(
            finalize(() => (this.loaderService.hide())
            ));
    }

    editFileTag(fileTag: FileTag): Observable<FileTag> {
        this.loaderService.show();
        return this.http.post<FileTag>(`${this.baseApiPath}/EditFileTag`, fileTag).pipe(
            finalize(() => (this.loaderService.hide())
            ));
    }

    deleteFileTag(id: number): Observable<boolean> {
        this.loaderService.show();
        return this.http.post<boolean>(`${this.baseApiPath}/DeleteFileTag`, id).pipe(
            finalize(() => (this.loaderService.hide())
            ));
    }

    getNumberOfFilesUsingTag(id: number): Observable<number> {
        this.loaderService.show();
        return this.http.post<number>(`${this.baseApiPath}/GetNumberOfFilesUsingTag`, id).pipe(
            finalize(() => (this.loaderService.hide())
            ));
    }

    tagExistingFiles(fileTagsToFiles: FileTagsToFiles): Observable<boolean> {
        this.loaderService.show();
        return this.http.post<boolean>(`${this.baseApiPath}/tagexistingfiles`, fileTagsToFiles).pipe(
            finalize(() => (this.loaderService.hide())
            ));
    }

    updateEffectiveDate(fileId: number, effectiveDate: Date | undefined): Observable<boolean> {
        let adjustedDate;
        if (effectiveDate) {
            adjustedDate = new DayPilot.Date(effectiveDate, true).getDatePart();
        }
        this.loaderService.show();
        return this.http.post<boolean>('/core/api/common/filecabinet/UpdateEffectiveDate', { fileId, effectiveDate: adjustedDate }).pipe(
            finalize(() => (this.loaderService.hide())
            ));
    }

    updateExpirationDate(fileId: number, expirationDate: Date | undefined): Observable<boolean> {
        let adjustedDate;
        if (expirationDate) {
            adjustedDate = new DayPilot.Date(expirationDate, true).getDatePart();
        }
        this.loaderService.show();
        return this.http.post<boolean>('/core/api/common/filecabinet/UpdateExpirationDate', { fileId, expirationDate: adjustedDate }).pipe(
            finalize(() => (this.loaderService.hide())
            ));
    }
}