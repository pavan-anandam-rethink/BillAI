import { Injectable } from "@angular/core";
import { HttpService } from "@core/services";
import { Observable, of } from "rxjs";
import { tap, delay } from "rxjs/operators";
import { Adjustment } from "@core/models/billing/adjustment";
import { IdsWithUserInfo } from "@core/models/billing/get-claim-by-identifier";
import { environment } from "src/environments/environment";
import { AccountMemberService } from "../account/account-member.service";
import { GetChargeDetails } from "@core/models/billing/get-charge-details";
import { AddOrEditAdjustmentModelWithUserInfo } from "@core/models/billing/add-or-edit-adjustment-model";
import { HttpHeaders } from "@angular/common/http";
import { AdjustmentReasonCodes } from "@core/models/billing/adjustmentReasonCodes";

@Injectable()
export class AdjustmentService {
  private apiBaseUrl = environment.claimApiBaseUrl;
  private reasonCodesCache: { value: AdjustmentReasonCodes[]; timestamp: number } | null = null;
  private readonly CACHE_TTL_MS = environment.reasonCodesCacheTTL;
  
  constructor(private http: HttpService, private accountService: AccountMemberService) { }

  getServiceLineAdjustments(serviceLineId: number): Observable<Adjustment[]> {
    return this.http.post<Adjustment[]>(this.apiBaseUrl + '/ServiceLineAdjustment/GetServiceLineAdjustments', serviceLineId);
  }

  getServiceLineAdjustmentsByChargeId(model: GetChargeDetails): Observable<Adjustment[]> {
    return this.http.post<Adjustment[]>(this.apiBaseUrl + '/ServiceLineAdjustment/GetServiceLineAdjustmentsByCharge',
      model);
  }

  addPaymentServiceLineAdjustments(model: AddOrEditAdjustmentModelWithUserInfo): Observable<Adjustment> {

    model.AccountInfoId = this.accountService.memberDetails.accountInfoId;
    model.MemberId = this.accountService.memberDetails.memberId;
    return this.http.post<Adjustment>(this.apiBaseUrl + '/ServiceLineAdjustment/AddPaymentServiceLineAdjustments', model);
  }

  addPaymentServiceLineAdjustmentsBulk(model: AddOrEditAdjustmentModelWithUserInfo): Observable<Adjustment> {

    model.AccountInfoId = this.accountService.memberDetails.accountInfoId;
    model.MemberId = this.accountService.memberDetails.memberId;
    return this.http.post<Adjustment>(this.apiBaseUrl + '/ServiceLineAdjustment/AddPaymentServiceLineAdjustments', model);
  }



  updateServiceLineAdjustments(editAdjustment: AddOrEditAdjustmentModelWithUserInfo): Observable<Adjustment> {
    editAdjustment.AccountInfoId = this.accountService.memberDetails.accountInfoId;
    editAdjustment.MemberId = this.accountService.memberDetails.memberId;
    return this.http.post<Adjustment>(this.apiBaseUrl + '/ServiceLineAdjustment/UpdateServiceLineAdjustments', editAdjustment);
  }

  deleteServiceLineAdjustments(adjustmentIds: number[]) {
    var idsWithUserInfoReq: IdsWithUserInfo = { AccountInfoId: this.accountService.memberDetails.accountInfoId, MemberId: this.accountService.memberDetails.memberId, Ids: adjustmentIds };
    return this.http.post(this.apiBaseUrl + '/ServiceLineAdjustment/DeleteServiceLineAdjustments', idsWithUserInfoReq);
  }

  getAdjustmentReasonDescriptions(newVal: any): Observable<AdjustmentReasonCodes[]> {
    // If searching with empty string and cache exists, check if still valid
    if (newVal === '' && this.reasonCodesCache !== null) {
      const cacheAge = Date.now() - this.reasonCodesCache.timestamp;
      if (cacheAge < this.CACHE_TTL_MS) {
        // Cache is still valid (less than 1 hour old)
        // Use delay(0) to make cached observable async like HTTP call
        return of(this.reasonCodesCache.value).pipe(delay(0));
      }
      // Cache expired, clear it
      this.reasonCodesCache = null;
    }
    
    const headers = new HttpHeaders({
      'Content-Type': 'application/json'
    });

    return this.http.post<AdjustmentReasonCodes[]>(this.apiBaseUrl + '/ServiceLineAdjustment/GetAdjustmentReasonDescriptions',
      JSON.stringify(newVal), { headers })
      .pipe(
        tap((codes) => {
          // Cache the full list with timestamp when loading with empty search
          if (newVal === '') {
            this.reasonCodesCache = { value: codes, timestamp: Date.now() };
          }
        })
      );
  }
}
