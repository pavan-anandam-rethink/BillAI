import { Component, ElementRef, HostListener, Input, OnDestroy, SimpleChanges, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { Breadcrumb } from '@core/models/billing/bread-crumb';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { NotificationService } from '@core/services/account/notification.service';
import { NotificationStorageService } from '@core/services/common/notification-storage.service';
import { AppointmentService } from '@core/services/billing/appointment.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'header-toolbar',
  templateUrl: './header.component.html',
  styleUrls: ['./header.css']
})

export class HeaderComponent implements OnDestroy {
  @Input() isBackBtnShow;
  @Input() isProfileShow;
  @Input() headerTitle;
  @Input() breadcrumbs: Breadcrumb[] = [];

  showUserProfile = false;
  userName = '';
  userRole = '';
  accountDetail = '';

  showNotifications = false;

  // Use the service's notifications array directly so state survives navigation
  get notifications(): any[] {
    return this.notificationService.notifications;
  }
  set notifications(value: any[]) {
    this.notificationService.notifications = value;
  }

  successCount = 0;
  processedCount = 0;
  totalClaims = 0;
  failedCount = 0;
  approvedCount = 0;
  total1 = 0;
  total2 = 0;
  nofailedCount = true;

  private sub!: Subscription;
  private expirationTimers: any[] = [];
  private readonly NOTIFICATION_TTL = 3 * 60 * 1000; // 3 minutes

  @ViewChild('drawer') drawer: any;
  @ViewChild('userAnchorEl', { read: ElementRef }) public userAnchor: ElementRef;
  @ViewChild('userAnchorElArrowUp', { read: ElementRef }) public userAnchorArrowUp: ElementRef;
  @ViewChild('userAnchorElArrowDown', { read: ElementRef }) public userAnchorArrowDown: ElementRef;
  @ViewChild("popup", { read: ElementRef }) public popup: ElementRef;

  constructor(private accountMemberService: AccountMemberService,
    private router: Router, private notificationService: NotificationService, private appointmentService: AppointmentService,
    private notificationStorageService: NotificationStorageService) {
  }

  ngOnInit() {
    this.userName = this.accountMemberService.memberDetails.memberName;
    this.userRole = this.accountMemberService.memberDetails.memberRole;
    this.accountDetail = this.accountMemberService.memberDetails.accountDetail;

    // Only restore from sessionStorage if service has no notifications yet
    if (this.notifications.length === 0) {
      this.loadPersistedNotifications();
    }

    this.ListenToNotifications();

    // Only fetch unprocessed appointments if not already present
    if (!this.notifications.some(n => n.title?.startsWith('Unprocessed Appointments'))) {
      this.notifyUnprocessedAppointments();
    }
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
    this.expirationTimers.forEach(t => clearTimeout(t));
  }

  private loadPersistedNotifications(): void {
    const restored = this.notificationStorageService.loadNotifications();
    if (restored.length > 0) {
      this.notifications = [...restored];
      // Set up expiration timers for each restored notification
      restored.forEach(notify => {
        const remaining = this.notificationStorageService.getRemainingTtl(notify);
        if (remaining > 0) {
          const timer = setTimeout(() => this.removeNotification(notify), remaining);
          this.expirationTimers.push(timer);
        } else {
          this.removeNotification(notify);
        }
      });
      this.successCount = this.notifications.reduce((sum, n: any) => sum + (Number(n.processed) || 0), 0);
    }
  }

  private persistNotifications(): void {
    this.notificationStorageService.saveNotifications(this.notifications);
  }

  setTabIndexFromBreadcrumb(breadcrumb?: Breadcrumb) {
    if (breadcrumb.isReportsPage != undefined && breadcrumb.isReportsPage === true) {
      if (breadcrumb.tabIndex !== undefined) {
        this.notificationService.setTabIndex(breadcrumb.tabIndex);
      }
    }
  }
  navigateToBilling() {
    let url = '/billing/claims/list';
    this.router.navigateByUrl('/', { skipLocationChange: true }).then(() => {
      this.router.navigateByUrl(url);
    })
  }

  @HostListener("document:keydown", ["$event"])
  public keydown(event: KeyboardEvent): void {
    if (event.code === "Escape") {
      this.showUserProfile = false;
    }
  }

  @HostListener("document:click", ["$event"])
  public documentClick(event: KeyboardEvent): void {
    if (!this.contains(event.target)) {
      this.showUserProfile = false;
    }
  }

  private contains(target: EventTarget): boolean {
    return (
      this.userAnchor.nativeElement.contains(target) || !this.userAnchorArrowUp?.nativeElement.contains(target) ||
      this.userAnchorArrowDown?.nativeElement.contains(target) |
      (this.popup ? this.popup.nativeElement.contains(target) : false)
    );
  }

  onDocumentClick() {
    this.showNotifications = false;
    this.showUserProfile = false;
  }

  ListenToNotifications() {
   this.notificationService.connect(
      this.accountMemberService.memberDetails.accountInfoId.toString(),
      this.accountMemberService.memberDetails.memberId.toString()
    );

    this.sub = this.notificationService.claimUpdates$.subscribe((update: any) => {
      const time = new Date();
      // For success/batch updates: only treat as batch success if success === 'Success'.
      // If successFlag is undefined, fall back to original behavior (numeric updates).
      const batchId = (update?.batchId !== undefined && update?.batchId !== null)
        ? update.batchId.toString()
        : `anon-${Date.now()}`;

      // incoming numeric value (either a delta or an absolute count)
      const incoming = Number(update?.processed ?? update?.count ?? 1) || 0;
      // allow sender to indicate whether the incoming number is an absolute total
      const isAbsolute = !!update?.isAbsolute || !!update?.absolute;

      const incomingStatus = update?.status ? String(update.status) : undefined;
      const idx = this.notifications.findIndex(n => {
        const sameBatch = (n.batchId?.toString() ?? '') === batchId;
        if (incomingStatus === 'Failure') {
          // match existing failure notifications for same batch
          return sameBatch && (n.title === 'Claims Failed');
        } else if (incomingStatus === 'Success' || incomingStatus === undefined) {
          // match existing success/processed notifications for same batch
          return sameBatch && (n.title === 'Claims Processed');
        }else if (incomingStatus === 'Approved' || incomingStatus === undefined) {
          // match existing success/processed notifications for same batch
          return sameBatch && (n.title === 'Claims Approved');
        }else if (incomingStatus === 'Rejected' || incomingStatus === undefined) {
          // match existing success/processed notifications for same batch
          return sameBatch && (n.title === 'Claims Rejected');
        }
        
        // fallback: match by batch only
        return sameBatch;
      });

      //pending Review Batch - Rejected

      const successFlagP = update?.status ? String(update.status) : undefined;
      if (successFlagP === 'Rejected') {
        // collect one or more failed claim ids from the update
        const claimIdsIncoming: string[] = [];
        if (update?.claimId) claimIdsIncoming.push(String(update.claimId));
        else if (Array.isArray(update?.claimIds)) claimIdsIncoming.push(...update.claimIds.map(String));
        else if (Array.isArray(update?.failedIds)) claimIdsIncoming.push(...update.failedIds.map(String));
        if (claimIdsIncoming.length === 0) claimIdsIncoming.push('unknown');

        const incomingFailedCount = claimIdsIncoming.length;

        this.totalClaims = Number(update?.total ?? this.totalClaims);

        // Send the ID to EncounterListComponent
        this.notificationService.sendEncounterIdPendingReview(claimIdsIncoming.length > 0 ? Number(claimIdsIncoming[0]) : 0);

        if (idx >= 0) {
          const existing = this.notifications[idx] as any;
          const existingFailedIds: string[] = Array.isArray(existing.failedIds) ? existing.failedIds.slice() : [];
          // merge unique ids
          const mergedIds = Array.from(new Set([...existingFailedIds, ...claimIdsIncoming]));
          const failedCount = mergedIds.length;
          const total = update?.total ?? existing.total ?? 'unknown';
          const message = `${failedCount} claims Rejected`;

          const updated = {
            ...existing,
            batchId,
            title: 'Claims Rejected',
            message,
            icon: 'highlight_off',
            time,
            failedIds: mergedIds,
            failedCount,
            processed: failedCount,
            total,
            buttonText: 'Approval Failed'
          };

          // move updated notification to the top
          this.notifications = [updated, ...this.notifications.slice(0, idx), ...this.notifications.slice(idx + 1)];

          // Auto-remove after 3 minutes
          setTimeout(() => {
            this.removeNotification(updated);
          }, this.NOTIFICATION_TTL);

        } else {
          const failedIds = claimIdsIncoming;
          const failedCount = incomingFailedCount;
          const total = update?.total ?? 'unknown';
          const message = `Claim Rejected for Processing`;
          const newNotification = {
            batchId,
            title: 'Claims Rejected',
            message,
            icon: 'cancel',
            time,
            failedIds,
            failedCount,
            processed: failedCount,
            total,
            buttonText: 'Approval Failed'
          };
          this.notifications = [newNotification, ...this.notifications];

          // Auto-remove after 3 minutes
          setTimeout(() => {
            this.removeNotification(newNotification);
          }, this.NOTIFICATION_TTL);

        }

        // recompute successCount (failures don't add to processed sum)
        this.successCount = this.notifications.reduce((sum, n: any) => sum + (Number(n.processed) || 0), 0);
        this.persistNotifications();
        return;
      }



      // If sender indicates a failure for a specific claim, show a failure notification with the claimId
      const successFlag = update?.status ? String(update.status) : undefined;
      if (successFlag === 'Failure') {
        // collect one or more failed claim ids from the update
        const claimIdsIncoming: string[] = [];
        if (update?.claimId) claimIdsIncoming.push(String(update.claimId));
        else if (Array.isArray(update?.claimIds)) claimIdsIncoming.push(...update.claimIds.map(String));
        else if (Array.isArray(update?.failedIds)) claimIdsIncoming.push(...update.failedIds.map(String));
        if (claimIdsIncoming.length === 0) claimIdsIncoming.push('unknown');

        const incomingFailedCount = claimIdsIncoming.length;

        this.totalClaims = Number(update?.total ?? this.totalClaims);

        // Send the ID to EncounterListComponent
        this.notificationService.sendEncounterId(claimIdsIncoming.length > 0 ? Number(claimIdsIncoming[0]) : 0);

        if (idx >= 0) {
          const existing = this.notifications[idx] as any;
          const existingFailedIds: string[] = Array.isArray(existing.failedIds) ? existing.failedIds.slice() : [];
          // merge unique ids
          const mergedIds = Array.from(new Set([...existingFailedIds, ...claimIdsIncoming]));
          const failedCount = mergedIds.length;
          const total = update?.total ?? existing.total ?? 'unknown';
          const message = `${failedCount} claims failed to process`;

          const updated = {
            ...existing,
            batchId,
            title: 'Claims Failed',
            message,
            icon: 'highlight_off',
            time,
            failedIds: mergedIds,
            failedCount,
            processed: failedCount,
            total
          };

          // move updated notification to the top
          this.notifications = [updated, ...this.notifications.slice(0, idx), ...this.notifications.slice(idx + 1)];

          // Auto-remove after 3 minutes
          setTimeout(() => {
            this.removeNotification(updated);
          }, this.NOTIFICATION_TTL);

        } else {
          const failedIds = claimIdsIncoming;
          const failedCount = incomingFailedCount;
          const total = update?.total ?? 'unknown';
          const message = `Claim failed to process`;
          const newNotification = {
            batchId,
            title: 'Claims Failed',
            message,
            icon: 'highlight_off',
            time,
            failedIds,
            failedCount,
            processed: failedCount,
            total
          };
          this.notifications = [newNotification, ...this.notifications];

          // Auto-remove after 3 minutes
          setTimeout(() => {
            this.removeNotification(newNotification);
          }, this.NOTIFICATION_TTL);

        }

        // recompute successCount (failures don't add to processed sum)
        this.successCount = this.notifications.reduce((sum, n: any) => sum + (Number(n.processed) || 0), 0);
        this.persistNotifications();
        return;
      }

      // Pending Review Batch - show in notification

      if (successFlag === 'Approved' || successFlag === undefined) {

        const successClaimIds: string[] = [];
        if (update?.claimId) successClaimIds.push(String(update.claimId));

        // Send the ID to EncounterListComponent
        this.notificationService.sendSuccessClaimId(successClaimIds?.length > 0 ? Number([...new Set(successClaimIds)][0]) : 0);

        // Treat update as batch-processed update (only show success message when success === 'Success',
        // but when success is undefined we preserve previous numeric behavior)
        if (idx >= 0) {
          const existing = this.notifications[idx] as any;
          const existingSuccessIds: string[] = Array.isArray(existing.successClaimIds) ? existing.successClaimIds.slice() : [];
          // merge unique ids
          const mergedIds = Array.from(new Set([...existingSuccessIds, ...successClaimIds]));
          //const failedCount = mergedIds.length;
          const existingProcessed = mergedIds.length;

          this.totalClaims = Number(update?.total ?? existing.total ?? 0);

          const newProcessed = isAbsolute ? incoming : existingProcessed + incoming;
          this.processedCount = newProcessed;

          const message = `${mergedIds.length} out of ${update?.total ?? existing.total ?? 'unknown'} claims processed`;

          const updated = {
            ...existing,
            batchId,
            successClaimIds: [...(existing.successClaimIds || []), ...successClaimIds],
            processed: mergedIds.length, // newProcessed,
            total: update?.total ?? existing.total,
            message,
            time
          };

          // move updated notification to the top
          this.notifications = [updated, ...this.notifications.slice(0, idx), ...this.notifications.slice(idx + 1)];

          // Auto-remove after 3 minutes
          setTimeout(() => {
            this.removeNotification(updated);
          }, this.NOTIFICATION_TTL);

        } else {
          const processed = isAbsolute ? incoming : incoming;
          const message = `${processed} out of ${update?.total ?? 'unknown'} claims Approved`;
          const newNotification = {
            batchId,
            successClaimIds: successClaimIds,
            title: 'Claims Approved',
            message,
            icon: 'check_circle_outline',
            time,
            processed,
            total: update?.total ?? 'unknown'
          };
          this.notifications = [newNotification, ...this.notifications];

          // Auto-remove after 3 minutes
          setTimeout(() => {
            this.removeNotification(newNotification);
          }, this.NOTIFICATION_TTL);

        }

        // Update successCount as the sum of processed counts across all notifications
        this.successCount = this.notifications.reduce((sum, n: any) => sum + (Number(n.processed) || 0), 0);
      } else {
        // If successFlag exists but is neither 'Success' nor 'Failure', ignore or handle as needed.
        // For now, do nothing.
      }



      if (successFlag === 'Success' || successFlag === undefined) {

        const successClaimIds: string[] = [];
        if (update?.claimId) successClaimIds.push(String(update.claimId));

        // Send the ID to EncounterListComponent
        this.notificationService.sendSuccessClaimId(successClaimIds?.length > 0 ? Number([...new Set(successClaimIds)][0]) : 0);

        //pending Review Send the ID to EncounterListComponent
        //this.notificationService.sendSuccessPendingReviewClaimId(successClaimIds?.length > 0 ? Number([...new Set(successClaimIds)][0]) : 0);

        // Treat update as batch-processed update (only show success message when success === 'Success',
        // but when success is undefined we preserve previous numeric behavior)
        if (idx >= 0) {
          const existing = this.notifications[idx] as any;
          const existingSuccessIds: string[] = Array.isArray(existing.successClaimIds) ? existing.successClaimIds.slice() : [];
          // merge unique ids
          const mergedIds = Array.from(new Set([...existingSuccessIds, ...successClaimIds]));
          //const failedCount = mergedIds.length;
          const existingProcessed = mergedIds.length;

          this.totalClaims = Number(update?.total ?? existing.total ?? 0);

          const newProcessed = isAbsolute ? incoming : existingProcessed + incoming;
          this.processedCount = newProcessed;

          const message = `${mergedIds.length} out of ${update?.total ?? existing.total ?? 'unknown'} claims processed`;

          const updated = {
            ...existing,
            batchId,
            successClaimIds: [...(existing.successClaimIds || []), ...successClaimIds],
            processed: mergedIds.length, // newProcessed,
            total: update?.total ?? existing.total,
            message,
            time
          };

          // move updated notification to the top
          this.notifications = [updated, ...this.notifications.slice(0, idx), ...this.notifications.slice(idx + 1)];

          // Auto-remove after 3 minutes
          setTimeout(() => {
            this.removeNotification(updated);
          }, this.NOTIFICATION_TTL);

        } else {
          const processed = isAbsolute ? incoming : incoming;
          const message = `${processed} out of ${update?.total ?? 'unknown'} claims processed`;
          const newNotification = {
            batchId,
            successClaimIds: successClaimIds,
            title: 'Claims Processed',
            message,
            icon: 'check_circle_outline',
            time,
            processed,
            total: update?.total ?? 'unknown'
          };
          this.notifications = [newNotification, ...this.notifications];

          // Auto-remove after 3 minutes
          setTimeout(() => {
            this.removeNotification(newNotification);
          }, this.NOTIFICATION_TTL);

        }
        // Update successCount as the sum of processed counts across all notifications
        this.successCount = this.notifications.reduce((sum, n: any) => sum + (Number(n.processed) || 0), 0);
      } else {
        // If successFlag exists but is neither 'Success' nor 'Failure', ignore or handle as needed.
        // For now, do nothing.
      }

      this.persistNotifications();
    });
  }

  retryFailedClaims(failedIds: string[], buttonText: string) {
    if (buttonText === 'Review Appointments') {
      // Navigate to the Appointments review page
      this.router.navigate(['/billing/reporting/unprocessed-appointments']);
    } else if (buttonText === 'Approval Failed') {
      const numericIds = failedIds.map(id => Number(id));
      this.notificationService.sendFailureClaimId(failedIds.length > 0 ? numericIds : []);
    }
    else {
      const numericIds = failedIds.map(id => Number(id));
      this.notificationService.viewFailedEncounters(failedIds.length > 0 ? numericIds : []);
      
    }
  }

  removeNotification(notify: any) {
    this.notifications = this.notifications.filter(n => n !== notify);
    this.notificationStorageService.removeNotification(notify);
  }

  shouldShowNotify1(notify: any): boolean {
    if (notify.batchId !== this.notifications[0].batchId) {
      return false;
    }
    if (notify.processed === notify.total) {
      return false;
    } else {
      if (notify.title?.toLowerCase().includes('claims approved')) {
        this.approvedCount = Number(notify.processed);
        this.total1 = Number(notify.total);
      }

      const updatedNotifications2: any[] = [];

      if (notify.title?.toLowerCase().includes('claims rejected')) {
        this.failedCount = Number(notify.processed);
        this.nofailedCount = true;
        this.total2 = Number(notify.total);

        this.notifications.forEach(n => {
          if (!(n.batchId === notify.batchId && n.title.toLowerCase() === 'claims approved')) {
            updatedNotifications2.push(n);
          }
        });

        this.notifications = updatedNotifications2;
      }
    }

    const result = !(
      (this.approvedCount + this.failedCount) === this.total1 ||
      (this.approvedCount + this.failedCount) === this.total2
    );

    if (result) {
      setTimeout(() => {
        return false; 
      }, 3000);
    }

    return result;
  }

  shouldShowNotify2(notify: any): boolean {
    if (notify.batchId !== this.notifications[0].batchId) {
      return false;
    }
    if (notify.processed === notify.total) {
      return false;

     
    } else {
      if (notify.title?.toLowerCase().includes('claims processed')) {
        this.approvedCount = Number(notify.processed);
        this.total1 = Number(notify.total);
      }

      const updatedNotifications1: any[] = [];

      if (notify.title?.toLowerCase().includes('claims failed')) {
        this.failedCount = Number(notify.processed);
        this.nofailedCount = true;
        this.total2 = Number(notify.total);

        this.notifications.forEach(n => {
          if (!(n.batchId === notify.batchId && n.title.toLowerCase() === 'claims processed')) {
            updatedNotifications1.push(n);
          }
        });

        this.notifications = updatedNotifications1;
      }
    }

    const result = !(
      (this.approvedCount + this.failedCount) === this.total1 ||
      (this.approvedCount + this.failedCount) === this.total2
    );

    if (result) {
      setTimeout(() => {
        return false; 
      }, 3000);
    }

    return result;
  }

  notifyUnprocessedAppointments() {
    this.appointmentService
      .getUnprocessedAppointmentsCount()
      .subscribe({
        next: (unprocessedCount: number) => {
          // Remove any existing unprocessed appointment notification to avoid duplicates
          this.notifications = this.notifications.filter(
            n => !n.title?.startsWith('Unprocessed Appointments')
          );
          if (unprocessedCount > 0) {
            this.notifications.push({
              title: `Unprocessed Appointments : ${unprocessedCount}`,
              //message: `There are ${unprocessedCount} unprocessed appointments in last 30 days.`,
              buttonText: 'Review Appointments',
              icon: 'info',
              time: new Date()
            });
          }
        },
        error: (error) => {
          // show error        
        }
      });
  }
}
