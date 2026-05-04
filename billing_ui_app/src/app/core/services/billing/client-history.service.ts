import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Encounter, ListFilterSort } from '@core/models/billing';
import { ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ClaimPatientGetModel } from '@core/models/billing/claim-patient-get-model';
import { ClientHistory, ClientHistoryChargeDetails, ClientHistoryChargeDetailsRequestModel, ClientHistoryChargeDetailsResponse, ClientHistoryGrid, ClientHistoryRequestModel, ClientInvoiceHistoryRequestModel, ClientInvoiceHistoryResponse } from '@core/models/billing/client-history';
import { Observable, Subject } from 'rxjs';
import { environment } from 'src/environments/environment';
import { HttpService } from '../http.service';
import { RequestUserData } from '@core/utils/request-user-data';
import { AccountMemberService } from '../account/account-member.service';
import { Locale } from '@app/locale';
import { Authorization } from '@core/models/clients/authorization';
import { BasicOption } from '@core/models/common';

@Injectable({
  providedIn: 'root'
})
export class ClientHistoryService {
  private apiBaseUrl = environment.claimApiBaseUrl;
  private apiBillingUrl: string;
  private clientData: any;
  public onLoad: Subject<Encounter>;
  constructor(private https: HttpClient,private http: HttpService, private reqUserData: RequestUserData, private accountService: AccountMemberService) { 
            this.onLoad = new Subject<Encounter>();
            this.apiBaseUrl = environment.claimApiBaseUrl;
           // this.apiBillingUrl = environment.billingApiBaseUrl;
  }

  GetLocation(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiBaseUrl}/GetLocation`);
  }

  GetClientName(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiBaseUrl}/GetClientName`);
  }

  GetFunder(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiBaseUrl}/GetFunder`);
  }

  GetClientId(): Observable<number[]> {
    return this.http.get<number[]>(`${this.apiBaseUrl}/GetClientId`);
  }

  getFilteredClientHistory(filters: any): Observable<ClientHistory[]> {
    return this.http.post<ClientHistory[]>(`${this.apiBaseUrl}/GetClientRecords`, filters);
  }
  getFilteredClientHistoryCharges(filters: any): Observable<ClientHistory[]> {
    return this.http.post<ClientHistory[]>(`${this.apiBaseUrl}/GetFilteredClientHistoryCharges`, filters);
  }
  // Client History Api

  GetAllClientHistoryDetails(model: ClientHistoryRequestModel, showSpinner: boolean = true): Observable<ClientHistoryGrid> {
    return this.http.post<ClientHistoryGrid>(`${this.apiBaseUrl}/ClientChargeHistory/GetClientRecords`, model, { showSpinner });
  }
// Charge Api
  GetClientHistoryChargeDetails(model: ClientHistoryChargeDetailsRequestModel, showSpinner: boolean = true): Observable<ClientHistoryChargeDetailsResponse> {
    return this.http.post<ClientHistoryChargeDetailsResponse>(`${this.apiBaseUrl}/ClientChargeHistory/GetClientChargeHistoryDetails`, model, { showSpinner });
  }
  
  setClientData(data: any) {
    this.clientData = data;
    localStorage.setItem('clientHistoryData', JSON.stringify(data));
  }

  getClientData() {
    return this.clientData || JSON.parse(localStorage.getItem('clientHistoryData'));
  }

   

   getClaimRenderingProviders(model: ClaimPatientGetModel) {
          return this.http.post<ClaimFilterOptionModel[]>(this.apiBaseUrl + '/Claim/GetClaimRenderingProviders', model, { showSpinner: false });
      }
    
  getClientAuthorizationsForClaim(clientId: number, funderId: number, clientFunderServiceLineId: number, accountInfoId: number): Observable<BasicOption[]> {
          return this.http.post(this.apiBaseUrl + '/client/GetClientAuthorizationsForClaim', {
              childProfileId: clientId,
              funderId: funderId,
              clientFunderServiceLineId: clientFunderServiceLineId,
              accountInfoId: accountInfoId
          });
      }
    getClaimFunders(model: ClaimPatientGetModel) {
        return this.http.post<ClaimFilterOptionModel[]>(this.apiBaseUrl + '/Claim/GetClaimFunders', model, { showSpinner: false });
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

      //filter list populate
      getClientListByIds(): Observable<ClaimFilterOptionModel[]> {
        var model = {
          accountInfoId: this.accountService.memberDetails.accountInfoId,
          memberId: this.accountService.memberDetails.memberId
        }
        return this.http.post<ClaimFilterOptionModel[]>(this.apiBillingUrl + '/AppointmentReports/GetClientListByIds', model, { showSpinner: false });
      }

    getClientInvoiceHistory(model: ClientInvoiceHistoryRequestModel, showSpinner: boolean = true): Observable<ClientInvoiceHistoryResponse> {
      return this.http.post<ClientInvoiceHistoryResponse>(`${this.apiBaseUrl}/ClientChargeHistory/Search`, model, { showSpinner });      
    } 
}
