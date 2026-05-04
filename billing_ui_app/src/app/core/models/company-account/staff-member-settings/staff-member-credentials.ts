export class StaffMemberCredential {
  id: number;
  accountInfoId?: number;
  name: string;
  credentialTypeId: number
  credentialType?: string;
  description: string;
  inactive?: boolean;
  issueDate: Date;
  expiredDate: Date;
  staffId: number;
}