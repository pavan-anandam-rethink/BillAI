import { DestroyRef, Injectable } from '@angular/core';
import { HttpContext } from "@angular/common/http";
import { Router } from "@angular/router";
import { catchError, Observable, of, tap } from "rxjs";
import { JwtHelperService } from "@auth0/angular-jwt";
import { environment } from 'src/environments/environment';
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { AccountMemberSettings } from '../account/account-member.service';
import { IS_PUBLIC } from '../auth.interceptor';
import { AccountPermissions } from '@core/enums/account';
import { HttpService, IRequestOptions } from '../http.service';


@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // Token will be refreshed 1 min before expiration time
  private readonly TOKEN_EXPIRY_THRESHOLD_MINUTES = 1;
  private readonly CONTEXT = {context: new HttpContext().set(IS_PUBLIC, true)};
  private apiAuthUrl = environment.authApiBaseUrl;

  getJwtToken() {
    return localStorage.getItem('token');
  }

  getUserData(): AccountMemberSettings {
    let decodedToken = this.jwtHelper.decodeToken(this.getJwtToken())
    return {
      accountInfoId: parseInt(decodedToken.AccountInfoId),
      memberId: parseInt(decodedToken.MemberId),
      memberName: decodedToken.MemberName,
      memberRole: decodedToken.MemberRole,
      accountDetail: decodedToken.AccountDetail,
      impersonationUserName: decodedToken.ImpersonationUserName,
      permissions: decodedToken.Permissions,
      impersonatedUser: decodedToken.ImpersonatedUser
    }
  }

  constructor(
    private http: HttpService,
    private router: Router,
    private jwtHelper: JwtHelperService,
    private destroyRef: DestroyRef
  ) {
  }

  isAuthenticated(): boolean {
    return !this.jwtHelper.isTokenExpired();
  }

  refreshToken(): Observable<AuthenticatedResponse | null> {
    const billing_token = this.getJwtToken();
    if (!billing_token) {
      return of();
    }
    let billingToken: Token = {
      token: billing_token
    }
    return this.http.post<AuthenticatedResponse>(
      this.apiAuthUrl + '/SSO/Refresh',
       billingToken,
       {
        context: new HttpContext().set(IS_PUBLIC, true),
        showSpinner: false
       } as IRequestOptions
    )
      .pipe(
        catchError(() => of()),
        tap(data => {
          const loginSuccessData = data as AuthenticatedResponse;
          this.storeTokens(loginSuccessData);
          this.scheduleTokenRefresh(loginSuccessData.token);
        })
      );
  }

  login(rethink_Token: any): Observable<AuthenticatedResponse | null> {
    if (!rethink_Token) {
      return of();
    }
    let rethinkToken: Token = {
      token: rethink_Token
    }
    return this.http.post<AuthenticatedResponse>(this.apiAuthUrl + '/SSO/SSOLogin', rethinkToken, this.CONTEXT)
      .pipe(
        catchError(error => {
          if (error.status === 401) {
            // Handle invalid credentials
            // this.logout();
            window.location.href = environment.rethinkBHUrl;
          }
          return of();
        }),
        tap(data => {
          const loginSuccessData = data as AuthenticatedResponse;
          // const loginSuccessData = {
          //   token: environment.token,
          //   refreshToken: 'sDjdWYn8/+cWt59Pa2QDzQ71879PQg18pyoQU0dxmHU='
          // }
          this.storeTokens(loginSuccessData);
          this.scheduleTokenRefresh(loginSuccessData.token);
          this.router.navigate([this.FindDefaultDashboard()]);
        })
      );
  }

  FindDefaultDashboard() {
    let path: string = '/billing/';
    let userPermissions: string[] = this.getUserData().permissions;
    //Based on available permissions, order of default dashboard:
    //Claims, Payments, PI, Reporting
    if (userPermissions.indexOf(AccountPermissions.BillingView) != -1)
      path = path.concat('claims');
    else if (userPermissions.indexOf(AccountPermissions.BillingPostPayments) != -1)
      path = path.concat('paymentposting');
    else if (userPermissions.indexOf(AccountPermissions.BillingReopenEncounter) != -1)
      path = path.concat('patientinvoicing');
    else if (userPermissions.indexOf(AccountPermissions.BillingCloseEncounters) != -1)
      path = path.concat('reporting');
    else if (userPermissions.indexOf(AccountPermissions.BillingClientHistory) != -1)
      path = path.concat('clienthistory');
    return path;
  }

  scheduleTokenRefresh(token: string): void {
    const expirationTime = this.jwtHelper.getTokenExpirationDate(token)?.getTime();
    const refreshTime = expirationTime ? expirationTime - this.TOKEN_EXPIRY_THRESHOLD_MINUTES * 60 * 1000 : Date.now();
    const refreshInterval = refreshTime - Date.now();
    if (refreshInterval > 0) {
      setTimeout(() => {
        this.refreshToken()
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe();
      }, refreshInterval);
    }
  }

  logout(): void {
    // Not required since we are not associating a user with JWT token in DB
    // const refreshtoken = localStorage.getItem('refreshtoken');
    // this.http.post<LoginResponse>(`${environment.authApiBaseUrl}/SSO/invalidate`, {refreshtoken}, this.CONTEXT)
    //   .pipe(takeUntilDestroyed(this.destroyRef))
    //   .subscribe(() => {
    //     this.removeTokens();
    //     window.location.href = environment.rethinkBHUrl;
    //   });
    if (this.getJwtToken() != null){
      this.removeTokens();
      window.location.href = environment.rethinkBHUrl;
    }
  }

  removeTokens(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshtoken');
  }

  storeTokens(data: AuthenticatedResponse): void {
    localStorage.setItem('token', data.token);
    localStorage.setItem('refreshtoken', data.refreshToken);
  }
}

export interface AuthenticatedResponse {
    token: string;
    refreshToken: string;
}

export interface Token {
    token: string;
}
