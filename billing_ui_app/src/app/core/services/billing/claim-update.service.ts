import { Injectable } from '@angular/core';
import { IdsWithUserInfo } from '@core/models/billing/get-claim-by-identifier';
import { HttpService } from '@core/services';
import { environment } from 'src/environments/environment';
import { AccountMemberService } from '../account/account-member.service';
import { ClaimUpdateResult } from '@core/models/billing/claim';

@Injectable({
  providedIn: 'root'
})
export class ClaimUpdateService {
  apiBaseUrl: any;
  private idsWithUserInfoReq: IdsWithUserInfo = {AccountInfoId: this.accountService.memberDetails.accountInfoId, MemberId: this.accountService.memberDetails.memberId, Ids: []};

  constructor(private http: HttpService, private accountService: AccountMemberService) 
  {
    this.apiBaseUrl = environment.claimApiBaseUrl;
  }

  getSecondaryFunderDetails(claimId: number) {
      var model = {
        id: claimId,
        accountInfoId: this.idsWithUserInfoReq.AccountInfoId,
        memberId: this.idsWithUserInfoReq.MemberId
      }
        return this.http.post<ClaimUpdateResult>(this.apiBaseUrl + '/ClaimUpdate/UpdateClaimIfSecondaryFunderPresent', model);
    }
}
