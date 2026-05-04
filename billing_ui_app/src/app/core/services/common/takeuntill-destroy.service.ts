import { Injectable } from "@angular/core";
import { Observable, Subject } from "rxjs";

@Injectable({
    providedIn: 'root'
})
export class TakeUntilDestroyService {
    private destroy$ = new Subject<void>();

    constructor() { }

    public get destroy(): Observable<void> {
        return this.destroy$.asObservable();
    }

    public markForDestruction(): void {
        this.destroy$.next();
    }

    public ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }
}