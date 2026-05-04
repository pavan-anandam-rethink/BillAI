export class BasicOption {
  id: number;
  name: string;
  isUserEntry: boolean;
  isSelected: boolean;
  isIEP: boolean;
  isArchived: boolean;
  isMastered: boolean;
}

export class BasicItem extends BasicOption {
  description: string;
  deactivationNotAllowed: boolean;
  canDelete: boolean;
  dateLastModified: string;
}