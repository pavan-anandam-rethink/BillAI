import { Injectable } from '@angular/core';
import { HttpService } from "@core/services/http.service";
import { BehaviorSubject, Observable, Subject } from 'rxjs';

import {
  PaymentPosting,
  PaymentPostingGrid,
  ListFilterSort,
  PaymentPostingMethods,
  PaymentPostingListFunderSearchBase,
  PaymentPostingFunderSearch,
  PaymentPostingShortInfo,
  PaymentSummary,
  ManualPaymentPatientSearchBase,
  ManualPaymentPatientSearch,
  PaymentEOBInfo,
  UpdateManualPaymentSummary,
  ManualCreatePayment
} from '@core/models/billing';
import { share } from 'rxjs/operators';
import { UpdatePaymentSummary } from "@core/models/billing/update-payment-summary";
import { PaymentProcessingDetails } from "@core/models/billing/payment-processing-details";
import { environment } from 'src/environments/environment';
import { AccountMemberService } from '../account/account-member.service';
import { PaymentPostingBulkModel } from '../../models/billing/cliam-posting';
import { BulkPaymentResponse } from '../../models/billing/bulk-payment-response';
import { HttpHeaders } from '@angular/common/http';
import { AddOrEditAdjustmentModel } from '../../models/billing/save-bulk-request';
import { UnAllocatedPaymentsModel } from '@core/models/billing/payment-posting';
import { UnallocatedManualCreatePayment } from '@core/models/billing/manual-create-payment';
import { ClaimFilterOptionModel, ClientHistoryUserInfo, ClientGuarantorInfo, ClaimEdiFilesModel } from '@core/models/billing/claim-filter-option-model';
import { RevSpringPayloadRequestModel } from '@core/models/billing/revspring-payload.request';
import { RevSpringPayloadResponse } from '@core/models/billing/revspring-payload.response';
import { EdiFileType } from '@core/enums/billing/edi-file-type';

@Injectable()
export class PaymentPostingService {
  private lastPaymentId = 0;
  private paymentSummary: Observable<PaymentSummary> | null = null;
  private paymentMethods: Observable<PaymentPostingMethods[]> | null = null;
  private apiBaseUrl = environment.claimApiBaseUrl;

  public onLoad: Subject<PaymentPosting>;

  constructor(private http: HttpService, private accountService: AccountMemberService) {
    this.onLoad = new Subject<PaymentPosting>();
  }


  private paymentPostingIdSubject = new BehaviorSubject<number | null>(null);

  // Expose the observable to components
  private paymentPostingId$ = this.paymentPostingIdSubject.asObservable();

  // Set the ID value
  setId(id: number): void {
    this.paymentPostingIdSubject.next(id);
  }

  // Get the ID as an observable
  getId(): Observable<number | null> {

    return this.paymentPostingId$;
  }

  private paymentPostingDataSubject = new BehaviorSubject<any>(null);

  private paymentPostingData$ = this.paymentPostingDataSubject.asObservable();

  // Set the data value
  setData(data: any): void {
    this.paymentPostingDataSubject.next(data);
  }

  // Get the data as an observable
  getData(): Observable<any> {
    return this.paymentPostingData$;
  }

  getBulkData(modelData: PaymentPostingBulkModel): Observable<BulkPaymentResponse[]> {

    const token = '86D1AB62-606F-46AC-94E7-35390F8A5379'
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
      /*   'Authorization': `Bearer ${token}`*/
    });

    return this.http.post<BulkPaymentResponse[]>(
      this.apiBaseUrl + '/BulkPaymentPosting/GetAllPaymentsForPosting',
      modelData, { headers }
    );
  }

  saveData(saveDataModel: AddOrEditAdjustmentModel[]): Observable<any[]> {

    const token = '86D1AB62-606F-46AC-94E7-35390F8A5379'
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
      /*   'Authorization': `Bearer ${token}`*/
    });

    return this.http.post<any[]>(
      this.apiBaseUrl + '/BulkPaymentPosting/AddOrUpdateBulkPaymentPostingAdjustments',
      saveDataModel, { headers }
    );
  }

  getAll(params: ListFilterSort, showSpinner: boolean = true): Observable<PaymentPostingGrid> {
    var model = {
      ...params,
      AccountInfoId: this.accountService.memberDetails.accountInfoId,
      MemberId: this.accountService.memberDetails.memberId
    };
    return this.http.post<PaymentPostingGrid>(this.apiBaseUrl + '/PaymentPosting/GetPayments', model, { showSpinner });
  }

  getSummaryById(id: number) {
    if (!this.paymentSummary || this.lastPaymentId != id) {
      this.lastPaymentId = id;
      this.paymentSummary = this.http.post<PaymentSummary>(this.apiBaseUrl + '/PaymentPosting/GetPaymentSummary', id)
        .pipe(share());
    }

    return this.paymentSummary;
  }

  updateManualPaymentSummary(reqModel: UpdateManualPaymentSummary) {
    var model = {
      ...reqModel,
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId
    }
    return this.http.post(this.apiBaseUrl + '/PaymentPosting/UpdateManualPaymentSummary', model);
  }

  updatePaymentSummary(model: UpdatePaymentSummary) {
    return this.http.post(this.apiBaseUrl + '/PaymentPosting/UpdatePaymentSummary', model);
  }

  getEOBInfoById(id: number) {
    return this.http.post<PaymentEOBInfo>(this.apiBaseUrl + '/PaymentPosting/GetEOBPaymentInfo', id);
  }

  getEdiFilesFromBlobData(ediModel: ClaimEdiFilesModel): Observable<string> {
    const requestModel = {
        accountInfoId: this.accountService.memberDetails.accountInfoId,
        memberId: this.accountService.memberDetails.memberId,
        fileType: ediModel.FileType || EdiFileType.EDI835,
        claimId: ediModel.ClaimId || 0,
        paymentId: ediModel.PaymentId || 0,
        batchId: ediModel.BatchId || null,
      };
      return this.http.post(
        this.apiBaseUrl + '/EdiFile/GetEdiFilesFromBlob',requestModel,{ responseType: 'text' as 'json' }
      );
  }

  getPaymentMethods() {
    if (!this.paymentMethods) {
      this.paymentMethods = this.http.post<PaymentPostingMethods[]>(this.apiBaseUrl + '/PaymentPosting/GetPaymentMethods', {}, { showSpinner: false })
        .pipe(share());
    }

    return this.paymentMethods;
  }

  getReconcileStatuses() {
    return this.http.post<string[]>(this.apiBaseUrl + '/PaymentPosting/GetReconcileStatuses', {}, { showSpinner: false });
  }

  getFunders(searchData: PaymentPostingListFunderSearchBase) {
    return this.http.post<PaymentPostingFunderSearch>(this.apiBaseUrl + '/PaymentPosting/GetFunders', searchData, { showSpinner: false });
  }

  getAssignedFunders(searchData: PaymentPostingListFunderSearchBase) {
    var model = {
      ...searchData,
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId
    }
    return this.http.post<PaymentPostingFunderSearch>(this.apiBaseUrl + '/PaymentPosting/GetAssignedFunders', model, { showSpinner: false });
  }

  getPaymentShortInfo(id: number) {
    return this.http.post<PaymentPostingShortInfo>(this.apiBaseUrl + '/PaymentPosting/GetPaymentShortInfo', id);
  }

  manualCreatePatientPayment(model: UnallocatedManualCreatePayment): Observable<number> {
    return this.http.post<number>(this.apiBaseUrl + '/PaymentPosting/ManualCreatePayment', model);
  }

  getPatients(searchData: ManualPaymentPatientSearchBase) {
    return this.http.post<ManualPaymentPatientSearch[]>(this.apiBaseUrl + '/PaymentPosting/GetPatients', searchData);
  }

  deletePayment(paymentId: number[]) {
    return this.http.post<boolean>(this.apiBaseUrl + '/PaymentPosting/DeletePayment', { "paymentId": paymentId, "memberId": this.accountService.memberDetails.memberId,"accountInfoId": this.accountService.memberDetails.accountInfoId  });
  }

  reconcilePayment(paymentId: number[]) {
    return this.http.post(this.apiBaseUrl + '/PaymentPosting/ReconcilePayment', { "paymentId": paymentId, "memberId": this.accountService.memberDetails.memberId });
  }

  reconcileClaimPayment(paymentId: number[], claimId: number) {
    return this.http.post(this.apiBaseUrl + '/PaymentPosting/ReconcileClaim', { "paymentId": paymentId, "claimId": claimId, "memberId": this.accountService.memberDetails.memberId });
  }

  getProcessingPayments() {
    var model = {
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId
    }
    return this.http.post<PaymentProcessingDetails[]>(this.apiBaseUrl + '/PaymentPosting/GetProcessingPayments', model);
  }

  startPaymentParsing(id: number) {
    return this.http.post(this.apiBaseUrl + '/PaymentPosting/StartPaymentParsing', { 'memberId': this.accountService.memberDetails.memberId, 'accountInfoId': this.accountService.memberDetails.accountInfoId, 'id': id })
  }

  hideProcessingInfo(ids: number[]) {
    var model = {
      ids: ids,
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId
    }
    return this.http.post(this.apiBaseUrl + '/PaymentPosting/HideProcessingInfo', model)
  }

  addUnAllocatedPayment(model: UnAllocatedPaymentsModel) {
    return this.http.post(this.apiBaseUrl + '/PaymentPosting/AddUnAllocatedPayments', model);
  }

  getUnAllocatedPayments(model: UnAllocatedPaymentsModel) {
    return this.http.post(this.apiBaseUrl + '/PaymentPosting/GetUnAllocatedPayments', model);
  }

  getGuarantorDetails(clientId?: number): Observable<ClientGuarantorInfo[]> {
    var model = {
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId,
      clientId: clientId
    }
    return this.http.post<ClientGuarantorInfo[]>(this.apiBaseUrl + '/PaymentPosting/GetGuarantorDetailsById', model);
  }

  getRevSpringPayload(model: RevSpringPayloadRequestModel): Observable<RevSpringPayloadResponse> {
    return this.http.post<RevSpringPayloadResponse>(
      this.apiBaseUrl + '/PaymentPosting/GetRevSpringPayload',
      model,
      { showSpinner: true }
    );
  }
}
