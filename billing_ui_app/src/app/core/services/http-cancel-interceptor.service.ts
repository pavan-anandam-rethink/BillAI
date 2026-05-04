import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable }  from 'rxjs/internal/Observable';
import { Injectable } from '@angular/core';
import { HttpCancelService } from './http-cancel.service';
import { takeUntil, tap }  from 'rxjs/operators';

@Injectable()
export class CancelInterceptor implements HttpInterceptor {
    constructor(private cancelService: HttpCancelService) {
    }

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        return next.handle(req).pipe(takeUntil(this.cancelService.cancelPendingRequest$
            .pipe(tap(() => { throw new Error('Request is canceled') }))));
    }
}