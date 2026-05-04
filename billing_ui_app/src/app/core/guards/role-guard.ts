import { AccountMemberService } from '@core/services/account/account-member.service';
import { AuthService } from '@core/services/sso/auth.service';
import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, CanActivateChild, Router } from '@angular/router';

@Injectable({
  providedIn: 'root'
})
export class roleGuard implements CanActivate, CanActivateChild {

  constructor(private authSvc: AuthService,
    private accountSvc: AccountMemberService,
    private router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot): boolean {
    return this.checkPermission(route.data.permissions);
  }

  canActivateChild(route: ActivatedRouteSnapshot): boolean {
    return this.checkPermission(route.data.permissions);
  }

  private checkPermission(permissions: string[]): boolean {
    if (this.authSvc.isAuthenticated()) {
      if (!this.accountSvc.checkPermissionAllLevels(permissions)) {
        //unauthorized
        this.router.navigate(['./billing/unauthorized']); 
        return false;
      }
    }
    return true;
  };
}