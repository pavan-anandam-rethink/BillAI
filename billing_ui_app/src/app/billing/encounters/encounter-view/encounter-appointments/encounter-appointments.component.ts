import {
  Component,
  OnInit,
  OnDestroy,
  Input,
  Output,
  EventEmitter,
} from '@angular/core';
import { Observable, Subscription } from 'rxjs';
import { GridDataResult } from '@progress/kendo-angular-grid';
import { State, process } from '@progress/kendo-data-query';
import { AppointmentService, ClaimService } from '@core/services/billing';
import { Appointment, Encounter } from '@core/models/billing';
import { ConfirmDialog, NotifyDialog } from '@core/models/common';
import { EncounterAppointmentListSubject } from '@app/subjects/billing/appointmentlist.subject';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { environment } from 'src/environments/environment';
import { AccountPermissions } from '@core/enums/account/account-permissions';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';

@Component({
  selector: 'app-encounter-appointments',
  templateUrl: './encounter-appointments.component.html',
  styleUrls: ['./encounter-appointments.component.css'],
})
export class EncounterAppointmentsComponent implements OnInit, OnDestroy {
  @Input()
  claim: Encounter | null = null;
  @Input()
  public disableLink = false;
  @Output() appointmentLinked = new EventEmitter<object>();
  @Input() readOnly: boolean;

  showAddAppointmentDialog = false;
  public forbidAddAppointmentShow = false;

  private appointmentIdToRemove = 0;
  gridLength: number;

  public confirmDeleteAppointmentDialog: ConfirmDialog = new ConfirmDialog(
    false,
    'Delete Appointment',
    "Are you sure you'd like to perform this action?",
    'Delete',
    'Cancel'
  );

  notifyDialog: NotifyDialog = new NotifyDialog(false, '', '');

  private appointmentLinkListSubject: EncounterAppointmentListSubject;
  public viewAppointmentLinkList: GridDataResult;
  public gridStateAppointmentLinkList: State = {
    sort: [{ dir: 'asc', field: 'startDate' }],
    skip: 0,
    take: 20,
  };

  private subscriptions = new Subscription();

  rethinkUrl: string;
  canEditApprove = false;

  gridPageSizes: any;

  constructor(
    private appointmentService: AppointmentService,
    private accountService: AccountMemberService,
    private notificationService: NotificationHandlerService,
    private claimsService: ClaimService
  ) {
    this.rethinkUrl = environment.rethinkBHUrl;

    this.getGridPageSizes();

    this.appointmentLinkListSubject = new EncounterAppointmentListSubject(
      this.appointmentService,
      this.accountService,
      notificationService
    );
    this.subscriptions.add(
      this.appointmentLinkListSubject.subscribe((data: any) => {
        this.gridLength = data.length;
        this.viewAppointmentLinkList = process(
          data,
          this.gridStateAppointmentLinkList
        );
      })
    );
    this.subscriptions.add(
      this.accountService.accountMemberSettings.subscribe((x) => {
        if (x) {
          this.canEditApprove = this.accountService.checkPermissionLevel(
            AccountPermissions.BillingEditApprovedAppointments
          );
        }
      })
    );
  }

  ngOnInit(): void {
    if (this.claim) {
      // request first page (use current take). If take == 0, subject will request all rows
      this.appointmentLinkListSubject.GetForClaim(
        this.claim.id,
        2,
        this.gridStateAppointmentLinkList.skip,
        this.gridStateAppointmentLinkList.take
      );
    }

    this.subscriptions.add(
      this.appointmentService.onLink.subscribe((result) => {
        if (this.claim) {
          this.appointmentLinkListSubject.GetForClaim(
            this.claim.id,
            result,
            this.gridStateAppointmentLinkList.skip,
            this.gridStateAppointmentLinkList.take
          );
        }
      })
    );
  }

  addAppointmentWithStatusCheck(): void {
    if (this.claim && this.claim.forbidAddAppointment === 1) {
      this.forbidAddAppointmentShow = true;
      //this.confirmAddAppointmentDialog.opened = true;
    } else {
      this.addAppointmentDialogToggle();
    }
  }

  navigateToStaff(clientId) {
    var url = this.rethinkUrl + '/core/staff/' + clientId + '/info/general'; // /' + authId;
    window.open(url, '_blank');
  }
  addWarningDialogConfirmed(): void {
    this.forbidAddAppointmentShow = !this.forbidAddAppointmentShow;
    this.addAppointmentDialogToggle();
  }

  addAppointmentDialogToggle(): void {
    this.showAddAppointmentDialog = !this.showAddAppointmentDialog;
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  public onAppointmentLinkListStateChange(state: State): void {
    this.gridStateAppointmentLinkList = state;
    // If take is provided (not zero), request server-side page; otherwise just reapply client-side pagination
    const take =
      this.gridStateAppointmentLinkList &&
      this.gridStateAppointmentLinkList.take
        ? this.gridStateAppointmentLinkList.take
        : 0;
    const skip =
      this.gridStateAppointmentLinkList &&
      this.gridStateAppointmentLinkList.skip
        ? this.gridStateAppointmentLinkList.skip
        : 0;
    if (take && take !== 0) {
      if (this.claim) {
        this.appointmentLinkListSubject.GetForClaim(
          this.claim.id,
          2,
          skip,
          take
        );
      }
    } else {
      // client-side: re-emit existing data so `process` is re-run
      this.gridStateAppointmentLinkList.take = this.gridLength;
      this.appointmentLinkListSubject.sync();
    }
  }

  onAppointmentNumberClick(dataItem: Appointment) {
    var url = this.rethinkUrl + `/core/scheduler/individual?id=${dataItem.id}`; // /' + authId;
    window.open(url, '_blank');
  }

  openDeleteAppointmentDialog(lineId: number): void {
    if (this.gridLength > 0) {
      this.appointmentIdToRemove = lineId;
      this.confirmDeleteAppointmentDialog.opened = true;
    } else {
      setTimeout(() => {
        this.notifyDialog.message =
          'Cannot remove the only appointment linked to this claim. Remove appointment after adding another one';
        this.notifyDialog.opened = true;
      });
    }
  }

  createAppointmentEvent(event) {
    if (event === 'close') this.addAppointmentDialogToggle();
    if (event.saveAppointmentsIds === 0) {
      return;
    }

    if (event.claimId) {
      this.appointmentService.LinkAppointments(
        event.claimId,
        event.saveAppointmentsIds
      );
      this.addAppointmentDialogToggle();
    }
  }

  removeAppointment() {
    if (this.appointmentIdToRemove === 0) {
      return;
    }

    if (this.claim) {
      this.appointmentService.UnlinkAppointment(
        this.claim.id,
        this.appointmentIdToRemove
      );
    }
  }

  getGridPageSizes(): void {
    const storedGridPageSizes = localStorage.getItem('gridPageSizes')
      ? JSON.parse(localStorage.getItem('gridPageSizes') || '')
      : null;
    if (storedGridPageSizes) {
      this.gridPageSizes = storedGridPageSizes;
    } else {
      this.subscriptions.add(
        this.claimsService
          .getGridPageSizes()
          .subscribe(
            (sizes: Array<number | { text: string; value: number }>) => {
              this.gridPageSizes = sizes;
            }
          )
      );
    }
  }

  getPageStart(total: number): number {
    if (!total) return 0;
    const skip = this.gridStateAppointmentLinkList?.skip || 0;
    return Math.min(skip + 1, total);
  }

  getPageEnd(total: number): number {
    if (!total) return 0;
    const skip = this.gridStateAppointmentLinkList?.skip || 0;
    const take = this.gridStateAppointmentLinkList?.take || 0;
    if (take === 0) {
      const loaded = this.viewAppointmentLinkList?.data?.length || 0;
      return Math.min(loaded || total, total);
    }
    return Math.min(skip + take, total);
  }
}
