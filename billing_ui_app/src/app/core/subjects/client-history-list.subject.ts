import { BehaviorSubject, Observable, of, Subject } from 'rxjs';
import { catchError, switchMap, takeUntil } from 'rxjs/operators';
import { BaseBehaviorSubject } from './base.behavior.subject';
import { ClientHistory, ClientHistoryRequestModel } from '@core/models/billing/client-history';
import { ClientHistoryService } from '@core/services/billing/client-history.service';

export class ClientHistoryListSubject extends BaseBehaviorSubject<ClientHistory> {
    protected totalCount: number = 0;
    protected destroy$ = new Subject<void>();
    private loadData$ = new Subject<ClientHistoryRequestModel>();
    public loading$ = new BehaviorSubject<boolean>(false);

    private appendData$ = new Subject<ClientHistoryRequestModel>();
    private buffer: ClientHistory[] = [];
    private isVirtualMode = false;

    constructor(private clientHistoryService: ClientHistoryService) {
        super();
        this.loading$.next(true);
        this.loadData$.pipe(
            takeUntil(this.destroy$),
            switchMap((params: ClientHistoryRequestModel) => this.clientHistoryService.GetAllClientHistoryDetails(params, true).pipe(
                catchError(err => {
                    console.error(err);
                    return of({ clientHistoryResponse: [], total: 0 });
                })
            ))
        ).subscribe(result => {
            this.totalCount = result.total;

            if (this.isVirtualMode) {
                this.buffer = result.clientHistoryResponse; // first batch only
                this.data = this.buffer;
            } else {
                this.data = result.clientHistoryResponse;
            }

            this.loading$.next(false);
            this.sync();
        });

        this.appendData$.pipe(
            takeUntil(this.destroy$),
            switchMap(params =>
                this.clientHistoryService.GetAllClientHistoryDetails(params, false).pipe(
                    catchError(err => {
                        console.error(err);
                        return of({ clientHistoryResponse: [], total: 0 });
                    })
                )
            )
        )
        .subscribe(result => {
            this.totalCount = result.total;
            this.buffer = this.buffer.concat(result.clientHistoryResponse);
            this.data = this.buffer;
            this.sync();
        });
    }
    
    async getAll(params: ClientHistoryRequestModel, virtual: boolean = false) {
        this.isVirtualMode = virtual;
        this.loading$.next(true);
        if (virtual) {
            this.buffer = [];
            this.data = [];
            this.sync();
        }
        this.loadData$.next(params);
    }

    append(params: ClientHistoryRequestModel) {
        if (!this.isVirtualMode) return;
        this.appendData$.next(params);
    }

    prependBatch(params: ClientHistoryRequestModel, showSpinner: boolean = false): void {
        if (!this.isVirtualMode) return;
        if (showSpinner) {
            this.loading$.next(true);
        }
        this.clientHistoryService.GetAllClientHistoryDetails(params, false)
            .pipe(takeUntil(this.destroy$))
            .subscribe(result => {
                this.totalCount = result.total;
                this.buffer = result.clientHistoryResponse.concat(this.buffer);
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