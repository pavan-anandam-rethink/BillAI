export class StaffMemberPayrollData {
  id: number;
  memberId: number;
  staffPaycodeId: number;
  staffMemberPayrollTypeId: number;
  rate?: number;
  isDefaultBillableAppointment?: boolean;
  isDefaultNonBillableAppointment?: boolean;
  isDefaultTravelAppointment?: boolean;
  effectiveDate?: string;
  endDate?: string;
  dateDeleted?: string;
}