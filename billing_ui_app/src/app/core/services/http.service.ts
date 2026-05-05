import { Injectable } from '@angular/core';
import { HttpClient, HttpContext, HttpHeaders } from '@angular/common/http';
import { EMPTY, Observable, Subject, defer, throwError } from 'rxjs';
import { catchError, finalize } from 'rxjs/operators';
import { LoaderService } from './common/loader.service';
import { ErrorHandlingService } from './error-handling/error-handling.service';


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
    /** When true, forwards HTTP failures to ErrorHandlingService (legacy wrapper did not notify). */
    notifyGlobalOnError?: boolean;
}

const defaultOptions: IRequestOptions = {
    showSpinner: true,
    notifyGlobalOnError: false
};


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

    /**
     * @param propagateError When true, errors are forwarded to subscribers after global handling (default false preserves legacy swallow-on-GET behavior).
     */
    public get<T>(
        url: string,
        options?: IRequestOptions,
        loadingSpinner: boolean = true,
        propagateError: boolean = false
    ): Observable<T> {
        const merged = {
            ...defaultOptions,
            ...options
        };
        const spinnerActive = merged.showSpinner !== false && loadingSpinner !== false;

        this.setApiUrl(url);

        return defer(() => {
            if (spinnerActive) {
                this.loaderService.show(false);
            }
            return this.http.get<T>(url, merged as object).pipe(
                catchError((error) =>
                    this.handleHttpError(error, propagateError, merged.notifyGlobalOnError)),
                finalize(() => {
                    if (spinnerActive) {
                        this.loaderService.hide();
                    }
                })
            );
        });
    }

    public post<T>(
        url: string,
        params: unknown,
        options?: IRequestOptions,
        isWithAddClaim: boolean = false,
        propagateError: boolean = true
    ): Observable<T> {
        const merged = {
            ...defaultOptions,
            ...options
        };
        const spinnerActive = merged.showSpinner !== false;

        this.setApiUrl(url);

        const n = url.lastIndexOf('/');
        const result = url.substring(n + 1);

        return defer(() => {
            this.applySpinnerForRequest(result, spinnerActive, isWithAddClaim);
            return this.http.post<T>(url, params, merged as object).pipe(
                catchError((error) =>
                    this.handleHttpError(error, propagateError, merged.notifyGlobalOnError)),
                finalize(() => {
                    if (spinnerActive) {
                        this.loaderService.hide();
                    }
                })
            );
        });
    }

    /**
     * Extension point for structured logging / APM (e.g. AppInsights, Datadog RUM).
     * Replace or wrap this in a subclass or delegate to your telemetry service.
     */
    protected logHttpFailure(method: string, url: string, error: unknown): void {
        // eslint-disable-next-line no-console
        console.error(`[HttpService] ${method} failed`, {
            url,
            error,
            ...(typeof error === 'object' && error !== null && 'status' in error
                ? { status: (error as { status?: number }).status }
                : {})
        });
    }

    private applySpinnerForRequest(
        apiName: string,
        spinnerActive: boolean,
        isWithAddClaim: boolean
    ): void {
        if (!spinnerActive) {
            return;
        }
        if (apiName === 'ValidateClaimData' && !isWithAddClaim) {
            this.loaderService.show(true);
        } else {
            this.loaderService.show(false);
        }
    }

    private handleHttpError(
        error: unknown,
        propagateError: boolean,
        notifyGlobalOnError: boolean | undefined
    ): Observable<never> {
        this.logHttpFailure('HTTP', this._apiUrl, error);

        if (notifyGlobalOnError) {
            const httpLike = error as { status?: number; error?: unknown };
            if (httpLike?.status != null || httpLike?.error !== undefined) {
                this.errorHandler.handleError(httpLike.error ?? httpLike ?? error);
            } else {
                this.errorHandler.handleError(error);
            }
        }

        return propagateError ? throwError(() => error) : EMPTY;
    }

    setApiUrl(url: string) {
        this._apiUrl = url;
        this.apiUrlSubject.next(url);
    }

    getApiUrl(): string {
        return this._apiUrl;
    }
}

