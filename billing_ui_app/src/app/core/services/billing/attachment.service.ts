import {Injectable} from "@angular/core";
import {HttpService} from "@core/services";
import {HttpErrorResponse, HttpEvent, HttpEventType, HttpResponse} from "@angular/common/http";
import {throwError} from "rxjs";
import {IdFilterSort} from "@core/models/billing/id-filter-sort";
import { RenameAttachmentModel } from "./encounter-attachment.service";
import { IdWithUserInfo } from "@core/models/billing/get-claim-by-identifier";
import { environment } from "src/environments/environment";
import { AccountMemberService } from "../account/account-member.service";

export class IUploadResult {
    status: string;
    progress: number;
    result: string = '';
}

@Injectable()
export class AttachmentService {
    private apiBaseUrl = environment.claimApiBaseUrl;

    constructor(private http: HttpService, private accountService: AccountMemberService) {

    }
    
    public renameAttachment(attachment: RenameAttachmentModel){
        return this.http.post(this.apiBaseUrl + '/PaymentAttachment/RenameAttachment', attachment);
    }

    public getPaymentAttachments(gridState: IdFilterSort, showSpinner: boolean = true) {
        return this.http.post<any>(
            this.apiBaseUrl + '/PaymentAttachment/GetPaymentAttachments',
            gridState,
            { showSpinner }
        );
    }

    public downloadFile(id: number) {
        this.http.post<HttpResponse<Object>>(this.apiBaseUrl + '/PaymentAttachment/GetFileUpload', id, {
            responseType: 'arraybuffer' as 'json',
            observe: 'response' as 'body'
        })
            .subscribe(x => {
                let contentType = x.headers.get('Content-Type') || "";
                let filename = this.getFileNameFromHttpResponse(x);

                if (contentType != "" && filename != "") {
                    this.writeContents(x.body, filename, contentType);
                }
            });
    }
    public downloadEraFile(id: number) {
        var model = {
            id: id,
            accountInfoId: this.accountService.memberDetails.accountInfoId,
            memberId: this.accountService.memberDetails.memberId
        }
        this.http.post<HttpResponse<Object>>(this.apiBaseUrl + '/PaymentPosting/GetFileUpload', model, {
            responseType: 'arraybuffer' as 'json',
            observe: 'response' as 'body'
        })
            .subscribe(x => {
                let contentType = x.headers.get('Content-Type') || "";
                let filename = this.getFileNameFromHttpResponse(x);

                if (contentType != "" && filename != "") {
                    this.writeContents(x.body, filename, contentType);
                }
            });
    }

    private getFileNameFromHttpResponse(httpResponse: HttpResponse<Object>) {
        let contentDispositionHeader = httpResponse.headers.get('Content-Disposition');
        if (contentDispositionHeader == undefined)
            return "";
        let result = contentDispositionHeader.split(';')[1].trim().split('=')[1];
        return result.replace(/"/g, '');
    }

    writeContents(content: any, fileName: string, contentType: string) {
        const a = document.createElement('a');
        const file = new Blob([content], {type: contentType});
        a.href = URL.createObjectURL(file);
        a.download = fileName;
        a.click();
    }

    // public uploadEra(formData: FormData): Observable<IUploadResult> {
    //     return this.http.post<any>(this.apiBaseUrl + '/PaymentPosting/UploadFile', formData, {
    //             reportProgress: true,
    //             observe: 'events' as 'body'
    //         },
    //         true)
    //         .pipe(
    //             map((event: HttpEvent<any>) => this.getEventMessage(event, formData)),
    //             catchError(this.handleError)
    //         );
    // }

    // public uploadFiles(formData: FormData): Observable<IUploadResult> {
    //     return this.http.post<any>(this.apiBaseUrl + '/PaymentAttachment/UploadFile', formData, {
    //             reportProgress: true,
    //             observe: 'events' as 'body'
    //         },
    //         true)
    //         .pipe(
    //             map((event: HttpEvent<any>) => this.getEventMessage(event, formData)),
    //             catchError(this.handleError)
    //         );
    // }
    
    // public uploadClaimFiles(formData: FormData): Observable<IUploadResult> {
    //     return this.http.post<any>(this.apiBaseUrl + '/ClaimAttachment/UploadFile', formData, {
    //             reportProgress: true,
    //             observe: 'events' as 'body'
    //         },
    //         true)
    //         .pipe(
    //             map((event: HttpEvent<any>) => this.getEventMessage(event, formData)),
    //             catchError(this.handleError)
    //         );
    // }

    public deleteUpload(model: IdWithUserInfo) {
        return this.http.post(this.apiBaseUrl + '/PaymentAttachment/DeleteUpload', model);
    }

    public deleteEraUpload(id: number) {
        var model = {
            id: id,
            accountInfoId: this.accountService.memberDetails.accountInfoId,
            memberId:  this.accountService.memberDetails.memberId,
        }
        return this.http.post(this.apiBaseUrl + '/PaymentPosting/DeleteUpload', model);
    }

    public deleteUploads(ids: number[]) {
        var model = {
            ids: ids,
            accountInfoId: this.accountService.memberDetails.accountInfoId,
            memberId:  this.accountService.memberDetails.memberId,
        }
        return this.http.post(this.apiBaseUrl + '/PaymentAttachment/DeleteUploads', model);
    }

    private getEventMessage(event: HttpEvent<any>, formData: FormData): IUploadResult {
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

}