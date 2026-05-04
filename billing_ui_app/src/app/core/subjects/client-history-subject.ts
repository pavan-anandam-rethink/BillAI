import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { BaseBehaviorSubject } from './base.behavior.subject';
import { ClientHistoryService } from '@core/services/billing/client-history.service';
import {
  ClientHistory,
  ClientHistoryRequestModel,
} from '@core/models/billing/client-history';

export class ClientHistorySubject extends BaseBehaviorSubject<ClientHistory> {
  private loadData$ = new Subject<ClientHistoryRequestModel>();
  public loading$ = new BehaviorSubject<boolean>(false);
  public totalCount = 0;
  constructor(private clientChargeService: ClientHistoryService) {
    super();
    this.loadData$
      .pipe(
        switchMap((params: ClientHistoryRequestModel) =>
          this.clientChargeService.GetAllClientHistoryDetails(params).pipe(
            catchError((err) => {
              console.error(err);
              return [];
            })
          )
        )
      )
      .subscribe((result) => {
        this.data = result.clientHistoryResponse;
        this.totalCount = result.total;
        this.sync();
        this.loading$.next(false);
      });
  }

  getReport(params: ClientHistoryRequestModel) {
    this.loading$.next(true);
    this.loadData$.next(params);
  }

  getCount(): number {
    return this.totalCount;
  }

  getAll(params: ClientHistoryRequestModel) {
    this.loading$.next(true);
    this.loadData$.next(params);
  }

  getLoading(): Observable<boolean> {
    return this.loading$;
  }
}
