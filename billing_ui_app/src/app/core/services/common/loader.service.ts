import { Injectable } from '@angular/core';
import { BehaviorSubject }  from 'rxjs/internal/BehaviorSubject';


@Injectable({
    providedIn: 'root'
})
export class LoaderService {
    private readonly loaderSubject = new BehaviorSubject<boolean>(false);
    private readonly validationLoaderSubject = new BehaviorSubject<boolean>(false);
    private numLoadings = 0;

    loaderState = this.loaderSubject.asObservable();
    validationLoaderState = this.validationLoaderSubject.asObservable();

    show(isValidationApi: boolean = false) {
        this.numLoadings++;
        isValidationApi === true ? this.validationLoaderSubject.next(true) : this.loaderSubject.next(true);
    }

    hide() {
        if ((--this.numLoadings) <= 0) {
            this.numLoadings = 0;     
            this.validationLoaderSubject.next(false);
            this.loaderSubject.next(false);       
        }
    }

    isLoading() {
        return this.loaderSubject.getValue();
    }
}