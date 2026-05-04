export class PropagatingAppointmentData {
  typeId: number;
  startDate: string;
}

export enum Type {
  ApplyToNewAndExistingAppointments = 1,
  ApplyToNewAndExistingAppointmentsAfterDate = 2,
  ApplyToNewAppointments = 3
}