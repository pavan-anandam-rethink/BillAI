import { CUSTOM_ELEMENTS_SCHEMA, NgModule } from '@angular/core';
import { HttpClientModule, provideHttpClient, withInterceptors } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { AppRoutingModule } from './app-routing.module';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AppComponent } from './app.component';
import { ComponentsModule } from './components/components.module';
import { KendoModule } from './plugins/kendo.module';
import { HttpService } from '@core/services';
import { ErrorPopupService, LoaderService } from '@core/services/common';
import { DatePipe } from '@angular/common';
import { ContainedButtonModule } from "./shared/components/buttons/contained-btn/contained-btn.module";
import { OutlinedButtonModule } from './shared/components/buttons/outlined-btn/outlined-btn.module';
import { MaterialModule } from './plugins/material.module';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS, MatFormFieldDefaultOptions } from '@angular/material/form-field';
import { SidebarModule } from './shared/components/sidebar/sidebar.module';
import { RequestUserData } from '@core/utils/request-user-data';
import { ConfirmDialogModule } from './shared/components/confirmation-dialog/confirm-dialog.module';
import { IconsModule } from '@progress/kendo-angular-icons';
import { DirtyFormGuard } from '@core/guards/dirty-form-guard';
import { ShellComponent } from './shell/shell.component';
import { JwtModule } from "@auth0/angular-jwt";
import { environment } from 'src/environments/environment';
import { authInterceptor } from '@core/services/auth.interceptor';
import { NgIdleKeepaliveModule } from '@ng-idle/keepalive';
import { authGuard } from '@core/guards/auth-guard';
import { roleGuard } from '@core/guards/role-guard';
import { ToastrModule } from 'ngx-toastr';
import { InputsModule, TextBoxModule } from '@progress/kendo-angular-inputs';

const appearance: MatFormFieldDefaultOptions = {
    appearance: 'outline'
  };

@NgModule({
    declarations: [
        AppComponent,
        ShellComponent,
    ],
    providers: [
        HttpService,
        LoaderService,
        ErrorPopupService,
        DatePipe,
        RequestUserData,
        { provide: MAT_FORM_FIELD_DEFAULT_OPTIONS, useValue: appearance },
        authGuard,
        roleGuard,
        DirtyFormGuard,
        provideHttpClient(withInterceptors([authInterceptor]))
    ],
    bootstrap: [AppComponent],
    imports: [
        BrowserModule,
        AppRoutingModule,
        BrowserAnimationsModule,
        KendoModule,
        MaterialModule,
        FormsModule,
        ReactiveFormsModule,
        HttpClientModule,
        RouterModule,
        ComponentsModule,
        ContainedButtonModule,
        OutlinedButtonModule,
        ConfirmDialogModule,
        InputsModule,
        TextBoxModule,
        JwtModule.forRoot({
            config: {
              tokenGetter: () => localStorage.getItem('token'),
              allowedDomains: [environment.authApiBaseUrl, environment.claimApiBaseUrl, environment.clientApiBaseUrl, environment.reportingApiBaseUrl],
              disallowedRoutes: [],
              authScheme: "Bearer "
            }
          }),
        NgIdleKeepaliveModule.forRoot(),
        SidebarModule,
        IconsModule,
        ToastrModule.forRoot({
          closeButton: true,
          timeOut: 10000, 
          progressBar: true,
          preventDuplicates: true,
          positionClass: 'toast-top-center',
      }),
    ],
    schemas: [
        CUSTOM_ELEMENTS_SCHEMA
      ]
})
export class AppModule { }
