import { BasicItem } from '../basic';

export class ProviderPermission extends BasicItem {
  dependencyPermissionId: number;
  isRadioSelection: boolean;
  list: PermissionsList[];
  permissionLevelTypeId: number;
  permissionsLevelTypes: BasicItem[];
}

export class PermissionsList extends BasicItem {
  levelkey: string;
  parentAccessLevel: number;
  permissionId: number;
  isPrimary: boolean;
  restrictedForContacts: boolean;
}

