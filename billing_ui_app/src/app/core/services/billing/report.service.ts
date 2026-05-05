import { Injectable } from '@angular/core';
import { Encounter } from '@core/models/billing/encounter';
import { Subject } from 'rxjs';
import { HttpService } from '../http.service';
import { RequestUserData } from '@core/utils/request-user-data';
import { AccountMemberService } from '../account/account-member.service';
import { environment } from 'src/environments/environment';
import { PatientInvoiceHeaderSearch } from '@core/models/billing/patient-invoice';
import { AccountsReceivablesChargeLevelRequestModel, AccountsReceivablesChargeLevelResponseModel, AccountsReceivablesRequestModel, AccountsReceivablesResponseModel, claimFollowUpRequestModel, ClaimFollowUpResponseModel, FunderDetails, paymentsAdjustmentsRequestModel, PaymentsAdjustmentsResponseModel, UnbilledAppointmentsRequestModel, UnbilledAppointmentsResponse, UnbilledAppointmentsResponseModel, UnprocessedAppointmentsResponseModel } from '@core/models/billing/report-model';
import { Observable, switchMap } from 'rxjs';
import { IdsWithUserInfo } from '@core/models/billing/get-claim-by-identifier';
import { ClaimFilterOptionModel, ClientHistoryUserInfo } from '@core/models/billing/claim-filter-option-model';
import { FinancialSummaryBaseRequest } from '@core/models/billing/monthly-financial-summary-request';
import { PaymentPostingListFunderSearchBase, PaymentPostingFunderSearch} from '@core/models/billing';

@Injectable({
  providedIn: 'root'
})
export class ReportService {

  public onLoad: Subject<Encounter>;
  private apiBaseUrl: string;
  private apiBillingUrl: string;
  private claimapiBaseUrl: string;
  constructor(private http: HttpService, private reqUserData: RequestUserData, private accountService: AccountMemberService) {
    this.onLoad = new Subject<Encounter>();
    this.apiBaseUrl = environment.reportingApiBaseUrl;
    this.apiBillingUrl = environment.claimApiBaseUrl;
    this.claimapiBaseUrl = environment.claimApiBaseUrl;
  }

  paymentsAdjustmentsExportToExcel(model: paymentsAdjustmentsRequestModel): Observable<Blob> {
    model.accountInfoId = this.accountService.memberDetails.accountInfoId;
    return this.http.post<any>(this.apiBaseUrl + '/PaymentAdjustment/ExportToExcel', model);
  }

  claimFollowupExportToExcel(model: claimFollowUpRequestModel): Observable<Blob> {

    model.accountInfoId = this.accountService.memberDetails.accountInfoId;
    return this.http.post<any>(this.apiBaseUrl + '/PaymentAdjustment/ExportToExcelClaimFollow', model);
  }


  getAccountsReceivables(model: AccountsReceivablesRequestModel,
    showSpinner: boolean = true): Observable<AccountsReceivablesResponseModel> {
    model.accountInfoId = this.accountService.memberDetails.accountInfoId;
    return this.http.post<AccountsReceivablesResponseModel>(
      this.apiBaseUrl + '/AccountsReceivable/GetAccountsReceivables',
      model,
      { showSpinner }
    );
  }

  getPaymentsAdjustments(model: paymentsAdjustmentsRequestModel ,showSpinner: boolean = true) {
    model.accountInfoId = this.accountService.memberDetails.accountInfoId;
    return this.http.post<PaymentsAdjustmentsResponseModel>(this.apiBaseUrl + '/PaymentAdjustment/GetPaymentsAdjustments', model,
       { showSpinner }
    );
  }

  getClaimFollowup(model: claimFollowUpRequestModel,showSpinner: boolean = true) {

    model.accountInfoId = this.accountService.memberDetails.accountInfoId;
    return this.http.post<ClaimFollowUpResponseModel>(this.apiBaseUrl + '/PaymentAdjustment/GetClaimFollowUpReport', model, { showSpinner });

  }

  getUnbilledAppointments(model: UnbilledAppointmentsRequestModel) {
    model.accountInfoId = this.accountService.memberDetails.accountInfoId;
    model.memberId = this.accountService.memberDetails.memberId;
    return this.http.post<UnbilledAppointmentsResponseModel>(this.apiBillingUrl + '/AppointmentReports/GetUnbilledAppointments', model);
  }

  getUnprocessedAppointments(model: UnbilledAppointmentsRequestModel) {
    model.accountInfoId = this.accountService.memberDetails.accountInfoId;
    model.memberId = this.accountService.memberDetails.memberId;
    return this.http.post<UnprocessedAppointmentsResponseModel>(this.apiBillingUrl + '/AppointmentReports/GetUnprocessedAppointments', model);
  }

  getUnbilledAppointmentsWithoutMasterData() {
    return this.http.post<UnbilledAppointmentsResponseModel>(this.apiBillingUrl + '/AppointmentReports/GetUnbilledAppointmentsWithoutMasterData', this.accountService.memberDetails.accountInfoId);
  }

  getFunders() {
    let accountInfoId = this.accountService.memberDetails.accountInfoId;
    return this.http.post<FunderDetails[]>(`${this.apiBaseUrl}/AccountsReceivable/GetFunders/${accountInfoId}`, {});
  }

  accountsReceivablesExportToExcel(model: AccountsReceivablesRequestModel): Observable<Blob> {
    model.accountInfoId = this.accountService.memberDetails.accountInfoId;
    return this.http.post<any>(this.apiBaseUrl + '/AccountsReceivable/ExportToExcel', model);
  }

  //Charge Level AR and Export to excel Api Call here
  getAccountsReceivablesChargeLevel(model: AccountsReceivablesChargeLevelRequestModel, showSpinner: boolean = true) {
    model.accountInfoId = this.accountService.memberDetails.accountInfoId;
    return this.http.post<AccountsReceivablesChargeLevelResponseModel>(this.apiBaseUrl + '/AccountsReceivable/GetAccountsReceivablesChargeLevel', model,
      { showSpinner }
    )
     
  }

  accountsReceivablesChargeLevelExportToExcel(model: AccountsReceivablesChargeLevelRequestModel): Observable<Blob> {
    model.accountInfoId = this.accountService.memberDetails.accountInfoId;
    return this.http.post<any>(this.apiBaseUrl + '/AccountsReceivable/ExportToExcelChargeLevel', model);
  }

  createClaims(selectedIds: number[]) {
    var model: IdsWithUserInfo = {
      Ids: selectedIds,
      AccountInfoId: this.accountService.memberDetails.accountInfoId,
      MemberId: this.accountService.memberDetails.memberId
    };
    return this.http.post<FunderDetails[]>(this.apiBillingUrl + '/AppointmentReports/CreateClaimsForUnbilledAppointments', model);
  }

  //filter list populate
  getClientListByIds(): Observable<ClaimFilterOptionModel[]> {
    var model = {
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId
    }
    return this.http.post<ClaimFilterOptionModel[]>(this.apiBillingUrl + '/AppointmentReports/GetClientListByIds', model, { showSpinner: false });
  }
  getFunderListByIds(): Observable<ClaimFilterOptionModel[]> {
    var model = {
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId
    }
    return this.http.post<ClaimFilterOptionModel[]>(this.apiBillingUrl + '/AppointmentReports/GetFunderListByIds', model, { showSpinner: false });
  }

   getAssignedFunders() {
    var model = {
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId,
      take: 1000000
    }
    return this.http.post<PaymentPostingFunderSearch>(this.claimapiBaseUrl + '/PaymentPosting/GetAssignedFunders', model, { showSpinner: false });
  }

  getPrimaryFunderListByIds(clientId?: number): Observable<ClaimFilterOptionModel[]> {
    var model = {
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId,
      clientId: clientId
    }
    return this.http.post<ClaimFilterOptionModel[]>(this.apiBillingUrl + '/AppointmentReports/GetClientHistoryFunderListByIds', model, { showSpinner: false });
  }
getStaffListByIds(): Observable<ClaimFilterOptionModel[]> {
    var model = {
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId
    }
    return this.http.post<ClaimFilterOptionModel[]>(this.apiBillingUrl + '/AppointmentReports/GetStaffListByIds', model, { showSpinner: false });
  }
  getPoSListByIds(): Observable<ClaimFilterOptionModel[]> {
    var model = {
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId
    }
    return this.http.post<ClaimFilterOptionModel[]>(this.apiBillingUrl + '/AppointmentReports/GetPoSListByIds', model, { showSpinner: false });
  }
  getLocationListByIds(): Observable<ClaimFilterOptionModel[]> {
    var model = {
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId
    }
    return this.http.post<ClaimFilterOptionModel[]>(this.apiBillingUrl + '/AppointmentReports/GetLocationListByIds', model, { showSpinner: false });
  }

  getAllAuthorizationNumbers(): Observable<ClaimFilterOptionModel[]> {
    var model = {
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId,
      clientId: this.accountService.memberDetails.clientId
    }
    return this.http.post<ClaimFilterOptionModel[]>(this.apiBillingUrl + '/ClientChargeHistory/GetAllAuthorizationNumbers', model, { showSpinner: false });
  }
  getMonthlyFinancialSummary(
    request: FinancialSummaryBaseRequest
  ): Observable<any> {
    request.accountInfoId = this.accountService.memberDetails.accountInfoId;
    return this.http.post<any>(
      this.apiBaseUrl + '/FinancialReports/GetMonthlyFinancialSummary',
      request
    );
  }

  getFunderFinancialSummary(
        request: FinancialSummaryBaseRequest
    ): Observable<any> {
        request.accountInfoId = this.accountService.memberDetails.accountInfoId;
        return this.http.post<any>(
            this.apiBaseUrl + '/FinancialReports/GetFunderFinancialSummary',
            request
        );
    }
    
  exportMonthlyFinancialSummaryToExcel(
  request: FinancialSummaryBaseRequest
) {
  request.accountInfoId = this.accountService.memberDetails.accountInfoId;

  return this.http.post<any>(
    `${this.apiBaseUrl}/FinancialReports/ExportToExcel`,
    request
  );
}

  exportFunderFinancialSummaryToExcel(request: FinancialSummaryBaseRequest) 
  {
    request.accountInfoId = this.accountService.memberDetails.accountInfoId;
    return this.http.post<any>(
      `${this.apiBaseUrl}/FinancialReports/ExportFunderToExcel`,
      request
    );
  }

  getLatestBillingProviders(): Observable<any[]> {
    var model = {
      accountInfoId: this.accountService.memberDetails.accountInfoId,
      memberId: this.accountService.memberDetails.memberId
    }  

    return this.http.post<any[]>(this.apiBillingUrl + `/Claim/GetLatestBillingProviders`, model , { showSpinner: false });
  }
}
