export interface Availability {
  id: number;
  dayId: number;
  startHour: number;
  endHour: number;
  startMinute: number;
  endMinute: number;
  dayName?: string;
  formatedStartTime: Date;
  formatedEndTime: Date;
}