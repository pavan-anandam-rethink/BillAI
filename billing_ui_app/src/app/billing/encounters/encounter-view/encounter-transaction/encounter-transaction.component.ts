import {
  Component,
  OnInit,
  OnDestroy,
  Input,
  ViewEncapsulation,
  ViewChild,
  ViewChildren,
  QueryList,
  ChangeDetectorRef,
} from '@angular/core';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { GridDataResult, PageChangeEvent } from '@progress/kendo-angular-grid';
import {
  State,
  process,
  SortDescriptor,
  FilterDescriptor,
} from '@progress/kendo-data-query';
import { ClaimService } from '@core/services/billing';
import { FormGroup, FormBuilder } from '@angular/forms';
import { ClaimHistory } from '@core/models/billing/claim-history';
import {
  HistoryViewModel,
  ClaimHistoryMapper,
  ClaimHistoryActionModel,
} from './mapper/claim-history-mapper';
import { ClaimAction } from '@core/enums/billing/claim-action';
import { ClaimActionMode } from '@core/enums/billing/claim-action-mode';
import {
  animate,
  state,
  style,
  transition,
  trigger,
} from '@angular/animations';
import { MatTable, MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';

export interface HistoryFilterModels {
  userList: string[] | [];
  actionList: string[] | [];
  modeList: string[] | [];
}

@Component({
  selector: 'app-encounter-transaction',
  templateUrl: './encounter-transaction.component.html',
  styleUrls: ['./encounter-transaction.component.css'],
  animations: [
    trigger('detailExpand', [
      state('collapsed, void', style({ height: '0px' })),
      state('expanded', style({ height: '*' })),
      transition(
        'expanded <=> collapsed',
        animate('225ms cubic-bezier(0.4, 0.0, 0.2, 1)')
      ),
      transition(
        'expanded <=> void',
        animate('225ms cubic-bezier(0.4, 0.0, 0.2, 1)')
      ),
    ]),
  ],
})
export class EncounterTransactionComponent {
  @Input()
  public claimId: number | null = null;
  @Input()
  public claimIdentifier: string;
  private unsubscribeAll$ = new Subject<void>();

  filterForm: FormGroup;
  showFilter = false;

  oldFilterFormVals: any = {
    changeDate: { value: '' },
    changeBy: { value: '', field: '' },
    action: { value: '', field: '' },
    mode: { value: '', field: '' },
  };

  view: GridDataResult = {
    data: [],
    total: 0,
  };
  claimHistoryModels: ClaimHistory[] = [];
  historyFilterModels: HistoryFilterModels;
  claimHistoryActions: ClaimHistoryActionModel[] = [];

  //TODO: with ListFilterSort
  //gridState: ListFilterSort = new ListFilterSort();
  public state: State = {
    skip: 0,
    take: 20,

    sort: [
      {
        field: 'changeDate',
        dir: 'desc',
      },
    ],

    filter: {
      logic: 'and',
      filters: [],
    },
  };

  @ViewChild(MatPaginator) paginator: MatPaginator;
  @ViewChild(MatSort) sort: MatSort;
  displayedColumns = [
    'expand',
    'claimIdentifier',
    'changeDate',
    'changeBy',
    'action',
    'mode',
    'description',
  ];
  childColumns = ['fieldName', 'oldValue', 'newValue'];
  dataSource = new MatTableDataSource<Element>();
  public isLoading = false;

  gridPageSizes: any;
  constructor(private fb: FormBuilder, private claimService: ClaimService) {
    this.getGridPageSizes();
  }

  ngOnInit(): void {
    this.filterForm = this.fb.group({
      changeDate: this.fb.group({
        value: [''],
      }),
      changeBy: this.fb.group({
        value: [''],
        field: [''],
      }),
      action: this.fb.group({
        value: [''],
        field: [''],
      }),
      mode: this.fb.group({
        value: [''],
        field: [''],
      }),
    });

    if (this.claimId != null && this.claimId > 0) {
      this.claimService.getClaimHistoryActions().subscribe((x) => {
        this.claimHistoryActions = x;
      });

      this.loadData(this.state).then((res) => {
        this.historyFilterModels = {
          userList: this.claimHistoryModels.map((x) => x.changeBy),
          actionList: this.claimHistoryModels.map(
            (x) => ClaimAction[x.actionId]
          ),
          modeList: this.claimHistoryModels.map((x) => ClaimActionMode[x.mode]),
        };
      });
    }
  }

  onFilterChanged() {
    const filterValues = this.filterForm.getRawValue();
    this.findNewValue(filterValues);
  }

  ngOnDestroy(): void {
    this.unsubscribeAll$.next();
    this.unsubscribeAll$.complete();
  }

  onSortChange(sortParams: SortDescriptor[]): void {
    this.state.sort = sortParams;
    this.loadData(this.state);
  }

  toggleFilter(event: boolean) {
    this.showFilter = !this.showFilter;
  }

  findNewValue(newFormVals: Object): void {
    let filtersToRemove: any[] = [];
    Object.keys(newFormVals).forEach((key) => {
      let newFormField = newFormVals[key];
      if (newFormField.value === '') {
        filtersToRemove.push(newFormField);
      }
      if (
        newFormField.value &&
        newFormField.value.filters !== undefined &&
        newFormField.value.filters.length === 0
      ) {
        filtersToRemove.push(newFormField);
      }
    });
    if (filtersToRemove.length === 4 && this.state.filter) {
      this.state.filter.filters = [];
      this.oldFilterFormVals = {
        changeDate: { value: '' },
        changeBy: { value: '', field: '' },
        action: { value: '', field: '' },
        mode: { value: '', field: '' },
      };
      this.loadData(this.state);
      return;
    }
    if (this.oldFilterFormVals != newFormVals) {
      let newFormVal: any;
      let oldFormVal: any;
      let newVal: any = {};
      for (newFormVal in newFormVals) {
        for (oldFormVal in this.oldFilterFormVals) {
          if (newFormVal === oldFormVal) {
            if (
              newFormVals[newFormVal].value !==
              this.oldFilterFormVals[oldFormVal].value
            ) {
              newVal = newFormVals[newFormVal];
              break;
            }
          }
        }
      }

      this.oldFilterFormVals = newFormVals;
      this.updateFilters(newVal);
    }
  }

  updateFilters(newFormVals: any): void {
    if (
      newFormVals.value &&
      newFormVals.value.filters &&
      newFormVals.field !== undefined
    ) {
      if (this.state.filter && this.state.filter.filters.length > 0) {
        let filtersIndexToRemove: number[] = [];
        Object.keys(this.state.filter.filters).forEach((key) => {
          if (this.state.filter) {
            let filterObj = this.state.filter.filters[key];
            if (filterObj !== undefined && filterObj.field !== 'changeDate') {
              let filters = filterObj.filters;
              Object.keys(filters).forEach((key) => {
                let filter = filters[key];
                if (
                  filter !== undefined &&
                  newFormVals.value.filters.length === 0 &&
                  filtersIndexToRemove.length === 0
                ) {
                  if (
                    newFormVals.field === 'user' &&
                    filter.field === 'changeBy'
                  ) {
                    filtersIndexToRemove.push(+key);
                  } else if (
                    newFormVals.field === 'action' &&
                    filter.field === 'action'
                  ) {
                    filtersIndexToRemove.push(+key);
                  } else if (
                    newFormVals.field === 'mode' &&
                    filter.field === 'mode'
                  ) {
                    filtersIndexToRemove.push(+key);
                  }
                }
                if (
                  filter !== undefined &&
                  newFormVals.value.filters[key] !== undefined &&
                  filter.field === newFormVals.value.filters[key].field &&
                  filtersIndexToRemove.length === 0
                ) {
                  filtersIndexToRemove.push(+key);
                }
              });
            }
          }
          if (filtersIndexToRemove.length > 0) {
            if (this.state.filter) {
              this.state.filter.filters.splice(+key, 1);
            }
          }
        });
      }
      if (newFormVals.value.filters.length > 0) {
        this.state.filter && this.state.filter.filters.push(newFormVals.value);
      }
    } else if (
      newFormVals.value &&
      newFormVals.value.length > 0 &&
      newFormVals.field === undefined
    ) {
      if (this.state.filter && this.state.filter.filters.length > 0) {
        let removeIndexies: number[] = [];
        Object.keys(this.state.filter.filters).forEach((filterKey) => {
          if (this.state.filter) {
            const filterItem = this.state.filter.filters[filterKey];
            if (filterItem !== undefined && filterItem.field === 'changeDate') {
              removeIndexies.push(+filterKey);
            }
          }
        });
        if (removeIndexies.length > 0) {
          for (var i = removeIndexies.length - 1; i >= 0; i--)
            this.state.filter.filters.splice(removeIndexies[i], 1);
        }
      }
      Object.keys(newFormVals.value).forEach((valueKey: any) => {
        const formFilterValue = newFormVals.value[valueKey];
        const filterEl: FilterDescriptor = {
          field: formFilterValue.field,
          operator: formFilterValue.operator,
          value: new Date(formFilterValue.value),
        };
        this.state.filter && this.state.filter.filters.push(filterEl);
      });
    }

    this.loadData(this.state);
  }

  public pageChange(event: PageChangeEvent): void {
    this.state.skip = event.skip;
    this.state.take = event.take;

    if (this.state.take === 0) this.state.take = this.view.total;
    this.loadData(this.state);
  }

  loadData(params: State) {
    if (this.claimId) {
      return new Promise<void>((resolve, reject) => {
        this.isLoading = true;
        this.claimService
          .getClaimHistory(this.claimId!, params)
          .pipe(takeUntil(this.unsubscribeAll$))
          .subscribe({
            next: (x) => {
              this.claimHistoryModels = x;
              this.state = params;
              const historyData = new ClaimHistoryMapper(
                this.claimIdentifier,
                this.claimHistoryActions
              ).map(this.claimHistoryModels);
              let filteredHistoryData = [];
              let i = 0;

              while (i < historyData.length) {
                const currentDescription = (
                  historyData[i].description || ''
                ).toLowerCase();
                const nextDescription = (
                  (historyData[i + 1] && historyData[i + 1].description) ||
                  ''
                ).toLowerCase();

                // Check if the current description is "Charge Entry note = '@newvalue' added"
                const isChargeEntryNoteAdded =
                  currentDescription ===
                  'charge entry note = "@newvalue" added';

                // Check if the next item is "Charge Entry note added"
                const isNextItemNewValue =
                  nextDescription === 'charge entry note added';

                if (isChargeEntryNoteAdded && isNextItemNewValue) {
                  // Skip both items if the current is "Charge Entry note added" and the next is "@newvalue"
                  i += 2; // Skip both the current and next item
                } else {
                  // Otherwise, keep the current item
                  filteredHistoryData.push(historyData[i]);
                  i++; // Move to the next item
                }
              }
              this.view = process(filteredHistoryData, this.state);
              this.isLoading = false;
              resolve();
            },
            error: (err) => {
              reject(err);
              this.isLoading = false;
            },
          });
        // window.setTimeout(() => {
        //     if(this.isLoading == true) {
        //         this.loadData(params);
        //     }
        // }, 1000);
      });
    } else {
      return new Promise<void>((resolve, reject) => {
        resolve();
      });
    }
  }

  showHistoryDetails(dataItem: HistoryViewModel, _index: number) {
    return !!dataItem.details;
  }
  currentIndex: number;
  expanded: boolean = false;
  expandedElement: any | null;
  expand(index) {
    this.currentIndex = index;
    this.expanded = !this.expanded;
  }

  ngAfterViewInit() {
    this.dataSource.paginator = this.paginator;
  }

  getGridPageSizes(): void {
    const storedGridPageSizes = localStorage.getItem('gridPageSizes')
      ? JSON.parse(localStorage.getItem('gridPageSizes') || '')
      : null;
    if (storedGridPageSizes) {
      this.gridPageSizes = storedGridPageSizes;
    } else {
      this.claimService
        .getGridPageSizes()
        .subscribe((sizes: Array<number | { text: string; value: number }>) => {
          this.gridPageSizes = sizes;
        });
    }
  }

  getPageStart(total: number): number {
    if (!total) return 0;
    const skip = this.state?.skip || 0;
    return Math.min(skip + 1, total);
  }

  getPageEnd(total: number): number {
    if (!total) return 0;
    const skip = this.state?.skip || 0;
    const take = this.state?.take || 0;
    if (take === 0) return total;
    return Math.min(skip + take, total);
  }
}
