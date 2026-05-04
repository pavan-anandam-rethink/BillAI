import { ClaimListFilterSort, RemovePaymentClaims } from '@core/models/billing';
import { PaymentClaims } from '@core/models/billing/cliam-posting';
import { ClaimPostingService } from '@core/services/billing';
import { BaseBehaviorSubject } from './base.behavior.subject';
import { BehaviorSubject, Observable, Subject, of } from 'rxjs';
import { catchError, switchMap, takeUntil } from 'rxjs/operators';
import { InsuranceClaimListFilterSort } from '@core/models/billing/claim-posting-filter-sort';


export class ClaimPostingListSubject extends BaseBehaviorSubject<PaymentClaims> {
    private loadData$ = new Subject<InsuranceClaimListFilterSort>();
    public totalCount = 0;
    public loading$ = new BehaviorSubject<boolean>(false);

    private appendData$ = new Subject<InsuranceClaimListFilterSort>();
    private buffer: PaymentClaims[] = [];
    private isVirtualMode = false;
    private destroy$ = new Subject<void>();
    private windowStart = 0;

    constructor(private claimPostingService: ClaimPostingService) {
        super();
        this.loading$.next(true);

        this.loadData$
            .pipe(
                takeUntil(this.destroy$),
                switchMap((params: InsuranceClaimListFilterSort) =>
                    this.claimPostingService.getAll(params, true).pipe(
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
                switchMap((params: InsuranceClaimListFilterSort) =>
                    this.claimPostingService.getAll(params, false).pipe(
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

    async getAll(params: InsuranceClaimListFilterSort, virtual: boolean = false) {
        this.isVirtualMode = virtual;
        this.loading$.next(true);
        if (virtual) {
            this.buffer = [];
            this.data = [];
            this.sync();
        }
        this.loadData$.next(params);
    }

    append(params: InsuranceClaimListFilterSort) {
        if (!this.isVirtualMode) return;
        this.appendData$.next(params);
    }

    prependBatch(params: InsuranceClaimListFilterSort, showSpinner: boolean = false): void {
        if (!this.isVirtualMode) return;
        if (showSpinner) {
            this.loading$.next(true);
        }
        this.claimPostingService
            .getAll(params, showSpinner)
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

    async delete(claim: PaymentClaims, paymentId: number) {
        if (claim.id > 0) {
            let model: RemovePaymentClaims = {
                paymentClaimsIds: [claim.id],
                paymentId: paymentId
            }
            return new Promise((resolve,rejects)=>{
                this.claimPostingService.deleteByIds(model).subscribe((res:any)=>{
                    this.remove(claim);
                    this.sync();
                    resolve(res);
                })
            })
        } else {
             this.sync();
        }
    }

    updateClaim(id: number) {
        this.claimPostingService.getClaimPostingDetails(id).subscribe(x => {
            let claim = this.data.find(x => x.id == id);
            if (claim != undefined) {
                claim.paidAmount = x.paidAmount;
                claim.patientResponsibility = x.patientResponsibility;
                claim.allowedAmount = x.allowedAmount;
                claim.billedAmount = x.billedAmount;
                claim.balance = x.balance;
                this.sync();
            }
        })
    }

    deleteSelected(model: RemovePaymentClaims) {
        if (model && model.paymentClaimsIds.length > 0) {
            this.claimPostingService.deleteSelectedClaims(model);
        } else {
            this.sync();
        }
    }
}