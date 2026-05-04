import {ClaimService} from '@core/services/billing';
import {BaseBehaviorSubject} from './base.behavior.subject';
import {BehaviorSubject, Observable, Subject, of} from 'rxjs';
import {catchError, switchMap, map, tap} from 'rxjs/operators';
import { ClaimHeader, ClaimHeaderSearch } from '@core/models/billing/claim-header-search';
import { ClaimsCount } from '@core/models/billing/claims-count';


export class ClaimDashboardListSubject extends BaseBehaviorSubject<ClaimHeader> {

    private loadData$ = new Subject<ClaimHeaderSearch>();
    private appendData$ = new Subject<ClaimHeaderSearch>();
    private buffer: ClaimHeader[] = [];
    private isVirtualScrollMode = false;
    public totalCount = 0;
    public claimsCountForTabs: ClaimsCount = {
        pendingReviewTotalCount: 0,
        readyToBillTotalCount: 0,
        billingPendingTotalCount: 0,
        closedTotalCount: 0,
        rejectedTotalCount: 0,
        deniedTotalCount: 0,
        flaggedTotalCount: 0
    }

    constructor(private claimService: ClaimService) {
       super();
        // Handle normal pagination - replace data
        this.loadData$.pipe(
            switchMap((params: ClaimHeaderSearch) => {
                // Show spinner for normal pagination or if explicitly set in params
                // Hide spinner for virtual scroll mode unless it's an initial load (tab change)
                const showSpinner = (params as any).showSpinner !== undefined 
                    ? (params as any).showSpinner 
                    : !this.isVirtualScrollMode;
                return this.claimService.getClaimHeaders(params, showSpinner).pipe(
                    map(result => ({ result, params })),
                    catchError(err => {
                        console.error(err);
                        return of({ result: { data: [], totalCount: 0, claimsCount: this.claimsCountForTabs } as any, params });
                    })
                );
            })
        ).subscribe(({ result, params }) => {
            if (result.claimsCount) {   
                this.claimsCountForTabs = result.claimsCount;
            }
            this.totalCount = result.totalCount;
            
            if (this.isVirtualScrollMode) {
                // Virtual scroll mode: buffer data for ALL selection
                const take = (params && params.take) ? params.take : (result.data ? result.data.length : 0);
                this.buffer = (result.data || []).slice(0, take);
                this.data = this.buffer;
            } else {
                // Normal pagination: just show current page
                this.buffer = [];
                this.data = result.data;
            }
            this.sync();
            this.claimService.setOldFilter(false);
            this.claimService.setTabId(0);
        });

        // Handle virtual scroll append - add more data as user scrolls
        this.appendData$.pipe(
            switchMap((params: ClaimHeaderSearch) => {
                return this.claimService.getClaimHeaders(params, false).pipe(
                    map(result => ({ result, params })),
                    catchError(err => {
                        console.error('Error loading claims for virtual scroll:', err);
                        return of({ result: { data: [], totalCount: this.totalCount, claimsCount: this.claimsCountForTabs } as any, params });
                    })
                );
            })
        ).subscribe(({ result, params }) => {
            if (result.claimsCount) {   
                this.claimsCountForTabs = result.claimsCount;
            }
            this.totalCount = result.totalCount;
            
            // Append new data to buffer
            if (result.data && result.data.length > 0 && this.isVirtualScrollMode) {
                const take = (params && params.take) ? params.take : result.data.length;
                const chunk = (result.data || []).slice(0, take);
                this.buffer = this.buffer.concat(chunk);
                this.data = this.buffer;
            }
            // Always sync to trigger view update and reset loading state
            this.sync();
        });
    }

    getAll(params: ClaimHeaderSearch, virtualScroll = false) {
        // Reset virtual scroll mode and buffer state
        this.isVirtualScrollMode = virtualScroll;
        // Always start fresh - clear buffer
        this.buffer = [];
        this.loadData$.next(params);
    }

    append(params: ClaimHeaderSearch) {
        this.appendData$.next(params);
    }

    prependBatch(params: ClaimHeaderSearch, showSpinner: boolean = false): Observable<void> {
        return this.claimService.getClaimHeaders(params, showSpinner).pipe(
            map(result => {
                if (result.claimsCount) {
                    this.claimsCountForTabs = result.claimsCount;
                }
                this.totalCount = result.totalCount;

                // Prepend new data to the beginning of buffer
                if (result.data && result.data.length > 0 && this.isVirtualScrollMode) {
                    const take = params.take || result.data.length;
                    const chunk = (result.data || []).slice(0, take);
                    this.buffer = chunk.concat(this.buffer);
                    this.data = this.buffer;
                }
                this.sync();
            }),
            catchError(err => {
                console.error('Error prepending batch:', err);
                return of(void 0);
            })
        );
    }

    removeFromTop(count: number): void {
        if (count <= 0 || count >= this.buffer.length) return;
        this.buffer = this.buffer.slice(count);
        this.data = this.buffer;
        this.sync();
    }

    removeFromBottom(count: number): void {
        if (count <= 0 || count >= this.buffer.length) return;
        this.buffer = this.buffer.slice(0, this.buffer.length - count);
        this.data = this.buffer;
        this.sync();
    }

    clearBuffer() {
        this.buffer = [];
        this.isVirtualScrollMode = false;
    }

    getCount(): number {
        return this.totalCount
    }

    getClaimsCountForTabs(): ClaimsCount {
        return this.claimsCountForTabs
    }

    getDataLength(): number {
        return this.data.length;
    }
}
