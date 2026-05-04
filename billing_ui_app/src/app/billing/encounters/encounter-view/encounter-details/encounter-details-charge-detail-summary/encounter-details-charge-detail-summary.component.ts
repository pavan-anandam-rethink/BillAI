import { CurrencyPipe } from '@angular/common';
import {
  ChangeDetectorRef,
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output,
  SimpleChanges,
} from '@angular/core';
import { SidebarService } from '@app/shared/components/sidebar';
import {
  ClaimDetailsListFilterSort,
  ClaimDetailsModel,
  ListFilterSort,
  ClaimUpdateDetailsModels,
} from '@core/models/billing';
import { BasicOption } from '@core/models/common';
import { ClaimUpdateModel } from '@core/models/billing/claim-details-model';
import { ClientBillingCode } from '@core/models/billing/billing-code';
import { ConfirmDialog } from '@core/models/common';
import { AppointmentService, ClaimService } from '@core/services/billing';
import {
  GridDataResult,
  PageChangeEvent,
  PagerSettings,
} from '@progress/kendo-angular-grid';
import { SortDescriptor } from '@progress/kendo-data-query';
import { Subject, Subscription, of, throwError } from 'rxjs';
import { takeUntil, tap } from 'rxjs/operators';
import { ChargeNotesComponent } from './charge-notes/charge-notes.component';
import { ActivatedRoute, Router } from '@angular/router';
import { NumberFormatOptions } from '@progress/kendo-angular-intl';
import { ClaimChargeDetailsShared } from '@app/billing/encounters/shared/claim-charge-details/claim-charge-details-shared';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';
import { DialogAction } from '@progress/kendo-angular-dialog';
import { DialogService } from '@progress/kendo-angular-dialog';
import { Helper } from '@app/billing/encounters/common/common-helper';
import { ModifiersGridColumnUpdateModel } from '@app/billing/encounters/shared/modifiers/modifiers.component';

@Component({
  selector: 'app-encounter-details-charge-detail-summary',
  templateUrl: './encounter-details-charge-detail-summary.component.html',
  styleUrls: ['./encounter-details-charge-detail-summary.component.css'],
})
export class EncounterDetailsChargeDetailSummaryComponent implements OnInit {
  @Input() claimId: number;
  @Input() isManualClaim: boolean;
  private _diagnosisCode: string = '';
  @Input() set diagnosisCode(value: string) {
    this._diagnosisCode = value || '';
    if (this._diagnosisCode && this.view?.data?.length) {
      this.applyDiagnosisToAllRows(this._diagnosisCode);
    }
  }
  get diagnosisCode(): string {
    return this._diagnosisCode;
  }
  @Input() billingCodes: ClientBillingCode[] = [];
  @Input() renderingProviders: BasicOption[] = [];
  @Output() isChargeEntryValid = new EventEmitter<boolean>();
  @Output() isChargeEntryDirty = new EventEmitter<boolean>();

  subscriptions = new Subscription();
  manualClaim = true;
  public formatOptions: NumberFormatOptions = {
    style: 'currency',
    currency: 'USD',
    currencyDisplay: 'name',
  };

  private unsubscribeAll$ = new Subject<void>();
  public confirmDeleteLineDialog: ConfirmDialog = new ConfirmDialog(
    false,
    'Delete Charge Detail Line',
    "Are you sure you'd like to perform this action?",
    'Delete',
    'Cancel'
  );
  public appointmentConnectedDialog: ConfirmDialog = new ConfirmDialog(
    false,
    'Delete Charge Detail Line',
    "Appointment is associated with the charge details, are you sure you'd like to perform this action?",
    'Delete',
    'Cancel'
  );

  view: GridDataResult = {
    data: [],
    total: 0,
  };

  gridState: ListFilterSort = new ListFilterSort();

  readonly pagingSettings: PagerSettings = {
    buttonCount: 5,
    type: 'numeric',
    pageSizes: true,
    previousNext: true,
  };

  gridPageSizes: any;

  lineIdToRemove = 0;
  private onLinkSubscription: Subscription | null = null;
  public dialogForTotalCount: boolean = false;
  public gatClaimsTabData: boolean = false;

  readonly claimChargeDetailsShared: ClaimChargeDetailsShared;

  constructor(
    private claimsService: ClaimService,
    private sidebarService: SidebarService,
    private appointmentService: AppointmentService,
    private notificationHandler: NotificationHandlerService,
    private currencyPipe: CurrencyPipe,
    private cd: ChangeDetectorRef,
    private router: Router,
    private readonly route: ActivatedRoute,
    private accountService: AccountMemberService,
    private dialogService: DialogService
  ) {
    this.claimChargeDetailsShared = new ClaimChargeDetailsShared(
      claimsService,
      currencyPipe,
      router,
      route,
      accountService,
      notificationHandler
    );
    this.getGridPageSizes();
  }

  updateLines() {
    this.claimChargeDetailsShared.chargeDetailsData = this.view.data;
    this.claimChargeDetailsShared.updateLines();
  }

  getChargeEntryUpdateModel() {
    return this.buildFullChargeEntryModel();
  }

  getIsChargeEntryUpdated() {
    return this.view.data.length > 0;
  }

  showLineNotesSidebar(item: ClaimDetailsModel) {
    this.sidebarService
      .openRight(ChargeNotesComponent, true, 'md')
      .subscribe((rsidebarRef) => rsidebarRef.instance.setChargeData(item));
  }

  openDeleteLineDialog(lineId: number, associatedAppointments: any): void {
    var appointments = associatedAppointments;
    if (lineId !== 0 && associatedAppointments !== 0) {
      this.lineIdToRemove = lineId;
      this.appointmentConnectedDialog.opened = true;
    } else if (lineId !== 0 && associatedAppointments === 0) {
      this.lineIdToRemove = lineId;
      this.confirmDeleteLineDialog.opened = true;
    }
    this.cd.detectChanges();
  }

  removeLine() {
    if (this.lineIdToRemove === 0) {
      return;
    }
    this.claimsService
      .removeBillingClaimDetail(this.lineIdToRemove)
      .pipe(takeUntil(this.unsubscribeAll$))
      .subscribe(
        () => {},
        () => {},
        () => {
          this.appointmentService.onLink.next(0);
        }
      );
  }

  removeLineWithoutAppointment() {
    if (this.lineIdToRemove === 0) {
    }
    this.claimsService
      .removeBillingClaimDetail(this.lineIdToRemove)
      .pipe(takeUntil(this.unsubscribeAll$))
      .subscribe(
        () => {},
        () => {},
        () => {
          this.loadData();
        }
      );
  }

  onPageChange(event: PageChangeEvent): void {
    // this.gridState.skip = event.skip;
    // this.gridState.take = event.take;

    // if (this.gridState.take === 0) this.gridState.take = this.view.total;
    // this.loadData();

    this.gridState.skip = event.skip;
    this.gridState.take = event.take;

    if (this.gridState.take === 0 && this.view.total > 1000) {
      this.dialogForTotalCount = true;
    } else {
      this.dialogForTotalCount = false;
      this.gatClaimsTabData = true;
      if (this.gridState.take === 0) this.gridState.take = 9999999;
      this.loadData();
      localStorage.setItem('lastPageSize', this.gridState.take.toString());
    }

    if (this.gridState.take === 0) this.gridState.take = 9999999;
    if (this.dialogForTotalCount) {
      this.subscriptions.add(
        this.SubmitPageCount().result.subscribe((result) => {
          if ((result as DialogAction).text === 'Yes') {
            this.loadData();
            this.gatClaimsTabData = true;
          } else {
            const lastPageSize = localStorage.getItem('lastPageSize');
            this.gridState.take = lastPageSize
              ? JSON.parse(lastPageSize)
              : null;
          }
        })
      );
    }
  }

  SubmitPageCount() {
    const confirmDialog = this.dialogService.open({
      title: '⚠️ Please confirm',
      width: 500,
      content:
        'There are more than 1000 records to display. This may take more time to process. You can either proceed or apply a filter to narrow down the results',
      actions: [{ text: 'Cancel' }, { text: 'Yes', primary: true }],
    });
    return confirmDialog;
  }

  modifierIconClick(e) {
    console.log(e);
    this.isModifierIconClick = true;
  }
  isModifierIconClick = false;
  onSortChange(sortParams: SortDescriptor[]): void {
    // Saurabh: Sorting is removed because of the bug 208202. But later if it is required just uncomment the below code
    // this.gridState.sortingModels = sortParams;
    // if (!this.isModifierIconClick) {
    //     this.loadData();
    // } this.isModifierIconClick = false;
  }

  loadData(): void {
    let params = new ClaimDetailsListFilterSort();
    params.claimId = this.claimId;
    params.filterModels = this.gridState.filterModels;
    params.sortingModels = this.gridState.sortingModels;
    params.skip = this.gridState.skip;
    params.take = this.gridState.take;

    this.claimChargeDetailsShared.claimId = this.claimId;

    this.claimsService
      .getBillingClaimDetails(params)
      .pipe(takeUntil(this.unsubscribeAll$))
      .subscribe(
        (result: any) => {
          this.view.data = result;
          if (this._diagnosisCode) {
            this.applyDiagnosisToAllRows(this._diagnosisCode);
          }
          this.claimChargeDetailsShared.chargeDetailsData = this.view.data;
          this.view.total = result.length > 0 ? result[0].totalCount : 0;
          this.claimChargeDetailsShared.fillArrayForm(this.view.data);
          this.claimChargeDetailsShared.calculateTotal(this.view.data);
        },
        (error) => {
          this.router.navigate(['/billing/claims']);
        }
      );
  }

  ngOnDestroy() {
    if (this.onLinkSubscription !== null) {
      this.onLinkSubscription.unsubscribe();
    }
    this.sidebarService.closeAll();
    this.unsubscribeAll$.next();
    this.unsubscribeAll$.complete();
  }

  ngOnInit() {
    this.loadData();

    this.onLinkSubscription = this.appointmentService.onLink.subscribe(() => {
      this.loadData();
    });
  }

  ngOnChanges(changes: SimpleChanges) {
    if (
      changes.isManualClaim &&
      changes.isManualClaim.currentValue != undefined
    ) {
      this.claimChargeDetailsShared.manualClaim = this.isManualClaim;
    }
  }

  formCheck() {
    this.isChargeEntryDirty.emit(true);
    const invalidModifiers =
      this.claimChargeDetailsShared.modifiersToUpdate.filter((x) => !x.isValid);
    const newRowsComplete = this.areNewRowsComplete();
    const overallValid =
      invalidModifiers.length < 1 &&
      !this.claimChargeDetailsShared.chargeDetailsArrayForm.invalid &&
      newRowsComplete;
    this.isChargeEntryValid.emit(overallValid);
  }

  private areNewRowsComplete(): boolean {
    if (!this.view?.data?.length) return true;
    return this.view.data
      .filter((r) => r.id === 0) // new unsaved rows
      .every((r) => {
        const hasDos = !!r.dos;
        const hasBillingCode = (r as any).billingCodeId != null;
        const hasUnits = typeof r.units === 'number' && r.units > 0;
        const hasPerUnitsCharge =
          typeof r.perUnitsCharge === 'number' && r.perUnitsCharge > 0;
        return hasDos && hasBillingCode && hasUnits && hasPerUnitsCharge;
      });
  }

  public onModifiersUpdated(
    event: ModifiersGridColumnUpdateModel,
    row: ClaimDetailsModel
  ) {
    if (!row) return;
    row.modifier1 = event.modifiers.modifier1 ?? '';
    row.modifier2 = event.modifiers.modifier2 ?? '';
    row.modifier3 = event.modifiers.modifier3 ?? '';
    row.modifier4 = event.modifiers.modifier4 ?? '';
    row.includeOnClaimMod1 = !!row.modifier1;
    row.includeOnClaimMod2 = !!row.modifier2;
    row.includeOnClaimMod3 = !!row.modifier3;
    row.includeOnClaimMod4 = !!row.modifier4;
    this.claimChargeDetailsShared.modifiersUpdated(event);
    this.formCheck();
  }

  addChargeLine(prevChargeLine?: ClientBillingCode) {
    let diagnosisValue = '';
    if (
      this.view.data &&
      this.view.data.length > 0 &&
      this.view.data[0].diagnosis
    ) {
      diagnosisValue = this.view.data[0].diagnosis;
    } else if (this.diagnosisCode) {
      diagnosisValue = this.diagnosisCode;
    }
    let claim: ClaimDetailsModel = {
      dos: null as any,
      billingCode: '',
      billingCodeId: null as any,
      diagnosis: diagnosisValue,
      units: 0,
      billedAmount: 0,
      expectedAmount: 0,
      hours: 0,
      paymentAmount: 0,
      patientAmount: 0,
      balanceAmount: 0,
      adjustmentAmount: 0,
      totalCount: 0,
      claimId: 0,
      perUnitsCharge: 0,
      noteCreatorName: '',
      noteCreatedDate: new Date(),
      id: 0,
      modifier1: '',
      includeOnClaimMod1: true,
      modifier2: '',
      includeOnClaimMod2: true,
      modifier3: '',
      includeOnClaimMod3: true,
      modifier4: '',
      includeOnClaimMod4: true,
      chargeId: 0,
      noteText: '',
      associatedAppointmentsCount: 0,
      reasonCode: '',
      renderingProvider: '',
      renderingProviderId: 0,
    };
    (claim as any).isSecondaryCode = false;
    this.view.data.push(claim);
    if (this._diagnosisCode) {
      this.applyDiagnosisToAllRows(this._diagnosisCode);
    }
    // Evaluate validity so Save is disabled until required fields entered
    this.formCheck();
  }

  private applyDiagnosisToAllRows(code: string) {
    if (!this.view?.data) return;
    this.view.data.forEach((r) => (r.diagnosis = code));
  }

  onBillingCodeSelected(row: ClaimDetailsModel, option: ClientBillingCode) {
    if (!option) {
      return;
    }
    const composed =
      option.serviceName +
      '/' +
      option.billingCodeName +
      (option.billingCodeName2 ? '/' + option.billingCodeName2 : '');
    const lastSegment = composed.split('/').pop() || composed;
    row.billingCode = lastSegment;
    (row as any).billingCodeId = option.billingCodeId;
    // capture unitTypeId for later recalculations
    (row as any).unitTypeId = option.unitTypeId;
    if (!row.units || row.units === 0) {
      row.units = 1; // temporary seed to show base hours immediately
      this.updateHours(row);
      row.units = 0; // reset back to 0 so user can enter actual units; hours will recalc on change
    } else {
      this.updateHours(row);
    }
    // handle secondary code creation if billingCodeName2 exists
    this.handleSecondaryCode(row, option);
    this.formCheck();
  }

  onBillingCodeChanged(row: ClaimDetailsModel, billingCodeId: number) {
    if (billingCodeId == null) {
      return;
    }
    const option = this.billingCodes?.find(
      (b) => b.billingCodeId === billingCodeId
    );
    this.onBillingCodeSelected(row, option as ClientBillingCode);
  }

  onRenderingProviderChanged(row: any, providerId: number) {
    row.renderingProviderId = providerId;
    this.formCheck();
  }

  private unitTypeMinutesMap: { [key: number]: number } = {
    1: 15,
    2: 30,
    3: 60,
    4: 90,
    5: 60, // Untimed – we will later coerce to 0 hours (Unlimited)
  };

  private updateHours(row: ClaimDetailsModel) {
    const unitTypeId = (row as any).unitTypeId;
    if (!unitTypeId || !(unitTypeId in this.unitTypeMinutesMap)) {
      row.hours = 0;
    }
    if (unitTypeId === 5) {
      // Untimed => display Unlimited (0)
      row.hours = 0;
      return;
    }
    const minutesPerUnit = this.unitTypeMinutesMap[unitTypeId];
    const effectiveUnits = row.units && row.units > 0 ? row.units : 1; // use 1 for preview when 0
    const totalMinutes = effectiveUnits * minutesPerUnit;
    // Round to 2 decimals of hours
    row.hours = parseFloat((totalMinutes / 60).toFixed(2));
  }

  onUnitsChanged(row: ClaimDetailsModel) {
    this.updateHours(row);
    const idx = this.view.data.indexOf(row);
    if (idx > -1) {
      const next = this.view.data[idx + 1];
      if (
        next &&
        (next as any).isSecondaryCode &&
        (next as any).pairBillingCodeId === (row as any).billingCodeId
      ) {
        this.updateHours(next);
      }
    }
    this.formCheck();
  }

  private handleSecondaryCode(
    primaryRow: ClaimDetailsModel,
    option: ClientBillingCode
  ) {
    // Remove existing secondary if option has no second code
    const primaryIndex = this.view.data.indexOf(primaryRow);
    if (primaryIndex === -1) return;
    const existingSecondary = this.view.data[primaryIndex + 1];
    const hasExistingSecondary =
      existingSecondary &&
      (existingSecondary as any).isSecondaryCode &&
      (existingSecondary as any).pairBillingCodeId === option.billingCodeId;

    if (
      !option.billingCodeName2 ||
      option.billingCodeName2.trim().length === 0
    ) {
      if (hasExistingSecondary) {
        this.view.data.splice(primaryIndex + 1, 1);
      }
      return;
    }

    // If secondary already present, just update rate/unit type
    if (hasExistingSecondary) {
      (existingSecondary as any).unitTypeId = option.unitTypeId2;
      existingSecondary.perUnitsCharge =
        option.rate2 ?? existingSecondary.perUnitsCharge;
      // For secondary line keep only code segment
      const secComposed = option.serviceName + '/' + option.billingCodeName2;
      existingSecondary.billingCode =
        secComposed.split('/').pop() || secComposed;
      this.updateHours(existingSecondary);
      return;
    }

    // Create new secondary line immediately after primary
    const secondary: ClaimDetailsModel = {
      dos: primaryRow.dos as any,
      // billingCode: option.serviceName + '/' + option.billingCodeName2,
      billingCode:
        option.billingCodeName2?.toString().split('/').pop() ||
        option.billingCodeName2,
      billingCodeId: option.billingCodeId,
      diagnosis: primaryRow.diagnosis,
      units: primaryRow.units,
      billedAmount: primaryRow.units * (option.rate2 ?? 0),
      expectedAmount: primaryRow.units * (option.rate2 ?? 0),
      hours: 0,
      paymentAmount: 0,
      patientAmount: 0,
      balanceAmount: 0,
      adjustmentAmount: 0,
      totalCount: 0,
      claimId: primaryRow.claimId ?? 0,
      perUnitsCharge: option.rate2 ?? 0,
      noteCreatorName: primaryRow.noteCreatorName ?? '',
      noteCreatedDate: new Date(),
      id: 0,
      modifier1: primaryRow.modifier1,
      includeOnClaimMod1: false,
      modifier2: primaryRow.modifier2,
      includeOnClaimMod2: false,
      modifier3: primaryRow.modifier3,
      includeOnClaimMod3: false,
      modifier4: primaryRow.modifier4,
      includeOnClaimMod4: false,
      chargeId: 0,
      noteText: '',
      associatedAppointmentsCount: 0,
      reasonCode: '',
      renderingProvider: '',
      renderingProviderId: 0,
    };
    (secondary as any).isSecondaryCode = true;
    (secondary as any).pairBillingCodeId = option.billingCodeId;
    (secondary as any).unitTypeId = option.unitTypeId2; // for hours calc
    this.view.data.splice(primaryIndex + 1, 0, secondary);
    // keep diagnosis in sync in case of late change
    secondary.diagnosis = this._diagnosisCode;
    this.updateHours(secondary);
  }

  // /** Sync DOS change to its secondary line if present */
  public syncSecondaryDos(row: ClaimDetailsModel) {
    const idx = this.view.data.indexOf(row);
    if (idx === -1) return;
    const secondary = this.view.data[idx + 1];
    if (
      secondary &&
      (secondary as any).isSecondaryCode &&
      (secondary as any).pairBillingCodeId === (row as any).billingCodeId
    ) {
      secondary.dos = row.dos;
    }
  }

  //** Remove new unsaved line (id=0) including paired secondary */
  public removeNewLine(row: ClaimDetailsModel) {
    const idx = this.view.data.indexOf(row);
    if (idx === -1) return;
    // If primary with secondary, remove both
    const maybeSecondary = this.view.data[idx + 1];
    if (
      row.id === 0 &&
      maybeSecondary &&
      (maybeSecondary as any).isSecondaryCode &&
      (maybeSecondary as any).pairBillingCodeId === (row as any).billingCodeId
    ) {
      this.view.data.splice(idx, 2);
    } else if (row.id === 0) {
      this.view.data.splice(idx, 1);
    }
    this.isChargeEntryDirty.emit(true);
  }
  decimalFilter(event: any) {
    const reg = /^\d{0,6}(\.\d{0,2})?$/;
    let input = event.target.value + String.fromCharCode(event.charCode);
    if (!reg.test(input)) {
      event.preventDefault();
    }
  }

  // Build update model including ALL lines (existing + new id=0) in one pass
  public buildFullChargeEntryModel(): ClaimUpdateDetailsModels {
    const model: ClaimUpdateDetailsModels = {
      billingClaimDetailsModels: [],
      memberId: this.accountService.memberDetails.memberId,
    };
    const convertDate = (d: Date) => {
      if (!d) return null;
      const ms = d.getTime();
      return Helper.shiftDateToUTC(new Date(ms));
    };
    (this.view.data || []).forEach((l) => {
      // UTC epoch
      const rawDate: any = l.dos
        ? l.dos instanceof Date
          ? l.dos
          : new Date(l.dos)
        : null;
      const shiftedDate = Helper.shiftDateToUTC(rawDate);
      const billed =
        l.units && l.perUnitsCharge ? l.units * l.perUnitsCharge : 0;
      // Normalize pairBillingCodeId: only set for secondary lines; send 0 (or null) for primaries to avoid undefined
      //const isSecondary = (l as any).isSecondaryCode === true;
      //const pairId = isSecondary ? (l as any).pairBillingCodeId ?? (l as any).billingCodeId ?? 0 : 0;
      model.billingClaimDetailsModels.push({
        id: l.id || 0,
        claimId: this.claimId,
        units: l.units,
        perUnitsCharge: l.perUnitsCharge,
        billingCodeId: (l as any).billingCodeId,
        diagnosis: l.diagnosis || this._diagnosisCode || '',
        unitTypeId: (l as any).unitTypeId,
        DateOfService: shiftedDate,
        billingCode: l.billingCode,
        hours: l.hours,
        billedAmount: billed,
        expectedAmount: l.expectedAmount,
        paymentAmount: l.paymentAmount,
        patientAmount: l.patientAmount,
        balanceAmount: l.balanceAmount,
        adjustmentAmount: l.adjustmentAmount,
        isSecondaryCode: (l as any).isSecondaryCode,
        pairBillingCodeId: (l as any).pairBillingCodeId,
        renderingProviderId: (l as any).renderingProviderId || null,
        modifier1: l.modifier1 || '',
        includeOnClaimMod1: true,
        modifier2: l.modifier2 || '',
        includeOnClaimMod2: true,
        modifier3: l.modifier3 || '',
        includeOnClaimMod3: true,
        modifier4: l.modifier4 || '',
        includeOnClaimMod4: true,
      });
    });
    return model;
  }

  public getBillingCodeFullName(code: ClientBillingCode) {
    if (!code) return '';
    const parts = [
      code.serviceName,
      code.billingCodeName,
      code.billingCodeName2,
    ].filter((p) => !!p);
    return parts.join('/');
  }
  public getBillingCodeName(code: ClientBillingCode) {
    if (!code) return '';
    const main = code.billingCodeName2
      ? code.billingCodeName2
      : code.billingCodeName;
    if (code.serviceName && main) return code.serviceName + '/' + main;
    return main || code.serviceName || '';
  }

  public onIndividualDateOfServiceChanged(row: ClaimDetailsModel) {
    this.syncSecondaryDos(row);
    this.formCheck();
  }

  public formatSelectedBillingCode(selected: any): string {
    // Return empty so placeholder 'Select' is only rendered by HTML template, not coming from TS logic
    if (
      !selected ||
      (typeof selected === 'object' && selected.billingCodeId == null)
    )
      return '';
    if (typeof selected === 'object')
      return this.getBillingCodeName(selected as ClientBillingCode);
    const found = this.billingCodes?.find((b) => b.billingCodeId === selected);
    return found ? this.getBillingCodeName(found) : '';
  }

  getGridPageSizes(): void {
    const storedGridPageSizes = localStorage.getItem('gridPageSizes')
      ? JSON.parse(localStorage.getItem('gridPageSizes') || '')
      : null;
    if (storedGridPageSizes) {
      this.gridPageSizes = storedGridPageSizes;
    } else {
      this.claimsService
        .getGridPageSizes()
        .subscribe((sizes: Array<number | { text: string; value: number }>) => {
          this.gridPageSizes = sizes;
        });
    }
  }
  // Called when units or perUnitsCharge changes for a new row
  onNewRowValueChange(row: any) {
    // Only update totals if both units and perUnitsCharge are provided and valid
    if (
      row.units != null &&
      row.perUnitsCharge != null &&
      row.units !== '' &&
      row.perUnitsCharge !== '' &&
      !isNaN(row.units) &&
      !isNaN(row.perUnitsCharge)
    ) {
      // Update the row's billed amount (if needed)
      row.billedAmount = row.units * row.perUnitsCharge;
      // Recalculate totals for the grid
      this.calculateFooterTotals();
    } else {
      // If either value is missing, do not update totals
      row.billedAmount = 0;
      this.calculateFooterTotals();
    }
  }

  // Recalculate grid footer totals for newRows and view.data
  calculateFooterTotals() {
    // Combine all rows (existing + new) – currently only view.data holds rows
    const allRows = [...(this.view.data || [])];
    // Calculate totals for each column
    this.claimChargeDetailsShared.billedAmountTotal = allRows.reduce(
      (sum, row) =>
        sum +
        (row.units && row.perUnitsCharge ? row.units * row.perUnitsCharge : 0),
      0
    );
    this.claimChargeDetailsShared.paymentAmountTotal = allRows.reduce(
      (sum, row) => sum + (row.paymentAmount || 0),
      0
    );
    this.claimChargeDetailsShared.patientAmountTotal = allRows.reduce(
      (sum, row) => sum + (row.patientAmount || 0),
      0
    );
    this.claimChargeDetailsShared.adjustmentAmountTotal = allRows.reduce(
      (sum, row) => sum + (row.adjustmentAmount || 0),
      0
    );
    this.claimChargeDetailsShared.balanceAmountTotal = allRows.reduce(
      (sum, row) =>
        sum +
        ((row.units && row.perUnitsCharge
          ? row.units * row.perUnitsCharge
          : 0) +
          (row.patientAmount || 0) -
          (row.paymentAmount || 0) +
          (row.adjustmentAmount || 0)),
      0
    );
  }

  getPageStart(total: number): number {
    if (!total) return 0;
    const skip = this.gridState?.skip || 0;
    return Math.min(skip + 1, total);
  }

  getPageEnd(total: number): number {
    if (!total) return 0;
    const skip = this.gridState?.skip || 0;
    const take = this.gridState?.take || 0;
    if (take === 0) return total;
    return Math.min(skip + take, total);
  }
}
