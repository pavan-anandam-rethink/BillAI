import { Injectable } from '@angular/core';
import { Observable } from 'rxjs/internal/Observable';
import { Subject } from 'rxjs/internal/Subject';
import { map } from 'rxjs/operators';
import { HttpService } from '../http.service';
import { Appointment } from '@core/models/billing';

import { NotificationService } from '@progress/kendo-angular-notification';
import { BehaviorSubject } from 'rxjs';
import { environment } from 'src/environments/environment';
import { AccountMemberService } from '../account/account-member.service';
import { IdWithUserInfo } from '@core/models/billing/get-claim-by-identifier';
import { NotificationHandlerService } from '../common/notification-handler.service';

export class AppointmentGetRequest {
    ClaimId: number;
    ClientId: number;
    AccountInfoId: number;
    MemberId: number;
    locationId?: number;
    StartDate?: Date;
    EndDate?: Date;
}
@Injectable({
  providedIn: 'root'
})
export class AppointmentService {
    public onLink: Subject<any>;
    public onLinkClaimInfo: Subject<any>;
    private apiBaseUrl: string;

    constructor(private http: HttpService, private notificationService: NotificationHandlerService, private accountService: AccountMemberService) {
        this.onLink = new Subject<any>();
        this.onLinkClaimInfo = new Subject<any>();
        this.apiBaseUrl = environment.claimApiBaseUrl;
    }

    private mapList(appointments: Appointment[]): Appointment[] {
        appointments.forEach((appointment) => {
            appointment.startDate = new Date(appointment.startDate);
            appointment.endDate = new Date(appointment.endDate);
            appointment.billingCode = appointment.billingCode + (appointment.billingCode2 ? ('/' + appointment.billingCode2) : '');
            return appointment;
        });
        return appointments;
    }

    public GetFor(req : AppointmentGetRequest): Observable<Appointment[]> {
        return this.http.post<Appointment[]>(this.apiBaseUrl + '/Appointment/GetFor', req, { showSpinner: false }).pipe(map(this.mapList));
    }

    public GetForClaim(req: IdWithUserInfo): Observable<Appointment[]> {
        return this.http.post<Appointment[]>(this.apiBaseUrl + '/Appointment/GetForClaim', req).pipe(map(this.mapList));
    }


    public LinkAppointments(claimId: number, appointmentIds: number[]) : void {
        var model = {
            ClaimId: claimId,
            appointmentIds: appointmentIds,
            accountInfoId :this.accountService.memberDetails.accountInfoId, 
            memberId:this.accountService.memberDetails.memberId
        }
        this.http.post(this.apiBaseUrl + '/Appointment/LinkAppointments', model).subscribe((result) => {
            if(result)
            {
                this.onLink.next(1);  
                this.onLinkClaimInfo.next(result);  
            }   
            else{
                this.notificationService.showNotificationSuccess("Failed to add appointment");
            }
        });
    }

    public UnlinkAppointment(claimId: number, appointmentIds: number) : void {
        var model = { ClaimId: claimId, AppointmentIds:[appointmentIds], AccountInfoId :this.accountService.memberDetails.accountInfoId, MemberId:this.accountService.memberDetails.memberId };
        this.http.post(this.apiBaseUrl +'/Appointment/UnLinkAppointments', model)
        .subscribe( 
           (result) => {
                if(result)
                {
                    this.onLink.next(0);
                    this.onLinkClaimInfo.next(result);  
                }
                else{
                    this.notificationService.showNotificationSuccess("Failed to delete appointment");
                }
                
            }    
        )
        .add(()=>{
        });
    }
    
    getUnprocessedAppointmentsCount() {
        const accountInfoId = this.accountService.memberDetails.accountInfoId
        return this.http.get<number>(this.apiBaseUrl + `/AppointmentReports/GetUnprocessedAppointmentsCount/${accountInfoId}`);
    }
}