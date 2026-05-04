export interface BasicOption {
    id: number;
    name: string;
}

export interface ClientReferringProviderOption extends BasicOption {
    isDefault: boolean;
}
