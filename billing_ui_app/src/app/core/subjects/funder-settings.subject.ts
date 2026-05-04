import { BehaviorSubject, Observable, Subject, of } from 'rxjs';
import { map, catchError, switchMap } from 'rxjs/operators';
import { BaseBehaviorSubject } from './base.behavior.subject';
import { ReportService } from '@core/services/billing/report.service';
import { BillingFunderSettingService } from '@core/services/billing/billing-funder-setting.service'; 
import { BillingFunderListRequestModel, BillingFunderSettings, ClaimFilingIndicatorModel } from '../models/billing/billingFunderSetting-model';
import { BillingFunderSettingResponseModel } from '../models/billing/billingFunderSetting-model';
import { Injectable } from '@angular/core';


@Injectable()
export class FunderSettingsListSubject
  extends BaseBehaviorSubject<BillingFunderSettings> {

  private loadData$ = new Subject<{
    params: BillingFunderListRequestModel;
    mode: 'replace' | 'append' | 'prepend';
  }>();

  private buffer: BillingFunderSettings[] = [];

  private loading$ = new BehaviorSubject<boolean>(false);
  private virtualLoading$ = new BehaviorSubject<boolean>(false);
  private totalCount = 0;
  private timeZones: { [key: number]: string } = {};
  private claimFilingIndicatorModel: ClaimFilingIndicatorModel[];
  private isVirtualMode = false;
  private virtualSkip = 0;
  private loadedVirtualPages = new Set<number>();

  constructor(
    private reportingService: ReportService,
    private billingFunderSettingService: BillingFunderSettingService) {
    super();
    const PAGE_SIZE = 50;
    const WINDOW_SIZE = 200;

    this.loadData$
      .pipe(
        switchMap(({ params, mode }) => {
          mode === 'replace' ? this.loading$.next(true)
            : this.virtualLoading$.next(true);
          this.virtualSkip = params.skip;
          if (mode === 'append' || mode === 'prepend') {
            const pageNumber = Math.floor(params.skip / PAGE_SIZE);
            if (mode === 'append')
              this.loadedVirtualPages.add(pageNumber);
            else
              this.loadedVirtualPages.delete(pageNumber);

          } else if (mode === 'replace') {
            this.loadedVirtualPages.clear();
            this.loadedVirtualPages.add(0);
          }

          return this.billingFunderSettingService
            .getBillingFunderSettings(params)
            .pipe(
              map((response: BillingFunderSettingResponseModel) => {
                this.totalCount = response.total;
                this.timeZones = response.timeZone;
                this.claimFilingIndicatorModel = response.claimFilingIndicator;
                const mappedData: BillingFunderSettings[] = response.data.map(item => ({
                  ...item,
                  id: item.id 
                }));

                return { result: mappedData, mode };
              }),
              catchError(() => {
                this.loading$.next(false);
                this.virtualLoading$.next(false);
                return of({ result: [], mode });
              })
            );
        })
      )
      .subscribe(({ result, mode }) => {
        const incoming = result;
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
          if (this.data.length == 200) {
            const excess = this.buffer.length - 150;
            this.buffer = this.buffer.slice(0, this.buffer.length - excess);
          }
          else if (this.buffer.length > 200) {
            const excess = this.buffer.length - 200;
            this.buffer = this.buffer.slice(0, this.buffer.length - excess);
          }
          this.data = this.buffer;
        }
        this.loading$.next(false);
        this.virtualLoading$.next(false);
        this.sync();
      });
  }

  ngOnInit(): void {
  }

  getReport(params: BillingFunderListRequestModel) {
    this.loadData$.next({ params, mode: 'replace' });
  }

  append(params: BillingFunderListRequestModel) {
    this.loadData$.next({ params, mode: 'append' });
  }

  prepend(params: BillingFunderListRequestModel) {
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

  getTimeZone() {
    return this.timeZones;
  }

  getClaimFilingIndicatorModel() {
    return this.claimFilingIndicatorModel;
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
}
