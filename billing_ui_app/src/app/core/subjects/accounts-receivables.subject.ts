import { BehaviorSubject, Observable, Subject, of } from 'rxjs';
import {map, catchError, switchMap } from 'rxjs/operators';
import { BaseBehaviorSubject } from './base.behavior.subject';
import { AccountsReceivablesRequestModel, AccountsReceivablesResponse, AccountsReceivablesResponseModel } from '@core/models/billing/report-model';
import { ReportService } from '@core/services/billing/report.service';

export class AccountsReceivablesListSubject
  extends BaseBehaviorSubject<AccountsReceivablesResponse> {

  private loadData$ = new Subject<{
    params: AccountsReceivablesRequestModel;
    mode: 'replace' | 'append' | 'prepend';
  }>();

  private buffer: AccountsReceivablesResponse[] = [];

  private loading$ = new BehaviorSubject<boolean>(false);
  private virtualLoading$ = new BehaviorSubject<boolean>(false);
  private totalCount = 0;
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
        this.loadedVirtualPages.add(0); 
      }
          return this.reportingService.getAccountsReceivables(
            params,
            false
          ).pipe(
            map(result => ({ result, mode, sort: params.sortingModels }))
          );
        })
      )
     .subscribe(({ result, mode, sort }) => {
    let incoming = result.accountsReceivables ?? [];
     incoming = this.sortData(incoming, sort);
      if (mode === 'replace') {
        this.data = incoming;
        this.buffer = incoming;
      } 
      else if (mode === 'append') {
        this.buffer = this.buffer.concat(incoming);
        this.buffer = this.sortData(this.buffer, sort);
        if (this.buffer.length > 200) {
          const excess = this.buffer.length - 200;
          this.buffer = this.buffer.slice(excess);  
        }
        this.data = this.buffer;
      } 
      else if (mode === 'prepend') {
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

  getReport(params: AccountsReceivablesRequestModel) {
    this.loadData$.next({ params, mode: 'replace' });
  }

  append(params: AccountsReceivablesRequestModel) {
    this.loadData$.next({ params, mode: 'append' });
  }

  prepend(params: AccountsReceivablesRequestModel) {
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
  getDataLength() {
    return this.data.length;
  }

  getCount() {
    return this.totalCount;
  }

  getVirtualLoadingValue() {
    return this.virtualLoading$.value;
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

  setCount(count: number): void {
    this.totalCount = count;
  }
  getLoading(): Observable<boolean> {
    return this.loading$;
  }

  sortData(data: any[], sort: any[]): any[] {
  if (!sort || sort.length === 0) return data;

  return data.sort((a, b) => {
    for (let s of sort) {
      const field = s.field;
      const dir = s.dir === 'desc' ? -1 : 1;

      let valA = (a[field] ?? '').toString().trim();
      let valB = (b[field] ?? '').toString().trim();

      if (valA === '' && valB !== '') return 1;
      if (valA !== '' && valB === '') return -1;


      let compare = 0;

      if (isNaN(valA) || isNaN(valB)) {
        compare = valA.localeCompare(valB);
      } else {
        compare = Number(valA) - Number(valB);
      }

      if (compare !== 0) return compare * dir;
    }
    return 0;
  });
}
}
