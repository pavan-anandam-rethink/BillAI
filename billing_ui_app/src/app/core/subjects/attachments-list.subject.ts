import { BaseBehaviorSubject } from './base.behavior.subject';
import { BehaviorSubject, Observable, Subject, of } from 'rxjs';
import { catchError, switchMap, takeUntil } from 'rxjs/operators';
import { AttachmentService } from '@core/services/billing/attachment.service';
import { PaymentAttachment } from '@core/models/billing/payment-attachment';
import { IdFilterSort } from '@core/models/billing/id-filter-sort';

export class AttachmentsListSubject extends BaseBehaviorSubject<PaymentAttachment> {
  private loadData$ = new Subject<IdFilterSort>();
  public totalCount = 0;
  public loading$ = new BehaviorSubject<boolean>(false);

  private appendData$ = new Subject<IdFilterSort>();
  private buffer: PaymentAttachment[] = [];
  private isVirtualMode = false;
  private destroy$ = new Subject<void>();
  private windowStart = 0;

  constructor(private attachmentService: AttachmentService) {
    super();
    this.loading$.next(true);

    this.loadData$
      .pipe(
        takeUntil(this.destroy$),
        switchMap((params: IdFilterSort) =>
          this.attachmentService.getPaymentAttachments(params, true).pipe(
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
        this.loading$.next(false);
        this.sync();
      });

    this.appendData$
      .pipe(
        takeUntil(this.destroy$),
        switchMap((params: IdFilterSort) =>
          this.attachmentService.getPaymentAttachments(params, false).pipe(
            catchError((err) => {
              console.error(err);
              return of({ data: [], totalCount: 0 });
            })
          )
        )
      )
      .subscribe((result) => {
        this.totalCount = result.totalCount;
        this.buffer = this.buffer.concat(result.data || []);
        this.data = this.buffer;
        this.sync();
      });
  }

  async getAll(params: IdFilterSort, virtual: boolean = false) {
    this.isVirtualMode = virtual;
    this.loading$.next(true);
    if (virtual) {
      this.buffer = [];
      this.data = [];
      this.sync();
    }
    this.loadData$.next(params);
  }

  append(params: IdFilterSort) {
    if (!this.isVirtualMode) return;
    this.appendData$.next(params);
  }

  prependBatch(params: IdFilterSort, showSpinner: boolean = false): void {
    if (!this.isVirtualMode) return;
    if (showSpinner) {
      this.loading$.next(true);
    }
    this.attachmentService
      .getPaymentAttachments(params, showSpinner)
      .pipe(takeUntil(this.destroy$))
      .subscribe((result) => {
        this.totalCount = result.totalCount;
        this.buffer = (result.data || []).concat(this.buffer);
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

  setWindowStart(start: number): void {
    this.windowStart = start;
  }

  getWindowStart(): number {
    return this.windowStart;
  }
}
