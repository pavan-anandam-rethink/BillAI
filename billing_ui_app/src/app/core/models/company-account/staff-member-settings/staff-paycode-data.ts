import { StaffMemberPayrollData } from './staff-member-payroll-data';

export class StaffPaycodeData {
  id: number;
  accountInfoId: number;
  name: string;
  paycode: string;
  staffMemberPayrolls: StaffMemberPayrollData[];
  dateDeleted?: string;
  canDelete?: boolean
}
