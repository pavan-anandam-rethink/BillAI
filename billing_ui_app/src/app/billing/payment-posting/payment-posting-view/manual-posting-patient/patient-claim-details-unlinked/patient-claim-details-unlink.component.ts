import { Component, Input, NgZone, OnDestroy, ViewChild, OnInit, Output, EventEmitter, OnChanges } from '@angular/core';
import {
    ClaimManualPostingDetails,
    ClaimPostingDetails,
    PatientClaimDetailsFilterSort
} from '@core/models/billing';
import { ClaimPostingService } from '@core/services/billing';
import { PatientClaimPostingDetailsSubject } from '@core/subjects/patient-claim-posting-details.subject';
import { GridComponent, GridDataResult, RowClassArgs } from '@progress/kendo-angular-grid';
import { process, SortDescriptor } from '@progress/kendo-data-query';
import { Observable, Subject } from 'rxjs';
import { first, map, take, takeUntil, tap } from 'rxjs/operators';
import { PaymentPostingAdjustmentsDetailsComponent } from '../../payment-details/payment-posting-adjustments-details';
import { SidebarService } from '@app/shared/components/sidebar';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { AccountPermissions } from "@core/enums/account";

@Component({
    selector: 'patient-claim-details-unlink',
    templateUrl: './patient-claim-details-unlink.component.html',
    styleUrls: ['./patient-claim-details-unlink.component.css']
})

export class PatientClaimDetailsUnlinkComponent implements OnInit, OnDestroy {
    _paymentId: number;
    canEdit: boolean = false;
    @Input() set paymentId(value: number) {
        this._paymentId = value;
    }

    _showPaid: boolean;
    @Input() set showPaid(value: boolean) {
        this._showPaid = value;
        this.redrawGrid();
    }

    _patientId: number;
    @Input() set patientId(value: number) {
        this._patientId = value;
        this.redrawGrid();
    }

    @Input() printType: string;

    _selectAllLines: boolean;
    @Input() set selectAllLines(value: boolean) {
        this._selectAllLines = value;
        this.allLinesSelectionChanges();
    };
    @Input() isErrors: boolean;
    @Output() adjustmentClick = new EventEmitter();
    @Output() selectedLinesChangeEmitter = new EventEmitter<any>();
    @Output() allUnlinkedLinesChangeEmitter = new EventEmitter<any>();
    @Output() updateClaim = new EventEmitter<any>();
    @Input() isLinked: boolean;

    private unsubscribe = new Subject<void>();
    @ViewChild(GridComponent) grid: GridComponent;

    PatientClaimPostingDetailsSubject: PatientClaimPostingDetailsSubject;
    view: Observable<GridDataResult>;

    gridView: Observable<GridDataResult>;
    gridData: ClaimPostingDetails[] = [];
    gridState: PatientClaimDetailsFilterSort = new PatientClaimDetailsFilterSort();

    selectedItems: ClaimManualPostingDetails[] = [];
    public isSubjectLoading$: Observable<boolean>;

    public mySelection: number[] = [];

    constructor(
        private ngZone: NgZone,private sidebarService: SidebarService,
        private claimPostingService: ClaimPostingService,
        private accountService: AccountMemberService,
    ) {
        this.accountService.accountMemberSettings.subscribe((x) => {
              if (x) {
                this.canEdit = this.accountService.checkPermissionLevel(
                  AccountPermissions.BillingEditApprovedAppointments
                );
              }
            });
     }


    allLinesSelectionChanges(): void {
        if (this.gridData.length == 0) {
            return;
        }

        if (this._selectAllLines) {
            this.gridData.forEach(dataItem => {
                dataItem.checked = true;
                let item: any = dataItem;
                item.patientId = this._patientId;

                let alreadyExist = this.selectedItems.find((x: any) => x.patientId == this._patientId);

                if (alreadyExist) {
                    alreadyExist = item;
                } else {
                    this.selectedItems.push(item);
                }
            });
        } else {
            this.gridData.forEach(dataItem => {
                dataItem.checked = false;
            })
            this.selectedItems = []
        }

        this.selectedLinesChanged();

    }

    rowSelectionClick(event: any, dataItem: any) {
        if (event.currentTarget.checked) {
            dataItem.checked = true;
            let selectedItem = dataItem;
            selectedItem._patientId = this._patientId;
            this.selectedItems.push(selectedItem);
        } else {
            dataItem.checked = false;
            let deselectedItem = dataItem;
            let item = this.selectedItems.find(x => x.id == deselectedItem.id);

            if (item !== undefined) {
                this.selectedItems.remove(item);
            }
        }

        this.selectedLinesChanged(event);
    }

    checkIsSelected(dataItemId: any, event: any = undefined): boolean {
        return this.selectedItems.any((x: any) => x.id == dataItemId);
    }

    getInitialInputValue(dataItemId: any): number {
        let item = this.selectedItems.find(x => x.id == dataItemId);

        if (item === undefined) {
            return 0;
        }

        return item.balance;
    }

    inputValueChange(event: any, dateItemId: any): void {
        let newValue = event.target.value;
        let item = this.selectedItems.find(x => x.id == dateItemId);

        if (item === undefined) {
            return;
        }

        if (newValue >= item.balance) {
            event.target.value = item.balance;
        }

        item.paidAmount = newValue;
    }

    selectedLinesChanged(event: any = null): void {
        if (event !== null) {
            let selectionState = event.currentTarget.checked;
            let calculatedLength = selectionState ? this.selectedItems.length : this.selectedItems.length + 1;

            if (calculatedLength == this.gridData.length) {
                if (selectionState) {
                    let emitModel = {
                        event: event,
                        patientId: this._patientId
                    };
                    this.allUnlinkedLinesChangeEmitter.emit(emitModel);
                    this._selectAllLines = true;
                } else {
                    this._selectAllLines = false;
                }
            } else {
                this._selectAllLines = false;
            }

            let model = {
                patientId: this._patientId,
                serviceLines: this.selectedItems
            }

            this.selectedLinesChangeEmitter.emit(model);
        }
    }

    selectAllLinesChanged(event: any): void {
        let emitModel = {
            event: event,
            patientId: this._patientId
        };
        this.selectedItems = [];
        if (event?.target?.checked) {
            this.view.forEach(dataItem => {
                dataItem.data.forEach(element => {
                    element.checked = true;
                    let selectedItem: any = element
                    selectedItem._patientId = this._patientId;
                    this.selectedItems.push(selectedItem);
                    this.selectedLinesChanged(event);
                });
            })
        }
        else {
            this.view.forEach(dataItem => {
                dataItem.data.forEach(element => {
                    element.checked = false;
                    // let selectedItem:any = element    
                    // selectedItem._patientId = this._patientId;
                    // this.selectedItems.push(selectedItem);
                    this.selectedLinesChanged(event);
                });
            })

        }
        //console.log(this.mySelection)
        // this.selectedLinesChanged(event);
        this.allUnlinkedLinesChangeEmitter.emit(emitModel);
    }

    redrawGrid() {
        this.gridState.patientId = this._patientId;
        this.gridState.paymentId = this._paymentId;
        this.gridState.showPaid = this._showPaid;

        if (this.gridState.patientId && this.gridState.paymentId) {
            this.PatientClaimPostingDetailsSubject = new PatientClaimPostingDetailsSubject(this.claimPostingService);
            this.view = this.PatientClaimPostingDetailsSubject.pipe(
                takeUntil(this.unsubscribe),
                map(data => {
                    // data.forEach(x => {
                    //     x.checked = this._selectAllLines;
                    // });
                    return process(data, this.gridState)
                },
                    tap(() => setTimeout(() => this.fitColumns(), 250)))
            );
            this.loadData(this.gridState);

        }
    }

    onSortChange(sortParams: SortDescriptor[]): void {
        this.gridState.sortingModels = sortParams;
        this.loadData(this.gridState);
    }

    loadData(params: PatientClaimDetailsFilterSort): void {
        params.isLinked = this.isLinked;
        this.PatientClaimPostingDetailsSubject.getAllUnlinked(params);
        this._selectAllLines = false;
    }

    fitColumns(): void {
        this.ngZone.onStable.asObservable().pipe(take(1)).subscribe(() => {
            this.grid.autoFitColumns(this.grid.columnList.toArray());
            this.grid.autoFitColumns();
        });
    }
    
    lastClickedId: number;
    selectedIds: number[] = [];
    adjustmentClicked(event: any) {
        if (event.id && !this.isErrors) {

            this.selectedIds.remove(event.id);
            if (this.lastClickedId !== event.id) {
                this.lastClickedId = event.id;
                this.sidebarService.openRight(PaymentPostingAdjustmentsDetailsComponent, true, "md",true).subscribe(rsidebarRef => {
                    rsidebarRef.instance.setData(event.id, this.patientId,0,true);
                    /*prevents doudle click on the same elemets : TODO*/
                    this.sidebarService.rightSidebarComponentRef.instance.onClose.pipe(first()).subscribe(x => {
                        this.lastClickedId = 0;
                    });

                    rsidebarRef.instance.onClose.subscribe(x => {
                        this.sidebarService.rightSidebarComponentRef.instance.close();
                    });
    
                    rsidebarRef.instance.onUpdate.subscribe((x: number) => {
                        // this.PatientClaimPostingDetailsSubject.updateServiceLine(x)
                        //this.loadData(this.gridState);

                        let claimId = this.PatientClaimPostingDetailsSubject.getClaimId(x);
                        let patientId = this.PatientClaimPostingDetailsSubject.getPatientId(x);
                        var data = {
                            claimId,
                            patientId
                        }
                        this.updateClaim.emit(data);
                    });
                });
            }
        }
    }

    ngOnDestroy() {
        this.PatientClaimPostingDetailsSubject && (this.PatientClaimPostingDetailsSubject.unsubscribe());
        this.unsubscribe.next();
        this.unsubscribe.complete();
    }

    ngAfterViewInit() {
        this.fitColumns();
    }

    ngOnInit(): void {
        this.gridState.skip = 0;
        this.gridState.take = 1000;

        this.view.pipe(takeUntil(this.unsubscribe)).subscribe((viewData: GridDataResult) => {
            this.gridData = viewData.data;
            this.allLinesSelectionChanges();
        });
    }

    public rowCallback = (context: RowClassArgs) => {
        return {
            hasErrors: context.dataItem.isLinked
        };
    }
}