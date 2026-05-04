export class RolePermissions {
  id: 0
  permissions: number[];
  permissionsAccessLevelId: number;
  permissionsLevelTypeId: number;
  permissionsLevelTypes: PermissionsLevelType[];
  roleId: number;
}

export class PermissionsLevelType {
  id?: number;
  permissionId: number | null;
  permissionsAccessLevelId: number;
  permissionLevelTypeId?: number | null;
}

export class RolePermissionsSaveModel {
  permissionsLevelTypes: PermissionsLevelType[];
  roleId: number;
}
