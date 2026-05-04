export class BillingSettings {
  public clearingHouseId: number;
  public billingProviderName: string;
  public billingProviderEmail: string;
  public billingProviderPhone: string;
  public billingProviderExtension: string;
  public billingProviderFax: string;
}

export class BillingClearingHouse {
  public Id: number;
  public IsDefault: boolean;
  public Name: string;
}