import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

import { HttpService } from "@core/services/http.service";
import {
    ClaimPosting,
    ListFilterSort,
    ClaimDetailsListFilterSort,
    ClaimPostingDetails,
    RemovePaymentClaims,
    ClientPrintData,
    PatientDetails,
    CreatePaymentPatientClaims,
    ClaimEOBInfo,
    CompanyPrintData,
    CreatePaymentEraClaims,
    PatientClaimDetailsFilterSort,
    PostRemovePatientClaims, PaymentClaimServiceLineSmall, ClaimListFilterSort
} from '@core/models/billing';
import { PaymentClaimServiceLine } from "@core/models/billing/payment-claim-service-line";
import { PaymentClaimError } from "@core/models/billing/payment-claim-error";
import { IdFilterSort } from "@core/models/billing/id-filter-sort";
import {HttpResponse} from "@angular/common/http";

import { environment } from 'src/environments/environment';
import { AccountMemberService } from '../account/account-member.service';
import { PaymentClaims, PaymentPatientModel, PaymentPostingPrintModel } from '@core/models/billing/cliam-posting';
import { GetChargeDetails } from '@core/models/billing/get-charge-details';
import { AddPatientResponseClaims } from '@core/models/billing/create-payment-patient-claims';
import { InsuranceClaimListFilterSort } from '@core/models/billing/claim-posting-filter-sort';

@Injectable()
export class ClaimPostingService {
    private apiBaseUrl = environment.claimApiBaseUrl;

    public onLoad: Subject<ClaimPosting>;

    constructor(private http: HttpService, private accountService: AccountMemberService) {
        this.onLoad = new Subject<ClaimPosting>();
    }

    getErrorClaims(gridState: IdFilterSort){
        return this.http.post<PaymentClaimError[]>(this.apiBaseUrl + '/ClaimPosting/GetPaymentClaimErrors', gridState)
    }
    
    getAll(params: InsuranceClaimListFilterSort, showSpinner: boolean = true) {
        const model = {
            ...params,
            memberId: this.accountService.memberDetails.memberId,
            accountInfoId: this.accountService.memberDetails.accountInfoId
        }
        return this.http.post<{ data: PaymentClaims[], totalCount: number }>(
            this.apiBaseUrl + '/ClaimPosting/GetClaims',
            model,
            { showSpinner }
        );
    }

    getPaymentPatients(paymentId: number) {
        return this.http.get<PaymentPatientModel[]>(this.apiBaseUrl + '/ClaimPosting/GetPaymentPatients', { params: { paymentId: paymentId}, showSpinner: false }, false);
    }

    getAllForPatients(params: ListFilterSort) {
        params.accountInfoId = this.accountService.memberDetails.accountInfoId;
        return this.http.post<{ data: ClaimPosting[], totalCount: number }>(this.apiBaseUrl + '/ClaimPosting/GetPatientClaims', params);
    }
    
    getEOBPdf(paymentId: number, claims: number[], currentUserDateTime: Date, showErrors: boolean){
        return this.http.post<HttpResponse<Object>>(this.apiBaseUrl + '/ClaimPosting/GetEOBPaymentClaimsPDF',
            {paymentId, claims, currentUserDateTime, showErrors}, {
                showSpinner: true,
                responseType: 'arraybuffer' as 'json',
                observe: 'response' as 'body'
            }, false);
    }

    getEOBClaims(paymentId: number) {
        return this.http.post<ClaimEOBInfo[]>(this.apiBaseUrl + '/ClaimPosting/GetEOBClaims', paymentId);
    }

    getSelectedEOBClaims(paymentId: number, claims: number[]) {
        return this.http.post<ClaimEOBInfo[]>(this.apiBaseUrl + '/ClaimPosting/GetSelectedEOBClaims',
            {paymentId, claims});
    }

    deleteById(claimId: number) {
        return this.http.post<ClaimPosting>(this.apiBaseUrl + '/ClaimPosting/RemovePaymentClaims', claimId);
    }
    
    deleteByIds(reqModel: RemovePaymentClaims) {
        var model = {
            ...reqModel,
            memberId: this.accountService.memberDetails.memberId,
            accountInfoId: this.accountService.memberDetails.accountInfoId
        }
        return this.http.post<ClaimPosting>(this.apiBaseUrl + '/ClaimPosting/RemovePaymentClaims', model);
    }
    
    deleteSelectedClaims(model: RemovePaymentClaims) {
        return this.http.post<ClaimPosting>(this.apiBaseUrl + '/ClaimPosting/RemovePaymentClaims', model);
    }

    deleteSelectedPatients(model: PostRemovePatientClaims) {
        return this.http.post<ClaimPosting>(this.apiBaseUrl + '/ClaimPosting/RemovePatientPaymentClaims', model);
    }

    deleteSelectedPaymentAmounts(model: PostRemovePatientClaims) {
        return this.http.post<ClaimPosting>(this.apiBaseUrl + '/ClaimPosting/RemoveSelectedPatientPaymentAmounts', model);
    }

    getById(claimId: number) {
        return this.http.post<ClaimPosting>(this.apiBaseUrl + '/ClaimPosting/GetClaims', claimId);
    }

    getPaymentClaimServiceLines(params: ClaimDetailsListFilterSort) {
        return this.http.post<{ data: ClaimPostingDetails[], totalCount: number }>(this.apiBaseUrl + '/ClaimPosting/getPaymentClaimServiceLines', params.claimId);
    }
    
    getPaymentClaimServiceLinesSmall(model: GetChargeDetails){
        return this.http.post<PaymentClaimServiceLineSmall[]>(this.apiBaseUrl + '/ClaimPosting/getPaymentClaimServiceLinesSmall',
            model);
    }

    getPatientPaymentClaimLinkedServiceLines(params: PatientClaimDetailsFilterSort) {
        return this.http.post<{ data: ClaimPostingDetails[], totalCount: number }>
            (this.apiBaseUrl + '/ClaimPosting/GetPatientPaymentClaimLinkedServiceLines', params);
    }

    getPatientPaymentClaimUnlinkedServiceLines(params: PatientClaimDetailsFilterSort) {
        return this.http.post<{ data: ClaimPostingDetails[], totalCount: number }>
            (this.apiBaseUrl + '/ClaimPosting/GetPatientPaymentClaimUnlinkedServiceLines', params);
    }

    getPaymentClaimServiceLine(serviceLineId: number) {
        return this.http.post<PaymentClaimServiceLine>(this.apiBaseUrl + '/ClaimPosting/getPaymentClaimServiceLine', serviceLineId);
    }

    updatePaymentClaimServiceLineAmounts(serviceLineId: number, allowedAmount: number, paymentAmount: number,
                                         isManual: boolean) {
        return this.http.post(this.apiBaseUrl + '/ClaimPosting/UpdatePaymentClaimServiceLineAmounts', {
            serviceLineId,
            allowedAmount,
            paymentAmount,
            isManual,
            "memberId": this.accountService.memberDetails.memberId,
            "accountInfoId": this.accountService.memberDetails.accountInfoId
        });
    }

    rebill(ids: number[]) {
        return this.http.post<{ processed: number[], rejected: number[] }>(this.apiBaseUrl + '/ClaimPosting/rebillClaims', ids);
    }


    GetClientPrintDataById(model: PaymentPostingPrintModel) {
        return this.http.post<ClientPrintData>(this.apiBaseUrl + '/ClaimPosting/GetClientPrintDataById', model);
    }

    GetCompanyPrintDataById(accountInfoId: number) {
        return this.http.post<CompanyPrintData>(this.apiBaseUrl + '/ClaimPosting/GetCompanyPrintDataById', accountInfoId);
    }

    getPatientDetails(patientId: number) {
        var model = {
            "patientId": patientId,
            "memberId": this.accountService.memberDetails.memberId,
            "accountInfoId": this.accountService.memberDetails.accountInfoId
        }
        return this.http.post<PatientDetails>(this.apiBaseUrl + '/claimPosting/GetPatientDetails', model);
    }

    getClaimPostingDetails(claimId: number) {
        var model = {
            "id": claimId,
            "memberId": this.accountService.memberDetails.memberId,
            "accountInfoId": this.accountService.memberDetails.accountInfoId
        }
        return this.http.post<ClaimPostingDetails>(this.apiBaseUrl + '/claimPosting/GetClaimDetails', model);
    }

    createPaymentPatientClaims(model: CreatePaymentPatientClaims) {
        return this.http.post<AddPatientResponseClaims[]>(this.apiBaseUrl + '/ClaimPosting/CreatePaymentPatientClaims', model);
    }
    
    createPaymentEraClaims(reqModel: CreatePaymentEraClaims) {
        var model = {
            ...reqModel,
            memberId: this.accountService.memberDetails.memberId,
            accountInfoId: this.accountService.memberDetails.accountInfoId
        }
        return this.http.post<number>(this.apiBaseUrl + '/ClaimPosting/CreateClaimsToEraPayment', model, {}, true);
    }

    postManualPayment(model: any) {
        return this.http.post<any>(this.apiBaseUrl + '/ClaimPosting/PostManualPaymentClaimLines', model);
    }

    postManualPatientPayment(model: PostRemovePatientClaims) {
        return this.http.post<any>(this.apiBaseUrl + '/ClaimPosting/PostManualPatientPaymentClaimLines', model);
    }
}
