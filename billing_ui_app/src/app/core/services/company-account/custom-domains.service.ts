import { Injectable } from '@angular/core';
import { filter, map, shareReplay, take } from 'rxjs/operators';

import { HttpService } from '..';
import { AccountPermissions } from '@core/enums/account';
import { AccountMemberService } from '@core/services/account/account-member.service'

@Injectable({
  providedIn: 'root',
})
export class CustomDomainsService {
  constructor(
    private http: HttpService,
    private accountService: AccountMemberService,
  ) { }

  canEdit$ = this.accountService
    .accountMemberSettings
    .pipe(
      filter(x => !!x),
      map(() => {
        return this.accountService.checkPermissionLevel(AccountPermissions.ProviderAccountEdit);
      }),
      shareReplay(1),
    );

  getAll() {
    const path = `/core/api/Provider/Provider/GetAllDomains`;
    return this.http.get<CustomDomain[]>(path, {
      params: {
        isCustom: true,
      },
    });
  }


  addDomain(name: string) {
    const path = `/core/api/Provider/Provider/AddCustomDomain`;
    return this.http.post<any>(path, {
      name,
      id: 0
    });
  }

  updateDomain(model: CustomDomain) {
    const path = `/core/api/Provider/Provider/UpdateCustomDomain`;
    return this.http.post<any>(path, model);
  }

  removeDomain(id: number) {
    const path = `/core/api/Provider/Provider/DeleteCustomDomain`;
    return this.http.post<any>(path, { id });
  }

}


export interface CustomDomain {
  id: number;
  name: string;
  pos?: any;
  parentCategoryId?: any;
  lessonCategoryType: number;
  categoryIcon?: any;
  dateCreated: string;
  dateLastModified: string;
  dateDeleted?: any;
  previewActivity?: any;
  isCustom: boolean;
  accountId: number;
  categoryType: number;
  lessonCategories?: any;
  lessons?: any;
}
