import { BehaviorSubject, EMPTY, Observable, Subject } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { BaseBehaviorSubject } from './base.behavior.subject';
import {
  paymentsAdjustmentsRequestModel,
  PaymentsAdjustmentsResponse,
} from '@core/models/billing/report-model';
import { ReportService } from '@core/services/billing/report.service';


export class PaymentsAdjustmentsListSubject extends BaseBehaviorSubject<PaymentsAdjustmentsResponse> {
  private loadData$ = new Subject<{
    params: paymentsAdjustmentsRequestModel;
    mode: 'replace' | 'append' | 'prepend';
  }>();
  private buffer: PaymentsAdjustmentsResponse[] = [];
  private loading$ = new BehaviorSubject<boolean>(false); // spinner
  private virtualLoading$ = new BehaviorSubject<boolean>(false); // no spinner
  public totalCount = 0;
    private isVirtualMode = false;
  private virtualSkip = 0;  
  private loadedVirtualPages = new Set<number>();  

  constructor(private reportingService: ReportService) {
    super();

    this.loadData$
      .pipe(
        switchMap(({ params, mode }) => {
          mode === 'replace'? this.loading$.next(true)
            : this.virtualLoading$.next(true);
            this.virtualSkip = params.skip;
          if (mode === 'append' || mode === 'prepend') {
             const pageNumber = Math.floor(params.skip / 50);
            if(mode === 'append')
           this.loadedVirtualPages.add(pageNumber);
            else
            this.loadedVirtualPages.delete(pageNumber);
           
          } else if (mode === 'replace') {
            this.loadedVirtualPages.clear();
            this.loadedVirtualPages.add(0); 
          }
          return this.reportingService
            .getPaymentsAdjustments(params,false)
            .pipe(
               map(result => ({ result, mode, sort: params.sortingModels }))
            );
        })
      )
     .subscribe(({ result, mode, sort }) => {
      let incoming = result.paymentsAdjustments ?? [];
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
        this.buffer = this.sortData(this.buffer, sort);
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

  getReport(params: paymentsAdjustmentsRequestModel) {
    this.loadData$.next({ params, mode: 'replace' });
  }

  append(params: paymentsAdjustmentsRequestModel) {
     this.loadData$.next({ params, mode: 'append' });
   }
 
   prepend(params: paymentsAdjustmentsRequestModel) {
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

  getCount(): number {
    return this.totalCount;
  }

  isPageLoaded(skip: number): boolean {
    const pageNumber = Math.floor(skip / 50);
    return this.loadedVirtualPages.has(pageNumber);
  }

  clearLoadedPages(): void {
    this.loadedVirtualPages.clear();
  }

   setCount(count: number): void {
    this.totalCount = count;
  }

  getDataLength() {
    return this.data.length;
  }
   getLoadedPages(): Set<number> {
    return this.loadedVirtualPages;
  }

  getLoading(): Observable<boolean> {
    return this.loading$;
  }

  getVirtualLoading(): Observable<boolean> {
    return this.virtualLoading$;
  }

  getVirtualLoadingValue(): boolean {
    return this.virtualLoading$.value;
  }
sortData(data: any[], sort: any[]): any[] {
  if (!sort || sort.length === 0) return data;
  const { field, dir } = sort[0];

  if (field !== 'clientFirst' && field !== 'clientLast') {
    return data;
  }

  const direction = dir === 'desc' ? -1 : 1;
  return [...data].sort((a, b) => {
    let valA = a[field];
    let valB = b[field];

    const isNullA = valA == null || valA === '';
    const isNullB = valB == null || valB === '';

    if (isNullA && isNullB) return 0;
    if (isNullA) return 1;
    if (isNullB) return -1;

    if (valA > valB) return 1 * direction;
    if (valA < valB) return -1 * direction;

    return 0;
  });
}
}
