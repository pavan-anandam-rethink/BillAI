import { BehaviorSubject, Observable, of, Subject } from 'rxjs';
import { catchError, switchMap, takeUntil } from 'rxjs/operators';
import { BaseBehaviorSubject } from './base.behavior.subject';
import { PaymentPostingService } from '@core/services/billing/payment-posting.service';
import { BulkPaymentResponse, Charge, Adjustment } from '@core/models/billing/bulk-payment-response';

interface BulkPostingParams {
  skip: number;
  take: number;
}

export class BulkPostingListSubject extends BaseBehaviorSubject<Charge> {
  protected totalCount: number = 0;
  protected destroy$ = new Subject<void>();
  private loadData$ = new Subject<BulkPostingParams>();
  public loading$ = new BehaviorSubject<boolean>(false);

  private appendData$ = new Subject<BulkPostingParams>();
  private buffer: Charge[] = [];
  private isVirtualMode = false;

  private allIds: number[] = [];

  constructor(private paymentPostingService: PaymentPostingService, ids: number[]) {
    super();
    this.allIds = ids || [];
    this.totalCount = this.allIds.length;

    this.loadData$
      .pipe(
        takeUntil(this.destroy$),
        switchMap((p) => this.fetchSlice(p).pipe(
          catchError((err) => {
            console.error(err);
            return of([] as Charge[]);
          })
        ))
      )
      .subscribe((firstBatch) => {
        if (this.isVirtualMode) {
          this.buffer = firstBatch;
          this.data = this.buffer;
        } else {
          this.data = firstBatch;
        }
        this.loading$.next(false);
        this.sync();
      });

    this.appendData$
      .pipe(
        takeUntil(this.destroy$),
        switchMap((p) => this.fetchSlice(p).pipe(
          catchError((err) => {
            console.error(err);
            return of([] as Charge[]);
          })
        ))
      )
      .subscribe((nextBatch) => {
        this.buffer = this.buffer.concat(nextBatch);
        this.data = this.buffer;
        this.sync();
      });
  }

  async getAll(params: BulkPostingParams, virtual: boolean = false) {
    this.isVirtualMode = virtual;
    this.loading$.next(true);

    if (virtual) {
      this.buffer = [];
      this.data = [];
      this.sync();
    }
    this.loadData$.next(params);
  }

  append(params: BulkPostingParams) {
    if (!this.isVirtualMode) return;
    this.appendData$.next(params);
  }

  prependBatch(params: BulkPostingParams, showSpinner: boolean = false): void {
    if (!this.isVirtualMode) return;
    if (showSpinner) this.loading$.next(true);

    this.fetchSlice(params).pipe(takeUntil(this.destroy$)).subscribe((prevBatch) => {
      this.buffer = prevBatch.concat(this.buffer);
      this.data = this.buffer;
      this.sync();
      if (showSpinner) this.loading$.next(false);
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

  private fetchSlice(p: BulkPostingParams) {
    const start = Math.max(0, p.skip || 0);
    const end = p.take && p.take > 0 ? Math.min(this.allIds.length, start + p.take) : this.allIds.length;
    const slice = this.allIds.slice(start, end);
    if (slice.length === 0) return of([] as Charge[]);

    return this.paymentPostingService.getBulkDataForIds(slice).pipe(
      switchMap((resp) => of(this.mapToCharges(resp)))
    );
  }

  private mapToCharges(resp: BulkPaymentResponse[]): Charge[] {
    return (resp || []).map((r) => ({
      id: r.id,
      clientName: r.patientName || '',
      serviceDate: r.dateOfService,
      procedureCode: r.procedure || '',
      modifiers: r.mods || '',
      billedAmount: r.billedAmount,
      allowedAmount: r.allowedAmount,
      writeOff: r.writeOff,
      paymentAmount: (r.paidAmount ?? 0) as number,
      adjustments: this.initAdjustments(r)
    } as Charge));
  }

  private initAdjustments(_r: BulkPaymentResponse): Adjustment[] {
    // Start empty; component ensures at least 3 slots
    return [] as Adjustment[];
  }
}
