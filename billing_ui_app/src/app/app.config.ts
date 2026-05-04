import { provideHttpClient, withInterceptors } from '@angular/common/http';
import {APP_INITIALIZER, ApplicationConfig} from '@angular/core';
import { authInterceptor } from '@core/services/auth.interceptor';
import { AuthService } from '@core/services/sso/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    {
      provide: APP_INITIALIZER,
      useFactory: initializerFactory,
      multi: true,
      deps: [AuthService]
    },
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
}

export function initializerFactory(authService: AuthService) {
  return () => authService.refreshToken();
}