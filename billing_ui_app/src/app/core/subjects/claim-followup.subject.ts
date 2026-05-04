import { BehaviorSubject, Observable, Subject,EMPTY } from 'rxjs';
import { catchError, switchMap, map } from 'rxjs/operators';
import { BaseBehaviorSubject } from './base.behavior.subject';
import { ReportService } from '@core/services/billing/report.service';
import { ClaimFollowUpResponseModel, claimFollowUpRequestModel } from '../models/billing/report-model';


export class ClaimFollowupListSubject extends BaseBehaviorSubject<ClaimFollowUpResponseModel> {
  //private loadData$ = new Subject<claimFollowUpRequestModel>();
   private loadData$ = new Subject<{
      params: claimFollowUpRequestModel;
      mode: 'replace' | 'append' | 'prepend';
    }>();
  public loading$ = new BehaviorSubject<boolean>(false);
  public totalCount = 0;
   
   private loadedVirtualPages = new Set<number>();  
  private virtualLoading$ = new BehaviorSubject<boolean>(false);

  private buffer: ClaimFollowUpResponseModel[] = [];
 
  private isVirtualMode = false;
  private virtualSkip = 0; 
  constructor(private reportingService: ReportService) {
    super();
    this.loading$.next(true);
    
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

      return this.reportingService
        .getClaimFollowup(params,false)
        .pipe(map(result => ({ result, mode })));
    })
  )
  .subscribe(({ result, mode }) => {
     const incoming : ClaimFollowUpResponseModel[] = (result as any).claimFollowUps ?? [];

      if (mode === 'replace') {
        this.data = incoming;
        this.buffer = incoming;
      } 
      else if (mode === 'append') {
        this.buffer = this.buffer.concat(incoming);
        if (this.buffer.length > 200) {
          const excess = this.buffer.length - 200;
          this.buffer = this.buffer.slice(excess); 
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

  getReport(params: claimFollowUpRequestModel) {
    this.loadData$.next({ params, mode: 'replace' });
  }


  getCount(): number {
    return this.totalCount;
  }

  getAll(params: claimFollowUpRequestModel) {
    this.loadData$.next({ params, mode: 'replace' });
  }
   setVirtualMode(v: boolean) {
    this.isVirtualMode = v;
  }
   getVirtualLoading(): Observable<boolean> {
    return this.virtualLoading$;
  }
  getDataLength() {
    return this.data.length;
  }
   getLoadedPages(): Set<number> {
    return this.loadedVirtualPages;
  }
  append(params: claimFollowUpRequestModel) {
       this.loadData$.next({ params, mode: 'append' });
     }
      prepend(params: claimFollowUpRequestModel) {
          this.loadData$.next({ params, mode: 'prepend' });
        }
   setCount(count: number): void {
    this.totalCount = count;
  }
  getLoading(): Observable<boolean> {
    return this.loading$;
  }
    getVirtualLoadingValue(): boolean {
    return this.virtualLoading$.value;
  }

}
