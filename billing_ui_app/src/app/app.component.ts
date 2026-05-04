import { AfterContentChecked, AfterViewInit, Component, OnInit, Renderer2 } from '@angular/core';
import { AuthService, AuthenticatedResponse } from '@core/services/sso/auth.service';
import { Idle, DEFAULT_INTERRUPTSOURCES } from '@ng-idle/core';
import { Keepalive } from '@ng-idle/keepalive';
import { environment } from 'src/environments/environment';
import { Router } from '@angular/router';
import { LoaderService } from '@core/services/common';
import { TakeUntilDestroyService } from '@core/services/common/takeuntill-destroy.service';
import { HttpErrorResponse } from '@angular/common/http';
import { HttpService } from '@core/services/http.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})

export class AppComponent implements OnInit, AfterViewInit, AfterContentChecked {
  opened: boolean = true;
  // idleState = 'Not started.';
  timedOut = false;
  lastPing?: Date = null;
  loading: boolean = true;
  displayLoader = false;
  displayValidateLoader = false;
  isValidationApi = false;
  displayError = false;
  errorMessage: string;
  validationMessageStr: string = 'Validating Claim(s) may take some time. Please wait...';
  
  constructor(private router: Router,
    private authSvc: AuthService,
    private idle: Idle, 
    private keepalive: Keepalive,
    private loader: LoaderService,
    private destroyService: TakeUntilDestroyService,
    private renderer:Renderer2,
    private httpService: HttpService
    ) {
      // sets an idle timeout of 10 minutes, for testing purposes.
    idle.setIdle(600);
    // sets a timeout period of 5 seconds. after 10 seconds of inactivity, the user will be considered timed out.
    idle.setTimeout(5);
    // sets the default interrupts, in this case, things like clicks, scrolls, touches to the document
    idle.setInterrupts(DEFAULT_INTERRUPTSOURCES);

    // idle.onIdleEnd.subscribe(() => { 
    //   this.idleState = 'No longer idle.'
    //   console.log(this.idleState);
    //   this.reset();
    // });
    
    idle.onTimeout.subscribe(() => {
      // this.idleState = 'Timed out!';
      this.timedOut = true;
      // this.authSvc.logout(); //Uncomment after Home Page implementation
      this.authSvc.removeTokens(); //Delete after Home Page implementation
      window.location.href = environment.rethinkBHUrl; //Delete after Home Page implementation
      // console.log(this.idleState);
    });
    
    // idle.onIdleStart.subscribe(() => {
    //     this.idleState = 'You\'ve gone idle!'
    //     console.log(this.idleState);
    // });
    
    // idle.onTimeoutWarning.subscribe((countdown) => {
    //   this.idleState = 'You will time out in ' + countdown + ' seconds!'
    //   console.log(this.idleState);
    // });

    // sets the ping interval to 15 seconds
    keepalive.interval(15);
    keepalive.onPing.subscribe(() => this.lastPing = new Date());

    this.reset();
  }

  getQueryParameters(url: string, keysArray: string[]) {
    const url_ = new URL(url);
    const urlParams = new URLSearchParams(url_.search);
    const result: any = {};
    let found = false;

    keysArray.forEach((key) => {
      const value = urlParams.get(key);
        if (value !== null) {
          found = true;
          result[key] = value;
        }
      }
    );

    return found ? result : null;
  }

ngOnInit() {

    //Step 1: Check if any of the valid tokens are available: token, testmode
    let queryParams = this.getQueryParameters(window.location.href, ['token', 'testmode']);
    if(queryParams != null)
    {
      // Step 2: Retrieve Rethink token from query string
      let rethink_Token = queryParams['token'];
      if (rethink_Token !== undefined){
        //Remove existing tokens, if any
        this.authSvc.removeTokens();
        //Step 3: Create JWT token
        //Step 3A: Call Rethink API to validate token & receive user data

        this.httpService.apiUrl$.subscribe(url => {
          if (url.includes('Refresh')) {
            this.loadingCompleted();
          }
        });

        this.authSvc.login(rethink_Token).subscribe(() => {
          this.loadingCompleted();
          //Redirect to login if not a valid token
          error: (err: HttpErrorResponse) => {
            this.authSvc.logout()
          }
        });
      }

      let testmode = queryParams['testmode'];
      if (testmode !== undefined){
        //Step 4: Hack for local debugging & lower env
        let authResponse: AuthenticatedResponse = {
          token: environment.token,
          refreshToken: 'sDjdWYn8/+cWt59Pa2QDzQ71879PQg18pyoQU0dxmHU='
        }
        this.authSvc.storeTokens(authResponse);
        this.authSvc.scheduleTokenRefresh(authResponse.token);
        this.loadingCompleted();
        this.router.navigate([this.authSvc.FindDefaultDashboard()]);
      }
    }
    else{
      this.loadingCompleted();
    }
  }

  ngAfterContentChecked(): void {
    this.loader.loaderState
            .subscribe((x) => {
                if (this.displayLoader !== x) {
                    this.displayLoader = x;
                }

                const activeLink = document.querySelector(".secondMenuPanel .mat-tab-label-active") as HTMLElement;
                if (activeLink) {
                    const parent = document.querySelector(".secondMenuPanel .mat-tab-link-container") as HTMLElement;
                    parent.scrollLeft = activeLink.offsetLeft - parent.offsetLeft;
                }

                const activeMenuLink = document.querySelector(".healthcareSubNav .mat-tab-label-active") as HTMLElement;
                if (activeMenuLink) {
                    const parent = document.querySelector(".healthcareSubNav .mat-tab-link-container") as HTMLElement;
                    parent.scrollLeft = activeMenuLink.offsetLeft - parent.offsetLeft;
                }

                const activeSettingsMenuLink = document.querySelector(".companyAccountSubNav .mat-tab-label-active") as HTMLElement;
                if (activeSettingsMenuLink && activeSettingsMenuLink.parentElement) {
                    const parent = document.querySelector(".companyAccountSubNav .mat-tab-link-container") as HTMLElement;
                    parent.scrollLeft = activeSettingsMenuLink.offsetLeft - parent.offsetLeft;
                }

                const staffMainNav = document.querySelector(".landscape-mobile-staff .mat-tab-label-active") as HTMLElement;
                if (staffMainNav) {
                    const parent = document.querySelector(".landscape-mobile-staff .mat-tab-link-container") as HTMLElement;
                    parent.scrollLeft = staffMainNav.offsetLeft - parent.offsetLeft;
                }
            });
           
            this.loader.validationLoaderState
            .subscribe((x) => {
                if (this.displayValidateLoader !== x) {
                    this.displayValidateLoader = x;
                }
            });
      // if (this.httpService.apiUrl.includes('Refresh')) {
      //   this.loadingCompleted();
      // }

      this.httpService.apiUrl$.subscribe(url => {
        if (url.includes('Refresh')) {
          this.loadingCompleted();
        }
      });
  }

  

  loadingCompleted(){
    this.loading = false;
  }

  ngAfterViewInit(): void {    
    window.setTimeout(() => {
      let loader = this.renderer.selectRootElement('#loader');
      this.renderer.setStyle(loader, 'display', 'none');
    }, 500);
  }

  onMouseHover(){
    this.opened = true;
  }

  onClick(){
    this.opened = !this.opened;
  }

  reset() {
    this.idle.watch();
    // this.idleState = 'Started.';
    this.timedOut = false;
  }

  close() {
    this.destroyService.markForDestruction();
  }
}