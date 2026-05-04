import { AccountRole } from '../account-role';

export interface StaffTitleData {
  id: number
  accountInfoId: number;
  name: string;
  description: string;
  roleTypeId: number;
  createdBy: number;
  modifiedBy: number;
  dateCreated: string;
  dateLastModified: string;
  dateDeleted?: string;
  accountRole: AccountRole;
}