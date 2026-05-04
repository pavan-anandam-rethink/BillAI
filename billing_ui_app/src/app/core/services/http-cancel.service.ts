import { Subject }  from 'rxjs/internal/Subject';
import { Injectable } from '@angular/core';

@Injectable()
export class HttpCancelService {
    private cancelPendingRequestSubject = new Subject<void>()
    cancelPendingRequest$ = this.cancelPendingRequestSubject.asObservable();

    cancelPendingRequest() {
        this.cancelPendingRequestSubject.next();
    }
}