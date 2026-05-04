import { Injectable } from '@angular/core';
import { Observable } from 'rxjs/internal/Observable';
import { catchError, map } from 'rxjs/operators';
import { HttpService } from '../http.service';
import { ClaimAttachment } from '@core/models/billing';
import { HttpErrorResponse, HttpEvent, HttpEventType } from '@angular/common/http';
import { throwError } from 'rxjs';
import { IdWithUserInfo, UserInfo } from '@core/models/billing/get-claim-by-identifier';
import { environment } from 'src/environments/environment';
import { AccountMemberService } from '../account/account-member.service';

export class FileToUpload extends UserInfo {
    FileName: string = "";
    FileMimeType: string = "";
    ClaimId?: number = 0;
    paymentId?: number = 0;
    Data: string = "";
}

export class IUploadResult {
    status: string;
    progress: number;
    result: string = '';
}

export class IdField {
    Id: number;
}

export class RenameAttachmentModel extends UserInfo {
    AttachmentId: number;
    FileName: string;
}

@Injectable({
    providedIn: 'root'
})
export class EncounterAttachmentService {

    private apiBaseUrl = environment.claimApiBaseUrl;
    constructor(private http: HttpService, private accountService: AccountMemberService) {
    }

    private mapList(encounterAttachments: ClaimAttachment[]): ClaimAttachment[] {
        encounterAttachments.forEach((encounterAttachment) => {
            encounterAttachment.dateCreated = new Date(encounterAttachment.dateCreated);
            return encounterAttachment;
        });
        return encounterAttachments;
    }

    public GetForEncounter(gridState: IdWithUserInfo): Observable<ClaimAttachment[]> {
        return this.http.post<any>(this.apiBaseUrl + '/ClaimAttachment/GetForClaim', gridState);
    }


    public Save(claimId: number, encounterAttachments: ClaimAttachment[]): Observable<ClaimAttachment[]> {
        return this.http.post<ClaimAttachment[]>("/core/api/Billing/ClaimAttachment/Save", {
            claimId,
            EncounterAttachments: encounterAttachments
        }).pipe(map(this.mapList));
    }

    public Delete(encounterAttachment: ClaimAttachment) {
        return this.http.post<ClaimAttachment>("/core/api/Billing/ClaimAttachment/Delete", encounterAttachment);
    }

    public uploadFiles(fileToUpload: FileToUpload, path: String): Observable<IUploadResult> {
        var model = {
            ...fileToUpload,
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId
        }
        return this.http.post<any>(this.apiBaseUrl + path, model, {
                reportProgress: true,
                observe: 'events' as 'body'
            },
            true)
            .pipe(
                map((event: HttpEvent<any>) => this.getEventMessage(event, fileToUpload)),
                catchError(this.handleError)
            );
    }

    public renameAttachment(attachment: RenameAttachmentModel) {
        return this.http.post(this.apiBaseUrl + '/ClaimAttachment/RenameAttachment',attachment);
    }


    public deleteUpload(req: IdWithUserInfo) {
        req.AccountInfoId = this.accountService.memberDetails.accountInfoId;
        req.MemberId = this.accountService.memberDetails.memberId;
        return this.http.post(this.apiBaseUrl + '/ClaimAttachment/DeleteUpload', req);
    }

    public getFileUpload(req: IdWithUserInfo) {
        req.AccountInfoId = this.accountService.memberDetails.accountInfoId;
        req.MemberId = this.accountService.memberDetails.memberId;
        this.http.post(this.apiBaseUrl + '/ClaimAttachment/GetFileUpload', req)
            .subscribe((x: any) => {
                setTimeout(() => window.open(x.downloadUrl, '_blank'), 1000);
            });
    }

    private getEventMessage(event: HttpEvent<any>, formData: FileToUpload): IUploadResult {
        switch (event.type) {
            case HttpEventType.UploadProgress:
                return this.fileUploadProgress(event);
            case HttpEventType.Response:
                return this.apiResponse(event);
            default:
                return {status: `${event.type}`, progress: 0, result: ''};
        }
    }

    private fileUploadProgress(event: any): IUploadResult {
        const percentDone = Math.round(100 * event.loaded / event.total);
        return {status: 'progress', progress: percentDone, result: ''};
    }

    private apiResponse(event: any): IUploadResult {
        return {status: "done", progress: 100, result: event.body};
    }

    private handleError(error: HttpErrorResponse) {
        if (error.error instanceof ErrorEvent) {
            // A client-side or network error occurred. Handle it accordingly.
            console.error('An error occurred:', error.error.message);
        } else {
            // The backend returned an unsuccessful response code.
            // The response body may contain clues as to what went wrong,
            console.error(`Backend returned code ${error.status}, ` + `body was: ${error.error}`);
        }
        // return an observable with a user-facing error message
        return throwError('Something bad happened. Please try again later.');
    }

    getEdiFilesFromBlob(model: any): Observable<any> {
        return this.http.post(
            `${this.apiBaseUrl}/EdiFile/GetEdiFilesFromBlob`,
            model,
            { 
                showSpinner: true,
                responseType: 'text' as 'json'
            }
        );
    }
}