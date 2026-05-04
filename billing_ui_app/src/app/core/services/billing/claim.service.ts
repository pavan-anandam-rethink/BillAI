import { Injectable } from '@angular/core';
import { Subject } from 'rxjs/internal/Subject';
import { HttpService } from '@core/services';
import {
    Appointment,
    ClaimDetailsInfoModel,
    ClaimDetailsListFilterSort,
    ClaimDetailsModel,
    ClaimOptions,
    ClaimUpdateDetailsModels,
    ClaimUpdateModifiersModel,
    Encounter, PaymentClaimsSearch, PaymentClaimsSearchBase,
    StateInformation
} from '@core/models/billing';
import { ClaimSaveRequestModel } from '@core/models/billing/claim-save-model';
import { ClaimBillingProviderDto } from '@core/models/billing/claim-billing-provider';
import { Observable, map, share } from 'rxjs';
import { State } from '@progress/kendo-data-query';
import { ActionResponseResult } from '@core/models/common/action-response-result';
import * as moment from 'moment';
import { ClaimErrorAlertModel } from '@core/models/billing/claim-errors-alerts';
import { ClaimHeader, ClaimHeaderSearch } from '@core/models/billing/claim-header-search';
import { ClaimHistoryActionModel } from '@app/billing/encounters/encounter-view/encounter-transaction/mapper/claim-history-mapper';
import { ClaimHistory } from '@core/models/billing/claim-history';
import { ClaimErrorsCodes, ClaimErrorsSources } from '@core/models/billing/errors-alerts-sources-codes';
import { MemberViewSettings } from '@core/models/billing/member-view-settings';
import { ClaimsVoidModelWithUserInfo, VoidClaimsModel } from '@core/models/billing/void-claim-model';
import { ClaimsRebillModelWithUserInfo } from '@core/models/billing/rebill-claims-model';
import { ClaimNextFundersAndControlNumberModel, ClaimPatientFunderOptionModel } from '@core/models/billing/claim-patient-funder-option-model';
import {  ClaimEdiFilesModel, ClaimFilterOptionModel } from '@core/models/billing/claim-filter-option-model';
import { ClaimFilterGetModel } from '@core/models/billing/claim-filter-get-model';
import { RequestUserData } from '@core/utils/request-user-data';
import { ClaimPatientGetModel, RenderingProviderGetModel } from '@core/models/billing/claim-patient-get-model';
import { IdWithUserInfo, IdsWithUserInfo, UnflagImperson, SaveSelectedColumn, GetClaimByIdentifier } from '@core/models/billing/get-claim-by-identifier';
import { ServiceLineIdModel } from '@core/models/billing/billing-code';
import { environment } from 'src/environments/environment';
import { AccountMemberService } from '../account/account-member.service';
import { ClaimStatusUpdateModel, ClaimUpdateModel } from '@core/models/billing/claim-details-model';
import { ClaimsSubmitModel, ClearingHouseClaimModel } from '@core/models/billing/claims-submit-model';
import { ClaimsSecondaryBillingRebillModel } from '@core/models/billing/claims-secondary-billing-rebill-model';
import { ClaimResponseModel, ClaimValidationModel } from '@core/models/billing/claim-validation-model';
import { CarcCodes } from '@core/models/billing/carc-codes';
import { BehaviorSubject } from 'rxjs';
import { FlagClaimsRequest } from '@core/models/billing/claim';
import { AssigneeModel, AssigneeRequestModel } from '@core/models/billing/assignee-model';
import { EdiFileType } from '@core/enums/billing/edi-file-type';
import { ExternalCodes } from '@core/models/billing/external-codes';

@Injectable({
    providedIn: 'root'
})
export class ClaimService {
    private lookupIds = new Map<number, boolean>();

    public onLoad: Subject<Encounter>;
    private apiBaseUrl: string;
    private idsWithUserInfoReq: IdsWithUserInfo = { AccountInfoId: this.accountService.memberDetails.accountInfoId, MemberId: this.accountService.memberDetails.memberId, Ids: [] };

    constructor(private http: HttpService, private reqUserData: RequestUserData, private accountService: AccountMemberService) {
        this.onLoad = new Subject<Encounter>();
        this.apiBaseUrl = environment.claimApiBaseUrl;
    }

    private rejectedtabIdSubject = new BehaviorSubject<number | null>(null);

    private rejectedtabId$ = this.rejectedtabIdSubject.asObservable();

    private carcCodeSubject = new BehaviorSubject<CarcCodes[] | null>(null);
    private externalCodeSubject = new BehaviorSubject<ExternalCodes[] | null>(null);

    private carcCode$ = this.carcCodeSubject.asObservable();
    private externalCode$ = this.externalCodeSubject.asObservable();

    private tabIdSubject = new BehaviorSubject<number | null>(null);
    tabId$ = this.tabIdSubject.asObservable();

    private claimHeaderFilterSubject = new BehaviorSubject<ClaimHeaderSearch | null>(null);
    claimHeaderFilter$ = this.claimHeaderFilterSubject.asObservable();

    private oldFilterSubject = new BehaviorSubject<boolean>(false);
    public oldFilterValue$ = this.oldFilterSubject.asObservable();

    setClaimHeaderFilter(filter: ClaimHeaderSearch): void {
        this.claimHeaderFilterSubject.next(filter);
      }

    getClaimHeaderFilter(): Observable<ClaimHeaderSearch | null> {
        return this.claimHeaderFilter$;
      }

    setTabId(id: number): void {
      this.tabIdSubject.next(id);
    }

    getTabId(): Observable<number | null> {
      return this.tabId$;
    }

    setOldFilter(value: boolean): void {
      this.oldFilterSubject.next(value);
    }

    getOldFilter(): Observable<boolean> {
      return this.oldFilterValue$;
    }

    setId(id: number): void {
        this.rejectedtabIdSubject.next(id);
    }

    getId(): Observable<number | null> {

        return this.rejectedtabId$;
    }

    setCarcCode(carcCode: CarcCodes[]): void {
        this.carcCodeSubject.next(carcCode);
    }

    getCarcCode(): Observable<CarcCodes[] | null> {
        return this.carcCode$;
    }

    setExternalCode(externalCodes: ExternalCodes[]): void {
        this.externalCodeSubject.next(externalCodes);
    }   

    getExternalCode(): Observable<ExternalCodes[] | null> {
        return this.externalCode$;
    }

    Get(claimIdentifier: string): Observable<Encounter> {
        var model: GetClaimByIdentifier = {
            claimIdentifier: claimIdentifier,
            Id: 0,
            MemberId: this.accountService.memberDetails.memberId,
            AccountInfoId: this.accountService.memberDetails.accountInfoId
        }
        return this.http.post<ActionResponseResult<Encounter>>(this.apiBaseUrl + '/Claim/Get', model).pipe(map((response) => {
            if (response.success && response.data) {
                let result = this.mapEncounter(response.data, this.lookupIds);

                this.onLoad.next(result);

                return result;
            }

            return response.data;
        }));
    }

  saveClaim(model: ClaimSaveRequestModel) {
        return this.http.post<number>(this.apiBaseUrl + '/Claim/SaveClaim', model);
    }

    GetOptions(claimId: number): Observable<ClaimOptions> {
        var model: IdWithUserInfo = {
            Id: claimId,
            MemberId: this.accountService.memberDetails.memberId,
            AccountInfoId: this.accountService.memberDetails.accountInfoId
        }
        return this.http.post<ClaimOptions>(this.apiBaseUrl + '/Claim/GetOptions', model).pipe(
            share(),
            map((options) => {
                this.lookupIds = new Map<number, boolean>();

                if (options != null && options.claimIds != null) {
                    for (let i = 0; i < options.claimIds.length; i++) {
                        const claimId = options.claimIds[i];

                        if (!this.lookupIds.has(claimId)) {
                            this.lookupIds.set(claimId, true);
                        }

                    }
                }

                return options;
            }));
    }

    generateEdi(ediModel: any): Observable<string> {
      return this.http.post(
        this.apiBaseUrl + '/ClearingHouse/GenerateEDIData', ediModel, { responseType: 'text' as 'json' });
    }

    getEdiFilesFromBlob(ediModel: ClaimEdiFilesModel): Observable<string> {
      const requestModel = {
        accountInfoId: this.accountService.memberDetails.accountInfoId,
        memberId: this.accountService.memberDetails.memberId,
        fileType: ediModel.FileType || EdiFileType.EDI837,
        claimSubmissionId: ediModel.ClaimSubmissionId || 0,
        claimId: ediModel.ClaimId || 0,
        paymentId: ediModel.PaymentId || 0,
        blobFilePath: '',
      };
      return this.http.post(
        this.apiBaseUrl + '/EdiFile/GetEdiFilesFromBlob',requestModel,{ responseType: 'text' as 'json' }
      );
    }

    getClaimFunders(model: ClaimPatientGetModel) {
        return this.http.post<ClaimFilterOptionModel[]>(this.apiBaseUrl + '/Claim/GetClaimFunders', model, { showSpinner: false });
    }

    getClaimLocations(model: ClaimPatientGetModel) {
        return this.http.post<ClaimFilterOptionModel[]>(this.apiBaseUrl + '/Claim/GetStaffLocations', model, { showSpinner: false });
    }

    getClaimPatients(model: ClaimPatientGetModel) {
        return this.http.post<ClaimFilterOptionModel[]>(this.apiBaseUrl + '/Claim/GetClaimPatients', model, { showSpinner: false });
    }

    getClaimRenderingProviders(model: ClaimPatientGetModel) {
        return this.http.post<ClaimFilterOptionModel[]>(this.apiBaseUrl + '/Claim/GetClaimRenderingProviders', model, { showSpinner: false });
    }

    getRenderingProviders() {
        const AccountInfoId = this.accountService.memberDetails.accountInfoId;
        // Disable global loader by passing loadingSpinner = false
        return this.http.get<ClaimFilterOptionModel[]>(
            this.apiBaseUrl + `/Claim/GetRenderingProvidersForAccount/${AccountInfoId}`,
            { showSpinner: false },
            false
        );
    }

    updateClaimInfo(model: ClaimUpdateModel) {
        var reqmodel = {
            ...model,
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId,
            ImpersonationUserName: this.accountService.memberDetails.impersonationUserName
        }
        return this.http.post<ClaimDetailsInfoModel>(this.apiBaseUrl + '/Claim/UpdateClaimDetails', reqmodel,
            {}, true);
    }

    getClaimInfo(claimId: number) {
        var model: IdWithUserInfo = {
            Id: claimId,
            MemberId: this.accountService.memberDetails.memberId,
            AccountInfoId: this.accountService.memberDetails.accountInfoId
        }
        return this.http.post<ClaimDetailsInfoModel>(this.apiBaseUrl + '/Claim/GetClaimDetails', model, {}, true)
            .pipe(map((claim: ClaimDetailsInfoModel) => {
                return { ...claim, dateOfServiceStart: new Date(claim.dateOfServiceStart), dateOfServiceEnd: new Date(claim.dateOfServiceEnd) };
            }));
    }



    private mapEncounter(encounter: Encounter, lookupIds: Map<number, boolean>): Encounter {
        if (!lookupIds.has(encounter.id)) {
            lookupIds.set(encounter.id, true);
        }

        encounter.startDate = new Date(encounter.startDate);
        encounter.endDate = new Date(encounter.endDate);
        const lastModified = new Date(encounter.dateLastModified);
        lastModified.setHours(0, 0, 0, 0);
        encounter.dateLastModified = lastModified;
        encounter.totalPayments = 0;
        if (encounter.encounterChargeInfoItems && encounter.encounterChargeInfoItems.length) {
            encounter.totalPayments = encounter.encounterChargeInfoItems.map(i => i.totalPaid).reduce((prev, next) => prev + next);
        }

        if (encounter.encounterChargeInfoItems) {
            encounter.encounterChargeInfoItems = encounter.encounterChargeInfoItems.map(ci => {
                ci.dateOfService = moment(ci.dateOfService).format('l');

                return ci;
            });
        }

        return encounter;
    }

    getClaimHeaders(searchModel: any, showSpinner: boolean = true): Observable<any> {
        var model = {
            ...searchModel,
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId
        }
        return this.http.post<{ data: ClaimHeader[] }>(this.apiBaseUrl + '/Claim/GetClaimHeaders', model, { showSpinner });
    }

    getClaimFlagReasons(showLoading: boolean = false): Observable<string[]> {
     const accountInfoId = this.accountService.memberDetails.accountInfoId;
     return this.http.get<any[]>(`${this.apiBaseUrl}/Claim/GetClaimFlagReasons?accountInfoId=${accountInfoId}`,{ showSpinner: showLoading },false);
    }

    getBillingClaimDetails(params: ClaimDetailsListFilterSort) {
        return this.http.post<ClaimDetailsModel[]>(this.apiBaseUrl + '/Claim/GetBillingClaimDetails', params, {}, true);
    }

    updateBillingClaimDetails(model: ClaimUpdateDetailsModels) {
        return this.http.post<ClaimDetailsModel[]>(this.apiBaseUrl + '/Claim/UpdateBillingClaimDetails', model, {}, true);
    }

    removeBillingClaimDetail(detailId: number) {
        var model = {
            ChargeId: detailId,
            AccountId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId
        }
        return this.http.post<ClaimDetailsModel[]>(this.apiBaseUrl + '/Claim/RemoveBillingClaimDetails', model, {}, true);
    }

    getClaimHistory(claimId: number, state?: State) {
        // build request model; include Skip/Take only when a non-zero take is provided
        const model: any = {
            Id: claimId,
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId
        };

        if (state && typeof state.take === 'number' && state.take !== 0) {
            model.Skip = state.skip || 0;
            model.Take = state.take;
        }

        return this.http.post<ClaimHistory[]>(this.apiBaseUrl + '/Claim/GetClaimHistory', model, {}, true);
    }

    getClaimHistoryActions() {
        return this.http.post<ClaimHistoryActionModel[]>(this.apiBaseUrl + '/Claim/GetClaimHistoryActions', {});
    }

    getErrorsCodes() {
        return this.http.post<ClaimErrorsCodes>(this.apiBaseUrl + "/Claim/GetErrorsCodes", {});
    }

    getClaimErrorsAndAlerts(claimId: number) {
        var model: IdWithUserInfo = {
            Id: claimId,
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId
        }
        return this.http.post<ClaimErrorAlertModel[]>(this.apiBaseUrl + "/Claim/GetClaimErrorsAndAlerts", model);
    }


    ReRunValidation(model: ClaimValidationModel, isWithAddClaim: boolean = false) {
        return this.http.post<number>(this.apiBaseUrl + "/Claim/ValidateClaimData", model, {}, isWithAddClaim);
    }

    getErrorsSources() {
        return this.http.post<ClaimErrorsSources>(this.apiBaseUrl + "/Claim/GetErrorsSources", {});
    }

    saveSelectedColumns(columns: string[]) {
        var req: SaveSelectedColumn = { AccountInfoId: this.accountService.memberDetails.accountInfoId, MemberId: this.accountService.memberDetails.memberId, SelectedColumns: columns }
        return this.http.post<MemberViewSettings>(this.apiBaseUrl + '/Claim/SaveSelectedColumns', req);
    }

    approveClaims(claimsIds: number[]) {
        this.idsWithUserInfoReq.Ids = claimsIds;
        return this.http.post<ClaimResponseModel[]>(this.apiBaseUrl + '/Claim/SubmitClaimsForApproval', this.idsWithUserInfoReq);
    }

    unapproveClaims(claimsIds: number[]) {
        this.idsWithUserInfoReq.Ids = claimsIds;
        return this.http.post<number[]>(this.apiBaseUrl + '/Claim/UnapproveClaims', this.idsWithUserInfoReq);
    }

    flagClaims(request: UnflagImperson) {
        return this.http.post(this.apiBaseUrl + '/Claim/FlagClaims', request);
    }

    flagClaimsWithReason(request: FlagClaimsRequest) {
      return this.http.post(`${this.apiBaseUrl}/Claim/FlagClaimsWithReasons`,request);
    }


    unflagClaims(request: UnflagImperson) {
        return this.http.post(this.apiBaseUrl + '/Claim/UnflagClaims', request);
    }

    deleteClaims(model: number[]) {
        this.idsWithUserInfoReq.Ids = model;
        const impersonationUserName = this.accountService.memberDetails.impersonationUserName;
          const requestBody = {
             ...this.idsWithUserInfoReq,
            impersonationUserName: impersonationUserName
            };
        return this.http.post<string[]>(this.apiBaseUrl + '/Claim/DeleteClaims', requestBody);
    }

    markBilledClaims(claimsIds: number[]) {
        this.idsWithUserInfoReq.Ids = claimsIds;
        return this.http.post(this.apiBaseUrl + '/Claim/MarkBilledClaims', this.idsWithUserInfoReq);
    }

    submitClaims(submitModel: ClaimsSubmitModel) {
        return this.http.post<string[]>(this.apiBaseUrl + '/Claim/SubmitClaims', submitModel);
    }

    voidClaims(model: VoidClaimsModel) {
        var req: ClaimsVoidModelWithUserInfo = {
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId,
            ClaimsToVoid: model
        }
        return this.http.post<string[]>(this.apiBaseUrl + '/Claim/VoidClaims', req);
    }

    completeClaims(claimIds: number[]) {
        this.idsWithUserInfoReq.Ids = claimIds;
        return this.http.post<string[]>(this.apiBaseUrl + '/Claim/CompleteClaims', this.idsWithUserInfoReq);
    }

    rebillClaims(model: ClaimsRebillModelWithUserInfo) {
        return this.http.post<string[]>(this.apiBaseUrl + '/Claim/RebillClaims', model);
    }

    rebillSecondaryBillingClaims(rebillsecondarybillingModel: ClaimsSecondaryBillingRebillModel) {
        return this.http.post<string[]>(this.apiBaseUrl + '/Claim/SecondaryBillingRebillClaims', rebillsecondarybillingModel);
    }


    getClaimBillNextFunders(claimId: number) {
        var model: IdWithUserInfo = {
            Id: claimId,
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId
        }
        return this.http.post<ClaimNextFundersAndControlNumberModel>(this.apiBaseUrl + "/Claim/GetClaimBillNextFunders", model);
    }

    getMemberViewSettings() {
        var model = {
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId
        }
        return this.http.post<MemberViewSettings>(this.apiBaseUrl + '/Claim/GetMemberViewSettings', model);
    }

    getClaimFilterItems(entityName: string, model: ClaimFilterGetModel) {
        return this.http.post<ClaimFilterOptionModel[]>(this.apiBaseUrl + `/Claim/GetClaim${entityName}`, model);
    }

    updateChargeModifiers(reqmodel: ClaimUpdateModifiersModel) {
        var model = {
            ...reqmodel,
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId,
        }
        return this.http.post<ClaimDetailsInfoModel>(this.apiBaseUrl + '/Claim/UpdateChargeModifiers', model,
            {}, true);
    }

    getClaimLineAppointments(model: ServiceLineIdModel) {
        return this.http.post<Appointment[]>(this.apiBaseUrl + '/Claim/GetClaimLineAppointments', model,
            {}, true);
    }

    getClaimTabStatuses(model: ClaimPatientGetModel) {
        return this.http.post<ClaimFilterOptionModel[]>(this.apiBaseUrl + '/Claim/GetClaimTabStatuses', model, { showSpinner: false });
    }

    // **************Payment List Services**********************************************************************************************************    

    getAccountClaims(searchData: PaymentClaimsSearchBase, showLoader = true) {
        return this.http.post<PaymentClaimsSearch[]>(this.apiBaseUrl + '/Claim/GetAccountClaims', searchData,
            { showSpinner: showLoader });
    }

    getHFCAClaimInfo(ids: number[]) {
        this.idsWithUserInfoReq.Ids = ids;
        return this.http.post<ClaimDetailsInfoModel[]>(this.apiBaseUrl + '/Claim/GetHFCAClaimDetails', this.idsWithUserInfoReq);
    }

    //*******************Get All Carc Codes***************************/
    getCarcCodes(): Observable<CarcCodes[]> {
        return this.http.get<CarcCodes[]>(this.apiBaseUrl + '/Claim/GetAllCarcCodes').pipe(
            map((response) => {
                if (response && response.length > 0) {
                    return response;
                }
                return [];
            })
        );
    }

    //*******************Get Grid Page Sizes***************************/
    // getGridPageSizes(): Observable<any> {
    //      return this.http.get<any>(this.apiBaseUrl + '/Claim/GetGridPageSizes').pipe(
    //         map((response) => {
    //             if (Array.isArray(response) && response.length > 0) {
    //                 localStorage.setItem('gridPageSizes', JSON.stringify(response));
    //                 return response;
    //             }
    //             return [20, 50, 100, { 'text': 'All', 'value': 0 }];
    //         })
    //     );
    // }

    getGridPageSizes(): Observable<any> {
        return this.http.get<any>(this.apiBaseUrl + '/Claim/GetGridPageSizes').pipe(
            map((response) => {

                if (response && Array.isArray(response.PageSizes) && response.PageSizes.length > 0) {
                    localStorage.setItem('gridPageSizes', JSON.stringify(response.PageSizes));
                } else {
                    localStorage.setItem('gridPageSizes', JSON.stringify([20, 50, 100, { 'text': 'All', 'value': 0 }]));
                }

                if (response && response.DefaultPageSize != null) {
                    localStorage.setItem('defaultPageSize', JSON.stringify(response.DefaultPageSize));
                } else {
                    localStorage.setItem('defaultPageSize', JSON.stringify(20));
                }

                return response;
            })
        );
    }

    updateClaimStatus(reqmodel: ClaimStatusUpdateModel) {
        var model = {
            ...reqmodel,
            AccountInfoId: this.accountService.memberDetails.accountInfoId,
            MemberId: this.accountService.memberDetails.memberId,
        }
        return this.http.post(this.apiBaseUrl + '/Claim/UpdateClaimStatus', model, {}, true);
    }

    SubmitClaimToServiceBus(submitModel: ClaimsSubmitModel) {
        return this.http.post(this.apiBaseUrl + '/Claim/SubmitClaimToServiceBus', submitModel);
    }

    getAssignee(model: ClaimPatientGetModel) {
        return this.http.post<AssigneeModel[]>(this.apiBaseUrl + '/Appointment/GetClaimsAssignee', model,
            { showSpinner: false }
        );
    }

    assignUserToClaim(model: AssigneeRequestModel){
        return this.http.post<boolean>(this.apiBaseUrl + '/Claim/Assign', model ,{} ,true);
    }

    /**
     * Get billing provider data when "Other" was selected
     * GET /Claim/GetBillingProviderDetails?claimId={claimId}
     */
    getBillingProviderOther(claimId: number): Observable<ClaimBillingProviderDto | null> {
        return this.http.get<ClaimBillingProviderDto>(
            `${this.apiBaseUrl}/Claim/GetBillingProviderDetails?claimId=${claimId}`,
            { showSpinner: false }
        );
    }

    /**
     * Get state information for dropdowns
     * GET /Claim/GetStateInformation
     */
    getStateInformation(): Observable<StateInformation[]> {
        return this.http.get<StateInformation[]>(
            `${this.apiBaseUrl}/Claim/GetStateInformation`,
            { showSpinner: false }
        );
    }

    //*******************Get All External Codes***************************/
    getExternalCodes(): Observable<ExternalCodes[]> {
        return this.http.get<ExternalCodes[]>(this.apiBaseUrl + '/Claim/GetAllExternalCodes').pipe(
            map((response) => {
                if (response && response.length > 0) {
                    return response;
                }
                return [];
            })
        );
    }
}
