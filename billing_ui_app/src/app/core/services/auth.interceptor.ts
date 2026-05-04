import {HttpContextToken, HttpInterceptorFn, HttpRequest} from '@angular/common/http';
import {inject} from "@angular/core";
import {switchMap} from "rxjs/operators";
import { AuthService } from './sso/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authSvc = inject(AuthService);
  // whenever this HttpContextToken is attached to a request 
  // as we did to login request earlier
  // it means the user does not need to be authenticated 
  // so we don't attach authorization header
  if (req.context.get(IS_PUBLIC)) {
    return next(req);
  }
  if (authSvc.isAuthenticated()) {
    const authRequest = addAuthorizationHeader(req);
    return next(authRequest);
  } 
  else {
    return authSvc.refreshToken().pipe(
      switchMap(() => {
        const authRequest = addAuthorizationHeader(req);
        return next(authRequest);
      })
    );
  }
};
const addAuthorizationHeader = (req: HttpRequest<any>) => {
  const token = localStorage.getItem('token');
  return req.clone({
    headers: req.headers.set('Authorization', `Bearer ${token}`)
  });
};
export const IS_PUBLIC = new HttpContextToken(() => false);