import { Component, OnInit, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { Breadcrumb } from '@core/models/billing/bread-crumb';
import { NotificationService } from '@core/services/account/notification.service';

export enum reportingListingTab {
  AccountsReceivables = 1,
  paymentsAdjustments
}
@Component({
  selector: 'app-report',
  templateUrl: './report.component.html',
  styleUrls: ['./report.component.css']
})
export class ReportComponent implements OnInit {
  all_Reports = AllReports;
  headerTitle = "Reporting";
  tabIndex = 0;
  breadcrumbs: Breadcrumb[];

  constructor(private router: Router, private notificationService: NotificationService) { }

  ngOnInit(): void {
    // Subscribing the tab index changes
    this.updateBreadcrumbs(0);
    this.notificationService.tabIndex$.subscribe(tabIndex => {
      this.tabIndex = tabIndex;
      this.updateBreadcrumbs(tabIndex);
    });
  }

  navigateToReport(report: any) {
    this.router.navigate([report.link]);
  }

  onTabChanged(event: any) {
    const updatedIndex = event.index;
    this.updateBreadcrumbs(updatedIndex);
  }

  private updateBreadcrumbs(tabIndex: number): void {
    switch (tabIndex) {
      case 1:
        this.breadcrumbs = [
          { label: 'Reporting', url: '/billing/reporting/list' },
          { label: 'Charge Reports', url: '/billing/reporting/list' }
        ];
        break;
      case 2:
        this.breadcrumbs = [
          { label: 'Reporting', url: '/billing/reporting/list' },
          { label: 'Appointment Reports', url: '/billing/reporting/list' }
        ];
        break;
      case 0:
      default:
        this.breadcrumbs = [
          { label: 'Reporting', url: '/billing/reporting/list' },
          { label: 'Claim Reports', url: '/billing/reporting/list' }
        ];
        break;
    }
  }
}
export const AllReports = [
  {
    report_name: "Claim Reports",
    report_type: "claim_reports",
    reports: [
      {
        name: 'Account Receivables- Claim Level',
        description: `This report breaks down the total accounts receivable by individual claims, providing visibility into the status,
         aging, and outstanding balances of each claim.`,
        link: '/billing/reporting/account-receivables'
      },
      {
        name: 'Payment & Adjustment Activity',
        description: `This report provides a detailed view of all payments and adjustments applied at the individual claim level.
         It captures financial activities such as payments, write-offs, patient responsibility, and contractual adjustments,
         offering a comprehensive picture of how each claim was resolved financially.`,
        link: '/billing/reporting/payment-adjustments'
      },
      {
        name: 'Claim Follow-up',
        description: `This report helps billing staff track and manage claims requiring follow-up review within a specified time period. By including completed follow-up items, the report provides a complete history of actions taken on each claim, allowing users to monitor progress and aid in resolving outstanding issues for accurate claim management.`,
        link: '/billing/reporting/app-claim-follow-up'
      },
      //{ name: 'TODO', description: 'description goes here...', link: '' },
    ]
  },
  {
    report_name: "Charge Reports",
    report_type: "charge-reports",
    reports: [
      {
        name: 'Accounts Receivable- Charge Level',
        description: 'This report breaks down the total accounts receivable by individual charge, providing visibility into the status, aging, and outstanding balances of each charge.',
        link: '/billing/reporting/accounts-receivables-charge-level'
      },
      {
        name: 'Financial Summary',
        description: 'The Monthly Financial Summary Report provides by month/payer view of changes in Accounts Receivable for a selected reporting period, showing the starting A/R balance, monthly charges, payments, adjustments, write-offs, net A/R change, ending balance by month, and unapplied credits reported separately, designed to support billing managers and finance teams with A/R follow-up, reconciliation, month-end close, and audit activities.',
        link: '/billing/reporting/financial-summary-report'
      },
    ]
  },
  {
    report_name: "Appointment Reports",
    report_type: "appointment_reports",
    reports: [
      {
        name: 'Unbilled Appointments',
        description: `This report displays appointments that were sent to the billing system but are no longer associated with a claim.
           This includes scenarios where the original claim was deleted or the appointment was manually removed from the claim.
            The report enables billers to identify these unbilled appointments and provides functionality to regenerate them into 
            new claims for submission, ensuring no billable services are missed.`,
        link: '/billing/reporting/unbilled-appointments'
      },
      {
        name: 'Claim Creation Failed',
        description: `This report shows appointments that were sent to the billing system but for which claims were not
            successfully created. Billers can access the report by clicking Unprocessed Appointments (displaying the count of unprocessed
            appointments) from the orange alert icon whenever any unprocessed appointments exist. The report helps billers
            easily identify these appointments and includes functionality to generate the missing claims, ensuring that no billable
            services are overlooked.`,
        link: '/billing/reporting/unprocessed-appointments'
      }
    ]
  }
]
