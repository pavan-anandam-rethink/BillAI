export class Appointment {
    public id: number;
    public startDate: Date;
    public startTime: Date;
    public endDate: Date;
    public endTime: Date;
    public timeRange: string;
    public clientName: string;
    public staffName: string;
    public location: string;
    public status: string;
    public appointmentDescription: string;
    public billingCode: string;
    public billingCode2: string;
    public serviceName: string;

    constructor() {
        this.id = 0;
        this.startDate = new Date();
        this.startTime = new Date();
        this.endDate = new Date();
        this.endTime = new Date();
        this.timeRange = '';
        this.clientName = '';
        this.staffName = '';
        this.location = '';
        this.status = '';
        this.appointmentDescription = '';
        this.billingCode = '';
        this.billingCode2 = '';
        this.serviceName = '';
    }
}

export class SelectableAppointment extends Appointment {
    public selected?: boolean;
}
