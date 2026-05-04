import { BehaviorSubject, Observable, Subject, of } from 'rxjs';
import { catchError, switchMap, takeUntil } from 'rxjs/operators';
import { BaseBehaviorSubject } from './base.behavior.subject';
import { ClientHistoryService } from '@core/services/billing/client-history.service';
import { ClientHistoryChargeDetails, ClientHistoryChargeDetailsRequestModel } from '@core/models/billing/client-history';

export class ClientChargeHistorySubject extends BaseBehaviorSubject<ClientHistoryChargeDetails> {
  private loadData$ = new Subject<ClientHistoryChargeDetailsRequestModel>();
  private appendData$ = new Subject<ClientHistoryChargeDetailsRequestModel>();
  public loading$ = new BehaviorSubject<boolean>(false);
  public totalCount = 0;
  private buffer: ClientHistoryChargeDetails[] = [];
  private isVirtualMode = false;
  private destroy$ = new Subject<void>();

  constructor(private clientChargeService: ClientHistoryService) {
    super();
    this.loading$.next(true);

    // Initial and normal loads (show spinner)
    this.loadData$
      .pipe(
        takeUntil(this.destroy$),
        switchMap((params: ClientHistoryChargeDetailsRequestModel) =>
          this.clientChargeService
            .GetClientHistoryChargeDetails(params, true)
            .pipe(
              catchError((err) => {
                console.error(err);
                return of({ chargeDetails: [], total: 0 });
              })
            )
        )
      )
      .subscribe((result) => {
        this.totalCount = result.total;
        if (this.isVirtualMode) {
          this.buffer = result.chargeDetails; // first batch only
          this.data = this.buffer;
        } else {
          this.data = result.chargeDetails;
        }
        this.sync();
        this.loading$.next(false);
      });

    // Append loads (suppress spinner)
    this.appendData$
      .pipe(
        takeUntil(this.destroy$),
        switchMap((params: ClientHistoryChargeDetailsRequestModel) =>
          this.clientChargeService
            .GetClientHistoryChargeDetails(params, false)
            .pipe(
              catchError((err) => {
                console.error(err);
                return of({ chargeDetails: [], total: 0 });
              })
            )
        )
      )
      .subscribe((result) => {
        this.totalCount = result.total;
        this.buffer = this.buffer.concat(result.chargeDetails);
        this.data = this.buffer;
        this.sync();
      });
  }

  getReport(params: ClientHistoryChargeDetailsRequestModel) {
    this.loading$.next(true);
    this.loadData$.next(params);
  }

  async getAll(params: ClientHistoryChargeDetailsRequestModel, virtual: boolean = false) {
    this.isVirtualMode = virtual;
    this.loading$.next(true);
    if (virtual) {
      this.buffer = [];
      this.data = [];
      this.sync();
    }
    this.loadData$.next(params);
  }

  append(params: ClientHistoryChargeDetailsRequestModel) {
    if (!this.isVirtualMode) return;
    this.appendData$.next(params);
  }

  prependBatch(params: ClientHistoryChargeDetailsRequestModel, showSpinner: boolean = false): void {
    if (!this.isVirtualMode) return;
    if (showSpinner) {
      this.loading$.next(true);
    }
    this.clientChargeService
      .GetClientHistoryChargeDetails(params, false)
      .pipe(takeUntil(this.destroy$))
      .subscribe((result) => {
        this.totalCount = result.total;
        this.buffer = result.chargeDetails.concat(this.buffer);
        this.data = this.buffer;
        this.sync();
        if (showSpinner) {
          this.loading$.next(false);
        }
      });
  }

  removeFromTop(count: number): void {
    if (!this.isVirtualMode) return;
    this.buffer = this.buffer.slice(count);
    this.data = this.buffer;
    this.sync();
  }

  removeFromBottom(count: number): void {
    if (!this.isVirtualMode) return;
    this.buffer = this.buffer.slice(0, this.buffer.length - count);
    this.data = this.buffer;
    this.sync();
  }

  getCount(): number {
    return this.totalCount;
  }

  getLoading(): Observable<boolean> {
    return this.loading$;
  }

  getDataLength(): number {
    return this.data.length;
  }

  clearBuffer(): void {
    this.buffer = [];
    this.data = [];
    this.sync();
  }
}
