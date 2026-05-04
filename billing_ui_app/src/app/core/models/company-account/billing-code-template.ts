export class BillingCodeTemplateModel {
  id: number;
  name: string;
  description: string;
  code: string;
  code2: string;
  modifier: string;
  unitTypeId: number;
  unitTypeId2?: number;
  rate?: number;
  rate2?: number;
  combined: boolean;
  duration?: number;
  durationTypeId?: number;
  disabled?: boolean;
}
