import { ListFilterSort, PaymentPosting } from '@core/models/billing';
import { PaymentPostingService } from '@core/services/billing';
import { BehaviorSubject, Observable, of, Subject } from 'rxjs';
import { catchError, switchMap, takeUntil } from 'rxjs/operators';
import { BaseBehaviorSubject } from './base.behavior.subject';


export class PaymentPostingListSubject extends BaseBehaviorSubject<PaymentPosting> {
    protected totalCount: number = 0;
    protected destroy$ = new Subject<void>();
    private loadData$ = new Subject<ListFilterSort>();
    public loading$ = new BehaviorSubject<boolean>(false);
    protected isRevSpringEnabled: boolean = false;

    private appendData$ = new Subject<ListFilterSort>();
    private buffer: PaymentPosting[] = [];
    private isVirtualMode = false;

    constructor(private paymentPostingService: PaymentPostingService) {
        super();
        this.loading$.next(true);
        this.loadData$.pipe(
            takeUntil(this.destroy$),
            switchMap((params: ListFilterSort) => this.paymentPostingService.getAll(params).pipe(
                catchError(err => {
                    console.error(err);
                    return of({ data: [], totalCount: 0 });
                })
            ))
        ).subscribe(result => {
            this.totalCount = result.totalCount;
            this.isRevSpringEnabled = ('isRevSpringEnabled' in result ? result.isRevSpringEnabled : false) ?? false;

            if (this.isVirtualMode) {
                this.buffer = result.data;   // first 50 rows only
                this.data = this.buffer;
            } else {
                this.data = result.data;
            }

            this.loading$.next(false);
            this.sync();
        });

        this.appendData$.pipe(
            takeUntil(this.destroy$),
            switchMap(params =>
                this.paymentPostingService.getAll(params, false).pipe(
                    catchError(err => {
                        console.error(err);
                        return of({ data: [], totalCount: 0 });
                    })
                )
            )
        )
        .subscribe(result => {
            this.totalCount = result.totalCount;
            this.isRevSpringEnabled = ('isRevSpringEnabled' in result ? result.isRevSpringEnabled : this.isRevSpringEnabled) ?? this.isRevSpringEnabled;
            this.buffer = this.buffer.concat(result.data);
            this.data = this.buffer;
            this.sync();
        });
    }
    
 
    async getAll(params: ListFilterSort, virtual: boolean = false) {
        this.isVirtualMode = virtual;

        // Indicate loading state whenever a new request is made
        this.loading$.next(true);

        // When entering virtual mode (e.g., "All"), clear current data immediately
        // so old, unfiltered rows are not displayed or mixed with new filtered results.
        if (virtual) {
            this.buffer = [];
            this.data = [];
            this.sync();
        }

        this.loadData$.next(params);
    }

    append(params: ListFilterSort) {
        if (!this.isVirtualMode) {
        return;
    }
        this.appendData$.next(params);
    }

    prependBatch(params: ListFilterSort, showSpinner: boolean = false): void {
        if (!this.isVirtualMode) return;

        if (showSpinner) {
            this.loading$.next(true);
        }

        this.paymentPostingService.getAll(params, false)
            .pipe(takeUntil(this.destroy$))
            .subscribe(result => {
                this.totalCount = result.totalCount;
                this.isRevSpringEnabled = ('isRevSpringEnabled' in result ? result.isRevSpringEnabled : this.isRevSpringEnabled) ?? this.isRevSpringEnabled;
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
        return this.totalCount
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

    public getRevSpringEnabled(): boolean {
        return this.isRevSpringEnabled;
    }
}