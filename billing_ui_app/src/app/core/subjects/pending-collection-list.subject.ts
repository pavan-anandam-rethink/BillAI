import { BehaviorSubject, Observable, of, Subject } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { BaseBehaviorSubject } from './base.behavior.subject';
import { InvoiceDetailsModel, PatientInvoiceDetails , PatientInvoiceHeader, PatientInvoiceHeaderSearch } from '@core/models/billing/patient-invoice';
import { PatientInvoiceService } from '@core/services/billing/patient-invoice.service';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';


export class PendingCollectionListSubject extends BaseBehaviorSubject<InvoiceDetailsModel> {
    private loadData$ = new Subject<{ params: PatientInvoiceHeaderSearch; virtualScroll: boolean }>();
    private appendData$ = new Subject<PatientInvoiceHeaderSearch>();
    private buffer: InvoiceDetailsModel[] = [];
    private isVirtualScrollMode = false;
    private userList = [];
    private allUsers$ = new BehaviorSubject<ClaimFilterOptionModel[]>([]);

    public totalCount = 0;

    constructor(private patientInvoiceService: PatientInvoiceService) {
        super();
        this.loadData$.pipe(
            // Always show loader for load/refresh (tab switch, filters, initial ALL)
            switchMap(({ params, virtualScroll }) => this.patientInvoiceService.getInvoiceDetails(params, true).pipe(
                map(result => ({ result, params, virtualScroll })),
                catchError(err => {
                    console.error(err);
                    return of({ result: { data: [], totalCount: 0, userList: [] } as any, params, virtualScroll });
                })
            ))
        ).subscribe(({ result, params, virtualScroll }) => {
            if (virtualScroll) {
                this.isVirtualScrollMode = true;
                this.buffer = result.data || [];
                this.data = this.buffer;
            } else {
                this.data = result.data || [];
            }
            
            this.data.forEach(x => x.checked = false);
            this.userList = result.userList || [];
            this.userList.forEach(x => x.checked = false);
            this.totalCount = result.totalCount || 0;
            
            if (result.userList && result.userList.length) {
                const currentAllUsers = this.allUsers$.getValue();
                if ((currentAllUsers.length === 0 && result.userList && result.userList.length) ||
                    (result.userList && result.userList.length > currentAllUsers.length)) {
                    this.allUsers$.next([...result.userList]);
                }
            }
            this.sync();
        });

        // Handle append operations for virtual scroll
        this.appendData$.pipe(
            switchMap((params: PatientInvoiceHeaderSearch) => 
                this.patientInvoiceService.getInvoiceDetails(params, false).pipe(
                    map(result => ({ result, params })),
                    catchError(err => {
                        console.error('Error loading data for virtual scroll:', err);
                        return of({ result: { data: [], totalCount: this.totalCount } as any, params });
                    })
                )
            )
        ).subscribe(({ result, params }) => {
            if (result && result.data && result.data.length > 0 && this.isVirtualScrollMode) {
                const chunk = result.data;
                this.buffer = this.buffer.concat(chunk);
                this.data = this.buffer;
            }
            this.sync();
        });
    }

    getAll(params: PatientInvoiceHeaderSearch, virtualScroll = false) {
        this.isVirtualScrollMode = virtualScroll;
        if (virtualScroll) {
            this.buffer = [];
        }
        this.loadData$.next({ params, virtualScroll });
    }

    append(params: PatientInvoiceHeaderSearch) {
        this.appendData$.next(params);
    }

    prependBatch(params: PatientInvoiceHeaderSearch): Observable<void> {
        return this.patientInvoiceService.getInvoiceDetails(params, false).pipe(
            map(result => {
                if (result && result.data && result.data.length > 0 && this.isVirtualScrollMode) {
                    const chunk = result.data;
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

    getDataLength(): number {
        return this.data.length;
    }

    getCount(): number {
        return this.totalCount
    }

    getAllUsers$() {
       return this.allUsers$.asObservable();
    }

    getUserList(): ClaimFilterOptionModel[] {
      return this.userList;
    }

    clearUserList(): void {
        this.userList = [];
    }

    fetchAllUsersFromApi(): void {
        const param = new PatientInvoiceHeaderSearch();
        this.patientInvoiceService.getInvoiceDetails(param).subscribe(result => {
            if (result && result.userList && result.userList.length) {
                this.allUsers$.next([...result.userList]);
            }
        });
    }

}