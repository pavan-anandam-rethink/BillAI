export interface SecuritySettings {
    id: number;
    accountInfoId?: number;
    passwordExpirationDays: number;
    passwordExpirationNotificationDays: number;
    passwordExpirationUpdatedOn?: Date;
    definePolicy: string;
    mfFactors: string[];
}