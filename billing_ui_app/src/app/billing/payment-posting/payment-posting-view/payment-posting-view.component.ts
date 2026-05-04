import { AfterViewChecked, AfterViewInit, Component, OnDestroy, OnInit, Renderer2, ViewChild } from '@angular/core';
import { ActivatedRoute, Router, NavigationEnd } from '@angular/router';
import { PaymentPostingService } from '@core/services/billing';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { CreatePaymentPatientClaims, PaymentPostingShortInfo } from '@core/models/billing';
import { NotificationTypes } from "@core/enums/common";
import { AttachmentsComponent } from "@app/billing/payment-posting/payment-posting-view/attachments/attachments.component";
import { ErrorsComponent } from "@app/billing/payment-posting/payment-posting-view/errors/errors.component";
import { EOBDetailsComponent } from "@app/billing/payment-posting/payment-posting-view/EOB-details/EOB-details.component";
import { ManualPaymentPatientDetailsComponent } from './manual-posting-patient/payment-patient-details/manual-payment-patient-details.component';
import { PaymentDetailsComponent } from './payment-details/payment-details.component';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { Breadcrumb } from '@core/models/billing/bread-crumb';

@Component({
  selector: 'payment-posting-view',
  templateUrl: './payment-posting-view.html',
  styleUrls: ['./payment-posting-view.css',
    '../status-actions.css']
})
export class PaymentPostingViewComponent implements OnInit,  OnDestroy, AfterViewChecked {
  @ViewChild(PaymentDetailsComponent) paymentDetails: PaymentDetailsComponent;
  @ViewChild(ManualPaymentPatientDetailsComponent) manualPaymentPatientDetailsComponent: ManualPaymentPatientDetailsComponent;
  @ViewChild(AttachmentsComponent) paymentAttachments: AttachmentsComponent;
  @ViewChild(ErrorsComponent) paymentErrors: ErrorsComponent;
  @ViewChild(EOBDetailsComponent) paymentEOB: EOBDetailsComponent;

  private unsubscribeAll$ = new Subject<void>();
  public isManual = false;
  public processAsPatientType: boolean;
  public postBtnDisabled = true;
  paymentId = 0;
  headerTitle = "Payment Posting";

  public postPaymentLinesEvent = new Subject<void>();
  paymentHeaderID = '';
  status = '';
  errors = 0;
  data: PaymentPostingShortInfo;

  showNotification = false;
  notificationType: NotificationTypes;
  notificationText: string;
  patientId: number;
  pendingPatientModel: { paymentId: number; patientIds: string[]; accountInfoId: number; memberId: number; };
  private initializedPatientDialog: boolean= false;
  private breadcrumbClickHandler: (event: any) => void;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private paymentService: PaymentPostingService,
    private accountService: AccountMemberService,
    private renderer: Renderer2,
  ) {
    this.route.params.pipe(takeUntil(this.unsubscribeAll$))
      .subscribe(x => {
        if (x["id"]) {
          this.paymentId = +x["id"];
          this.loadPaymentShortInfo(this.paymentId);
        }
        
        if (x["patientId"]) {
          this.patientId = +x["patientId"];
          const patientIds = [this.patientId.toString()];
          this.pendingPatientModel = {
            paymentId: this.paymentId,
            patientIds: patientIds,
            accountInfoId: this.accountService.memberDetails.accountInfoId,
            memberId: this.accountService.memberDetails.memberId
          };
        }
    });
  }

  ngOnInit(): void {
          // Set initial breadcrumbs
          this.breadcrumbs = [
        { label: 'Payment Posting', url: '/billing/paymentposting/list', tabIndex: 0 },
                { label: 'Payment Details', url: '/billing/paymentposting', tabIndex: 1 }
          ];
          // Listen for breadcrumb updates from child components
          window.addEventListener('updateBreadcrumbs', (event: any) => {
              this.breadcrumbs = event.detail;
          });

          // Intercept breadcrumb clicks to override header navigation
          this.breadcrumbClickHandler = (event: any) => {
              const target = event.target;
              if (target && target.classList.contains('breadcrumb-link')) {
                  event.preventDefault(); // Prevent router navigation
                  const breadcrumbText = target.textContent?.trim();
                  const breadcrumbIndex = this.breadcrumbs.findIndex(b => b.label === breadcrumbText);
                  if (breadcrumbIndex !== -1 && breadcrumbIndex < this.breadcrumbs.length - 1) {
                      this.handleBreadcrumbClick(this.breadcrumbs[breadcrumbIndex], breadcrumbIndex);
                  }
              }
          };
          document.addEventListener('click', this.breadcrumbClickHandler);
      }
       // Method for child components to update breadcrumbs
    updateBreadcrumbs(breadcrumbs: any[]): void {
        this.breadcrumbs = breadcrumbs;
    }

    handleBreadcrumbClick(breadcrumb: any, index: number): void {
        if (index === this.breadcrumbs.length - 1) {
            return; // Don't navigate if clicking on the last breadcrumb
        }
        
        if (breadcrumb.label === 'Payment Posting') {
            // Navigate back to Payment Posting list
            this.router.navigate(['/billing/paymentposting/list']);
        } else if (breadcrumb.label === 'Payment Details') {
            // Remove Service Line Details if present
            this.breadcrumbs = this.breadcrumbs.slice(0, 2);
            this.collapseChildGrids();
        }
    }

    collapseChildGrids(): void {
        // Emit custom event to child components to collapse their grids
        window.dispatchEvent(new CustomEvent('collapseGrids'));
    }

  ngAfterViewChecked(): void {
  if (
    !this.initializedPatientDialog &&
    this.pendingPatientModel &&
    this.manualPaymentPatientDetailsComponent
  ) {
    this.manualPaymentPatientDetailsComponent.showAddPatientDialog = true;
    this.manualPaymentPatientDetailsComponent.addPatientDialogToggle(this.pendingPatientModel);
    this.pendingPatientModel = null;
    this.initializedPatientDialog = true;
  }
}

  loadPaymentShortInfo(id: number) {
    this.paymentService.setId(id);
    if (!id) {
      id = this.paymentId;
    }
    this.paymentService.getPaymentShortInfo(id).pipe(takeUntil(this.unsubscribeAll$))
      .subscribe(data => {
        if (data) {
          this.isManual = data.isManual;
          this.postBtnDisabled = true;
          this.processAsPatientType = data.isPatientType || data.isOtherType || data.isInsuranceType;
          this.data = data;
          let loader = this.renderer.selectRootElement('#loader');
          this.renderer.setStyle(loader, 'display', 'none');
          data
            && (this.paymentHeaderID = `Payment ID #${data.paymentIdentifier}`)
            && (this.status = data.reconcileStatus)
            && (this.errors = data.errorsCount);
        }
        else {
          this.router.navigate(["/billing/not-found"]);
        }
      });
  }

  cancelPostPaymentLines(): void {
    const routeParams = this.route.snapshot.params;
    if (routeParams['patientId']) {
      this.router.navigate(['/billing/patientinvoicing'], {
        queryParams: { tab: 'pendingCollection' }
      });
    } else {
      this.router.navigate(['/billing/paymentposting/list']);
    }
  }

  postPaymentLines(): void {
    this.postPaymentLinesEvent.next();
    // this.postPaymentLinesEvent.subscribe({
    //     next: (data) => {
    //         this.showNotificationComponent(NotificationTypes.success, "Payment line(s) posted successfully");
    //     }, error: (err) => {
    //         console.error(err);
    //     }
    // });
  }

  ngOnDestroy(): void {
    this.postPaymentLinesEvent.unsubscribe();
    this.unsubscribeAll$.next();
    this.unsubscribeAll$.complete();
    this.initializedPatientDialog = false;
    
    // Clean up breadcrumb click handler
    if (this.breadcrumbClickHandler) {
      document.removeEventListener('click', this.breadcrumbClickHandler);
    }
  }

  zeroCreatedClaimsNotify(): void {
    let type = NotificationTypes.error;
    let text = "Patient doesn't have any open claims";

    this.showNotificationComponent(type, text);
  }

  showNotificationComponent(type: NotificationTypes, text: string): void {
    this.notificationType = type;
    this.notificationText = text
    this.showNotification = true;

    window.setTimeout(() => this.hideNotificationComponent(), 5000);
  }

  hideNotificationComponent(): void {
    this.showNotification = false;
  }

  breadcrumbs: Breadcrumb[] = [
  { label: 'Payment Posting', url: '/billing/paymentposting/list', tabIndex: 0 },
  { label: 'Payment Details', url: '/billing/paymentposting', tabIndex: 1 }
];

 selectedTabChanged(event: any) {
  const tabLabel = event.tab.textLabel;
  
  // Update breadcrumb based on selected tab
  this.breadcrumbs[1] = {
    label: tabLabel,
    url: '/billing/paymentposting/list',
    tabIndex: 1
  };

  switch (tabLabel) {
    case "Payment Details":
      this.paymentDetails && this.paymentDetails.updateSummary();
      this.manualPaymentPatientDetailsComponent && this.manualPaymentPatientDetailsComponent.updateSummary();
      break;
    case "EOB Details":
      this.paymentAttachments.loadPaymentDetailsInfo()
      this.paymentEOB && this.paymentEOB.loadInfo()
      break;
    case "Errors":
      this.paymentErrors.loadErrorClaims()
      break;
    case "Attachments":
      this.paymentAttachments.loadPaymentAttachments()
      this.paymentAttachments.loadPaymentDetailsInfo()
      break;
  }
}


}
