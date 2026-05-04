import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { HttpService } from '../http.service';
import { ProviderRoles, ProviderRole, RolePermissions, RolePermissionsSaveModel } from '@core/models/company-account';


@Injectable({
  providedIn: 'root'
})
export class ProviderRolesService {
  constructor(private http: HttpService) { }

  getProviderRoles(): Observable<ProviderRoles> {
    const path = `/core/api/Provider/Provider/GetProviderRoles`;
    return this.http.post<ProviderRoles>(path, {});
  }

  saveProviderRole(data: ProviderRole): Observable<ProviderRole> {
    const path = `/core/api/Provider/Provider/SaveProviderRoleAsync`;
    return this.http.post<ProviderRole>(path, { ...data });
  }

  getRolePermissions(roleId: number): Observable<RolePermissions> {
    const path = `/core/api/Provider/Provider/GetRolePermissions`;
    return this.http.post<RolePermissions>(path, roleId);
  }

  saveRolePermissions(data: RolePermissionsSaveModel): Observable<ProviderRole> {
    const path = `/core/api/Provider/Provider/SaveRolePermissions`;
    return this.http.post<ProviderRole>(path, { ...data });
  }
}