export class ProviderServiceLine {
  canDeleteServiceLine: boolean;
  deactivationNotAllowed: boolean;
  description?: string;
  id: number;
  isActive: boolean;
  isDph: boolean;
  name: string;
  billingSubmissionMethodId: number;
  billingProviderOptionId: number;
  canDelete: boolean;
}

export class ProviderService {
  baseRate: number;
  canDelete: boolean;
  id: number;
  isActive: boolean;
  name: string;
  propagatingData: any | undefined;
}

export class ServiceLines {
  serviceLines: ProviderServiceLine[];
  services: ProviderService[];
  showDph: boolean;
}