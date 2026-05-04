import { BehaviorSubject, Observable, of, Subject } from 'rxjs';
import { catchError, switchMap, takeUntil } from 'rxjs/operators';
import { BaseBehaviorSubject } from './base.behavior.subject';
import { ClientHistoryService } from '@core/services/billing/client-history.service';
import { ClientInvoiceHistoryDetails, ClientInvoiceHistoryRequestModel } from '@core/models/billing/client-history';

export class ClientInvoiceHistorySubject extends BaseBehaviorSubject<ClientInvoiceHistoryDetails> {
  protected totalCount: number = 0;
  protected destroy$ = new Subject<void>();
  private loadData$ = new Subject<ClientInvoiceHistoryRequestModel>();
  public loading$ = new BehaviorSubject<boolean>(false);

  private appendData$ = new Subject<ClientInvoiceHistoryRequestModel>();
  private buffer: ClientInvoiceHistoryDetails[] = [];
  private isVirtualMode = false;

  constructor(private clientChargeService: ClientHistoryService) {
    super();
    this.loading$.next(true);

    // Initial and normal loads (show spinner)
    this.loadData$
      .pipe(
        takeUntil(this.destroy$),
        switchMap((params) =>
          this.clientChargeService.getClientInvoiceHistory(params, true).pipe(
            catchError((err) => {
              console.error(err);
              return of({ data: [], totalCount: 0 });
            })
          )
        )
      )
      .subscribe((result) => {
        this.totalCount = result.totalCount;
        if (this.isVirtualMode) {
          this.buffer = result.data; // first batch only
          this.data = this.buffer;
        } else {
          this.data = result.data;
        }
        this.sync();
        this.loading$.next(false);
      });

    // Append loads (suppress spinner)
    this.appendData$
      .pipe(
        takeUntil(this.destroy$),
        switchMap((params) =>
          this.clientChargeService.getClientInvoiceHistory(params, false).pipe(
            catchError((err) => {
              console.error(err);
              return of({ data: [], totalCount: 0 });
            })
          )
        )
      )
      .subscribe((result) => {
        this.totalCount = result.totalCount;
        this.buffer = this.buffer.concat(result.data);
        this.data = this.buffer;
        this.sync();
      });
  }

  async getAll(params: ClientInvoiceHistoryRequestModel, virtual: boolean = false) {
    this.isVirtualMode = virtual;
    this.loading$.next(true);
    if (virtual) {
      this.buffer = [];
      this.data = [];
      this.sync();
    }
    this.loadData$.next(params);
  }

  append(params: ClientInvoiceHistoryRequestModel) {
    if (!this.isVirtualMode) return;
    this.appendData$.next(params);
  }

  prependBatch(params: ClientInvoiceHistoryRequestModel, showSpinner: boolean = false): void {
    if (!this.isVirtualMode) return;
    if (showSpinner) {
      this.loading$.next(true);
    }
    this.clientChargeService.getClientInvoiceHistory(params, false)
      .pipe(takeUntil(this.destroy$))
      .subscribe(result => {
        this.totalCount = result.totalCount;
        this.buffer = result.data.concat(this.buffer);
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
