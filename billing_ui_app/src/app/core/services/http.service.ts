import { Injectable } from '@angular/core';
import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { Observable }  from 'rxjs/internal/Observable';
import { of }  from 'rxjs/internal/observable/of';
import { EMPTY }  from 'rxjs/internal/observable/empty';
import { tap, catchError, finalize, switchMap }  from 'rxjs/operators';
import { LoaderService } from './common/loader.service';
import { ErrorHandlingService } from './error-handling/error-handling.service';
import { Subject } from 'rxjs';


export interface IRequestOptions {
    headers?: HttpHeaders;
    context?: HttpContext;
    observe?: 'body';
    params?: any;
    reportProgress?: boolean;
    responseType?: 'json';
    withCredentials?: boolean;
    body?: any;
    showSpinner?: boolean;
}

const defaultOptions: IRequestOptions = {
    showSpinner: true
}


@Injectable({
    providedIn: 'root'
})
export class HttpService {
    private apiUrlSubject = new Subject<string>();
    apiUrl$ = this.apiUrlSubject.asObservable();

    private _apiUrl: string = '';

    constructor(
        private loaderService: LoaderService, 
        public http: HttpClient,
        private errorHandler: ErrorHandlingService
    ) { }

    public get<T>(url: string, options?: IRequestOptions, loadingSpinner: boolean = true, propagateError: boolean = false): Observable<T> {
        if (loadingSpinner) {
            this.loaderService.show();
        }

        this.setApiUrl(url);
        
        options = {
            ...defaultOptions,
            ...options
        };

        return of({}).pipe(
            tap(x => this.onStart('', options)),
            switchMap(x => this.http.get<T>(url, options)),
            catchError((error, caught) => this.onCatch()),

            tap((res: any) => { },
            (error: any) => {
                this.onError(error);
            }),
            finalize(() => {
                if (loadingSpinner !== false) {
                    this.loaderService.hide();
                }
                this.onEnd(options);
            }),
        )
    }

    public post<T>(url: string, params: any, options?: IRequestOptions,
        isWithAddClaim: boolean = false): Observable<T> {
        options = {
            ...defaultOptions,
            ...options
        };

        this.setApiUrl(url);
        
        var n = url.lastIndexOf('/');
        var result = url.substring(n + 1);

        return of({}).pipe(
            tap(x => this.onStart(result, options, isWithAddClaim)),
            switchMap(x => this.http.post<T>(url, params, options)),
            catchError((error, caught) => {
                throw error;
            }),
            tap((res: any) => { },
                (error: any) => {
                    this.onError(error);
                }),
            finalize(() => {
                this.onEnd(options);
            }),
        )
    }

    private onCatch() {
        return EMPTY;
    }

    private onError(res: any): void {
        console.log('Error, status code: ' + res.status);
        this.errorHandler.handleError(res.error);
        this.loaderService.hide();
    }

    private onStart(apiName: string, options?: IRequestOptions, isWithAddClaim: boolean = false): void { 
        if (apiName === 'ValidateClaimData' && !isWithAddClaim) {
            options && options.showSpinner && this.loaderService.show(true);
        } else {
            options && options.showSpinner && this.loaderService.show(false);
        }
    }

    private onEnd(options?: IRequestOptions): void { 
        options && options.showSpinner && this.loaderService.hide();
    }

    setApiUrl(url: string) {
        this._apiUrl = url;
        this.apiUrlSubject.next(url);
    }

    getApiUrl(): string {
        return this._apiUrl;
    }
}


