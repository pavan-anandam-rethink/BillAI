
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { switchMap, map, catchError } from 'rxjs/operators';
import { BaseBehaviorSubject } from './base.behavior.subject';
import {
  AccountsReceivablesChargeLevelRequestModel,
  AccountsReceivablesChargeLevelResponse,
} from '@core/models/billing/report-model';
import { ReportService } from '@core/services/billing/report.service';

type LoadMode = 'replace' | 'append';
export class AccountsReceivablesChargeLevelResponseModel {
  accountsReceivables: AccountsReceivablesChargeLevelResponse[];
  totalCount: number;
}

export class AccountsReceivablesChargeLevelSubject extends BaseBehaviorSubject<AccountsReceivablesChargeLevelResponse> {
  private loadData$ = new Subject<{
    params: AccountsReceivablesChargeLevelRequestModel;
    mode: 'replace' | 'append' | 'prepend';
  }>();
  private buffer: AccountsReceivablesChargeLevelResponse[] = [];

  private loading$ = new BehaviorSubject<boolean>(false); // FULL spinner
  private virtualLoading$ = new BehaviorSubject<boolean>(false); // scroll append
  public totalCount = 0;
   private isVirtualMode = false;
  private virtualSkip = 0;  
  private loadedVirtualPages = new Set<number>();

  constructor(private reportingService: ReportService) {
    super();
        const PAGE_SIZE = 50;
const WINDOW_SIZE = 200;
    this.loadData$
      .pipe(
        switchMap(({ params, mode }) => {
          mode === 'replace'? this.loading$.next(true)
            : this.virtualLoading$.next(true);
            this.virtualSkip = params.skip;
           if (mode === 'append' || mode === 'prepend') {
          const pageNumber = Math.floor(params.skip / PAGE_SIZE);
        if(mode === 'append')
        this.loadedVirtualPages.add(pageNumber);
        else
        this.loadedVirtualPages.delete(pageNumber);
        
      } else if (mode === 'replace') {
        this.loadedVirtualPages.clear();
        this.loadedVirtualPages.add(0);  // First page always loaded
      }

          return this.reportingService
            .getAccountsReceivablesChargeLevel(params, false)
            .pipe(
              map(result => ({ result, mode })), 
              catchError(err => {
                console.error(err);
                this.loading$.next(false);
                this.virtualLoading$.next(false);
                return [];
              })
            );
        })
      )
      .subscribe(({ result, mode }) => {
      const incoming : AccountsReceivablesChargeLevelResponse[] = (result as any).accountsReceivables ?? [];
      if (mode === 'replace') {
        this.data = incoming;
        this.buffer = incoming;
      } 
      else if (mode === 'append') {
        this.buffer = this.buffer.concat(incoming);
        if (this.buffer.length > 200) {
          const excess = this.buffer.length - 200;
          this.buffer = this.buffer.slice(excess);  // Remove excess from TOP
        }
        this.data = this.buffer;
      } 
      else if (mode === 'prepend') {
        this.buffer = incoming.concat(this.buffer);
        this.buffer = incoming.concat(this.buffer);
        if(this.data.length ==200){
          const excess = this.buffer.length - 150;
          this.buffer = this.buffer.slice(0, this.buffer.length - excess); 
        }
        else if (this.buffer.length > 200) {
          const excess = this.buffer.length - 200;
          this.buffer = this.buffer.slice(0, this.buffer.length - excess); 
        }
        this.data = this.buffer;
      }

    this.totalCount = result.totalCount ?? this.totalCount;

    this.loading$.next(false);
    this.virtualLoading$.next(false);
    this.sync();
  });
  }

  /** Initial load / filter / sort */
  getReport(params: AccountsReceivablesChargeLevelRequestModel): void {
    this.loadData$.next({ params, mode: 'replace' });
  }

  /** Virtual scroll append */
  append(params: AccountsReceivablesChargeLevelRequestModel): void {
    this.loadData$.next({ params, mode: 'append' });
  }
  prepend(params: AccountsReceivablesChargeLevelRequestModel) {
      this.loadData$.next({ params, mode: 'prepend' });
    }
    removeFromTop(count: number) {
    this.buffer = this.buffer.slice(count);
    this.data = this.buffer;
    this.sync();
  }

  removeFromBottom(count: number) {
    this.buffer = this.buffer.slice(0, this.buffer.length - count);
    this.data = this.buffer;
    this.sync();
  }

  setVirtualMode(v: boolean) {
    this.isVirtualMode = v;
  }
   setCount(count: number): void {
    this.totalCount = count;
  }

  /** Total rows on server */
  getCount() {
    return this.totalCount;
  }

  /** Rows already loaded */
 getDataLength() {
    return this.data.length;
  }


  isPageLoaded(skip: number): boolean {
    const pageNumber = Math.floor(skip / 50);
    return this.loadedVirtualPages.has(pageNumber);
  }

  getLoadedPages(): Set<number> {
    return this.loadedVirtualPages;
  }

  clearLoadedPages(): void {
    this.loadedVirtualPages.clear();
  }
  /** Spinner observable (FIRST LOAD ONLY) */
  getLoading(): Observable<boolean> {
    return this.loading$;
  }

  /** Virtual scroll loading observable */
  getVirtualLoading(): Observable<boolean> {
    return this.virtualLoading$;
  }

  /** Used by scroll guard */
  getVirtualLoadingValue() {
    return this.virtualLoading$.value;
  }
}
