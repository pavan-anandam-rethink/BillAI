import { AppointmentService } from '@core/services/billing';
import { Appointment } from '@core/models/billing';
import { BaseBehaviorSubject } from './base.behavior.subject';


export class EncounterAppointmentListSubject extends BaseBehaviorSubject<Appointment>
{
    constructor(private appointmentService: AppointmentService)
    {
        super();
    }

    public GetFor(claimId: number, clientId: number, startDate: Date): void
    {
        this.appointmentService.GetFor(claimId, clientId, startDate).subscribe(result =>
        {
            this.data = result;
            super.next(result);
        });
    }

    public GetForClaim(claimId: number): void
    {
        this.appointmentService.GetForClaim(claimId).subscribe(result =>
        {
            this.data = result;
            super.next(result);
        });
    }
}