import { AppointmentService } from '@core/services/billing';
import { BaseBehaviorSubject } from '../base.behavior.subject';
import { Appointment } from '@core/models/billing';
import { BehaviorSubject, Observable } from 'rxjs';
import { AccountMemberService } from '@core/services/account/account-member.service';
import { IdWithUserInfo } from '@core/models/billing/get-claim-by-identifier';
import { NotificationHandlerService } from '@core/services/common/notification-handler.service';


export class EncounterAppointmentListSubject extends BaseBehaviorSubject<Appointment>
{
    public loading$ = new BehaviorSubject<boolean>(false);
    constructor(private appointmentService: AppointmentService, 
        private accountService: AccountMemberService,
    private notificationService: NotificationHandlerService)
    {
        super();
    }

    // public GetFor(claimId: number, clientId: number, startDate?: Date): void
    // {
    //     var req : AppointmentGetRequest = { ClaimId: claimId, ClientId: clientId, StartDate: startDate, MemberId: this.accountService.memberDetails.memberId, AccountInfoId: this.accountService.memberDetails.accountInfoId}
    //     this.appointmentService.GetFor(req).subscribe(result =>
    //     {
    //         this.data = result;
    //         super.next(result);
    //     });
    // }

    public GetForClaim(claimId: number, method: number, skip?: number, take?: number): Observable<boolean>
    {
        this.loading$.next(true);
        var req: IdWithUserInfo = { Id: claimId, AccountInfoId: this.accountService.memberDetails.accountInfoId, MemberId: this.accountService.memberDetails.memberId };
        if (typeof skip === 'number') req.Skip = skip;
        if (typeof take === 'number') req.Take = take;
        this.appointmentService.GetForClaim(req).subscribe(result =>
        {
            this.data = result;
            super.next(result);
            this.loading$.next(false);
            if (method === 0) this.notificationService.showNotificationSuccess("Appointment(s) deleted succesfully");
            else if (method === 1) this.notificationService.showNotificationSuccess("Appointment(s) added succesfully");
        });
        return this.loading$;
    }
}