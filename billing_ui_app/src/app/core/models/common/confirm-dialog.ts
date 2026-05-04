import { BehaviorSubject, Observable } from "rxjs";

export class ConfirmDialog {
    public subject = new BehaviorSubject<any | null>(null);
    readonly result: Observable<any | null> = this.subject.asObservable();
    constructor(public opened: boolean, public title: string, public message: string, public confirmText = "Yes",
                public cancelText = "No")
    {
    }
}