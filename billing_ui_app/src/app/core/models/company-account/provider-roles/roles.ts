import { BasicItem } from '../basic';
import { AccountRole } from '../account-role';

export class ProviderRole extends BasicItem {
  roleId?: number;
  rolePermissions: number[];
  titles: RolesTitles[];
  isParentContactRole: boolean;
}

export class RolesTitles {
  accountInfoId: number;
  accountRole: AccountRole;
  createdBy: number;
  dateCreated: string;
  dateDeleted: string;
  dateLastModified: string;
  description: string;
  id: number;
  modifiedBy: number;
  name: string;
  roleTypeId: number;
}
