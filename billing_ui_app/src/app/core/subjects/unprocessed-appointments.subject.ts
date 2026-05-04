import { UnbilledAppointmentsRequestModel, UnbilledAppointmentsResponse, UnprocessedAppointmentsResponseModel } from "@core/models/billing/report-model";
import { BaseBehaviorSubject } from "./base.behavior.subject";
import { BehaviorSubject, catchError, Observable, Subject, switchMap } from "rxjs";
import { ReportService } from "@core/services/billing/report.service";

export class UnprocessedAppointmentsListSubject extends BaseBehaviorSubject<UnprocessedAppointmentsResponseModel> {
  private loadData$ = new Subject<UnbilledAppointmentsRequestModel>();
  public loading$ = new BehaviorSubject<boolean>(false);
  public totalCount = 0;
  constructor(private reportingService: ReportService) {
    super();
    this.loadData$.pipe(
      switchMap((params: UnbilledAppointmentsRequestModel) => this.reportingService.getUnprocessedAppointments(params).pipe(
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
    const cleanedParams = this.cleanParams(params);
    this.loadData$.next(cleanedParams);

  }

  cleanParams(params: any) {
    const arrayKeys = [
      'payerOrFunder',
      'clients',
      'staff',
      'location',
      'placeOfService',
      'sortingModels'
    ];

    const cleaned = { ...params };

    arrayKeys.forEach(key => {

      if (Array.isArray(cleaned[key])) {
        cleaned[key] = cleaned[key].filter((x: any) => x != null && x !== '');
      }

      else if (cleaned[key] == null) {
        delete cleaned[key];
      }

    });

    return cleaned;
  }

  getLoading(): Observable<boolean> {
    return this.loading$;
  }

}
