import { environment } from 'src/environments/environment';
import { Injectable } from '@angular/core';
import { CanActivate, CanActivateChild } from '@angular/router';
import { AuthService } from '@core/services/sso/auth.service';

@Injectable({
  providedIn: 'root'
})
export class authGuard implements CanActivate, CanActivateChild  {
  constructor(private authSvc: AuthService) {}

  canActivate(): boolean {
    return this.checkAuth();
  }

  canActivateChild(): boolean {
    return this.checkAuth();
  }

  private checkAuth(): boolean {
    if (!this.authSvc.isAuthenticated()) {
      // Redirect to the login page if the user is not authenticated
      this.authSvc.logout();
      return false;
    }
    return true;
  }
}