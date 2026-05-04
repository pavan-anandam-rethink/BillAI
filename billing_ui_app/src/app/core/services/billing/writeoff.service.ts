import { Injectable } from '@angular/core';
import { IdsWithUserInfo, IdWithUserInfo } from '@core/models/billing/get-claim-by-identifier';
import { environment } from 'src/environments/environment';
import { HttpService } from '../http.service';
import { AccountMemberService } from '../account/account-member.service';
import { EditWriteOffModelWithUserInfo, GetChargeEntryWriteOffModel, WriteOffChargeEntryModel, WriteOffReasonCodDescriptionModel } from '@core/models/billing/write-off-charge-entry-model';
import { Observable, of } from 'rxjs';
import { tap, delay } from 'rxjs/operators';
import { AddWriteOffResponseModel, WriteOffClaimModelWithUserInfo } from '@core/models/billing/write-off-claim-model';

@Injectable()
export class WriteoffService 
{
  private apiBaseUrl = environment.claimApiBaseUrl;
  private idWithUserInfoReq : GetChargeEntryWriteOffModel = {AccountInfoId: this.accountService.memberDetails.accountInfoId, MemberId: this.accountService.memberDetails.memberId, id: undefined,isServiceLineId: undefined};
  private reasonCodesCache: { value: WriteOffReasonCodDescriptionModel[]; timestamp: number } | null = null;
  private readonly CACHE_TTL_MS = environment.reasonCodesCacheTTL;
    
  constructor(private http: HttpService, private accountService: AccountMemberService) {}

    writeOffClaim(model: WriteOffClaimModelWithUserInfo) {
      model.AccountInfoId = this.accountService.memberDetails.accountInfoId;
       model.MemberId = this.accountService.memberDetails.memberId;
      return this.http.post<AddWriteOffResponseModel>(this.apiBaseUrl + '/writeoff/AddWriteOff', model);
    }

    getChargeEntryWriteOffsByChargeId(Id: number,isServiceLineId: boolean): Observable<WriteOffChargeEntryModel[]> {
      this.idWithUserInfoReq.id = Id;
      this.idWithUserInfoReq.isServiceLineId = isServiceLineId;  
      return this.http.post<WriteOffChargeEntryModel[]>(this.apiBaseUrl + '/writeoff/GetChargeEntryWriteOffsByChargeId',this.idWithUserInfoReq);
    }

    getReasonCodesWithDescriptions(): Observable<WriteOffReasonCodDescriptionModel[]> {
      // Check if cache exists and is still valid
      if (this.reasonCodesCache !== null) {
        const cacheAge = Date.now() - this.reasonCodesCache.timestamp;
        if (cacheAge < this.CACHE_TTL_MS) {
          // Cache is still valid (less than 1 hour old)
          // Use delay(0) to make cached observable async like HTTP call
          return of(this.reasonCodesCache.value).pipe(delay(0));
        }
        // Cache expired, clear it
        this.reasonCodesCache = null;
      }
      
      return this.http.get<WriteOffReasonCodDescriptionModel[]>(this.apiBaseUrl + '/writeoff/GetReasonCodes')
        .pipe(
          tap((codes) => {
            // Cache the reason codes with timestamp
            this.reasonCodesCache = { value: codes, timestamp: Date.now() };
          })
        );
    }

  updateChargeEntryWriteOff(model: EditWriteOffModelWithUserInfo): Observable<WriteOffChargeEntryModel> {

    model.AccountInfoId =  this.accountService.memberDetails.accountInfoId;
    model.MemberId = this.accountService.memberDetails.memberId;
    return this.http.post<WriteOffChargeEntryModel>(this.apiBaseUrl + '/writeoff/UpdateChargeEntryWriteOffsByChargeId', model);
  }

  deleteChargeEntryWriteOff(ids: number[]) {
      var model : IdsWithUserInfo = {
        Ids: ids,
        AccountInfoId: this.accountService.memberDetails.accountInfoId,
        MemberId: this.accountService.memberDetails.memberId
      };
      return this.http.post(this.apiBaseUrl + '/writeoff/DeleteChargeEntryWriteOffsByCharge', model);
  }
}
