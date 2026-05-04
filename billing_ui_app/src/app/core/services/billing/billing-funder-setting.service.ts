import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { HttpService } from '../http.service';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { BillingFeatures, BillingFunderIdRequestModel, BillingSettingInformationModel, ClaimFilingIndicatorModel, SaveBillingSettingRequest } from '../../models/billing/claim-filingIndicator-model';
import { BillingFunderSettingRequestModel, BillingFunderSettingResponseModel, BillingFunderListRequestModel } from '../../models/billing/billingFunderSetting-model';
import { UnbilledAppointmentsResponseModel } from '../../models/billing/report-model';
import { AccountMemberService } from '../account/account-member.service';



@Injectable({
  providedIn: 'root'
})
export class BillingFunderSettingService {
  private apiBaseUrl = environment.claimApiBaseUrl;

  constructor(
    private http: HttpClient,
    private httpService: HttpService,
    private accountService: AccountMemberService
  ) { }

  getAllClaimFilingIndicators(): Observable<ClaimFilingIndicatorModel[]> {
    return this.http.get<ClaimFilingIndicatorModel[]>(
      `${this.apiBaseUrl}/BillingSettings/GetClaimFilingIndicators`
    );
  }

  getBillingSettingInformation(): Observable<BillingSettingInformationModel> {
    const accountInfoId = this.accountService.memberDetails.accountInfoId;

    return this.http.get<BillingSettingInformationModel>(
      `${this.apiBaseUrl}/BillingSettings/GetBillingSettingInformation/${accountInfoId}`
    );
  }

  getDefaultBilling(): Observable<BillingSettingInformationModel> {
    const accountInfoId = this.accountService.memberDetails.accountInfoId;

    return this.http.get<BillingSettingInformationModel>(
      `${this.apiBaseUrl}/BillingSettings/GetDefaultBilling/${accountInfoId}`
    );
  }

  SaveBillingSettings(model: SaveBillingSettingRequest): Observable<void> {
    model.accountId = this.accountService.memberDetails.accountInfoId;
    const memberId = this.accountService.memberDetails.memberId;
    return this.http.post<void>(
      `${this.apiBaseUrl}/BillingSettings/SaveBillingSettings?memberId=${memberId}`,
      model,
      { headers: { 'Content-Type': 'application/json' } } 
    );
  }

  // POST Set Billing Funder Settings (returns 204 No Content)
  setBillingFunderSettings(request: BillingFunderSettingRequestModel): Observable<void> {
    return this.http.post<void>(
      `${this.apiBaseUrl}/BillingSettings/SetBillingFunderSettings`,
      request
    );
  }

  deleteBillingFunderSetting(id: number): Observable<void> {
    return this.http.delete<void>(
      `${this.apiBaseUrl}/BillingSettings/DeleteFunder/${id}`
    );
  }

  getBillingFunderSettings(
    model: BillingFunderListRequestModel
  ): Observable<BillingFunderSettingResponseModel> {
    model.accountInfoId = this.accountService.memberDetails.accountInfoId;
    model.memberId = this.accountService.memberDetails.memberId;

    return this.http.post<BillingFunderSettingResponseModel>(
      `${this.apiBaseUrl}/BillingSettings/GetBillingFunderSettings`,
      model
    );
  }

  getBillingFeatures() {
    const accountInfoId = this.accountService.memberDetails.accountInfoId;
    return this.httpService.get<BillingFeatures[]>(`${this.apiBaseUrl}/BillingSettings/GetBillingFeatures?accountId=${accountInfoId}`)
  }

  getBillingFunderIdsSetting(funderId: number) {
  const accountInfoId = this.accountService.memberDetails.accountInfoId;
  return this.httpService.get<BillingFunderIdRequestModel>(
    `${this.apiBaseUrl}/BillingSettings/GetBillingFunderIdsSetting`,
    {
      params: {
        funderId: funderId,
        accountInfoId: accountInfoId
      }
    },
    true
  );
 }
 
saveBillingFunderSettings(payload: any,showSpinner: boolean = true) {
    payload.accountInfoId = this.accountService.memberDetails.accountInfoId;
    payload.changedBy = this.accountService.memberDetails.memberId;
  return this.httpService.post<any>(
    `${this.apiBaseUrl}/FunderSetting/SaveFunderSettings`,
    payload, { showSpinner }
  );
}
}
