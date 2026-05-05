import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';


@Injectable({
    providedIn: 'root'
})
export class ErrorPopupService {
    public errorObj = {
        isShow: false,
        message: ""
    }
    private readonly errorSubject = new BehaviorSubject<any>(this.errorObj);
    private numLoadings = 0;

    errorState = this.errorSubject.asObservable();

    show(error: any) {
        this.numLoadings++;
        var errorObj = {
            isShow: true,
            message: error.message
        }
        this.errorSubject.next(errorObj);
    }

    hide() {
        if ((--this.numLoadings) <= 0) {
            this.numLoadings = 0;
            var errorObj = {
                isShow: false,
                message: 'The error has occurred'
            }
            this.errorSubject.next(errorObj);
        }
    }

    isLoading() {
        var value = this.errorSubject.getValue(); 
        return value.isShow;
    }
}