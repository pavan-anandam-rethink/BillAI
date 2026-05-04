export interface ClientFunderModel {
    id: number;
    funderId: number;
    funderName: string;
    funderType: number;
    serviceLines: FunderServiceLine[];
    referringProviderRequiredOnClaim: boolean;
    startDate: Date;
    endDate: Date | null;
    isActive: boolean;
    billingProviderOptionId: number | null;
}

export interface FunderServiceLine {
    mappingId: number;
    name: string;
    sequence: string;
    serviceId: number;
    billingProviderOptionId: number;
}