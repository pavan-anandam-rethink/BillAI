import { EventEmitter, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, BehaviorSubject } from 'rxjs';
import { HttpService } from '@core/services/http.service'

@Injectable({
    providedIn: 'root'
})
export class ChildPickerService {

    private childProfileIdSubject = new BehaviorSubject<number | null>(null);
    readonly childProfileId: Observable<number | null> = this.childProfileIdSubject.asObservable();
    private childProfileListSubject = new BehaviorSubject<any[]>([]);
    readonly chidpickerList: Observable<any[]> = this.childProfileListSubject.asObservable();
    private childpickerListSnapshotLoaded = false;
    isEditMode = new EventEmitter<boolean>()
    unsubscribeAll$: any;

    constructor(
        private http: HttpService,
        private router: Router
    ) { }

    setChildProfileId(id: number) {
        this.childProfileIdSubject.next(id);
    }

    getChildPickerData(): Observable<any[]> {
        if (this.childpickerListSnapshotLoaded === false) {
            this.childpickerListSnapshotLoaded = true;
            this.http.post('/core/api/WorkArea/AccountMember/GetChildPickerData', {})
                .subscribe((x: any) => {
                    this.childProfileListSubject.next(x);
                });
        }

        return this.chidpickerList;
    };

    updateChildPickerData() {
        this.childpickerListSnapshotLoaded = false;
    }
}