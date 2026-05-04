import { HttpResponse, HttpEvent, HttpEventType, HttpErrorResponse } from "@angular/common/http";
import { Injectable } from "@angular/core";
import { IdFilterSort } from "@core/models/billing/id-filter-sort";
import { HttpService } from "@core/services";
import { map, catchError } from "rxjs/operators";
import { IUploadResult } from "../billing/attachment.service";
import { throwError } from "rxjs";

@Injectable()
export class AuthAttachmentService {
    controller: AbortController;
    signal: AbortSignal;

    constructor(private http: HttpService) {
        this.controller = new AbortController();
        this.signal = this.controller.signal;
    }

    public rename(attachmentId: number, fileName: string) {
        return this.http.post('/core/api/Client/AuthorizationAttachment/Rename', { id: attachmentId, fileName: fileName });
    }

    public getAll(authId: number) {
        return this.http.post<any>('/core/api/Client/AuthorizationAttachment/GetAll', authId);
    }

    public downloadFile(id: number) {
        this.http.post<HttpResponse<Object>>('/core/api/Client/AuthorizationAttachment/Get', id)
            .subscribe((x: any) => {
                setTimeout(() => window.open(x.downloadUrl, '_blank'), 1000);
            });
    }

    public upload(formData: FormData) {
        return this.http.post<any>('/core/api/Client/AuthorizationAttachment/Upload', formData, {
            reportProgress: true,
            observe: 'events' as 'body',
        },
            true)
            .pipe(
                map((event: HttpEvent<any>) => this.getEventMessage(event, formData)),
                catchError(this.handleError)
            );
    }
    private getEventMessage(event: HttpEvent<any>, formData: FormData): IUploadResult {
        switch (event.type) {
            case HttpEventType.UploadProgress:
                return this.fileUploadProgress(event);
            case HttpEventType.Response:
                return this.apiResponse(event);
            default:
                return { status: `${event.type}`, progress: 0, result: '' };
        }
    }

    private fileUploadProgress(event: any): IUploadResult {
        const percentDone = Math.round(100 * event.loaded / event.total);
        return { status: 'progress', progress: percentDone, result: '' };
    }

    private apiResponse(event: any): IUploadResult {
        return { status: "done", progress: 100, result: event.body };
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

    public delete(id: number) {
        return this.http.post('/core/api/Client/AuthorizationAttachment/Delete', id);
    }

    public bulkDelete(ids: number[]) {
        return this.http.post('/core/api/Client/AuthorizationAttachment/BulkDelete', ids);
    }

}