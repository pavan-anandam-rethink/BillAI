export interface CustomField {
  id: number;
  accountInfoId: number;
  label: string;
  type: number;
  order: number;
  showInProfile: boolean;
  showInList: boolean;
  dateDeleted?: Date;
  isDeleting?: boolean;  
  customFieldValues: CustomFieldValue[];
  customFieldOptions: CustomFieldOption[];
  possibleValues?: string[];
  hasOptions?: boolean
}

export class CustomFieldShort {
    label: string;
    type: number;
    id?: number;
}

export class CustomFieldValue {
  id?: number;
  name: string;
  value?: string | boolean;
  displayValue?: Date | null;
  isDateError?: boolean;
}

export class CustomFieldOption {
  id?: number;
  value: string;
  values: CustomFieldOptionValue[];
}

export class CustomFieldOptionValue {
    id?: number;
    optionId: number;
    selected: boolean;
}

export enum CustomFieldTypesEnum {
  Text = 1,
  Date = 2,
  Textarea = 3,
  DropDown = 4,
  RadioButton = 5,
  CheckBox = 6
}

export const CustomFieldTypes = [
  { name: 'Text', id: CustomFieldTypesEnum.Text },
  { name: 'Date', id: CustomFieldTypesEnum.Date },
  { name: 'Text-Area', id: CustomFieldTypesEnum.Textarea },
  { name: 'Drop-down', id: CustomFieldTypesEnum.DropDown },
  { name: 'Radio-Button', id: CustomFieldTypesEnum.RadioButton },
  { name: 'Check-Box', id: CustomFieldTypesEnum.CheckBox }
];