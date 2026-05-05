import { Injectable } from '@angular/core';
import { BasicOption } from '@core/models/common';
import { HttpService } from '@core/services';
import { BehaviorSubject, Observable } from 'rxjs';
import { ClientOptionModel} from '@core/models/clients';
import { ClaimCreateInfoGetModel, PlaceOfServiceServerModel ,} from '@core/models/billing';
import { ClientFunderModel, FunderServiceLine } from '@core/models/company-account/funders/client-funder-model';
import { Authorization, AuthorizationDiagnosisCode } from "@core/models/clients/authorization";
import { Locale } from '@app/locale';
import { AuthGridCache, AuthEditInfoCache } from '@core/models/clients';
import { environment } from 'src/environments/environment';
import { ClientsForClaimModel } from '@core/models/clients/ClientsForClaimModel';
import { ClientFundersSmallModel } from '@core/models/clients/Client-Funders-Small-model';
import { AccountMemberService } from '../account/account-member.service';


@Injectable({
    providedIn: 'root'
})
export class ClientsService {
    private clientDemographicsSubject = new BehaviorSubject<any | null>(null);
    readonly clientDemographics: Observable<any> = this.clientDemographicsSubject.asObservable();
    apiBaseUrl: string;

    clearCache() {
        this.authEditInfoCache.clear();
        this.authGridCache.clear();
    }

    authGridCache: AuthGridCache;
    authEditInfoCache: AuthEditInfoCache;

    constructor(private http: HttpService, private accountService: AccountMemberService) {
        this.authGridCache = new AuthGridCache();
        this.authEditInfoCache = new AuthEditInfoCache();
        this.apiBaseUrl = environment.clientApiBaseUrl;
    }

    getAuthorization(authorizationId: number | string, clientId: number) {
        const locale = new Locale();
        return this.http.post<Authorization>(this.apiBaseUrl + '/Client/GetClientAuthorization',
            { authorizationId, childProfileId: clientId, localeString: locale.userDateTimeFormat ,accountInfoId:this.accountService.memberDetails.accountInfoId});
    }

    getClientAuthorizationsForClaim(clientId: number, funderId: number, clientFunderServiceLineId: number, accountInfoId: number): Observable<BasicOption[]> {
        return this.http.post(this.apiBaseUrl + '/client/GetClientAuthorizationsForClaim', {
            childProfileId: clientId,
            funderId: funderId,
            clientFunderServiceLineId: clientFunderServiceLineId,
            accountInfoId: accountInfoId
        });
    }

    getClientsForClaim(model :ClientsForClaimModel): Observable<ClientOptionModel[]> {
        return this.http.post(this.apiBaseUrl + '/Client/GetClientsForClaim', model);
    }
	
	getClaimCreateInfo(model: ClaimCreateInfoGetModel) {
        return this.http.post(this.apiBaseUrl + '/client/GetClaimCreateInfo', model);
    }
	getDiagnosisForClaimWithoutAuth(clientId: number, serviceLineId: number, accountInfoId: number) {
        return this.http.post<AuthorizationDiagnosisCode[]>(this.apiBaseUrl + '/client/GetDiagnosisForClaimWithoutAuth', { childProfileId: clientId, serviceLineId, accountInfoId: accountInfoId });
    }
	
	 getPlacesOfService(accountInfoId:number): Observable<PlaceOfServiceServerModel[]> {
        return this.http.post(this.apiBaseUrl + '/client/GetPlacesOfService', {accountInfoId,MemberId:this.accountService.memberDetails.memberId});
    }

    getClientFundersSmall(model: ClientFundersSmallModel): Observable<ClientFunderModel[]> {
        return this.http.post(this.apiBaseUrl + '/client/GetClientFundersSmall', model);
    }

    getFundersServiceLines(clientId: number, funderId: number, id: number): Observable<FunderServiceLine[]> {
        return this.http.post(this.apiBaseUrl + '/client/GetFunderServiceLines',  { ClientId: clientId, FunderId: funderId, Id: id ,AccountInfoId:this.accountService.memberDetails.accountInfoId});
    }

    getClientFacilityId(childProfileId: number): Observable<number> {
        return this.http.post(this.apiBaseUrl + '/client/GetClientFacilityId',{ accountInfoId:this.accountService.memberDetails.accountInfoId,childProfileId});
    }
	
	getClientFunderResponsibleParties(clientId: number, clientFunderId: number): Observable<any> {
        return this.http.post(this.apiBaseUrl + '/client/GetClientFunderResponsibleParties', { ChildProfileId: clientId, ClientFunderId: clientFunderId ,AccountInfoId:this.accountService.memberDetails.accountInfoId,MemberId:this.accountService.memberDetails.memberId});
        // const url: string = this.jsonbaseUrl + '/GetClientFunderResponsibleParties.json';
        // return this.http.get(url, {});
    }

    searchDiagnosisCodes(req: any): Observable<any> {
        return this.http.post(this.apiBaseUrl + '/client/SearchDiagnosis', req);
        // const url: string = this.jsonbaseUrl + '/SearchDiagnosisCodes.json';
        // return this.http.get(url, {});
    }
}