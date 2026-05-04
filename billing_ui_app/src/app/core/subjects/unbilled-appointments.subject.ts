import { UnbilledAppointmentsRequestModel, UnbilledAppointmentsResponse } from "@core/models/billing/report-model";
import { BaseBehaviorSubject } from "./base.behavior.subject";
import { BehaviorSubject, catchError, Observable, Subject, switchMap } from "rxjs";
import { ReportService } from "@core/services/billing/report.service";

export class UnbilledAppointmentsListSubject extends BaseBehaviorSubject<UnbilledAppointmentsResponse> {
    private loadData$ = new Subject<UnbilledAppointmentsRequestModel>();
    public loading$ = new BehaviorSubject<boolean>(false);
    public totalCount = 0;
    constructor(private reportingService: ReportService) {
        super();
        this.loadData$.pipe(
            switchMap((params: UnbilledAppointmentsRequestModel) => this.reportingService.getUnbilledAppointments(params).pipe(
                catchError(err => {
                    console.error(err);
                    return [];
                })
            ))
        ).subscribe(result => {
            this.data = result.appointmentModels
            this.totalCount = result.totalCount;
            this.sync();
            this.loading$.next(false)
        });

    }

    getReport(params: UnbilledAppointmentsRequestModel) {
        this.loading$.next(true);
        this.loadData$.next(params);
    }

    
    getCount(): number {
        return this.totalCount
    }

    setCount(count: number) {
        this.totalCount = count;
    }

    getAll(params: UnbilledAppointmentsRequestModel) {
        this.loading$.next(true);
        this.loadData$.next(params);

    }

    getLoading(): Observable<boolean> {
        return this.loading$;
    }

}
